# 🎯 어린이 음성 Wav2Vec2 파인튜닝 마스터 플랜

## 📋 전체 개요

**목표**: Wav2Vec2-XLS-R-300M을 어린이 음성 100h 서브셋으로 단계별 파인튜닝

**전략**:
- ✅ 하위 6-8층 동결 + 상위층 LoRA (r=16)
- ✅ 20h → 60h → 100h 단계적 학습 (Curriculum Learning)
- ✅ SpecAugment + Speed Perturbation
- ✅ EarlyStopping + EMA
- ✅ 화자 분리 완료 (서브셋 생성 시 적용됨)

---

## 🗂️ 데이터 준비 상태

### ✅ 완료된 작업
```
/home/j-k13a206/data/child_subset_100h/
├── 1.Training/              # Train 데이터
├── 2.Validation_split/      # Validation 데이터 (학습 중 모니터링)
├── 3.Test/                  # Test 데이터 (최종 평가용, 학습 시 숨김)
├── 2.Validation/            # 원본 Validation (백업용)
├── subset_metadata.csv      # 원본 메타데이터
└── subset_metadata_with_test.csv  # Train/Val/Test 분할 메타데이터
```

**특징**:
- 층화 샘플링: 연령/SNR/길이 분포 유지
- 어려운 샘플 25% 포함 (낮은 SNR, 긴 발화)
- **화자 완전 분리**: Train/Val/Test 간 화자 중복 0명

### 📊 최종 데이터 분포 (실제)

| Split | 샘플 수 | 시간 | 화자 수 | 비율 |
|-------|---------|------|---------|------|
| **Train** | 57,746개 | **136.19h** | 63명 | 91.5% |
| **Val** | 2,715개 | **7.78h** | 9명 | 5.2% |
| **Test** | 2,216개 | **4.89h** | 10명 | 3.3% |
| **전체** | 62,677개 | **148.86h** | 82명 | 100% |

**주요 변경사항**:
- 원래 목표 100h → 실제 148.86h (화자 단위 선택으로 인한 초과)
- 이는 **정상적이며 장점**:
  - 화자 분리 완벽 유지
  - 더 많은 학습 데이터
  - 여전히 3500h의 4.3%로 빠른 파일럿 가능

### 📊 Phase별 데이터 분할 계획 (수정됨)

```python
# 148h 기준으로 재계산
Phase 1: 30h   (20% of 148h - 초기 학습용)
Phase 2: 90h   (60% of 148h - 도메인 적응용)
Phase 3: 148h  (100% - 전체 정제용)
```

**Phase별 구현 방식**:
- **Phase 1**: Train 데이터의 첫 20% 사용 (파일 순서대로)
- **Phase 2**: Train 데이터의 첫 60% 사용
- **Phase 3**: Train 데이터 100% 사용
- **Val/Test**: 모든 Phase에서 동일하게 유지 (7.78h + 4.89h)

---

## 🏗️ 모델 아키텍처

### 베이스 모델
- **모델**: `facebook/wav2vec2-xls-r-300m`
- **총 층 수**: 24층
- **파라미터**: 약 300M

### LoRA 설정
```python
LoraConfig(
    r=16,                    # Rank
    lora_alpha=32,           # Scaling (r × 2)
    target_modules=[
        "q_proj",            # Query projection
        "k_proj",            # Key projection
        "v_proj",            # Value projection
    ],
    lora_dropout=0.1,
    bias="none",
    task_type="CAUSAL_LM"    # CTC 헤드용
)
```

### 층 동결 전략
```python
# 옵션 A: 8층 동결 (추천)
freeze_layers = list(range(0, 8))    # 0~7층 동결
trainable_layers = 16                # 상위 16층 학습

# 옵션 B: 12층 동결 (빠른 실험용)
freeze_layers = list(range(0, 12))   # 0~11층 동결
trainable_layers = 12                # 상위 12층 학습
```

---

## 📅 Phase별 학습 계획

### Phase 1: 초기 LoRA 학습 (30h)
**목표**: LoRA 가중치 초기화, 기본 도메인 적응

