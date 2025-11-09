package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.repository.TrainProblemHistoriesKcMapRepository;
import com.readingbuddy.backend.domain.bkt.service.BktService;
import com.readingbuddy.backend.domain.train.dto.request.AttemptRequest;
import com.readingbuddy.backend.domain.train.dto.response.AttemptResponse;
import com.readingbuddy.backend.domain.train.dto.response.LastPlayedStageResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageCompleteResponse;
import com.readingbuddy.backend.domain.train.dto.response.StageStartResponse;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.dto.result.Stage3Problem;
import com.readingbuddy.backend.domain.train.dto.result.StageSessionInfo;
import com.readingbuddy.backend.domain.train.repository.TrainedProblemHistoriesRepository;
import com.readingbuddy.backend.domain.train.repository.TrainedStageHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;
import com.readingbuddy.backend.domain.user.entity.TrainedStageHistories;
import com.readingbuddy.backend.domain.user.entity.User;
import com.readingbuddy.backend.domain.user.repository.UserRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDateTime;
import java.util.*;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("TrainedStageService 테스트")
class TrainedStageServiceTest {

    @Mock
    private UserRepository userRepository;

    @Mock
    private TrainedStageHistoriesRepository trainedStageHistoriesRepository;

    @Mock
    private TrainedProblemHistoriesRepository trainedProblemHistoriesRepository;

    @Mock
    private TrainManager trainManager;

    @Mock
    private TrainProblemHistoriesKcMapRepository trainProblemHistoriesKcMapRepository;

    @Mock
    private KnowledgeComponentRepository knowledgeComponentRepository;

    @Mock
    private BktService bktService;

    @InjectMocks
    private TrainedStageService trainedStageService;

    private Long testUserId;
    private User testUser;
    private TrainedStageHistories testStageHistory;
    private LocalDateTime testPlayedAt;

    @BeforeEach
    void setUp() {
        testUserId = 1L;
        testPlayedAt = LocalDateTime.of(2025, 11, 9, 14, 30, 0);

        testUser = User.builder()
                .id(testUserId)
                .email("test@example.com")
                .build();

        testStageHistory = TrainedStageHistories.builder()
                .id(1L)
                .user(testUser)
                .stage("1.1.1")
                .problemCount(5)
                .correctCount(3)
                .wrongCount(2)
                .tryCount(1)
                .startedAt(testPlayedAt)
                .build();
    }

    @Test
    @DisplayName("getLastPlayedStage - 정상 케이스: 마지막 플레이 스테이지 반환")
    void getLastPlayedStage_Success() {
        // given
        when(trainedStageHistoriesRepository.findFirstByUserIdOrderByStartedAtDesc(testUserId))
                .thenReturn(Optional.of(testStageHistory));

        // when
        LastPlayedStageResponse response = trainedStageService.getLastPlayedStage(testUserId);

        // then
        assertNotNull(response);
        assertEquals("1.1.1", response.getStage());
        assertEquals(testPlayedAt, response.getPlayedAt());

        verify(trainedStageHistoriesRepository, times(1))
                .findFirstByUserIdOrderByStartedAtDesc(testUserId);
    }

    @Test
    @DisplayName("getLastPlayedStage - 플레이 기록이 없는 경우")
    void getLastPlayedStage_NoHistory() {
        // given
        when(trainedStageHistoriesRepository.findFirstByUserIdOrderByStartedAtDesc(testUserId))
                .thenReturn(Optional.empty());

        // when
        LastPlayedStageResponse response = trainedStageService.getLastPlayedStage(testUserId);

        // then
        assertNotNull(response);
        assertEquals("마지막으로 플레이한 스테이지가 없습니다", response.getStage());
        assertNull(response.getPlayedAt());

        verify(trainedStageHistoriesRepository, times(1))
                .findFirstByUserIdOrderByStartedAtDesc(testUserId);
    }

    @Test
    @DisplayName("getLastPlayedStage - 다양한 스테이지 값 반환")
    void getLastPlayedStage_VariousStages() {
        // given
        String[] stages = {"1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4.1", "4.2"};

        for (String stage : stages) {
            TrainedStageHistories stageHistory = TrainedStageHistories.builder()
                    .id(1L)
                    .user(testUser)
                    .stage(stage)
                    .problemCount(5)
                    .correctCount(3)
                    .wrongCount(2)
                    .tryCount(1)
                    .startedAt(testPlayedAt)
                    .build();

            when(trainedStageHistoriesRepository.findFirstByUserIdOrderByStartedAtDesc(testUserId))
                    .thenReturn(Optional.of(stageHistory));

            // when
            LastPlayedStageResponse response = trainedStageService.getLastPlayedStage(testUserId);

            // then
            assertNotNull(response);
            assertEquals(stage, response.getStage());
            assertEquals(testPlayedAt, response.getPlayedAt());
        }

        verify(trainedStageHistoriesRepository, times(stages.length))
                .findFirstByUserIdOrderByStartedAtDesc(testUserId);
    }

