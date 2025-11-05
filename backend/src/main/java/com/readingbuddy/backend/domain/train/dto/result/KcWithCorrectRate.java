package com.readingbuddy.backend.domain.train.dto.result;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import lombok.AllArgsConstructor;
import lombok.Getter;

/**
 * KC와 정답률을 함께 담는 DTO
 */
@Getter
@AllArgsConstructor
public class KcWithCorrectRate {
    private KnowledgeComponent knowledgeComponent;
    private Float correctRate;
}

