# Korean Pronunciation AI Training Pipeline

> Wav2Vec2 + LoRA Fine-tuning for Korean Children's Speech Recognition

**프로젝트**: 한국어 어린이 음성 인식 AI 학습 파이프라인
**기법**: LoRA (Low-Rank Adaptation)
**베이스 모델**: facebook/wav2vec2-xls-r-300m
**데이터**: 한국어 어린이 음성 3500시간

---

## 목차

- [프로젝트 개요](#프로젝트-개요)
- [주요 특징](#주요-특징)
- [프로젝트 구조](#프로젝트-구조)
- [설치 방법](#설치-방법)
- [사용 방법](#사용-방법)
- [실험 결과](#실험-결과)
- [문서](#문서)
- [라이선스](#라이선스)

---

## 프로젝트 개요

한국어 어린이 음성에 특화된 음성 인식 모델을 개발하기 위한 학습 파이프라인입니다. Wav2Vec2 베이스 모델에 LoRA 기법을 적용하여 효율적으로 파인튜닝했습니다.

### 목표
- **주요 목표**: 어린이 음성 인식을 위한 Wav2Vec2 모델 파인튜닝
- **성능 목표**: Phoneme Error Rate (PER) < 10-12%
- **용도**: VR 환경 또는 교육 애플리케이션에서 어린이 음성 인식

### 도전 과제
- 어린이 음성 특성 (높은 음높이, 불안정한 발화)
- 대규모 데이터 (약 177만 샘플, 3500시간) 효율적 처리
- 제한된 GPU 환경에서 효율적 학습

---

## 주요 특징

### 1. 3단계 Curriculum Learning
```
Phase 1: 30h  → 초기 LoRA 학습 (목표 WER < 25%)
Phase 2: 90h  → 도메인 적응 (목표 WER < 18%)
Phase 3: 148h → 전체 정제 (목표 WER < 15%)
```

### 2. LoRA 최적화
- **r=16**: 기본 설정 (빠른 학습, 적은 메모리)
- **r=32**: 고성능 설정 (더 나은 성능, 더 많은 파라미터)
- 하위 8층 동결 전략

### 3. 화자 분리 데이터 분할
- Train/Val/Test 간 화자 중복 0명
- 층화 샘플링으로 연령/SNR/길이 분포 유지

### 4. 프로덕션 최적화
- SpecAugment + Speed Perturbation
- EarlyStopping + EMA
- 상세한 실험 결과 및 로깅

---

## 프로젝트 구조

```
Fine-tuning/
├── README.md                   # 프로젝트 개요
├── .gitignore                  # Git 제외 파일 설정
├── requirements.txt            # Python 의존성
│
├── docs/                       # 문서
│   ├── analysis.md             # 프로젝트 분석 보고서
│   ├── TRAINING_PLAN.md        # 학습 계획 문서
│   ├── USAGE.md                # 사용 가이드
│   ├── TRAIN_FULL_GUIDE.md     # 전체 학습 가이드
│   └── UPDATE_VALIDATION_SETTINGS.md
│
├── scripts/                    # 실행 스크립트
│   ├── training/               # 학습 스크립트
│   │   ├── train_all_phases.py          # Phase 1-3 학습 (메인)
│   │   ├── train_all_phases_all.py      # 전체 데이터 (r=16)
│   │   └── train_all_phases_all_r32_optimized.py  # r=32 최적화
│   │
│   ├── evaluation/             # 평가 스크립트
│   │   ├── evaluate_all_models.py       # 전체 모델 평가
│   │   ├── evaluate_test.py             # 테스트셋 평가
│   │   └── compare_base_r16_r32.py      # 모델 비교
│   │
│   ├── data_prep/              # 데이터 준비
│   │   └── split_full_validation.py     # Validation 분할
│   │
│   └── testing/                # 테스트 스크립트
│       ├── test.py
│       ├── test_dataset.py
│       ├── test_phase2_dataset.py
│       └── test_val_loading.py
│
└── results/                    # 실험 결과
    ├── comparison_results/     # 모델 비교 결과
    │   ├── summary.json
    │   ├── results_base.json
    │   ├── results_lora_r16_*.json
    │   └── results_lora_r32_*.json
    │
    ├── evaluation_results/     # 평가 결과
    │   ├── comparison_summary.json
    │   └── results_*.json
    │
    └── baseline_test_results.json
```

---

## 설치 방법

### 1. 환경 요구사항
- Python 3.10+
- CUDA 11.8+ (GPU 사용 시)
- 디스크 공간: 최소 100GB (체크포인트 저장용)
- GPU 메모리: 최소 16GB (V100 권장)

### 2. 의존성 설치
```bash
pip install torch>=2.0.0 torchaudio>=2.0.0
pip install transformers>=4.30.0 datasets>=2.12.0
pip install peft>=0.4.0
pip install librosa soundfile
pip install jiwer  # WER 계산용
```

또는 requirements.txt 사용:
```bash
pip install -r requirements.txt
```

---

## 사용 방법

### 1. 데이터 준비
```bash
# 데이터 디렉토리 구조
/path/to/data/child_subset_100h/
├── 1.Training/              # Train 데이터
├── 2.Validation_split/      # Validation 데이터
└── 3.Test/                  # Test 데이터
```

### 2. Phase별 학습 실행

#### Phase 1 (30h)
```bash
cd scripts/training
python train_all_phases.py --phase 1 --gpu 0
```

#### Phase 2 (90h)
```bash
python train_all_phases.py \
  --phase 2 \
  --resume_from ./checkpoints/phase1_30h/checkpoint-best \
  --gpu 0
```

#### Phase 3 (148h)
```bash
python train_all_phases.py \
  --phase 3 \
  --resume_from ./checkpoints/phase2_90h/checkpoint-best \
  --gpu 0
```

#### 전체 자동 실행 (Phase 1→2→3)
```bash
nohup python train_all_phases.py --phase all --gpu 0 > training.log 2>&1 &
tail -f training.log
```

### 3. 모델 평가
```bash
cd scripts/evaluation
python evaluate_test.py \
  --model /path/to/checkpoint-best \
  --test_dir /path/to/test/data
```

### 4. 모델 비교
```bash
python compare_base_r16_r32.py
```

---

## 실험 결과

### Phase별 성능 (148h 서브셋)

| Phase | 데이터 | Epochs | WER (목표) | 실제 WER | 학습 시간 |
|-------|--------|--------|-----------|---------|----------|
| Phase 1 | 30h | 10 | < 25% | 23.5% | ~4.5h |
| Phase 2 | 90h | 8 | < 18% | 17.2% | ~15h |
| Phase 3 | 148h | 5 | < 15% | 14.8% | ~18h |

### LoRA 설정 비교

| 모델 | r | 파라미터 | WER | 추론 속도 |
|-----|---|---------|-----|----------|
| Base (no LoRA) | - | 300M | 28.3% | 100% |
| LoRA r=16 | 16 | +2.1M | 14.8% | 98% |
| LoRA r=32 | 32 | +4.2M | 14.2% | 96% |

### 주요 발견사항
- Curriculum Learning이 효과적 (Phase 1→3: 23.5% → 14.8%)
- r=32가 r=16 대비 0.6% 성능 향상
- 화자 분리 데이터셋이 과적합 방지에 효과적
- 어린이 음성 특성상 성인 대비 WER 높음

---

## 문서

### 핵심 문서
- [프로젝트 분석 보고서](docs/analysis.md) - 전체 프로젝트 분석 및 학습 내용
- [학습 계획](docs/TRAINING_PLAN.md) - Phase별 학습 전략 및 하이퍼파라미터
- [사용 가이드](docs/USAGE.md) - train_all_phases.py 상세 사용법
- [전체 학습 가이드](docs/TRAIN_FULL_GUIDE.md) - 3500h 전체 데이터 학습 방법

### 주요 내용
- **LoRA 설정**: r=16/32, alpha=32, target_modules=[q_proj, k_proj, v_proj]
- **데이터 증강**: SpecAugment, Speed Perturbation
- **최적화**: EarlyStopping, Gradient Clipping, Warmup
- **평가**: WER, CER, Phoneme Error Rate

---

## 확장 계획

### 100h → 3500h 확장
현재 148h 서브셋에서 WER 14.8% 달성 후, 3500h 전체 데이터로 확장 예정:

```bash
python scripts/training/train_all_phases_all.py \
  --resume_from ./checkpoints/phase3_148h/final_model \
  --epochs 3 \
  --lr 1e-4
```

**예상 성능**: WER 12-14%

---

## 기여

이 프로젝트는 SSAFY 13기 특화 프로젝트의 일부입니다.

**개선 제안 환영**:
- 데이터 증강 기법 추가
- 다른 LoRA 설정 실험
- 평가 메트릭 개선

---

## 라이선스

### 코드
MIT License - 자유롭게 사용 가능

### 데이터
- 한국어 어린이 음성 데이터: [라이선스 확인 필요]
- AI Hub 데이터 사용 규정 준수 필요

### 베이스 모델
- facebook/wav2vec2-xls-r-300m: Apache 2.0 License

---

## 참고 자료

- **LoRA 논문**: [LoRA: Low-Rank Adaptation of Large Language Models](https://arxiv.org/abs/2106.09685)
- **Wav2Vec2**: [Hugging Face Wav2Vec2 Documentation](https://huggingface.co/docs/transformers/model_doc/wav2vec2)
- **PEFT 라이브러리**: [Hugging Face PEFT](https://github.com/huggingface/peft)

---

## 문의

프로젝트 관련 문의사항은 Issue를 통해 등록해주세요.

---

**마지막 업데이트**: 2025-11-09
**버전**: 1.0.0
