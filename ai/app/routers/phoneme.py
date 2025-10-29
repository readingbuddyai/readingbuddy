from fastapi import APIRouter, UploadFile, Form, HTTPException
from app.services.inference import transcribe_stream, clean_tokens, decompose_to_jamo
from app.core.config import settings
from app.schemas import JamoCheckResponse, SyllableCheckResponse, WordCheckResponse, ErrorResponse

router = APIRouter(prefix="/check", tags=["Pronunciation"])

def validate_audio_file(file: UploadFile):
    """오디오 파일 유효성 검사"""
    if not file:
        raise HTTPException(status_code=400, detail="오디오 파일이 없습니다.")

    # 파일 확장자 확인
    if file.filename:
        file_ext = "." + file.filename.split(".")[-1].lower()
        if file_ext not in settings.ALLOWED_AUDIO_EXTENSIONS:
            raise HTTPException(
                status_code=400,
                detail=f"지원하지 않는 파일 형식입니다. 허용 형식: {', '.join(settings.ALLOWED_AUDIO_EXTENSIONS)}"
            )

    # Content-Type 확인 (선택적)
    if file.content_type and not file.content_type.startswith("audio/"):
        raise HTTPException(status_code=400, detail="오디오 파일만 업로드 가능합니다.")

from fastapi import APIRouter, UploadFile, Form, HTTPException
# ✅ ONNX 추론 함수로 변경
from app.services.inference import transcribe_stream, clean_tokens, decompose_to_jamo
from app.core.config import settings
from app.schemas import JamoCheckResponse, SyllableCheckResponse, WordCheckResponse, ErrorResponse

router = APIRouter(prefix="/check", tags=["Pronunciation"])

# =================================
# 자모 단위
# =================================
@router.post("/jamo", response_model=JamoCheckResponse, responses={400: {"model": ErrorResponse}})
async def check_jamo(file: UploadFile, target: str = Form(...)):
    """자모 단위 발음 검사"""
    validate_audio_file(file)

    if len(target) != 1:
        raise HTTPException(status_code=400, detail="자모 하나만 입력해야 합니다 (예: 'ㄱ', 'ㅏ').")

    result = transcribe_stream(file)
    decoded_tokens = clean_tokens(result["decoded_sequence"])
    is_correct = target in decoded_tokens

    feedback = f"'{target}' 발음이 정확해요!" if is_correct else f"'{target}'로 인식되지 않았어요."
    return JamoCheckResponse(
        type="jamo",
        target=target,
        decoded_tokens=decoded_tokens,
        is_correct=is_correct,
        feedback=feedback,
    )

# =================================
# 음절 단위
# =================================
@router.post("/syllable", response_model=SyllableCheckResponse, responses={400: {"model": ErrorResponse}})
async def check_syllable(file: UploadFile, target: str = Form(...)):
    """음절 단위 발음 검사"""
    validate_audio_file(file)

    if len(target) != 1:
        raise HTTPException(status_code=400, detail="한 글자만 입력해야 합니다 (예: '가').")

    jamos = decompose_to_jamo(target)

    # ✅ 교체
    result = transcribe_stream(file)
    decoded_tokens = clean_tokens(result["decoded_sequence"])

    matched = [j for j in jamos if j in decoded_tokens]
    is_correct = len(matched) == len(jamos)
    feedback = f"'{target}' 발음이 정확해요!" if is_correct else f"'{target}'의 일부 발음이 달라요."

    return SyllableCheckResponse(
        type="syllable",
        target=target,
        decomposed=jamos,
        decoded_tokens=decoded_tokens,
        is_correct=is_correct,
        feedback=feedback,
    )

# =================================
# 단어 단위
# =================================
@router.post("/word", response_model=WordCheckResponse, responses={400: {"model": ErrorResponse}})
async def check_word(file: UploadFile, target: str = Form(...)):
    """단어 단위 발음 검사"""
    validate_audio_file(file)

    if not target or not all("가" <= ch <= "힣" for ch in target):
        raise HTTPException(status_code=400, detail="한글 단어만 입력해야 합니다 (예: '감자').")

    syllables = [decompose_to_jamo(ch) for ch in target]

    # ✅ 교체
    result = transcribe_stream(file)
    decoded_tokens = clean_tokens(result["decoded_sequence"])

    flat_target = sum(syllables, [])
    is_correct = all(j in decoded_tokens for j in flat_target)
    feedback = f"'{target}' 발음이 정확해요!" if is_correct else f"'{target}' 발음이 달라요."

    return WordCheckResponse(
        type="word",
        target=target,
        syllables=syllables,
        decoded_tokens=decoded_tokens,
        is_correct=is_correct,
        feedback=feedback,
    )
