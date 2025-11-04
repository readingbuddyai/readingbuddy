package com.readingbuddy.backend.domain.train.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "letter_kc_map")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class LetterKcMap {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne
    @JoinColumn(name = "knowledge_component_id")
    private KnowledgeComponent knowledgeComponent;

    @ManyToOne
    @JoinColumn(name = "letters_id")
    private Letters letters;
}
