package com.readingbuddy.backend.domain.train.service;

import com.readingbuddy.backend.domain.train.dto.response.VoiceCheckResponse;
import com.readingbuddy.backend.domain.train.dto.result.StageSessionInfo;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.core.io.ByteArrayResource;
import org.springframework.http.MediaType;
import org.springframework.http.client.MultipartBodyBuilder;
import org.springframework.stereotype.Component;
import org.springframework.web.multipart.MultipartFile;
import org.springframework.web.reactive.function.BodyInserters;
import org.springframework.web.reactive.function.client.WebClient;
import reactor.core.publisher.Mono;

import java.io.IOException;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

@Slf4j
@Component
@RequiredArgsConstructor
public class TrainManager {

    private final Map<String, StageSessionInfo> stageSessions = new ConcurrentHashMap<>();
    private final WebClient webClient;

    public String generateQuestionSession(Long id) {
        String stageSessionId = UUID.randomUUID().toString();

        StageSessionInfo stageSessionInfo = StageSessionInfo.builder()
                .isProblemCorrect(new HashMap<>())
                .trainedStageHistoriesId(id)
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
            String stageSessionId, MultipartFile audioFile, String stage, Integer problemNumber, String target
    ) {
        StageSessionInfo stageSessionInfo = stageSessions.get(stageSessionId);

        String path = "/check";
        if (stage.matches("^1(\\.\\d+)*$")) path += "/jamo";
        else if (stage.equals("2")) path += "/word";
        else if (stage.equals("3")) path += "/syllable";
        else if (stage.equals("^4(\\.\\d+)*$")) path += "/syllable";

        MultipartBodyBuilder builder = new MultipartBodyBuilder();
        try {
            long startTime = System.currentTimeMillis();
            ByteArrayResource resource = new ByteArrayResource(audioFile.getBytes()) {
                @Override
                public String getFilename() {
                    return audioFile.getOriginalFilename();
                }
            };
            long endTime = System.currentTimeMillis();
            log.info("ByteArrayResource 변환 시간: {}ms (파일 크기: {} bytes)",
                    endTime - startTime, audioFile.getSize());

            builder.part("file", resource)
                    .contentType(MediaType.parseMediaType("audio/wav"));
            builder.part("target", target);
        } catch (IOException e) {
            log.error("오디오 파일 읽기 실패: {}", e.getMessage(), e);
            throw new RuntimeException("오디오 파일 처리 중 오류가 발생했습니다.", e);
        }

        try {
            long apiStartTime = System.currentTimeMillis();
            log.info("AI 서버 요청 시작 - path: {}, target: {}, fileName: {}", path, target, audioFile.getOriginalFilename());

            Object response = webClient.post()
                    .uri(path)
                    .body(BodyInserters.fromMultipartData(builder.build()))
                    .retrieve()
                    .onStatus(status -> status.is4xxClientError() || status.is5xxServerError(),
                            clientResponse -> clientResponse.bodyToMono(String.class)
                                    .flatMap(errorBody -> {
                                        log.error("AI 서버 에러 응답 - status: {}, body: {}", clientResponse.statusCode(), errorBody);
                                        return Mono.error(new RuntimeException("AI 서버 오류: " + errorBody));
                                    }))
                    .bodyToMono(Object.class)
                    .block();

            long apiEndTime = System.currentTimeMillis();
            log.info("AI 서버 응답 수신 완료 - 소요 시간 (네트워크 + AI 처리): {}ms", apiEndTime - apiStartTime);
            log.info("AI 서버 응답 내용: {}", response.toString());

            if (response instanceof Map) {
                Map<String, Object> responseMap = (Map<String, Object>) response;
                Boolean isCorrect = (Boolean) responseMap.get("is_correct");
                List<String> decomposed = (List<String>) responseMap.get("decomposed");

                // 세션 정보에 결과 저장
                if (stageSessionInfo != null) {
                    Map<Integer, Boolean> isProblemCorrect = stageSessionInfo.getIsProblemCorrect();
                    isProblemCorrect.put(problemNumber, isCorrect != null ? isCorrect : false);
                    log.info("문제 {}번 결과 저장: {}", problemNumber, isCorrect);
                }

                return VoiceCheckResponse.builder()
                        .isReplyCorrect(isCorrect)
                        .reply(decomposed)
                        .build();
            } else {
                log.error("예상하지 못한 응답 형식: {}", response.getClass().getName());
                return VoiceCheckResponse.builder()
                        .isReplyCorrect(false)
                        .build();
            }
        } catch (Exception e) {
            log.error("AI 서버 호출 실패: problemId={}, error={}", problemNumber, e.getMessage(), e);
            return VoiceCheckResponse.builder()
                    .isReplyCorrect(false)
                    .build();
        }
    }
}
