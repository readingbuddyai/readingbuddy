#!/usr/bin/env python3
"""데이터셋 로딩 테스트"""

import sys
import json
from pathlib import Path
import soundfile as sf

sys.path.append('/home/j-k13a206/finetunning')
from korean_g2p import KoreanG2P

# 테스트할 JSON 파일
test_json = "/home/j-k13a206/data/child_subset_100h/1.Training/라벨링데이터/kor_formatted/2021-10-24/9032/K08509032-AFG22-L1N2D1-I-K0KK-01380410.json"

print("=" * 80)
print("📊 데이터셋 로딩 테스트")
print("=" * 80)

# 1. JSON 읽기
print(f"\n1. JSON 파일 읽기: {test_json}")
try:
    with open(test_json, 'r', encoding='utf-8') as f:
        metadata = json.load(f)
    print("✅ JSON 로드 성공")
    print(f"   텍스트: {metadata['Transcription']['LabelText']}")
    print(f"   파일명: {metadata['File']['FileName']}")
except Exception as e:
    print(f"❌ JSON 로드 실패: {e}")
    sys.exit(1)

# 2. WAV 경로 구성
print(f"\n2. WAV 경로 구성")
filename = metadata['File']['FileName']
json_path_obj = Path(test_json)
parts = json_path_obj.parts

print(f"   JSON 경로 parts: {parts}")

try:
    labeling_idx = parts.index("라벨링데이터")
    print(f"   라벨링데이터 인덱스: {labeling_idx}")
    relative_parts = parts[labeling_idx + 1:]
    print(f"   상대 경로: {relative_parts}")
except ValueError as e:
    print(f"❌ 라벨링데이터를 찾을 수 없음: {e}")
    sys.exit(1)

# WAV 경로
data_dir = Path("/home/j-k13a206/data/child_subset_100h")
split = "1.Training"
wav_path = data_dir / split / "원천데이터" / Path(*relative_parts[:-1]) / filename

print(f"   WAV 경로: {wav_path}")
print(f"   존재 여부: {wav_path.exists()}")

if not wav_path.exists():
    print(f"❌ WAV 파일이 없음!")
    sys.exit(1)

# 3. 오디오 로드
print(f"\n3. 오디오 로드")
try:
    speech, sr = sf.read(str(wav_path))
    print(f"✅ 오디오 로드 성공")
    print(f"   샘플링 레이트: {sr}")
    print(f"   길이: {len(speech)} samples ({len(speech)/sr:.2f}초)")
except Exception as e:
    print(f"❌ 오디오 로드 실패: {e}")
    sys.exit(1)

# 4. G2P 테스트
print(f"\n4. G2P 테스트")
text = metadata['Transcription']['LabelText'].strip()
print(f"   원본 텍스트: {text}")

try:
    g2p = KoreanG2P()
    phonemes = g2p(text)
    print(f"✅ G2P 성공")
    print(f"   음소: {phonemes}")
except Exception as e:
    print(f"❌ G2P 실패: {e}")
    sys.exit(1)

print("\n" + "=" * 80)
print("✅ 모든 테스트 통과!")
print("=" * 80)
