package com.readingbuddy.backend.domain.bkt.service;

import com.readingbuddy.backend.domain.bkt.entity.KnowledgeComponent;
import com.readingbuddy.backend.domain.bkt.entity.UserKcMastery;
import com.readingbuddy.backend.domain.bkt.enums.KcCategory;
import com.readingbuddy.backend.domain.bkt.repository.UserKcMasteryRepository;
import com.readingbuddy.backend.domain.user.entity.User;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDateTime;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@DisplayName("BktService 테스트 - pLearn 변화 관찰")
class BktServiceTest {

    @Mock
    private UserKcMasteryRepository userKcMasteryRepository;

    @InjectMocks
    private BktService bktService;

    private User testUser;
    private KnowledgeComponent testKc;
    private UserKcMastery initialMastery;

    @BeforeEach
    void setUp() {
        testUser = User.builder()
                .id(1L)
                .nickname("testUser")
                .build();

        testKc = KnowledgeComponent.builder()
                .id(1L)
                .stage("3")
                .category(KcCategory.CLOSED_SYLLABLE)
                .build();

        // 초기 BKT 파라미터 설정
        initialMastery = UserKcMastery.builder()
                .id(1L)
                .user(testUser)
                .knowledgeComponent(testKc)
                .pLearn(0.0f)    // 초기 숙달 확률 0%
                .pTrain(0.1f)    // 학습 확률 10%
                .pGuess(0.2f)   // 추측 확률 20%
                .pSlip(0.1f)     // 실수 확률 10%
                .createdAt(LocalDateTime.now())
                .updatedAt(LocalDateTime.now())
                .build();
    }

    @Test
    @DisplayName("연속 정답 - pLearn이 0.8 이상 도달하는 시점 관찰")
    void testPLearnProgressionWithCorrectAnswers() {
        // given
        UserKcMastery currentMastery = initialMastery;
        int attemptCount = 0;
        int targetReached = -1;
        final float TARGET_MASTERY = 0.8f;
        final int MAX_ATTEMPTS = 50;

        System.out.println("=== BKT pLearn 변화 관찰 (연속 정답) ===");
        System.out.printf("초기값 - pLearn: %.4f, pTrain: %.4f, pGuess: %.4f, pSlip: %.4f%n",
                initialMastery.getPLearn(), initialMastery.getPTrain(),
                initialMastery.getPGuess(), initialMastery.getPSlip());
        System.out.println("목표: pLearn >= " + TARGET_MASTERY);
        System.out.println("----------------------------------------");

        // when - 정답을 계속 맞추면서 pLearn 변화 관찰
        while (attemptCount < MAX_ATTEMPTS && currentMastery.getPLearn() < TARGET_MASTERY) {
            attemptCount++;

            // Mock 설정: 현재 mastery 반환
            when(userKcMasteryRepository.findFirstByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(
                    testUser.getId(), testKc.getId()))
                    .thenReturn(Optional.of(currentMastery));

            // save() 호출 시 입력값을 그대로 반환 (ID는 새로 생성)
            final int finalAttemptCount = attemptCount;
            when(userKcMasteryRepository.save(any(UserKcMastery.class))).thenAnswer(invocation -> {
                UserKcMastery saved = invocation.getArgument(0);
                // saved.getId()는 null이므로 새 ID 생성
                return UserKcMastery.builder()
                        .id((long) finalAttemptCount + 1)  // 순차적으로 ID 부여
                        .user(saved.getUser())
                        .knowledgeComponent(saved.getKnowledgeComponent())
                        .pLearn(saved.getPLearn())
                        .pTrain(saved.getPTrain())
                        .pGuess(saved.getPGuess())
                        .pSlip(saved.getPSlip())
                        .createdAt(saved.getCreatedAt() != null ? saved.getCreatedAt() : LocalDateTime.now())
                        .updatedAt(saved.getUpdatedAt() != null ? saved.getUpdatedAt() : LocalDateTime.now())
                        .build();
            });

            // 정답률 계산
            Float correctRate = bktService.getCorrectAnswerRate(testUser.getId(), testKc.getId());

            // updateLearnedMastery 호출 (정답)
            bktService.updateLearnedMastery(testUser.getId(), testKc.getId(), true, correctRate);

            // save()가 반환한 값을 다음 mastery로 설정
            // verify로 save가 호출되었는지 확인하고 실제 저장된 값 가져오기
            ArgumentCaptor<UserKcMastery> masteryCaptor = ArgumentCaptor.forClass(UserKcMastery.class);
            verify(userKcMasteryRepository, atLeastOnce()).save(masteryCaptor.capture());
            UserKcMastery updatedMastery = masteryCaptor.getValue();
            currentMastery = updatedMastery;

            // 진행 상황 출력
            System.out.printf("시도 %2d: pLearn = %.6f (correctRate: %.4f)%n",
                    attemptCount, updatedMastery.getPLearn(), correctRate);

            // 목표 도달 확인
            if (updatedMastery.getPLearn() >= TARGET_MASTERY && targetReached == -1) {
                targetReached = attemptCount;
                System.out.println(">>> 목표 도달! pLearn >= " + TARGET_MASTERY);
            }
        }

        // then
        System.out.println("----------------------------------------");
        System.out.printf("최종 결과 - pLearn: %.6f (시도 횟수: %d)%n",
                currentMastery.getPLearn(), attemptCount);

        if (targetReached > 0) {
            System.out.printf("✅ pLearn 0.8 도달: %d번째 정답%n", targetReached);
            assertTrue(currentMastery.getPLearn() >= TARGET_MASTERY,
                    "pLearn이 목표값에 도달해야 함");
        } else {
            System.out.printf("⚠️ %d번 시도 후에도 목표 미달성 (현재: %.6f)%n",
                    MAX_ATTEMPTS, currentMastery.getPLearn());
        }

        // 검증
        assertNotNull(currentMastery);
        assertTrue(currentMastery.getPLearn() > initialMastery.getPLearn(),
                "pLearn이 초기값보다 증가해야 함");
        assertTrue(currentMastery.getPLearn() <= 1.0f,
                "pLearn은 1.0을 초과할 수 없음");
    }

