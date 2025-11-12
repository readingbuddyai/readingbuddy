package com.readingbuddy.backend.domain.dashboard.service;

import com.readingbuddy.backend.domain.dashboard.dto.response.*;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageKcMasteryTrendResponse.KcTrend;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageKcMasteryTrendResponse.MasteryPoint;
import com.readingbuddy.backend.domain.train.repository.TrainedStageHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;
import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

import com.readingbuddy.backend.domain.dashboard.repository.AttendanceHistoriesRepository;
import com.readingbuddy.backend.domain.train.repository.TrainedProblemHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.AttendHistories;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.util.stream.Collectors;

import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;

@Service
@RequiredArgsConstructor
public class DashBoardService {

    private final TrainedStageHistoriesRepository trainedStageHistoriesRepository;
    private final AttendanceHistoriesRepository attendanceHistoriesRepository;
    private final TrainedProblemHistoriesRepository trainedProblemHistoriesRepository;
    private final UserKcMasteryRepository userKcMasteryRepository;
    private final KnowledgeComponentRepository knowledgeComponentRepository;

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
     * 사용자별 해당 스테이지의 problem_number별 평균 시도 횟수 조회
     * @param userId 사용자 ID
     * @param stage 스테이지 정보
     * @return 스테이지의 problem_number별 평균 시도 횟수
     */
    public StageTryAvgResponse getStageTryAverage(Long userId, String stage) {
        // 해당 사용자의 특정 스테이지에 대한 모든 기록 조회 (세션 수 계산용)
        List<TrainedStageHistories> histories = trainedStageHistoriesRepository.findByUserIdAndStage(userId, stage);

        // problem_number별 평균 시도 횟수 조회
        Double averageTryCount = trainedStageHistoriesRepository.getAverageTryCountPerProblem(userId, stage);

        // 데이터가 없는 경우
        if (averageTryCount == null) {
            return StageTryAvgResponse.builder()
                    .stage(stage)
                    .averageTryCount(0.0)
                    .totalSessions(histories.size())
                    .build();
        }

        return StageTryAvgResponse.builder()
                .stage(stage)
                .averageTryCount(Math.round(averageTryCount * 100.0) / 100.0) // 소수점 2자리까지
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
                    .build();
        }

        TrainedStageHistories history = latestHistory.get();

        int correctCount = history.getCorrectCount() != null ? history.getCorrectCount() : 0;
        int wrongCount = history.getWrongCount() != null ? history.getWrongCount() : 0;
        int totalProblems = history.getTotalCount() != null ? history.getTotalCount() : 0;

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

    /**
     * 특정 KC의 숙련도 p_l 변화 추이 조회
     * @param userId 사용자 ID
     * @param kcId Knowledge Component ID
     * @param startDate 조회 시작 날짜
     * @param endDate 조회 종료 날짜
     * @return KC 숙련도 변화 추이
     */
    public KcMasteryTrendResponse getKcMasteryTrend(Long userId, Long kcId, LocalDate startDate, LocalDate endDate) {
        // KC 정보 조회
        KnowledgeComponent kc = knowledgeComponentRepository.findById(kcId)
                .orElseThrow(() -> new IllegalArgumentException("존재하지 않는 Knowledge Component ID입니다: " + kcId));

        // LocalDate를 LocalDateTime으로 변환
        LocalDateTime startDateTime = startDate.atStartOfDay();
        LocalDateTime endDateTime = endDate.atTime(LocalTime.MAX);

        // 해당 사용자의 KC 숙련도 변화 이력 조회 (시간순, 기간 필터링)
        List<UserKcMastery> masteryHistory = userKcMasteryRepository
                .findByUser_IdAndKnowledgeComponent_IdAndCreatedAtBetweenOrderByCreatedAtAsc(
                        userId, kcId, startDateTime, endDateTime);

        // MasteryPoint 리스트로 변환
        List<KcMasteryTrendResponse.MasteryPoint> masteryTrend = masteryHistory.stream()
                .map(mastery -> KcMasteryTrendResponse.MasteryPoint.builder()
                        .p_l(mastery.getPLearn())
                        .p_t(mastery.getPTrain())
                        .p_g(mastery.getPGuess())
                        .p_s(mastery.getPSlip())
                        .updatedAt(mastery.getUpdatedAt())
                        .build())
                .collect(Collectors.toList());

        return KcMasteryTrendResponse.builder()
                .kcId(kcId)
                .kcCategory(kc.getCategory().name())
                .stage(kc.getStage())
                .masteryTrend(masteryTrend)
                .build();
    }

