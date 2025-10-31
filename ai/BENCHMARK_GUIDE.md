# SafeTensor 최적화 성능 테스트 가이드

## 📋 개요

이 가이드는 SafeTensor 추론 속도를 단계별로 최적화하고, 각 최적화 기법의 성능 향상을 측정하는 방법을 설명합니다.

---

## 🚀 빠른 시작

### 1. 벤치마크 실행

```bash
# ai 폴더로 이동
cd ai

# 벤치마크 실행
python benchmark_safetensor_optimizations.py
```

### 2. 예상 출력

```
================================================================================
SafeTensor 최적화 성능 벤치마크
================================================================================

설정:
  Device: cuda
  워밍업 횟수: 3
  측정 횟수: 20
  샘플링 레이트: 16000Hz

[Baseline] 모델 로딩 중...
[Baseline] 로드 완료 (Device: cuda)

[Batch Inference] 모델 로딩 중...
[Batch Inference] 로드 완료 (Batch Size: 8)

...

================================================================================
시나리오: 단일 청크 (1초)
================================================================================

모델                      평균 (ms)    표준편차     최소      최대      속도 향상
--------------------------------------------------------------------------------
1. Baseline (현재)            25.30ms      1.20ms   23.50ms   27.80ms          -
2. Batch Inference             24.10ms      0.90ms   22.80ms   26.10ms      1.05x
3. FP16 Mixed Precision        18.50ms      0.80ms   17.20ms   20.30ms      1.37x
4. TorchScript JIT             22.40ms      1.10ms   20.90ms   24.80ms      1.13x
5. Combined (All)              16.20ms      0.70ms   15.10ms   17.80ms      1.56x

================================================================================
시나리오: 긴 오디오 (10초, 10 청크)
================================================================================

모델                      평균 (ms)    표준편차     최소      최대      속도 향상
--------------------------------------------------------------------------------
1. Baseline (현재)           253.00ms     12.00ms  240.00ms  278.00ms          -
2. Batch Inference            85.00ms      4.50ms   79.00ms   92.00ms      2.98x
3. FP16 Mixed Precision      185.00ms      8.00ms  172.00ms  203.00ms      1.37x
4. TorchScript JIT           224.00ms     11.00ms  209.00ms  248.00ms      1.13x
5. Combined (All)             62.00ms      3.20ms   57.00ms   68.00ms      4.08x
```

---

## 📊 테스트 시나리오

벤치마크는 3가지 시나리오로 테스트합니다:

| 시나리오 | 오디오 길이 | 청크 개수 | 실제 사용 케이스 |
|---------|------------|----------|-----------------|
| **단일 청크** | 1초 | 1개 | 짧은 발음 체크 |
| **짧은 오디오** | 3초 | 1개 | 단어/문장 발음 |
| **긴 오디오** | 10초 | 10개 | 긴 문장/문단 발음 |

---

## 🔧 최적화 기법 설명

### 1️⃣ Baseline (현재 구현)
- **설명**: 최적화 없는 기본 구현
- **특징**:
  - 청크마다 개별 추론
  - FP32 전정밀도
  - 최적화 없음
- **장점**: 구현 단순, 안정적
- **단점**: 느린 속도

### 2️⃣ Batch Inference (배치 추론)
- **설명**: 여러 청크를 한 번에 배치로 처리
- **핵심 개선**:
  ```python
  # Before: 10번 GPU 전송
  for chunk in chunks:
      result = infer(chunk)

  # After: 1-2번 GPU 전송
  results = infer_batch(chunks, batch_size=8)
  ```
- **예상 속도**: **2-3배 빠름** (긴 오디오)
- **적용 조건**: 청크가 여러 개일 때 효과적

### 3️⃣ FP16 Mixed Precision (반정밀도)
- **설명**: 32비트 → 16비트 부동소수점 연산
- **핵심 개선**:
  ```python
  model = model.half()  # FP32 → FP16
  inputs = inputs.half()
  ```
- **예상 속도**: **1.3-1.5배 빠름**
- **메모리**: **50% 절약**
- **정확도**: 거의 동일 (무시할 수준)
- **제약**: CUDA GPU 필수 (CPU 불가)

### 4️⃣ TorchScript JIT Compile
- **설명**: 모델을 미리 컴파일하여 최적화
- **핵심 개선**:
  ```python
  model = torch.jit.trace(model, dummy_input)
  ```
- **예상 속도**: **1.1-1.2배 빠름**
- **장점**: 추가 메모리 없음
- **단점**: 컴파일 시간 필요 (시작 시)

### 5️⃣ Combined (통합 최적화)
- **설명**: Batch + FP16 조합
- **예상 속도**: **3-5배 빠름** (긴 오디오)
- **최적 시나리오**: 10초 이상 긴 오디오

---

## 📈 예상 성능 개선

