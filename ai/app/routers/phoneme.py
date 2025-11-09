from fastapi import APIRouter, UploadFile, Form, HTTPException
from app.services.inference import transcribe_stream, normalize_target_to_jamo
from app.services.utils_audio import detect_audio_format
from app.core.config import settings
from app.schemas import JamoCheckResponse, SyllableCheckResponse, WordCheckResponse, ErrorResponse

router = APIRouter(prefix="/check", tags=["Pronunciation"])

def validate_audio_file(file: UploadFile):
    """오디오 파일 유효성 검사 (확장자 + 매직 바이트)"""
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

    # 매직 바이트로 실제 파일 포맷 검증 (보안 강화)
    file_content = file.file.read(16)  # 첫 16바이트만 읽기
    file.file.seek(0)  # 파일 포인터 리셋

    detected_format = detect_audio_format(file_content)
    if detected_format == "unknown":
        raise HTTPException(
            status_code=400,
            detail="지원하지 않는 오디오 포맷입니다. 파일 내용을 확인할 수 없습니다."
        )

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
    model_output = result["decoded_sequence"]  # 자모 시퀀스 (예: "ㄱ ㅗ ㅑ")

    # 자모를 리스트로 변환
    decoded_tokens = model_output.split()
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

    # 정답을 자모로 변환
    target_jamo = normalize_target_to_jamo(target)
    target_tokens = target_jamo.split()

    # 모델 출력
    result = transcribe_stream(file)
    model_output = result["decoded_sequence"]
    decoded_tokens = model_output.split()

    # 비교
    is_correct = (target_jamo == model_output)
    feedback = f"'{target}' 발음이 정확해요!" if is_correct else f"'{target}'의 발음이 달라요."

    return SyllableCheckResponse(
        type="syllable",
        target=target,
        decomposed=target_tokens,
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

    if not target or not all("가" <= ch <= "힣" or ch == " " for ch in target):
        raise HTTPException(status_code=400, detail="한글 단어만 입력해야 합니다 (예: '감자').")

    # 정답을 자모로 변환
    target_jamo = normalize_target_to_jamo(target)

    # 각 음절별로 자모 분해 (화면 표시용)
    syllables = []
    for ch in target:
        if ch == " ":
            continue
        ch_jamo = normalize_target_to_jamo(ch)
        syllables.append(ch_jamo.split())

    # 모델 출력
    result = transcribe_stream(file)
    model_output = result["decoded_sequence"]
    decoded_tokens = model_output.split()

    # 비교 (띄어쓰기 무시하고 비교)
    target_no_space = target_jamo.replace("|", "").replace(" ", "").strip()
    model_no_space = model_output.replace("|", "").replace(" ", "").strip()
    is_correct = (target_no_space == model_no_space)

    feedback = f"'{target}' 발음이 정확해요!" if is_correct else f"'{target}' 발음이 달라요."

    return WordCheckResponse(
        type="word",
        target=target,
        syllables=syllables,
        decoded_tokens=decoded_tokens,
        is_correct=is_correct,
        feedback=feedback,
    )
