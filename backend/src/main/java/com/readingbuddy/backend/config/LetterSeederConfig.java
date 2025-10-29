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
                        if (!isJongOrthPronSame(cp)) continue;
                        if (!isJungOrthPronSame(cp)) continue;

                        String ucode = String.format("U+%04X", cp);
                        String id = ucode; // 필요 시 다른 규칙 사용
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

    static boolean isJungOrthPronSame(int cp) {
        if (cp < 0xAC00 || cp > 0xD7A3) return false;
        int sIndex = cp - 0xAC00;

        int jung = (sIndex % (21 * 28)) / 28;

        switch (jung) {
            // 단모음 동일: ㅏ,ㅑ,ㅓ,ㅕ,ㅗ,ㅛ,ㅜ,ㅠ,ㅡ,ㅣ
            case 0:   // ㅏ
            case 2:   // ㅑ
            case 4:   // ㅓ
            case 6:   // ㅕ
            case 8:   // ㅗ
            case 12:  // ㅛ
            case 13:  // ㅜ
            case 17:  // ㅠ
            case 18:  // ㅡ
            case 20:  // ㅣ
            case 9:   // ㅘ
            case 14:  // ㅝ
            case 16:  // ㅟ
                return true;

            default:
                return false;
        }
    }

    static boolean isJongOrthPronSame(int cp) {
        if (cp < 0xAC00 || cp > 0xD7A3) return false;
        int sIndex = cp - 0xAC00;
        int jong = sIndex % 28;  // 종성 인덱스(0~27)
        switch (jong) {
            case 0:  // (없음)
            case 1:  // ㄱ
            case 4:  // ㄴ
            case 7:  // ㄷ
            case 8:  // ㄹ
            case 16: // ㅁ
            case 17: // ㅂ
            case 21: // ㅇ
                return true;
            default:
                return false;
        }
    }
}
