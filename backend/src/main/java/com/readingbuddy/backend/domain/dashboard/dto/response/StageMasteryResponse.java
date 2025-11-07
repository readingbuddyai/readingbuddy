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
public class StageMasteryResponse {

    private String stage;
    private List<KcMastery> kcMasteries;
    private Double averageMastery;

    @Getter
    @NoArgsConstructor
    @AllArgsConstructor
    @Builder
    public static class KcMastery {
        private Long kcId;
        private String kcCategory;
        private Float pLearn;
        private Float pTrain;
        private Float pGuess;
        private Float pSlip;
        private LocalDateTime updatedAt;
    }
}
