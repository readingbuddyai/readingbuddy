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
public class AllKcAverageMasteryResponse {

    private Integer totalKcCount;
    private Double overallAverageMastery;
    private List<KcMasteryInfo> kcMasteries;

    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class KcMasteryInfo {
        private Long kcId;
        private String kcCategory;
        private String kcDescription;
        private String stage;
        private Float pLearn;
        private Float pTrain;
        private Float pGuess;
        private Float pSlip;
        private LocalDateTime updatedAt;
    }
}
