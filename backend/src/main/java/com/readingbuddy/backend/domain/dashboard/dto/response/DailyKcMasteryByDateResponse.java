package com.readingbuddy.backend.domain.dashboard.dto.response;

import lombok.*;

import java.time.LocalDate;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class DailyKcMasteryByDateResponse {
    private LocalDate date;
    private Double onset;  // 초성 평균
    private Double nucleus;  // 중성 평균
    private Double coda;  // 종성 평균

}
