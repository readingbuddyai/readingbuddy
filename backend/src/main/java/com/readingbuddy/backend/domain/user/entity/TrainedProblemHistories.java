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

    @Column(nullable = false)
    private String phonemes;

    @Column(nullable = false)
    private String word;

    @Column(nullable = false)
    private Boolean isCorrect;

    @Column(nullable = false)
    private Integer tryCount;

    @Column(nullable = false)
    private String reply;

    @Column(nullable = false)
    private LocalDateTime solvedAt;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "id")  // FK 실제 위치
    private TrainedProblemHistories trainedProblemHistories;
}
