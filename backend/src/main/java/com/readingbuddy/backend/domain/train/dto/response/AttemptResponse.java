package com.readingbuddy.backend.domain.train.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class AttemptResponse {

    private Long attemptId;
    private String sessionId;
    private Integer problemNumber;  // 문제 번호
    private Long phonemeId;  // Phonemes ID
    private String phonemes;
    private String word;
    private String selectedAnswer;
    private Boolean isCorrect;
    private Boolean isReplyCorrect;
    private Double accuracy;
    private Integer attemptNumber;
    private String audioUrl;
}
