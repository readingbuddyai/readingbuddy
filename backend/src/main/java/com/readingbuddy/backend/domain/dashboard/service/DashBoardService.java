package com.readingbuddy.backend.domain.dashboard.service;

import com.readingbuddy.backend.domain.dashboard.dto.response.StageCorrectRateResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageInfoResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageTryAvgResponse;
import com.readingbuddy.backend.domain.train.repository.TrainedStageHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;
import com.readingbuddy.backend.domain.dashboard.dto.response.DailyAttendResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.PhonemesTryRankResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.PhonemesWrongRankResponse;
import com.readingbuddy.backend.domain.dashboard.repository.AttendanceHistoriesRepository;
import com.readingbuddy.backend.domain.dashboard.dto.response.AttendanceResponse;
import com.readingbuddy.backend.domain.train.repository.TrainedProblemHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.AttendHistories;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class DashBoardService {

    private final TrainedStageHistoriesRepository trainedStageHistoriesRepository;
    private final AttendanceHistoriesRepository attendanceHistoriesRepository;
    private final TrainedProblemHistoriesRepository trainedProblemHistoriesRepository;

    /**
     * 사용자별 해당 스테이지의 통계 정보 조회
     * @param userId 사용자 ID
     * @param stage 스테이지 정보
     * @return 스테이지 통계 정보
     */
    public StageInfoResponse getStageInfo(Long userId, String stage) {
        // 해당 사용자의 특정 스테이지에 대한 모든 기록 조회
        List<TrainedStageHistories> histories = trainedStageHistoriesRepository.findByUserIdAndStage(userId, stage);

        // 통계 집계
        int totalTryCount = 0;
        int totalCorrectCount = 0;
        int totalWrongCount = 0;

        for (TrainedStageHistories history : histories) {
            if (history.getTryCount() != null) {
                totalTryCount += history.getTryCount();
            }
            if (history.getCorrectCount() != null) {
                totalCorrectCount += history.getCorrectCount();
            }
            if (history.getWrongCount() != null) {
                totalWrongCount += history.getWrongCount();
            }
        }

        return StageInfoResponse.builder()
                .stage(stage)
                .totalTryCount(totalTryCount)
                .totalCorrectCount(totalCorrectCount)
                .totalWrongCount(totalWrongCount)
                .build();
    }

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
     * 사용자별 해당 스테이지의 평균 시도 횟수 조회
     * @param userId 사용자 ID
     * @param stage 스테이지 정보
     * @return 스테이지 평균 시도 횟수
     */
    public StageTryAvgResponse getStageTryAverage(Long userId, String stage) {
        // 해당 사용자의 특정 스테이지에 대한 모든 기록 조회
        List<TrainedStageHistories> histories = trainedStageHistoriesRepository.findByUserIdAndStage(userId, stage);

        // 세션이 없는 경우
        if (histories.isEmpty()) {
            return StageTryAvgResponse.builder()
                    .stage(stage)
                    .averageTryCount(0.0)
                    .totalSessions(0)
                    .build();
        }

        // 평균 계산
        int totalTryCount = 0;
        for (TrainedStageHistories history : histories) {
            if (history.getTryCount() != null) {
                totalTryCount += history.getTryCount();
            }
        }

        double average = (double) totalTryCount / histories.size();

        return StageTryAvgResponse.builder()
                .stage(stage)
                .averageTryCount(Math.round(average * 100.0) / 100.0) // 소수점 2자리까지
                .totalSessions(histories.size())
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

    /**
     * 사용자별 해당 스테이지의 최근 세션 정답률 조회
     * @param userId 사용자 ID
     * @param stage 스테이지 정보
     * @return 최근 스테이지 세션의 정답률
     */
    public StageCorrectRateResponse getStageCorrectRate(Long userId, String stage) {
        // 해당 사용자의 특정 스테이지에 대한 최근 세션 조회
        Optional<TrainedStageHistories> latestHistory =
                trainedStageHistoriesRepository.findFirstByUserIdAndStageOrderByIdDesc(userId, stage);

        // 세션이 없는 경우
        if (latestHistory.isEmpty()) {
            return StageCorrectRateResponse.builder()
                    .stage(stage)
                    .correctRate(0.0)
                    .correctCount(0)
                    .wrongCount(0)
                    .totalProblems(0)
                    .completedAt(null)
                    .sessionKey(null)
                    .build();
        }

        TrainedStageHistories history = latestHistory.get();

        int correctCount = history.getCorrectCount() != null ? history.getCorrectCount() : 0;
        int wrongCount = history.getWrongCount() != null ? history.getWrongCount() : 0;
        int totalProblems = correctCount + wrongCount;

        // 정답률 계산 (0~100 사이의 값)
        double correctRate = 0.0;
        if (totalProblems > 0) {
            correctRate = ((double) correctCount / totalProblems) * 100.0;
            correctRate = Math.round(correctRate * 100.0) / 100.0; // 소수점 2자리까지
        }

        return StageCorrectRateResponse.builder()
                .stage(stage)
                .correctRate(correctRate)
                .correctCount(correctCount)
                .wrongCount(wrongCount)
                .totalProblems(totalProblems)
                .completedAt(history.getCompletedAt())
                .sessionKey(history.getSessionKey())
                .build();
    }

    /**
     * 사용자별 틀린 음소 조회 (내림차순)
     */
    public List<PhonemesWrongRankResponse> getWrongPhonemesRanking(Long userId, int limit) {
        List<Object[]> results = trainedProblemHistoriesRepository.getWrongPhonemesRanking(userId, limit);

        return results.stream()
                .map(row -> PhonemesWrongRankResponse.builder()
                        .phonemeId(((Number) row[0]).longValue())
                        .value((String) row[1])
                        .category((String) row[2])
                        .wrongCnt(((Number) row[3]).longValue())
                        .build())
                .collect(Collectors.toList());

    }

    /**
     * 사용자별 시도 횟수가 많음 음소 조회 (내림차순)
     */
     public List<PhonemesTryRankResponse> getTryPhonemesRanking(Long userId, int limit) {
         List<Object[]> results = trainedProblemHistoriesRepository.getTryPhonemesRanking(userId, limit);

         return results.stream()
                 .map(row -> PhonemesTryRankResponse.builder()
                         .phonemeId(((Number)row[0]).longValue())
                         .value((String) row[1])
                         .category((String) row[2])
                         .tryCnt(((Number) row[3]).longValue())
                         .build())
                 .collect(Collectors.toList());
     }

}
