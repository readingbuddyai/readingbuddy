package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StageInfoResponse {

    private String stage;
    private Integer totalTryCount;
    private Integer totalCorrectCount;
    private Integer totalWrongCount;
}