    /**
     * 특정 stage에 대한 현재 p_l 조회
     * @param userId 사용자 ID
     * @param stage 스테이지 정보
     * @param startDate 조회 시작 날짜
     * @param endDate 조회 종료 날짜
     * @return stage별 KC 숙련도 목록
     */
    public StageMasteryResponse getStageMastery(Long userId, String stage, LocalDate startDate, LocalDate endDate) {
        // 해당 stage에 속한 모든 KC 조회
        List<KnowledgeComponent> kcs = knowledgeComponentRepository.findByStage(stage);

        if (kcs.isEmpty()) {
            throw new IllegalArgumentException("해당 stage에 대한 Knowledge Component가 존재하지 않습니다: " + stage);
        }

        // LocalDate를 LocalDateTime으로 변환
        LocalDateTime startDateTime = startDate.atStartOfDay();
        LocalDateTime endDateTime = endDate.atTime(LocalTime.MAX);

        // 각 KC의 해당 기간 내 최신 숙련도 조회
        List<StageMasteryResponse.KcMastery> kcMasteries = kcs.stream()
                .map(kc -> {
                    Optional<UserKcMastery> latestMastery = userKcMasteryRepository
                            .findFirstByUser_IdAndKnowledgeComponent_IdAndCreatedAtBetweenOrderByCreatedAtDesc(
                                    userId, kc.getId(), startDateTime, endDateTime);

                    // 숙련도 데이터가 없는 경우 기본값 (초기 상태)
                    if (latestMastery.isEmpty()) {
                        return StageMasteryResponse.KcMastery.builder()
                                .kcId(kc.getId())
                                .kcCategory(kc.getCategory().name())
                                .pLearn(0.0f)
                                .pTrain(0.0f)
                                .pGuess(0.0f)
                                .pSlip(0.0f)
                                .updatedAt(null)
                                .build();
                    }

                    UserKcMastery mastery = latestMastery.get();
                    return StageMasteryResponse.KcMastery.builder()
                            .kcId(kc.getId())
                            .kcCategory(kc.getCategory().name())
                            .pLearn(mastery.getPLearn())
                            .pTrain(mastery.getPTrain())
                            .pGuess(mastery.getPGuess())
                            .pSlip(mastery.getPSlip())
                            .updatedAt(mastery.getUpdatedAt())
                            .build();
                })
                .collect(Collectors.toList());

        // 평균 숙련도 계산
        double averageMastery = kcMasteries.stream()
                .mapToDouble(StageMasteryResponse.KcMastery::getPLearn)
                .average()
                .orElse(0.0);

        // 소수점 4자리까지 반올림
        averageMastery = Math.round(averageMastery * 10000.0) / 10000.0;

        return StageMasteryResponse.builder()
                .stage(stage)
                .kcMasteries(kcMasteries)
                .averageMastery(averageMastery)
                .build();
    }

