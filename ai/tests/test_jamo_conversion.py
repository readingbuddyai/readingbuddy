"""자모 변환 테스트"""
import pytest
from app.services.inference import compose_hangul, decompose_to_jamo, jamo_to_hangul


class TestHangulComposition:
    """한글 조합 테스트"""

    def test_compose_simple(self):
        """단순 한글 조합"""
        # 가 = ㄱ + ㅏ
        result = compose_hangul("ㄱ", "ㅏ", None)
        assert result == "가"

    def test_compose_with_jongseong(self):
        """종성이 있는 한글 조합"""
        # 감 = ㄱ + ㅏ + ㅁ
        result = compose_hangul("ㄱ", "ㅏ", "ㅁ")
        assert result == "감"

    def test_compose_with_coda_map(self):
        """종성 매핑 테스트"""
        # 각 = ㄱ + ㅏ + ㄱ*
        result = compose_hangul("ㄱ", "ㅏ", "ㄱ*")
        assert result == "각"

    def test_compose_none_choseong(self):
        """초성 None (ㅇ) 테스트"""
        # 아 = None + ㅏ = ㅇ + ㅏ
        result = compose_hangul(None, "ㅏ", None)
        assert result == "아"


class TestHangulDecomposition:
    """한글 분해 테스트"""

    def test_decompose_simple(self):
        """단순 한글 분해"""
        # 가 → [ㄱ, ㅏ]
        result = decompose_to_jamo("가")
        assert result == ["ㄱ", "ㅏ"]

    def test_decompose_with_jongseong(self):
        """종성이 있는 한글 분해"""
        # 감 → [ㄱ, ㅏ, ㅁ]
        result = decompose_to_jamo("감")
        assert result == ["ㄱ", "ㅏ", "ㅁ"]

    def test_decompose_complex(self):
        """복잡한 자모 분해"""
        # 강 → [ㄱ, ㅏ, ㅇ]
        result = decompose_to_jamo("강")
        assert result == ["ㄱ", "ㅏ", "ㅇ"]


class TestJamoToHangul:
    """자모 → 한글 변환 테스트"""

    def test_simple_word(self):
        """단순 단어 변환"""
        # "ㄱ ㅏ" → "가"
        result = jamo_to_hangul("ㄱ ㅏ")
        assert result == "가"

    def test_word_with_jongseong(self):
        """종성이 있는 단어"""
        # "ㄱ ㅏ ㅁ" → "감"
        result = jamo_to_hangul("ㄱ ㅏ ㅁ")
        assert result == "감"

    def test_multiple_syllables(self):
        """여러 음절"""
        # "ㄱ ㅏ ㅁ ㅈ ㅏ" → "감자"
        result = jamo_to_hangul("ㄱ ㅏ ㅁ ㅈ ㅏ")
        assert result == "감자"

    def test_vowel_only(self):
        """모음만 있는 경우"""
        # "ㅏ" → "아" (초성 ㅇ 자동 추가)
        result = jamo_to_hangul("ㅏ")
        assert result == "아"

    def test_with_space(self):
        """띄어쓰기 포함"""
        # "ㄱ ㅏ | ㅈ ㅏ" → "가 자"
        result = jamo_to_hangul("ㄱ ㅏ | ㅈ ㅏ")
        assert result == "가 자"

    def test_coda_mapping(self):
        """종성 매핑 (*)"""
        # "ㄱ ㅏ ㄱ*" → "각"
        result = jamo_to_hangul("ㄱ ㅏ ㄱ*")
        assert result == "각"
