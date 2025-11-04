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
}