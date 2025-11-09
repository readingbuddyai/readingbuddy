package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.TrainProblemHistoriesKcMap;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.repository.TrainProblemHistoriesKcMapRepository;
import com.readingbuddy.backend.domain.bkt.service.BktService;
import com.readingbuddy.backend.domain.train.dto.request.AttemptRequest;
import com.readingbuddy.backend.domain.train.dto.response.AttemptResponse;
import com.readingbuddy.backend.domain.train.dto.response.LastPlayedStageResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageCompleteResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageStartResponse;
import com.readingbuddy.backend.domain.train.dto.result.*;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage3Problem;
import com.readingbuddy.backend.domain.train.dto.result.Stage4Problem;
import com.readingbuddy.backend.domain.train.dto.result.StageSessionInfo;
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
import java.util.HashMap;
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
    private final TrainProblemHistoriesKcMapRepository trainProblemHistoriesKcMapRepository;
    private final KnowledgeComponentRepository knowledgeComponentRepository;
    private final BktService bktService;

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
    public AttemptResponse submitAttempt(Long userId, AttemptRequest request) {
        // 세션 조회
        String stageSessionId = request.getStageSessionId();
        StageSessionInfo stageSessionInfo = trainManager.getStageSession(stageSessionId);

        if(stageSessionInfo==null){
            throw new IllegalArgumentException("세션을 찾을 수 없습니다."+stageSessionId);
        }

        TrainedStageHistories stage = trainedStageHistoriesRepository.findById(stageSessionInfo.getTrainedStageHistoriesId())
                .orElseThrow(() -> new IllegalArgumentException("세션을 찾을 수 없습니다: " + stageSessionId));

        // 세션에서 해당 문제의 KC ID와 candidateList 조회
        String candidateList = "0";
        Long kcId = null;
        if (stageSessionInfo.getProblemKcMap() != null && stageSessionInfo.getKcCandidateList() != null) {
            kcId = stageSessionInfo.getProblemKcMap().get(request.getProblemNumber());
            if (kcId != null) {
                candidateList = stageSessionInfo.getKcCandidateList().getOrDefault(kcId, "0");
            }
        }

        // 시도 기록 생성
        TrainedProblemHistories attempt = TrainedProblemHistories.builder()
                .trainedStageHistories(stage)
                .problemNumber(request.getProblemNumber())
                .attemptNumber(request.getAttemptNumber())
                .problem(request.getProblem())
                .answer(request.getAnswer())
                .isCorrect(request.getIsCorrect())
                .isReplyCorrect(request.getIsReplyCorrect())
                .audioUrl(request.getAudioUrl())
                .candidateList(candidateList)  // 세션에서 조회한 candidateList 저장
                .solvedAt(LocalDateTime.now())
                .build();

        attempt = trainedProblemHistoriesRepository.save(attempt);

        // BKT 업데이트 및 KC 매핑 저장 (isCorrect가 있을 때만)
        if (request.getIsCorrect() != null && kcId != null) {
            Float correctRate = bktService.getCorrectAnswerRate(userId, kcId);
            bktService.updateLearnedMastery(userId, kcId, request.getIsCorrect(), correctRate);

            // KC 매핑 저장 (Stage 3, 4 등 KC가 있는 경우)
            KnowledgeComponent knowledgeComponent = knowledgeComponentRepository.findById(kcId)
                    .orElseThrow(() -> new IllegalArgumentException("Knowledge Component를 찾을 수 없습니다: "));

            TrainProblemHistoriesKcMap kcMap = new TrainProblemHistoriesKcMap(attempt, knowledgeComponent);
            trainProblemHistoriesKcMapRepository.save(kcMap);
        }

        // 저장한 session 업데이트
        // 시도 횟수가 커진다면, 전체 try count를 올립니다.
        if (request.getAttemptNumber() > 1) stage.updateTryCount();
        if (Boolean.TRUE.equals(request.getIsCorrect())) stage.updateCorrectCount();

        return AttemptResponse.builder()
                .attemptId(attempt.getId())
                .stageSessionId(stageSessionId)
                .problemNumber(attempt.getProblemNumber())
                .stage(stage.getStage())
                .problem(attempt.getProblem())
                .answer(attempt.getAnswer())
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

    /**
     * 문제 생성 후 세션에 문제별 KC 정보와 candidateList 저장
     */
    public void saveProblemInfoToSession(String stageSessionId, List<ProblemResult> problems) {
        // 세션 조회
        StageSessionInfo stageSessionInfo = trainManager.getStageSession(stageSessionId);

        if (stageSessionInfo == null) {
            throw new IllegalArgumentException("세션을 찾을 수 없습니다: " + stageSessionId);
        }

        // 문제별 KC 매핑과 KC별 candidateList 저장
        if (stageSessionInfo.getProblemKcMap() == null) {
            stageSessionInfo.setProblemKcMap(new HashMap<>());
        }
        if (stageSessionInfo.getKcCandidateList() == null) {
            stageSessionInfo.setKcCandidateList(new HashMap<>());
        }

        for (int i = 0; i < problems.size(); i++) {
            ProblemResult problem = problems.get(i);
            int problemNumber = i + 1;  // 문제 번호는 1부터 시작

            if (problem instanceof Stage3Problem) {
                Stage3Problem stage3Problem = (Stage3Problem) problem;

                // 문제 번호 -> KC ID 매핑
                stageSessionInfo.getProblemKcMap().put(problemNumber, stage3Problem.getKcId());

                // KC ID -> candidateList 매핑 (업데이트된 값으로)
                stageSessionInfo.getKcCandidateList().put(stage3Problem.getKcId(), stage3Problem.getCandidateList());
            } else if (problem instanceof Stage4Problem) {
                Stage4Problem stage4Problem = (Stage4Problem) problem;

                // 문제 번호 -> KC ID 매핑
                stageSessionInfo.getProblemKcMap().put(problemNumber, stage4Problem.getKcId());

                // KC ID -> candidateList 매핑 (업데이트된 값으로)
                stageSessionInfo.getKcCandidateList().put(stage4Problem.getKcId(), stage4Problem.getCandidateList());
            }
            else if (problem instanceof Stage1_1Problem) {
                Stage1_1Problem stage1_1Problem = (Stage1_1Problem) problem;
                problemNumber = i + 1;  // 문제 번호는 1부터 시작

                // 문제 번호 -> KC ID 매핑
                stageSessionInfo.getProblemKcMap().put(problemNumber, stage1_1Problem.getKcId());

                // KC ID -> candidateList 매핑 (업데이트된 값으로)
                stageSessionInfo.getKcCandidateList().put(stage1_1Problem.getKcId(), stage1_1Problem.getCandidateList());
            }else if (problem instanceof Stage1_2Problem) {
                Stage1_2Problem stage1_2Problem = (Stage1_2Problem) problem;
                problemNumber = i + 1;  // 문제 번호는 1부터 시작

                // 문제 번호 -> KC ID 매핑
                stageSessionInfo.getProblemKcMap().put(problemNumber, stage1_2Problem.getKcId());

                // KC ID -> candidateList 매핑 (업데이트된 값으로)
                stageSessionInfo.getKcCandidateList().put(stage1_2Problem.getKcId(), stage1_2Problem.getCandidateList());
            }
        }
    }

    /**
     * 마지막으로 플레이한 스테이지 조회
     */
    public LastPlayedStageResponse getLastPlayedStage(Long userId) {
        TrainedStageHistories lastStage = trainedStageHistoriesRepository
                .findFirstByUserIdOrderByStartedAtDesc(userId)
                .orElse(null);

        if (lastStage == null) {
            return LastPlayedStageResponse.builder()
                    .stage("마지막으로 플레이한 스테이지가 없습니다")
                    .playedAt(null)
                    .build();
        }

        return LastPlayedStageResponse.builder()
                .stage(lastStage.getStage())
                .playedAt(lastStage.getStartedAt())
                .build();
    }
}
