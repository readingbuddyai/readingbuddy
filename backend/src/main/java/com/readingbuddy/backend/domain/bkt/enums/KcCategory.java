package com.readingbuddy.backend.domain.bkt.enums;

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
    LABIAL_1("양순음", "ㅂ, ㅃ, ㅍ, ㅁ"),
    VELAR_1("연구개음", "ㄱ, ㄲ, ㅋ, ㅇ"),
    ALVEOLAR_1("치조음", "ㄷ, ㄸ, ㅌ, ㄴ"),
    PALATAL_1("경구개음", "ㅈ, ㅉ, ㅊ"),
    ALVEOLAR_FRICATIVE_1("치조음/치경음", "ㅅ, ㅆ"),
    GLOTTAL_AND_ALVEOLAR_1("혀뿌리음/성문음 + 치조음", "ㅎ, ㄹ"),

    // 7-12: 초성 음절 자음 (Onset)
    LABIAL_ONSET_1("양순음 초성", "ㅂ, ㅃ, ㅍ, ㅁ 초성"),
    VELAR_ONSET_1("연구개음 초성", "ㄱ, ㄲ, ㅋ, ㅇ 초성"),
    ALVEOLAR_ONSET_1("치조음 초성", "ㄷ, ㄸ, ㅌ, ㄴ 초성"),
    PALATAL_ONSET_1("경구개음 초성", "ㅈ, ㅉ, ㅊ 초성"),
    ALVEOLAR_FRICATIVE_ONSET_1("치조음/치경음 초성", "ㅅ, ㅆ 초성"),
    GLOTTAL_AND_ALVEOLAR_ONSET_1("혀뿌리음/성문음 + 치조음 초성", "ㅎ, ㄹ 초성"),

    // 13-18: 종성 음절 자음 (Coda)
    LABIAL_CODA_1("양순음 종성", "ㅂ, ㅃ, ㅍ, ㅁ 종성"),
    VELAR_CODA_1("연구개음 종성", "ㄱ, ㄲ, ㅋ, ㅇ 종성"),
    ALVEOLAR_CODA_1("치조음 종성", "ㄷ, ㄸ, ㅌ, ㄴ 종성"),
    PALATAL_CODA_1("경구개음 종성", "ㅈ, ㅉ, ㅊ 종성"),
    ALVEOLAR_FRICATIVE_CODA_1("치조음/치경음 종성", "ㅅ, ㅆ 종성"),
    GLOTTAL_AND_ALVEOLAR_CODA_1("혀뿌리음/성문음 + 치조음 종성", "ㅎ, ㄹ 종성"),

    // 19-20: 모음 분류
    MONOPHTHONG_1("단모음", "ㅏ, ㅓ, ㅗ, ㅜ, ㅡ, ㅣ, ㅐ, ㅔ, ㅚ, ㅟ"),
    DIPHTHONG_1("이중모음", "ㅑ, ㅕ, ㅛ, ㅠ, ㅒ, ㅖ, ㅘ, ㅙ, ㅝ, ㅞ, ㅢ"),

    MONOPHTHONG_2("단모음", "ㅏ, ㅓ, ㅗ, ㅜ, ㅡ, ㅣ, ㅐ, ㅔ, ㅚ, ㅟ"),
    DIPHTHONG_2("이중모음", "ㅑ, ㅕ, ㅛ, ㅠ, ㅒ, ㅖ, ㅘ, ㅙ, ㅝ, ㅞ, ㅢ"),

    // 21-22: 중성 음절 모음 (Nucleus)
    MONOPHTHONG_NUCLEUS_1("단모음 중성", "단모음 중성 음절"),
    DIPHTHONG_NUCLEUS_1("이중모음 중성", "이중모음 중성 음절"),

    // 23-24: 음절 구조
    CLOSED_SYLLABLE("받침 있는 음절", "종성이 있는 음절"),
    OPEN_SYLLABLE("받침 없는 음절", "종성이 없는 음절"),


    // 1-6: 자음 조음 위치 분류
    LABIAL_2("양순음", "ㅂ, ㅃ, ㅍ, ㅁ"),
    VELAR_2("연구개음", "ㄱ, ㄲ, ㅋ, ㅇ"),
    ALVEOLAR_2("치조음", "ㄷ, ㄸ, ㅌ, ㄴ"),
    PALATAL_2("경구개음", "ㅈ, ㅉ, ㅊ"),
    ALVEOLAR_FRICATIVE_2("치조음/치경음", "ㅅ, ㅆ"),
    GLOTTAL_AND_ALVEOLAR_2("혀뿌리음/성문음 + 치조음", "ㅎ, ㄹ"),

    LABIAL_ONSET_2("양순음 초성", "ㅂ, ㅃ, ㅍ, ㅁ 초성"),
    VELAR_ONSET_2("연구개음 초성", "ㄱ, ㄲ, ㅋ, ㅇ 초성"),
    ALVEOLAR_ONSET_2("치조음 초성", "ㄷ, ㄸ, ㅌ, ㄴ 초성"),
    PALATAL_ONSET_2("경구개음 초성", "ㅈ, ㅉ, ㅊ 초성"),
    ALVEOLAR_FRICATIVE_ONSET_2("치조음/치경음 초성", "ㅅ, ㅆ 초성"),
    GLOTTAL_AND_ALVEOLAR_ONSET_2("혀뿌리음/성문음 + 치조음 초성", "ㅎ, ㄹ 초성"),

    // 13-18: 종성 음절 자음 (Coda)
    LABIAL_CODA_2("양순음 종성", "ㅂ, ㅃ, ㅍ, ㅁ 종성"),
    VELAR_CODA_2("연구개음 종성", "ㄱ, ㄲ, ㅋ, ㅇ 종성"),
    ALVEOLAR_CODA_2("치조음 종성", "ㄷ, ㄸ, ㅌ, ㄴ 종성"),
    PALATAL_CODA_2("경구개음 종성", "ㅈ, ㅉ, ㅊ 종성"),
    ALVEOLAR_FRICATIVE_CODA_2("치조음/치경음 종성", "ㅅ, ㅆ 종성"),
    GLOTTAL_AND_ALVEOLAR_CODA_2("혀뿌리음/성문음 + 치조음 종성", "ㅎ, ㄹ 종성"),

    MONOPHTHONG_NUCLEUS_2("단모음 중성", "단모음 중성 음절"),
    DIPHTHONG_NUCLEUS_2("이중모음 중성", "이중모음 중성 음절");

    private final String description;
    private final String examples;

}