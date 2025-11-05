package com.readingbuddy.backend.domain.train.entity;

import com.readingbuddy.backend.domain.user.entity.User;
import jakarta.persistence.*;
import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.hibernate.annotations.CreationTimestamp;
import org.springframework.data.annotation.CreatedDate;
import org.springframework.data.annotation.LastModifiedDate;

import java.time.LocalDateTime;

@Entity
@Table(name = "user_kc_mastery")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class UserKcMastery {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne
    @JoinColumn(name = "user_id")
    private User user;

    @ManyToOne
    @JoinColumn(name = "knowledge_component")
    private KnowledgeComponent knowledgeComponent;

    // 현재 숙달 확률
    @Column(nullable = false)
    private Float p_l;

    // 학습 확률
    @Column(nullable = false)
    private Float p_t;

    // 추측 확률
    @Column(nullable = false)
    private Float p_g;

    // 실수 확률
    @Column(nullable = false)
    private Float p_s;

    @CreatedDate
    @Column(nullable = false)
    private LocalDateTime createdAt;

    @LastModifiedDate
    @Column(nullable = false)
    private LocalDateTime updatedAt;
}
