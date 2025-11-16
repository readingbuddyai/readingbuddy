"""
음소 유사도 계산 모듈
- 난독증 아동의 발음 평가를 위한 유연한 매칭 시스템
- 실제 아동 발음 장애 연구 데이터 기반 (Wav2Vec2 XLS-R, 137명 아동 데이터)
"""

import logging
from typing import List, Dict, Tuple

logger = logging.getLogger(__name__)

# ==========================================
# 1. 발음이 실제로 같은 모음 그룹
# ==========================================
VOWEL_EQUIVALENCE_GROUPS = [
    {"ㅐ", "ㅔ"},           # 애/에 - 현대 한국어에서 발음 구분 안 됨
    {"ㅒ", "ㅖ"},           # 얘/예 - 현대 한국어에서 발음 구분 안 됨
    {"ㅙ", "ㅞ", "ㅚ"},      # 왜/웨/외 - 대부분의 화자가 구분 못함
]

# ==========================================
# 2. 음소 간 유사도 점수 (실제 아동 데이터 기반)
# ==========================================
# 출처: "Automatic Speech Recognition (ASR) for the Diagnosis of pronunciation
#       of Speech Sound Disorders in Korean children" (2024)
#       - 137명 한국 아동 (발음 장애)
#       - Wav2Vec2 XLS-R 모델 기반
#       - 90% 언어치료사 판단 일치

PHONEME_SIMILARITY = {
    # ===== 모음 - 실제 발음 동일 =====
    ("ㅐ", "ㅔ"): 1.0,    # 완전히 같은 발음
    ("ㅒ", "ㅖ"): 1.0,    # 완전히 같은 발음
    ("ㅙ", "ㅞ"): 1.0,    # 완전히 같은 발음
    ("ㅙ", "ㅚ"): 0.95,   # 거의 같게 들림
    ("ㅞ", "ㅚ"): 0.95,   # 거의 같게 들림

    # ===== 자음 - 아동 발음 장애 연구 데이터 =====
    ("ㄷ", "ㅈ"): 0.85,   # 아동이 가장 많이 혼동 (치경음, 연구 증명)
    ("ㄴ", "ㅇ"): 0.80,   # 높은 대체 오류율 (연구 증명)
    ("ㅈ", "ㅉ"): 0.75,   # 후치경 마찰음, 높은 오류율 (연구 증명)

    # ===== 자음 - 삼중 대립 (평음/격음/경음) =====
    ("ㄱ", "ㅋ"): 0.80,   # 같은 위치, 세기만 다름
    ("ㄱ", "ㄲ"): 0.75,
    ("ㅋ", "ㄲ"): 0.65,

    ("ㄷ", "ㅌ"): 0.80,
    ("ㄷ", "ㄸ"): 0.75,
    ("ㅌ", "ㄸ"): 0.65,

    ("ㅂ", "ㅍ"): 0.80,
    ("ㅂ", "ㅃ"): 0.75,
    ("ㅍ", "ㅃ"): 0.65,

    ("ㅈ", "ㅊ"): 0.80,
    ("ㅊ", "ㅉ"): 0.65,

    ("ㅅ", "ㅆ"): 0.75,

    # ===== 자음 - 같은 조음 위치 =====
    ("ㄴ", "ㄷ"): 0.60,   # 둘 다 치경음
    ("ㄴ", "ㄹ"): 0.55,   # 둘 다 치경음
    ("ㅁ", "ㅂ"): 0.60,   # 둘 다 양순음
    ("ㅇ", "ㄱ"): 0.55,   # 둘 다 연구개음

    # ===== 종성 정규화 (별표 있는 것과 없는 것) =====
    ("ㄱ*", "ㄱ"): 0.90,
    ("ㄷ*", "ㄷ"): 0.90,
    ("ㅂ*", "ㅂ"): 0.90,
    ("ㄹ*", "ㄹ"): 0.90,
}


def get_phoneme_similarity(phone1: str, phone2: str) -> float:
    """
    두 음소 간 유사도 점수 반환

    Args:
        phone1: 첫 번째 음소
        phone2: 두 번째 음소

    Returns:
        유사도 점수 (0.0 ~ 1.0)
    """
    if phone1 == phone2:
        return 1.0

    # 양방향 체크
    score = PHONEME_SIMILARITY.get((phone1, phone2))
    if score is not None:
        return score

    score = PHONEME_SIMILARITY.get((phone2, phone1))
    if score is not None:
        return score

    # 별표(*) 무시하고 비교
    phone1_clean = phone1.replace("*", "")
    phone2_clean = phone2.replace("*", "")
    if phone1_clean == phone2_clean:
        return 0.95

    # 유사도 없음
    return 0.0


def is_vowel_equivalence(v1: str, v2: str) -> bool:
    """
    두 모음이 발음상 동일한지 확인

    Args:
        v1, v2: 비교할 모음

    Returns:
        발음이 같으면 True
    """
    if v1 == v2:
        return True

    for group in VOWEL_EQUIVALENCE_GROUPS:
        if v1 in group and v2 in group:
            return True

    return False