| 항목 | 값 |
|-----|---|
| 데이터 | 30h (Train의 20%) |
| Train 샘플 | ~11,549개 (57,746의 20%) |
| Val 샘플 | 2,715개 (7.78h) |
| Epoch | 10 |
| Learning Rate | 5e-4 |
| Batch Size | 16 |
| Warmup Steps | 500 |
| Weight Decay | 0.01 |
| Gradient Clip | 1.0 |
| **목표 WER** | **< 25%** |

**특징**:
- 높은 LR로 빠른 수렴
- LoRA만 학습 (Feature Extractor + 하위층 동결)
- SpecAugment 약하게 적용

**예상 시간**: ~4.5시간 (V100 1대 기준, 30h 기준)

---

### Phase 2: 도메인 적응 (90h)
**목표**: 어린이 음성 특성 학습, 다양성 확보

| 항목 | 값 |
|-----|---|
| 데이터 | 90h (Train의 60%) |
| Train 샘플 | ~34,648개 (57,746의 60%) |
| Val 샘플 | 2,715개 (7.78h) |
| Epoch | 8 |
| Learning Rate | 3e-4 |
| Batch Size | 16 |
| Warmup Steps | 1000 |
| Weight Decay | 0.01 |
| Gradient Clip | 1.0 |
| **목표 WER** | **< 18%** |

**특징**:
- Phase 1 체크포인트부터 시작
- 중간 LR로 안정적 학습
- SpecAugment + Speed Perturbation 적극 활용

**예상 시간**: ~15시간 (V100 1대 기준, 90h 기준)

---

### Phase 3: 전체 정제 (148h)
**목표**: 모든 데이터로 성능 극대화

| 항목 | 값 |
|-----|---|
| 데이터 | 148h (Train 전체 100%) |
| Train 샘플 | 57,746개 |
| Val 샘플 | 2,715개 (7.78h) |
| Epoch | 5 |
| Learning Rate | 1e-4 |
| Batch Size | 16 |
| Warmup Steps | 500 |
| Weight Decay | 0.01 |
| Gradient Clip | 1.0 |
| **목표 WER** | **< 15%** |

**특징**:
- Phase 2 체크포인트부터 시작
- 낮은 LR로 세밀 조정
- EarlyStopping patience=3

**예상 시간**: ~18시간 (V100 1대 기준, 148h 기준)

---

### Phase 4: 소프트 전해동 (선택사항)
**목표**: 극저 LR로 전체 모델 미세 조정

| 항목 | 값 |
|-----|---|
| 데이터 | 148h (Train 전체 100%) |
| Train 샘플 | 57,746개 |
| Val 샘플 | 2,715개 (7.78h) |
| Epoch | 2 |
| Learning Rate | 5e-5 |
| Batch Size | 16 |
| Weight Decay | 0.01 |
| **목표 WER 개선** | **0.5~1.0%** |

**주의**:
- Phase 3 Val WER이 더 떨어지지 않으면 **SKIP**
- 모든 층 해동 (과적합 위험)
- EarlyStopping patience=1

**예상 시간**: ~7시간 (V100 1대 기준, 148h 기준)

---

## 🎨 Data Augmentation

### SpecAugment
```python
SpecAugment(
    time_mask_width_range=(0, 30),   # 어린이 음성은 짧게
    freq_mask_width_range=(0, 15),
    num_time_mask=2,
    num_freq_mask=2,
)
```

### Speed Perturbation
```python
SpeedPerturb(
    factors=[0.9, 1.0, 1.1],  # 어린이는 속도 변화 큼
    p=0.5                     # 50% 확률로 적용
)
```

### Phase별 강도
| Phase | SpecAugment | Speed Perturb | 이유 |
|-------|-------------|---------------|-----|
| 1 (20h) | 약함 (50%) | 없음 | 안정적 초기 학습 |
| 2 (60h) | 강함 (100%) | 있음 (50%) | Robustness 향상 |
| 3 (100h) | 중간 (75%) | 있음 (50%) | 과적합 방지 |
| 4 (선택) | 없음 | 없음 | 순수 성능 극대화 |

---

## 📊 모니터링 메트릭

### 필수 메트릭
```python
metrics = {
    'train/loss',              # 학습 손실
    'train/lr',                # Learning Rate
    'val/loss',                # 검증 손실
    'val/wer',                 # ⭐ 핵심: Word Error Rate
    'val/cer',                 # Character Error Rate
    'grad_norm',               # Gradient 폭발 감지
    'epoch_time',              # 에폭당 소요 시간
}
```

