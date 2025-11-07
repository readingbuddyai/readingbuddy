package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;
import java.util.List;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class KcMasteryTrendResponse {

    private Long kcId;
    private String kcCategory;
    private String stage;
    private List<MasteryPoint> masteryTrend;

    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class MasteryPoint {
        private Float p_l;
        private Float p_t;
        private Float p_g;
        private Float p_s;
        private LocalDateTime updatedAt;
    }
}
