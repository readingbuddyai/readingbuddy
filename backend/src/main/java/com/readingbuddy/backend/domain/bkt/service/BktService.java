package com.readingbuddy.backend.domain.bkt.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.enums.KcCategory;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import com.readingbuddy.backend.domain.train.dto.result.KcWithCorrectRate;
import com.readingbuddy.backend.domain.bkt.entity.PhonemesKcMap;
import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.repository.PhonemesKcMapRepository;
import com.readingbuddy.backend.domain.bkt.repository.TrainProblemHistoriesKcMapRepository;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.repository.TrainedProblemHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.*;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
@Slf4j
public class BktService {

    private final UserKcMasteryRepository userKcMasteryRepository;
    private final KnowledgeComponentRepository knowledgeComponentRepository;
    private final TrainProblemHistoriesKcMapRepository trainedStageHistoriesKcMapRepository;
    private final PhonemesKcMapRepository phonemesKcMapRepository;
    private final TrainedProblemHistoriesRepository trainedProblemHistoriesRepository;
    /**
     * TODO: 유저와 stage 가 들어오면 해당 stage에 대한 kc들의 숙련도 출력 (부족한 부분까지 sorting) 해서 주기
     */

    /**
     * TODO: 유저, kc가 들어오면 해당 kc에 대한 정답률 반환
     */
    public Float getCorrectAnswerRate(Long userId, Long kcId) {
        UserKcMastery userKcMastery = userKcMasteryRepository.findByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(userId, kcId);

        /**
         * 정답을 맞출 확률 = 이미 알고 있을 확률  * 실수 하지 않을 확룰 + 모를 확률 * 찍어서 맞출 확률
          */
        return userKcMastery.getP_l() * (1 - userKcMastery.getP_s()) + (1 - userKcMastery.getP_l()) * (userKcMastery.getP_g());
    }


    /**
     * TODO: 유저, stage와 문제의 합불이 들어오면 해당 문제에 해당 하는 kc에 대한 숙련도 update
     */
    public void updateLearnedMastery(Long userId, Long kcId, Boolean isCorrect, Float correctRate) {
        UserKcMastery userKcMastery = userKcMasteryRepository.findByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(userId, kcId);

        float conditionalProbability = 0F;
        if (isCorrect) {
            conditionalProbability = (userKcMastery.getP_l() * (1 - userKcMastery.getP_s())) / correctRate;
        }
        else {
            conditionalProbability = (userKcMastery.getP_l() * (userKcMastery.getP_s())) / (1 - correctRate);
        }

        Float updatedLearnedMastery = conditionalProbability + (1 - conditionalProbability) * userKcMastery.getP_t();

        UserKcMastery updatedKcMastery = userKcMastery.toBuilder()
                .id(null)
                .p_l(updatedLearnedMastery)
                .build();

        userKcMasteryRepository.save(updatedKcMastery);
    }

    /**
     * userId, stage에서 정답률이 낮은 KC 순으로 반환
     */
    public List<KcWithCorrectRate> getLowestCorrectRateKcsByStage(Long userId, String stage) {
        // stage에 속하는 모든 KC 조회
        List<KnowledgeComponent> kcs = knowledgeComponentRepository.findByStage(stage);

        // 각 KC에 대한 정답률 계산 및 DTO 생성
        List<KcWithCorrectRate> kcWithRates = new ArrayList<>();
        for (KnowledgeComponent kc : kcs) {
            try {
                Float correctRate = getCorrectAnswerRate(userId, kc.getId());
                kcWithRates.add(new KcWithCorrectRate(kc, correctRate));
            } catch (Exception e) {
                // UserKcMastery가 없는 경우, 정답률을 0으로 간주 (가장 낮은 우선순위)
                kcWithRates.add(new KcWithCorrectRate(kc, 0.0f));
            }
        }

        // 정답률이 낮은 순으로 정렬
        return kcWithRates.stream()
                .sorted(Comparator.comparing(KcWithCorrectRate::getCorrectRate))
                .collect(Collectors.toList());
    }

    /**
     * TODO: stage에서 개발하는 kc 출력
     */

