package com.readingbuddy.backend.domain.train.dto.request;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
public class AttemptRequest {

    private String sessionId;  // 훈련 세션 ID
    private Integer problemId;
    private String stage;
    private Integer tryCount;
    private String phonemes;
    private String selectedAnswer;
    private String word;
    private Boolean isCorrect;
    private Boolean isReplyCorrect;
    private String audioUrl;
}
