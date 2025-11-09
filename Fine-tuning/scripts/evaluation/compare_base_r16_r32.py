#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
기본 모델, LoRA r16, LoRA r32 성능 비교
"""

import os
import sys
import json
import subprocess
from pathlib import Path
from datetime import datetime

# 비교할 모델들 정의 (총 7개)
MODELS = {
    "base": {
        "name": "기본 모델 (wav2vec2-korean-phoneme)",
        "path": "/home/j-k13a206/models/wav2vec2-korean-phoneme",
        "output": "results_base.json"
    },
    # LoRA r16 모델들 (checkpoints_full)
    "lora_r16_3500h": {
        "name": "LoRA r16 (3500h)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full/phase1_full_3500h/final_model",
        "output": "results_lora_r16_3500h.json"
    },
    "lora_r16_early3": {
        "name": "LoRA r16 (3500h, Early Stop 3)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full/phase1_full_3500h_earlystop3/final_model",
        "output": "results_lora_r16_early3.json"
    },
    "lora_r16_early15": {
        "name": "LoRA r16 (3500h, Early Stop 15)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full/phase1_full_3500h_earlystop15/final_model",
        "output": "results_lora_r16_early15.json"
    },
    # LoRA r32 모델들 (checkpoints_full_r32)
    "lora_r32_3500h": {
        "name": "LoRA r32 (3500h)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full_r32/phase1_full_3500h_r32/final_model",
        "output": "results_lora_r32_3500h.json"
    },
    "lora_r32_early3": {
        "name": "LoRA r32 (3500h, Early Stop 3)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full_r32/phase1_full_3500h_r32_ealry3/final_model",
        "output": "results_lora_r32_early3.json"
    },
    "lora_r32_early15": {
        "name": "LoRA r32 (3500h, Early Stop 15)",
        "path": "/home/j-k13a206/fine_tunining_new/checkpoints_full_r32/phase1_full_3500h_r32_ealry15/final_model",
        "output": "results_lora_r32_early15.json"
    }
}

def run_evaluation(model_id, model_info, gpu="3", test_dir=None):
    """단일 모델 평가 실행"""

    if test_dir is None:
        test_dir = "/home/j-k13a206/data/child_subset_100h/3.Test"

    model_path = model_info["path"]
    output_path = Path("/home/j-k13a206/fine_tunining_new/comparison_results") / model_info["output"]

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
    print("📊 전체 모델 성능 비교 결과 (총 7개)")
    print("=" * 80)

    # 테이블 헤더
    print(f"\n{'모델명':<45} {'PER':>10} {'CER':>10} {'개선율':>10} {'성공':>10}")
    print("-" * 90)

    # 기본 모델 PER
    base_per = all_results.get("base", {}).get("per", 1.0) if all_results.get("base") else 1.0

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
            print(f"{model_name:<45} {'N/A':>10} {'N/A':>10} {'N/A':>10} {'N/A':>10}")
            continue

        per = results['per'] * 100
        cer = results['cer'] * 100
        success = results['success_count']

        # 개선율 계산
        if model_id == "base":
            improvement = "-"
        else:
            improvement_rate = ((base_per - results['per']) / base_per) * 100
            improvement = f"{improvement_rate:+.1f}%"

        # 최고 성능 모델 추적
        if per < best_per:
            best_per = per
            best_model = model_name

        # 결과 출력
        marker = " 🏆" if model_id == sorted_results[0][0] and results else ""
        print(f"{model_name:<45} {per:>9.2f}% {cer:>9.2f}% {improvement:>10} {success:>10,}{marker}")

    print("-" * 90)

    # 상세 분석
    print("\n" + "=" * 80)
    print("📈 상세 분석")
    print("=" * 80)

    if "base" in all_results and all_results["base"]:
        base_per_val = all_results["base"]["per"] * 100
        print(f"\n🔹 기본 모델 PER: {base_per_val:.2f}%")

    # LoRA r16 그룹 분석
    print("\n📦 LoRA r16 모델들:")
    r16_models = ["lora_r16_3500h", "lora_r16_early3", "lora_r16_early15"]
    r16_best = None
    r16_best_per = float('inf')

    for model_id in r16_models:
        if model_id in all_results and all_results[model_id]:
            per = all_results[model_id]["per"] * 100
            improvement = ((base_per - all_results[model_id]["per"]) / base_per) * 100
            print(f"  • {MODELS[model_id]['name']}: {per:.2f}% (개선: {improvement:+.1f}%)")
            if per < r16_best_per:
                r16_best_per = per
                r16_best = model_id

    # LoRA r32 그룹 분석
    print("\n📦 LoRA r32 모델들:")
    r32_models = ["lora_r32_3500h", "lora_r32_early3", "lora_r32_early15"]
    r32_best = None
    r32_best_per = float('inf')

    for model_id in r32_models:
        if model_id in all_results and all_results[model_id]:
            per = all_results[model_id]["per"] * 100
            improvement = ((base_per - all_results[model_id]["per"]) / base_per) * 100
            print(f"  • {MODELS[model_id]['name']}: {per:.2f}% (개선: {improvement:+.1f}%)")
            if per < r32_best_per:
                r32_best_per = per
                r32_best = model_id

    # r16 vs r32 최고 모델 비교
    if r16_best and r32_best:
        diff = r16_best_per - r32_best_per
        print(f"\n🔸 최고 r16 vs 최고 r32 비교:")
        print(f"  • 최고 r16: {MODELS[r16_best]['name']} - {r16_best_per:.2f}%")
        print(f"  • 최고 r32: {MODELS[r32_best]['name']} - {r32_best_per:.2f}%")
        print(f"  • 차이: {diff:+.2f}%p")
        if diff > 0:
            print(f"  ✅ r32가 r16보다 {abs(diff):.2f}%p 더 우수")
        elif diff < 0:
            print(f"  ✅ r16이 r32보다 {abs(diff):.2f}%p 더 우수")
        else:
            print(f"  ⚖️  r16과 r32 성능이 동일")

    # 최고 성능 모델
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
    comparison_path = Path("/home/j-k13a206/fine_tunining_new/comparison_results/summary.json")
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

    parser = argparse.ArgumentParser(description='기본 모델 vs LoRA r16 vs LoRA r32 비교')
    parser.add_argument('--gpu', type=str, default='3', help='GPU 번호')
    parser.add_argument('--test_dir', type=str,
                        default='/home/j-k13a206/data/child_subset_100h/3.Test',
                        help='Test 디렉토리 경로')

    args = parser.parse_args()

    print("=" * 80)
    print("🎯 전체 모델 성능 비교 (기본 + r16×3 + r32×3)")
    print("=" * 80)
    print(f"📦 비교할 모델 수: {len(MODELS)}개")
    print(f"   • 기본 모델: 1개")
    print(f"   • LoRA r16 모델: 3개")
    print(f"   • LoRA r32 모델: 3개")
    print(f"🖥️  GPU: {args.gpu}")
    print(f"📂 Test 디렉토리: {args.test_dir}")
    print("=" * 80)

    # 결과 저장 디렉토리 생성
    results_dir = Path("/home/j-k13a206/fine_tunining_new/comparison_results")
    results_dir.mkdir(parents=True, exist_ok=True)

    # 모든 모델 평가
    all_results = {}

    for i, (model_id, model_info) in enumerate(MODELS.items(), 1):
        print(f"\n{'='*80}")
        print(f"진행: {i}/{len(MODELS)}")
        print(f"{'='*80}")

        results = run_evaluation(
            model_id=model_id,
            model_info=model_info,
            gpu=args.gpu,
            test_dir=args.test_dir
        )

        all_results[model_id] = results

    # 결과 비교
    compare_results(all_results)

    print("\n✅ 모든 비교 완료!")


if __name__ == "__main__":
    main()
