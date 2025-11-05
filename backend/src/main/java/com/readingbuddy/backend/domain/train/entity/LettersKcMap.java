package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.EqualsAndHashCode;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.io.Serializable;

@Entity
@Getter
@NoArgsConstructor
@AllArgsConstructor
public class LettersKcMap {

    @EmbeddedId
    private LettersKcMapId id;

    @MapsId("lettersId")
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "letters_id")
    private Letters letters;

    @MapsId("knowledgeComponentId")
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "knowledge_component_id")
    private KnowledgeComponent knowledgeComponent;

    public LettersKcMap(Letters letters, KnowledgeComponent kc) {
        this.letters = letters;
        this.knowledgeComponent = kc;
        this.id = new LettersKcMapId(letters.getId(), kc.getId());
    }

    // 👇 엔티티 안에 복합키 클래스를 중첩으로 정의
    @Embeddable
    @Getter
    @NoArgsConstructor
    @EqualsAndHashCode
    public static class LettersKcMapId implements Serializable {
        private String lettersId;
        private Long knowledgeComponentId;

        public LettersKcMapId(String lettersId, Long knowledgeComponentId) {
            this.lettersId = lettersId;
            this.knowledgeComponentId = knowledgeComponentId;
        }
    }
}
