package com.readingbuddy.backend.domain.train.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;

import java.util.List;

@Getter
@Builder
@AllArgsConstructor
public class BasicLevelResponse {

    private Long questionId;
    private String value;
    private String unicode;
    private String voiceUrl;
    private String imageUrl;
    private List<PhonemesDto> options;  // 문제 선택지
}
