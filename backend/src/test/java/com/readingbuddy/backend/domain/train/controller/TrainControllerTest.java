package com.readingbuddy.backend.domain.train.controller;

import com.readingbuddy.backend.auth.dto.CustomUserDetails;
import com.readingbuddy.backend.common.service.S3Service;
import com.readingbuddy.backend.common.util.format.ApiResponse;
import com.readingbuddy.backend.domain.train.dto.response.LastPlayedStageResponse;
import com.readingbuddy.backend.domain.train.service.*;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.time.LocalDateTime;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("TrainController 테스트")
class TrainControllerTest {

    @Mock
    private ProblemGenerateService problemGenerateService;

    @Mock
    private VowelTrainService vowelTrainService;

    @Mock
    private ConsonantTrainService consonantTrainService;

    @Mock
    private TrainManager trainManager;

    @Mock
    private TrainedStageService trainedStageService;

    @Mock
    private S3Service s3Service;

    @InjectMocks
    private TrainController trainController;

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
    @DisplayName("GET /api/train/last/stage - 정상 케이스: 마지막 플레이 스테이지 반환")
    void getLastPlayedStage_Success() {
        // given
        LocalDateTime playedAt = LocalDateTime.of(2025, 11, 9, 14, 30, 0);
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage("1.1.1")
                .playedAt(playedAt)
                .build();

        when(trainedStageService.getLastPlayedStage(testUserId))
                .thenReturn(response);

        // when
        ResponseEntity<ApiResponse<LastPlayedStageResponse>> result =
                trainController.getLastPlayedStage(testUserDetails);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.OK, result.getStatusCode());
        assertNotNull(result.getBody());
        assertTrue(result.getBody().isSuccess());
        assertEquals("마지막 플레이 스테이지를 조회했습니다.", result.getBody().getMessage());
        assertEquals("1.1.1", result.getBody().getData().getStage());
        assertEquals(playedAt, result.getBody().getData().getPlayedAt());

        verify(trainedStageService, times(1)).getLastPlayedStage(testUserId);
    }

    @Test
    @DisplayName("GET /api/train/last/stage - 플레이 기록이 없는 경우")
    void getLastPlayedStage_NoHistory() {
        // given
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage("마지막으로 플레이한 스테이지가 없습니다")
                .playedAt(null)
                .build();

        when(trainedStageService.getLastPlayedStage(testUserId))
                .thenReturn(response);

        // when
        ResponseEntity<ApiResponse<LastPlayedStageResponse>> result =
                trainController.getLastPlayedStage(testUserDetails);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.OK, result.getStatusCode());
        assertNotNull(result.getBody());
        assertTrue(result.getBody().isSuccess());
        assertEquals("마지막 플레이 스테이지를 조회했습니다.", result.getBody().getMessage());
        assertEquals("마지막으로 플레이한 스테이지가 없습니다", result.getBody().getData().getStage());
        assertNull(result.getBody().getData().getPlayedAt());

        verify(trainedStageService, times(1)).getLastPlayedStage(testUserId);
    }

    @Test
    @DisplayName("GET /api/train/last/stage - 다양한 스테이지 값 반환")
    void getLastPlayedStage_VariousStages() {
        // given
        String[] stages = {"1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4.1", "4.2"};

        for (String stage : stages) {
            LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                    .stage(stage)
                    .playedAt(LocalDateTime.now())
                    .build();

            when(trainedStageService.getLastPlayedStage(testUserId))
                    .thenReturn(response);

            // when
            ResponseEntity<ApiResponse<LastPlayedStageResponse>> result =
                    trainController.getLastPlayedStage(testUserDetails);

            // then
            assertNotNull(result);
            assertEquals(HttpStatus.OK, result.getStatusCode());
            assertNotNull(result.getBody());
            assertTrue(result.getBody().isSuccess());
            assertEquals(stage, result.getBody().getData().getStage());
        }

        verify(trainedStageService, times(stages.length)).getLastPlayedStage(testUserId);
    }

    @Test
    @DisplayName("GET /api/train/last/stage - 서비스에서 예외 발생 시 500 에러")
    void getLastPlayedStage_ServiceException() {
        // given
        when(trainedStageService.getLastPlayedStage(testUserId))
                .thenThrow(new RuntimeException("데이터베이스 오류"));

        // when
        ResponseEntity<ApiResponse<LastPlayedStageResponse>> result =
                trainController.getLastPlayedStage(testUserDetails);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.INTERNAL_SERVER_ERROR, result.getStatusCode());
        assertNotNull(result.getBody());
        assertFalse(result.getBody().isSuccess());
        assertEquals("마지막 플레이 스테이지 조회 중 오류가 발생했습니다: 데이터베이스 오류", result.getBody().getMessage());

        verify(trainedStageService, times(1)).getLastPlayedStage(testUserId);
    }

    @Test
    @DisplayName("GET /api/train/last/stage - 오늘 날짜로 플레이한 경우")
    void getLastPlayedStage_TodayDate() {
        // given
        LocalDateTime today = LocalDateTime.now();
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage("4.2")
                .playedAt(today)
                .build();

        when(trainedStageService.getLastPlayedStage(testUserId))
                .thenReturn(response);

        // when
        ResponseEntity<ApiResponse<LastPlayedStageResponse>> result =
                trainController.getLastPlayedStage(testUserDetails);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.OK, result.getStatusCode());
        assertNotNull(result.getBody());
        assertTrue(result.getBody().isSuccess());
        assertEquals("4.2", result.getBody().getData().getStage());
        assertEquals(today, result.getBody().getData().getPlayedAt());

        verify(trainedStageService, times(1)).getLastPlayedStage(testUserId);
    }

    @Test
    @DisplayName("GET /api/train/last/stage - 서비스 호출 횟수 검증")
    void getLastPlayedStage_VerifyServiceCall() {
        // given
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage("3")
                .playedAt(LocalDateTime.now())
                .build();

        when(trainedStageService.getLastPlayedStage(testUserId))
                .thenReturn(response);

        // when
        ResponseEntity<ApiResponse<LastPlayedStageResponse>> result =
                trainController.getLastPlayedStage(testUserDetails);

        // then
        assertNotNull(result);
        assertEquals(HttpStatus.OK, result.getStatusCode());

        verify(trainedStageService, times(1)).getLastPlayedStage(testUserId);
        verify(trainedStageService, never()).startStage(anyLong(), anyString());
        verify(trainedStageService, never()).completeStage(anyString());
    }
}
