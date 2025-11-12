package com.readingbuddy.backend.domain.dashboard.controller;

import com.readingbuddy.backend.auth.dto.CustomUserDetails;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageKcMasteryTrendResponse;
import com.readingbuddy.backend.domain.dashboard.service.DashBoardService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("DashBoardController 테스트")
class DashBoardControllerTest {

    @Mock
    private DashBoardService dashBoardService;

    @InjectMocks
    private DashBoardController dashBoardController;

    private CustomUserDetails testUserDetails;
    private Long testUserId;

    @BeforeEach
    void setUp() {
        testUserId = 1L;
        testUserDetails = new CustomUserDetails(
                String.valueOf(testUserId),
                "test@example.com",
                "testUser"
        );
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 정상 케이스: 날짜 범위 지정")
    void getStageKcMasteryTrend_Success_WithDateRange() {
        // given
        String stage = "1.1.1";
        String startDate = "250101";
        String endDate = "250131";

        StageKcMasteryTrendResponse.MasteryPoint point1 = StageKcMasteryTrendResponse.MasteryPoint.builder()
                .pLearn(0.5f)
                .pTrain(0.6f)
                .pGuess(0.2f)
                .pSlip(0.1f)
                .updatedAt(LocalDateTime.of(2025, 1, 5, 10, 0))
                .build();

        StageKcMasteryTrendResponse.MasteryPoint point2 = StageKcMasteryTrendResponse.MasteryPoint.builder()
                .pLearn(0.7f)
                .pTrain(0.8f)
                .pGuess(0.2f)
                .pSlip(0.1f)
                .updatedAt(LocalDateTime.of(2025, 1, 10, 15, 30))
                .build();

        StageKcMasteryTrendResponse.KcTrend kcTrend = StageKcMasteryTrendResponse.KcTrend.builder()
                .kcId(1L)
                .kcCategory("LABIAL_1")
                .kcDescription("양순음_1")
                .masteryTrend(Arrays.asList(point1, point2))
                .build();

        StageKcMasteryTrendResponse response = StageKcMasteryTrendResponse.builder()
                .stage(stage)
                .kcTrends(Arrays.asList(kcTrend))
                .build();

        when(dashBoardService.getStageKcMasteryTrend(
                eq(testUserId),
                eq(stage),
                eq(LocalDate.of(2025, 1, 1)),
                eq(LocalDate.of(2025, 1, 31))
        )).thenReturn(response);

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, startDate, endDate);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.OK, result.getStatusCode());
        assertNotNull(result.getBody());
        assertTrue(result.getBody().isSuccess());
        assertEquals("Stage별 KC 숙련도 변화 추이가 조회되었습니다.", result.getBody().getMessage());

        StageKcMasteryTrendResponse data = result.getBody().getData();
        assertEquals(stage, data.getStage());
        assertEquals(1, data.getKcTrends().size());

        StageKcMasteryTrendResponse.KcTrend resultKcTrend = data.getKcTrends().get(0);
        assertEquals(1L, resultKcTrend.getKcId());
        assertEquals("LABIAL_1", resultKcTrend.getKcCategory());
        assertEquals("양순음_1", resultKcTrend.getKcDescription());
        assertEquals(2, resultKcTrend.getMasteryTrend().size());

        verify(dashBoardService, times(1)).getStageKcMasteryTrend(
                eq(testUserId), eq(stage), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 정상 케이스: 날짜 미지정 (기본값 사용)")
    void getStageKcMasteryTrend_Success_WithoutDateRange() {
        // given
        String stage = "1.1.1";

        StageKcMasteryTrendResponse.KcTrend kcTrend = StageKcMasteryTrendResponse.KcTrend.builder()
                .kcId(1L)
                .kcCategory("LABIAL_1")
                .kcDescription("양순음_1")
                .masteryTrend(new ArrayList<>())
                .build();

        StageKcMasteryTrendResponse response = StageKcMasteryTrendResponse.builder()
                .stage(stage)
                .kcTrends(Arrays.asList(kcTrend))
                .build();

        when(dashBoardService.getStageKcMasteryTrend(
                eq(testUserId),
                eq(stage),
                any(LocalDate.class),
                any(LocalDate.class)
        )).thenReturn(response);

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, null, null);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.OK, result.getStatusCode());
        assertNotNull(result.getBody());
        assertTrue(result.getBody().isSuccess());
        assertEquals("Stage별 KC 숙련도 변화 추이가 조회되었습니다.", result.getBody().getMessage());
        assertEquals(stage, result.getBody().getData().getStage());

        verify(dashBoardService, times(1)).getStageKcMasteryTrend(
                eq(testUserId), eq(stage), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 여러 KC를 가진 Stage")
    void getStageKcMasteryTrend_Success_MultipleKcs() {
        // given
        String stage = "1.1.1";

        StageKcMasteryTrendResponse.KcTrend kcTrend1 = StageKcMasteryTrendResponse.KcTrend.builder()
                .kcId(1L)
                .kcCategory("LABIAL_1")
                .kcDescription("양순음_1")
                .masteryTrend(new ArrayList<>())
                .build();

        StageKcMasteryTrendResponse.KcTrend kcTrend2 = StageKcMasteryTrendResponse.KcTrend.builder()
                .kcId(2L)
                .kcCategory("VELAR_1")
                .kcDescription("연구개음_1")
                .masteryTrend(new ArrayList<>())
                .build();

        StageKcMasteryTrendResponse response = StageKcMasteryTrendResponse.builder()
                .stage(stage)
                .kcTrends(Arrays.asList(kcTrend1, kcTrend2))
                .build();

        when(dashBoardService.getStageKcMasteryTrend(
                eq(testUserId),
                eq(stage),
                any(LocalDate.class),
                any(LocalDate.class)
        )).thenReturn(response);

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, null, null);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.OK, result.getStatusCode());
        StageKcMasteryTrendResponse data = result.getBody().getData();
        assertEquals(2, data.getKcTrends().size());
        assertEquals("LABIAL_1", data.getKcTrends().get(0).getKcCategory());
        assertEquals("VELAR_1", data.getKcTrends().get(1).getKcCategory());

