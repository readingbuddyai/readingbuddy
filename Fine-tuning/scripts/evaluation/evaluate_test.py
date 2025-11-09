#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test 셋으로 최종 모델 평가
WER (Word Error Rate) 계산
"""

import os
import sys
import json
import argparse
import glob
import numpy as np
import torch
import soundfile as sf
from pathlib import Path
from tqdm import tqdm
from typing import Dict, List
from transformers import Wav2Vec2Processor, Wav2Vec2ForCTC
from jiwer import wer, cer
from peft import PeftModel

sys.path.append('/home/j-k13a206/finetunning/organized/03_data_processing')
from korean_g2p import KoreanG2P


def load_model_and_processor(model_path: str, device: str):
    """모델과 Processor 로드 (LoRA 지원)"""
    # 절대 경로로 변환
    model_path = str(Path(model_path).resolve())

    print(f"\n📦 Processor 로드")
    processor = Wav2Vec2Processor.from_pretrained(
        '/home/j-k13a206/models/wav2vec2-korean-phoneme'
    )

    print(f"\n🤖 모델 로드: {model_path}")

    # adapter_config.json이 있는지 확인 (LoRA 모델인지)
    adapter_config_path = Path(model_path) / "adapter_config.json"

    if adapter_config_path.exists():
        # LoRA 모델 로드
        print("  LoRA 모델 감지 - PEFT로 로드")
        # 베이스 모델 먼저 로드
        base_model = Wav2Vec2ForCTC.from_pretrained(
            '/home/j-k13a206/models/wav2vec2-korean-phoneme'
        )
        # LoRA 어댑터 로드
        model = PeftModel.from_pretrained(base_model, model_path)
        # 추론을 위해 merge (선택사항, 속도 향상)
        model = model.merge_and_unload()
    else:
        # 일반 모델 로드
        print("  일반 모델로 로드")
        model = Wav2Vec2ForCTC.from_pretrained(model_path)

    model.to(device)
    model.eval()

    return model, processor


def phonemes_to_text(phonemes: str) -> str:
    """음소를 대략적인 텍스트로 변환 (간단한 역변환)"""
    # 실제로는 g2p의 역변환이 필요하지만, 평가를 위해 음소 그대로 사용
    return phonemes


def evaluate_test_set(
    model,
    processor,
    g2p: KoreanG2P,
    test_dir: str,
    device: str,
    max_samples: int = None
):
    """Test 셋 평가"""

    # Test JSON 파일 수집
    test_path = Path(test_dir)
    json_pattern = str(test_path / "**/*.json")
    json_files = sorted(glob.glob(json_pattern, recursive=True))

    if max_samples:
        json_files = json_files[:max_samples]

    print(f"\n📊 Test 셋: {len(json_files):,}개 샘플")

    references = []  # 정답 음소
    hypotheses = []  # 예측 음소

    success_count = 0
    error_count = 0

    print("\n🔍 평가 중...")
    for json_path in tqdm(json_files, desc="Evaluating"):
        try:
            # JSON 로드
            with open(json_path, 'r', encoding='utf-8') as f:
                metadata = json.load(f)

            # WAV 경로 구성
            filename = metadata['File']['FileName']
            json_path_obj = Path(json_path)
            parts = json_path_obj.parts

            # 라벨링데이터 → 원천데이터
            try:
                labeling_idx = parts.index("라벨링데이터")
                relative_parts = parts[labeling_idx + 1:]
            except ValueError:
                error_count += 1
                continue

            wav_path = test_path / "원천데이터" / Path(*relative_parts[:-1]) / filename

            if not wav_path.exists():
                error_count += 1
                continue

            # 오디오 로드
            speech, sr = sf.read(str(wav_path))

            if len(speech) < 1600:  # 너무 짧으면 스킵
                error_count += 1
                continue

            # 정답 텍스트
            text = metadata['Transcription']['LabelText'].strip()
            if not text:
                error_count += 1
                continue

            # 정답 음소
            ref_phonemes = g2p.text_to_phonemes(text)
            if not ref_phonemes:
                error_count += 1
                continue

            # 음성 인식 (예측)
            input_values = processor(
                speech,
                sampling_rate=16000,
                return_tensors="pt",
                padding=False
            ).input_values.to(device)

            with torch.no_grad():
                logits = model(input_values).logits

            # 디코딩
            predicted_ids = torch.argmax(logits, dim=-1)
            pred_phonemes = processor.batch_decode(predicted_ids)[0]

            # 저장
            references.append(ref_phonemes)
            hypotheses.append(pred_phonemes)
            success_count += 1

        except Exception as e:
            error_count += 1
            continue

    print(f"\n✅ 성공: {success_count:,}개")
    print(f"❌ 실패: {error_count:,}개")

    if not references or not hypotheses:
        print("\n⚠️ 평가할 샘플이 없습니다!")
        return None

    # WER/CER 계산
    print("\n" + "=" * 80)
    print("📈 평가 결과")
    print("=" * 80)

    # 음소 단위 WER (Phoneme Error Rate, PER)
    per_score = wer(references, hypotheses)
    print(f"\n🎯 PER (Phoneme Error Rate): {per_score*100:.2f}%")

    # CER (Character Error Rate)
    cer_score = cer(references, hypotheses)
    print(f"📝 CER (Character Error Rate): {cer_score*100:.2f}%")

    # 샘플 출력
    print("\n" + "=" * 80)
    print("📋 예측 샘플 (처음 5개)")
    print("=" * 80)
    for i in range(min(5, len(references))):
        print(f"\n[샘플 {i+1}]")
        print(f"정답: {references[i]}")
        print(f"예측: {hypotheses[i]}")

    # 결과 저장
    results = {
        'per': per_score,
        'cer': cer_score,
        'success_count': success_count,
        'error_count': error_count,
        'total_samples': len(json_files),
    }

    return results


def main():
    parser = argparse.ArgumentParser(description='Test 셋 평가')
    parser.add_argument('--model', type=str, required=True, help='모델 체크포인트 경로')
    parser.add_argument('--test_dir', type=str,
                        default='/home/j-k13a206/data/child_subset_100h/3.Test',
                        help='Test 디렉토리 경로')
    parser.add_argument('--gpu', type=str, default='3', help='GPU 번호')
    parser.add_argument('--max_samples', type=int, default=None, help='최대 샘플 수 (테스트용)')
    parser.add_argument('--output', type=str, default=None, help='결과 JSON 저장 경로')

    args = parser.parse_args()

    # GPU 설정
    os.environ['CUDA_VISIBLE_DEVICES'] = args.gpu
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    print(f"🖥️ Device: {device} (GPU {args.gpu})")

    # 모델 로드
    model, processor = load_model_and_processor(args.model, device)
    g2p = KoreanG2P()

    # 평가 실행
    results = evaluate_test_set(
        model=model,
        processor=processor,
        g2p=g2p,
        test_dir=args.test_dir,
        device=device,
        max_samples=args.max_samples,
    )

    if results is None:
        print("\n❌ 평가 실패!")
        return

    # 결과 저장
    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
        print(f"\n💾 결과 저장: {output_path}")

    # 최종 판정
    print("\n" + "=" * 80)
    print("🏆 최종 판정")
    print("=" * 80)

    per = results['per'] * 100

    if per < 15:
        print(f"✅ 성공! PER {per:.2f}% < 15% 목표 달성! 🎉")
    elif per < 18:
        print(f"⚠️ 양호. PER {per:.2f}%는 목표(15%)에 근접했습니다.")
        print("   추가 학습이나 하이퍼파라미터 조정을 고려하세요.")
    else:
        print(f"❌ 목표 미달성. PER {per:.2f}% > 18%")
        print("   다음을 시도해보세요:")
        print("   1. Learning rate 조정 (더 낮게)")
        print("   2. 더 많은 epoch 학습")
        print("   3. 데이터 품질 재확인")
        print("   4. Phase 1부터 재학습")

    print("=" * 80)


if __name__ == "__main__":
    main()
