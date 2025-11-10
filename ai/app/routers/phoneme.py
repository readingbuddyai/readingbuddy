from fastapi import APIRouter, UploadFile, Form, HTTPException
from app.services.inference import transcribe_stream, normalize_target_to_jamo
from app.services.utils_audio import detect_audio_format
from app.services.phoneme_similarity import calculate_similarity_with_feedback
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
    """자모 단위 발음 검사 (유사도 기반)"""
    validate_audio_file(file)

    if len(target) != 1:
        raise HTTPException(status_code=400, detail="자모 하나만 입력해야 합니다 (예: 'ㄱ', 'ㅏ').")

    result = transcribe_stream(file)
    model_output = result["decoded_sequence"]  # 자모 시퀀스 (예: "ㄱ ㅗ ㅑ")

    # 자모를 리스트로 변환
    decoded_tokens = model_output.split()

    # 자모는 단일 토큰이므로 각 토큰과 비교
    # 포함되어 있거나 유사한 것이 있는지 확인
    from app.services.phoneme_similarity import get_phoneme_similarity

    max_similarity = 0.0
    for token in decoded_tokens:
        sim = get_phoneme_similarity(target, token)
        if sim > max_similarity:
            max_similarity = sim

    # 자모는 더 관대하게 (80% 이상)
    is_correct = max_similarity >= 0.80

    # 피드백 생성
    if max_similarity >= 0.95:
        feedback = f"완벽해요! '{target}' 발음이 정확해요!"
    elif max_similarity >= 0.80:
        feedback = f"잘했어요! '{target}'를 잘 발음했어요!"
    elif max_similarity >= 0.60:
        feedback = f"'{target}'와 비슷하게 들렸어요. 다시 한번 해볼까요?"
    else:
        feedback = f"'{target}'를 다시 한번 발음해보세요!"

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
    """음절 단위 발음 검사 (유사도 기반)"""
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

    # 유사도 기반 평가
    evaluation = calculate_similarity_with_feedback(
        target_tokens=target_tokens,
        decoded_tokens=decoded_tokens,
        target_word=target
    )

    return SyllableCheckResponse(
        type="syllable",
        target=target,
        decomposed=target_tokens,
        decoded_tokens=decoded_tokens,
        is_correct=evaluation["is_correct"],
        feedback=evaluation["feedback"],
    )

# =================================
# 단어 단위
# =================================
@router.post("/word", response_model=WordCheckResponse, responses={400: {"model": ErrorResponse}})
async def check_word(file: UploadFile, target: str = Form(...)):
    """단어 단위 발음 검사 (유사도 기반)"""
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

    # 띄어쓰기 제거하고 비교용 토큰 생성
    target_tokens = target_jamo.replace("|", " ").split()  # "|"를 공백으로 변환 후 split
    target_tokens = [t for t in target_tokens if t]  # 빈 토큰 제거

    # 유사도 기반 평가
    evaluation = calculate_similarity_with_feedback(
        target_tokens=target_tokens,
        decoded_tokens=decoded_tokens,
        target_word=target
    )

    return WordCheckResponse(
        type="word",
        target=target,
        syllables=syllables,
        decoded_tokens=decoded_tokens,
        is_correct=evaluation["is_correct"],
        feedback=evaluation["feedback"],
    )
