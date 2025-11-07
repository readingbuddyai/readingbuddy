package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Getter;

@Getter
public class Stage3Problem extends ProblemResult {

    private final String problemVoiceUrl;
    private final Integer answerCnt;
    private final Long kcId;  // Knowledge Component ID
    private final String candidateList;  // 업데이트된 candidateList 비트마스크 (BigInteger String)

    public Stage3Problem(String problemWord, String problemVoiceUrl, Integer answerCnt) {
        super(problemWord);
        this.problemVoiceUrl = problemVoiceUrl;
        this.answerCnt = answerCnt;
        this.kcId = null;
        this.candidateList = null;
    }

    public Stage3Problem(String problemWord, String problemVoiceUrl, Integer answerCnt, Long kcId, String candidateList) {
        super(problemWord);
        this.problemVoiceUrl = problemVoiceUrl;
        this.answerCnt = answerCnt;
        this.kcId = kcId;
        this.candidateList = candidateList;
    }
}
