package com.readingbuddy.backend.domain.train.controller;

import com.readingbuddy.backend.domain.train.dto.request.TrainResultRequest;
import com.readingbuddy.backend.domain.train.dto.response.ProblemSetResponse;
import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import com.readingbuddy.backend.domain.train.service.ConsonantTrainService;
import com.readingbuddy.backend.domain.train.service.ProblemGenerateService;
import com.readingbuddy.backend.domain.train.dto.response.BasicLevelResponse;
import com.readingbuddy.backend.domain.train.service.VowelTrainService;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
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

    /**
     * 훈련 문제 세트 생성 API
     *
     * @param stage 문제 단계 ( 1.1, 1.2, 2.1, 2.2, 3, 4 ... )
     * @param count 문제 개수 ( 기본값: 5 )
     * @return 생성된 문제 세트
     */
    @GetMapping(value = "/set")
    public ResponseEntity<ApiResponse<?>> generateTrainSet(
            @RequestParam String stage,
            @RequestParam(defaultValue = "5") Integer count) {

        try {
            ProblemSetResponse problemSetResponse;
            List<ProblemResult> problems = new ArrayList<>();
            // TODO stage 별로 문제 생성
            switch (stage) {
                case "1.1.1":
                    for (int i = 0; i < count; i++) {
                        problems.add(vowelTrainService.getBasicProblem());
                    }

                    problemSetResponse = ProblemSetResponse.builder()
                            .problems(problems)
                            .build();

                    return ResponseEntity.status(HttpStatus.CREATED)
                            .body(ApiResponse.success("모음 기초 단계 문제가 생성되었습니다.", problemSetResponse));

                case "1.1.2":
                    for (int i=0; i<count; i++) {
                        problems.add(vowelTrainService.getAdvancedProblem());
                    }

                    problemSetResponse = ProblemSetResponse.builder()
                            .problems(problems)
                            .build();

                    return ResponseEntity.status(HttpStatus.CREATED)
                            .body(ApiResponse.success("모음 심화 단계 문제가 생성되었습니다.", problemSetResponse));

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

                    String message = stage.equals("1.2.1")
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
                        .problems(problemGenerateService.extractLetters(stage, count))
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
    public ResponseEntity<ApiResponse<Void>> checkVoice(
            @RequestParam("audio") MultipartFile audioFile,
            @RequestParam("problemId") String problemId,
            @RequestParam(value = "userId", required = false) Long userId) {

        try {
            // 파일 검증
            if (audioFile.isEmpty()) {
                return ResponseEntity.badRequest()
                        .body(ApiResponse.error("음성 파일이 비어있습니다."));
            }

            // TODO: 음성 파일 처리 로직 (STT, 정답 판단 등)

            return ResponseEntity.ok(ApiResponse.success("음성 데이터를 성공적으로 받았습니다.", null));

        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("음성 처리 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    @PostMapping(value = "/result", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<ApiResponse<Void>> saveResult(@RequestBody TrainResultRequest request) {

        try {
            // 필수 필드 검증
            if (request.getUserId() == null) {
                return ResponseEntity.badRequest()
                        .body(ApiResponse.error("필수 필드가 누락되었습니다. (userId)"));
            }

            // TODO: 데이터베이스에 결과 저장 로직

            return ResponseEntity.status(HttpStatus.CREATED)
                    .body(ApiResponse.success("문제 결과가 성공적으로 저장되었습니다.", null));

        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("결과 저장 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }


}
