"""
테스트 결과 비교 및 리포트 생성

기능:
1. JSON 결과 파일 읽기 및 분석
2. 여러 테스트 결과 비교
3. Markdown/HTML 리포트 생성
4. 최적 설정 추천
"""

import json
from pathlib import Path
from datetime import datetime
from typing import List, Dict
import numpy as np


class TestResultAnalyzer:
    """테스트 결과 분석"""

    def __init__(self):
        self.results = []

    def load_result_file(self, file_path: str):
        """결과 파일 로드"""
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
            self.results.extend(data if isinstance(data, list) else [data])

    def analyze(self) -> Dict:
        """전체 결과 분석"""
        if not self.results:
            return {}

        analysis = {
            'total_tests': len(self.results),
            'by_method': {},
            'recommendations': []
        }

        # 방법별 통계
        for method in ['fp16', 'batch', 'combined']:
            accuracies = [r[method]['accuracy']['exact_match_rate'] for r in self.results]
            speedups = [r[method]['speedup'] for r in self.results]
            char_accuracies = [r[method]['accuracy']['char_accuracy'] for r in self.results]

            analysis['by_method'][method] = {
                'accuracy': {
                    'mean': np.mean(accuracies),
                    'std': np.std(accuracies),
                    'min': np.min(accuracies),
                    'max': np.max(accuracies)
                },
                'char_accuracy': {
                    'mean': np.mean(char_accuracies),
                    'std': np.std(char_accuracies),
                    'min': np.min(char_accuracies),
                    'max': np.max(char_accuracies)
                },
                'speedup': {
                    'mean': np.mean(speedups),
                    'std': np.std(speedups),
                    'min': np.min(speedups),
                    'max': np.max(speedups)
                }
            }

        # 추천 생성
        analysis['recommendations'] = self._generate_recommendations(analysis)

        return analysis

    def _generate_recommendations(self, analysis: Dict) -> List[str]:
        """추천 사항 생성"""
        recommendations = []

        # 정확도 체크
        for method, stats in analysis['by_method'].items():
            acc = stats['accuracy']['mean']
            speedup = stats['speedup']['mean']

            if acc >= 99.0:
                recommendations.append(
                    f"✅ {method.upper()}: 정확도 {acc:.1f}%, 속도 {speedup:.2f}x - 안전하게 사용 가능"
                )
            elif acc >= 95.0:
                recommendations.append(
                    f"⚠️  {method.upper()}: 정확도 {acc:.1f}% - 일부 차이 있음, 검증 필요"
                )
            else:
                recommendations.append(
                    f"❌ {method.upper()}: 정확도 {acc:.1f}% - 사용 권장하지 않음"
                )

        # 최적 방법 추천
        best_method = max(
            analysis['by_method'].items(),
            key=lambda x: x[1]['speedup']['mean'] * (x[1]['accuracy']['mean'] / 100)
        )

        recommendations.append(
            f"\n🏆 추천: {best_method[0].upper()} "
            f"(속도 {best_method[1]['speedup']['mean']:.2f}x, "
            f"정확도 {best_method[1]['accuracy']['mean']:.1f}%)"
        )

        return recommendations

    def generate_markdown_report(self, output_file: str = None) -> str:
        """Markdown 리포트 생성"""
        if output_file is None:
            output_file = f"test_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.md"

        analysis = self.analyze()

        report = []
        report.append("# SafeTensor 최적화 테스트 리포트\n")
        report.append(f"생성 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        report.append("---\n")

        # 요약
        report.append("## 📊 테스트 요약\n")
        report.append(f"- **총 테스트 수**: {analysis['total_tests']}개\n")

        for result in self.results:
            report.append(f"- **{result['audio_file']}**")
            report.append(f"  - 길이: {result['duration']:.2f}초")
            report.append(f"  - 청크: {result['num_chunks']}개\n")

        # 방법별 통계
        report.append("\n## 📈 방법별 성능\n")

        report.append("| 방법 | 정확도 | 문자 정확도 | 속도 향상 | 평가 |\n")
        report.append("|------|--------|------------|----------|------|\n")

        for method, stats in analysis['by_method'].items():
            acc = stats['accuracy']['mean']
            char_acc = stats['char_accuracy']['mean']
            speedup = stats['speedup']['mean']

            if acc >= 99.0:
                status = "✅ 우수"
            elif acc >= 95.0:
                status = "⚠️ 보통"
            else:
                status = "❌ 주의"

            report.append(
                f"| {method.upper()} | "
                f"{acc:.1f}% ± {stats['accuracy']['std']:.1f}% | "
                f"{char_acc:.1f}% | "
                f"{speedup:.2f}x ± {stats['speedup']['std']:.2f}x | "
                f"{status} |\n"
            )

        # 상세 결과
        report.append("\n## 📝 상세 결과\n")

        for i, result in enumerate(self.results, 1):
            report.append(f"\n### 테스트 {i}: {Path(result['audio_file']).name}\n")

            report.append("| 방법 | 완전 일치율 | 문자 정확도 | 시간 | 속도 향상 |\n")
            report.append("|------|-----------|-----------|------|----------|\n")

            baseline_time = result['baseline']['time']
            report.append(f"| Baseline | - | - | {baseline_time:.3f}초 | - |\n")

            for method in ['fp16', 'batch', 'combined']:
                acc = result[method]['accuracy']['exact_match_rate']
                char_acc = result[method]['accuracy']['char_accuracy']
                time_taken = result[method]['time']
                speedup = result[method]['speedup']

                report.append(
                    f"| {method.upper()} | "
                    f"{acc:.1f}% | "
                    f"{char_acc:.1f}% | "
                    f"{time_taken:.3f}초 | "
                    f"{speedup:.2f}x |\n"
                )

            # 차이점
            for method in ['fp16', 'batch', 'combined']:
                diffs = result[method]['accuracy']['differences']
                if diffs:
                    report.append(f"\n**{method.upper()} 차이점** ({len(diffs)}개):\n")
                    for diff in diffs[:3]:
                        report.append(f"- 청크 {diff['chunk']}:\n")
                        report.append(f"  - Baseline: `{diff['baseline']}`\n")
                        report.append(f"  - {method.upper()}: `{diff['optimized']}`\n")
                    if len(diffs) > 3:
                        report.append(f"- ... 외 {len(diffs) - 3}개\n")

        # 추천 사항
        report.append("\n## 💡 추천 사항\n")
        for rec in analysis['recommendations']:
            report.append(f"- {rec}\n")

        # 결론
        report.append("\n## 🎯 결론\n")

        best_method = max(
            analysis['by_method'].items(),
            key=lambda x: x[1]['speedup']['mean']
        )

        report.append(f"1. **최고 속도**: {best_method[0].upper()} ({best_method[1]['speedup']['mean']:.2f}배)\n")

        safest_method = max(
            analysis['by_method'].items(),
            key=lambda x: x[1]['accuracy']['mean']
        )

        report.append(f"2. **최고 정확도**: {safest_method[0].upper()} ({safest_method[1]['accuracy']['mean']:.1f}%)\n")

        report.append("\n### 실전 적용 가이드\n")
        report.append("```python\n")
        report.append("# 짧은 오디오 (< 5초)\n")
        report.append("if audio_length < 5.0:\n")
        report.append("    use_fp16()  # 안정적이고 빠름\n")
        report.append("\n")
        report.append("# 긴 오디오 (>= 5초)\n")
        report.append("else:\n")
        report.append("    use_combined()  # 최고 성능\n")
        report.append("```\n")

        # 파일 저장
        report_text = ''.join(report)
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(report_text)

        print(f"\n✅ 리포트 생성: {output_file}")
        return report_text

    def print_summary(self):
        """콘솔에 요약 출력"""
        analysis = self.analyze()

        print("\n" + "="*80)
        print("테스트 결과 요약")
        print("="*80)

        print(f"\n총 테스트 수: {analysis['total_tests']}개")

        print(f"\n{'방법':<12} {'정확도':<20} {'문자 정확도':<20} {'속도 향상':<20}")
        print("-" * 80)

        for method, stats in analysis['by_method'].items():
            print(f"{method.upper():<12} "
                  f"{stats['accuracy']['mean']:>6.1f}% ± {stats['accuracy']['std']:>5.1f}%   "
                  f"{stats['char_accuracy']['mean']:>6.1f}% ± {stats['char_accuracy']['std']:>5.1f}%   "
                  f"{stats['speedup']['mean']:>6.2f}x ± {stats['speedup']['std']:>5.2f}x")

        print("\n" + "="*80)
        print("추천 사항")
        print("="*80)
        for rec in analysis['recommendations']:
            print(rec)


def main():
    print("="*80)
    print("테스트 결과 분석 및 리포트 생성")
    print("="*80)

    # JSON 파일 찾기
    result_files = list(Path(".").glob("accuracy_test_results_*.json"))

    if not result_files:
        print("\n❌ 결과 파일을 찾을 수 없습니다.")
        print("   먼저 'python test_accuracy_with_real_audio.py'를 실행하세요.")
        return

    print(f"\n발견된 결과 파일: {len(result_files)}개")
    for f in result_files:
        print(f"  - {f}")

    # 최신 파일 사용
    latest_file = max(result_files, key=lambda p: p.stat().st_mtime)
    print(f"\n사용할 파일: {latest_file}")

    # 분석
    analyzer = TestResultAnalyzer()
    analyzer.load_result_file(str(latest_file))

    # 콘솔 출력
    analyzer.print_summary()

    # 리포트 생성
    analyzer.generate_markdown_report()

    print(f"\n{'='*80}")
    print("분석 완료!")
    print(f"{'='*80}")


if __name__ == "__main__":
    main()
