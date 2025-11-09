# 🚀 train_all_phases.py 사용 가이드

## 📋 개요

전체 Phase (1→2→3)를 하나의 파일로 실행하는 통합 학습 스크립트

**특징**:
- ✅ LoRA (r=16) + 하위 8층 동결
- ✅ Phase 간 자동 체크포인트 연결
- ✅ 화자 분리 데이터 (Train/Val/Test)
- ✅ 30h → 90h → 148h Curriculum Learning
- ✅ EarlyStopping + 상세 로깅

---

## 🎯 실행 방법

### 옵션 1: Phase 1만 실행 (추천 - 먼저 테스트)

```bash
cd /home/j-k13a206/fine_tunining_new

python3 train_all_phases.py --phase 1 --gpu 1
```

**예상 시간**: ~4.5시간
**목표 WER**: < 25%

---

### 옵션 2: Phase 2 실행 (Phase 1 완료 후)

```bash
python3 train_all_phases.py \
  --phase 2 \
  --resume_from ./checkpoints/phase1_30h/checkpoint-best \
  --gpu 1
```

**예상 시간**: ~15시간
**목표 WER**: < 18%

---

### 옵션 3: Phase 3 실행 (Phase 2 완료 후)

```bash
python3 train_all_phases.py \
  --phase 3 \
  --resume_from ./checkpoints/phase2_90h/checkpoint-best \
  --gpu 1
```

**예상 시간**: ~18시간
**목표 WER**: < 15% ⭐

---

### 옵션 4: 전체 자동 실행 (1→2→3)

```bash
# 백그라운드 실행 (추천)
nohup python3 train_all_phases.py --phase all --gpu 1 > training_all.log 2>&1 &

# 로그 확인
tail -f training_all.log
```

**예상 총 시간**: ~37.5시간
**주의**: 중간에 에러 발생 시 해당 Phase만 재실행 가능

---

## 📂 출력 구조

```
fine_tunining_new/
├── checkpoints/
│   ├── phase1_30h/
│   │   ├── checkpoint-best/          # Best 모델 (Val loss 최저)
│   │   ├── final_model/              # 마지막 epoch 모델
│   │   └── phase1_30h_training.log   # 학습 로그
│   ├── phase2_90h/
│   │   ├── checkpoint-best/
│   │   ├── final_model/
│   │   └── phase2_90h_training.log
│   └── phase3_148h/
│       ├── checkpoint-best/
│       ├── final_model/
│       └── phase3_148h_training.log
└── train_all_phases.py
```

---

## 📊 모니터링

### 실시간 로그 확인

```bash
# Phase 1 로그
tail -f checkpoints/phase1_30h/phase1_30h_training.log

# 전체 실행 로그 (--phase all 사용 시)
tail -f training_all.log
```

### GPU 사용률 확인

```bash
watch -n 1 nvidia-smi
```

### 학습 진행 상황

```bash
# Trainer 로그 (자동 생성)
ls -lh checkpoints/phase1_30h/checkpoint-*/
```

---

## 🎛️ 커스터마이징

### GPU 변경

```bash
python3 train_all_phases.py --phase 1 --gpu 0  # GPU 0 사용
python3 train_all_phases.py --phase 1 --gpu 2  # GPU 2 사용
```

### Batch Size 변경

`train_all_phases.py` 파일 수정:

```python
COMMON_CONFIG = {
    'batch_size': 8,  # 16 → 8 (메모리 부족 시)
    ...
}
```

### Learning Rate 변경

```python
PHASE_CONFIG = {
    1: {
        'learning_rate': 3e-4,  # 5e-4 → 3e-4 (더 안정적)
        ...
    }
}
```

### Epoch 수 조정

```python
PHASE_CONFIG = {
    1: {
        'epochs': 5,  # 10 → 5 (빠른 테스트)
        ...
    }
}
```

---

## 🐛 문제 해결

### 1. CUDA Out of Memory

**해결책**:
```python
# Batch size 줄이기
'batch_size': 8,  # 또는 4

# Gradient accumulation 사용
'gradient_accumulation_steps': 2,
```

### 2. Phase 2/3 실행 시 체크포인트 에러

**증상**: `--resume_from` 경로가 없다는 오류

**해결책**:
```bash
# 체크포인트 경로 확인
ls -la checkpoints/phase1_30h/checkpoint-best/

# 올바른 경로 지정
python3 train_all_phases.py \
  --phase 2 \
  --resume_from checkpoints/phase1_30h/checkpoint-best
```

### 3. 학습이 너무 느림

**원인**: DataLoader workers 부족

**해결책**:
```python
COMMON_CONFIG = {
    'num_workers': 64,  # 48 → 64 (CPU 여유 있으면)
    'prefetch_factor': 16,  # 10 → 16
}
```

### 4. Eval Loss가 줄지 않음

**증상**: Val loss가 계속 높거나 증가

**해결책**:
- Learning rate 줄이기 (5e-4 → 3e-4)
- 데이터 품질 재확인
- Phase 1이 잘 수렴했는지 확인 (< 25% WER)

---

## ✅ 체크리스트

### 실행 전
- [ ] `/home/j-k13a206/data/child_subset_100h` 존재
- [ ] `1.Training/` 디렉토리 확인
- [ ] `2.Validation_split/` 디렉토리 확인 (Val용)
- [ ] GPU 메모리 16GB 이상
- [ ] 디스크 공간 100GB 이상
- [ ] `peft` 패키지 설치: `pip install peft`

### Phase 1 완료 후
- [ ] Val loss < 0.5
- [ ] WER < 25%
- [ ] `checkpoint-best/` 존재

### Phase 2 완료 후
- [ ] Val loss < 0.3
- [ ] WER < 18%
- [ ] Phase 1보다 개선됨

### Phase 3 완료 후
- [ ] Val loss < 0.2
- [ ] WER < 15% ⭐
- [ ] 최종 모델 저장 완료

---

## 🎓 다음 단계

### Phase 3 성공 시 (WER < 15%)

```bash
# 1. Test 셋으로 최종 평가
python3 evaluate_test.py \
  --model checkpoints/phase3_148h/checkpoint-best \
  --test_dir /home/j-k13a206/data/child_subset_100h/3.Test

# 2. 3500h 전체 데이터로 확장
# (별도 스크립트 필요)
```

### Phase 3 실패 시 (WER > 18%)

1. 데이터 품질 재확인
2. Learning rate 조정
3. Phase 1부터 재실행
4. 서브셋 재샘플링 고려

---

## 📞 참고

**학습 설정 요약**:

| Phase | 데이터 | Epochs | LR | 시간 | 목표 WER |
|-------|--------|--------|-----|------|---------|
| 1 | 30h | 10 | 5e-4 | 4.5h | <25% |
| 2 | 90h | 8 | 3e-4 | 15h | <18% |
| 3 | 148h | 5 | 1e-4 | 18h | <15% |

**LoRA 설정**:
- r=16, alpha=32
- target: q_proj, k_proj, v_proj
- 하위 8층 동결 (0~7)

**데이터 분포**:
- Train: 136.19h (63명 화자)
- Val: 7.78h (9명 화자)
- Test: 4.89h (10명 화자)
- 화자 중복: 0명 ✅

---

**Good luck! 🚀**
