package com.readingbuddy.backend.domain.bkt.service;

import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class BktService {

    private final UserKcMasteryRepository userKcMasteryRepository;

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
     * TODO: stage에서 개발하는 kc 출력
     */

    /**
     * TODO: 유저와 stage 가 들어오면 해당 stage에 대한 kc들의 숙련도가 충분한지 출력
     */
}
