package com.readingbuddy.backend.domain.train.dto.result;

import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

import java.util.Map;

@Getter
@Setter
@Builder
public class StageSessionInfo {
    // TODO: 현재는 <문제번호(Integer), 정답 여부(Boolean)>로 예상하여 작성함. 추후 변경
    private Map<Integer, Boolean> isProblemCorrect;
    private Long trainedStageHistoriesId;
    private Map<Long, Integer> kcCandidateList;  // KC ID -> candidateList 비트마스크
    private Map<Integer, Long> problemKcMap;      // 문제 번호 -> KC ID
}