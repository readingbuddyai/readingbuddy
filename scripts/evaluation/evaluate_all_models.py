#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
모든 모델에 대해 Test 셋 평가를 실행하고 결과 비교
"""

import os
import sys
import json
import subprocess
from pathlib import Path
from datetime import datetime

# 평가할 모델들 정의
MODELS = {
    "original": {
        "name": "원본 모델 (wav2vec2-korean-phoneme)",
        "path": "/home/j-k13a206/models/wav2vec2-korean-phoneme",
        "output": "results_original.json"
    },
    "lora_r16_3500h": {
        "name": "LoRA r16 (3500h)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full/phase1_full_3500h/final_model",
        "output": "results_lora_r16_3500h.json"
    },
    "lora_r16_3500h_early": {
        "name": "LoRA r16 (3500h, Early Stop)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full/phase1_full_3500h_earlystop3/final_model",
        "output": "results_lora_r16_3500h_early.json"
    },
    "lora_r32_3500h": {
        "name": "LoRA r32 (3500h)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full_r32/phase1_full_3500h_r32/final_model",
        "output": "results_lora_r32_3500h.json"
    },
    "lora_r32_3500h_early": {
        "name": "LoRA r32 (3500h, Early Stop)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full_r32/phase1_full_3500h_r32_ealry3/final_model",
        "output": "results_lora_r32_3500h_early.json"
    }
}

def run_evaluation(model_id, model_info, gpu="3", test_dir=None):
    """단일 모델 평가 실행"""

    if test_dir is None:
        test_dir = "/home/j-k13a206/data/child_subset_100h/3.Test"

    model_path = model_info["path"]
    output_path = Path("/home/j-k13a206/fine_tunining_new/evaluation_results") / model_info["output"]

    print("\n" + "=" * 80)
    print(f"🚀 평가 시작: {model_info['name']}")
    print(f"📂 모델 경로: {model_path}")
    print(f"💾 결과 저장: {output_path}")
    print("=" * 80)

    # 경로 존재 확인
    if not Path(model_path).exists():
        print(f"❌ 모델 경로를 찾을 수 없습니다: {model_path}")
        return None

    # evaluate_test.py 실행
    cmd = [
        "python3",
        "/home/j-k13a206/fine_tunining_new/evaluate_test.py",
        "--model", model_path,
        "--test_dir", test_dir,
        "--gpu", gpu,
        "--output", str(output_path)
    ]

    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            encoding='utf-8'
        )

        # 출력 표시
        print(result.stdout)
        if result.stderr:
            print("STDERR:", result.stderr)

        if result.returncode != 0:
            print(f"❌ 평가 실패 (exit code: {result.returncode})")
            return None

        # 결과 로드
        if output_path.exists():
            with open(output_path, 'r', encoding='utf-8') as f:
                results = json.load(f)
            print(f"✅ 평가 완료: PER {results['per']*100:.2f}%")
            return results
        else:
            print(f"❌ 결과 파일이 생성되지 않았습니다")
            return None

    except Exception as e:
        print(f"❌ 오류 발생: {e}")
        return None


def compare_results(all_results):
    """모든 결과 비교 및 요약"""

    print("\n\n" + "=" * 80)
    print("📊 모델 성능 비교 결과")
    print("=" * 80)

    # 테이블 헤더
    print(f"\n{'모델명':<40} {'PER':>10} {'CER':>10} {'성공':>10} {'실패':>10}")
    print("-" * 80)

    # 결과 정렬 (PER 기준)
    sorted_results = sorted(
        all_results.items(),
        key=lambda x: x[1]['per'] if x[1] else float('inf')
    )

    best_model = None
    best_per = float('inf')

    for model_id, results in sorted_results:
        model_name = MODELS[model_id]['name']

        if results is None:
            print(f"{model_name:<40} {'N/A':>10} {'N/A':>10} {'N/A':>10} {'N/A':>10}")
            continue

        per = results['per'] * 100
        cer = results['cer'] * 100
        success = results['success_count']
        errors = results['error_count']

        # 최고 성능 모델 추적
        if per < best_per:
            best_per = per
            best_model = model_name

        # 결과 출력
        marker = " 🏆" if model_id == sorted_results[0][0] and results else ""
        print(f"{model_name:<40} {per:>9.2f}% {cer:>9.2f}% {success:>10,} {errors:>10,}{marker}")

    print("-" * 80)

    # 최고 성능 모델 강조
    if best_model:
        print(f"\n🏆 최고 성능 모델: {best_model}")
        print(f"   PER: {best_per:.2f}%")

        if best_per < 15:
            print(f"   ✅ 목표 달성! (PER < 15%)")
        elif best_per < 18:
            print(f"   ⚠️ 목표에 근접 (PER < 18%)")
        else:
            print(f"   ❌ 목표 미달성 (PER > 18%)")

    print("\n" + "=" * 80)

    # 종합 비교 JSON 저장
    comparison_path = Path("/home/j-k13a206/fine_tunining_new/evaluation_results/comparison_summary.json")
    comparison_data = {
        "timestamp": datetime.now().isoformat(),
        "best_model": best_model,
        "best_per": best_per,
        "results": {
            model_id: {
                "name": MODELS[model_id]['name'],
                "per": results['per'] * 100 if results else None,
                "cer": results['cer'] * 100 if results else None,
                "success_count": results['success_count'] if results else None,
                "error_count": results['error_count'] if results else None,
            }
            for model_id, results in all_results.items()
        }
    }

    comparison_path.parent.mkdir(parents=True, exist_ok=True)
    with open(comparison_path, 'w', encoding='utf-8') as f:
        json.dump(comparison_data, f, ensure_ascii=False, indent=2)

    print(f"💾 비교 결과 저장: {comparison_path}")


def main():
    import argparse

    parser = argparse.ArgumentParser(description='모든 모델 평가 및 비교')
    parser.add_argument('--gpu', type=str, default='3', help='GPU 번호')
    parser.add_argument('--test_dir', type=str,
                        default='/home/j-k13a206/data/child_subset_100h/3.Test',
                        help='Test 디렉토리 경로')
    parser.add_argument('--models', nargs='+',
                        choices=list(MODELS.keys()) + ['all'],
                        default=['all'],
                        help='평가할 모델 선택 (기본: all)')

    args = parser.parse_args()

    # 평가할 모델 목록 결정
    if 'all' in args.models:
        models_to_eval = list(MODELS.keys())
    else:
        models_to_eval = args.models

    print("=" * 80)
    print("🎯 모델 성능 평가 시작")
    print("=" * 80)
    print(f"📦 평가할 모델 수: {len(models_to_eval)}개")
    print(f"🖥️  GPU: {args.gpu}")
    print(f"📂 Test 디렉토리: {args.test_dir}")
    print("=" * 80)

    # 결과 저장 디렉토리 생성
    results_dir = Path("/home/j-k13a206/fine_tunining_new/evaluation_results")
    results_dir.mkdir(parents=True, exist_ok=True)

    # 모든 모델 평가
    all_results = {}

    for i, model_id in enumerate(models_to_eval, 1):
        print(f"\n{'='*80}")
        print(f"진행: {i}/{len(models_to_eval)}")
        print(f"{'='*80}")

        model_info = MODELS[model_id]
        results = run_evaluation(
            model_id=model_id,
            model_info=model_info,
            gpu=args.gpu,
            test_dir=args.test_dir
        )

        all_results[model_id] = results

    # 결과 비교
    compare_results(all_results)

    print("\n✅ 모든 평가 완료!")


if __name__ == "__main__":
    main()
