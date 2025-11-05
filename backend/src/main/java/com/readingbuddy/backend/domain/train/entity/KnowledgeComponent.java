package com.readingbuddy.backend.domain.train.entity;

import com.readingbuddy.backend.domain.train.enums.KcCategory;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "knowledge_component")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class KnowledgeComponent {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    @Enumerated(EnumType.STRING)
    private KcCategory category;

    @Column(nullable = false)
    private String stage;
}
