package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDate;
import java.util.List;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class AttendanceResponse {

    // 기간별 조회 데이터
    private PeriodAttendance periodData;

    // 일별 조회 데이터
    private DailyAttendance dailyData;

    /**
     * 기간별 출석 정보
     */
    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class PeriodAttendance {
        private List<AttendDateInfo> attendDates;
        private Integer totalAttendDays;
    }

    /**
     * 일별 출석 정보
     */
    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class DailyAttendance {
        private LocalDate attendDate;
        private String playtime;
        private boolean attended;
    }

    /**
     * 출석 날짜 상세 정보
     */
    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class AttendDateInfo {
        private LocalDate attendDate;
        private String playtime;
    }

    /**
     * 초 단위 playtime을 "분:초" 형식으로 변환
     */
    public static String formatPlaytime(Integer playtimeInSeconds) {
        if (playtimeInSeconds == null) {
            return "00:00";
        }

        int minutes = playtimeInSeconds / 60;
        int seconds   = playtimeInSeconds % 60;
        return String.format("%d:%02d", minutes, seconds);
    }
}