    @Test
    @DisplayName("정답/오답 혼합 - pLearn 변화 관찰")
    void testPLearnProgressionWithMixedAnswers() {
        // given
        UserKcMastery currentMastery = initialMastery;
        int correctCount = 0;
        int wrongCount = 0;
        int targetReached = -1;
        final float TARGET_MASTERY = 0.8f;
        final int MAX_ATTEMPTS = 100;

        // 정답/오답 패턴: 정답 3번 → 오답 1번 반복
        boolean[] answerPattern = {true, true, true, false};
        int patternIndex = 0;

        System.out.println("=== BKT pLearn 변화 관찰 (정답/오답 혼합) ===");
        System.out.printf("초기값 - pLearn: %.4f%n", initialMastery.getPLearn());
        System.out.println("패턴: 정답 3번 → 오답 1번 반복");
        System.out.println("목표: pLearn >= " + TARGET_MASTERY);
        System.out.println("----------------------------------------");

        // when
        while ((correctCount + wrongCount) < MAX_ATTEMPTS && currentMastery.getPLearn() < TARGET_MASTERY) {
            boolean isCorrect = answerPattern[patternIndex % answerPattern.length];
            patternIndex++;

            if (isCorrect) {
                correctCount++;
            } else {
                wrongCount++;
            }

            // Mock 설정
            when(userKcMasteryRepository.findFirstByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(
                    testUser.getId(), testKc.getId()))
                    .thenReturn(Optional.of(currentMastery));

            // save() 호출 시 입력값을 그대로 반환 (ID는 새로 생성)
            final int totalAttempts = correctCount + wrongCount;
            when(userKcMasteryRepository.save(any(UserKcMastery.class))).thenAnswer(invocation -> {
                UserKcMastery saved = invocation.getArgument(0);
                return UserKcMastery.builder()
                        .id((long) totalAttempts + 1)
                        .user(saved.getUser())
                        .knowledgeComponent(saved.getKnowledgeComponent())
                        .pLearn(saved.getPLearn())
                        .pTrain(saved.getPTrain())
                        .pGuess(saved.getPGuess())
                        .pSlip(saved.getPSlip())
                        .createdAt(saved.getCreatedAt() != null ? saved.getCreatedAt() : LocalDateTime.now())
                        .updatedAt(saved.getUpdatedAt() != null ? saved.getUpdatedAt() : LocalDateTime.now())
                        .build();
            });

            // 정답률 계산 및 업데이트
            Float correctRate = bktService.getCorrectAnswerRate(testUser.getId(), testKc.getId());
            bktService.updateLearnedMastery(testUser.getId(), testKc.getId(), isCorrect, correctRate);

            // save()가 반환한 값을 다음 mastery로 설정
            ArgumentCaptor<UserKcMastery> masteryCaptor = ArgumentCaptor.forClass(UserKcMastery.class);
            verify(userKcMasteryRepository, atLeastOnce()).save(masteryCaptor.capture());
            UserKcMastery updatedMastery = masteryCaptor.getValue();
            currentMastery = updatedMastery;

            // 진행 상황 출력 (5번마다)
            if ((correctCount + wrongCount) % 5 == 0) {
                System.out.printf("시도 %3d (%dO/%dX): pLearn = %.6f%n",
                        correctCount + wrongCount, correctCount, wrongCount, updatedMastery.getPLearn());
            }

            // 목표 도달 확인
            if (updatedMastery.getPLearn() >= TARGET_MASTERY && targetReached == -1) {
                targetReached = correctCount + wrongCount;
                System.out.printf(">>> 목표 도달! pLearn >= %.2f (정답: %d, 오답: %d)%n",
                        TARGET_MASTERY, correctCount, wrongCount);
            }
        }

        // then
        System.out.println("----------------------------------------");
        System.out.printf("최종 결과 - pLearn: %.6f%n", currentMastery.getPLearn());
        System.out.printf("총 시도: %d회 (정답: %d, 오답: %d, 정답률: %.1f%%)%n",
                correctCount + wrongCount, correctCount, wrongCount,
                (correctCount * 100.0 / (correctCount + wrongCount)));

        if (targetReached > 0) {
            System.out.printf("✅ pLearn 0.8 도달: %d번째 시도%n", targetReached);
        } else {
            System.out.printf("⚠️ %d번 시도 후에도 목표 미달성%n", MAX_ATTEMPTS);
        }

        // 검증
        assertNotNull(currentMastery);
        assertTrue(currentMastery.getPLearn() > initialMastery.getPLearn(),
                "pLearn이 증가해야 함 (오답이 있어도 정답이 더 많으면 증가)");
    }