### 추가 메트릭 (권장)
```python
advanced_metrics = {
    'val/wer_by_age',          # 연령별 WER
    'val/wer_by_snr',          # SNR별 WER
    'val/phoneme_error_rate',  # 음소 단위 에러
    'model/trainable_params',  # 학습 가능 파라미터 수
}
```

---

## 🔧 EarlyStopping 전략

### Phase별 설정
```python
# Phase 1 (20h, 10 epoch)
EarlyStopping(
    monitor='val_wer',
    patience=4,              # 4 epoch 개선 없으면 중단
    min_delta=0.01,          # 1% 이상 개선
    mode='min'
)

# Phase 2 (60h, 8 epoch)
EarlyStopping(
    monitor='val_wer',
    patience=3,
    min_delta=0.005,         # 0.5% 이상 개선
    mode='min'
)

# Phase 3 (100h, 5 epoch)
EarlyStopping(
    monitor='val_wer',
    patience=3,
    min_delta=0.005,
    mode='min'
)

# Phase 4 (선택, 2 epoch)
EarlyStopping(
    monitor='val_wer',
    patience=1,              # 1 epoch만 기다림
    min_delta=0.002,         # 0.2% 이상 개선
    mode='min'
)
```

---

## 🎯 성공 기준

### Phase별 목표 WER
| Phase | 데이터 | 목표 WER | 최소 허용 | 판단 기준 |
|-------|--------|---------|---------|---------|
| Phase 1 (30h) | 30h | < 25% | < 30% | 기본 학습 성공 |
| Phase 2 (90h) | 90h | < 18% | < 22% | 도메인 적응 성공 |
| Phase 3 (148h) | 148h | < 15% | < 18% | 파일럿 성공 ⭐ |
| Phase 4 (선택) | 148h | < 14% | < 15% | 추가 개선 |

### Phase 3 성공 시 (WER < 15%)
→ **3500h 전체 데이터로 확장 가능!**

### Phase 3 실패 시 (WER > 18%)
→ **데이터 품질 점검 필요**
- 전사 정확도 재확인
- 노이즈 레벨 점검
- 서브셋 재샘플링 고려

---

## ⏱️ 총 예상 시간 (수정됨)

| Phase | 시간 (V100 1대) | 누적 |
|-------|----------------|------|
| Phase 1 (30h) | ~4.5시간 | 4.5h |
| Phase 2 (90h) | ~15시간 | 19.5h |
| Phase 3 (148h) | ~18시간 | 37.5h |
| Phase 4 (선택) | ~7시간 | 44.5h |
| **총합 (Phase 1~3)** | **~37.5시간** | - |

**참고**: 원래 계획 대비 약 50% 시간 증가 (100h → 148h)
하지만 여전히 3500h 학습(400~600h) 대비 **1/10 수준**

---

## 🗃️ 체크포인트 관리

### 저장 전략
```
fine_tunining_new/checkpoints/
├── phase1_20h/
│   ├── best_model/                    # Best WER 모델
│   │   ├── pytorch_model.bin
│   │   ├── config.json
│   │   └── preprocessor_config.json
│   ├── final_model/                   # 마지막 epoch
│   └── training_log.json
├── phase2_60h/
│   ├── best_model/
│   ├── final_model/
│   └── training_log.json
├── phase3_100h/
│   ├── best_model/
│   ├── final_model/
│   └── training_log.json
└── phase4_full_finetune/ (선택)
    ├── best_model/
    ├── final_model/
    └── training_log.json
```

### 저장 조건
- **Best Model**: Val WER 최저 갱신 시
- **Final Model**: 각 Phase 마지막 epoch
- **중간 체크포인트**: 매 epoch (디스크 여유 있으면)

---

## 📈 100h → 3500h 확장 전략

### 옵션 A: Direct Scale-Up (추천)
```
Phase 3 (100h, WER 15%)
  ↓
3500h 전체 학습
  - Epoch: 3~5
  - LR: 1e-4 → 5e-5 (감소)
  - 동일한 augmentation
  ↓
예상 WER: 12~14%
```

