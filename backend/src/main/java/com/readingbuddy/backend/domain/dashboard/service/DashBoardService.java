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

@Service
@RequiredArgsConstructor
public class DashBoardService {

    private final TrainedStageHistoriesRepository trainedStageHistoriesRepository;

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
}
