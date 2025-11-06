package com.readingbuddy.backend.domain.train.dto.result;

import com.readingbuddy.backend.domain.train.entity.Phonemes;
import lombok.*;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
@Builder
public class PhonemeWithKcIdAndCandidate {
    private Phonemes phonemes;
    private Long KcId;
    private Integer candidateList;  // 업데이트된 candidateList 비트마스크{
}
