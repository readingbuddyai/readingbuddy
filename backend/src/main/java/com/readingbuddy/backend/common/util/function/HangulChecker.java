package com.readingbuddy.backend.common.util.function;

public class HangulChecker {

    // 완성형 음절인지? (가~힣)
    public static boolean isHangulSyllable(char ch) {
        return ch >= 0xAC00 && ch <= 0xD7A3;
    }

    // 자음/모음(자모)인지? (ㄱ~힣 사이 말고 자모 블록)
    public static boolean isHangulJamo(char ch) {
        // 호환 자모
        if (ch >= 0x3131 && ch <= 0x318E) return true;
        // 자모 (초성/중성/종성)
        if (ch >= 0x1100 && ch <= 0x11FF) return true;
        return false;
    }

    /**
     * 문자열이 전부 '완성형 음절'로만 이루어졌는지
     */
    public static boolean isAllSyllables(String s) {
        if (s == null || s.isEmpty()) return false;
        for (char ch : s.toCharArray()) {
            if (!isHangulSyllable(ch)) {
                return false;
            }
        }
        return true;
    }

    /**
     * 문자열이 전부 '자음/모음(자모)'로만 이루어졌는지
     */
    public static boolean isAllJamo(String s) {
        if (s == null || s.isEmpty()) return false;
        for (char ch : s.toCharArray()) {
            if (!isHangulJamo(ch)) {
                return false;
            }
        }
        return true;
    }

    /**
     * 문자열이 '완성형'과 '자모'가 섞여있는지 체크하고 싶으면 이렇게
     */
    public static String classify(String s) {
        if (isAllSyllables(s)) return "SYLLABLE_ONLY";   // 음절만
        if (isAllJamo(s)) return "JAMO_ONLY";            // 자음/모음만
        return "MIXED_OR_OTHER";                         // 섞였거나 한글 아님
    }
}
