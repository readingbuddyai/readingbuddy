package com.readingbuddy.backend.domain.train.dto.result;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.experimental.SuperBuilder;

import java.util.List;

@Getter
@AllArgsConstructor
@SuperBuilder
public class Stage1_2Problem extends ProblemResult {

    private final Long phonemeId;
    private final String targetPhoneme;
    private final String imageUrl;
    private final String voiceUrl;
    private final List<OptionDto> options;
    private final Long kcId;  // Knowledge Component ID
    private final Integer candidateList;  // 업데이트된 candidateList 비트마스크

    public Stage1_2Problem(String problemWord, Long phonemeId, String targetPhoneme, String imageUrl, String voiceUrl, List<OptionDto> options, Long kcId, Integer candidateList) {
        super(problemWord);
        this.phonemeId = phonemeId;
        this.targetPhoneme = targetPhoneme;
        this.imageUrl = imageUrl;
        this.voiceUrl = voiceUrl;
        this.options = options;
        this.kcId = kcId;
        this.candidateList = candidateList;
    }

    @Getter
    @Builder
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
