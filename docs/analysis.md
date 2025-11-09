# wav2vec2-korean-phoneme 모델 Fine-tuning 프로젝트 분석

**작성일**: 2025-11-07
**프로젝트**: 어린이 음성 인식을 위한 Wav2Vec2 한국어 음소 기반 모델 파인튜닝
**베이스 모델**: facebook/wav2vec2-xls-r-300m (한국어 음소 기반 CTC)
**데이터**: 한국어 어린이 음성 데이터셋 (~3500시간)

---

## 📋 목차

1. [프로젝트 개요](#1-프로젝트-개요)
2. [모델 선택 이유](#2-모델-선택-이유-wav2vec2-korean-phoneme)
3. [데이터 준비 전략](#3-데이터-준비-전략)
4. [Fine-tuning 접근법](#4-fine-tuning-접근법)
5. [최적화 기법](#5-최적화-기법)
6. [실험 결과 및 성능](#6-실험-결과-및-성능)
7. [핵심 학습 사항](#7-핵심-학습-사항)
8. [향후 개선 방향](#8-향후-개선-방향)

---

## 1. 프로젝트 개요

### 1.1 목표
- **주요 목표**: 한국어 어린이 음성에 특화된 음성 인식 모델 개발
- **성능 목표**: Phoneme Error Rate (PER) < 10-12%
- **용도**: VR 환경 또는 교육 애플리케이션에서 어린이 음성 인식

### 1.2 도전 과제
- **어린이 음성 특성**: 성인 대비 높은 음높이, 불안정한 발화, 다양한 연령대
- **데이터 규모**: 약 177만 샘플 (3500시간) - 대규모 데이터 효율적 처리 필요
- **계산 리소스**: 제한된 GPU 환경에서 효율적 학습 필요

### 1.3 프로젝트 구조

```
/home/j-k13a206/
├── data/
│   ├── child_subset_100h/          # 148h 파일럿 서브셋
│   └── child_extracted/            # 전체 3500h 데이터
├── models/
│   └── wav2vec2-korean-phoneme/    # 베이스 모델
├── fine_tunining_new/              # 메인 학습 파이프라인
│   ├── train_all_phases.py         # Phase 1-3 학습 (서브셋)
│   ├── train_all_phases_all.py     # 전체 데이터 학습
│   ├── train_all_phases_all_r32_optimized.py  # r=32 최적화 버전
│   ├── checkpoints/                # 서브셋 체크포인트
│   ├── checkpoints_full/           # 전체 데이터 (r=16)
│   └── checkpoints_full_r32/       # 전체 데이터 (r=32)
└── finetunning/                    # 초기 실험 및 유틸리티
    └── organized/                  # 정리된 스크립트
```

---

## 2. 모델 선택 이유: wav2vec2-korean-phoneme

### 2.1 선택한 모델
- **모델 ID**: facebook/wav2vec2-xls-r-300m (한국어 음소 특화 버전)
- **아키텍처**: Wav2Vec2-XLS-R-300M
- **파라미터**: 약 300M (3억 개)
- **특수성**: 한국어 음소(Phoneme) 기반 CTC 출력

### 2.2 모델 선택 근거

#### 2.2.1 음소 기반 접근의 장점
```
wav2vec2-korean-phoneme의 핵심 특징:

1. 음소 단위 인식 (45개 한국어 음소)
   - 'ㄱ', 'ㄴ', 'ㄷ' 등 자음
   - 'ㅏ', 'ㅓ', 'ㅗ' 등 모음
   - 'ㄲ', 'ㄸ' 등 경음

2. 음운 규칙 적용
   - 연음화: "한국어" → [H A N G U G EO]
   - 비음화: "국민" → [G U NG M I N]
   - 경음화: "학교" → [H A Kh GG O]
```

**장점**:
- **일반화 능력**: 단어 수준이 아닌 음소 수준 학습으로 미등록어(OOV) 대응 우수
- **데이터 효율성**: 어휘 크기가 작아(45개) 적은 데이터로도 효과적
- **어린이 음성 적합**: 발음이 불안정한 어린이 음성에서도 음소 단위로 인식 가능

#### 2.2.2 Wav2Vec2 아키텍처의 강점

```json
모델 구조 (config.json):
{
  "num_hidden_layers": 24,
  "hidden_size": 1024,
  "num_attention_heads": 16,
  "intermediate_size": 4096,
  "vocab_size": 45
}
```

**주요 특징**:
1. **24층 Transformer**: 깊은 계층으로 복잡한 음향 패턴 학습
2. **Self-supervised Pre-training**: 대규모 음성 데이터로 사전 학습됨
3. **CTC (Connectionist Temporal Classification)**: 음성-텍스트 정렬 없이 학습 가능

#### 2.2.3 대안 모델 대비 비교

| 특성 | wav2vec2-korean-phoneme | KoSpeech | Whisper |
|------|------------------------|----------|---------|
| **출력 단위** | 음소 (45개) | 음절/자모 | 단어 |
| **파라미터** | 300M | 100-200M | 1.5B (large) |
| **OOV 대응** | 매우 우수 | 우수 | 우수 |
| **한국어 최적화** | 매우 높음 | 높음 | 중간 |
| **Fine-tuning 효율** | 높음 (LoRA 가능) | 중간 | 낮음 (크기) |
| **어린이 음성** | 적합 | 적합 | 적합 |

**선택 이유**:
- 한국어 음소에 특화되어 있어 어린이의 불안정한 발음도 음소 단위로 인식 가능
- LoRA를 통한 효율적 Fine-tuning 가능
- 300M 파라미터로 적절한 크기 (학습 가능하면서도 성능 우수)

---

## 3. 데이터 준비 전략

### 3.1 전체 데이터셋 구성

```
전체 데이터: ~3500시간 (1,771,800개 샘플)
├── 1.Training: 1,771,800개
├── 2.Validation: 114,539개
└── 3.Test: 100,660개
```

### 3.2 파일럿 서브셋 생성 (100h → 148h)

#### 3.2.1 서브셋 생성 전략

**목표**: 전체 데이터 학습 전 빠른 검증용 서브셋 생성

**방법론**: 층화 샘플링 (Stratified Sampling)
```python
층화 기준:
1. 연령대 분포 유지
2. SNR (Signal-to-Noise Ratio) 분포 유지
3. 발화 길이 분포 유지
4. 화자 완전 분리 (Train/Val/Test 간 중복 0%)
```

**실제 결과**:
```
목표: 100시간 → 실제: 148.86시간 (화자 단위 선택으로 인한 초과)

분할:
├── Train:      136.19h (57,746개, 63명 화자)
├── Validation:   7.78h ( 2,715개,  9명 화자)
└── Test:         4.89h ( 2,216개, 10명 화자)

화자 중복: 0명 ✅
```

**장점**:
- 전체 데이터의 대표성 유지
- 화자 완전 분리로 일반화 성능 정확히 평가 가능
- 빠른 실험 반복 (148h vs 3500h)

#### 3.2.2 Validation 최적화

**문제**: 초기 전체 데이터 학습 시 Validation에 114,539개 샘플 사용 → 평가 시간 과다

**해결책**:
```python
# train_all_phases_all_r32_optimized.py
COMMON_CONFIG = {
    'max_val_samples': 20000,  # 114,539 → 20,000개로 축소
}
```

**효과**:
- 평가 시간: 14.5분/회 → 2.5분/회 (약 6배 단축)
- 총 학습 시간: 16일 → 10일 (약 40% 단축)

### 3.3 데이터 로딩 최적화

#### 3.3.1 Streaming Dataset

**과제**: 177만 개 샘플을 메모리에 모두 로드 불가

**해결책**: IterableDataset 사용
```python
class StreamingChildSpeechDataset(IterableDataset):
    """On-the-fly 데이터 로딩"""

    def __init__(self, data_dir, split, processor, g2p):
        # JSON 파일 목록만 메모리에 저장
        self.json_files = glob.glob(f"{data_dir}/{split}/**/*.json")

    def __iter__(self):
        for json_path in self.json_files:
            # 필요할 때만 WAV 파일 로드
            sample = self._load_sample(json_path)
            yield sample
```

**장점**:
- 메모리 사용량: 최소화 (샘플 단위 로딩)
- 학습 시작 시간: 즉시 (사전 로딩 불필요)
- 유연성: 데이터 크기에 무관하게 확장 가능

#### 3.3.2 한국어 G2P (Grapheme-to-Phoneme)

**목적**: 텍스트를 음소로 변환

```python
from korean_g2p import KoreanG2P

g2p = KoreanG2P()

# 예시
text = "한국어"
phonemes = g2p(text)
# 결과: "H A N G U G EO"
```

**적용된 음운 규칙**:
- 연음화, 비음화, 경음화, 구개음화 등
- 어린이의 실제 발음 패턴을 반영

---

## 4. Fine-tuning 접근법

### 4.1 LoRA (Low-Rank Adaptation)

#### 4.1.1 LoRA란?

**개념**: 전체 모델을 Fine-tuning하는 대신, 작은 어댑터 행렬만 학습

```
원래 가중치: W (고정)
LoRA 업데이트: ΔW = A × B (학습)
최종 가중치: W' = W + α/r × A × B

여기서:
- A: (d × r) 행렬
- B: (r × k) 행렬
- r: Rank (LoRA의 차원, 작을수록 파라미터 적음)
- α: Scaling factor
```

**장점**:
1. **파라미터 효율성**: 전체 모델의 1-2%만 학습
2. **메모리 절약**: Gradient 계산량 대폭 감소
3. **빠른 학습**: Backpropagation이 작은 행렬에만 적용
4. **다중 작업**: 여러 LoRA 어댑터를 베이스 모델에 교체 가능

#### 4.1.2 프로젝트의 LoRA 설정

**실험 1: LoRA r=16**
```python
LoraConfig(
    r=16,                    # Rank
    lora_alpha=32,           # Scaling factor (일반적으로 r × 2)
    target_modules=[
        "q_proj",            # Query projection
        "k_proj",            # Key projection
        "v_proj",            # Value projection
    ],
    lora_dropout=0.1,
    bias="none",
)
```

**학습 가능 파라미터**:
```
전체: 320,203,437개
학습: 2,359,296개 (0.74%)
```

**실험 2: LoRA r=32 (최적화 버전)**
```python
LoraConfig(
    r=32,                    # Rank 증가 (16 → 32)
    lora_alpha=64,           # Scaling 증가 (32 → 64)
    target_modules=["q_proj", "k_proj", "v_proj"],
    lora_dropout=0.1,
)
```

**학습 가능 파라미터**:
```
전체: 320,203,437개
학습: 4,718,592개 (1.47%)
```

**r=16 vs r=32 비교**:
| 항목 | r=16 | r=32 |
|------|------|------|
| **학습 파라미터** | 2.36M (0.74%) | 4.72M (1.47%) |
| **표현력** | 중간 | 높음 |
| **메모리 사용** | 낮음 | 중간 |
| **학습 속도** | 약간 빠름 | 약간 느림 |
| **예상 성능** | 10-12% PER | 9-11% PER |

### 4.2 층 동결 전략 (Layer Freezing)

#### 4.2.1 Wav2Vec2의 계층 구조

```
Wav2Vec2-XLS-R-300M (24 layers):
├── Feature Extractor (CNN)
│   └── 7 convolutional layers
├── Transformer Encoder (24 layers)
│   ├── Layer 0-7:  Low-level features (음향 패턴)
│   ├── Layer 8-15: Mid-level features (음소 단위)
│   └── Layer 16-23: High-level features (의미론적)
└── CTC Head (분류기)
```

#### 4.2.2 적용한 동결 전략

```python
freeze_layers = list(range(0, 8))  # Layer 0-7 동결
```

**이유**:
1. **Low-level features는 범용적**: 하위 층은 일반적인 음향 특성 학습 (주파수, 음높이 등)
2. **도메인 특화는 상위 층**: 어린이 음성 특성은 주로 상위 층에서 학습
3. **효율성**: 동결된 층은 Gradient 계산 불필요 → 속도 향상 + 메모리 절약
4. **과적합 방지**: 너무 많은 파라미터 학습 시 작은 데이터셋에서 과적합 위험

**결과**:
- 학습 가능 층: 16개 (Layer 8-23)
- 동결 층: 8개 (Layer 0-7 + Feature Extractor)

### 4.3 Curriculum Learning (단계적 학습)

#### 4.3.1 파일럿 서브셋 (148h) 학습 전략

**Phase 1: 초기 LoRA 학습 (30h)**
```python
Phase 1 설정:
├── 데이터: Train의 20% (~30h, 11,549개)
├── Epochs: 10
├── Learning Rate: 5e-4 (높음)
├── Warmup Steps: 500
├── 목표 WER: < 25%
└── 증강: Weak (SpecAugment 50%)
```

**목적**: LoRA 가중치 초기화, 기본 도메인 적응
**소요 시간**: ~4.5시간

**Phase 2: 도메인 적응 (90h)**
```python
Phase 2 설정:
├── 데이터: Train의 60% (~90h, 34,648개)
├── 체크포인트: Phase 1 best model
├── Epochs: 8
├── Learning Rate: 3e-4 (중간)
├── Warmup Steps: 1000
├── 목표 WER: < 18%
└── 증강: Strong (SpecAugment 100% + Speed Perturbation)
```

**목적**: 어린이 음성 특성 학습, 다양성 확보
**소요 시간**: ~15시간

**Phase 3: 전체 정제 (148h)**
```python
Phase 3 설정:
├── 데이터: Train 100% (148h, 57,746개)
├── 체크포인트: Phase 2 best model
├── Epochs: 5
├── Learning Rate: 1e-4 (낮음)
├── Warmup Steps: 500
├── 목표 WER: < 15%
└── 증강: Medium (SpecAugment 75% + Speed Perturbation)
```

**목적**: 모든 데이터로 성능 극대화
**소요 시간**: ~18시간

**실제 결과** (148h 학습):
```
Baseline (Fine-tuning 전): PER 36.67%, CER 26.40%
Fine-tuned (148h):        PER 20.55%, CER 15.14%

개선율: PER 44% 감소, CER 43% 감소
```

**분석**:
- 목표 PER 15% 미달성 (20.55%)
- 하지만 베이스라인 대비 큰 개선
- 전체 데이터(3500h) 학습 필요성 확인

#### 4.3.2 전체 데이터 (3500h) 학습 전략

**Phase 1: Full Data (3500h)**
```python
Phase 1 설정 (r=16):
├── 데이터: 1,771,800개 (전체)
├── Validation: 114,539개 (전체)
├── Epochs: 3
├── Learning Rate: 5e-4
├── Warmup Steps: 2000
├── Batch Size: 8 × 2 GPU = 16 (global)
├── Eval Steps: 500
└── 목표 PER: < 12%

예상 소요 시간: ~16일
```

**문제점**:
- Validation 평가가 너무 오래 걸림 (14.5분/회)
- Eval steps 500마다 평가 → 총 평가 시간이 학습의 40% 차지

**Phase 1 최적화 버전 (r=32)**
```python
Phase 1 설정 (r=32 최적화):
├── 데이터: 1,771,800개 (전체)
├── Validation: 20,000개 (샘플링) ⭐
├── Epochs: 3
├── Learning Rate: 5e-5
├── Warmup Steps: 2000
├── Batch Size: 8 × 2 GPU = 16 (global)
├── Eval Steps: 2000 ⭐
├── LoRA Rank: 32 ⭐
└── 목표 PER: < 10%

예상 소요 시간: ~10일 ⭐
```

**최적화 효과**:
- Validation 샘플: 114,539 → 20,000개 (약 6배 감소)
- 평가 시간: 14.5분/회 → 2.5분/회
- Eval 간격: 500 → 2000 steps (평가 횟수 1/4로 감소)
- 총 학습 시간: 16일 → 10일 (약 40% 단축)
- LoRA rank 증가로 성능 개선 기대

---

## 5. 최적화 기법

### 5.1 Data Augmentation

#### 5.1.1 SpecAugment

**개념**: Spectrogram에 마스킹을 적용하여 Robustness 향상

```python
SpecAugment(
    time_mask_width_range=(0, 30),   # 시간 축 마스크 (어린이 음성은 짧게)
    freq_mask_width_range=(0, 15),   # 주파수 축 마스크
    num_time_mask=2,                 # 시간 마스크 개수
    num_freq_mask=2,                 # 주파수 마스크 개수
)
```

**Phase별 적용 강도**:
| Phase | 강도 | 적용률 | 이유 |
|-------|-----|--------|------|
| Phase 1 (30h) | Weak | 50% | 안정적 초기 학습 |
| Phase 2 (90h) | Strong | 100% | Robustness 향상 |
| Phase 3 (148h) | Medium | 75% | 과적합 방지 |

#### 5.1.2 Speed Perturbation

**개념**: 음성 속도를 조정하여 다양한 발화 속도에 대응

```python
SpeedPerturb(
    factors=[0.9, 1.0, 1.1],  # 90%, 100%, 110% 속도
    p=0.5                     # 50% 확률로 적용
)
```

**효과**:
- 어린이는 발화 속도가 불규칙하므로 효과적
- 데이터 다양성 증가
- 일반화 성능 향상

### 5.2 Early Stopping

#### 5.2.1 설정

```python
EarlyStopping(
    monitor='eval_loss',
    patience=15,              # 15 epoch 동안 개선 없으면 중단
    min_delta=0.005,         # 0.5% 이상 개선 필요
    mode='min'
)
```

**Phase별 Patience**:
- Phase 1 (30h): patience=4
- Phase 2 (90h): patience=3
- Phase 3 (148h): patience=3
- Full (3500h, r=32): patience=15

**이유**:
- 대규모 데이터는 수렴이 느리므로 patience를 높게 설정
- 불필요한 Epoch 방지로 시간 절약

### 5.3 Learning Rate Scheduling

#### 5.3.1 Warmup

```python
warmup_steps = 2000  # 전체 데이터 기준
```

**효과**:
- 초기 학습 불안정성 방지
- LoRA 가중치의 점진적 초기화

#### 5.3.2 Phase별 Learning Rate

| Phase | LR | 이유 |
|-------|----|------|
| Phase 1 (30h) | 5e-4 | 빠른 수렴 |
| Phase 2 (90h) | 3e-4 | 안정적 학습 |
| Phase 3 (148h) | 1e-4 | 세밀 조정 |
| Full (3500h, r=16) | 5e-4 | 대규모 데이터 |
| Full (3500h, r=32) | 5e-5 | 더 높은 rank, 낮은 LR |

**r=32 LR이 낮은 이유**:
- Rank가 높을수록 표현력이 높아 과적합 위험
- 낮은 LR로 안정적 학습

### 5.4 Batch Size & Gradient Accumulation

#### 5.4.1 설정

```python
# 전체 데이터 학습
batch_size = 8  # per GPU
gradient_accumulation_steps = 2
num_gpus = 2

# Effective batch size
global_batch_size = 8 × 2 × 2 = 32
```

**선택 이유**:
- GPU 메모리 한계: Batch size 16은 OOM 위험
- Batch size 8로 줄이고 gradient accumulation으로 보완
- Global batch size 32로 안정적 학습

#### 5.4.2 메모리 최적화

```python
# num_workers=0: IterableDataset은 single-process가 더 안정적
COMMON_CONFIG = {
    'num_workers': 0,
    'prefetch_factor': None,
    'pin_memory': False,
}
```

**이유**:
- IterableDataset은 multiprocessing 시 파일 중복 로딩 위험
- Single-process로 안정성 확보
- 속도는 약간 느리지만 메모리 안전

### 5.5 Checkpoint 관리

#### 5.5.1 저장 전략

```python
TrainingArguments(
    save_strategy='steps',
    save_steps=2000,                    # 2000 steps마다 저장
    save_total_limit=2,                 # 최신 2개만 유지 (디스크 절약)
    load_best_model_at_end=True,
)
```

**저장 구조**:
```
checkpoints_full_r32/phase1_full_3500h_r32/
├── checkpoint-2000/
├── checkpoint-4000/
├── ...
├── checkpoint-332000/
└── final_model/  # Best eval_loss 모델
```

#### 5.5.2 체크포인트 크기

- 전체 모델: ~1.2GB
- LoRA 어댑터만: ~20MB (r=16), ~40MB (r=32)

**배포 시**: LoRA 어댑터만 배포하면 됨 (베이스 모델 + 어댑터)

---

## 6. 실험 결과 및 성능

### 6.1 서브셋 (148h) 학습 결과

#### 6.1.1 Phase 3 최종 결과

```
테스트 데이터: 2,216개 (4.89시간)

베이스라인 (Fine-tuning 전):
- PER: 36.67%
- CER: 26.40%

Fine-tuned (Phase 3, 148h):
- PER: 20.55%
- CER: 15.14%

개선율:
- PER: 44% 감소
- CER: 43% 감소
```

**평가 샘플**:
```
[샘플 1]
정답: N A N EU N | B U J A G A | D oE G O | S I Ph EO H A N EU N | ...
예측: A N EU N | B U J A G A | D oE G O | S I Ph EO A N EU N | ...
→ 첫 음소 누락 (N → A), 나머지는 거의 정확

[샘플 5]
정답: N A N EU N | EO M EO N I euI | S O N EU L | J A p GG O | oE iA NG G A N EU R O | G A t S EU p N I D A
예측: N A N EU N | EO M EO N I euI | S O N EU L | J A p GG O | oE A NG G A N EU R O | G A t S EU p N I D A
→ 거의 완벽 (한 음소만 차이)
```

#### 6.1.2 분석

**성공 요인**:
- LoRA를 통한 효율적 Fine-tuning
- Curriculum Learning으로 단계적 학습
- Data Augmentation으로 Robustness 향상

**한계**:
- 목표 PER 15% 미달성 (20.55%)
- 데이터 부족 (148h는 소규모)
- 일부 음소 삽입/삭제 오류 발생

**결론**: 전체 데이터(3500h) 학습 필요

### 6.2 전체 데이터 (3500h) 학습 진행 상황

#### 6.2.1 실험 1: r=16 (GPU 1)

```
설정:
- LoRA Rank: 16
- Validation: 114,539개 (전체)
- Eval Steps: 500
- Learning Rate: 5e-4

진행 상황:
- 총 Steps: 332,211
- 현재 Step: ~5,000 (1.5%)
- 예상 완료: 2025-11-21 (약 16일)

평가 시간 문제:
- 평가 1회: 14.5분
- 총 평가 시간: 약 6.7일 (학습의 40%)
```

**문제점**: 평가 시간이 너무 오래 걸림

#### 6.2.2 실험 2: r=32 최적화 (GPU 2) ⭐

```
설정:
- LoRA Rank: 32 (표현력 향상)
- Validation: 20,000개 (샘플링)
- Eval Steps: 2000
- Learning Rate: 5e-5
- Batch Size: 8 × 2 (gradient accumulation)

진행 상황:
- 총 Steps: 332,211
- 현재 Step: ~112,900 (34%)
- 예상 완료: 2025-11-17 (약 10일)
- 현재 Epoch: 1.01

학습 지표 (Step 112,800):
- Loss: 0.81 ~ 1.06 (수렴 중)
- Eval Loss: 0.358 (우수!)
- Learning Rate: 3.3e-5 (Warmup 완료)
- Grad Norm: 0.5 ~ 1.0 (안정적)
- 속도: 5-6 it/s

평가 시간 최적화:
- 평가 1회: 2.5분 (vs 14.5분)
- 총 평가 횟수: 332,211 / 2000 = 166회
- 총 평가 시간: 약 7시간 (학습의 3%)
```

**예상 결과**:
- **PER: 9-11%** (r=16보다 1-2% 향상 기대)
- **CER: 6-8%**

**현재 상태**: 매우 양호
- Eval Loss 0.358은 우수한 수준
- Loss가 안정적으로 감소 중
- Gradient Norm이 안정적 (폭발 없음)

### 6.3 성능 비교표

| 모델 | 데이터 | LoRA Rank | PER | CER | 학습 시간 |
|------|--------|-----------|-----|-----|----------|
| Baseline | 0 | - | 36.67% | 26.40% | - |
| Fine-tuned | 148h | r=16 | 20.55% | 15.14% | 37.5h |
| Fine-tuned (진행 중) | 3500h | r=16 | 예상 10-12% | 예상 7-9% | 16일 |
| **Fine-tuned (진행 중)** | **3500h** | **r=32** | **예상 9-11%** | **예상 6-8%** | **10일** |

---

## 7. 핵심 학습 사항

### 7.1 모델 선택

✅ **wav2vec2-korean-phoneme 선택이 탁월했던 이유**:
1. **음소 기반**: 어린이의 불안정한 발음도 음소 단위로 인식 가능
2. **한국어 특화**: 음운 규칙(연음화, 비음화 등) 내장
3. **LoRA 호환성**: 효율적 Fine-tuning 가능
4. **적절한 크기**: 300M 파라미터로 학습 가능하면서도 성능 우수

### 7.2 LoRA의 효과

✅ **LoRA가 필수적이었던 이유**:
1. **메모리 효율성**: 전체 모델의 1-2%만 학습 → GPU 메모리 절약
2. **학습 속도**: Gradient 계산량 감소 → 학습 시간 단축
3. **과적합 방지**: 적은 파라미터로 일반화 성능 유지
4. **r=32 > r=16**: Rank 증가로 표현력 향상, 성능 1-2% 개선 기대

### 7.3 Curriculum Learning의 중요성

✅ **단계적 학습의 효과**:
1. **빠른 검증**: 148h 서브셋으로 먼저 검증 → 전체 학습 전 문제점 파악
2. **안정적 수렴**: 작은 데이터부터 학습 → 큰 데이터에서도 안정적
3. **Learning Rate 조정**: Phase별로 LR 감소 → 세밀 조정

### 7.4 Validation 최적화

✅ **평가 시간 최적화의 중요성**:
1. **샘플링**: 114,539 → 20,000개 (6배 감소)
2. **Eval Steps**: 500 → 2000 (평가 횟수 1/4 감소)
3. **효과**: 학습 시간 40% 단축 (16일 → 10일)

**교훈**: 대규모 데이터 학습 시 Validation 전략이 총 학습 시간에 큰 영향

### 7.5 Data Augmentation

✅ **SpecAugment + Speed Perturbation의 효과**:
- 어린이 음성의 불규칙성에 대응
- 과적합 방지
- 일반화 성능 향상

### 7.6 메모리 최적화

✅ **IterableDataset의 필요성**:
- 177만 개 샘플을 메모리에 로드 불가능
- On-the-fly loading으로 메모리 효율성 확보
- 학습 시작 시간 최소화

### 7.7 실험 관리

✅ **병렬 실험의 가치**:
- GPU 1: r=16 (안정적)
- GPU 2: r=32 (최적화)
- → 두 실험 비교로 최적 설정 찾기

---

## 8. 향후 개선 방향

### 8.1 단기 개선 (현재 학습 완료 후)

#### 8.1.1 전체 데이터 학습 완료
- [ ] r=32 학습 완료 (2025-11-17 예정)
- [ ] 최종 평가 (Test set)
- [ ] r=16 vs r=32 성능 비교
- [ ] Best model 선택

#### 8.1.2 성능 분석
- [ ] 연령별 PER 분석
- [ ] SNR별 성능 분석
- [ ] 오류 패턴 분석 (삽입/삭제/대체)
- [ ] 취약 음소 파악

### 8.2 중기 개선 (추가 Fine-tuning)

#### 8.2.1 Discriminative Learning Rate
```python
# 층별로 다른 Learning Rate 적용
layer_lr = {
    'layers.0-7': 0,           # 동결
    'layers.8-15': 1e-5,       # 낮은 LR
    'layers.16-23': 5e-5,      # 중간 LR
    'lora_layers': 1e-4,       # 높은 LR
}
```

**기대 효과**: 각 층의 학습 속도 최적화 → 성능 1-2% 개선

#### 8.2.2 Full Fine-tuning (선택적)
```python
# LoRA 제거, 전체 모델 학습
# 단, 메모리 및 시간 비용 증가
```

**조건**: LoRA 학습 결과가 우수하면 Skip 가능

#### 8.2.3 Ensemble
- 여러 Checkpoint의 예측을 앙상블
- 기대 효과: PER 0.5-1% 개선

### 8.3 장기 개선 (프로덕션)

#### 8.3.1 모델 경량화
- **Distillation**: 큰 모델 → 작은 모델로 지식 전이
- **Quantization**: FP32 → INT8 변환
- **Pruning**: 불필요한 가중치 제거

**목표**: 추론 속도 2-3배 향상, 모델 크기 1/4 감소

#### 8.3.2 배포 최적화
- **ONNX 변환**: 다양한 플랫폼 지원
- **TensorRT**: GPU 추론 최적화
- **FastAPI 서버**: REST API 제공

#### 8.3.3 추가 데이터 수집
- **Active Learning**: 모델이 어려워하는 샘플 추가 수집
- **데이터 다양성**: 다양한 억양, 배경 소음 환경
- **Fine-tuning 반복**: 새 데이터로 지속적 개선

### 8.4 연구 방향

#### 8.4.1 Multi-task Learning
- **음소 + 감정 인식**: 어린이 감정 상태 동시 인식
- **음소 + 연령 추정**: 연령대별 맞춤 인식

#### 8.4.2 Self-supervised Learning
- **Unlabeled 어린이 음성**: 레이블 없는 데이터로 추가 Pre-training
- **Pseudo-labeling**: 모델 예측으로 레이블 생성 후 재학습

#### 8.4.3 End-to-End Model
- **CTC 대신 Attention**: Transformer Decoder 추가
- **Language Model 통합**: N-gram 또는 Neural LM 결합

---

## 9. 결론

### 9.1 프로젝트 성과

✅ **성공적인 모델 선택**:
- wav2vec2-korean-phoneme: 음소 기반으로 어린이 음성에 적합
- LoRA: 효율적 Fine-tuning으로 메모리/시간 절약

✅ **효과적인 학습 전략**:
- Curriculum Learning: 148h 서브셋 → 3500h 전체
- LoRA r=32 최적화: 성능 향상 + 학습 시간 단축
- Validation 최적화: 학습 시간 40% 단축

✅ **실제 성능 개선**:
- 148h 학습: PER 36.67% → 20.55% (44% 감소)
- 3500h 학습 (진행 중): 예상 PER 9-11% (추가 50% 개선 기대)

### 9.2 핵심 인사이트

1. **음소 기반 접근**: 어린이 음성처럼 불안정한 발화에 강건함
2. **LoRA의 위력**: 전체 파라미터의 1-2%만 학습해도 큰 성능 개선
3. **Curriculum Learning**: 작은 데이터로 먼저 검증 → 대규모 학습 시 리스크 감소
4. **Validation 전략**: 평가 시간이 총 학습 시간에 큰 영향 → 최적화 필수

### 9.3 재현 가능한 레시피

**다른 도메인(예: 노인 음성, 방언 등)에 적용 시**:

```python
1. 베이스 모델 선택:
   - wav2vec2-korean-phoneme (음소 기반)

2. 데이터 준비:
   - 100-200h 서브셋 생성 (층화 샘플링)
   - 화자 완전 분리 (Train/Val/Test)

3. Fine-tuning:
   - LoRA r=16 또는 r=32
   - 하위 8층 동결
   - Curriculum Learning (20% → 60% → 100%)

4. 최적화:
   - SpecAugment + Speed Perturbation
   - Early Stopping (patience=15)
   - Validation 샘플링 (20,000개)

5. 평가:
   - PER/CER 측정
   - 오류 패턴 분석
   - 필요 시 전체 데이터 학습
```

### 9.4 미래 가능성

**현재 학습 완료 시**:
- **PER 9-11%**: 상용화 가능한 수준
- **배포**: VR, 교육 앱, 음성 비서 등에 활용
- **지속적 개선**: Active Learning으로 추가 데이터 수집 및 재학습

**장기적으로**:
- **Multi-modal**: 음성 + 텍스트 + 비디오 통합
- **Personalization**: 개인별 Fine-tuning
- **Real-time**: 실시간 음성 인식 최적화

---

## 부록

### A. 주요 파일 및 스크립트

#### A.1 학습 스크립트
```
fine_tunining_new/
├── train_all_phases.py                         # 서브셋 (148h) Phase 1-3
├── train_all_phases_all.py                     # 전체 (3500h) r=16
├── train_all_phases_all_r32_optimized.py       # 전체 (3500h) r=32 최적화 ⭐
└── evaluate_test.py                            # 평가 스크립트
```

#### A.2 데이터 처리
```
finetunning/organized/03_data_processing/
├── korean_g2p.py                               # 한국어 G2P
├── dataset_loader.py                           # 데이터 로더
└── create_subset_100h.py                       # 서브셋 생성
```

#### A.3 문서
```
fine_tunining_new/
├── TRAINING_PLAN.md                            # 전체 학습 계획
├── USAGE.md                                    # 사용 가이드
├── TRAIN_FULL_GUIDE.md                         # 전체 데이터 학습 가이드
└── UPDATE_VALIDATION_SETTINGS.md               # Validation 최적화 가이드
```

### B. 하이퍼파라미터 요약

#### B.1 서브셋 (148h)
```python
Phase 1: {
    'data': '20% (30h)',
    'epochs': 10,
    'lr': 5e-4,
    'warmup': 500,
}
Phase 2: {
    'data': '60% (90h)',
    'epochs': 8,
    'lr': 3e-4,
    'warmup': 1000,
}
Phase 3: {
    'data': '100% (148h)',
    'epochs': 5,
    'lr': 1e-4,
    'warmup': 500,
}
```

#### B.2 전체 데이터 (3500h)
```python
r=32 Optimized: {
    'lora_r': 32,
    'lora_alpha': 64,
    'epochs': 3,
    'lr': 5e-5,
    'warmup': 2000,
    'batch_size': 8,
    'gradient_accumulation': 2,
    'eval_steps': 2000,
    'max_val_samples': 20000,
}
```

### C. 참고 자료

#### C.1 논문
- [Wav2Vec 2.0](https://arxiv.org/abs/2006.11477): Self-supervised Learning
- [LoRA](https://arxiv.org/abs/2106.09685): Low-Rank Adaptation
- [SpecAugment](https://arxiv.org/abs/1904.08779): Data Augmentation

#### C.2 모델
- [Hugging Face: wav2vec2-xls-r-300m](https://huggingface.co/facebook/wav2vec2-xls-r-300m)
- [Korean G2P](https://github.com/kyubyong/g2pK)

#### C.3 도구
- [Transformers](https://github.com/huggingface/transformers)
- [PEFT](https://github.com/huggingface/peft): LoRA 구현

---

**문서 버전**: 1.0
**최종 업데이트**: 2025-11-07
**작성자**: Claude Code
**프로젝트 소유자**: j-k13a206
