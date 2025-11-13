package com.readingbuddy.backend.domain.dashboard.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import com.readingbuddy.backend.domain.dashboard.dto.response.StageKcMasteryTrendResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Repository;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.time.LocalTime;
import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class CalculateService {

    private final UserKcMasteryRepository userKcMasteryRepository;

    /**
     * 단일 KC의 초기값 계산
     * @param userId 사용자 ID
     * @param kcId KC ID
     * @param startDateTime 조회 시작 시점
     * @return 초기 MasteryPoint (시작 시점의 값 또는 그 이전 최신값, 없으면 0)
     */
    public StageKcMasteryTrendResponse.MasteryPoint calculateInitialMastery(Long userId, Long kcId, LocalDateTime startDateTime) {
        // 시작 날짜(day) 범위 계산
        LocalDateTime dayStart = startDateTime.toLocalDate().atStartOfDay();
        LocalDateTime dayEnd = startDateTime.toLocalDate().atTime(LocalTime.MAX);

        // 1. 시작 날짜 당일의 데이터 조회 (가장 빠른 시간의 데이터)
        Optional<UserKcMastery> sameDayValue = userKcMasteryRepository
                .findFirstByUser_IdAndKnowledgeComponent_IdAndCreatedAtBetweenOrderByCreatedAtAsc(
                        userId, kcId, dayStart, dayEnd);

        if (sameDayValue.isPresent()) {
            UserKcMastery mastery = sameDayValue.get();
            return StageKcMasteryTrendResponse.MasteryPoint.builder()
                    .pLearn(mastery.getPLearn())
                    .pTrain(mastery.getPTrain())
                    .pGuess(mastery.getPGuess())
                    .pSlip(mastery.getPSlip())
                    .updatedAt(mastery.getUpdatedAt())
                    .build();
        }

        // 2. 시작 날짜 이전의 최신값 조회
        Optional<UserKcMastery> beforeValue = userKcMasteryRepository
                .findFirstByUser_IdAndKnowledgeComponent_IdAndCreatedAtBeforeOrderByCreatedAtDesc(
                        userId, kcId, dayStart);

        if (beforeValue.isPresent()) {
            UserKcMastery mastery = beforeValue.get();
            return StageKcMasteryTrendResponse.MasteryPoint.builder()
                    .pLearn(mastery.getPLearn())
                    .pTrain(mastery.getPTrain())
                    .pGuess(mastery.getPGuess())
                    .pSlip(mastery.getPSlip())
                    .updatedAt(mastery.getUpdatedAt())
                    .build();
        }

        // 3. 값이 없으면 0으로 초기화
        return StageKcMasteryTrendResponse.MasteryPoint.builder()
                .pLearn(0.0f)
                .pTrain(0.0f)
                .pGuess(0.0f)
                .pSlip(0.0f)
                .updatedAt(null)
                .build();
    }

    /**
     * 그룹(초성/중성/종성)의 초기값 계산
     * @param userId 사용자 ID
     * @param groupKcs 그룹에 속한 KC 리스트
     * @param startDateTime 조회 시작 시점
     * @return 그룹 평균 초기 MasteryPoint
     */
    public StageKcMasteryTrendResponse.MasteryPoint calculateGroupInitialMastery(Long userId, List<KnowledgeComponent> groupKcs, LocalDateTime startDateTime) {
        List<StageKcMasteryTrendResponse.MasteryPoint> kcInitialValues = groupKcs.stream()
                .map(kc -> calculateInitialMastery(userId, kc.getId(), startDateTime))
                .collect(Collectors.toList());

        // 평균 계산
        float avgPLearn = (float) kcInitialValues.stream()
                .mapToDouble(StageKcMasteryTrendResponse.MasteryPoint::getPLearn)
                .average()
                .orElse(0.0);
        float avgPTrain = (float) kcInitialValues.stream()
                .mapToDouble(StageKcMasteryTrendResponse.MasteryPoint::getPTrain)
                .average()
                .orElse(0.0);
        float avgPGuess = (float) kcInitialValues.stream()
                .mapToDouble(StageKcMasteryTrendResponse.MasteryPoint::getPGuess)
                .average()
                .orElse(0.0);
        float avgPSlip = (float) kcInitialValues.stream()
                .mapToDouble(StageKcMasteryTrendResponse.MasteryPoint::getPSlip)
                .average()
                .orElse(0.0);

        // updatedAt은 가장 최신값 사용 (null이 아닌 것 중)
        LocalDateTime latestUpdatedAt = kcInitialValues.stream()
                .map(StageKcMasteryTrendResponse.MasteryPoint::getUpdatedAt)
                .filter(java.util.Objects::nonNull)
                .max(LocalDateTime::compareTo)
                .orElse(null);

        return StageKcMasteryTrendResponse.MasteryPoint.builder()
                .pLearn(avgPLearn)
                .pTrain(avgPTrain)
                .pGuess(avgPGuess)
                .pSlip(avgPSlip)
                .updatedAt(latestUpdatedAt)
                .build();
    }
}
