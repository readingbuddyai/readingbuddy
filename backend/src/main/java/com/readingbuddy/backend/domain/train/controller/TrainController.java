package com.readingbuddy.backend.domain.train.controller;

import com.readingbuddy.backend.auth.dto.CustomUserDetails;
import com.readingbuddy.backend.common.service.S3Service;
import com.readingbuddy.backend.domain.train.dto.request.AttemptRequest;
import com.readingbuddy.backend.domain.train.dto.response.*;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.service.*;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import java.util.ArrayList;
import java.util.List;

@RestController
@RequestMapping("/api/train")
@RequiredArgsConstructor
public class TrainController {

    private final ProblemGenerateService problemGenerateService;
    private final VowelTrainService vowelTrainService;
    private final ConsonantTrainService consonantTrainService;
    private final TrainManager trainManager;
    private final TrainedStageService trainedStageService;
    private final S3Service s3Service;

    /**
     * 훈련 문제 세트 생성 API
     * @param stage 문제 단계 ( 1.1.1: 모음 기초, 1.1.2: 모음 심화, 1.2.1: 자음 기초, 1.2.2: 자음 심화, 2: 음절 개수, 3, 4: 음소 개수 )
     * @param count 문제 개수 ( 기본값: 5 )
     * @return 생성된 문제 세트
     */
    @GetMapping(value = "/set")
    public ResponseEntity<ApiResponse<?>> generateTrainSet(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam String stage,
            @RequestParam(defaultValue = "5") Integer count) {

        try {
            ProblemSetResponse problemSetResponse;
            List<ProblemResult> problems = new ArrayList<>();
            String message = "";
            Long userId = customUserDetails.getId();

            switch (stage) {
                case "1.1.1","1.1.2":
                    for (int i = 0; i < count; i++) {
                        problems.add(
                                stage.equals("1.1.1")
                                        ? vowelTrainService.getBasicProblem()
                                        : vowelTrainService.getAdvancedProblem()
                        );
                    }

                    problemSetResponse = ProblemSetResponse.builder()
                            .problems(problems)
                            .build();

                    message = stage.equals("1.1.1")
                            ? "모음 기초 단계 문제가 생성되었습니다."
                            : "모음 심화 단계 문제가 생성되었습니다.";

                    return ResponseEntity.status(HttpStatus.CREATED)
                            .body(ApiResponse.success(message, problemSetResponse));

                case "1.2.1", "1.2.2":
                    for (int i = 0; i < count; i++) {
                        problems.add(
                                stage.equals("1.2.1")
                                        ? consonantTrainService.getBasicProblem()
                                        : consonantTrainService.getAdvancedProblem()
                        );
                    }

                    problemSetResponse = ProblemSetResponse.builder()
                            .problems(problems)
                            .build();

                    message = stage.equals("1.2.1")
                            ? "자음 기초 단계 문제가 생성되었습니다."
                            : "자음 심화 단계 문제가 생성되었습니다.";

                    return ResponseEntity.status(HttpStatus.CREATED)
                            .body(ApiResponse.success(message, problemSetResponse));

                case "2":
                    problemSetResponse = ProblemSetResponse.builder()
                            .problems(problemGenerateService.extractWords(count))
                            .build();

                    return ResponseEntity.status(HttpStatus.CREATED)
                            .body(ApiResponse.success("음절 개수 세기 문제가 생성되었습니다.", problemSetResponse));
                case "3", "4":
                    problemSetResponse = ProblemSetResponse.builder()
                            .problems(problemGenerateService.extractLetters(stage, count, userId))
                            .build();

                    return ResponseEntity.status(HttpStatus.CREATED)
                            .body(ApiResponse.success("음소 개수 세기 문제가 생성되었습니다.", problemSetResponse));
                default:
                    return ResponseEntity.badRequest()
                            .body(ApiResponse.error("유효하지 않은 단계입니다. " + stage));
            }
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("문제 생성 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    @PostMapping(value = "/check/voice", consumes = MediaType.MULTIPART_FORM_DATA_VALUE)
    public ResponseEntity<ApiResponse<VoiceCheckResponse>> checkVoice(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam("audio") MultipartFile audioFile,
            @RequestParam("stageSessionId") String stageSessionId,
            @RequestParam("stage") String stage,
            @RequestParam("problemNumber") Integer problemNumber
    ) {

        try {
            // 파일 검증
            if (audioFile.isEmpty()) {
                return ResponseEntity.badRequest()
                        .body(ApiResponse.error("음성 파일이 비어있습니다."));
            }

            // JWT에서 직접 userId 가져오기
            Long userId = customUserDetails.getId();

            // S3에 업로드
            String audioUrl = s3Service.uploadAudioFile(audioFile, stageSessionId, userId, problemNumber);

            // AI 서버로 음성 전송하고 응답 받기 (동기)
            VoiceCheckResponse aiResponse = trainManager.sendVoiceToAI(stageSessionId, audioFile, stage, problemNumber);

            VoiceCheckResponse response = VoiceCheckResponse.builder()
                    .reply(aiResponse.getReply())
                    .isReplyCorrect(aiResponse.getIsReplyCorrect())
                    .accuracy(aiResponse.getAccuracy())
                    .audioUrl(audioUrl)
                    .build();

            return ResponseEntity.ok(ApiResponse.success("음성 인식이 완료되었습니다.", response));

        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("음성 처리 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 훈련 스테이지 시작
     * 새로운 훈련 세션을 생성하고 stageSessionId 반환
     */
    @PostMapping("/stage/start")
    public ResponseEntity<ApiResponse<StageStartResponse>> startStage(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam("stage") String stage,
            @RequestParam("totalProblems") Integer totalProblems) {

        try {
            // JWT에서 직접 userId 가져오기
            Long userId = customUserDetails.getId();
            StageStartResponse response = trainedStageService.startStage(userId, stage, totalProblems);
            return ResponseEntity.status(HttpStatus.CREATED)
                    .body(ApiResponse.success("스테이지가 시작되었습니다.", response));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("스테이지 시작 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 문제 시도 기록 API
     * 개별 문제의 시도 결과를 DB에 저장
     */
    @PostMapping("/attempt")
    public ResponseEntity<ApiResponse<AttemptResponse>> submitAttempt(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestBody AttemptRequest request) {

        try {
            // JWT에서 직접 userId 가져오기
            Long userId = customUserDetails.getId();
            AttemptResponse response = trainedStageService.submitAttempt(request);
            return ResponseEntity.status(HttpStatus.CREATED)
                    .body(ApiResponse.success("문제 풀이가 기록되었습니다.", response));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("문제 풀이 기록 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 스테이지 완료 API
     * 세션의 모든 시도 기록을 집계하고 통계를 업데이트
     */
    @PostMapping("/stage/complete")
    public ResponseEntity<ApiResponse<StageCompleteResponse>> completeStage(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam("stageSessionId") String stageSessionId) {

        try {
            // JWT에서 직접 userId 가져오기
            Long userId = customUserDetails.getId();

            StageCompleteResponse response = trainedStageService.completeStage(stageSessionId);
            return ResponseEntity.ok(ApiResponse.success("스테이지가 완료되었습니다.", response));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("스테이지 완료 처리 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }
}