    @Test
    @DisplayName("다양한 초기값에서 pLearn 변화 비교")
    void testPLearnProgressionWithDifferentInitialValues() {
        System.out.println("=== 초기 pLearn 값에 따른 0.8 도달 시간 비교 ===");
        System.out.println("조건: 연속 정답만 입력");
        System.out.println("----------------------------------------");

        float[] initialPLearnValues = {0.1f, 0.3f, 0.5f, 0.7f};
        final float TARGET_MASTERY = 0.8f;
        final int MAX_ATTEMPTS = 50;

        for (float initialPLearn : initialPLearnValues) {
            // 초기 mastery 설정
            UserKcMastery currentMastery = UserKcMastery.builder()
                    .id(1L)
                    .user(testUser)
                    .knowledgeComponent(testKc)
                    .pLearn(initialPLearn)
                    .pTrain(0.3f)
                    .pGuess(0.25f)
                    .pSlip(0.1f)
                    .createdAt(LocalDateTime.now())
                    .updatedAt(LocalDateTime.now())
                    .build();

            int attemptCount = 0;
            int targetReached = -1;

            System.out.printf("%n초기 pLearn: %.2f%n", initialPLearn);

            // 연속 정답 입력
            while (attemptCount < MAX_ATTEMPTS && currentMastery.getPLearn() < TARGET_MASTERY) {
                attemptCount++;

                when(userKcMasteryRepository.findFirstByUser_IdAndKnowledgeComponent_IdOrderByCreatedAtDesc(
                        testUser.getId(), testKc.getId()))
                        .thenReturn(Optional.of(currentMastery));

                // save() 호출 시 입력값을 그대로 반환 (ID는 새로 생성)
                final int finalAttemptCount2 = attemptCount;
                when(userKcMasteryRepository.save(any(UserKcMastery.class))).thenAnswer(invocation -> {
                    UserKcMastery saved = invocation.getArgument(0);
                    return UserKcMastery.builder()
                            .id((long) finalAttemptCount2 + 1)
                            .user(saved.getUser())
                            .knowledgeComponent(saved.getKnowledgeComponent())
                            .pLearn(saved.getPLearn())
                            .pTrain(saved.getPTrain())
                            .pGuess(saved.getPGuess())
                            .pSlip(saved.getPSlip())
                            .createdAt(saved.getCreatedAt() != null ? saved.getCreatedAt() : LocalDateTime.now())
                            .updatedAt(saved.getUpdatedAt() != null ? saved.getUpdatedAt() : LocalDateTime.now())
                            .build();
                });

                Float correctRate = bktService.getCorrectAnswerRate(testUser.getId(), testKc.getId());
                bktService.updateLearnedMastery(testUser.getId(), testKc.getId(), true, correctRate);

                // save()가 반환한 값을 다음 mastery로 설정
                ArgumentCaptor<UserKcMastery> masteryCaptor = ArgumentCaptor.forClass(UserKcMastery.class);
                verify(userKcMasteryRepository, atLeastOnce()).save(masteryCaptor.capture());
                UserKcMastery updatedMastery = masteryCaptor.getValue();
                currentMastery = updatedMastery;

                if (updatedMastery.getPLearn() >= TARGET_MASTERY && targetReached == -1) {
                    targetReached = attemptCount;
                }
            }

            if (targetReached > 0) {
                System.out.printf("  → %.2f에서 시작 → %d번 정답으로 0.8 도달 (최종: %.4f)%n",
                        initialPLearn, targetReached, currentMastery.getPLearn());
            } else {
                System.out.printf("  → %.2f에서 시작 → %d번 후에도 미달성 (최종: %.4f)%n",
                        initialPLearn, MAX_ATTEMPTS, currentMastery.getPLearn());
            }
        }

        System.out.println("----------------------------------------");
    }
}