        verify(dashBoardService, times(1)).getStageKcMasteryTrend(
                eq(testUserId), eq(stage), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 잘못된 날짜 형식")
    void getStageKcMasteryTrend_InvalidDateFormat() {
        // given
        String stage = "1.1.1";
        String invalidStartDate = "2025-01-01"; // 잘못된 형식
        String invalidEndDate = "2025-01-31";

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, invalidStartDate, invalidEndDate);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.BAD_REQUEST, result.getStatusCode());
        assertNotNull(result.getBody());
        assertFalse(result.getBody().isSuccess());
        assertTrue(result.getBody().getMessage().contains("날짜 형식이 올바르지 않습니다"));

        verify(dashBoardService, never()).getStageKcMasteryTrend(
                anyLong(), anyString(), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 시작 날짜가 종료 날짜보다 이후")
    void getStageKcMasteryTrend_StartDateAfterEndDate() {
        // given
        String stage = "1.1.1";
        String startDate = "250131"; // 2025-01-31
        String endDate = "250101";   // 2025-01-01

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, startDate, endDate);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.BAD_REQUEST, result.getStatusCode());
        assertNotNull(result.getBody());
        assertFalse(result.getBody().isSuccess());
        assertTrue(result.getBody().getMessage().contains("시작 날짜는 종료 날짜보다 이전이어야 합니다"));

        verify(dashBoardService, never()).getStageKcMasteryTrend(
                anyLong(), anyString(), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 날짜 하나만 입력")
    void getStageKcMasteryTrend_OnlyOneDateProvided() {
        // given
        String stage = "1.1.1";
        String startDate = "250101";

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, startDate, null);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.BAD_REQUEST, result.getStatusCode());
        assertNotNull(result.getBody());
        assertFalse(result.getBody().isSuccess());
        assertTrue(result.getBody().getMessage().contains("시작 날짜와 종료 날짜를 모두 입력하거나 모두 생략해주세요"));

        verify(dashBoardService, never()).getStageKcMasteryTrend(
                anyLong(), anyString(), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 존재하지 않는 Stage")
    void getStageKcMasteryTrend_StageNotFound() {
        // given
        String stage = "9.9.9";

        when(dashBoardService.getStageKcMasteryTrend(
                eq(testUserId),
                eq(stage),
                any(LocalDate.class),
                any(LocalDate.class)
        )).thenThrow(new IllegalArgumentException("해당 stage에 대한 Knowledge Component가 존재하지 않습니다: " + stage));

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, null, null);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.BAD_REQUEST, result.getStatusCode());
        assertNotNull(result.getBody());
        assertFalse(result.getBody().isSuccess());
        assertTrue(result.getBody().getMessage().contains("해당 stage에 대한 Knowledge Component가 존재하지 않습니다"));

        verify(dashBoardService, times(1)).getStageKcMasteryTrend(
                eq(testUserId), eq(stage), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 서비스 레이어 예외 발생")
    void getStageKcMasteryTrend_ServiceException() {
        // given
        String stage = "1.1.1";

        when(dashBoardService.getStageKcMasteryTrend(
                eq(testUserId),
                eq(stage),
                any(LocalDate.class),
                any(LocalDate.class)
        )).thenThrow(new RuntimeException("데이터베이스 오류"));

        // when
        ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, null, null);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.INTERNAL_SERVER_ERROR, result.getStatusCode());
        assertNotNull(result.getBody());
        assertFalse(result.getBody().isSuccess());
        assertTrue(result.getBody().getMessage().contains("Stage별 KC 숙련도 변화 추이 조회 중 오류가 발생했습니다"));

        verify(dashBoardService, times(1)).getStageKcMasteryTrend(
                eq(testUserId), eq(stage), any(LocalDate.class), any(LocalDate.class));
    }

    @Test
    @DisplayName("GET /api/dashboard/stage/kc-mastery-trend - 다양한 Stage 값 테스트")
    void getStageKcMasteryTrend_VariousStages() {
        // given
        String[] stages = {"1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4"};

        for (String stage : stages) {
            StageKcMasteryTrendResponse response = StageKcMasteryTrendResponse.builder()
                    .stage(stage)
                    .kcTrends(new ArrayList<>())
                    .build();

            when(dashBoardService.getStageKcMasteryTrend(
                    eq(testUserId),
                    eq(stage),
                    any(LocalDate.class),
                    any(LocalDate.class)
            )).thenReturn(response);

            // when
            ResponseEntity<ApiResponse<StageKcMasteryTrendResponse>> result =
                    dashBoardController.getStageKcMasteryTrend(testUserDetails, stage, null, null);

            // then
            assertNotNull(result);
            assertEquals(HttpStatus.OK, result.getStatusCode());
            assertEquals(stage, result.getBody().getData().getStage());
        }

        verify(dashBoardService, times(stages.length)).getStageKcMasteryTrend(
                eq(testUserId), anyString(), any(LocalDate.class), any(LocalDate.class));
    }
}
