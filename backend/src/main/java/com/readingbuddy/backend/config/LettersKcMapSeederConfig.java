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

    // 초성 인덱스 -> ONSET KC ID 매핑 (4.1 + 4.2)
    private static final Map<Integer, List<Long>> CHO_TO_ONSET_KC = new HashMap<>() {{
        put(0, List.of(8L, 34L));   // ㄱ -> VELAR_ONSET_1 + VELAR_ONSET_2
        put(1, List.of(8L, 34L));   // ㄲ -> VELAR_ONSET_1 + VELAR_ONSET_2
        put(2, List.of(9L, 35L));   // ㄴ -> ALVEOLAR_ONSET_1 + ALVEOLAR_ONSET_2
        put(3, List.of(9L, 35L));   // ㄷ -> ALVEOLAR_ONSET_1 + ALVEOLAR_ONSET_2
        put(4, List.of(9L, 35L));   // ㄸ -> ALVEOLAR_ONSET_1 + ALVEOLAR_ONSET_2
        put(5, List.of(12L, 38L));  // ㄹ -> GLOTTAL_AND_ALVEOLAR_ONSET_1 + GLOTTAL_AND_ALVEOLAR_ONSET_2
        put(6, List.of(7L, 33L));   // ㅁ -> LABIAL_ONSET_1 + LABIAL_ONSET_2
        put(7, List.of(7L, 33L));   // ㅂ -> LABIAL_ONSET_1 + LABIAL_ONSET_2
        put(8, List.of(7L, 33L));   // ㅃ -> LABIAL_ONSET_1 + LABIAL_ONSET_2
        put(9, List.of(11L, 37L));  // ㅅ -> ALVEOLAR_FRICATIVE_ONSET_1 + ALVEOLAR_FRICATIVE_ONSET_2
        put(10, List.of(11L, 37L)); // ㅆ -> ALVEOLAR_FRICATIVE_ONSET_1 + ALVEOLAR_FRICATIVE_ONSET_2
        put(11, List.of(8L, 34L));  // ㅇ -> VELAR_ONSET_1 + VELAR_ONSET_2
        put(12, List.of(10L, 36L)); // ㅈ -> PALATAL_ONSET_1 + PALATAL_ONSET_2
        put(13, List.of(10L, 36L)); // ㅉ -> PALATAL_ONSET_1 + PALATAL_ONSET_2
        put(14, List.of(10L, 36L)); // ㅊ -> PALATAL_ONSET_1 + PALATAL_ONSET_2
        put(15, List.of(8L, 34L));  // ㅋ -> VELAR_ONSET_1 + VELAR_ONSET_2
        put(16, List.of(9L, 35L));  // ㅌ -> ALVEOLAR_ONSET_1 + ALVEOLAR_ONSET_2
        put(17, List.of(7L, 33L));  // ㅍ -> LABIAL_ONSET_1 + LABIAL_ONSET_2
        put(18, List.of(12L, 38L)); // ㅎ -> GLOTTAL_AND_ALVEOLAR_ONSET_1 + GLOTTAL_AND_ALVEOLAR_ONSET_2
    }};

    // 종성 인덱스 -> CODA KC ID 매핑 (4.1 + 4.2)
    private static final Map<Integer, List<Long>> JONG_TO_CODA_KC = new HashMap<>() {{
        put(1, List.of(14L, 40L));  // ㄱ -> VELAR_CODA_1 + VELAR_CODA_2
        put(2, List.of(14L, 40L));  // ㄲ -> VELAR_CODA_1 + VELAR_CODA_2
        put(4, List.of(15L, 41L));  // ㄴ -> ALVEOLAR_CODA_1 + ALVEOLAR_CODA_2
        put(7, List.of(15L, 41L));  // ㄷ -> ALVEOLAR_CODA_1 + ALVEOLAR_CODA_2
        put(8, List.of(18L, 44L));  // ㄹ -> GLOTTAL_AND_ALVEOLAR_CODA_1 + GLOTTAL_AND_ALVEOLAR_CODA_2
        put(16, List.of(13L, 39L)); // ㅁ -> LABIAL_CODA_1 + LABIAL_CODA_2
        put(17, List.of(13L, 39L)); // ㅂ -> LABIAL_CODA_1 + LABIAL_CODA_2
        put(19, List.of(17L, 43L)); // ㅅ -> ALVEOLAR_FRICATIVE_CODA_1 + ALVEOLAR_FRICATIVE_CODA_2
        put(20, List.of(17L, 43L)); // ㅆ -> ALVEOLAR_FRICATIVE_CODA_1 + ALVEOLAR_FRICATIVE_CODA_2
        put(21, List.of(14L, 40L)); // ㅇ -> VELAR_CODA_1 + VELAR_CODA_2
        put(22, List.of(16L, 42L)); // ㅈ -> PALATAL_CODA_1 + PALATAL_CODA_2
        put(23, List.of(16L, 42L)); // ㅊ -> PALATAL_CODA_1 + PALATAL_CODA_2
        put(24, List.of(14L, 40L)); // ㅋ -> VELAR_CODA_1 + VELAR_CODA_2
        put(25, List.of(15L, 41L)); // ㅌ -> ALVEOLAR_CODA_1 + ALVEOLAR_CODA_2
        put(26, List.of(13L, 39L)); // ㅍ -> LABIAL_CODA_1 + LABIAL_CODA_2
        put(27, List.of(18L, 44L)); // ㅎ -> GLOTTAL_AND_ALVEOLAR_CODA_1 + GLOTTAL_AND_ALVEOLAR_CODA_2
    }};

    // 중성 인덱스 -> NUCLEUS KC ID 매핑 (4.1 + 4.2)
    private static final Map<Integer, List<Long>> JUNG_TO_NUCLEUS_KC = new HashMap<>() {{
        // 단모음 -> MONOPHTHONG_NUCLEUS_1 + MONOPHTHONG_NUCLEUS_2
        put(0, List.of(21L, 45L));  // ㅏ
        put(1, List.of(21L, 45L));  // ㅐ
        put(4, List.of(21L, 45L));  // ㅓ
        put(5, List.of(21L, 45L));  // ㅔ
        put(8, List.of(21L, 45L));  // ㅗ
        put(11, List.of(21L, 45L)); // ㅚ
        put(12, List.of(21L, 45L)); // ㅛ
        put(13, List.of(21L, 45L)); // ㅜ
        put(16, List.of(21L, 45L)); // ㅟ
        put(17, List.of(21L, 45L)); // ㅠ
        put(18, List.of(21L, 45L)); // ㅡ
        put(20, List.of(21L, 45L)); // ㅣ

        // 이중모음 -> DIPHTHONG_NUCLEUS_1 + DIPHTHONG_NUCLEUS_2
        put(2, List.of(22L, 46L));  // ㅑ
        put(3, List.of(22L, 46L));  // ㅒ
        put(6, List.of(22L, 46L));  // ㅕ
        put(7, List.of(22L, 46L));  // ㅖ
        put(9, List.of(22L, 46L));  // ㅘ
        put(10, List.of(22L, 46L)); // ㅙ
        put(14, List.of(22L, 46L)); // ㅝ
        put(15, List.of(22L, 46L)); // ㅞ
        put(19, List.of(22L, 46L)); // ㅢ
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

                        // 1. 초성 -> ONSET KCs (4.1 + 4.2)
                        List<Long> onsetKcIds = CHO_TO_ONSET_KC.get(components.cho);
                        if (onsetKcIds != null) {
                            kcIds.addAll(onsetKcIds);
                        }

                        // 2. 중성 -> NUCLEUS KCs (4.1 + 4.2)
                        List<Long> nucleusKcIds = JUNG_TO_NUCLEUS_KC.get(components.jung);
                        if (nucleusKcIds != null) {
                            kcIds.addAll(nucleusKcIds);
                        }

                        // 3. 종성 -> CODA KCs (있는 경우만, 4.1 + 4.2)
                        if (components.jong > 0) {
                            List<Long> codaKcIds = JONG_TO_CODA_KC.get(components.jong);
                            if (codaKcIds != null) {
                                kcIds.addAll(codaKcIds);
                            }
                        }

                        if (components.jung > 0) {
                            kcIds.add(23L);
                        } else {
                            kcIds.add(24L);
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