    @Test
    @DisplayName("getLastPlayedStage - 가장 최근 스테이지만 반환")
    void getLastPlayedStage_ReturnsOnlyLatest() {
        // given
        LocalDateTime latestPlayedAt = LocalDateTime.now();
        TrainedStageHistories latestStage = TrainedStageHistories.builder()
                .id(2L)
                .user(testUser)
                .stage("4.2")
                .problemCount(5)
                .correctCount(4)
                .wrongCount(1)
                .tryCount(0)
                .startedAt(latestPlayedAt)
                .build();

        when(trainedStageHistoriesRepository.findFirstByUserIdOrderByStartedAtDesc(testUserId))
                .thenReturn(Optional.of(latestStage));

        // when
        LastPlayedStageResponse response = trainedStageService.getLastPlayedStage(testUserId);

        // then
        assertNotNull(response);
        assertEquals("4.2", response.getStage());
        assertEquals(latestPlayedAt, response.getPlayedAt());

        // 최근 것만 조회하는지 확인
        verify(trainedStageHistoriesRepository, times(1))
                .findFirstByUserIdOrderByStartedAtDesc(testUserId);
    }

    @Test
    @DisplayName("getLastPlayedStage - 존재하지 않는 사용자 ID로 조회")
    void getLastPlayedStage_NonExistentUser() {
        // given
        Long nonExistentUserId = 999L;
        when(trainedStageHistoriesRepository.findFirstByUserIdOrderByStartedAtDesc(nonExistentUserId))
                .thenReturn(Optional.empty());

        // when
        LastPlayedStageResponse response = trainedStageService.getLastPlayedStage(nonExistentUserId);

        // then
        assertNotNull(response);
        assertEquals("마지막으로 플레이한 스테이지가 없습니다", response.getStage());
        assertNull(response.getPlayedAt());

        verify(trainedStageHistoriesRepository, times(1))
                .findFirstByUserIdOrderByStartedAtDesc(nonExistentUserId);
    }

    @Test
    @DisplayName("getLastPlayedStage - startedAt이 오늘인 경우")
    void getLastPlayedStage_TodayDate() {
        // given
        LocalDateTime today = LocalDateTime.now();
        TrainedStageHistories todayStage = TrainedStageHistories.builder()
                .id(1L)
                .user(testUser)
                .stage("3")
                .problemCount(5)
                .correctCount(5)
                .wrongCount(0)
                .tryCount(0)
                .startedAt(today)
                .build();

        when(trainedStageHistoriesRepository.findFirstByUserIdOrderByStartedAtDesc(testUserId))
                .thenReturn(Optional.of(todayStage));

        // when
        LastPlayedStageResponse response = trainedStageService.getLastPlayedStage(testUserId);

        // then
        assertNotNull(response);
        assertEquals("3", response.getStage());
        assertEquals(today, response.getPlayedAt());

        verify(trainedStageHistoriesRepository, times(1))
                .findFirstByUserIdOrderByStartedAtDesc(testUserId);
    }

    @Test
    @DisplayName("getLastPlayedStage - 레포지토리 메서드가 정확히 호출되는지 검증")
    void getLastPlayedStage_VerifyRepositoryCall() {
        // given
        when(trainedStageHistoriesRepository.findFirstByUserIdOrderByStartedAtDesc(testUserId))
                .thenReturn(Optional.of(testStageHistory));

        // when
        trainedStageService.getLastPlayedStage(testUserId);

        // then
        verify(trainedStageHistoriesRepository, times(1))
                .findFirstByUserIdOrderByStartedAtDesc(testUserId);
        verify(trainedStageHistoriesRepository, never())
                .findById(anyLong());
        verify(trainedStageHistoriesRepository, never())
                .findByUserId(anyLong());
    }

    // ===== startStage 테스트 =====

