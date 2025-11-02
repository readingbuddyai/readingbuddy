package com.readingbuddy.backend.domain.user.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;
import org.hibernate.annotations.CreationTimestamp;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;

@Entity
@Table(name = "trained_stage_histories")
@NoArgsConstructor(access = lombok.AccessLevel.PROTECTED)
@AllArgsConstructor
@Builder
@Getter
public class TrainedStageHistories {

    int TOTAL_COUNT = 5;

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(unique = true, nullable = false)
    private String sessionKey;  // UUID

    @Column(nullable = false)
    private String stage;

    @Column(nullable = false)
    private Integer problemCount;

    @Column(nullable = false)
    private Integer correctCount;

    @Column(nullable = false)
    private Integer wrongCount;
    
    // 몇번 시도했는지
    @Column(nullable = false)
    private Integer tryCount;

    @CreationTimestamp
    @Column(nullable = false, updatable = false)
    private LocalDateTime startedAt;  // 시작 시간

    @Column
    private LocalDateTime completedAt;  // 완료 시간

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id")  // FK 컬럼명
    private User user;

    //== Setter 메서드 ==//
    public void updateCompleteInfo(int correctCount, int wrongCount, int tryCount) {
        this.correctCount = correctCount;
        this.wrongCount = wrongCount;
        this.tryCount = tryCount;
        this.completedAt = LocalDateTime.now();
    }

    @Transactional
    public void updateTryCount() {
        this.tryCount = this.tryCount + 1;
    }

    @Transactional
    public void updateCorrectCount() {
        this.correctCount = this.correctCount + 1;
    }

    @Transactional
    public void updateWrongCount() {
        this.wrongCount = TOTAL_COUNT - this.correctCount;
    }
}
