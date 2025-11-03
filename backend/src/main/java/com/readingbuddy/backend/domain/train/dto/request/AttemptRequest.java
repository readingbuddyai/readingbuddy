package com.readingbuddy.backend.domain.train.dto.request;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
public class AttemptRequest {

    private String sessionId;  // 훈련 세션 ID
    private Integer problemNumber;  // 문제 번호 (1, 2, 3, ...)
    private String stage;
    private Integer attemptNumber;
    private String phonemes;  // 'ㅏ', 'ㄱ' 등 음소 문자
    private String selectedAnswer;
    private String word;
    private Boolean isCorrect;
    private Boolean isReplyCorrect;
    private String audioUrl;
}
