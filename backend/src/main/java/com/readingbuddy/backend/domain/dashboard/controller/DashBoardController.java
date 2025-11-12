package com.readingbuddy.backend.domain.dashboard.controller;

import com.readingbuddy.backend.auth.dto.CustomUserDetails;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.*;
import com.amazonaws.Response;
import com.readingbuddy.backend.auth.dto.CustomUserDetails;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.dashboard.service.DashBoardService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;
import java.util.List;

@Tag(name = "대시보드", description = "대시보드 관련 API")
@Slf4j
@RestController
@RequestMapping("/api/dashboard")
@RequiredArgsConstructor
public class DashBoardController {

    private final DashBoardService dashBoardService;
    private static final DateTimeFormatter DATE_FORMATTER = DateTimeFormatter.ofPattern("yyMMdd");

    /**
     * 회원의 특정 KC의 숙련도 p_l 변화 추이 조회 API
     * @param customUserDetails JWT로부터 추출한 사용자 정보
     * @param kcId Knowledge Component ID
     * @param startDate 조회 시작 날짜 (Optional, yyMMdd 형식, 예: 250101, 기본값: 한 달 전)
     * @param endDate 조회 종료 날짜 (Optional, yyMMdd 형식, 예: 250131, 기본값: 오늘)
     * @return KC의 숙련도 변화 추이 (p_l, p_t, p_g, p_s)
     */
    @Operation(summary = "KC 숙련도 변화 추이 조회",
               description = "특정 KC의 시간별 숙련도(p_l) 변화 추이를 조회합니다. 기간을 지정하지 않으면 최근 한 달 이력을 조회합니다.")
    @GetMapping("/kc/mastery-trend")
    public ResponseEntity<ApiResponse<KcMasteryTrendResponse>> getKcMasteryTrend(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @Parameter(description = "Knowledge Component ID", required = true)
            @RequestParam Long kcId,
            @Parameter(description = "조회 시작 날짜 (yyMMdd 형식, 예: 250101, 미입력시 한 달 전)", required = false)
            @RequestParam(value = "startdate", required = false) String startDate,
            @Parameter(description = "조회 종료 날짜 (yyMMdd 형식, 예: 250131, 미입력시 오늘)", required = false)
            @RequestParam(value = "enddate", required = false) String endDate) {

        try {
            Long userId = customUserDetails.getId();

            // 날짜 파싱 및 기본값 설정
            LocalDate parsedStartDate;
            LocalDate parsedEndDate;

            // 둘 다 입력된 경우
            if (startDate != null && !startDate.isEmpty() && endDate != null && !endDate.isEmpty()) {
                parsedStartDate = parseDate(startDate);
                parsedEndDate = parseDate(endDate);

                // 날짜 유효성 검증
                if (parsedStartDate.isAfter(parsedEndDate)) {
                    return ResponseEntity.badRequest()
                            .body(ApiResponse.error("시작 날짜는 종료 날짜보다 이전이어야 합니다."));
                }
            }
            // 하나만 입력된 경우
            else if ((startDate != null && !startDate.isEmpty()) || (endDate != null && !endDate.isEmpty())) {
                return ResponseEntity.badRequest()
                        .body(ApiResponse.error("시작 날짜와 종료 날짜를 모두 입력하거나 모두 생략해주세요."));
            }
            // 둘 다 입력되지 않은 경우 기본값 설정 (최근 한 달)
            else {
                parsedEndDate = LocalDate.now();
                parsedStartDate = parsedEndDate.minusMonths(1);
            }

            KcMasteryTrendResponse response = dashBoardService.getKcMasteryTrend(userId, kcId, parsedStartDate, parsedEndDate);

            return ResponseEntity.status(HttpStatus.OK)
                    .body(ApiResponse.success("KC 숙련도 변화 추이가 조회되었습니다.", response));
        } catch (DateTimeParseException e) {
            log.error("날짜 형식 오류", e);
            return ResponseEntity.badRequest()
                    .body(ApiResponse.error("날짜 형식이 올바르지 않습니다. yyMMdd 형식으로 입력해주세요. (예: 250101)"));
        } catch (IllegalArgumentException e) {
            return ResponseEntity.status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error(e.getMessage()));
        } catch (Exception e) {
            log.error("KC 숙련도 변화 추이 조회 중 오류 발생", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("KC 숙련도 변화 추이 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 회원의 특정 stage에 속한 모든 KC의 숙련도 변화 추이 조회 API
     * @param customUserDetails JWT로부터 추출한 사용자 정보
     * @param stage 스테이지 정보 (예: 1.1.1, 1.1.2, 1.2.1, 1.2.2, 2, 3, 4)
     * @param startDate 조회 시작 날짜 (Optional, yyMMdd 형식, 예: 250101, 기본값: 한 달 전)
     * @param endDate 조회 종료 날짜 (Optional, yyMMdd 형식, 예: 250131, 기본값: 오늘)
     * @return 해당 stage의 모든 KC별 숙련도 변화 추이
     */
    @Operation(summary = "Stage별 KC 숙련도 변화 추이 조회",
               description = "특정 stage에 속한 모든 KC의 시간별 숙련도 변화 추이를 조회합니다. 기간을 지정하지 않으면 최근 한 달 이력을 조회합니다.")
    @GetMapping("/stage/kc-mastery-trend")
    public ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> getStageKcMasteryTrend(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @Parameter(description = "스테이지 정보", required = true, example = "1.1.1")
            @RequestParam String stage,
            @Parameter(description = "조회 시작 날짜 (yyMMdd 형식, 예: 250101, 미입력시 한 달 전)", required = false)
            @RequestParam(value = "startdate", required = false) String startDate,
            @Parameter(description = "조회 종료 날짜 (yyMMdd 형식, 예: 250131, 미입력시 오늘)", required = false)
            @RequestParam(value = "enddate", required = false) String endDate) {

        try {
            Long userId = customUserDetails.getId();

            // 날짜 파싱 및 기본값 설정
            LocalDate parsedStartDate;
            LocalDate parsedEndDate;

            // 둘 다 입력된 경우
            if (startDate != null && !startDate.isEmpty() && endDate != null && !endDate.isEmpty()) {
                parsedStartDate = parseDate(startDate);
                parsedEndDate = parseDate(endDate);

                // 날짜 유효성 검증
                if (parsedStartDate.isAfter(parsedEndDate)) {
                    return ResponseEntity.badRequest()
                            .body(ApiResponse.error("시작 날짜는 종료 날짜보다 이전이어야 합니다."));
                }
            }
            // 하나만 입력된 경우
            else if ((startDate != null && !startDate.isEmpty()) || (endDate != null && !endDate.isEmpty())) {
                return ResponseEntity.badRequest()
                        .body(ApiResponse.error("시작 날짜와 종료 날짜를 모두 입력하거나 모두 생략해주세요."));
            }
            // 둘 다 입력되지 않은 경우 기본값 설정 (최근 한 달)
            else {
                parsedEndDate = LocalDate.now();
                parsedStartDate = parsedEndDate.minusMonths(1);
            }

            StageKcMasteryTrendResponse response = dashBoardService.getStageKcMasteryTrend(userId, stage, parsedStartDate, parsedEndDate);

            return ResponseEntity.status(HttpStatus.OK)
                    .body(ApiResponse.success("Stage별 KC 숙련도 변화 추이가 조회되었습니다.", response));
        } catch (DateTimeParseException e) {
            log.error("날짜 형식 오류", e);
            return ResponseEntity.badRequest()
                    .body(ApiResponse.error("날짜 형식이 올바르지 않습니다. yyMMdd 형식으로 입력해주세요. (예: 250101)"));
        } catch (IllegalArgumentException e) {
            return ResponseEntity.status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error(e.getMessage()));
        } catch (Exception e) {
            log.error("Stage별 KC 숙련도 변화 추이 조회 중 오류 발생", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("Stage별 KC 숙련도 변화 추이 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 회원의 특정 stage에 대한 현재 p_l 조회 API
     * @param customUserDetails JWT로부터 추출한 사용자 정보
     * @param stage 스테이지 정보 (예: 1.1.1, 1.1.2, 1.2.1, 1.2.2, 2, 3, 4)
     * @param startDate 조회 시작 날짜 (Optional, yyMMdd 형식, 예: 250101, 기본값: 한 달 전)
     * @param endDate 조회 종료 날짜 (Optional, yyMMdd 형식, 예: 250131, 기본값: 오늘)
     * @return 해당 stage의 모든 KC별 현재 숙련도 및 평균 숙련도
     */
    @Operation(summary = "Stage별 현재 숙련도 조회",
               description = "특정 stage에 속한 모든 KC의 현재 숙련도를 조회합니다. 기간을 지정하지 않으면 최근 한 달 이력을 조회합니다.")
    @GetMapping("/stage/mastery")
    public ResponseEntity<ApiResponse<StageMasteryResponse>> getStageMastery(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @Parameter(description = "스테이지 정보", required = true, example = "1.1.1")
            @RequestParam String stage,
            @Parameter(description = "조회 시작 날짜 (yyMMdd 형식, 예: 250101, 미입력시 한 달 전)", required = false)
            @RequestParam(value = "startdate", required = false) String startDate,
            @Parameter(description = "조회 종료 날짜 (yyMMdd 형식, 예: 250131, 미입력시 오늘)", required = false)
            @RequestParam(value = "enddate", required = false) String endDate) {

        try {
            Long userId = customUserDetails.getId();

            // 날짜 파싱 및 기본값 설정
            LocalDate parsedStartDate;
            LocalDate parsedEndDate;

            // 둘 다 입력된 경우
            if (startDate != null && !startDate.isEmpty() && endDate != null && !endDate.isEmpty()) {
                parsedStartDate = parseDate(startDate);
                parsedEndDate = parseDate(endDate);

                // 날짜 유효성 검증
                if (parsedStartDate.isAfter(parsedEndDate)) {
                    return ResponseEntity.badRequest()
                            .body(ApiResponse.error("시작 날짜는 종료 날짜보다 이전이어야 합니다."));
                }
            }
            // 하나만 입력된 경우
            else if ((startDate != null && !startDate.isEmpty()) || (endDate != null && !endDate.isEmpty())) {
                return ResponseEntity.badRequest()
                        .body(ApiResponse.error("시작 날짜와 종료 날짜를 모두 입력하거나 모두 생략해주세요."));
            }
            // 둘 다 입력되지 않은 경우 기본값 설정 (최근 한 달)
            else {
                parsedEndDate = LocalDate.now();
                parsedStartDate = parsedEndDate.minusMonths(1);
            }

            StageMasteryResponse response = dashBoardService.getStageMastery(userId, stage, parsedStartDate, parsedEndDate);

            return ResponseEntity.status(HttpStatus.OK)
                    .body(ApiResponse.success("Stage 숙련도가 조회되었습니다.", response));
        } catch (DateTimeParseException e) {
            log.error("날짜 형식 오류", e);
            return ResponseEntity.badRequest()
                    .body(ApiResponse.error("날짜 형식이 올바르지 않습니다. yyMMdd 형식으로 입력해주세요. (예: 250101)"));
        } catch (IllegalArgumentException e) {
            return ResponseEntity.status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error(e.getMessage()));
        } catch (Exception e) {
            log.error("Stage 숙련도 조회 중 오류 발생", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("Stage 숙련도 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

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

    @GetMapping("/attendance")
    public ResponseEntity<ApiResponse<AttendanceResponse>> getAttendance(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam(value = "startdate", required = false) String startDate,
            @RequestParam(value = "enddate", required = false) String endDate,
            @RequestParam(value = "date", required = false) String date) {

        try {
            // JWT에서 userId 가져오기
            Long userId = customUserDetails.getId();

            // 일별 조회 (date 파라미터가 있는 경우)
            if (date != null && !date.isEmpty()) {
                LocalDate parsedDate = parseDate(date);
                AttendanceResponse response = dashBoardService.getDailyAttendance(userId, parsedDate);
                return ResponseEntity.ok(ApiResponse.success("일별 출석 현황 조회가 완료되었습니다.", response));
            }

            // 기간별 조회 (startdate, enddate 파라미터가 있는 경우)
            if (startDate != null && !startDate.isEmpty() && endDate != null && !endDate.isEmpty()) {
                LocalDate parsedStartDate = parseDate(startDate);
                LocalDate parsedEndDate = parseDate(endDate);

                // 날짜 유효성 검증
                if (parsedStartDate.isAfter(parsedEndDate)) {
                    return ResponseEntity.badRequest()
                            .body(ApiResponse.error("시작 날짜는 종료 날짜보다 이전이어야 합니다."));
                }

                AttendanceResponse response = dashBoardService.getAttendanceHistoriesByDate(userId, parsedStartDate, parsedEndDate);
                return ResponseEntity.ok(ApiResponse.success("출석 기록 조회가 완료되었습니다.", response));
            }

            // 파라미터가 없는 경우
            return ResponseEntity.badRequest()
                    .body(ApiResponse.error("조회 타입을 선택해주세요. 기간 조회: startdate & enddate, 일별 조회: date"));

        } catch (DateTimeParseException e) {
            log.error("날짜 형식 오류", e);
            return ResponseEntity.badRequest()
                    .body(ApiResponse.error("날짜 형식이 올바르지 않습니다. yyMMdd 형식으로 입력해주세요. (예: 250101)"));
        } catch (Exception e) {
            log.error("출석 기록 조회 중 오류 발생", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("출석 기록 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 모든 KC의 평균 숙련도 조회 API
     * @param customUserDetails JWT로부터 추출한 사용자 정보
     * @return 모든 KC의 현재 숙련도 및 전체 평균
     */
    @Operation(summary = "모든 KC 평균 숙련도 조회",
               description = "사용자의 모든 KC에 대한 현재 숙련도와 전체 평균을 조회합니다.")
    @GetMapping("/kc/all-mastery")
    public ResponseEntity<ApiResponse<AllKcAverageMasteryResponse>> getAllKcAverageMastery(
            @AuthenticationPrincipal CustomUserDetails customUserDetails) {

        try {
            Long userId = customUserDetails.getId();

            AllKcAverageMasteryResponse response = dashBoardService.getAllKcAverageMastery(userId);

            return ResponseEntity.status(HttpStatus.OK)
                    .body(ApiResponse.success("모든 KC의 평균 숙련도가 조회되었습니다.", response));
        } catch (Exception e) {
            log.error("모든 KC 평균 숙련도 조회 중 오류 발생", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("모든 KC 평균 숙련도 조회 중 오류가 발생했습니다: " + e.getMessage()));
        }
    }

    /**
     * 사용자별 틀린 음소 조회 (내림차순)
     */
    @GetMapping("/mistake/phonemes/rank")
    public ResponseEntity<ApiResponse<List<PhonemesWrongRankResponse>>> getWrongPhonemesRanking(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam("limit") int limit) {

        Long userId = customUserDetails.getId();

        List<PhonemesWrongRankResponse> ranking = dashBoardService.getWrongPhonemesRanking(userId, limit);
        return ResponseEntity.ok(ApiResponse.success("틀린 음소 랭킹이 조회되었습니다. ", ranking));

    }

    /**
     * 사용자별 시도 횟수가 많은 음소 조회 (내림차순)
     */
    @GetMapping("/try/phonemes/rank")
    public ResponseEntity<ApiResponse<List<PhonemesTryRankResponse>>> getTryPhonemesRanking(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam("limit") int limit) {

        Long userId = customUserDetails.getId();

        List<PhonemesTryRankResponse> ranking = dashBoardService.getTryPhonemesRanking(userId, limit);
        return ResponseEntity.ok(ApiResponse.success("시도 횟수가 많은 음소 랭킹이 조회되었습니다.", ranking));
    }

    /**
     * 특정 날짜의 훈련 기록 조회 API
     */
    @GetMapping("/practice/list")
    public ResponseEntity<ApiResponse<StageProblemListResponse>> getStageProblemListByDate(
            @AuthenticationPrincipal CustomUserDetails customUserDetails,
            @RequestParam("date") String date) {

        try {
            Long userId = customUserDetails.getId();
            LocalDate parsedDate = parseDate(date);

            StageProblemListResponse response = dashBoardService.getStageProblemListByDate(userId, parsedDate);

            return ResponseEntity.ok(ApiResponse.success("일별 훈련 기록이 조회되었습니다.", response));

        } catch (DateTimeParseException e) {
            log.error("날짜 형식 오류", e);
            return ResponseEntity.badRequest()
                    .body(ApiResponse.error("날짜 형식이 올바르지 않습니다. yyMMdd 형식으로 입력해주세요. (예: 250111)"));
        }  catch (Exception e) {
            log.error("일별 훈련 기록 조회 중 오류 발생", e);
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("일별 훈련 기록 조회 중 오류가 발생했습니다." + e.getMessage()));
        }
    }


    //== 헬퍼 메서드 ==//
    /**
     * 날짜 문자열 파싱 헬퍼 메서드
     */
    private LocalDate parseDate(String dateString) throws DateTimeParseException {
        return LocalDate.parse(dateString, DATE_FORMATTER);
    }

}
