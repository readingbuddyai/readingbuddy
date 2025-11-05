package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.request.AttemptRequest;
import com.readingbuddy.backend.domain.train.dto.response.AttemptResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageCompleteResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageStartResponse;
import com.readingbuddy.backend.domain.train.dto.result.StageSessionInfo;
import com.readingbuddy.backend.domain.train.repository.PhonemesRepository;
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
     * Stage 시작 - 새로운 훈련 세션 생성
     * TrainManager의 generateQuestionSession()을 사용하여 sessionKey 생성
     */
    public StageStartResponse startStage(Long userId, String stage, Integer totalProblems) {

        User user = userRepository.findById(userId)
                .orElseThrow(() -> new IllegalArgumentException("회원을 찾을 수 없습니다: " + userId));

        // 새 세션 생성
        TrainedStageHistories createStage = TrainedStageHistories.builder()
                .user(user)
                .stage(stage)
                .problemCount(totalProblems)
                .correctCount(0)
                .wrongCount(0)
                .tryCount(0)
                .build();

        createStage = trainedStageHistoriesRepository.save(createStage);

        // TrainManager에서 stageSessionId 생성 (메모리에 세션도 자동 생성됨)
        String stageSessionId = trainManager.generateQuestionSession(createStage.getId());

        return StageStartResponse.builder()
                .stageSessionId(stageSessionId)
                .stage(createStage.getStage())
                .totalProblems(createStage.getProblemCount())
                .startAt(createStage.getStartedAt())
                .build();
    }

    /**
     * 문제 시도 제출 - 개별 문제 시도 기록 저장
     * 개별 시도 map 에도 저장
     */
    public AttemptResponse submitAttempt(AttemptRequest request) {
        // 세션 조회
        String stageSessionId = request.getStageSessionId();
        StageSessionInfo stageSessionInfo = trainManager.getStageSession(stageSessionId);

        if(stageSessionInfo==null){
            throw new IllegalArgumentException("세션을 찾을 수 없습니다."+stageSessionId);
        }

        TrainedStageHistories stage = trainedStageHistoriesRepository.findById(stageSessionInfo.getTrainedStageHistoriesId())
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + stageSessionId));

        // 시도 기록 생성
        TrainedProblemHistories attempt = TrainedProblemHistories.builder()
                .trainedStageHistories(stage)
                .problemNumber(request.getProblemNumber())
                .problem(request.getProblem())
                .answer(request.getAnswer())
                .reply(request.getReply())
                .isCorrect(request.getIsCorrect())
                .isReplyCorrect(request.getIsReplyCorrect())
                .attemptNumber(request.getAttemptNumber())
                .audioUrl(request.getAudioUrl())
                .solvedAt(LocalDateTime.now())
                .build();

        attempt = trainedProblemHistoriesRepository.save(attempt);

        // 저장한 session 업데이트
        // 시도 횟수가 커진다면, 전체 try count를 올립니다.
        if (request.getAttemptNumber() > 1) stage.updateTryCount();
        if (request.getIsCorrect()) stage.updateCorrectCount();

        return AttemptResponse.builder()
                .attemptId(attempt.getId())
                .stageSessionId(stageSessionId)
                .problemNumber(attempt.getProblemNumber())
                .stage(stage.getStage())
                .problem(attempt.getProblem())
                .answer(attempt.getAnswer())
                .reply(attempt.getReply())
                .isCorrect(attempt.getIsCorrect())
                .isReplyCorrect(attempt.getIsReplyCorrect())
                .attemptNumber(attempt.getAttemptNumber())
                .audioUrl(attempt.getAudioUrl())
                .build();
    }

    /**
     * Stage 완료 - 부족한 음성 리스트 전달
     */
    public StageCompleteResponse completeStage(String stageSessionId) {
        // 세션 조회
        StageSessionInfo stageSessionInfo = trainManager.getStageSession(stageSessionId);

        if(stageSessionInfo==null){
            throw new IllegalArgumentException("세션을 찾을 수 없습니다."+stageSessionId);
        }

        TrainedStageHistories stage = trainedStageHistoriesRepository.findById(stageSessionInfo.getTrainedStageHistoriesId())
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + stageSessionId));

        stage.updateWrongCount();

        Set<Integer> voiceResult = stageSessionInfo.getIsProblemCorrect().keySet();

        return StageCompleteResponse.builder()
                .stageSessionId(stageSessionId)
                .voiceResult(voiceResult)
                .build();
    }
}
