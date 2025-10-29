package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.result.QuestionInfo;
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

    private final Map<String, QuestionInfo> questionSession = new ConcurrentHashMap<>();
    private final WebClient webClient;

    public String generateQuestionSession() {
        String problemId = UUID.randomUUID().toString();

        QuestionInfo questionInfo = QuestionInfo.builder()
                .questionAccuracy(new HashMap<>())
                .build();

        this.questionSession.put(problemId, questionInfo);

        return problemId;
    }

    public QuestionInfo getProblemSession(String problemId) {
        return this.questionSession.get(problemId);
    }

    public void removeProblemSession(String problemId) {
        this.questionSession.remove(problemId);
    }

    // TODO : Object -> Dto로 변경
    public void sendVoiceToAI(String problemId, MultipartFile audioFile, String stage) {
        QuestionInfo questionInfo = questionSession.get(problemId);

        Mono<Object> response = webClient.post()
                .uri("/judge")
                .bodyValue(Object.class)
                .retrieve()
                .bodyToMono(Object.class);

        response.subscribe(
                res -> {
                    Map<String, Double> map =questionInfo.getQuestionAccuracy();
                    map.put(stage,Double.MAX_VALUE);
                },
                err -> {
                    log.error("AI 서버 호출 실패: problemId={}, error={}", problemId, err.getMessage(), err);
                }
        );
    }
}
