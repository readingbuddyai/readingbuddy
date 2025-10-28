package com.readingbuddy.backend.train.controller;

import com.readingbuddy.backend.train.dto.request.TrainResultRequest;
import com.readingbuddy.backend.util.ApiResponse;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

@RestController
@RequestMapping("/api/train")
public class TrainController {

    @GetMapping(value = "/set", params = {"stage, count"})
    public ResponseEntity<ApiResponse<?>> generateTrainSet(
            @RequestParam String stage,
            @RequestParam Integer count) {

        try {
            // TODO stage 별로 문제 생성

            return ResponseEntity.status(HttpStatus.CREATED)
                    .body(ApiResponse.success("Success", null));
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
