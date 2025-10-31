package com.readingbuddy.backend.domain.train.dto.request;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
@AllArgsConstructor
public class StageStartRequest {

    private Long userId;
    private String stage;
    private Integer totalProblems;
}
