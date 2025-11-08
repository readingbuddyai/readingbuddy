package com.readingbuddy.backend.domain.bkt.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.repository.KnowledgeComponentRepository;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import com.readingbuddy.backend.domain.train.dto.result.KcWithCorrectRate;
import com.readingbuddy.backend.domain.bkt.entity.PhonemesKcMap;
import com.readingbuddy.backend.domain.bkt.repository.PhonemesKcMapRepository;
import com.readingbuddy.backend.domain.train.dto.result.PhonemeWithKcIdAndCandidate;
import com.readingbuddy.backend.domain.train.entity.Phonemes;
import com.readingbuddy.backend.domain.train.repository.TrainedProblemHistoriesRepository;
import com.readingbuddy.backend.domain.user.entity.TrainedProblemHistories;

import com.readingbuddy.backend.domain.user.entity.User;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.math.BigInteger;
import java.time.LocalDateTime;
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
    private final PhonemesKcMapRepository phonemesKcMapRepository;
    private final TrainedProblemHistoriesRepository trainedProblemHistoriesRepository;
    private final double BASE_SPEED = 1.0;
    // p_train = 0.1
    private final double BASE_P_TRAIN_LOGIT = -2.1972245773362196;
    // 변화량 스케일링 0.3 (30%만 반영)
    private final double DELTA_SCALING = 0.3;
    /**
     * TODO: 유저와 stage 가 들어오면 해당 stage에 대한 kc들의 숙련도 출력 (부족한 부분까지 sorting) 해서 주기
     */

    /**
     * TODO: 유저, kc가 들어오면 해당 kc에 대한 정답률 반환
     */
    public Float getCorrectAnswerRate(Long userId, Long kcId) {
        UserKcMastery userKcMastery = userKcMasteryRepository.findFirstByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(userId, kcId)
                .orElseThrow(() -> new IllegalArgumentException("UserKcMastery를 찾을 수 없습니다: userId=" + userId + ", kcId=" + kcId));
        /**
         * 정답을 맞출 확률 = 이미 알고 있을 확률  * 실수 하지 않을 확룰 + 모를 확률 * 찍어서 맞출 확률
          */
        return userKcMastery.getPLearn() * (1 - userKcMastery.getPSlip()) + (1 - userKcMastery.getPLearn()) * (userKcMastery.getPGuess());
    }


    /**
     * TODO: 유저, stage와 문제의 합불이 들어오면 해당 문제에 해당 하는 kc에 대한 숙련도 update
     */
    public void updateLearnedMastery(Long userId, Long kcId, Boolean isCorrect, Float correctRate) {
        UserKcMastery userKcMastery = userKcMasteryRepository.findFirstByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(userId, kcId)
                .orElseThrow(() -> new IllegalArgumentException("UserKcMastery를 찾을 수 없습니다: userId=" + userId + ", kcId=" + kcId));

        float conditionalProbability = 0F;
        if (isCorrect) {
            conditionalProbability = (userKcMastery.getPLearn() * (1 - userKcMastery.getPSlip())) / correctRate;
        }
        else {
            conditionalProbability = (userKcMastery.getPLearn() * (userKcMastery.getPSlip())) / (1 - correctRate);
        }

        Float updatedLearnedMastery = conditionalProbability + (1 - conditionalProbability) * userKcMastery.getPTrain();

        Float updatedTrainedMastery= predictPersonalizedPLearn(userKcMastery.getUser(),userKcMastery.getKnowledgeComponent());
        UserKcMastery updatedKcMastery = userKcMastery.toBuilder()
                .id(null)
                .pLearn(updatedLearnedMastery)
                .pTrain(updatedTrainedMastery)
                .createdAt(LocalDateTime.now())
                .updatedAt(LocalDateTime.now())
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
    public PhonemeWithKcIdAndCandidate selectPhonemeUsingBitMask(Long userId, Long kcId, Set<Long> excludedPhonemeIds) {
        // 1. 선택된 KC에 해당하는 모든 Phonemes 조회
        List<Phonemes> kcPhonemes = phonemesKcMapRepository.findByKnowledgeComponent_Id(kcId)
                .stream()
                .map(PhonemesKcMap::getPhonemes)
                .toList();
        log.info("선택된 KC에 속한 Phonemes 개수: {}", kcPhonemes.size());

        Optional<TrainedProblemHistories> latestProblemHistory =
                trainedProblemHistoriesRepository.findFirstKCProbleHistories(userId, kcId);


        String candidateListStr = latestProblemHistory.map(TrainedProblemHistories::getCandidateList).orElse("0");
        BigInteger candidateList = new BigInteger(candidateListStr);

        log.info("candidateList 비트마스크: {} (binary: {})", candidateListStr, candidateList.toString(2));

        // 3. candidateList에서 0인 비트(아직 출제되지 않은 문제) + 제외 목록에 없는 문제 찾기
        List<Phonemes> availablePhonemes = new ArrayList<>();
        for (int i = 0; i < kcPhonemes.size(); i++) {
            Phonemes phoneme = kcPhonemes.get(i);
            // i번째 비트가 0이고, 제외 목록에 없으면 사용 가능
            if (!candidateList.testBit(i) && !excludedPhonemeIds.contains(phoneme.getId())) {
                availablePhonemes.add(phoneme);
                log.info("비트 {} (Phoneme: {})는 사용 가능", i, phoneme.getValue());
            }
        }

        // 4. 그래도 없으면 전체 Phoneme에서 제외 목록만 고려
        if (availablePhonemes.isEmpty()) {
            availablePhonemes = kcPhonemes.stream()
                    .filter(p -> !excludedPhonemeIds.contains(p.getId()))
                    .collect(Collectors.toList());
        }

        // 5. 최종적으로 사용 가능한 Phoneme이 없으면 랜덤 선택 (중복 허용)
        if (availablePhonemes.isEmpty()) {
            log.error("사용 가능한 Phoneme이 전혀 없음. 중복을 허용하여 랜덤 선택");
            availablePhonemes = kcPhonemes;
        }

        // 6. 사용 가능한 Phoneme 중 랜덤 선택
        Phonemes selected = availablePhonemes.get(new Random().nextInt(availablePhonemes.size()));
        log.info("선택된 Phoneme: {}", selected.getValue());
        return PhonemeWithKcIdAndCandidate.builder()
                .phonemes(selected)
                .candidateList(candidateListStr)
                .KcId(kcId)
                .build();
    }

    public String getCandidateBitMask(Long userId, Long kcId) {
        Optional<TrainedProblemHistories> latestProblemHistory =
                trainedProblemHistoriesRepository.findFirstKCProbleHistories(userId, kcId);

        // 문제 이력이 없음
        if (latestProblemHistory.isEmpty()) {
            return "0";
        }

        String candidateList = latestProblemHistory.get().getCandidateList();
        if (candidateList == null) {
            log.info("candidateList가 null이어서 랜덤 선택");
            return "0";
        }

        log.info("candidateList 비트마스크: {} (binary: {})", candidateList, new BigInteger(candidateList).toString(2));

        return candidateList;
    }

    public Float predictPersonalizedPLearn(User user, KnowledgeComponent kc) {
        double learnSpeedFactor = estimateLearnSpeed(user, kc);

        log.info("learn speed {}",learnSpeedFactor);
        double combinedLogit = BASE_P_TRAIN_LOGIT + Math.log(learnSpeedFactor);

        return sigmoid(combinedLogit);
    }

    // 특정 user, KC의 과거 기록으로 학습 속도 추정
    private double estimateLearnSpeed(User user, KnowledgeComponent kc) {
        List<UserKcMastery> records = userKcMasteryRepository.findByUserAndKnowledgeComponentOrderByCreatedAtAsc(user, kc);

        if (records.size() < 5) {
            return BASE_SPEED; // 기록이 충분하지 않으면 기본 속도
        }

        int validCount = 0;
        double totalDelta = 0.0;

        for (int i = 1; i < records.size(); i++) {
            double prevP = records.get(i - 1).getPLearn();
            double currP = records.get(i).getPLearn();

            // 1: 유효성 검증 - 이미 마스터했으면 학습 속도 추정 불가
            if (prevP >= 0.99 || currP >= 0.99) {
                continue;
            }

            double prevLogit = logit(prevP);
            double currLogit = logit(currP);
            double delta = currLogit - prevLogit;

            // 3: 유효한 변화량만 수집 (증가/감소 모두 포함, 너무 큰 변화는 노이즈로 간주)
            if (Math.abs(delta) < 5.0) {  // 양수/음수 모두 처리
                totalDelta += delta;
                validCount++;
            }
        }

        // 유효한 데이터가 없으면 기본 속도 반환
        if (validCount == 0) {
            return BASE_SPEED;
        }

        // 평균 변화량 계산
        double avgDelta = totalDelta / validCount;

        // 학습 속도 계산 (exponential scaling을 완화)
        // exp(avgDelta) 대신 더 부드러운 증가를 위해 스케일링 적용
        log.info("avg delta {}",avgDelta);
        double learnSpeedFactor = Math.exp(avgDelta * DELTA_SCALING);
        // 학습 속도가 너무 빠르거나 느리지 않도록 제한
        return Math.max(0.5, Math.min(learnSpeedFactor, 2.0));
    }

    // 로그 오즈 변환
    private double logit(double p) {
        if (p <= 0.0) p = 1e-6;
        if (p >= 1.0) p = 1 - 1e-6;
        return Math.log(p / (1 - p));
    }

    // 시그모이드 (역변환)
    private float sigmoid(double x) {
        return (float) (1.0 / (1.0 + Math.exp(-x)));
    }
}
