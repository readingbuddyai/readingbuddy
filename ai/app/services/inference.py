import torch
import numpy as np
import onnxruntime as ort
from fastapi import UploadFile, HTTPException
from transformers import Wav2Vec2Processor
from app.services.utils_audio import load_audio_to_mono_16k, denoise_audio, chunk_audio
from app.core.config import settings
import logging

logger = logging.getLogger(__name__)

DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

MODEL_PATH = str(settings.get_model_path())
ONNX_PATH = f"{MODEL_PATH}/model.onnx"

# 모델 로드
try:
    processor = Wav2Vec2Processor.from_pretrained(MODEL_PATH)
    available = ort.get_available_providers()
    providers = ["CUDAExecutionProvider"] if "CUDAExecutionProvider" in available else ["CPUExecutionProvider"]
    session = ort.InferenceSession(ONNX_PATH, providers=providers)
    logger.info(f"ONNX 모델 로드 성공: {ONNX_PATH} (providers={providers})")
except Exception as e:
    raise RuntimeError(f"모델 로드 실패: {e}")

@torch.no_grad()
def infer_chunk_onnx(waveform: np.ndarray, sr: int = 16000):
    """단일 청크 추론 (ONNX GPU)"""
    try:
        inputs = processor(waveform, sampling_rate=sr, return_tensors="np")
        logits = session.run(None, {"input_values": inputs.input_values})[0]
        pred_ids = np.argmax(logits, axis=-1)
        text = processor.decode(pred_ids[0])
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
    """노이즈 제거 + 청크 기반 ONNX 추론"""
    try:
        # load_audio_to_mono_16k는 이미 16kHz로 리샘플링된 numpy array 반환
        wave = load_audio_to_mono_16k(file.file)
        sr = 16000  # 항상 16kHz
        wave = denoise_audio(wave, sr)
        chunks = chunk_audio(wave, sr, chunk_size=1.0, overlap=0.2)
        results = [infer_chunk_onnx(chunk, sr) for chunk in chunks]
        decoded = "".join(results).replace("|", "").replace(" ", "")

        return {"decoded_sequence": decoded}
    except Exception as e:
        logger.error(f"스트리밍 추론 중 오류: {e}")
        raise HTTPException(status_code=500, detail="청크 기반 음성 인식 중 오류가 발생했습니다.")


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
