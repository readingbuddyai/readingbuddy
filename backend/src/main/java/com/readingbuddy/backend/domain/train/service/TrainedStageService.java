package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.request.AttemptRequest;
import com.readingbuddy.backend.domain.train.dto.request.StageCompleteRequest;
import com.readingbuddy.backend.domain.train.dto.request.StageStartRequest;
import com.readingbuddy.backend.domain.train.dto.response.AttemptResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageCompleteResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageStartResponse;
import com.readingbuddy.backend.domain.train.repository.TrainedProblemHistoriesRepository;
import com.readingbuddy.backend.domain.train.repository.TrainedStageHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;
import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import com.readingbuddy.backend.domain.user.entity.User;
import com.readingbuddy.backend.domain.user.repository.UserRepository;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.List;

@Service
@Transactional
@RequiredArgsConstructor
public class TrainedStageService {

    private final UserRepository userRepository;
    private final TrainedStageHistoriesRepository trainedStageHistoriesRepository;
    private final TrainedProblemHistoriesRepository trainedProblemHistoriesRepository;

    /**
     * Stage 시작 - 새로운 훈련 세션 생성
     */
    public StageStartResponse startStage(StageStartRequest request) {

        User user = userRepository.findById(request.getUserId())
                .orElseThrow(() -> new IllegalArgumentException("회원을 찾을 수 없습니다: " + request.getUserId()));

        // 새 세션 생성
        TrainedStageHistories session = TrainedStageHistories.builder()
                .user(user)
                .stage(request.getStage())
                .problemCount(request.getTotalProblems())
                .correctCount(0)
                .wrongCount(0)
                .turnedCount(0)
                .build();

        session = trainedStageHistoriesRepository.save(session);

        return StageStartResponse.builder()
                .sessionId(session.getId())
                .stage(session.getStage())
                .totalProblems(session.getProblemCount())
                .startAt(session.getStartedAt())
                .build();
    }

    /**
     * 문제 시도 제출 - 개별 문제 시도 기록 저장
     */
    public AttemptResponse submitAttempt(AttemptRequest request) {
        // 세션 조회
        TrainedStageHistories session = trainedStageHistoriesRepository.findById(request.getSessionId())
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + request.getSessionId()));

        // 시도 기록 생성
        TrainedProblemHistories attempt = TrainedProblemHistories.builder()
                .trainedStageHistories(session)
                .problemId(request.getProblemId())
                .phonemes(request.getPhonemes())
                .word(request.getWord())
                .tryCount(request.getTryCount())
                .reply(request.getReply())
                .isCorrect(request.getIsCorrect())
                .solvedAt(LocalDateTime.now())
                .build();

        attempt = trainedProblemHistoriesRepository.save(attempt);

        return AttemptResponse.builder()
                .attemptId(attempt.getId())
                .sessionId(session.getId())
                .attemptsTried(request.getTryCount())
                .build();
    }

    /**
     * Stage 완료 - 통계 집계 및 세션 종료
     */
    public StageCompleteResponse completeStage(StageCompleteRequest request) {
        // 세션 조회
        TrainedStageHistories session = trainedStageHistoriesRepository.findById(request.getSessionId())
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + request.getSessionId()));

        // 이 세션의 모든 시도 기록 조회
        List<TrainedProblemHistories> attempts = trainedProblemHistoriesRepository.findByTrainedStageHistories(session);

        // 통계 집계
        long correctCount = attempts.stream()
                .filter(TrainedProblemHistories::getIsCorrect)
                .map(TrainedProblemHistories::getProblemId)
                .distinct()  // 같은 문제 여러 시도 중 한 번이라도 맞으면 정답
                .count();

        int totalProblems = session.getProblemCount();
        int wrongCount = totalProblems - (int) correctCount;

        // Setter 추가해서 값 갱신

        return StageCompleteResponse.builder()
                .sessionId(session.getId())
                .stage(session.getStage())
                .totalProblems(totalProblems)
                .correctCount((int) correctCount)
                .wrongCount(wrongCount)
                .turnedCount(0)
                .completedAt(LocalDateTime.now())
                .build();



    }




}
