package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.response.VoiceCheckResponse;
import com.readingbuddy.backend.domain.train.dto.result.StageSessionInfo;
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

    private final Map<String, StageSessionInfo> stageSessions = new ConcurrentHashMap<>();
    private final WebClient webClient;

    public String generateQuestionSession() {
        String stageSessionId = UUID.randomUUID().toString();

        StageSessionInfo stageSessionInfo = StageSessionInfo.builder()
                .isProblemCorrect(new HashMap<>())
                .build();

        this.stageSessions.put(stageSessionId, stageSessionInfo);

        return stageSessionId;
    }

    public StageSessionInfo getStageSession(String stageSessionId) {
        return this.stageSessions.get(stageSessionId);
    }

    public void removeStageSession(String stageSessionId) {
        this.stageSessions.remove(stageSessionId);
    }

    // TODO : Object -> Dto로 변경
    public VoiceCheckResponse sendVoiceToAI(
            String stageSessionId, MultipartFile audioFile, String stage, Integer problemNumber
    ) {
        StageSessionInfo stageSessionInfo = stageSessions.get(stageSessionId);

        // TODO: S3 저장 후 URL AI server로 전달

//        Mono<Object> response = webClient.post()
//                .uri("/judge")
//                .bodyValue(Object.class)
//                .retrieve()
//                .bodyToMono(Object.class);
//
//        response.subscribe(
//                res -> {
//                    Map<Integer, Boolean> map = stageSessionInfo.getIsProblemCorrect();
//                    // TODO: 실제 답변 저장
//                    map.put(problemNumber, Boolean.TRUE);
//                },
//                err -> {
//                    log.error("AI 서버 호출 실패: problemId={}, error={}", problemNumber, err.getMessage(), err);
//                }
//        );
        return null;
    }
}
