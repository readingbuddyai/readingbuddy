package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.request.AttemptRequest;
import com.readingbuddy.backend.domain.train.dto.request.StageCompleteRequest;
import com.readingbuddy.backend.domain.train.dto.request.StageStartRequest;
import com.readingbuddy.backend.domain.train.dto.response.AttemptResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageCompleteResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageStartResponse;
import com.readingbuddy.backend.domain.train.dto.result.SessionInfo;
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
import java.util.Set;

@Service
@Transactional
@RequiredArgsConstructor
public class TrainedStageService {

    private final UserRepository userRepository;
    private final TrainedStageHistoriesRepository trainedStageHistoriesRepository;
    private final TrainedProblemHistoriesRepository trainedProblemHistoriesRepository;
    private final TrainManager trainManager;

    /**
     * sessionId로 userId 조회
     */
    public Long getUserIdBySessionId(String sessionId) {
        TrainedStageHistories session = trainedStageHistoriesRepository.findBySessionKey(sessionId)
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + sessionId));
        return session.getUser().getId();
    }

    /**
     * Stage 시작 - 새로운 훈련 세션 생성
     * TrainManager의 generateQuestionSession()을 사용하여 sessionKey 생성
     */
    public StageStartResponse startStage(StageStartRequest request) {

        User user = userRepository.findById(request.getUserId())
                .orElseThrow(() -> new IllegalArgumentException("회원을 찾을 수 없습니다: " + request.getUserId()));

        // TrainManager에서 sessionKey 생성 (메모리에 세션도 자동 생성됨)
        String sessionKey = trainManager.generateQuestionSession();

        // 새 세션 생성
        TrainedStageHistories stage = TrainedStageHistories.builder()
                .user(user)
                .sessionKey(sessionKey)
                .stage(request.getStage())
                .problemCount(request.getTotalProblems())
                .correctCount(0)
                .wrongCount(0)
                .tryCount(0)
                .build();

        stage = trainedStageHistoriesRepository.save(stage);

        return StageStartResponse.builder()
                .sessionId(stage.getSessionKey())
                .stage(stage.getStage())
                .totalProblems(stage.getProblemCount())
                .startAt(stage.getStartedAt())
                .build();
    }

    /**
     * 문제 시도 제출 - 개별 문제 시도 기록 저장
     * 개별 시도 map 에도 저장
     */
    public AttemptResponse submitAttempt(AttemptRequest request) {
        // 세션 조회
        TrainedStageHistories stage = trainedStageHistoriesRepository.findBySessionKey(request.getSessionId())
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + request.getSessionId()));

        // 시도 기록 생성
        TrainedProblemHistories attempt = TrainedProblemHistories.builder()
                .trainedStageHistories(stage)
                .problemId(request.getProblemId())
                .phonemes(request.getPhonemes())
                .word(request.getWord())
                .selectedAnswer(request.getSelectedAnswer())
                .attemptNumber(request.getAttemptNumber())
                .isCorrect(request.getIsCorrect())
                .isReplyCorrect(request.getIsReplyCorrect())
                .audioUrl(request.getAudioUrl())
                .solvedAt(LocalDateTime.now())
                .build();

        // 문제 하나씩 저장
        attempt = trainedProblemHistoriesRepository.save(attempt);

        // 저장한 session 업데이트
        // 시도 횟수가 커진다면, 전체 try count를 올립니다.
        if (request.getAttemptNumber() > 1) stage.updateTryCount();
        if (request.getIsCorrect()) stage.updateCorrectCount();

        return AttemptResponse.builder()
                .attemptId(attempt.getId())
                .sessionId(stage.getSessionKey())
                .problemId(attempt.getProblemId())
                .phonemes(attempt.getPhonemes())
                .word(attempt.getWord())
                .selectedAnswer(attempt.getSelectedAnswer())
                .isCorrect(attempt.getIsCorrect())
                .isReplyCorrect(attempt.getIsReplyCorrect())
                .attemptNumber(attempt.getAttemptNumber())
                .audioUrl(attempt.getAudioUrl())
                .build();
    }

    /**
     * Stage 완료 - 부족한 음성 리스트 전달
     */
    public StageCompleteResponse completeStage(StageCompleteRequest request) {
        TrainedStageHistories stage = trainedStageHistoriesRepository.findBySessionKey(request.getSessionId())
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + request.getSessionId()));

        SessionInfo sessionInfo = trainManager.getProblemSession(request.getSessionId());
        stage.updateWrongCount();

        Set<String> voiceResult = sessionInfo.getQuestionAccuracy().keySet();

        return StageCompleteResponse.builder()
                .voiceResult(voiceResult)
                .build();
    }
}
