package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.result.SessionInfo;
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
    private Map<String, SessionInfo> questionSession;

    @BeforeEach
    void setUp() throws IOException {
        // MockWebServer 시작
        mockWebServer = new MockWebServer();
        mockWebServer.start();

        // 테스트용 WebClient 생성 (MockWebServer URL로 설정)
        WebClient webClient = WebClient.builder()
                .baseUrl(mockWebServer.url("/").toString())
                .build();

        // TrainManager 생성 및 questionSession 필드 직접 설정
        trainManager = new TrainManager(webClient);
        questionSession = new ConcurrentHashMap<>();
        ReflectionTestUtils.setField(trainManager, "questionSession", questionSession);
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
        String problemId = trainManager.generateQuestionSession();

        // then
        assertNotNull(problemId);
        assertFalse(problemId.isEmpty());
        
        // 세션에 저장되었는지 확인
        SessionInfo savedSession = questionSession.get(problemId);
        assertNotNull(savedSession);
        assertNotNull(savedSession.getQuestionAccuracy());
        assertTrue(savedSession.getQuestionAccuracy().isEmpty());
    }

    @Test
    @DisplayName("문제 세션 조회 성공")
    void getProblemSession_Success() {
        // given
        String problemId = "test-problem-id";
        SessionInfo expectedInfo = SessionInfo.builder()
                .questionAccuracy(new HashMap<>())
                .build();
        questionSession.put(problemId, expectedInfo);

        // when
        SessionInfo result = trainManager.getProblemSession(problemId);

        // then
        assertNotNull(result);
        assertSame(expectedInfo, result);
    }

    @Test
    @DisplayName("문제 세션 삭제 성공")
    void removeProblemSession_Success() {
        // given
        String problemId = "test-problem-id";
        SessionInfo sessionInfo = SessionInfo.builder()
                .questionAccuracy(new HashMap<>())
                .build();
        questionSession.put(problemId, sessionInfo);

        // when
        trainManager.removeProblemSession(problemId);

        // then
        assertFalse(questionSession.containsKey(problemId));
        assertNull(questionSession.get(problemId));
    }

    @Test
    @DisplayName("세션 생성 후 조회 - 정확도 맵 초기화 확인")
    void generateQuestionSession_InitializedAccuracyMap() {
        // when
        String problemId = trainManager.generateQuestionSession();
        SessionInfo sessionInfo = trainManager.getProblemSession(problemId);

        // then
        assertNotNull(sessionInfo);
        assertNotNull(sessionInfo.getQuestionAccuracy());
        assertTrue(sessionInfo.getQuestionAccuracy().isEmpty());
        
        // 맵에 데이터 추가 가능 확인
        sessionInfo.getQuestionAccuracy().put("test", 0.5);
        assertEquals(0.5, sessionInfo.getQuestionAccuracy().get("test"));
    }

    @Test
    @DisplayName("AI 서버에 음성 전송 - 성공 응답")
    void sendVoiceToAI_Success() throws InterruptedException {
        // given
        String sessionId = trainManager.generateQuestionSession();
        String stage = "1.1.1";
        String problemId = "t";
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
        trainManager.sendVoiceToAI(sessionId, audioFile, stage, problemId);

        // 비동기 처리 대기
        Thread.sleep(500);

        // then
        SessionInfo sessionInfo = trainManager.getProblemSession(problemId);
        assertNotNull(sessionInfo);
        assertNotNull(sessionInfo.getQuestionAccuracy());
        
        // AI 응답이 정확도 맵에 저장되었는지 확인
        assertTrue(sessionInfo.getQuestionAccuracy().containsKey(stage));
        assertEquals(Double.MAX_VALUE, sessionInfo.getQuestionAccuracy().get(stage));
    }

}
