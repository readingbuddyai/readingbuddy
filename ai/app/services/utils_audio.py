import io
import numpy as np
import soundfile as sf
from fastapi import HTTPException
from app.core.config import settings

try:
    import librosa
    LIBROSA_AVAILABLE = True
except ImportError:
    LIBROSA_AVAILABLE = False

def load_audio_to_mono_16k(file_obj) -> np.ndarray:
    """UploadFile → 16kHz mono numpy 변환"""
    try:
        if isinstance(file_obj, bytes):
            data, sr = sf.read(io.BytesIO(file_obj))
        else:
            data, sr = sf.read(file_obj)
    except Exception as e:
        raise HTTPException(
            status_code=400,
            detail=f"오디오 파일을 읽을 수 없습니다: {str(e)}"
        )

    # 빈 오디오 체크
    if len(data) == 0:
        raise HTTPException(status_code=400, detail="빈 오디오 파일입니다.")

    # 오디오 길이 체크
    max_length = settings.MAX_AUDIO_LENGTH_SECONDS
    if len(data) / sr > max_length:
        raise HTTPException(
            status_code=400,
            detail=f"오디오 파일이 너무 깁니다 (최대 {max_length}초)."
        )

    # stereo → mono
    if data.ndim > 1:
        data = np.mean(data, axis=1)

    # 리샘플링
    if sr != 16000:
        if not LIBROSA_AVAILABLE:
            raise HTTPException(
                status_code=500,
                detail="리샘플링을 위한 librosa 라이브러리가 설치되지 않았습니다."
            )
        try:
            data = librosa.resample(data, orig_sr=sr, target_sr=16000)
        except Exception as e:
            raise HTTPException(
                status_code=500,
                detail=f"오디오 리샘플링 중 오류 발생: {str(e)}"
            )

    return data.astype(np.float32)
