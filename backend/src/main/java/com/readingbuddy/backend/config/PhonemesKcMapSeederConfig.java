package com.readingbuddy.backend.config;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import javax.sql.DataSource;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.*;

@Configuration
@RequiredArgsConstructor
@Slf4j
public class PhonemesKcMapSeederConfig {

    private final DataSource dataSource;

    // 자음 -> KnowledgeComponent ID 리스트 매핑 (하나의 자음이 여러 KC와 연결)
    private static final Map<String, List<Long>> CONSONANT_KC_MAP = new HashMap<>() {{
        // LABIAL 계열
        put("ㅂ", Arrays.asList(1L, 25L));  // LABIAL_1, LABIAL_2
        put("ㅃ", Arrays.asList(1L, 25L));
        put("ㅍ", Arrays.asList(1L, 25L));
        put("ㅁ", Arrays.asList(1L, 25L));

        // VELAR 계열
        put("ㄱ", Arrays.asList(2L, 26L));  // VELAR_1, VELAR_2
        put("ㄲ", Arrays.asList(2L, 26L));
        put("ㅋ", Arrays.asList(2L, 26L));
        put("ㅇ", Arrays.asList(2L, 26L));

        // ALVEOLAR 계열
        put("ㄴ", Arrays.asList(3L, 27L));  // ALVEOLAR_1, ALVEOLAR_2
        put("ㄷ", Arrays.asList(3L, 27L));
        put("ㄸ", Arrays.asList(3L, 27L));
        put("ㅌ", Arrays.asList(3L, 27L));

        // PALATAL 계열
        put("ㅈ", Arrays.asList(4L, 28L));  // PALATAL_1, PALATAL_2
        put("ㅉ", Arrays.asList(4L, 28L));
        put("ㅊ", Arrays.asList(4L, 28L));

        // ALVEOLAR_FRICATIVE 계열
        put("ㅅ", Arrays.asList(5L, 29L));  // ALVEOLAR_FRICATIVE_1, ALVEOLAR_FRICATIVE_2
        put("ㅆ", Arrays.asList(5L, 29L));

        // GLOTTAL_AND_ALVEOLAR 계열
        put("ㅎ", Arrays.asList(6L, 30L));  // GLOTTAL_AND_ALVEOLAR_1, GLOTTAL_AND_ALVEOLAR_2
        put("ㄹ", Arrays.asList(6L, 30L));
    }};

    // 모음 -> KnowledgeComponent ID 리스트 매핑
    private static final Map<String, List<Long>> VOWEL_KC_MAP = new HashMap<>() {{
        // MONOPHTHONG 계열
        put("ㅏ", Arrays.asList(19L, 31L));  // MONOPHTHONG_1, MONOPHTHONG_2, MONOPHTHONG_NUCLEUS
        put("ㅓ", Arrays.asList(19L, 31L));
        put("ㅗ", Arrays.asList(19L, 31L));
        put("ㅜ", Arrays.asList(19L, 31L));
        put("ㅡ", Arrays.asList(19L, 31L));
        put("ㅣ", Arrays.asList(19L, 31L));
        put("ㅐ", Arrays.asList(19L, 31L));
        put("ㅔ", Arrays.asList(19L, 31L));
        put("ㅚ", Arrays.asList(19L, 31L));
        put("ㅟ", Arrays.asList(19L, 31L));

        // DIPHTHONG 계열
        put("ㅑ", Arrays.asList(20L, 32L));  // DIPHTHONG_1, DIPHTHONG_2
        put("ㅕ", Arrays.asList(20L, 32L));
        put("ㅠ", Arrays.asList(20L, 32L));
//        put("ㅒ", Arrays.asList(20L, 32L));
        put("ㅛ", Arrays.asList(20L, 32L));
        put("ㅖ", Arrays.asList(20L, 32L));
        put("ㅘ", Arrays.asList(20L, 32L));
//        put("ㅙ", Arrays.asList(20L, 32L));
        put("ㅝ", Arrays.asList(20L, 32L));
//        put("ㅞ", Arrays.asList(20L, 32L));
        put("ㅢ", Arrays.asList(20L, 32L));
    }};

    @Bean
    ApplicationRunner seedPhonemesKcMapRunner() {
        return args -> {
            try (Connection con = dataSource.getConnection()) {
                con.setAutoCommit(false);

                // Phonemes ID를 조회하기 위한 맵
                Map<String, Long> phonemeIdMap = loadPhonemesIdMap(con);

                // PhonemesKcMap 테이블에 매핑 데이터 삽입
                final String sql = """
                    INSERT INTO phonemes_kc_map (phonemes_id, knowledge_component_id)
                    VALUES (?, ?)
                    ON CONFLICT (phonemes_id, knowledge_component_id) DO NOTHING
                    """;

                try (PreparedStatement ps = con.prepareStatement(sql)) {
                    // 자음 매핑 삽입
                    insertConsonantMappings(ps, phonemeIdMap);

                    // 모음 매핑 삽입
                    insertVowelMappings(ps, phonemeIdMap);

                    ps.executeBatch();
                    con.commit();
                } catch (Exception e) {
                    con.rollback();
                    throw e;
                }
            }
        };
    }

    private Map<String, Long> loadPhonemesIdMap(Connection con) throws Exception {
        Map<String, Long> map = new HashMap<>();
        String sql = "SELECT id, value FROM phonemes";

        try (PreparedStatement ps = con.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {
            while (rs.next()) {
                Long id = rs.getLong("id");
                String value = rs.getString("value");
                map.put(value, id);
            }
        }
        return map;
    }

    private void insertConsonantMappings(PreparedStatement ps, Map<String, Long> phonemeIdMap) throws Exception {
        for (Map.Entry<String, List<Long>> entry : CONSONANT_KC_MAP.entrySet()) {
            String consonant = entry.getKey();
            List<Long> kcIds = entry.getValue();

            Long phonemeId = phonemeIdMap.get(consonant);
            if (phonemeId == null) {
                log.error("phoneme 테이블에 매핑되는 자음 Id가 없습니다.");
                continue; // Phoneme이 아직 없으면 스킵
            }

            for (Long kcId : kcIds) {
                ps.setLong(1, phonemeId);
                ps.setLong(2, kcId);
                ps.addBatch();
            }
        }
    }

    private void insertVowelMappings(PreparedStatement ps, Map<String, Long> phonemeIdMap) throws Exception {
        for (Map.Entry<String, List<Long>> entry : VOWEL_KC_MAP.entrySet()) {
            String vowel = entry.getKey();
            List<Long> kcIds = entry.getValue();

            Long phonemeId = phonemeIdMap.get(vowel);
            if (phonemeId == null) {
                log.error("phoneme 테이블에 매핑되는 모음 Id가 없습니다.");
                continue; // Phoneme이 아직 없으면 스킵
            }

            for (Long kcId : kcIds) {
                ps.setLong(1, phonemeId);
                ps.setLong(2, kcId);
                ps.addBatch();
            }
        }
    }
}