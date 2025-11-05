package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.result.StageSessionInfo;
import okhttp3.mockwebserver.MockResponse;
import okhttp3.mockwebserver.MockWebServer;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.mock.web.MockMultipartFile;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.web.reactive.function.client.WebClient;

import java.io.IOException;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

import static org.junit.jupiter.api.Assertions.*;

@DisplayName("TrainManager 테스트")
class TrainManagerTest {

    private MockWebServer mockWebServer;
    private TrainManager trainManager;
    private Map<String, StageSessionInfo> stageSessions;

    @BeforeEach
    void setUp() throws IOException {
        // MockWebServer 시작
        mockWebServer = new MockWebServer();
        mockWebServer.start();

        // 테스트용 WebClient 생성 (MockWebServer URL로 설정)
        WebClient webClient = WebClient.builder()
                .baseUrl(mockWebServer.url("/").toString())
                .build();

        // TrainManager 생성 및 stageSessions 필드 직접 설정
        trainManager = new TrainManager(webClient);
        stageSessions = new ConcurrentHashMap<>();
        ReflectionTestUtils.setField(trainManager, "stageSessions", stageSessions);
    }

    @AfterEach
    void tearDown() throws IOException {
        // MockWebServer 종료
        if (mockWebServer != null) {
            mockWebServer.shutdown();
        }
    }

    @Test
    @DisplayName("문제 세션 생성 성공")
    void generateQuestionSession_Success() {
        // when
        Long trainedHistoryId = 100L;
        String stageSessionId = trainManager.generateQuestionSession(trainedHistoryId);

        // then
        assertNotNull(stageSessionId);
        assertFalse(stageSessionId.isEmpty());
        
        // 세션에 저장되었는지 확인
        StageSessionInfo savedSession = stageSessions.get(stageSessionId);
        assertNotNull(savedSession);
        assertNotNull(savedSession.getIsProblemCorrect());
        assertTrue(savedSession.getIsProblemCorrect().isEmpty());
    }

    @Test
    @DisplayName("문제 세션 조회 성공")
    void getStageSession_Success() {
        // given
        String problemId = "test-problem-id";
        StageSessionInfo expectedInfo = StageSessionInfo.builder()
                .isProblemCorrect(new HashMap<>())
                .build();
        stageSessions.put(problemId, expectedInfo);

        // when
        StageSessionInfo result = trainManager.getStageSession(problemId);

        // then
        assertNotNull(result);
        assertSame(expectedInfo, result);
    }

    @Test
    @DisplayName("문제 세션 삭제 성공")
    void removeStageSession_Success() {
        // given
        String problemId = "test-problem-id";
        StageSessionInfo stageSessionInfo = StageSessionInfo.builder()
                .isProblemCorrect(new HashMap<>())
                .build();
        stageSessions.put(problemId, stageSessionInfo);

        // when
        trainManager.removeStageSession(problemId);

        // then
        assertFalse(stageSessions.containsKey(problemId));
        assertNull(stageSessions.get(problemId));
    }

    @Test
    @DisplayName("세션 생성 후 조회 - 정확도 맵 초기화 확인")
    void generateQuestionSession_InitializedAccuracyMap() {
        // when
        Long trainedHistoryId = 100L;
        String stageSessionId = trainManager.generateQuestionSession(trainedHistoryId);
        StageSessionInfo stageSessionInfo = trainManager.getStageSession(stageSessionId);

        // then
        assertNotNull(stageSessionInfo);
        assertNotNull(stageSessionInfo.getIsProblemCorrect());
        assertTrue(stageSessionInfo.getIsProblemCorrect().isEmpty());
        
        // 맵에 데이터 추가 가능 확인
        stageSessionInfo.getIsProblemCorrect().put(1, Boolean.TRUE);
        assertEquals(Boolean.TRUE, stageSessionInfo.getIsProblemCorrect().get(1));
    }

    @Test
    @DisplayName("AI 서버에 음성 전송 - 성공 응답")
    void sendVoiceToAI_Success() throws InterruptedException {
        // given
        Long trainedHistoryId = 100L;
        String stageSessionId = trainManager.generateQuestionSession(trainedHistoryId);
        String stage = "1.1.1";
        int problemId = 1;
        MockMultipartFile audioFile = new MockMultipartFile(
                "audio",
                "test.wav",
                "audio/wav",
                "test audio content".getBytes()
        );

        // MockWebServer 응답 설정
        mockWebServer.enqueue(new MockResponse()
                .setResponseCode(200)
                .setBody("{\"accuracy\": 0.95, \"isCorrect\": true}")
                .addHeader("Content-Type", "application/json"));

        // when
        trainManager.sendVoiceToAI(stageSessionId, audioFile, stage, problemId);

        // 비동기 처리 대기
        Thread.sleep(500);

        // then
        StageSessionInfo stageSessionInfo = trainManager.getStageSession(stageSessionId);
        assertNotNull(stageSessionInfo);
        assertNotNull(stageSessionInfo.getIsProblemCorrect());
        
        // AI 응답이 정확도 맵에 저장되었는지 확인
        assertTrue(stageSessionInfo.getIsProblemCorrect().containsKey(1));
        assertEquals(Boolean.TRUE, stageSessionInfo.getIsProblemCorrect().get(1));
    }

}
