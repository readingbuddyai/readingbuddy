package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.response.VoiceCheckResponse;
import com.readingbuddy.backend.domain.train.dto.result.SessionInfo;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.web.multipart.MultipartFile;
import org.springframework.web.reactive.function.client.WebClient;
import reactor.core.publisher.Mono;

import java.util.HashMap;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

@Slf4j
@Component
@RequiredArgsConstructor
public class TrainManager {

    private final Map<String, SessionInfo> questionSession = new ConcurrentHashMap<>();
    private final WebClient webClient;

    public String generateQuestionSession() {
        String problemId = UUID.randomUUID().toString();

        SessionInfo sessionInfo = SessionInfo.builder()
                .questionAccuracy(new HashMap<>())
                .build();

        this.questionSession.put(problemId, sessionInfo);

        return problemId;
    }

    public SessionInfo getProblemSession(String problemId) {
        return this.questionSession.get(problemId);
    }

    public void removeProblemSession(String problemId) {
        this.questionSession.remove(problemId);
    }

    // TODO : Object -> Dto로 변경
    public VoiceCheckResponse sendVoiceToAI(
            String sessionId, MultipartFile audioFile, String stage, String problemId
    ) {
        SessionInfo sessionInfo = questionSession.get(sessionId);

        // TODO: S3 저장 후 URL AI server로 전달

        Mono<Object> response = webClient.post()
                .uri("/judge")
                .bodyValue(Object.class)
                .retrieve()
                .bodyToMono(Object.class);

        response.subscribe(
                res -> {
                    Map<String, Double> map = sessionInfo.getQuestionAccuracy();
                    // TODO: 답변 저장
                    map.put(problemId, Double.MAX_VALUE);
                },
                err -> {
                    log.error("AI 서버 호출 실패: problemId={}, error={}", problemId, err.getMessage(), err);
                }
        );
        return null;
    }
}
