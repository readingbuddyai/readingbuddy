package com.readingbuddy.backend.domain.train.dto.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
public class AttemptRequest {

    private String stageSessionId;
    private Integer problemNumber;  // 문제 번호
    private String stage;  // 스테이지
    private String problem;  // 문제

    @JsonProperty("answer")
    private Object answer;  // 정답

    @JsonProperty("reply")
    private Object reply;  // 사용자 응답

    private Boolean isCorrect;
    private Boolean isReplyCorrect;
    private Integer attemptNumber;
    private String audioUrl;

    public String getAnswer() {
        return answer != null ? String.valueOf(answer) : null;
    }

    public String getReply() {
        return reply != null ? String.valueOf(reply) : null;
    }
}
