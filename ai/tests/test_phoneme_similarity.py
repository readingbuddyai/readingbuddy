"""
음소 유사도 계산 테스트
"""
import pytest
from app.services.phoneme_similarity import (
    get_phoneme_similarity,
    calculate_token_similarity,
    calculate_similarity_with_feedback,
    is_vowel_equivalence
)


class TestPhonemeSimilarity:
    """음소 유사도 함수 테스트"""

    def test_exact_match(self):
        """완전 일치 테스트"""
        assert get_phoneme_similarity("ㄱ", "ㄱ") == 1.0
        assert get_phoneme_similarity("ㅏ", "ㅏ") == 1.0

    def test_vowel_equivalence(self):
        """모음 동치 테스트"""
        # 애/에
        assert get_phoneme_similarity("ㅐ", "ㅔ") == 1.0
        assert is_vowel_equivalence("ㅐ", "ㅔ") is True

        # 얘/예
        assert get_phoneme_similarity("ㅒ", "ㅖ") == 1.0
        assert is_vowel_equivalence("ㅒ", "ㅖ") is True

        # 왜/웨
        assert get_phoneme_similarity("ㅙ", "ㅞ") == 1.0
        assert is_vowel_equivalence("ㅙ", "ㅞ") is True

    def test_consonant_similarity(self):
        """자음 유사도 테스트 (아동 연구 데이터 기반)"""
        # ㄷ/ㅈ - 아동이 가장 많이 혼동
        assert get_phoneme_similarity("ㄷ", "ㅈ") == 0.85

        # ㄴ/ㅇ - 높은 대체 오류율
        assert get_phoneme_similarity("ㄴ", "ㅇ") == 0.80

        # 평음/격음
        assert get_phoneme_similarity("ㄱ", "ㅋ") == 0.80
        assert get_phoneme_similarity("ㄷ", "ㅌ") == 0.80
        assert get_phoneme_similarity("ㅂ", "ㅍ") == 0.80

    def test_no_similarity(self):
        """유사도 없음 테스트"""
        assert get_phoneme_similarity("ㄱ", "ㅁ") == 0.0
        assert get_phoneme_similarity("ㅏ", "ㅜ") == 0.0


class TestTokenSimilarity:
    """토큰 시퀀스 유사도 테스트"""

    def test_perfect_match(self):
        """완벽한 일치"""
        target = ["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"]
        decoded = ["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"]

        result = calculate_token_similarity(target, decoded)
        assert result["similarity"] == 1.0
        assert result["matched_count"] == 6
        assert len(result["differences"]) == 0

    def test_similar_pronunciation(self):
        """유사 발음 (강아지 → 강아치)"""
        target = ["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"]
        decoded = ["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅊ", "ㅣ"]

        result = calculate_token_similarity(target, decoded)
        # ㅈ/ㅊ 유사도 0.80
        expected = (1.0 + 1.0 + 1.0 + 1.0 + 0.80 + 1.0) / 6
        assert result["similarity"] == pytest.approx(expected, rel=0.01)
        assert result["matched_count"] == 5  # ㅊ은 0.80이므로 매칭으로 간주 안됨 (0.85 미만)

    def test_one_different(self):
        """한 자모만 다름 (강아지 → 강마지)"""
        target = ["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"]
        decoded = ["ㄱ", "ㅏ", "ㅁ", "ㅏ", "ㅈ", "ㅣ"]

        result = calculate_token_similarity(target, decoded)
        # ㅇ/ㅁ 유사도 0.0
        expected = (1.0 + 1.0 + 0.0 + 1.0 + 1.0 + 1.0) / 6
        assert result["similarity"] == pytest.approx(expected, rel=0.01)

    def test_vowel_equivalence_in_word(self):
        """단어 내 모음 동치 (왜 → 웨)"""
        target = ["ㅙ"]
        decoded = ["ㅞ"]

        result = calculate_token_similarity(target, decoded)
        assert result["similarity"] == 1.0

    def test_length_difference_too_large(self):
        """길이 차이 너무 큼 (30% 이상)"""
        target = ["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"]  # 6개
        decoded = ["ㄱ", "ㅏ"]  # 2개 (66% 차이)

        result = calculate_token_similarity(target, decoded)
        assert result["similarity"] == 0.0

    def test_empty_tokens(self):
        """빈 토큰"""
        result = calculate_token_similarity([], [])
        assert result["similarity"] == 1.0

        result = calculate_token_similarity(["ㄱ"], [])
        assert result["similarity"] == 0.0


