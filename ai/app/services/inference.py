import torch
import time
import numpy as np
import onnxruntime as ort
from fastapi import UploadFile, HTTPException
from transformers import Wav2Vec2Processor
from app.services.utils_audio import load_audio_to_mono_16k, chunk_audio
from app.core.config import settings
import logging
import threading

logger = logging.getLogger(__name__)

# 자모 분류 (vocab.json 기반)
VOWELS = {
    "ㅏ", "ㅓ", "ㅔ", "ㅣ", "ㅗ", "ㅜ", "ㅡ", "ㅢ",
    "ㅑ", "ㅕ", "ㅛ", "ㅠ", "ㅖ",
    "ㅘ", "ㅙ", "ㅝ", "ㅟ"
}

ONSETS = {
    "ㄱ", "ㄲ", "ㄴ", "ㄷ", "ㄸ", "ㄹ", "ㅁ", "ㅂ", "ㅃ",
    "ㅅ", "ㅆ", "ㅇ", "ㅈ", "ㅉ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ"
}

CODAS = {"ㄱ*", "ㄷ*", "ㅂ*", "ㄹ*", "ㄴ", "ㅁ", "ㅇ"}

# 종성 정규화 (7종성 법칙)
TO_CODA = {
    # ㄱ 계열
    "ㄱ": "ㄱ*", "ㄲ": "ㄱ*", "ㅋ": "ㄱ*",
    # ㄴ 계열
    "ㄴ": "ㄴ",
    # ㄷ 계열
    "ㄷ": "ㄷ*", "ㅅ": "ㄷ*", "ㅆ": "ㄷ*", "ㅈ": "ㄷ*",
    "ㅊ": "ㄷ*", "ㅌ": "ㄷ*", "ㅎ": "ㄷ*",
    # ㄹ 계열
    "ㄹ": "ㄹ*",
    # ㅁ 계열
    "ㅁ": "ㅁ",
    # ㅂ 계열
    "ㅂ": "ㅂ*", "ㅍ": "ㅂ*", "ㅃ": "ㅂ*",
    # ㅇ 계열
    "ㅇ": "ㅇ",
}

DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

MODEL_PATH = str(settings.get_model_path())
ONNX_PATH = f"{MODEL_PATH}/model.onnx"

# 모델 로드
try:
    processor = Wav2Vec2Processor.from_pretrained(MODEL_PATH)
    available = ort.get_available_providers()
    providers = ["CUDAExecutionProvider"] if "CUDAExecutionProvider" in available else ["CPUExecutionProvider"]

    # SessionOptions 최적화
    sess_options = ort.SessionOptions()
    sess_options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
    sess_options.enable_mem_pattern = True
    sess_options.enable_cpu_mem_arena = True

    session = ort.InferenceSession(ONNX_PATH, sess_options, providers=providers)
    logger.info(f"ONNX 모델 로드 성공: {ONNX_PATH} (providers={providers})")

    # 웜업 추론 (콜드 런 오버헤드 제거)
    logger.info("웜업 추론 시작...")
    warmup_audio = np.random.randn(1, 16000).astype(np.float32)  # 1초 더미 오디오
    for i in range(5):
        _ = session.run(None, {"audio": warmup_audio})
    logger.info("웜업 추론 완료 - 모델 준비됨")

    # GPU Keep-Alive: 백그라운드에서 주기적으로 더미 추론 실행 (GPU 절전 방지)
    def gpu_keepalive():
        """GPU가 절전 모드로 들어가지 않도록 주기적으로 더미 추론 실행"""
        dummy = np.random.randn(1, 8000).astype(np.float32)  # 0.5초 짧은 오디오
        while True:
            try:
                time.sleep(3)  # 3초마다 실행
                _ = session.run(None, {"audio": dummy})
            except Exception:
                break

    # 백그라운드 스레드로 GPU keep-alive 시작
    keepalive_thread = threading.Thread(target=gpu_keepalive, daemon=True)
    keepalive_thread.start()
    logger.info("GPU keep-alive 스레드 시작 (3초 간격)")

except Exception as e:
    raise RuntimeError(f"모델 로드 실패: {e}")

