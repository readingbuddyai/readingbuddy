package com.readingbuddy.backend.domain.train.dto.result;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.experimental.SuperBuilder;

import java.util.List;

@Getter
@AllArgsConstructor
@SuperBuilder
public class Stage1_1Problem extends ProblemResult {

    private final Long phonemeId;
    private final String voiceUrl;
    private final String imageUrl;
    private final List<OptionDto> options;
    private final Long kcId;  // Knowledge Component ID
    private final Integer candidateList;


    public Stage1_1Problem(String problemWord, Long phonemeId, String voiceUrl, String imageUrl, List<OptionDto> options, Long kcId, Integer candidateList, Integer candidateList1) {
        super(problemWord);
        this.phonemeId = phonemeId;
        this.voiceUrl = voiceUrl;
        this.imageUrl = imageUrl;
        this.options = options;
        this.kcId = kcId;
        this.candidateList = candidateList1;
    }

    @Getter
    @Builder
    public static class OptionDto {
        private final Long id;
        private final String value;
        private final String unicode;

        public OptionDto(Long id, String value, String unicode) {
            this.id = id;
            this.value = value;
            this.unicode = unicode;
        }
    }
}
