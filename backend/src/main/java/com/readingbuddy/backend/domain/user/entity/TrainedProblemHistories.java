package com.readingbuddy.backend.domain.user.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Table(name = "trained_histories")
@NoArgsConstructor(access = lombok.AccessLevel.PROTECTED)
@AllArgsConstructor
@Builder
@Getter
public class TrainedProblemHistories {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private Integer problemId;

    @Column(nullable = true)
    private String phonemes;

    @Column(nullable = true)
    private String word;

    @Column(nullable = false)
    private Boolean isCorrect;  // 문제 정답 여부

    @Column
    private Boolean isReplyCorrect;  // 발음 정답 여부

    @Column(nullable = false)
    private Integer tryCount;

    @Column(nullable = false)
    private String selectedAnswer;

    @Column(nullable = false)
    private String reply;

    @Column(nullable = false)
    private LocalDateTime solvedAt;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "trained_stage_id")  // FK 실제 위치
    private TrainedStageHistories trainedStageHistories;
}
