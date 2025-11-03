package com.readingbuddy.backend.domain.train.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Set;

@Getter
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class StageCompleteResponse {

    private String sessionId;
    private Set<Integer> voiceResult;
}
