# 🚀 전체 데이터(3500h) LoRA 학습 가이드

## 📋 개요

**전체 1,771,800개 샘플**로 Wav2Vec2-Korean-Phoneme 모델을 LoRA 파인튜닝합니다.

**데이터 분포:**
- Training: 1,771,800개 (전체 사용)
- Validation: 114,539개 (358명 화자)
- Test: 100,660개 (358명 화자)
- **화자 중복: 0명** ✅

---

## 🎯 학습 설정

### Phase 1: Full Data (3500h)

```python
{
    'name': 'phase1_full_3500h',
    'epochs': 3,
    'learning_rate': 5e-4,
    'batch_size': 16,
    'warmup_steps': 2000,
    'target_per': 12%,
}
```

### LoRA 설정
- **r**: 16
- **alpha**: 32
- **target_modules**: q_proj, k_proj, v_proj
- **freeze_layers**: 0-7층 (하위 8층 동결)

### 예상 시간
- **총 Steps**: ~332,000 steps (3 epochs)
- **예상 시간**: 10-14일 (GPU 3 기준)
- **1 epoch**: ~3.5-4.5일

---

## 🚀 실행 방법

### 1️⃣ 백그라운드 실행 (권장)

```bash
cd /home/j-k13a206/fine_tunining_new

# nohup으로 백그라운드 실행
nohup python3 train_all_phases_all.py \
  --phase 1 \
  --gpu 3 > training_full_3500h.log 2>&1 &

# 프로세스 ID 확인
echo $!
```

### 2️⃣ 로그 모니터링

```bash
# 실시간 로그 확인
tail -f training_full_3500h.log

# 학습 진행률 확인 (grep으로 필터링)
tail -f training_full_3500h.log | grep -E "loss|eval"

# GPU 사용률 확인
watch -n 1 nvidia-smi
```

### 3️⃣ 학습 상태 확인

```bash
# 체크포인트 확인
ls -lh checkpoints_full/phase1_full_3500h/

# 최신 체크포인트
ls -lt checkpoints_full/phase1_full_3500h/ | head -10

# 로그 파일 확인
cat checkpoints_full/phase1_full_3500h/phase1_full_3500h_training.log
```

---

## 📊 모니터링 지표

### 확인할 지표:
1. **Train Loss**: 감소 추세 (목표: < 0.5)
2. **Eval Loss**: 감소 추세 (목표: < 0.3)
3. **Learning Rate**: Warmup 후 안정화
4. **Steps/sec**: ~1-2 steps/sec

### 정상 학습 예시:
```
Step 500   | Train Loss: 1.52 | Eval Loss: 1.35 | LR: 2.5e-4
Step 1000  | Train Loss: 1.12 | Eval Loss: 0.98 | LR: 5e-4
Step 5000  | Train Loss: 0.65 | Eval Loss: 0.58 | LR: 5e-4
Step 10000 | Train Loss: 0.42 | Eval Loss: 0.39 | LR: 5e-4
```

---

## 🛠️ 중간 평가 (Optional)

학습 중 중간 체크포인트로 성능 확인:

```bash
# 10,000 step 체크포인트로 평가
python3 evaluate_test.py \
  --model checkpoints_full/phase1_full_3500h/checkpoint-10000 \
  --test_dir /home/j-k13a206/data/child_extracted/3.Test \
  --gpu 3 \
  --output results/checkpoint_10k_results.json
```

---

## ⚠️ 문제 해결

### 1. CUDA Out of Memory

**증상**: RuntimeError: CUDA out of memory

**해결책**:
```python
# train_all_phases_all.py 수정
COMMON_CONFIG = {
    'batch_size': 8,  # 16 → 8
    'gradient_accumulation_steps': 2,  # 1 → 2
}
```

### 2. 학습이 너무 느림

**증상**: 1 step > 5초

**원인**: 데이터 로딩 병목

**해결책**: 이미 최적화됨 (num_workers=0, IterableDataset)

### 3. Loss가 줄지 않음

**증상**: 5000 steps 후에도 Loss > 1.0

**해결책**:
- Learning rate 줄이기: 5e-4 → 3e-4
- Warmup steps 늘리기: 2000 → 3000
- 데이터 품질 재확인

### 4. 학습 중단 및 재개

**중단된 경우**:
```bash
# 최신 체크포인트 확인
ls -lt checkpoints_full/phase1_full_3500h/checkpoint-* | head -1

# 해당 체크포인트부터 재개
python3 train_all_phases_all.py \
  --phase 1 \
  --resume_from checkpoints_full/phase1_full_3500h/checkpoint-XXXXX \
  --gpu 3
```

---

## ✅ 최종 평가

학습 완료 후 Test 셋으로 최종 평가:

```bash
cd /home/j-k13a206/fine_tunining_new

python3 evaluate_test.py \
  --model checkpoints_full/phase1_full_3500h/final_model \
  --test_dir /home/j-k13a206/data/child_extracted/3.Test \
  --gpu 3 \
  --output results/full_3500h_final_results.json
```

**목표 성능:**
- **PER < 12%** ⭐
- **CER < 8%**

---

## 📈 예상 성능 비교

| 모델 | 데이터 | PER | CER |
|------|--------|-----|-----|
| Baseline | 0 | 36.67% | 26.40% |
| Fine-tuned (148h) | 148h | 20.55% | 15.14% |
| **Fine-tuned (3500h)** | **1.7M** | **10-12%** | **6-8%** |

---

## 📞 체크리스트

### 실행 전:
- [x] Validation 분할 완료 (114,539개)
- [x] Test 분할 완료 (100,660개)
- [x] 화자 중복 확인 (0명)
- [ ] GPU 메모리 확인 (>= 16GB)
- [ ] 디스크 공간 확인 (>= 500GB)
- [ ] `train_all_phases_all.py` 경로 확인

### 실행 중:
- [ ] 로그 파일 모니터링
- [ ] GPU 사용률 확인 (80-90%)
- [ ] Train/Eval Loss 감소 확인
- [ ] 체크포인트 저장 확인 (500 steps마다)

### 완료 후:
- [ ] Final model 저장 확인
- [ ] Test 셋 평가 완료
- [ ] PER < 12% 달성
- [ ] 결과 JSON 저장

---

## 🎓 다음 단계

### 성공 시 (PER < 12%):
1. ✅ 전체 데이터 학습 완료!
2. 📊 성능 분석 리포트 작성
3. 🚀 프로덕션 배포 준비

### 실패 시 (PER > 15%):
1. Epoch 늘리기 (3 → 5)
2. Learning rate 조정
3. 데이터 품질 재검토

---

**Good luck! 🚀**

약 2주 후 좋은 결과를 기대합니다!
