package com.readingbuddy.backend.common.util.function;

import lombok.NoArgsConstructor;

import java.util.ArrayList;
import java.util.List;

@NoArgsConstructor
public class PhonemeCounter {

    // 한글 초성, 중성, 종성 테이블
    private static final char[] CHOSUNG = {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ',
            'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    };

    private static final char[] JUNGSUNG = {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
    };

    private static final char[] JONGSUNG = {
            '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ',
            'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    };

    // 종성 인덱스(1~27) 중 복합받침
    private static boolean isComplexJong(int j) {
        return j == 3  || j == 5  || j == 6  || j == 9  || j == 10 ||
                j == 11 || j == 12 || j == 13 || j == 14 || j == 15 || j == 18;
    }

    // 중성 인덱스(0~20): ㅏ,ㅐ,ㅑ,ㅒ,ㅓ,ㅔ,ㅕ,ㅖ,ㅗ,ㅘ,ㅙ,ㅚ,ㅛ,ㅜ,ㅝ,ㅞ,ㅟ,ㅠ,ㅡ,ㅢ,ㅣ
    private static int jungsungCount(int jung) {
        switch (jung) {
            case 9:  // ㅘ = ㅗ+ㅏ
            case 14: // ㅝ = ㅜ+ㅓ
                return 2;
            case 10: // ㅙ = ㅗ+ㅐ(=ㅗ+ㅏ+ㅣ)
            case 15: // ㅞ = ㅜ+ㅔ(=ㅜ+ㅓ+ㅣ)
                return 3;
            // ㅚ/ㅟ/ㅢ는 1로 취급(필요시 2로 조정 가능)
            default:
                return 1;
        }
    }

    /** 한 글자(code point)의 음소 개수 반환 (한글 음절 아닐 때 0) */
    public static int countForCodePoint(int cp) {
        if (cp < 0xAC00 || cp > 0xD7A3) return 0;
        int si   = cp - 0xAC00;
        int jung = (si % 588) / 28;   // 0..20
        int jong = si % 28;           // 0..27

        int cnt = 2;                  // 초성 중성 2
        if (jong != 0) cnt += 1; // 종성
        return cnt;
    }

    /**
     * 한 글자(code point)의 음소 리스트 반환 (한글 음절 아닐 때 빈 리스트)
     * @param cp 한글 음절의 유니코드 포인트
     * @return 음소 리스트 (초성, 중성, 종성이 있으면 종성 포함)
     */
    public static List<Character> getPhonemesForCodePoint(int cp) {
        List<Character> phonemes = new ArrayList<>();

        if (cp < 0xAC00 || cp > 0xD7A3) {
            return phonemes; // 한글이 아니면 빈 리스트 반환
        }

        int si = cp - 0xAC00;
        int cho = si / 588;           // 초성 인덱스 (0..18)
        int jung = (si % 588) / 28;   // 중성 인덱스 (0..20)
        int jong = si % 28;           // 종성 인덱스 (0..27)

        // 초성 추가
        phonemes.add(CHOSUNG[cho]);

        // 중성 추가
        phonemes.add(JUNGSUNG[jung]);

        // 종성이 있으면 추가 (종성 인덱스 0은 종성 없음)
        if (jong > 0) {
            phonemes.add(JONGSUNG[jong]);
        }

        return phonemes;
    }
}
