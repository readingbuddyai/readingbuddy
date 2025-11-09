package com.readingbuddy.backend.domain.train.dto.response;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.time.LocalDateTime;

import static org.junit.jupiter.api.Assertions.*;

@DisplayName("LastPlayedStageResponse DTO 테스트")
class LastPlayedStageResponseTest {

    @Test
    @DisplayName("빌더로 생성 - 정상 케이스")
    void builder_Success() {
        // given
        String stage = "1.1.1";
        LocalDateTime playedAt = LocalDateTime.now();

        // when
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage(stage)
                .playedAt(playedAt)
                .build();

        // then
        assertNotNull(response);
        assertEquals(stage, response.getStage());
        assertEquals(playedAt, response.getPlayedAt());
    }

    @Test
    @DisplayName("빌더로 생성 - stage가 null인 경우")
    void builder_WithNullStage() {
        // given
        LocalDateTime playedAt = LocalDateTime.now();

        // when
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage(null)
                .playedAt(playedAt)
                .build();

        // then
        assertNotNull(response);
        assertNull(response.getStage());
        assertEquals(playedAt, response.getPlayedAt());
    }

    @Test
    @DisplayName("빌더로 생성 - playedAt이 null인 경우")
    void builder_WithNullPlayedAt() {
        // given
        String stage = "1.1.1";

        // when
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage(stage)
                .playedAt(null)
                .build();

        // then
        assertNotNull(response);
        assertEquals(stage, response.getStage());
        assertNull(response.getPlayedAt());
    }

    @Test
    @DisplayName("빌더로 생성 - 마지막 플레이 스테이지가 없는 경우 메시지")
    void builder_WithNoStageMessage() {
        // given
        String message = "마지막으로 플레이한 스테이지가 없습니다";

        // when
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage(message)
                .playedAt(null)
                .build();

        // then
        assertNotNull(response);
        assertEquals(message, response.getStage());
        assertNull(response.getPlayedAt());
    }

    @Test
    @DisplayName("모든 필드가 null인 경우")
    void builder_WithAllNullFields() {
        // when
        LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                .stage(null)
                .playedAt(null)
                .build();

        // then
        assertNotNull(response);
        assertNull(response.getStage());
        assertNull(response.getPlayedAt());
    }

    @Test
    @DisplayName("다양한 stage 값 테스트")
    void builder_WithVariousStageValues() {
        // given
        String[] stages = {"1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4.1", "4.2"};

        // when & then
        for (String stage : stages) {
            LastPlayedStageResponse response = LastPlayedStageResponse.builder()
                    .stage(stage)
                    .playedAt(LocalDateTime.now())
                    .build();

            assertNotNull(response);
            assertEquals(stage, response.getStage());
            assertNotNull(response.getPlayedAt());
        }
    }
}
