package com.readingbuddy.backend.config;

import lombok.RequiredArgsConstructor;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import javax.sql.DataSource;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;

@Configuration
@RequiredArgsConstructor
public class WordSeederConfig {

    private final DataSource dataSource;

    // S3 voice URL 포맷
    private static final String VOICE_URL_FORMAT = "https://final-a206.s3.ap-northeast-2.amazonaws.com/voices/words/%s_normal.mp3";

    @Bean
    ApplicationRunner seedWordsVoiceUrlRunner() {
        return args -> {
            try (Connection con = dataSource.getConnection()) {
                con.setAutoCommit(false);

                // voice_url이 null이거나 비어있는 레코드 조회
                final String selectSql = "SELECT id, word FROM words WHERE voice_url IS NULL OR voice_url = ''";
                final String updateSql = "UPDATE words SET voice_url = ? WHERE id = ?";

                try (PreparedStatement selectPs = con.prepareStatement(selectSql);
                     PreparedStatement updatePs = con.prepareStatement(updateSql)) {

                    ResultSet rs = selectPs.executeQuery();
                    int batch = 0;

                    while (rs.next()) {
                        Long id = rs.getLong("id");
                        String word = rs.getString("word");

                        if (word != null && !word.isEmpty()) {
                            String voiceUrl = String.format(VOICE_URL_FORMAT, id);
                            updatePs.setString(1, voiceUrl);
                            updatePs.setLong(2, id);
                            updatePs.addBatch();

                            if (++batch % 1000 == 0) {
                                updatePs.executeBatch();
                            }
                        }
                    }

                    // 남은 배치 실행
                    if (batch % 1000 != 0) {
                        updatePs.executeBatch();
                    }

                    con.commit();
                } catch (Exception e) {
                    con.rollback();
                    throw e;
                }
            }
        };
    }
}
