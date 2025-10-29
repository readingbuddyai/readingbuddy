package com.readingbuddy.backend.domain.train.dto.response;

import com.readingbuddy.backend.domain.train.dto.result.ProblemResult;
import lombok.Builder;
import lombok.Getter;

import java.util.List;

@Getter
@Builder
public class ProblemSetResponse {

    private List<ProblemResult> problems;
}