    @Test
    @DisplayName("startStage - 정상 케이스: 새로운 스테이지 세션 생성")
    void startStage_Success() {
        // given
        String stage = "1.1.1";
        Integer totalProblems = 5;
        String stageSessionId = "session-123";

        when(userRepository.findById(testUserId))
                .thenReturn(Optional.of(testUser));
        when(trainedStageHistoriesRepository.save(any(TrainedStageHistories.class)))
                .thenReturn(testStageHistory);
        when(trainManager.generateQuestionSession(anyLong()))
                .thenReturn(stageSessionId);

        // when
        StageStartResponse response = trainedStageService.startStage(testUserId, stage, totalProblems);

        // then
        assertNotNull(response);
        assertEquals(stageSessionId, response.getStageSessionId());
        assertEquals(stage, response.getStage());
        assertEquals(totalProblems, response.getTotalProblems());
        assertNotNull(response.getStartAt());

        verify(userRepository, times(1)).findById(testUserId);
        verify(trainedStageHistoriesRepository, times(1)).save(any(TrainedStageHistories.class));
        verify(trainManager, times(1)).generateQuestionSession(anyLong());
    }

    @Test
    @DisplayName("startStage - 사용자가 존재하지 않는 경우 예외 발생")
    void startStage_UserNotFound() {
        // given
        String stage = "1.1.1";
        Integer totalProblems = 5;

        when(userRepository.findById(testUserId))
                .thenReturn(Optional.empty());

        // when & then
        IllegalArgumentException exception = assertThrows(
                IllegalArgumentException.class,
                () -> trainedStageService.startStage(testUserId, stage, totalProblems)
        );

        assertTrue(exception.getMessage().contains("회원을 찾을 수 없습니다"));
        verify(userRepository, times(1)).findById(testUserId);
        verify(trainedStageHistoriesRepository, never()).save(any());
    }

    @Test
    @DisplayName("startStage - 다양한 스테이지로 시작")
    void startStage_VariousStages() {
        // given
        String[] stages = {"1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4.1", "4.2"};
        Integer totalProblems = 5;

        when(userRepository.findById(testUserId))
                .thenReturn(Optional.of(testUser));
        when(trainedStageHistoriesRepository.save(any(TrainedStageHistories.class)))
                .thenReturn(testStageHistory);
        when(trainManager.generateQuestionSession(anyLong()))
                .thenReturn("session-123");

        // when & then
        for (String stage : stages) {
            StageStartResponse response = trainedStageService.startStage(testUserId, stage, totalProblems);

            assertNotNull(response);
            assertNotNull(response.getStageSessionId());
            assertNotNull(response.getStartAt());
        }

        verify(userRepository, times(stages.length)).findById(testUserId);
        verify(trainedStageHistoriesRepository, times(stages.length)).save(any(TrainedStageHistories.class));
    }

    // ===== submitAttempt 테스트 =====

    @Test
    @DisplayName("submitAttempt - 정상 케이스: 문제 시도 기록 저장")
    void submitAttempt_Success() {
        // given
        String stageSessionId = "session-123";
        AttemptRequest request = AttemptRequest.builder()
                .stageSessionId(stageSessionId)
                .problemNumber(1)
                .attemptNumber(1)
                .problem("가")
                .answer("가")
                .isCorrect(true)
                .isReplyCorrect(true)
                .audioUrl("https://s3.amazonaws.com/audio.mp3")
                .build();

        StageSessionInfo sessionInfo = StageSessionInfo.builder()
                .trainedStageHistoriesId(1L)
                .problemKcMap(new HashMap<>())
                .kcCandidateList(new HashMap<>())
                .build();

        TrainedProblemHistories savedAttempt = TrainedProblemHistories.builder()
                .id(1L)
                .trainedStageHistories(testStageHistory)
                .problemNumber(1)
                .attemptNumber(1)
                .problem("가")
                .answer("가")
                .isCorrect(true)
                .isReplyCorrect(true)
                .audioUrl("https://s3.amazonaws.com/audio.mp3")
                .candidateList("0")
                .solvedAt(LocalDateTime.now())
                .build();

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(sessionInfo);
        when(trainedStageHistoriesRepository.findById(1L))
                .thenReturn(Optional.of(testStageHistory));
        when(trainedProblemHistoriesRepository.save(any(TrainedProblemHistories.class)))
                .thenReturn(savedAttempt);

        // when
        AttemptResponse response = trainedStageService.submitAttempt(testUserId, request);

        // then
        assertNotNull(response);
        assertEquals(1L, response.getAttemptId());
        assertEquals(stageSessionId, response.getStageSessionId());
        assertEquals(1, response.getProblemNumber());
        assertEquals("가", response.getProblem());
        assertEquals("가", response.getAnswer());
        assertTrue(response.getIsCorrect());
        assertTrue(response.getIsReplyCorrect());

        verify(trainManager, times(1)).getStageSession(stageSessionId);
        verify(trainedStageHistoriesRepository, times(1)).findById(1L);
        verify(trainedProblemHistoriesRepository, times(1)).save(any(TrainedProblemHistories.class));
    }