class TestFeedbackGeneration:
    """피드백 생성 테스트"""

    def test_perfect_score_feedback(self):
        """완벽한 점수 (95% 이상)"""
        result = calculate_similarity_with_feedback(
            target_tokens=["ㄱ", "ㅏ"],
            decoded_tokens=["ㄱ", "ㅏ"],
            target_word="가"
        )

        assert result["is_correct"] is True
        assert result["similarity"] == 1.0
        assert "완벽해요" in result["feedback"]

    def test_high_score_feedback(self):
        """높은 점수 (85~95%)"""
        result = calculate_similarity_with_feedback(
            target_tokens=["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"],
            decoded_tokens=["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅊ", "ㅣ"],
            target_word="강아지"
        )

        assert result["is_correct"] is True  # 85% 이상
        assert result["similarity"] > 0.85
        assert "잘했어요" in result["feedback"] or "정답" in result["feedback"]

    def test_medium_score_feedback(self):
        """중간 점수 (70~85%)"""
        result = calculate_similarity_with_feedback(
            target_tokens=["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"],
            decoded_tokens=["ㄱ", "ㅏ", "ㅁ", "ㅏ", "ㅈ", "ㅣ"],
            target_word="강아지"
        )

        assert result["is_correct"] is False  # 85% 미만
        assert 0.70 <= result["similarity"] < 0.85
        assert "좋아요" in result["feedback"] or "노력" in result["feedback"]

    def test_low_score_feedback(self):
        """낮은 점수 (<70%)"""
        result = calculate_similarity_with_feedback(
            target_tokens=["ㄱ", "ㅏ", "ㅇ", "ㅏ", "ㅈ", "ㅣ"],
            decoded_tokens=["ㄱ", "ㅗ", "ㅑ", "ㅇ", "ㅣ", "ㄱ*"],
            target_word="강아지"
        )

        assert result["is_correct"] is False
        assert result["similarity"] < 0.85
        assert "다시" in result["feedback"] or "천천히" in result["feedback"]

    def test_vowel_equivalence_feedback(self):
        """모음 동치 특별 피드백"""
        result = calculate_similarity_with_feedback(
            target_tokens=["ㅙ"],
            decoded_tokens=["ㅞ"],
            target_word="왜"
        )

        assert result["is_correct"] is True
        assert result["similarity"] == 1.0
        # 모음 동치 특별 메시지 확인
        assert "같은 발음" in result["feedback"] or "정답" in result["feedback"]


class TestRealWorldScenarios:
    """실제 시나리오 테스트"""

    def test_scenario_1_perfect_pronunciation(self):
        """시나리오 1: 완벽한 발음"""
        # 아이가 "감자"를 완벽하게 발음
        result = calculate_similarity_with_feedback(
            target_tokens=["ㄱ", "ㅏ", "ㅁ", "ㅈ", "ㅏ"],
            decoded_tokens=["ㄱ", "ㅏ", "ㅁ", "ㅈ", "ㅏ"],
            target_word="감자"
        )

        assert result["is_correct"] is True
        assert result["similarity"] == 1.0

    def test_scenario_2_similar_consonant(self):
        """시나리오 2: 유사 자음 혼동 (격음/평음)"""
        # 아이가 "가"를 "카"로 발음
        result = calculate_similarity_with_feedback(
            target_tokens=["ㄱ", "ㅏ"],
            decoded_tokens=["ㅋ", "ㅏ"],
            target_word="가"
        )

        # ㄱ/ㅋ 유사도 0.80
        assert result["similarity"] == pytest.approx(0.90, rel=0.1)  # (0.80 + 1.0) / 2

    def test_scenario_3_vowel_confusion(self):
        """시나리오 3: 모음 동치 (왜 → 웨)"""
        # 아이가 "왜"를 "웨"로 발음 (실제로는 같은 발음)
        result = calculate_similarity_with_feedback(
            target_tokens=["ㅙ"],
            decoded_tokens=["ㅞ"],
            target_word="왜"
        )

        assert result["is_correct"] is True
        assert result["similarity"] == 1.0
        assert "같은 발음" in result["feedback"] or "정답" in result["feedback"]

    def test_scenario_4_partial_correct(self):
        """시나리오 4: 부분적으로 맞음"""
        # 아이가 "고양이"를 "고야이"로 발음 (ㅇ 누락)
        result = calculate_similarity_with_feedback(
            target_tokens=["ㄱ", "ㅗ", "ㅑ", "ㅇ", "ㅣ"],
            decoded_tokens=["ㄱ", "ㅗ", "ㅑ", "ㅣ"],
            target_word="고양이"
        )

        # 길이 차이 20% (5개 vs 4개)
        assert result["similarity"] < 0.85  # 오답
