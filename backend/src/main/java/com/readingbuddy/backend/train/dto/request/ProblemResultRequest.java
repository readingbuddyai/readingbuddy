package com.readingbuddy.backend.train.dto.request;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class ProblemResultRequest {

    private String problemId;
    private String userAnswer;
    private Boolean isCorrect;
    private Integer score;
    private String feedback;
}