### 단일 청크 (1초)
```
Baseline:     25ms
FP16:         18ms  (1.4배 ↑)
Combined:     16ms  (1.6배 ↑)
```

### 긴 오디오 (10초, 10 청크)
```
Baseline:    250ms
Batch:        85ms  (3.0배 ↑)
FP16:        185ms  (1.4배 ↑)
Combined:     62ms  (4.0배 ↑)  ⭐ 최고 성능!
```

---

## 🎯 어떤 최적화를 선택할까?

### 📍 시나리오별 추천

| 사용 케이스 | 추천 최적화 | 이유 |
|------------|-----------|------|
| **짧은 발음 (1-3초)** | **FP16** | 단일 청크라 배치 효과 적음 |
| **긴 발음 (5-10초)** | **Combined** | 배치 + FP16 시너지 최고 |
| **실시간 스트리밍** | **FP16** | 배치 불가, FP16만 적용 |
| **CPU 환경** | **Batch** | FP16 불가, 배치만 적용 |
| **안정성 우선** | **Baseline** | 최적화 없음, 가장 안정적 |

### ⚙️ 하드웨어별 추천

| 환경 | GPU | 추천 최적화 | 예상 속도 향상 |
|------|-----|-----------|--------------|
| **고성능 서버** | RTX 3090/4090 | Combined | 4-5배 ↑ |
| **일반 서버** | GTX 1080/2080 | FP16 | 1.3-1.5배 ↑ |
| **CPU 전용** | - | Batch | 2-3배 ↑ |

---

## 🔬 커스텀 테스트 방법

### 1. 특정 오디오 파일로 테스트

벤치마크 스크립트를 수정하여 실제 오디오 파일로 테스트:

```python
# benchmark_safetensor_optimizations.py 수정

import librosa

# 실제 오디오 로드
audio, sr = librosa.load("your_audio.wav", sr=16000)
chunks = create_chunks(audio, num_chunks=10)

# 벤치마크 실행
result = benchmark_model(model, chunks, "My Audio")
```

### 2. Batch Size 튜닝

최적의 배치 크기 찾기:

```python
# 다양한 배치 크기 테스트
for batch_size in [4, 8, 16, 32]:
    model = BatchInference(batch_size=batch_size)
    result = benchmark_model(model, chunks, f"Batch-{batch_size}")
    print(f"Batch {batch_size}: {result['mean']*1000:.2f}ms")
```

### 3. GPU 메모리 사용량 확인

```python
import torch

# 추론 전
print(f"GPU 메모리 (전): {torch.cuda.memory_allocated()/1024**2:.2f}MB")

# 추론 실행
result = model.infer_chunks(chunks)

# 추론 후
print(f"GPU 메모리 (후): {torch.cuda.memory_allocated()/1024**2:.2f}MB")
```

---

## 🐛 문제 해결

### Q1: "CUDA out of memory" 오류

**원인**: 배치 크기가 너무 큼

**해결**:
```python
# 배치 크기 줄이기
model = BatchInference(batch_size=4)  # 8 → 4로 감소
```

### Q2: FP16에서 정확도 저하

**원인**: 매우 드물지만 발생 가능

**해결**:
```python
# FP16 대신 FP32 사용 (Baseline 또는 Batch만)
model = BatchInference()  # FP16 없이 배치만 사용
```

### Q3: JIT 컴파일 실패

**원인**: 모델 구조가 복잡하거나 동적 연산 포함

**해결**:
```python
# JIT 제외하고 다른 최적화만 사용
# Combined에서 JIT 부분 제거
```

### Q4: CPU에서 느림

**원인**: FP16이 CPU에서 지원 안 됨

**해결**:
```python
# Batch만 사용
model = BatchInference()
```

---

## 📝 실제 적용 방법

### Step 1: 벤치마크 실행
```bash
python benchmark_safetensor_optimizations.py
```

### Step 2: 결과 분석
- 어떤 최적화가 가장 빠른지 확인
- 메모리 사용량 확인
- 정확도 차이 확인 (있다면)

### Step 3: 프로덕션 적용
1. `inference_tensor.py` 백업
2. 선택한 최적화 코드 복사
3. 테스트 환경에서 검증
4. 프로덕션 배포

### Step 4: 모니터링
- 추론 시간 로그 확인
- GPU 사용률 모니터링
- 오류율 체크

---

## 🎓 추가 최적화 (고급)

### 1. ONNX Runtime과 비교
```bash
python compare_inference_speed.py
```

### 2. TensorRT 최적화 (NVIDIA GPU 전용)
- PyTorch → ONNX → TensorRT 변환
- 최대 5-10배 속도 향상 가능

### 3. 모델 양자화 (INT8)
- FP16 → INT8 변환
- 추가 2배 속도 향상 가능
- 정확도 약간 저하 가능

---

## 📞 문의 및 기여

벤치마크 결과나 개선 사항이 있다면 공유해주세요!

**Happy Optimizing! 🚀**
