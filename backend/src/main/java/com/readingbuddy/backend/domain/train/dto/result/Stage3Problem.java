package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Getter;

@Getter
public class Stage3Problem extends ProblemResult {

    private final String problemVoiceUrl;
    private final Integer answerCnt;

    public Stage3Problem(String problemWord, String problemVoiceUrl, Integer answerCnt) {
        super(problemWord);
        this.problemVoiceUrl = problemVoiceUrl;
        this.answerCnt = answerCnt;
    }
}