    /**
     * 특정 날짜의 훈련 기록 조회
     */
    public StageProblemListResponse getStageProblemListByDate(Long userId, LocalDate date) {
        // 날짜 범위 설정 (해당 날짜의 00:00:00 ~ 23:59:59)
        LocalDateTime startDateTime = date.atStartOfDay();
        LocalDateTime endDateTime = date.atTime(LocalTime.MAX);

        // 해당 날짜의 모든 훈련 세션 조회
        List<TrainedStageHistories> sessions = trainedStageHistoriesRepository
                .getStageProblemListByDate(userId, startDateTime, endDateTime);

        // 세션별로 문제 정보 조회 및 변환
        List<StageProblemListResponse.SessionInfo> sessionInfos = sessions.stream()
                .map(stage -> {
                    // 해당 세션의 모든 문제 이력 조회
                    List<TrainedProblemHistories> problems = trainedProblemHistoriesRepository
                            .findByTrainedStageHistories(stage);

                    // 중복되지 않는 problemNumber의 개수 계산
                    int totalCount = (int) problems.stream()
                            .map(TrainedProblemHistories::getProblemNumber)
                            .distinct()
                            .count();

                    // 문제 정보를 DTO로 변환
                    List<StageProblemListResponse.ProblemInfo> problemInfos = problems.stream()
                            .map(problem -> StageProblemListResponse.ProblemInfo.builder()
                                    .problemId(problem.getId())
                                    .problemNumber(problem.getProblemNumber())
                                    .problem(problem.getProblem())
                                    .answer(problem.getAnswer())
                                    .isCorrect(problem.getIsCorrect())
                                    .isReplyCorrect(problem.getIsReplyCorrect())
                                    .attemptNumber(problem.getAttemptNumber())
                                    .audioUrl(problem.getAudioUrl())
                                    .solvedAt(problem.getSolvedAt())
                                    .build())
                            .sorted((p1, p2) -> p1.getProblemNumber().compareTo(p2.getProblemNumber()))
                            .collect(Collectors.toList());

                    // 세션 정보 생성
                    return StageProblemListResponse.SessionInfo.builder()
                            .trainedStageHistoryId(stage.getId())
                            .stage(stage.getStage())
                            .startedAt(stage.getStartedAt())
                            .totalCount(totalCount)
                            .correctCount(stage.getCorrectCount())
                            .wrongCount(stage.getWrongCount())
                            .problems(problemInfos)
                            .build();
                }).collect(Collectors.toList());

        return StageProblemListResponse.builder()
                .date(date)
                .session(sessionInfos)
                .build();
    }

    /**
     * 특정 stage에 속한 모든 KC의 숙련도 변화 추이 조회
     * @param userId 사용자 ID
     * @param stage 스테이지 정보
     * @param startDate 조회 시작 날짜
     * @param endDate 조회 종료 날짜
     * @return stage별 모든 KC의 숙련도 변화 추이
     */
    public StageKcMasteryTrendResponse getStageKcMasteryTrend(Long userId, String stage, LocalDate startDate, LocalDate endDate) {
        // 해당 stage에 속한 모든 KC 조회
        List<KnowledgeComponent> kcs = knowledgeComponentRepository.findByStage(stage);

        if (kcs.isEmpty()) {
            throw new IllegalArgumentException("해당 stage에 대한 Knowledge Component가 존재하지 않습니다: " + stage);
        }

        // LocalDate를 LocalDateTime으로 변환
        LocalDateTime startDateTime = startDate.atStartOfDay();
        LocalDateTime endDateTime = endDate.atTime(LocalTime.MAX);

        // 각 KC의 숙련도 변화 추이 조회
        List<KcTrend> kcTrends = kcs.stream()
                .map(kc -> {
                    // 해당 KC의 기간 내 모든 숙련도 이력 조회
                    List<UserKcMastery> masteryHistory = userKcMasteryRepository
                            .findByUser_IdAndKnowledgeComponent_IdAndCreatedAtBetweenOrderByCreatedAtAsc(
                                    userId, kc.getId(), startDateTime, endDateTime);

                    // MasteryPoint 리스트로 변환
                    List<MasteryPoint> masteryTrend = masteryHistory.stream()
                            .map(mastery -> MasteryPoint.builder()
                                    .pLearn(mastery.getPLearn())
                                    .pTrain(mastery.getPTrain())
                                    .pGuess(mastery.getPGuess())
                                    .pSlip(mastery.getPSlip())
                                    .updatedAt(mastery.getUpdatedAt())
                                    .build())
                            .collect(Collectors.toList());

                    return KcTrend.builder()
                            .kcId(kc.getId())
                            .kcCategory(kc.getCategory().name())
                            .kcDescription(kc.getCategory().getDescription())
                            .masteryTrend(masteryTrend)
                            .build();
                })
                .collect(Collectors.toList());

        return StageKcMasteryTrendResponse.builder()
                .stage(stage)
                .kcTrends(kcTrends)
                .build();
    }

}
