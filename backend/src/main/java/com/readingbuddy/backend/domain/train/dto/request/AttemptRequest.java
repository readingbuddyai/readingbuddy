package com.readingbuddy.backend.domain.train.dto.request;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
public class AttemptRequest {

    private Long sessionId;  // 훈련 세션 ID
    private Integer problemId;
    private String stage;
    private Integer tryCount;
    private String phonemes;
    private String word;
    private String reply;
    private Boolean isCorrect;
}