    @Test
    @DisplayName("submitAttempt - 세션이 존재하지 않는 경우 예외 발생")
    void submitAttempt_SessionNotFound() {
        // given
        String stageSessionId = "invalid-session";
        AttemptRequest request = AttemptRequest.builder()
                .stageSessionId(stageSessionId)
                .problemNumber(1)
                .attemptNumber(1)
                .problem("가")
                .answer("가")
                .isCorrect(true)
                .isReplyCorrect(true)
                .audioUrl("https://s3.amazonaws.com/audio.mp3")
                .build();

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(null);

        // when & then
        IllegalArgumentException exception = assertThrows(
                IllegalArgumentException.class,
                () -> trainedStageService.submitAttempt(testUserId, request)
        );

        assertTrue(exception.getMessage().contains("세션을 찾을 수 없습니다"));
        verify(trainManager, times(1)).getStageSession(stageSessionId);
        verify(trainedProblemHistoriesRepository, never()).save(any());
    }

    @Test
    @DisplayName("submitAttempt - KC 정보가 있는 경우 BKT 업데이트")
    void submitAttempt_WithKC() {
        // given
        String stageSessionId = "session-123";
        Long kcId = 100L;

        AttemptRequest request = AttemptRequest.builder()
                .stageSessionId(stageSessionId)
                .problemNumber(1)
                .attemptNumber(1)
                .problem("가")
                .answer("가")
                .isCorrect(true)
                .isReplyCorrect(true)
                .audioUrl("https://s3.amazonaws.com/audio.mp3")
                .build();

        Map<Integer, Long> problemKcMap = new HashMap<>();
        problemKcMap.put(1, kcId);

        Map<Long, String> kcCandidateList = new HashMap<>();
        kcCandidateList.put(kcId, "3");

        StageSessionInfo sessionInfo = StageSessionInfo.builder()
                .trainedStageHistoriesId(1L)
                .problemKcMap(problemKcMap)
                .kcCandidateList(kcCandidateList)
                .build();

        KnowledgeComponent kc = KnowledgeComponent.builder()
                .id(kcId)
                .stage("3")
                .build();

        TrainedProblemHistories savedAttempt = TrainedProblemHistories.builder()
                .id(1L)
                .trainedStageHistories(testStageHistory)
                .problemNumber(1)
                .attemptNumber(1)
                .problem("가")
                .answer("가")
                .isCorrect(true)
                .isReplyCorrect(true)
                .audioUrl("https://s3.amazonaws.com/audio.mp3")
                .candidateList("3")
                .solvedAt(LocalDateTime.now())
                .build();

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(sessionInfo);
        when(trainedStageHistoriesRepository.findById(1L))
                .thenReturn(Optional.of(testStageHistory));
        when(trainedProblemHistoriesRepository.save(any(TrainedProblemHistories.class)))
                .thenReturn(savedAttempt);
        when(knowledgeComponentRepository.findById(kcId))
                .thenReturn(Optional.of(kc));
        when(bktService.getCorrectAnswerRate(testUserId, kcId))
                .thenReturn(0.5f);

        // when
        AttemptResponse response = trainedStageService.submitAttempt(testUserId, request);

        // then
        assertNotNull(response);
        verify(bktService, times(1)).getCorrectAnswerRate(testUserId, kcId);
        verify(bktService, times(1)).updateLearnedMastery(testUserId, kcId, true, 0.5f);
        verify(trainProblemHistoriesKcMapRepository, times(1)).save(any());
    }

    // ===== completeStage 테스트 =====

    @Test
    @DisplayName("completeStage - 정상 케이스: 스테이지 완료")
    void completeStage_Success() {
        // given
        String stageSessionId = "session-123";

        Map<Integer, Boolean> isProblemCorrect = new HashMap<>();
        isProblemCorrect.put(1, true);
        isProblemCorrect.put(2, false);
        isProblemCorrect.put(3, true);

        StageSessionInfo sessionInfo = StageSessionInfo.builder()
                .trainedStageHistoriesId(1L)
                .isProblemCorrect(isProblemCorrect)
                .build();

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(sessionInfo);
        when(trainedStageHistoriesRepository.findById(1L))
                .thenReturn(Optional.of(testStageHistory));

        // when
        StageCompleteResponse response = trainedStageService.completeStage(stageSessionId);

        // then
        assertNotNull(response);
        assertEquals(stageSessionId, response.getStageSessionId());
        assertNotNull(response.getVoiceResult());
        assertEquals(3, response.getVoiceResult().size());

        verify(trainManager, times(1)).getStageSession(stageSessionId);
        verify(trainedStageHistoriesRepository, times(1)).findById(1L);
    }