    /**
     * TODO: 유저와 stage 가 들어오면 해당 stage에 대한 kc들의 숙련도가 충분한지 출력
     */
    public KnowledgeComponent selectKnowledgeComponent(Long userId, String stage) {
        // 1. Stage에 해당하는 모든 KnowledgeComponent 조회
        List<KnowledgeComponent> stageKcs = knowledgeComponentRepository.findByStage(stage);
        log.info("Stage {} KnowledgeComponents 개수: {}", stage, stageKcs.size());

        // 2. 각 KC에 대한 정답률 계산 (BKT 기반)
        Map<Long, Float> kcCorrectRateMap = new HashMap<>();
        for (KnowledgeComponent kc : stageKcs) {
            try {
                Float correctRate = getCorrectAnswerRate(userId, kc.getId());
                kcCorrectRateMap.put(kc.getId(), correctRate);
                log.info("KC ID: {}, 정답률: {}", kc.getId(), correctRate);
            } catch (Exception e) {
                // mastery가 없는 경우 기본값 0.0 사용
                kcCorrectRateMap.put(kc.getId(), 0.0f);
                log.info("KC ID: {} - mastery 없음, 기본값 0.0 사용", kc.getId());
            }
        }

        // 3. 정답률이 가장 높은 KC 찾기
        Long highestKcId = kcCorrectRateMap.entrySet().stream()
                .max(Map.Entry.comparingByValue())
                .map(Map.Entry::getKey)
                .orElse(null);

        if (highestKcId != null) {
            log.info("정답률이 가장 높은 KC ID: {}, 정답률: {}", highestKcId, kcCorrectRateMap.get(highestKcId));
        }

        // 4. 정답률이 가장 높은 KC를 제외한 후보 KC 목록 생성
        List<KnowledgeComponent> candidateKcs = stageKcs.stream()
                .filter(kc -> highestKcId == null || !kc.getId().equals(highestKcId))
                .collect(Collectors.toList());

        log.info("정답률이 가장 높은 KC 제외 후 후보 KC 개수: {}", candidateKcs.size());

        // 5. 후보 KC 중 랜덤으로 하나 선택
        if (candidateKcs.isEmpty()) {
            candidateKcs = stageKcs;
        }

        return candidateKcs.get(new Random().nextInt(candidateKcs.size()));
    }

    public Phonemes selectPhonemeUsingBitMask(KnowledgeComponent selectedKc, Long userId, String stage) {
        // 1. 선택된 KC에 해당하는 모든 Phonemes 조회
        List<Phonemes> kcPhonemes = phonemesKcMapRepository.findByKnowledgeComponent_Id(selectedKc.getId())
                .stream()
                .map(PhonemesKcMap::getPhonemes)
                .toList();
        log.info("선택된 KC에 속한 Phonemes 개수: {}", kcPhonemes.size());

        // 2. 해당 user의 해당 stage에 대한 최신 문제 이력 조회 (candidateList)
        Optional<TrainedProblemHistories> latestProblemHistory =
                trainedProblemHistoriesRepository.findFirstByTrainedStageHistories_User_IdAndTrainedStageHistories_StageOrderBySolvedAtDesc(userId, stage);

        // 3. 문제 이력이 없으면 처음 문제를 푸는 것이므로 랜덤 선택
        if (latestProblemHistory.isEmpty()) {
            log.info("문제 이력이 없어 랜덤 선택");
            return kcPhonemes.get(new Random().nextInt(kcPhonemes.size()));
        }

        // 4. 최신 candidateList 가져오기
        Integer candidateList = latestProblemHistory.get().getCandidateList();
        if (candidateList == null) {
            log.info("candidateList가 null이어서 랜덤 선택");
            return kcPhonemes.get(new Random().nextInt(kcPhonemes.size()));
        }

        log.info("candidateList 비트마스크: {} (binary: {})", candidateList, Integer.toBinaryString(candidateList));

        // 5. candidateList에서 0인 비트(아직 출제되지 않은 문제) 찾기
        List<Phonemes> availablePhonemes = new ArrayList<>();
        for (int i = 0; i < kcPhonemes.size(); i++) {
            // i번째 비트가 0이면 아직 출제되지 않은 문제
            if ((candidateList & (1 << i)) == 0) {
                availablePhonemes.add(kcPhonemes.get(i));
                log.info("비트 {} (Phoneme: {})는 아직 출제되지 않음", i, kcPhonemes.get(i).getValue());
            }
        }

        if (availablePhonemes.isEmpty()) {
            log.warn("모든 Phoneme이 이미 출제되었습니다. candidateList 초기화 필요");
            return null;
        }

        // 6. 사용 가능한 Phoneme 중 랜덤 선택
        Phonemes selected = availablePhonemes.get(new Random().nextInt(availablePhonemes.size()));
        log.info("선택된 Phoneme: {}", selected.getValue());
        return selected;
    }

    public Integer getCandidateBitMask(Long userId, Long kcId) {
        Optional<TrainedProblemHistories> latestProblemHistory =
                trainedProblemHistoriesRepository.findFirstKCProbleHistories(userId, kcId);

        // 문제 이력이 없음
        if (latestProblemHistory.isEmpty()) {
            return 0;
        }

        Integer candidateList = latestProblemHistory.get().getCandidateList();
        if (candidateList == null) {
            log.info("candidateList가 null이어서 랜덤 선택");
            return 0;
        }

        log.info("candidateList 비트마스크: {} (binary: {})", candidateList, Integer.toBinaryString(candidateList));

        return candidateList;
    }
}
