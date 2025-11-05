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
    LABIAL_1("양순음_1", "ㅂ, ㅃ, ㅍ, ㅁ"),
    VELAR_1("연구개음_1", "ㄱ, ㄲ, ㅋ, ㅇ"),
    ALVEOLAR_1("치조음_1", "ㄷ, ㄸ, ㅌ, ㄴ"),
    PALATAL_1("경구개음_1", "ㅈ, ㅉ, ㅊ"),
    ALVEOLAR_FRICATIVE_1("치조음/치경음_1", "ㅅ, ㅆ"),
    GLOTTAL_AND_ALVEOLAR_1("혀뿌리음/성문음 + 치조음_1", "ㅎ, ㄹ"),

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
    OPEN_SYLLABLE("받침 없는 음절", "종성이 없는 음절"),


    // 1-6: 자음 조음 위치 분류
    LABIAL_2("양순음_2", "ㅂ, ㅃ, ㅍ, ㅁ"),
    VELAR_2("연구개음_2", "ㄱ, ㄲ, ㅋ, ㅇ"),
    ALVEOLAR_2("치조음_2", "ㄷ, ㄸ, ㅌ, ㄴ"),
    PALATAL_2("경구개음_2", "ㅈ, ㅉ, ㅊ"),
    ALVEOLAR_FRICATIVE_2("치조음/치경음_2", "ㅅ, ㅆ"),
    GLOTTAL_AND_ALVEOLAR_2("혀뿌리음/성문음 + 치조음_2", "ㅎ, ㄹ");

    private final String description;
    private final String examples;

}