package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StageTryAvgResponse {

    private String stage;
    private Double averageTryCount;  // problem_number별 평균 시도 횟수
    private Integer totalSessions;   // 전체 세션 수
}
