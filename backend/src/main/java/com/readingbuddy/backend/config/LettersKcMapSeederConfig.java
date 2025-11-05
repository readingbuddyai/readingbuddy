package com.readingbuddy.backend.config;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.annotation.Order;

import javax.sql.DataSource;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.*;

@Configuration
@RequiredArgsConstructor
@Slf4j
public class LettersKcMapSeederConfig {

    private final DataSource dataSource;

    // 초성 인덱스 -> ONSET KC ID 매핑
    private static final Map<Integer, Long> CHO_TO_ONSET_KC = new HashMap<>() {{
        put(0, 8L);   // ㄱ -> VELAR_ONSET
        put(1, 8L);   // ㄲ -> VELAR_ONSET
        put(2, 9L);   // ㄴ -> ALVEOLAR_ONSET
        put(3, 9L);   // ㄷ -> ALVEOLAR_ONSET
        put(4, 9L);   // ㄸ -> ALVEOLAR_ONSET
        put(5, 12L);  // ㄹ -> GLOTTAL_AND_ALVEOLAR_ONSET
        put(6, 7L);   // ㅁ -> LABIAL_ONSET
        put(7, 7L);   // ㅂ -> LABIAL_ONSET
        put(8, 7L);   // ㅃ -> LABIAL_ONSET
        put(9, 11L);  // ㅅ -> ALVEOLAR_FRICATIVE_ONSET
        put(10, 11L); // ㅆ -> ALVEOLAR_FRICATIVE_ONSET
        put(11, 8L);  // ㅇ -> VELAR_ONSET
        put(12, 10L); // ㅈ -> PALATAL_ONSET
        put(13, 10L); // ㅉ -> PALATAL_ONSET
        put(14, 10L); // ㅊ -> PALATAL_ONSET
        put(15, 8L);  // ㅋ -> VELAR_ONSET
        put(16, 9L);  // ㅌ -> ALVEOLAR_ONSET
        put(17, 7L);  // ㅍ -> LABIAL_ONSET
        put(18, 12L); // ㅎ -> GLOTTAL_AND_ALVEOLAR_ONSET
    }};

    // 종성 인덱스 -> CODA KC ID 매핑
    private static final Map<Integer, Long> JONG_TO_CODA_KC = new HashMap<>() {{
        put(1, 14L);  // ㄱ -> VELAR_CODA
        put(2, 14L);  // ㄲ -> VELAR_CODA
        put(4, 15L);  // ㄴ -> ALVEOLAR_CODA
        put(7, 15L);  // ㄷ -> ALVEOLAR_CODA
        put(8, 18L);  // ㄹ -> GLOTTAL_AND_ALVEOLAR_CODA
        put(16, 13L); // ㅁ -> LABIAL_CODA
        put(17, 13L); // ㅂ -> LABIAL_CODA
        put(19, 17L); // ㅅ -> ALVEOLAR_FRICATIVE_CODA
        put(20, 17L); // ㅆ -> ALVEOLAR_FRICATIVE_CODA
        put(21, 14L); // ㅇ -> VELAR_CODA
        put(22, 16L); // ㅈ -> PALATAL_CODA
        put(23, 16L); // ㅊ -> PALATAL_CODA
        put(24, 14L); // ㅋ -> VELAR_CODA
        put(25, 15L); // ㅌ -> ALVEOLAR_CODA
        put(26, 13L); // ㅍ -> LABIAL_CODA
        put(27, 18L); // ㅎ -> GLOTTAL_AND_ALVEOLAR_CODA
    }};

    // 중성 인덱스 -> NUCLEUS KC ID 매핑
    private static final Map<Integer, Long> JUNG_TO_NUCLEUS_KC = new HashMap<>() {{
        // 단모음 -> MONOPHTHONG_NUCLEUS (21)
        put(0, 21L);  // ㅏ
        put(1, 21L);  // ㅐ
        put(4, 21L);  // ㅓ
        put(5, 21L);  // ㅔ
        put(8, 21L);  // ㅗ
        put(11, 21L); // ㅚ
        put(12, 21L); // ㅛ
        put(13, 21L); // ㅜ
        put(16, 21L); // ㅟ
        put(17, 21L); // ㅠ
        put(18, 21L); // ㅡ
        put(20, 21L); // ㅣ

        // 이중모음 -> DIPHTHONG_NUCLEUS (22)
        put(2, 22L);  // ㅑ
        put(3, 22L);  // ㅒ
        put(6, 22L);  // ㅕ
        put(7, 22L);  // ㅖ
        put(9, 22L);  // ㅘ
        put(10, 22L); // ㅙ
        put(14, 22L); // ㅝ
        put(15, 22L); // ㅞ
        put(19, 22L); // ㅢ
    }};

