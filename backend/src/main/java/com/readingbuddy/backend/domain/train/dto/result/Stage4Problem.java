package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Getter;
import lombok.experimental.SuperBuilder;

import java.util.List;

@SuperBuilder
@Getter
public class Stage4Problem extends ProblemResult {

    private final String slowVoiceUrl;
    private final String voiceUrl;
    private final Integer answerCnt;
    private final List<Character> phonemes;

    public Stage4Problem(String problemWord, String slowVoiceUrl, String voiceUrl, Integer answerCnt, List<Character> phonemes) {
        super(problemWord);
        this.slowVoiceUrl = slowVoiceUrl;
        this.voiceUrl = voiceUrl;
        this.answerCnt = answerCnt;
        this.phonemes = phonemes;
    }
}
