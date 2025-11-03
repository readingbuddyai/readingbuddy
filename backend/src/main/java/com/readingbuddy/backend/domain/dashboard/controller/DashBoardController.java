package com.readingbuddy.backend.domain.dashboard.controller;

import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.amazonaws.Response;
import com.readingbuddy.backend.auth.dto.CustomUserDetails;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.PhonemesWrongRankResponse;
import com.readingbuddy.backend.domain.dashboard.service.DashBoardService;
import com.readingbuddy.backend.domain.dashboard.dto.response.AttendanceResponse;
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

    @GetMapping(value = "/stage/info")
    public ResponseEntity<ApiResponse<?>> stageInfo(@RequestParam String stage) {

        return ResponseEntity.status(HttpStatus.OK).body(ApiResponse.success("a"));
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



    //== 헬퍼 메서드 ==//
    /**
     * 날짜 문자열 파싱 헬퍼 메서드
     */
    private LocalDate parseDate(String dateString) throws DateTimeParseException {
        return LocalDate.parse(dateString, DATE_FORMATTER);
    }

}
