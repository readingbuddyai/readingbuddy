package com.readingbuddy.backend.domain.train.dto.request;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
public class AttemptRequest {

    private String stageSessionId;  // 훈련 세션 ID
    private Integer problemNumber;
    private String stage;
    private Integer attemptNumber;
    private String phonemes;
    private String selectedAnswer;
    private String word;
    private Boolean isCorrect;
    private Boolean isReplyCorrect;
    private String audioUrl;
    private Integer candidateList;  // 업데이트된 candidateList 비트마스크
}
