package com.readingbuddy.backend.domain.bkt.entity;

import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.EqualsAndHashCode;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.io.Serializable;

@Entity
@Getter
@AllArgsConstructor
@NoArgsConstructor
public class TrainProblemHistoriesKcMap {

    @EmbeddedId
    private TrainProblemHistoriesKcMapId id;

    @MapsId("trainedProblemHistoriesId")
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "trained_problem_histories_id")
    private TrainedProblemHistories trainedProblemHistories;

    @MapsId("knowledgeComponentId")
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "knowledge_component_id")
    private KnowledgeComponent knowledgeComponent;

    public TrainProblemHistoriesKcMap(TrainedProblemHistories trainedProblemHistories, KnowledgeComponent kc) {
        this.trainedProblemHistories = trainedProblemHistories;
        this.knowledgeComponent = kc;
        this.id = new TrainProblemHistoriesKcMapId(trainedProblemHistories.getId(), kc.getId());
    }

    @Embeddable
    @Getter
    @NoArgsConstructor
    @EqualsAndHashCode
    public static class TrainProblemHistoriesKcMapId implements Serializable {
        private Long trainedProblemHistoriesId;
        private Long knowledgeComponentId;

        public TrainProblemHistoriesKcMapId(Long trainedProblemHistoriesId, Long knowledgeComponentId) {
            this.trainedProblemHistoriesId = trainedProblemHistoriesId;
            this.knowledgeComponentId = knowledgeComponentId;
        }
    }
}
