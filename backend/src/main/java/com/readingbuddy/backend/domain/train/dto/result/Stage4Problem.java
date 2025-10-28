package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Getter;

import java.util.List;

@Getter
public class Stage4Problem extends ProblemResult {

    private final String slowVoiceUrl;
    private final Integer answerCnt;
    private final List<Character> phonemes;

    public Stage4Problem(String problemWord, String slowVoiceUrl, Integer answerCnt, List<Character> phonemes) {
        super(problemWord);
        this.slowVoiceUrl = slowVoiceUrl;
        this.answerCnt = answerCnt;
        this.phonemes = phonemes;
    }
}