@torch.no_grad()
def infer_chunk_onnx(waveform: np.ndarray, sr: int = 16000):
    """단일 청크 추론 (ONNX GPU)"""
    try:
        # 1. Wav2Vec2 Processor 전처리
        t_prep = time.time()
        inputs = processor(waveform, sampling_rate=sr, return_tensors="np")
        prep_time = time.time() - t_prep

        # 2. ONNX 모델 추론 (순수 모델 실행)
        t_onnx = time.time()
        logits = session.run(None, {"audio": inputs.input_values})[0]
        onnx_time = time.time() - t_onnx

        # 3. Argmax 연산
        t_argmax = time.time()
        pred_ids = np.argmax(logits, axis=-1)
        argmax_time = time.time() - t_argmax

        # 4. Processor 디코딩 (vocab.json 사용)
        t_decode = time.time()
        text = processor.decode(pred_ids[0])
        decode_time = time.time() - t_decode

        # 세부 시간 로그 출력
        print(f"[전처리] Processor: {prep_time:.4f}초")
        print(f"[ONNX 추론] 순수 모델: {onnx_time:.4f}초")
        print(f"[Argmax] 연산: {argmax_time:.4f}초")
        print(f"[디코딩] Vocab 변환: {decode_time:.4f}초")
        print(f"[합계] {prep_time + onnx_time + argmax_time + decode_time:.4f}초")

        return text
    except Exception as e:
        logger.error(f"ONNX 추론 중 오류: {e}")
        raise HTTPException(status_code=500, detail="ONNX 추론 중 오류가 발생했습니다.")


def ctc_decode(logits: torch.Tensor) -> str:
    try:
        ids = torch.argmax(logits, dim=-1).detach().cpu().numpy()
        return processor.batch_decode(torch.tensor([ids]))[0]
    except Exception as e:
        logger.error(f"CTC 디코딩 중 오류: {e}")
        raise HTTPException(status_code=500, detail=f"음성 디코딩 중 오류가 발생했습니다: {str(e)}")


def transcribe_stream(file: UploadFile):
    start_time = time.time()
    
    try:
        # 1단계: 오디오 로딩
        t1 = time.time()
        wave = load_audio_to_mono_16k(file.file)
        logger.info(f"오디오 로딩: {time.time() - t1:.3f}초")
        
        sr = 16000
        
        # # 2단계: 노이즈 제거
        # t2 = time.time()
        # wave = denoise_audio(wave, sr)
        # logger.info(f"노이즈 제거: {time.time() - t2:.3f}초")
        
        # 오디오 길이 계산
        audio_length = len(wave) / sr
        
        # 3단계: 추론
        print(f"\n{'='*60}")
        print(f"[추론 시작] 오디오 길이: {audio_length:.2f}초")
        print(f"{'='*60}")

        t3 = time.time()
        if audio_length <= 5.0:
            print(f"모드: 전체 추론 (청크 분리 없음)")
            result = infer_chunk_onnx(wave, sr)

            t_postprocess = time.time()
            decoded = result  # 자모 시퀀스 그대로 반환
            postprocess_time = time.time() - t_postprocess
            print(f"[후처리] 문자열 처리: {postprocess_time:.4f}초")
        else:
            print(f"모드: 청크 분리 추론")
            t_chunk = time.time()
            chunks = chunk_audio(wave, sr, chunk_size=1.0, overlap=0.2)
            chunk_time = time.time() - t_chunk
            print(f"[청크 분할] {len(chunks)}개 생성: {chunk_time:.4f}초")

            results = []
            for i, chunk in enumerate(chunks):
                print(f"\n[청크 {i+1}/{len(chunks)}]")
                result = infer_chunk_onnx(chunk, sr)
                results.append(result)

            t_postprocess = time.time()
            # 청크 결과를 공백으로 합치기
            decoded = " ".join(results).strip()
            postprocess_time = time.time() - t_postprocess
            print(f"\n[후처리] 문자열 처리: {postprocess_time:.4f}초")

        total_inference_time = time.time() - t3
        print(f"\n{'='*60}")
        print(f"[추론 완료] 총 시간: {total_inference_time:.4f}초")
        print(f"{'='*60}\n")
        logger.info(f"모델 추론: {total_inference_time:.3f}초")
        
        # 전체 시간
        elapsed_time = time.time() - start_time
        logger.info(f"[전체] 처리 시간: {elapsed_time:.3f}초 (오디오: {audio_length:.2f}초)")

        return {
            "decoded_sequence": decoded
        }
    except Exception as e:
        logger.error(f"스트리밍 추론 중 오류: {e}")
        raise HTTPException(status_code=500, detail="청크 기반 음성 인식 중 오류가 발생했습니다.")
    