    @Test
    @DisplayName("completeStage - 세션이 존재하지 않는 경우 예외 발생")
    void completeStage_SessionNotFound() {
        // given
        String stageSessionId = "invalid-session";

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(null);

        // when & then
        IllegalArgumentException exception = assertThrows(
                IllegalArgumentException.class,
                () -> trainedStageService.completeStage(stageSessionId)
        );

        assertTrue(exception.getMessage().contains("세션을 찾을 수 없습니다"));
        verify(trainManager, times(1)).getStageSession(stageSessionId);
    }

    // ===== saveProblemInfoToSession 테스트 =====

    @Test
    @DisplayName("saveProblemInfoToSession - Stage3 문제 정보 저장")
    void saveProblemInfoToSession_Stage3() {
        // given
        String stageSessionId = "session-123";
        Long kcId = 100L;

        Stage3Problem problem = new Stage3Problem(
                "가",
                "https://voice.mp3",
                2,
                kcId,
                "3"
        );

        List<ProblemResult> problems = Arrays.asList(problem);

        StageSessionInfo sessionInfo = StageSessionInfo.builder()
                .trainedStageHistoriesId(1L)
                .build();

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(sessionInfo);

        // when
        trainedStageService.saveProblemInfoToSession(stageSessionId, problems);

        // then
        assertNotNull(sessionInfo.getProblemKcMap());
        assertNotNull(sessionInfo.getKcCandidateList());
        assertEquals(kcId, sessionInfo.getProblemKcMap().get(1));
        assertEquals("3", sessionInfo.getKcCandidateList().get(kcId));

        verify(trainManager, times(1)).getStageSession(stageSessionId);
    }

    @Test
    @DisplayName("saveProblemInfoToSession - 세션이 존재하지 않는 경우 예외 발생")
    void saveProblemInfoToSession_SessionNotFound() {
        // given
        String stageSessionId = "invalid-session";
        List<ProblemResult> problems = new ArrayList<>();

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(null);

        // when & then
        IllegalArgumentException exception = assertThrows(
                IllegalArgumentException.class,
                () -> trainedStageService.saveProblemInfoToSession(stageSessionId, problems)
        );

        assertTrue(exception.getMessage().contains("세션을 찾을 수 없습니다"));
        verify(trainManager, times(1)).getStageSession(stageSessionId);
    }

    @Test
    @DisplayName("saveProblemInfoToSession - 여러 문제 정보 저장")
    void saveProblemInfoToSession_MultipleProblems() {
        // given
        String stageSessionId = "session-123";

        Stage3Problem problem1 = new Stage3Problem("가", "https://voice1.mp3", 2, 100L, "3");
        Stage3Problem problem2 = new Stage3Problem("나", "https://voice2.mp3", 3, 101L, "5");
        Stage3Problem problem3 = new Stage3Problem("다", "https://voice3.mp3", 2, 100L, "7");

        List<ProblemResult> problems = Arrays.asList(problem1, problem2, problem3);

        StageSessionInfo sessionInfo = StageSessionInfo.builder()
                .trainedStageHistoriesId(1L)
                .build();

        when(trainManager.getStageSession(stageSessionId))
                .thenReturn(sessionInfo);

        // when
        trainedStageService.saveProblemInfoToSession(stageSessionId, problems);

        // then
        assertEquals(3, sessionInfo.getProblemKcMap().size());
        assertEquals(100L, sessionInfo.getProblemKcMap().get(1));
        assertEquals(101L, sessionInfo.getProblemKcMap().get(2));
        assertEquals(100L, sessionInfo.getProblemKcMap().get(3));

        assertEquals(2, sessionInfo.getKcCandidateList().size());
        assertEquals("7", sessionInfo.getKcCandidateList().get(100L)); // 마지막 업데이트 값
        assertEquals("5", sessionInfo.getKcCandidateList().get(101L));

        verify(trainManager, times(1)).getStageSession(stageSessionId);
    }
}
