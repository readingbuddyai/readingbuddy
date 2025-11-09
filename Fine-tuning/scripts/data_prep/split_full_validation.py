#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
전체 데이터셋의 Validation을 Val/Test로 50:50 분할
화자 단위로 분리하여 데이터 누수 방지
"""

import os
import json
import glob
import shutil
import random
from pathlib import Path
from collections import defaultdict
from tqdm import tqdm

# 설정
VALIDATION_DIR = Path("/home/j-k13a206/data/child_extracted/2.Validation")
OUTPUT_VAL_DIR = Path("/home/j-k13a206/data/child_extracted/2.Validation_split")
OUTPUT_TEST_DIR = Path("/home/j-k13a206/data/child_extracted/3.Test")

random.seed(42)

print("=" * 80)
print("📊 Validation 분할: Val/Test 50:50")
print("=" * 80)
print(f"입력: {VALIDATION_DIR}")
print(f"출력 Val: {OUTPUT_VAL_DIR}")
print(f"출력 Test: {OUTPUT_TEST_DIR}")
print("=" * 80)

# JSON 파일 수집
print("\n1️⃣ JSON 파일 수집 중...")
json_files = sorted(glob.glob(str(VALIDATION_DIR / "**/*.json"), recursive=True))
print(f"✅ 총 {len(json_files):,}개 파일 발견")

# 화자별 그룹화 (경로에서 추출)
print("\n2️⃣ 화자별 그룹화 중...")
speaker_groups = defaultdict(list)

for json_path in tqdm(json_files, desc="Grouping by speaker"):
    try:
        # 경로에서 화자 ID 추출 (예: .../2021-12-24/5372/K...)
        # 날짜 다음의 숫자 폴더가 화자 ID
        path_obj = Path(json_path)
        parts = path_obj.parts

        # kor_formatted 다음의 두 폴더가 date/speaker_id
        try:
            kor_idx = parts.index("kor_formatted")
            speaker_id = parts[kor_idx + 2]  # date 다음이 speaker_id
            speaker_groups[speaker_id].append(json_path)
        except (ValueError, IndexError):
            continue

    except Exception as e:
        continue

speakers = list(speaker_groups.keys())
print(f"✅ 총 {len(speakers):,}명의 화자")

# 화자를 Val/Test로 50:50 분할
print("\n3️⃣ 화자 분할 (50:50)...")
random.shuffle(speakers)
split_idx = len(speakers) // 2

val_speakers = set(speakers[:split_idx])
test_speakers = set(speakers[split_idx:])

print(f"  Val 화자: {len(val_speakers):,}명")
print(f"  Test 화자: {len(test_speakers):,}명")

# 파일 리스트 생성
val_files = []
test_files = []

for speaker_id, files in speaker_groups.items():
    if speaker_id in val_speakers:
        val_files.extend(files)
    elif speaker_id in test_speakers:
        test_files.extend(files)

print(f"\n  Val 파일: {len(val_files):,}개")
print(f"  Test 파일: {len(test_files):,}개")

# 화자 중복 체크
print("\n4️⃣ 화자 중복 체크...")
overlap = val_speakers & test_speakers
if overlap:
    print(f"❌ 경고: {len(overlap)}명의 화자가 중복됨!")
else:
    print("✅ 화자 중복 없음!")

# 파일 복사
def copy_files(file_list, output_dir, desc):
    """파일 복사 (경로 구조 유지)"""
    print(f"\n5️⃣ {desc} 복사 중...")

    for src_path in tqdm(file_list, desc=desc):
        src_path_obj = Path(src_path)

        # 상대 경로 계산
        try:
            rel_path = src_path_obj.relative_to(VALIDATION_DIR)
        except ValueError:
            print(f"⚠️ 경로 오류: {src_path}")
            continue

        # 출력 경로
        dst_json_path = output_dir / rel_path
        dst_json_path.parent.mkdir(parents=True, exist_ok=True)

        # JSON 복사
        shutil.copy2(src_path, dst_json_path)

        # WAV 파일도 복사
        try:
            with open(src_path, 'r', encoding='utf-8') as f:
                metadata = json.load(f)
            filename = metadata['File']['FileName']

            # 라벨링데이터 → 원천데이터 경로 변환
            parts = src_path_obj.parts
            labeling_idx = parts.index("라벨링데이터")
            relative_parts = parts[labeling_idx + 1:]

            src_wav_path = VALIDATION_DIR / "원천데이터" / Path(*relative_parts[:-1]) / filename
            dst_wav_path = output_dir / "원천데이터" / Path(*relative_parts[:-1]) / filename

            if src_wav_path.exists():
                dst_wav_path.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(src_wav_path, dst_wav_path)
        except Exception as e:
            # WAV 복사 실패는 무시 (JSON만 있어도 됨)
            pass

# Val 복사
copy_files(val_files, OUTPUT_VAL_DIR, "Validation")

# Test 복사
copy_files(test_files, OUTPUT_TEST_DIR, "Test")

# 결과 요약
print("\n" + "=" * 80)
print("✅ 분할 완료!")
print("=" * 80)
print(f"📁 Validation: {OUTPUT_VAL_DIR}")
print(f"   - 화자: {len(val_speakers):,}명")
print(f"   - 파일: {len(val_files):,}개")
print()
print(f"📁 Test: {OUTPUT_TEST_DIR}")
print(f"   - 화자: {len(test_speakers):,}명")
print(f"   - 파일: {len(test_files):,}개")
print()
print(f"🎯 화자 중복: {'없음 ✅' if not overlap else f'{len(overlap)}명 ⚠️'}")
print("=" * 80)

# 최종 데이터 구조
print("\n📊 최종 데이터 구조:")
print(f"  1.Training: 1,771,800개 (기존)")
print(f"  2.Validation_split: {len(val_files):,}개")
print(f"  3.Test: {len(test_files):,}개")
print(f"  Total: {1771800 + len(val_files) + len(test_files):,}개")
