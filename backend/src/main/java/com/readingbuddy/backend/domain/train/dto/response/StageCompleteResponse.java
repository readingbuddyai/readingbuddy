package com.readingbuddy.backend.domain.train.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StageCompleteResponse {

    private Long sessionId;
    private String stage;
    private Integer totalProblems;
    private Integer correctCount;
    private Integer wrongCount;
    private Integer turnedCount;
    private LocalDateTime completedAt;
}
