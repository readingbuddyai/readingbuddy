package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDate;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class DailyAttendResponse {

    private LocalDate attendDate;
    private String playtime;
    private boolean attended;

    /**
     * 초 단위 playtime을 "분:초" 형식으로 변환
     */
    public static String formatPlaytime(Integer playtimeInSeconds) {
        return AttendanceResponse.formatPlaytime(playtimeInSeconds);
    }
}

