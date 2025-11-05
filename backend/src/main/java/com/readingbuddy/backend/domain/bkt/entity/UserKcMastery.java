package com.readingbuddy.backend.domain.bkt.entity;

import com.readingbuddy.backend.domain.user.entity.User;
import jakarta.persistence.*;
import lombok.*;
import org.springframework.data.annotation.CreatedDate;
import org.springframework.data.annotation.LastModifiedDate;

import java.time.LocalDateTime;

@Entity
@Table(name = "user_kc_mastery")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
@Builder(toBuilder = true)
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

    /**
     * TODO: 확률 네이밍 풀네임으로 변경
     * ex) p_l -> learnedProbability
      */
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
