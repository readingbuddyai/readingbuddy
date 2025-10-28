import torch
import numpy as np
from fastapi import UploadFile, HTTPException
from transformers import Wav2Vec2Processor, Wav2Vec2ForCTC
from app.services.utils_audio import load_audio_to_mono_16k
from app.core.config import settings
import logging

logger = logging.getLogger(__name__)

DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

# 모델 로드
try:
    model_path = str(settings.get_model_path())
    processor = Wav2Vec2Processor.from_pretrained(model_path)
    model = Wav2Vec2ForCTC.from_pretrained(model_path).to(DEVICE)
    model.eval()
    logger.info(f"모델 로드 성공: {model_path} (device: {DEVICE})")
except FileNotFoundError:
    raise RuntimeError(f"모델을 찾을 수 없습니다: {settings.get_model_path()}")
except Exception as e:
    raise RuntimeError(f"모델 로드 실패: {e}")

@torch.no_grad()
def infer_logits(wave_16k: np.ndarray):
    try:
        inputs = processor(wave_16k, sampling_rate=16000, return_tensors="pt", padding="longest")
        logits = model(inputs.input_values.to(DEVICE)).logits
        return logits.squeeze(0)
    except Exception as e:
        logger.error(f"모델 추론 중 오류: {e}")
        raise HTTPException(status_code=500, detail=f"음성 인식 중 오류가 발생했습니다: {str(e)}")

def ctc_decode(logits: torch.Tensor) -> str:
    try:
        ids = torch.argmax(logits, dim=-1).detach().cpu().numpy()
        return processor.batch_decode(torch.tensor([ids]))[0]
    except Exception as e:
        logger.error(f"CTC 디코딩 중 오류: {e}")
        raise HTTPException(status_code=500, detail=f"음성 디코딩 중 오류가 발생했습니다: {str(e)}")

def transcribe_audio(file: UploadFile):
    try:
        wave = load_audio_to_mono_16k(file.file)
        logits = infer_logits(wave)
        decoded = ctc_decode(logits)
        return {"decoded_sequence": decoded}
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"음성 변환 중 예상치 못한 오류: {e}")
        raise HTTPException(status_code=500, detail="음성 처리 중 오류가 발생했습니다.")

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