def normalize_target_to_jamo(target_word):
    """
    정답 단어를 모델 출력과 동일한 자모 형식으로 변환

    Args:
        target_word: 완성형 한글 단어 (예: "고양이")

    Returns:
        자모 시퀀스 문자열 (예: "ㄱ ㅗ ㅑ ㄴ ㅣ")

    규칙:
    - 초성 ㅇ은 제거 (묵음)
    - 종성은 vocab.json의 TO_CODA 규칙에 따라 정규화 (ㄱ → ㄱ* 등)
    """
    jamo_list = []

    for char in target_word:
        # 공백이나 특수문자는 그대로 추가
        if char == ' ':
            jamo_list.append('|')
            continue

        # 한글 완성형인 경우만 분해
        if '가' <= char <= '힣':
            parts = decompose_to_jamo(char)

            # parts는 [초성, 중성] 또는 [초성, 중성, 종성] 형태의 리스트
            cho = parts[0]
            jung = parts[1]
            jong = parts[2] if len(parts) == 3 else None

            # 초성 추가 (ㅇ은 제외 - 묵음)
            if cho != 'ㅇ':
                jamo_list.append(cho)

            # 중성 추가
            jamo_list.append(jung)

            # 종성 추가 (있는 경우)
            if jong:
                # TO_CODA 규칙에 따라 정규화
                normalized_jong = TO_CODA.get(jong, jong)
                jamo_list.append(normalized_jong)

    return " ".join(jamo_list)


def compose_hangul(cho, jung, jong):
    """
    자모를 조합하여 완성형 한글 생성

    Args:
        cho: 초성 ('ㄱ', 'ㄲ', None 등) - None이면 초성 ㅇ으로 처리
        jung: 중성 ('ㅏ', 'ㅓ' 등)
        jong: 종성 ('ㄱ*', 'ㄴ', None 등) - None이면 받침 없음

    Returns:
        완성형 한글 문자
    """
    CHO = ['ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ','ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ']
    JUNG = ['ㅏ','ㅐ','ㅑ','ㅒ','ㅓ','ㅔ','ㅕ','ㅖ','ㅗ','ㅘ','ㅙ','ㅚ','ㅛ','ㅜ','ㅝ','ㅞ','ㅟ','ㅠ','ㅡ','ㅢ','ㅣ']
    JONG = ['','ㄱ','ㄲ','ㄳ','ㄴ','ㄵ','ㄶ','ㄷ','ㄹ','ㄺ','ㄻ','ㄼ','ㄽ','ㄾ','ㄿ','ㅀ','ㅁ','ㅂ','ㅄ','ㅅ','ㅆ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ']

    # vocab.json의 별표(*) → 표준 종성 매핑
    JONG_MAP = {
        "ㄱ*": "ㄱ", "ㄷ*": "ㄷ", "ㅂ*": "ㅂ", "ㄹ*": "ㄹ",
        "ㄴ": "ㄴ", "ㅁ": "ㅁ", "ㅇ": "ㅇ"
    }

    # 초성 처리 (None이면 ㅇ)
    if cho is None:
        cho = "ㅇ"
    cho_idx = CHO.index(cho)

    # 중성 처리
    jung_idx = JUNG.index(jung)

    # 종성 처리
    if jong is None:
        jong_idx = 0
    else:
        standard_jong = JONG_MAP.get(jong, jong)
        jong_idx = JONG.index(standard_jong)

    # 유니코드 조합
    code = 0xAC00 + (cho_idx * 588) + (jung_idx * 28) + jong_idx
    return chr(code)


