package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

import java.util.Map;

@Getter
@Setter
@Builder
public class SessionInfo {
    // 문제 번호, 정답 오답 여부
    private Map<String, Double> questionAccuracy;
}