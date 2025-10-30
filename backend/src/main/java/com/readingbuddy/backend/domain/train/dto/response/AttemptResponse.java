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
    private Integer problemId;
    private String phonemes;
    private String word;
    private String selectedAnswer;
    private Boolean isCorrect;
    private Boolean isReplyCorrect;
    private Double accuracy;
    private Integer tryCount;
    private String audioUrl;
}
