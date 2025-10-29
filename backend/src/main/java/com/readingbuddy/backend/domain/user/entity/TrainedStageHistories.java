package com.readingbuddy.backend.domain.user.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Table(name = "trained_words")
@NoArgsConstructor(access = lombok.AccessLevel.PROTECTED)
@AllArgsConstructor
@Builder
@Getter
public class TrainedStageHistories {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private Integer stage;

    @Column(nullable = false)
    private Integer problem_cnt;

    @Column(nullable = false)
    private Integer correct_cnt;

    @Column(nullable = false)
    private Integer wrong_cnt;

    @Column(nullable = false)
    private Integer turned_cnt;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "id")  // FK 실제 위치
    private User user;
}
