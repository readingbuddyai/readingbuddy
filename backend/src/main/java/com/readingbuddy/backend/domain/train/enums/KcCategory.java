package com.readingbuddy.backend.domain.train.enums;

import lombok.AllArgsConstructor;
import lombok.Getter;

/**
 * Knowledge Component Category
 * 지식단위 카테고리 - 한국어 음운론 기반 자음/모음/음절 분류
 */
@Getter
@AllArgsConstructor
public enum KcCategory {
    // 1-6: 자음 조음 위치 분류
    LABIAL("양순음", "ㅂ, ㅃ, ㅍ, ㅁ"),
    VELAR("연구개음", "ㄱ, ㄲ, ㅋ, ㅇ"),
    ALVEOLAR("치조음", "ㄷ, ㄸ, ㅌ, ㄴ"),
    PALATAL("경구개음", "ㅈ, ㅉ, ㅊ"),
    ALVEOLAR_FRICATIVE("치조음/치경음", "ㅅ, ㅆ"),
    GLOTTAL_AND_ALVEOLAR("혀뿌리음/성문음 + 치조음", "ㅎ, ㄹ"),

    // 7-12: 초성 음절 자음 (Onset)
    LABIAL_ONSET("양순음 초성", "ㅂ, ㅃ, ㅍ, ㅁ 초성"),
    VELAR_ONSET("연구개음 초성", "ㄱ, ㄲ, ㅋ, ㅇ 초성"),
    ALVEOLAR_ONSET("치조음 초성", "ㄷ, ㄸ, ㅌ, ㄴ 초성"),
    PALATAL_ONSET("경구개음 초성", "ㅈ, ㅉ, ㅊ 초성"),
    ALVEOLAR_FRICATIVE_ONSET("치조음/치경음 초성", "ㅅ, ㅆ 초성"),
    GLOTTAL_AND_ALVEOLAR_ONSET("혀뿌리음/성문음 + 치조음 초성", "ㅎ, ㄹ 초성"),

    // 13-18: 종성 음절 자음 (Coda)
    LABIAL_CODA("양순음 종성", "ㅂ, ㅃ, ㅍ, ㅁ 종성"),
    VELAR_CODA("연구개음 종성", "ㄱ, ㄲ, ㅋ, ㅇ 종성"),
    ALVEOLAR_CODA("치조음 종성", "ㄷ, ㄸ, ㅌ, ㄴ 종성"),
    PALATAL_CODA("경구개음 종성", "ㅈ, ㅉ, ㅊ 종성"),
    ALVEOLAR_FRICATIVE_CODA("치조음/치경음 종성", "ㅅ, ㅆ 종성"),
    GLOTTAL_AND_ALVEOLAR_CODA("혀뿌리음/성문음 + 치조음 종성", "ㅎ, ㄹ 종성"),

    // 19-20: 모음 분류
    MONOPHTHONG("단모음", "ㅏ, ㅓ, ㅗ, ㅜ, ㅡ, ㅣ, ㅐ, ㅔ, ㅚ, ㅟ"),
    DIPHTHONG("이중모음", "ㅑ, ㅕ, ㅛ, ㅠ, ㅒ, ㅖ, ㅘ, ㅙ, ㅝ, ㅞ, ㅢ"),

    // 21-22: 중성 음절 모음 (Nucleus)
    MONOPHTHONG_NUCLEUS("단모음 중성", "단모음 중성 음절"),
    DIPHTHONG_NUCLEUS("이중모음 중성", "이중모음 중성 음절"),

    // 23-24: 음절 구조
    CLOSED_SYLLABLE("받침 있는 음절", "종성이 있는 음절"),
    OPEN_SYLLABLE("받침 없는 음절", "종성이 없는 음절");

    private final String description;
    private final String examples;

}