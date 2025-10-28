package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Getter;

@Getter
public class Stage2Problem extends ProblemResult {

    private final String wordVoiceUrl;
    private final Integer wordLength;

    public Stage2Problem(String word, String wordVoiceUrl, int length) {
        super(word);
        this.wordVoiceUrl = wordVoiceUrl;
        this.wordLength = length;
    }
}
