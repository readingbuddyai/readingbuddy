package com.readingbuddy.backend.domain.bkt.entity;

import com.readingbuddy.backend.domain.bkt.enums.KcCategory;
import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "knowledge_component")
@Getter
@Setter
@Builder
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
