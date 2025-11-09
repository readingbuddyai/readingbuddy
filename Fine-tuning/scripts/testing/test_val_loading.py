#!/usr/bin/env python3
"""Validation 데이터 로딩 테스트"""

import sys
sys.path.append('/home/j-k13a206/finetunning')

from pathlib import Path
from transformers import Wav2Vec2Processor
from korean_g2p import KoreanG2P

# train_all_phases.py에서 클래스 임포트
sys.path.append('/home/j-k13a206/fine_tunining_new')
from train_all_phases import StreamingChildSpeechDataset

print("=" * 80)
print("📊 Validation 데이터셋 테스트")
print("=" * 80)

# Processor 로드
print("\n1. Processor 로드 중...")
processor = Wav2Vec2Processor.from_pretrained('/home/j-k13a206/models/wav2vec2-korean-phoneme')
g2p = KoreanG2P()
print("✅ Processor 로드 완료")

# 데이터셋 생성
print("\n2. Validation 데이터셋 생성...")
val_dataset = StreamingChildSpeechDataset(
    data_dir='/home/j-k13a206/data/child_subset_100h',
    split="2.Validation_split",
    processor=processor,
    g2p=g2p,
)
print("✅ 데이터셋 생성 완료")

# 처음 20개 샘플 로드 시도
print("\n3. 처음 20개 샘플 로드 테스트...")
iterator = iter(val_dataset)
success_count = 0
fail_count = 0

for i in range(20):
    try:
        sample = next(iterator)
        if sample is not None:
            success_count += 1
            if success_count <= 3:
                print(f"  ✅ 샘플 {i+1}: input_values={sample['input_values'].shape}, labels={len(sample['labels'])}")
        else:
            fail_count += 1
    except StopIteration:
        print(f"  ⚠️ StopIteration at sample {i+1}")
        break
    except Exception as e:
        fail_count += 1
        if fail_count <= 3:
            print(f"  ❌ 샘플 {i+1}: {type(e).__name__}: {e}")

print(f"\n📈 결과:")
print(f"  성공: {success_count}/20")
print(f"  실패: {fail_count}/20")

print(f"\n📊 통계:")
for key, value in val_dataset.stats.items():
    if value > 0:
        print(f"  {key}: {value}")

print("\n" + "=" * 80)