### 옵션 B: Curriculum Learning
```
100h (WER 15%)
  ↓
500h (WER 14%)
  ↓
1500h (WER 13%)
  ↓
3500h (WER 12%)
```

### 옵션 C: Active Learning
```
100h 모델로 3500h 예측
  ↓
높은 WER 샘플 우선 학습
  ↓
점진적 확장
```

---

## 🚀 실행 순서

### Step 1: 데이터 분할
```bash
python prepare_phase_data.py \
  --input_dir /home/j-k13a206/data/child_subset_100h \
  --output_dir /home/j-k13a206/fine_tunining_new/data \
  --phase1_ratio 0.2 \
  --phase2_ratio 0.6 \
  --phase3_ratio 1.0
```

### Step 2: Phase 1 실행
```bash
python train_phase1.py \
  --data_dir ./data/phase1_20h \
  --output_dir ./checkpoints/phase1_20h \
  --epochs 10 \
  --lr 5e-4 \
  --batch_size 16
```

### Step 3: Phase 2 실행
```bash
python train_phase2.py \
  --data_dir ./data/phase2_60h \
  --resume_from ./checkpoints/phase1_20h/best_model \
  --output_dir ./checkpoints/phase2_60h \
  --epochs 8 \
  --lr 3e-4
```

### Step 4: Phase 3 실행
```bash
python train_phase3.py \
  --data_dir ./data/phase3_100h \
  --resume_from ./checkpoints/phase2_60h/best_model \
  --output_dir ./checkpoints/phase3_100h \
  --epochs 5 \
  --lr 1e-4
```

### Step 5: Phase 4 실행 (선택)
```bash
# Phase 3 결과 확인 후 결정
python train_phase4.py \
  --data_dir ./data/phase3_100h \
  --resume_from ./checkpoints/phase3_100h/best_model \
  --output_dir ./checkpoints/phase4_full_finetune \
  --epochs 2 \
  --lr 5e-5 \
  --unfreeze_all
```

---

## 📋 체크리스트

### 실행 전
- [ ] `/home/j-k13a206/data/child_subset_100h` 존재 확인
- [ ] `subset_metadata.csv`에 화자 ID 포함 확인
- [ ] GPU 메모리 확인 (최소 16GB)
- [ ] 디스크 공간 확인 (최소 100GB)
- [ ] 필요 패키지 설치 (`transformers`, `peft`, `torchaudio`)

### Phase 1 후
- [ ] Val WER < 30%
- [ ] Loss가 수렴함
- [ ] Gradient 폭발 없음
- [ ] 체크포인트 저장 완료

### Phase 2 후
- [ ] Val WER < 22%
- [ ] Phase 1보다 개선됨
- [ ] 과적합 징후 없음

### Phase 3 후
- [ ] Val WER < 18% (⭐ 핵심)
- [ ] Phase 2보다 개선됨
- [ ] 최종 모델 safetensors 저장

### 3500h 확장 전
- [ ] Phase 3 WER < 15%
- [ ] 연령별/SNR별 성능 균형적
- [ ] 데이터 품질 재확인

---

## 🎓 참고 자료

- **LoRA 논문**: [LoRA: Low-Rank Adaptation](https://arxiv.org/abs/2106.09685)
- **Wav2Vec2**: [Hugging Face Wav2Vec2](https://huggingface.co/docs/transformers/model_doc/wav2vec2)
- **Curriculum Learning**: [Bengio et al. 2009](https://qmro.qmul.ac.uk/xmlui/handle/123456789/15972)

---

## 💬 FAQ

**Q: Phase 1이 너무 느려요**
A: Batch size를 8로 줄이거나, 12층 동결로 변경

**Q: Phase 3에서 WER 20% 이상이에요**
A: 데이터 품질 재점검, 서브셋 재샘플링 고려

**Q: Phase 4를 꼭 해야 하나요?**
A: 아니요. Phase 3에서 목표 달성하면 skip

**Q: 3500h는 언제 학습하나요?**
A: Phase 3 WER < 15% 달성 후, 동일 설정으로 바로 확장

---

**작성일**: 2025-11-06
**버전**: v1.0
**상태**: 준비 완료 ✅
