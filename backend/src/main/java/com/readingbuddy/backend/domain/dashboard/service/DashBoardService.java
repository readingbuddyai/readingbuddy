package com.readingbuddy.backend.domain.dashboard.service;

import com.readingbuddy.backend.domain.dashboard.dto.response.DailyAttendResponse;
import com.readingbuddy.backend.domain.dashboard.repository.AttendanceHistoriesRepository;
import com.readingbuddy.backend.domain.dashboard.dto.response.AttendanceResponse;
import com.readingbuddy.backend.domain.user.entity.AttendHistories;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;

@Service
@RequiredArgsConstructor
public class DashBoardService {

    private final AttendanceHistoriesRepository attendanceHistoriesRepository;

    /**
     * 특정 기간의 출석 기록 조회
     */
    public AttendanceResponse getAttendanceHistoriesByDate(Long userId, LocalDate startDate, LocalDate endDate) {
        // 기간별 출석 기록 조회
        List<AttendHistories> attendanceHistories = attendanceHistoriesRepository
                .findByUserIdAndDateRange(userId, startDate, endDate);

        // AttendHistories를 AttendDateInfo로 변환 (playtime을 "분:초" 형식으로 변환)
        List<AttendanceResponse.AttendDateInfo> attendDateInfos = attendanceHistories.stream()
                .map(history -> AttendanceResponse.AttendDateInfo.builder()
                        .attendDate(history.getAttendDate())
                        .playtime(AttendanceResponse.formatPlaytime(history.getPlaytime()))
                        .build())
                .toList();

        // PeriodAttendance 생성
        AttendanceResponse.PeriodAttendance periodData = AttendanceResponse.PeriodAttendance.builder()
                .attendDates(attendDateInfos)
                .totalAttendDays(attendDateInfos.size())
                .build();

        return AttendanceResponse.builder()
                .periodData(periodData)
                .build();
    }

    /**
     * 특정 날짜의 출석 기록 조회
     */
    public AttendanceResponse getDailyAttendance(Long userId, LocalDate date) {

        Optional<AttendHistories> attendHistory =  attendanceHistoriesRepository.findByUserIdAndDate(userId, date);

        AttendanceResponse.DailyAttendance dailyData;

        if (attendHistory.isPresent()) {
            AttendHistories history = attendHistory.get();
            dailyData = AttendanceResponse.DailyAttendance.builder()
                    .attendDate(date)
                    .playtime(AttendanceResponse.formatPlaytime(history.getPlaytime()))
                    .attended(true)
                    .build();
        } else {
            dailyData = AttendanceResponse.DailyAttendance.builder()
                    .attendDate(date)
                    .playtime("0:00")
                    .attended(false)
                    .build();
        }
        return AttendanceResponse.builder()
                .dailyData(dailyData)
                .build();
    }
}
