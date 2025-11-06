package com.readingbuddy.backend.domain.train.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.experimental.SuperBuilder;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class AttemptResponse {

    private Long attemptId;
    private String stageSessionId;
    private Integer problemNumber;  // 문제 번호
    private String stage;  // 스테이지
    private String problem;  // 문제
    private String answer;  // 정답
    private String audioUrl;  // 사용자 응답
    private Boolean isCorrect;
    private Boolean isReplyCorrect;
    private Integer attemptNumber;
}
