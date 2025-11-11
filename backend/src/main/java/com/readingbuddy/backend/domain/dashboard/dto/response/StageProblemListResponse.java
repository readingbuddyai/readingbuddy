package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.List;

@Getter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class StageProblemListResponse {

    private LocalDate date;  // 조회한 날짜
    private List<SessionInfo> session;  // 해당 날짜의 세션 목록

    /**
     * Session 정보
     */
    @Getter
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class SessionInfo {
        private Long sessionId;
        private String stage;
        private LocalDateTime startedAt;
        private Integer totalCount;
        private Integer correctCount;
        private Integer wrongCount;
        private List<ProblemInfo> problems;
    }

    /**
     * 문제 정보
     */
    @Getter
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class ProblemInfo {
        private Long problemId;  // 문제 기록 ID
        private Integer problemNumber;  // 문제 번호
        private String problem;  // 문제 (음소, 음절, 단어)
        private String answer;  // 회원의 정답
        private Boolean isCorrect;  // 문제 정답 여부
        private Boolean isReplyCorrect;  // 발음 정답 여부
        private Integer attemptNumber;  // 시도 횟수
        private String audioUrl;  // 사용자 응답 오디오 URL
        private LocalDateTime solvedAt;  // 풀이 시간
    }
}
