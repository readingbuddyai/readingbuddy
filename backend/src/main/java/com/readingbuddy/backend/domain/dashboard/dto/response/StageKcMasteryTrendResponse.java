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
public class StageKcMasteryTrendResponse {

    private String stage;
    private List<KcTrend> kcTrends;

    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class KcTrend {
        private Long kcId;
        private String kcCategory;
        private String kcDescription;
        private List<MasteryPoint> masteryTrend;
    }

    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class MasteryPoint {
        private Float pLearn;
        private Float pTrain;
        private Float pGuess;
        private Float pSlip;
        private LocalDateTime updatedAt;
    }
}
