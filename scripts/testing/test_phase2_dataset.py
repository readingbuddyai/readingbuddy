#!/usr/bin/env python3
"""Phase 2 데이터셋 로딩 테스트"""

import sys
sys.path.append('/home/j-k13a206/finetunning')

from pathlib import Path
from transformers import Wav2Vec2Processor
from korean_g2p import KoreanG2P
import torch

# train_all_phases.py에서 클래스 임포트
sys.path.append('/home/j-k13a206/fine_tunining_new')
from train_all_phases import StreamingChildSpeechDataset, DataCollatorCTCWithPadding

print("=" * 80)
print("📊 Phase 2 데이터셋 테스트")
print("=" * 80)

# Processor 로드
print("\n1. Processor 로드 중...")
processor = Wav2Vec2Processor.from_pretrained('/home/j-k13a206/models/wav2vec2-korean-phoneme')
g2p = KoreanG2P()
print("✅ Processor 로드 완료")

# 데이터셋 생성
print("\n2. Phase 2 데이터셋 생성 (train_ratio=0.6)...")
train_dataset = StreamingChildSpeechDataset(
    data_dir='/home/j-k13a206/data/child_subset_100h',
    split="1.Training",
    processor=processor,
    g2p=g2p,
    train_ratio=0.6,
)
print("✅ 데이터셋 생성 완료")

# 샘플 로드 테스트
print("\n3. 첫 3개 샘플 로드 테스트...")
iterator = iter(train_dataset)
samples = []
for i in range(3):
    try:
        sample = next(iterator)
        samples.append(sample)
        print(f"  샘플 {i+1}: input_values shape = {sample['input_values'].shape}, labels len = {len(sample['labels'])}")
    except StopIteration:
        print(f"  ❌ 샘플 {i+1}: StopIteration (데이터셋 끝)")
        break
    except Exception as e:
        print(f"  ❌ 샘플 {i+1}: {type(e).__name__}: {e}")
        break

if not samples:
    print("\n❌ 샘플을 하나도 로드하지 못했습니다!")
    sys.exit(1)

print(f"\n✅ {len(samples)}개 샘플 로드 성공")

# DataCollator 테스트
print("\n4. DataCollator 테스트...")
collator = DataCollatorCTCWithPadding(processor=processor)
try:
    batch = collator(samples)
    print(f"✅ Batch 생성 성공")
    print(f"   input_values: {batch['input_values'].shape}")
    print(f"   labels: {batch['labels'].shape}")
except Exception as e:
    print(f"❌ Batch 생성 실패: {type(e).__name__}: {e}")
    sys.exit(1)

print("\n" + "=" * 80)
print("✅ 모든 테스트 통과!")
print("=" * 80)
