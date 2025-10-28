package com.readingbuddy.backend.config;

import com.readingbuddy.backend.common.util.function.PhonemeCounter;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import javax.sql.DataSource;
import java.sql.Connection;
import java.sql.PreparedStatement;

@Configuration
@RequiredArgsConstructor
public class LetterSeederConfig {

    private final DataSource dataSource;

    // 필요 시 application.yml로 뺄 수 있음
    private static final String VOICE_FMT      = "";
    private static final String VOICE_SLOW_FMT = "";

    @Bean
    ApplicationRunner seedLettersRunner() {
        return args -> {
            // 재실행해도 중복은 자동 스킵(ON CONFLICT DO NOTHING)
            try (Connection con = dataSource.getConnection()) {
                con.setAutoCommit(false);

                // PostgreSQL: unicode에 UNIQUE 인덱스가 있다고 가정
                final String sql = ""
                        + "INSERT INTO letters(id, unicode, unicode_point, \"count\", voice_url, slow_voice_url) "
                        + "VALUES (?, ?, ?, ?, ?, ?) "
                        + "ON CONFLICT (unicode) DO NOTHING";

                try (PreparedStatement ps = con.prepareStatement(sql)) {
                    int batch = 0;
                    for (int cp = 0xAC00; cp <= 0xD7A3; cp++) {
                        String ucode = String.format("U+%04X", cp);
                        String id = ucode; // 필요 시 다른 규칙 사용
                        Integer unicodePoint = cp;
                        int cnt = PhonemeCounter.countForCodePoint(cp);
                        String voice = String.format(VOICE_FMT, cp);
                        String slow  = String.format(VOICE_SLOW_FMT, cp);

                        ps.setString(1, id);
                        ps.setString(2, ucode);
                        ps.setInt(3, cp);
                        ps.setInt(4, cnt);
                        ps.setString(5, voice);
                        ps.setString(6, slow);
                        ps.addBatch();

                        if (++batch % 1000 == 0) ps.executeBatch();
                    }
                    ps.executeBatch();
                    con.commit();
                } catch (Exception e) {
                    con.rollback();
                    throw e;
                }
            }
        };
    }
}
