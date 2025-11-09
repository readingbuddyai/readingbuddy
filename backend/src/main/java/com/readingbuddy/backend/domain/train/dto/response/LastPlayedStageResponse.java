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
public class LastPlayedStageResponse {

    private String stage;
    private LocalDateTime playedAt;
}
