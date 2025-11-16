import io
import numpy as np
from fastapi import HTTPException, UploadFile
from app.core.config import settings
import tempfile
import os
import logging

logger = logging.getLogger(__name__)

def detect_audio_format(file_content: bytes) -> str:
    """파일 내용의 매직 바이트로 오디오 포맷 감지"""
    if not file_content or len(file_content) < 16:
        return "unknown"

    # 매직 바이트 체크
    header = file_content[:16]

    # WebM/Matroska (EBML header: 0x1A 0x45 0xDF 0xA3)
    if header[0:4] == b'\x1a\x45\xdf\xa3':
        return "webm"

    # OGG (OggS)
    if header[0:4] == b'OggS':
        return "ogg"

    # WAV (RIFF....WAVE)
    if header[0:4] == b'RIFF' and header[8:12] == b'WAVE':
        return "wav"

    # MP3 (ID3 or 0xFF 0xFB)
    if header[0:3] == b'ID3' or (header[0] == 0xFF and (header[1] & 0xE0) == 0xE0):
        return "mp3"

    # FLAC (fLaC)
    if header[0:4] == b'fLaC':
        return "flac"

    # M4A/MP4 (ftyp)
    if header[4:8] == b'ftyp':
        return "m4a"

    return "unknown"

def load_audio_to_mono_16k(file_obj) -> np.ndarray:
    """UploadFile → 16kHz mono numpy 변환 (WebM 포맷 지원)"""
    # Lazy import - 필요할 때만 로드
    import soundfile as sf
    from pydub import AudioSegment
    import librosa

    # UploadFile 객체인 경우 파일명과 내용 가져오기
    filename = None
    file_content = None

    if hasattr(file_obj, 'filename'):
        # FastAPI UploadFile 객체
        filename = file_obj.filename
        file_content = file_obj.file.read()
        file_obj.file.seek(0)  # 파일 포인터 리셋
    elif isinstance(file_obj, bytes):
        # bytes 객체
        filename = "audio"
        file_content = file_obj
    else:
        # 다른 파일 객체
        filename = getattr(file_obj, 'name', 'audio')
        if hasattr(file_obj, 'read'):
            file_content = file_obj.read()
            if hasattr(file_obj, 'seek'):
                file_obj.seek(0)
        else:
            file_content = file_obj

    max_size_bytes = int(settings.MAX_FILE_SIZE_MB * 1024 * 1024)
    if file_content and len(file_content) > max_size_bytes:
        raise HTTPException(
            status_code=400,
            detail=f"오디오 파일이 너무 큽니다 (최대 {settings.MAX_FILE_SIZE_MB}MB)."
        )

    # 매직 바이트로 오디오 포맷 감지
    detected_format = detect_audio_format(file_content) if file_content else "unknown"
    is_webm = detected_format in ["webm", "ogg"]

    logger.debug(f"파일 처리 - filename={filename}, format={detected_format}, webm={is_webm}, size={len(file_content) if file_content else 0}")

    try:
        # WebM/OGG 포맷인 경우 pydub으로 변환
        if is_webm:
            logger.info("WebM/OGG 포맷 감지 - pydub으로 변환 중")

            # 임시 파일로 저장
            with tempfile.NamedTemporaryFile(delete=False, suffix='.webm') as tmp_input:
                tmp_input.write(file_content)
                tmp_input_path = tmp_input.name

            try:
                # pydub으로 WebM 로드
                audio = AudioSegment.from_file(tmp_input_path, format="webm")

                # WAV로 변환하여 메모리에 저장
                wav_io = io.BytesIO()
                audio.export(wav_io, format='wav')
                wav_io.seek(0)

                # soundfile로 읽기
                data, sr = sf.read(wav_io)
                logger.info(f"WebM 변환 성공 - SR: {sr}, Shape: {data.shape}")

            finally:
                # 임시 파일 삭제
                if os.path.exists(tmp_input_path):
                    os.unlink(tmp_input_path)
        else:
            # 일반 포맷 처리 (WAV, MP3 등)
            if file_content:
                try:
                    # soundfile로 먼저 시도 (WAV, FLAC, OGG/Vorbis)
                    data, sr = sf.read(io.BytesIO(file_content))
                    logger.debug(f"soundfile 로드 성공 - format={detected_format}, SR: {sr}, Shape: {data.shape}")
                except Exception as sf_error:
                    logger.info(f"soundfile 실패, pydub으로 재시도 - {sf_error}")
                    # soundfile이 실패하면 pydub으로 시도 (MP3, M4A 등)
                    with tempfile.NamedTemporaryFile(delete=False, suffix=f'.{detected_format}') as tmp_input:
                        tmp_input.write(file_content)
                        tmp_input_path = tmp_input.name

                    try:
                        audio = AudioSegment.from_file(tmp_input_path)
                        wav_io = io.BytesIO()
                        audio.export(wav_io, format='wav')
                        wav_io.seek(0)
                        data, sr = sf.read(wav_io)
                        logger.info(f"pydub fallback 성공 - SR: {sr}, Shape: {data.shape}")
                    finally:
                        if os.path.exists(tmp_input_path):
                            os.unlink(tmp_input_path)
            else:
                raise HTTPException(
                    status_code=400,
                    detail="파일 내용을 읽을 수 없습니다."
                )

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"오디오 읽기 오류 - {type(e).__name__}: {str(e)}", exc_info=True)
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
        try:
            data = librosa.resample(data, orig_sr=sr, target_sr=16000)
        except Exception as e:
            raise HTTPException(
                status_code=500,
                detail=f"오디오 리샘플링 중 오류 발생: {str(e)}"
            )

    return data.astype(np.float32)

# # 노이즈 제거
# def denoise_audio(waveform: np.ndarray, sr: int = 16000) -> np.ndarray:
#     """노이즈 제거"""
#     try:
#         reduced = nr.reduce_noise(y=waveform, sr=sr)
#         return reduced.astype(np.float32)
#     except Exception as e:
#         print(f"[Noise Reduction 실패] {e}")
#         return waveform
    

# 청크 분할
def chunk_audio(waveform: np.ndarray, sr: int = 16000, chunk_size=1.0, overlap=0.2):
    """1초 단위 청크 분할 (겹침 포함)"""
    samples_per_chunk = int(sr * chunk_size)
    step = int(samples_per_chunk * (1 - overlap))
    chunks = []
    for start in range(0, len(waveform) - samples_per_chunk + 1, step):
        chunks.append(waveform[start:start + samples_per_chunk])
    return chunks
