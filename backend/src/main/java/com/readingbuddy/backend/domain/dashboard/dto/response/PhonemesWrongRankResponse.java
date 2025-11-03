package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class PhonemesWrongRankResponse {

    private Long phonemeId;
    private String value;
    private String category;
    private Long wrongCnt;
}