    @Bean
    @Order(100)
    ApplicationRunner seedLettersKcMapRunner() {
        return args -> {
            log.info("LettersKcMapSeeder 시작...");

            try (Connection con = dataSource.getConnection()) {
                con.setAutoCommit(false);

                // Letters 데이터 조회
                List<LetterInfo> letters = loadLetters(con);
                log.info("조회된 Letters 개수: {}", letters.size());

                if (letters.isEmpty()) {
                    log.warn("Letters 테이블이 비어있습니다. LetterSeederConfig가 먼저 실행되었는지 확인하세요.");
                    return;
                }

                // LettersKcMap 테이블에 매핑 데이터 삽입 (복합키 사용)
                final String sql = """
                    INSERT INTO letters_kc_map (letters_id, knowledge_component_id)
                    VALUES (?, ?)
                    ON CONFLICT (letters_id, knowledge_component_id) DO NOTHING
                    """;

                try (PreparedStatement ps = con.prepareStatement(sql)) {
                    int batchCount = 0;
                    int totalMappings = 0;

                    for (LetterInfo letter : letters) {
                        // 음절 분해
                        SyllableComponents components = decomposeSyllable(letter.unicodePoint);
                        if (components == null) continue;

                        Set<Long> kcIds = new HashSet<>();

                        // 1. 초성 -> ONSET KC
                        Long onsetKcId = CHO_TO_ONSET_KC.get(components.cho);
                        if (onsetKcId != null) {
                            kcIds.add(onsetKcId);
                        }

                        // 2. 중성 -> NUCLEUS KC
                        Long nucleusKcId = JUNG_TO_NUCLEUS_KC.get(components.jung);
                        if (nucleusKcId != null) {
                            kcIds.add(nucleusKcId);
                        }

                        // 3. 종성 -> CODA KC (있는 경우만)
                        if (components.jong > 0) {
                            Long codaKcId = JONG_TO_CODA_KC.get(components.jong);
                            if (codaKcId != null) {
                                kcIds.add(codaKcId);
                            }
                        }

                        // 4. 받침 유무 -> SYLLABLE KC
                        if (components.jong > 0) {
                            kcIds.add(23L); // CLOSED_SYLLABLE
                        } else {
                            kcIds.add(24L); // OPEN_SYLLABLE
                        }

                        // 각 KC에 대해 매핑 삽입 (복합키: letters_id + knowledge_component_id)
                        for (Long kcId : kcIds) {
                            ps.setString(1, letter.id);
                            ps.setLong(2, kcId);
                            ps.addBatch();
                            batchCount++;
                            totalMappings++;
                        }

                        if (batchCount >= 1000) {
                            ps.executeBatch();
                            batchCount = 0;
                            log.info("진행 중: {} 매핑 삽입됨", totalMappings);
                        }
                    }

                    ps.executeBatch();
                    con.commit();
                    log.info("LetterKcMap 시딩 완료: {} letters processed", letters.size());
                } catch (Exception e) {
                    con.rollback();
                    log.error("LetterKcMap 시딩 실패", e);
                    throw e;
                }
            }
        };
    }

    private List<LetterInfo> loadLetters(Connection con) throws Exception {
        List<LetterInfo> letters = new ArrayList<>();
        String sql = "SELECT id, unicode, unicode_point FROM letters";

        try (PreparedStatement ps = con.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {
            while (rs.next()) {
                String id = rs.getString("id");
                String unicode = rs.getString("unicode");
                int unicodePoint = rs.getInt("unicode_point");
                letters.add(new LetterInfo(id, unicode, unicodePoint));
            }
        }
        return letters;
    }

    private SyllableComponents decomposeSyllable(int codePoint) {
        // 한글 음절 범위 확인
        if (codePoint < 0xAC00 || codePoint > 0xD7A3) {
            return null;
        }

        int sIndex = codePoint - 0xAC00;
        int cho = sIndex / 588;           // 초성 (0~18)
        int jung = (sIndex % 588) / 28;   // 중성 (0~20)
        int jong = sIndex % 28;           // 종성 (0~27)

        return new SyllableComponents(cho, jung, jong);
    }

    private static class LetterInfo {
        String id;
        String unicode;
        int unicodePoint;

        LetterInfo(String id, String unicode, int unicodePoint) {
            this.id = id;
            this.unicode = unicode;
            this.unicodePoint = unicodePoint;
        }
    }

    private static class SyllableComponents {
        int cho;   // 초성 인덱스
        int jung;  // 중성 인덱스
        int jong;  // 종성 인덱스

        SyllableComponents(int cho, int jung, int jong) {
            this.cho = cho;
            this.jung = jung;
            this.jong = jong;
        }
    }
}