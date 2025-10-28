package com.readingbuddy.backend.common.util.function;

import lombok.NoArgsConstructor;

@NoArgsConstructor
public class PhonemeCounter {

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

        int cnt = 1;                  // 초성 1
        cnt += jungsungCount(jung);   // 중성
        if (jong != 0) cnt += isComplexJong(jong) ? 2 : 1; // 종성
        return cnt;
    }
}
