package com.readingbuddy.backend.config;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import javax.sql.DataSource;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.util.LinkedHashMap;
import java.util.Map;

@Slf4j
@Configuration
@RequiredArgsConstructor
public class PhonemesSeederConfig {

    private final DataSource dataSource;

    // S3 voice URL 포맷
    private static final String VOICE_URL_FORMAT = "https://final-a206.s3.ap-northeast-2.amazonaws.com/voices/letters/%s_normal.mp3";

    // 자음 목록 (category: "consonant")
    // value: 자음 단독 문자, unicode: 자음 단독 유니코드, voiceUnicode: 'ㅡ'와 결합된 글자의 유니코드 (음성용)
    private static final Map<String, ConsonantInfo> CONSONANTS = new LinkedHashMap<>() {{
        put("ㄱ", new ConsonantInfo("U+3131", "U+ADF8")); // 그
        put("ㄴ", new ConsonantInfo("U+3134", "U+B290")); // 느
        put("ㄷ", new ConsonantInfo("U+3137", "U+B4DC")); // 드
        put("ㄹ", new ConsonantInfo("U+3139", "U+B974")); // 르
        put("ㅁ", new ConsonantInfo("U+3141", "U+BBC0")); // 므
        put("ㅂ", new ConsonantInfo("U+3142", "U+BE0C")); // 브
        put("ㅅ", new ConsonantInfo("U+3145", "U+C2A4")); // 스
        put("ㅇ", new ConsonantInfo("U+3147", "U+C73C")); // 으
        put("ㅈ", new ConsonantInfo("U+3148", "U+C988")); // 즈
        put("ㅊ", new ConsonantInfo("U+314A", "U+CE20")); // 츠
        put("ㅋ", new ConsonantInfo("U+314B", "U+D06C")); // 크
        put("ㅌ", new ConsonantInfo("U+314C", "U+D2B8")); // 트
        put("ㅍ", new ConsonantInfo("U+314D", "U+D504")); // 프
        put("ㅎ", new ConsonantInfo("U+314E", "U+D750")); // 흐
        // 된소리
        put("ㄲ", new ConsonantInfo("U+3132", "U+B044")); // 끄
        put("ㄸ", new ConsonantInfo("U+3138", "U+B728")); // 뜨
        put("ㅃ", new ConsonantInfo("U+3143", "U+C058")); // 쁘
        put("ㅆ", new ConsonantInfo("U+3146", "U+C4F0")); // 쓰
        put("ㅉ", new ConsonantInfo("U+3149", "U+CBD4")); // 쯔
    }};

    // 자음 정보를 담는 내부 클래스
    private static class ConsonantInfo {
        final String unicode;        // 자음 단독 유니코드
        final String voiceUnicode;   // 음성용 유니코드 (자음+'ㅡ')

        ConsonantInfo(String unicode, String voiceUnicode) {
            this.unicode = unicode;
            this.voiceUnicode = voiceUnicode;
        }
    }

    // 모음 목록 (category: "vowel")
    // value: 모음 단독 문자, unicode: 모음 단독 유니코드, voiceUnicode: 'ㅇ'과 결합된 글자의 유니코드 (음성용)
    private static final Map<String, VowelInfo> VOWELS = new LinkedHashMap<>() {{
        put("ㅏ", new VowelInfo("U+314F", "U+C544")); // 아
        put("ㅑ", new VowelInfo("U+3151", "U+C57C")); // 야
        put("ㅓ", new VowelInfo("U+3153", "U+C5B4")); // 어
        put("ㅕ", new VowelInfo("U+3155", "U+C5EC")); // 여
        put("ㅗ", new VowelInfo("U+3157", "U+C624")); // 오
        put("ㅛ", new VowelInfo("U+315B", "U+C694")); // 요
        put("ㅜ", new VowelInfo("U+315C", "U+C6B0")); // 우
        put("ㅠ", new VowelInfo("U+3160", "U+C720")); // 유
        put("ㅡ", new VowelInfo("U+3161", "U+C73C")); // 으
        put("ㅣ", new VowelInfo("U+3163", "U+C774")); // 이
        // 복합 모음
        put("ㅐ", new VowelInfo("U+3150", "U+C560")); // 애
        put("ㅒ", new VowelInfo("U+3152", "U+C598")); // 얘
        put("ㅔ", new VowelInfo("U+3154", "U+C5D0")); // 에
        put("ㅖ", new VowelInfo("U+3156", "U+C608")); // 예
        put("ㅘ", new VowelInfo("U+3158", "U+C640")); // 와
        put("ㅙ", new VowelInfo("U+3159", "U+C65C")); // 왜
        put("ㅚ", new VowelInfo("U+315A", "U+C678")); // 외
        put("ㅝ", new VowelInfo("U+315D", "U+C6CC")); // 워
        put("ㅞ", new VowelInfo("U+315E", "U+C6E8")); // 웨
        put("ㅟ", new VowelInfo("U+315F", "U+C704")); // 위
        put("ㅢ", new VowelInfo("U+3162", "U+C758")); // 의
    }};

    // 모음 정보를 담는 내부 클래스
    private static class VowelInfo {
        final String unicode;        // 모음 단독 유니코드
        final String voiceUnicode;   // 음성용 유니코드 (ㅇ+모음)

        VowelInfo(String unicode, String voiceUnicode) {
            this.unicode = unicode;
            this.voiceUnicode = voiceUnicode;
        }
    }

    @Bean
    ApplicationRunner seedPhonemesRunner() {
        return args -> {
            log.info("Updating Phonemes voice URLs...");

            try (Connection con = dataSource.getConnection()) {
                con.setAutoCommit(false);

                // 이미 존재하는 데이터의 voice_url만 업데이트
                final String sql = """
                    UPDATE phonemes
                    SET voice_url = ?
                    WHERE category = ? AND value = ?
                    """;

                try (PreparedStatement ps = con.prepareStatement(sql)) {
                    int totalUpdated = 0;

                    // 자음 업데이트 (음성 URL은 'ㅡ'와 결합된 형태 사용)
                    for (Map.Entry<String, ConsonantInfo> entry : CONSONANTS.entrySet()) {
                        String value = entry.getKey();
                        ConsonantInfo info = entry.getValue();
                        String voiceUrl = String.format(VOICE_URL_FORMAT, info.voiceUnicode);

                        ps.setString(1, voiceUrl);      // SET voice_url = ?
                        ps.setString(2, "consonant");   // WHERE category = ?
                        ps.setString(3, value);         // AND value = ?
                        ps.addBatch();
                        totalUpdated++;
                    }

                    // 모음 업데이트 (음성 URL은 'ㅇ'과 결합된 형태 사용)
                    for (Map.Entry<String, VowelInfo> entry : VOWELS.entrySet()) {
                        String value = entry.getKey();
                        VowelInfo info = entry.getValue();
                        String voiceUrl = String.format(VOICE_URL_FORMAT, info.voiceUnicode);

                        ps.setString(1, voiceUrl);      // SET voice_url = ?
                        ps.setString(2, "vowel");       // WHERE category = ?
                        ps.setString(3, value);         // AND value = ?
                        ps.addBatch();
                        totalUpdated++;
                    }

                    ps.executeBatch();
                    con.commit();

                    log.info("Successfully updated {} phonemes voice URLs (Consonants: {}, Vowels: {})",
                            totalUpdated, CONSONANTS.size(), VOWELS.size());
                } catch (Exception e) {
                    con.rollback();
                    log.error("Failed to update phonemes voice URLs: {}", e.getMessage(), e);
                    throw e;
                }
            }
        };
    }
}
