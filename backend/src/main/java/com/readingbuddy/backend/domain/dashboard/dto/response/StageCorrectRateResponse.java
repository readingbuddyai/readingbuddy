package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StageCorrectRateResponse {

    private String stage;
    private Double correctRate;
    private Integer correctCount;
    private Integer wrongCount;
    private Integer totalProblems;
    private LocalDateTime completedAt;
    private String sessionKey;
}
