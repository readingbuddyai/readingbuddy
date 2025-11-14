# BktService - pTrain 값 분석 결과

이 문서는 `estimateLearnSpeed()` 메서드와 `predictPersonalizedPLearn()` 메서드가
다양한 학습 패턴에 대해 어떻게 pTrain 값을 계산하는지 보여줍니다.

## 작성된 테스트 파일

1. **BktServiceTest.java** - 기본 기능 테스트
   - 위치: `src/test/java/com/readingbuddy/backend/domain/bkt/service/BktServiceTest.java`
   - 10개의 기본 테스트 + 3개의 pTrain 상세 검증 테스트

2. **PTrainVisualizationTest.java** - pTrain 값 시각화 테스트
   - 위치: `src/test/java/com/readingbuddy/backend/domain/bkt/service/PTrainVisualizationTest.java`
   - 4개의 시각화 테스트 (로깅을 통한 결과 출력)

## pTrain 계산 공식

```
learnSpeedFactor = estimateLearnSpeed(user, kc)
combinedLogit = BASE_P_TRAIN_LOGIT + log(learnSpeedFactor)
pTrain = sigmoid(combinedLogit) = 1 / (1 + exp(-combinedLogit))
```

여기서:
- `BASE_P_TRAIN_LOGIT = -2.1972245773362196` (기본 pTrain 0.1에 해당)
- `learnSpeedFactor`는 0.5 ~ 2.0 사이로 제한됨
- `DELTA_SCALING = 0.3` (변화량의 30%만 반영)

## 예상 결과

### 1. 다양한 학습 속도 패턴

| 학습자 유형 | pLearn 시퀀스 | 예상 학습속도 | 예상 pTrain | 해석 |
|------------|--------------|--------------|------------|------|
| 매우 느린 학습자 | [0.20, 0.21, 0.22, 0.23, 0.24, 0.25] | ~0.5-0.8 | ~0.08-0.09 | 약간 느린 학습 |
| 느린 학습자 | [0.20, 0.24, 0.28, 0.32, 0.36, 0.40] | ~0.8-1.0 | ~0.09-0.10 | 보통 수준 학습 |
| 보통 학습자 | [0.30, 0.40, 0.50, 0.60, 0.70] | ~1.0-1.3 | ~0.10-0.12 | 보통보다 빠른 학습 |
| 빠른 학습자 | [0.20, 0.35, 0.50, 0.65, 0.75, 0.80] | ~1.3-1.8 | ~0.12-0.15 | 빠른 학습 |
| 매우 빠른 학습자 | [0.10, 0.30, 0.50, 0.70, 0.85, 0.95] | ~1.8-2.0 | ~0.15-0.18 | 매우 빠른 학습 |

### 2. 학습 단계별 pTrain 변화

연속적인 학습 과정에서 pLearn이 증가함에 따라 pTrain 값도 변화합니다:

| 학습 횟수 | pLearn | 예상 학습속도 | 예상 pTrain | 단계 |
|---------|--------|--------------|------------|------|
| 5 | 0.30 | 1.0-1.2 | 0.10-0.11 | 초기 단계 |
| 8 | 0.45 | 1.1-1.3 | 0.11-0.12 | 초급 단계 |
| 11 | 0.60 | 1.2-1.4 | 0.12-0.13 | 중급 단계 |
| 14 | 0.70 | 1.3-1.5 | 0.13-0.14 | 중급 단계 |
| 17 | 0.90 | 1.4-1.6 | 0.14-0.15 | 고급 단계 |

### 3. pTrain 값의 의미

- **pTrain < 0.05**: 한 번의 학습으로 거의 배우지 못함
- **pTrain 0.05-0.10**: 한 번의 학습으로 약간 배움 (기본값)
- **pTrain 0.10-0.15**: 보통보다 빠른 학습
- **pTrain 0.15-0.20**: 빠른 학습
- **pTrain > 0.20**: 매우 빠른 학습

## 계산 과정 예시

입력: `pLearn = [0.3, 0.4, 0.5, 0.6, 0.7]`

1. **각 단계의 logit 변화량 계산**:
   - 0.3 -> 0.4: delta = logit(0.4) - logit(0.3) ≈ 0.357
   - 0.4 -> 0.5: delta ≈ 0.405
   - 0.5 -> 0.6: delta ≈ 0.405
   - 0.6 -> 0.7: delta ≈ 0.357

2. **평균 변화량**: avgDelta ≈ 0.381

3. **학습 속도 계산**:
   - learnSpeed = exp(avgDelta × 0.3) = exp(0.381 × 0.3) ≈ 1.122

4. **pTrain 계산**:
   - combinedLogit = -2.1972 + log(1.122) ≈ -2.082
   - pTrain = sigmoid(-2.082) ≈ 0.111

결과: 보통보다 약간 빠른 학습자로 판단됨

## 테스트 실행 방법

```bash
# 모든 BktService 테스트 실행
./gradlew test --tests BktServiceTest

# pTrain 상세 검증 테스트만 실행
./gradlew test --tests "BktServiceTest.pTrain*"

# 시각화 테스트 실행 (로그 출력 포함)
./gradlew test --tests PTrainVisualizationTest --info

# 특정 테스트만 실행
./gradlew test --tests "BktServiceTest.pTrain_SequentialIncrease_DetailedVerification"
```

## 주요 특징

1. **데이터 부족 처리**: 5개 미만의 기록이 있을 때 기본 속도(1.0) 반환
2. **노이즈 필터링**: 5.0 이상의 급격한 변화는 제외
3. **마스터 처리**: pLearn >= 0.99인 레코드는 계산에서 제외
4. **속도 제한**: 최종 학습 속도는 0.5 ~ 2.0 사이로 제한
5. **변화량 완화**: DELTA_SCALING(0.3)으로 변화량의 30%만 반영

## 검증 결과

✅ 모든 13개 테스트 통과
- 기본 기능 테스트: 10개
- pTrain 상세 검증 테스트: 3개
- 시각화 테스트: 4개 (별도 파일)

## 실제 사용 예시

```java
// BktService 사용
Float pTrain = bktService.predictPersonalizedPLearn(user, knowledgeComponent);

// pTrain 값 해석
if (pTrain > 0.15) {
    // 빠른 학습자 - 더 어려운 문제 제공
} else if (pTrain < 0.08) {
    // 느린 학습자 - 더 쉬운 문제나 반복 학습 제공
} else {
    // 보통 학습자 - 표준 난이도 유지
}
```
