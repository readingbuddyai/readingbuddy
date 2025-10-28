package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Getter;

import java.util.List;

public class Stage1_2Problem extends ProblemResult {

    private final Long questionId;
    private final String targetPhoneme;
    private final String voiceUrl;
    private final List<OptionDto> options;

    public Stage1_2Problem(String problemWord, Long questionId, String targetPhoneme, String voiceUrl, List<OptionDto> options) {
        super(problemWord);
        this.questionId = questionId;
        this.targetPhoneme = targetPhoneme;
        this.voiceUrl = voiceUrl;
        this.options = options;
    }

    @Getter
    public static class OptionDto {
        private final Long wordId;
        private final String word;
        private final String voiceUrl;
        private final boolean isAnswer;  // 정답 여부

        public OptionDto(Long wordId, String word, String voiceUrl, boolean isAnswer) {
            this.wordId = wordId;
            this.word = word;
            this.voiceUrl = voiceUrl;
            this.isAnswer = isAnswer;
        }
    }

}
