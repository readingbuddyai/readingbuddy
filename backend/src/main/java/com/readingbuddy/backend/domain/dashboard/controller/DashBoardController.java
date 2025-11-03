package com.readingbuddy.backend.domain.dashboard.controller;

import com.readingbuddy.backend.auth.dto.CustomUserDetails;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageCorrectRateResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageInfoResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageTryAvgResponse;
import com.readingbuddy.backend.domain.dashboard.service.DashBoardService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/dashboard")
@RequiredArgsConstructor
public class DashBoardController {

    private final DashBoardService dashBoardService;

    /**
     * 사용자별 스테이지 통계 정보 조회 API
     * @param customUserDetails JWT로부터 추출한 사용자 정보
     * @param stage 스테이지 정보 (예: 1.1.1, 1.1.2, 1.2.1, 1.2.2, 2, 3, 4)
     * @return 해당 스테이지의 시도 횟수, 정답 횟수, 오답 횟수
     */
    @GetMapping(value = "/stage/info")
    public ResponseEntity<ApiResponse<StageInfoResponse>> stageInfo(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam String stage) {

        try {
            // JWT에서 직접 userId 가져오기
            Long userId = customUserDetails.getId();

            StageInfoResponse response = dashBoardService.getStageInfo(userId, stage);

            return ResponseEntity.status(HttpStatus.OK)
                    .body(ApiResponse.success("스테이지 정보가 조회되었습니다.", response));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("스테이지 정보 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 사용자별 스테이지 평균 시도 횟수 조회 API
     * @param customUserDetails JWT로부터 추출한 사용자 정보
     * @param stage 스테이지 정보 (예: 1.1.1, 1.1.2, 1.2.1, 1.2.2, 2, 3, 4)
     * @return 해당 스테이지의 평균 시도 횟수, 총 세션 수
     */
    @GetMapping(value = "/stage/try-avg")
    public ResponseEntity<ApiResponse<StageTryAvgResponse>> stageTryAverage(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam String stage) {

        try {
            // JWT에서 직접 userId 가져오기
            Long userId = customUserDetails.getId();

            StageTryAvgResponse response = dashBoardService.getStageTryAverage(userId, stage);

            return ResponseEntity.status(HttpStatus.OK)
                    .body(ApiResponse.success("스테이지 평균 시도 횟수가 조회되었습니다.", response));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("스테이지 평균 시도 횟수 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 사용자별 스테이지 최근 세션 정답률 조회 API
     * @param customUserDetails JWT로부터 추출한 사용자 정보
     * @param stage 스테이지 정보 (예: 1.1.1, 1.1.2, 1.2.1, 1.2.2, 2, 3, 4)
     * @return 최근 진행한 스테이지 세션의 정답률, 정답/오답 개수
     */
    @GetMapping(value = "/stage/correct-rate")
    public ResponseEntity<ApiResponse<StageCorrectRateResponse>> stageCorrectRate(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam String stage) {

        try {
            // JWT에서 직접 userId 가져오기
            Long userId = customUserDetails.getId();

            StageCorrectRateResponse response = dashBoardService.getStageCorrectRate(userId, stage);

            return ResponseEntity.status(HttpStatus.OK)
                    .body(ApiResponse.success("스테이지 정답률이 조회되었습니다.", response));
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("스테이지 정답률 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }
}
