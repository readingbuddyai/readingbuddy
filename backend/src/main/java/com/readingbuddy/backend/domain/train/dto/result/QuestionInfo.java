package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

import java.util.Map;

@Getter
@Setter
@Builder
public class QuestionInfo {
    private Map<String, Double> questionAccuracy;
}