def calculate_token_similarity(target_tokens: List[str], decoded_tokens: List[str]) -> Dict:
    """
    토큰 리스트 간 유사도 계산

    Args:
        target_tokens: 정답 자모 리스트 ["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"]
        decoded_tokens: 모델 출력 자모 리스트

    Returns:
        {
            "similarity": float (0.0 ~ 1.0),
            "matched_count": int,
            "total_count": int,
            "differences": List[Dict]  # {"position": int, "expected": str, "actual": str, "similarity": float}
        }
    """
    target_len = len(target_tokens)
    decoded_len = len(decoded_tokens)

    # 빈 입력 처리
    if target_len == 0 and decoded_len == 0:
        return {
            "similarity": 1.0,
            "matched_count": 0,
            "total_count": 0,
            "differences": []
        }

    if target_len == 0 or decoded_len == 0:
        return {
            "similarity": 0.0,
            "matched_count": 0,
            "total_count": max(target_len, decoded_len),
            "differences": []
        }

    # 길이 차이가 너무 크면 오답 (아예 다른 단어)
    len_diff_ratio = abs(target_len - decoded_len) / max(target_len, decoded_len)
    if len_diff_ratio > 0.3:  # 30% 이상 차이
        logger.debug(f"길이 차이가 너무 큼: {target_len} vs {decoded_len}")
        return {
            "similarity": 0.0,
            "matched_count": 0,
            "total_count": max(target_len, decoded_len),
            "differences": []
        }

    # 토큰별 유사도 계산
    max_len = max(target_len, decoded_len)
    min_len = min(target_len, decoded_len)

    total_similarity = 0.0
    differences = []

    for i in range(max_len):
        if i >= target_len:
            # 추가 발음
            differences.append({
                "position": i,
                "expected": "",
                "actual": decoded_tokens[i],
                "similarity": 0.0
            })
            continue

        if i >= decoded_len:
            # 누락 발음
            differences.append({
                "position": i,
                "expected": target_tokens[i],
                "actual": "",
                "similarity": 0.0
            })
            continue

        # 유사도 계산
        sim = get_phoneme_similarity(target_tokens[i], decoded_tokens[i])
        total_similarity += sim

        if sim < 1.0:
            differences.append({
                "position": i,
                "expected": target_tokens[i],
                "actual": decoded_tokens[i],
                "similarity": sim
            })

    # 평균 유사도
    average_similarity = total_similarity / max_len

    # 매칭된 개수 (유사도 0.85 이상을 "매칭"으로 간주)
    matched_count = sum(1 for i in range(min_len)
                       if get_phoneme_similarity(target_tokens[i], decoded_tokens[i]) >= 0.85)

    return {
        "similarity": round(average_similarity, 3),
        "matched_count": matched_count,
        "total_count": max_len,
        "differences": differences
    }


def generate_feedback(target_word: str, similarity: float, differences: List[Dict]) -> str:
    """
    유사도 기반 피드백 메시지 생성 (난독증 아동용 긍정적 메시지)

    Args:
        target_word: 목표 단어
        similarity: 유사도 점수 (0.0 ~ 1.0)
        differences: 차이점 리스트

    Returns:
        피드백 메시지
    """
    # 완벽한 발음 (95% 이상)
    if similarity >= 0.95:
        return f"완벽해요! '{target_word}' 발음이 정확해요!"

    # 매우 좋은 발음 (85~95%)
    elif similarity >= 0.85:
        # 차이점 설명 추가
        if differences:
            # 주요 차이점 1~2개만 설명
            main_diffs = [d for d in differences if d["similarity"] > 0][:2]

            if main_diffs:
                # 발음이 같은 모음인 경우 특별 메시지
                if len(main_diffs) == 1:
                    diff = main_diffs[0]
                    expected = diff["expected"]
                    actual = diff["actual"]

                    # 모음 동치 케이스
                    if is_vowel_equivalence(expected, actual):
                        return f"정답이에요! '{target_word}'에서 '{expected}'와 '{actual}'는 같은 발음이에요!"

                    # 일반적인 유사 발음
                    if diff["similarity"] >= 0.8:
                        return f"잘했어요! '{target_word}'를 거의 정확하게 읽었어요. ('{expected}'과 '{actual}'은 비슷한 소리예요)"

                # 여러 개 차이나는 경우
                diff_str = ", ".join([f"'{d['expected']}'과 '{d['actual']}'" for d in main_diffs[:2]])
                return f"잘했어요! '{target_word}'를 거의 정확하게 읽었어요. ({diff_str}은 비슷한 소리예요)"

        return f"잘했어요! '{target_word}'를 거의 정확하게 읽었어요!"

    # 괜찮은 발음 (70~85%)
    elif similarity >= 0.70:
        return f"좋아요! '{target_word}'를 읽으려고 노력했네요. 조금만 더 연습해봐요!"

    # 비슷한 발음 (50~70%)
    elif similarity >= 0.50:
        return f"'{target_word}'와 비슷하게 들렸어요. 다시 한번 천천히 읽어볼까요?"

    # 오답 (<50%)
    else:
        return f"'{target_word}'를 다시 한번 읽어보세요. 천천히 소리내어 읽어봐요!"


def calculate_similarity_with_feedback(
    target_tokens: List[str],
    decoded_tokens: List[str],
    target_word: str
) -> Dict:
    """
    유사도 계산 + 피드백 생성 (통합 함수)

    Args:
        target_tokens: 정답 자모 리스트
        decoded_tokens: 모델 출력 자모 리스트
        target_word: 목표 단어 (피드백용)

    Returns:
        {
            "is_correct": bool,
            "similarity": float,
            "feedback": str
        }
    """
    # 유사도 계산
    result = calculate_token_similarity(target_tokens, decoded_tokens)

    # 정답 판정 (85% 기준)
    is_correct = result["similarity"] >= 0.85

    # 피드백 생성
    feedback = generate_feedback(target_word, result["similarity"], result["differences"])

    logger.info(
        f"유사도 평가 - 목표: {target_word}, "
        f"유사도: {result['similarity']:.1%}, "
        f"판정: {'정답' if is_correct else '오답'}"
    )

    if result["differences"]:
        logger.debug(f"차이점: {result['differences'][:3]}")

    return {
        "is_correct": is_correct,
        "similarity": result["similarity"],
        "feedback": feedback
    }