def jamo_to_hangul(raw_text):
    """
    processor.decode() 결과를 완성형 한글로 변환

    Args:
        raw_text: "ㄱ ㅏ ㄴ* | ㅇ ㅏ ㅈ ㅣ" 같은 자모 문자열

    Returns:
        "강 아지" 같은 완성형 한글 문자열
    """
    # 공백으로 토큰 분리
    tokens = raw_text.split()

    result = []
    i = 0

    while i < len(tokens):
        token = tokens[i]

        # 특수 토큰 처리
        if token == "|":
            result.append(" ")
            i += 1
            continue

        if token in ["[UNK]", "[PAD]", "<s>", "</s>"]:
            i += 1
            continue

        # 모음이 단독으로 나오면 초성 ㅇ(묵음) + 모음 처리
        if token in VOWELS:
            cho = None  # 초성 ㅇ은 묵음
            jung = token
            jong = None

            # 다음이 종성인지 확인
            if i + 1 < len(tokens):
                next_token = tokens[i + 1]

                # Case 1: 별표(*) 있으면 종성
                if next_token in CODAS:
                    jong = next_token
                    i += 2

                # Case 2: 별표 없는 자음 → 위치로 판단
                elif next_token in TO_CODA:
                    # 그 다음이 모음이 아니면 종성으로 처리
                    if i + 2 >= len(tokens) or tokens[i + 2] not in VOWELS:
                        jong = TO_CODA[next_token]
                        i += 2
                    else:
                        i += 1
                else:
                    i += 1
            else:
                i += 1

            # 한글 조합
            try:
                syllable = compose_hangul(cho, jung, jong)
                result.append(syllable)
            except (ValueError, IndexError) as e:
                logger.warning(f"조합 실패 (단독 모음): cho={cho}, jung={jung}, jong={jong}, error={e}")

        # 초성 감지
        elif token in ONSETS:
            cho = token

            # 다음 토큰이 중성인지 확인
            if i + 1 < len(tokens) and tokens[i + 1] in VOWELS:
                jung = tokens[i + 1]
                jong = None

                # 초성 ㅇ은 묵음 처리
                if cho == "ㅇ":
                    cho = None

                # 종성 확인
                if i + 2 < len(tokens):
                    next_token = tokens[i + 2]

                    # Case 1: 이미 별표(*)가 있으면 종성
                    if next_token in CODAS:
                        jong = next_token
                        i += 3

                    # Case 2: 별표 없는 자음 → 위치로 판단
                    elif next_token in TO_CODA:
                        # 그 다음이 모음이 아니면 종성으로 처리
                        if i + 3 >= len(tokens) or tokens[i + 3] not in VOWELS:
                            jong = TO_CODA[next_token]
                            i += 3
                        else:
                            # 다음이 모음이면 현재 음절은 종성 없음
                            i += 2
                    else:
                        i += 2
                else:
                    i += 2

                # 한글 조합
                try:
                    syllable = compose_hangul(cho, jung, jong)
                    result.append(syllable)
                except (ValueError, IndexError) as e:
                    logger.warning(f"조합 실패: cho={cho}, jung={jung}, jong={jong}, error={e}")
                    i += 1
            else:
                i += 1
        else:
            # 기타 토큰 (처리 안 함)
            i += 1

    return "".join(result)


def clean_tokens(seq: str):
    SPECIAL_TOKENS = {"<s>", "</s>", "|", "[PAD]", "[UNK]"}
    toks = [t for t in seq.split() if t and t not in SPECIAL_TOKENS]
    return toks

def decompose_to_jamo(syllable: str):
    CHO = [ 'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ','ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ']
    JUNG = [ 'ㅏ','ㅐ','ㅑ','ㅒ','ㅓ','ㅔ','ㅕ','ㅖ','ㅗ','ㅘ','ㅙ','ㅚ','ㅛ','ㅜ','ㅝ','ㅞ','ㅟ','ㅠ','ㅡ','ㅢ','ㅣ']
    JONG = [ '', 'ㄱ','ㄲ','ㄳ','ㄴ','ㄵ','ㄶ','ㄷ','ㄹ','ㄺ','ㄻ','ㄼ','ㄽ','ㄾ','ㄿ','ㅀ','ㅁ','ㅂ','ㅄ','ㅅ','ㅆ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ']

    base = ord(syllable) - 0xAC00
    cho = base // 588
    jung = (base - (588 * cho)) // 28
    jong = base % 28
    parts = [CHO[cho], JUNG[jung]]
    if JONG[jong] != '':
        parts.append(JONG[jong])
    return parts
