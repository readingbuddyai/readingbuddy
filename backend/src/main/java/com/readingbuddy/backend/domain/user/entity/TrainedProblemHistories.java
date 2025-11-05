package com.readingbuddy.backend.domain.user.entity;

import com.readingbuddy.backend.domain.train.entity.Letters;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Table(name = "trained_problem_histories")
@NoArgsConstructor(access = lombok.AccessLevel.PROTECTED)
@AllArgsConstructor
@Builder
@Getter
public class TrainedProblemHistories {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "trained_stage_id")  // FK 실제 위치
    private TrainedStageHistories trainedStageHistories;

    @Column(nullable = false)
    private Integer problemNumber;  // 문제 번호

    @Column(nullable = false)
    private String problem;  // 문제 (음소, 음절, 단어)

    @Column(nullable = false)
    private String answer;  // 정답 (개수, 음소, 음절)

    @Column(nullable = false)
    private Boolean isCorrect;  // 문제 정답 여부

    @Column
    private Boolean isReplyCorrect;  // 발읍 정답 여부

    @Column(nullable = false)
    private Integer attemptNumber;  // 문제 시도 횟수

    @Column
    private String audioUrl;  // 응답 (개수, 음소, 음절)

    @Column(nullable = false)
    private LocalDateTime solvedAt;  // 문제 푼 시간
}
