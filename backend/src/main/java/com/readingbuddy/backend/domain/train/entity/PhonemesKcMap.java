package com.readingbuddy.backend.domain.train.entity;

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
public class PhonemesKcMap {

    @EmbeddedId
    private PhonemesKcMap.phonemesKcMapId id;

    @MapsId("phonemesId")
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "phonemes_id")
    private Phonemes phonemes;

    @MapsId("knowledgeComponentId")
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "knowledge_component_id")
    private KnowledgeComponent knowledgeComponent;

    public PhonemesKcMap(Phonemes phonemes, KnowledgeComponent kc) {
        this.phonemes = phonemes;
        this.knowledgeComponent = kc;
        this.id = new PhonemesKcMap.phonemesKcMapId(phonemes.getId(), kc.getId());
    }

    // 👇 엔티티 안에 복합키 클래스를 중첩으로 정의
    @Embeddable
    @Getter
    @NoArgsConstructor
    @EqualsAndHashCode
    public static class phonemesKcMapId implements Serializable {
        private Long phonemesId;
        private Long knowledgeComponentId;

        public phonemesKcMapId(Long phonemesId, Long knowledgeComponentId) {
            this.phonemesId = phonemesId;
            this.knowledgeComponentId = knowledgeComponentId;
        }
    }
}
