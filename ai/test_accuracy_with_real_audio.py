"""
실제 오디오 파일로 최적화 기법의 정확도 및 성능 테스트

목적:
1. 더미 데이터가 아닌 실제 음성 파일로 테스트
2. 최적화 기법별로 출력 결과가 동일한지 검증
3. 속도와 정확도를 동시에 측정
"""

import time
import numpy as np
import torch
from transformers import Wav2Vec2ForCTC, Wav2Vec2Processor
from pathlib import Path
import librosa
from typing import List, Dict, Tuple
import json
from datetime import datetime

# 설정
MODEL_DIR = Path("models/slplab_wav2vec2_korean")
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
SAMPLE_RATE = 16000

# 테스트 오디오 파일 경로 (실제 경로로 수정 필요)
TEST_AUDIO_DIR = Path("test_audio")  # 테스트 오디오 폴더


class AccuracyTester:
    """정확도 및 성능 테스트"""

    def __init__(self):
        self.processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
        self.results = []

    def load_audio(self, audio_path: str) -> np.ndarray:
        """오디오 로드"""
        audio, sr = librosa.load(audio_path, sr=SAMPLE_RATE)
        return audio.astype(np.float32)

    def create_test_chunks(self, audio: np.ndarray, chunk_size: float = 1.0) -> List[np.ndarray]:
        """오디오를 청크로 분할"""
        samples_per_chunk = int(SAMPLE_RATE * chunk_size)
        num_chunks = len(audio) // samples_per_chunk

        chunks = []
        for i in range(num_chunks):
            start = i * samples_per_chunk
            end = start + samples_per_chunk
            chunks.append(audio[start:end])

        # 나머지 부분 추가
        if len(audio) % samples_per_chunk > 0:
            chunks.append(audio[num_chunks * samples_per_chunk:])

        return chunks

    def test_baseline(self, chunks: List[np.ndarray]) -> Tuple[List[str], float]:
        """Baseline 테스트"""
        print("\n[Baseline 테스트]")
        model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))
        model.to(DEVICE)
        model.eval()

        results = []
        start_time = time.time()

        with torch.no_grad():
            for i, chunk in enumerate(chunks):
                inputs = self.processor(chunk, sampling_rate=SAMPLE_RATE, return_tensors="pt")
                inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

                logits = model(**inputs).logits
                pred_ids = torch.argmax(logits, dim=-1)
                text = self.processor.decode(pred_ids[0])
                results.append(text)

                if (i + 1) % 5 == 0:
                    print(f"  진행: {i + 1}/{len(chunks)} 청크")

        elapsed = time.time() - start_time
        print(f"  완료: {elapsed:.3f}초")

        del model
        torch.cuda.empty_cache()

        return results, elapsed

    def test_fp16(self, chunks: List[np.ndarray]) -> Tuple[List[str], float]:
        """FP16 테스트"""
        print("\n[FP16 테스트]")
        model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))

        if DEVICE == "cuda":
            model = model.half()
            print("  FP16 모드 활성화")
        else:
            print("  CPU에서는 FP32 사용")

        model.to(DEVICE)
        model.eval()

        results = []
        start_time = time.time()

        with torch.no_grad():
            for i, chunk in enumerate(chunks):
                inputs = self.processor(chunk, sampling_rate=SAMPLE_RATE, return_tensors="pt")

                if DEVICE == "cuda":
                    inputs = {k: v.to(DEVICE).half() for k, v in inputs.items()}
                else:
                    inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

                logits = model(**inputs).logits
                pred_ids = torch.argmax(logits, dim=-1)
                text = self.processor.decode(pred_ids[0])
                results.append(text)

                if (i + 1) % 5 == 0:
                    print(f"  진행: {i + 1}/{len(chunks)} 청크")

        elapsed = time.time() - start_time
        print(f"  완료: {elapsed:.3f}초")

        del model
        torch.cuda.empty_cache()

        return results, elapsed

    def test_batch(self, chunks: List[np.ndarray], batch_size: int = 8) -> Tuple[List[str], float]:
        """Batch 추론 테스트"""
        print(f"\n[Batch 테스트] (Batch Size: {batch_size})")
        model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))
        model.to(DEVICE)
        model.eval()

        results = []
        start_time = time.time()

        with torch.no_grad():
            for i in range(0, len(chunks), batch_size):
                batch_chunks = chunks[i:i + batch_size]

                inputs = self.processor(
                    batch_chunks,
                    sampling_rate=SAMPLE_RATE,
                    return_tensors="pt",
                    padding=True
                )
                inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

                logits = model(**inputs).logits
                pred_ids = torch.argmax(logits, dim=-1)
                texts = self.processor.batch_decode(pred_ids)
                results.extend(texts)

                if (i + batch_size) % (batch_size * 2) == 0:
                    print(f"  진행: {min(i + batch_size, len(chunks))}/{len(chunks)} 청크")

        elapsed = time.time() - start_time
        print(f"  완료: {elapsed:.3f}초")

        del model
        torch.cuda.empty_cache()

        return results, elapsed

    def test_combined(self, chunks: List[np.ndarray], batch_size: int = 8) -> Tuple[List[str], float]:
        """Combined (Batch + FP16) 테스트"""
        print(f"\n[Combined 테스트] (Batch: {batch_size}, FP16: {DEVICE == 'cuda'})")
        model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))

        if DEVICE == "cuda":
            model = model.half()

        model.to(DEVICE)
        model.eval()

        results = []
        start_time = time.time()

        with torch.no_grad():
            for i in range(0, len(chunks), batch_size):
                batch_chunks = chunks[i:i + batch_size]

                inputs = self.processor(
                    batch_chunks,
                    sampling_rate=SAMPLE_RATE,
                    return_tensors="pt",
                    padding=True
                )

                if DEVICE == "cuda":
                    inputs = {k: v.to(DEVICE).half() for k, v in inputs.items()}
                else:
                    inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

                logits = model(**inputs).logits
                pred_ids = torch.argmax(logits, dim=-1)
                texts = self.processor.batch_decode(pred_ids)
                results.extend(texts)

                if (i + batch_size) % (batch_size * 2) == 0:
                    print(f"  진행: {min(i + batch_size, len(chunks))}/{len(chunks)} 청크")

        elapsed = time.time() - start_time
        print(f"  완료: {elapsed:.3f}초")

        del model
        torch.cuda.empty_cache()

        return results, elapsed

    def compare_results(self, baseline: List[str], optimized: List[str], name: str) -> Dict:
        """결과 비교"""
        # 청크별 비교
        matches = sum(1 for b, o in zip(baseline, optimized) if b == o)
        total = len(baseline)
        accuracy = (matches / total) * 100

        # 문자열 유사도 (Levenshtein 거리 대신 간단한 비교)
        char_matches = 0
        total_chars = 0

        for b, o in zip(baseline, optimized):
            total_chars += max(len(b), len(o))
            char_matches += sum(1 for c1, c2 in zip(b, o) if c1 == c2)

        char_accuracy = (char_matches / total_chars * 100) if total_chars > 0 else 100

        return {
            'name': name,
            'exact_match_rate': accuracy,
            'char_accuracy': char_accuracy,
            'matches': matches,
            'total': total,
            'differences': [
                {'chunk': i, 'baseline': b, 'optimized': o}
                for i, (b, o) in enumerate(zip(baseline, optimized)) if b != o
            ]
        }

    def test_audio_file(self, audio_path: str, chunk_size: float = 1.0, batch_size: int = 8) -> Dict:
        """단일 오디오 파일 테스트"""
        print(f"\n{'='*80}")
        print(f"테스트 오디오: {audio_path}")
        print(f"{'='*80}")

        # 오디오 로드
        audio = self.load_audio(audio_path)
        duration = len(audio) / SAMPLE_RATE
        print(f"오디오 길이: {duration:.2f}초")

        # 청크 생성
        chunks = self.create_test_chunks(audio, chunk_size)
        print(f"청크 개수: {len(chunks)}개 (청크 크기: {chunk_size}초)")

        # 각 방법으로 테스트
        baseline_results, baseline_time = self.test_baseline(chunks)
        fp16_results, fp16_time = self.test_fp16(chunks)
        batch_results, batch_time = self.test_batch(chunks, batch_size)
        combined_results, combined_time = self.test_combined(chunks, batch_size)

        # 결과 비교
        print(f"\n{'='*80}")
        print("정확도 비교")
        print(f"{'='*80}")

        fp16_comparison = self.compare_results(baseline_results, fp16_results, "FP16")
        batch_comparison = self.compare_results(baseline_results, batch_results, "Batch")
        combined_comparison = self.compare_results(baseline_results, combined_results, "Combined")

        # 출력
        print(f"\n{'방법':<15} {'완전 일치율':<15} {'문자 정확도':<15} {'추론 시간':<15} {'속도 향상'}")
        print("-" * 80)

        print(f"{'Baseline':<15} {'-':<15} {'-':<15} {baseline_time:>10.3f}초     {'-':<15}")

        for comp, time_taken in [(fp16_comparison, fp16_time),
                                  (batch_comparison, batch_time),
                                  (combined_comparison, combined_time)]:
            speedup = baseline_time / time_taken
            print(f"{comp['name']:<15} "
                  f"{comp['exact_match_rate']:>10.1f}%    "
                  f"{comp['char_accuracy']:>10.1f}%    "
                  f"{time_taken:>10.3f}초     "
                  f"{speedup:>10.2f}x")

        # 차이점 출력
        for comp in [fp16_comparison, batch_comparison, combined_comparison]:
            if comp['differences']:
                print(f"\n⚠️  {comp['name']} 차이점 ({len(comp['differences'])}개):")
                for diff in comp['differences'][:3]:  # 최대 3개만 표시
                    print(f"  청크 {diff['chunk']}:")
                    print(f"    Baseline: {diff['baseline']}")
                    print(f"    {comp['name']:>8}: {diff['optimized']}")
                if len(comp['differences']) > 3:
                    print(f"  ... 외 {len(comp['differences']) - 3}개")

        # 결과 저장
        result = {
            'audio_file': audio_path,
            'duration': duration,
            'num_chunks': len(chunks),
            'chunk_size': chunk_size,
            'batch_size': batch_size,
            'baseline': {
                'time': baseline_time,
                'results': baseline_results
            },
            'fp16': {
                'time': fp16_time,
                'speedup': baseline_time / fp16_time,
                'accuracy': fp16_comparison
            },
            'batch': {
                'time': batch_time,
                'speedup': baseline_time / batch_time,
                'accuracy': batch_comparison
            },
            'combined': {
                'time': combined_time,
                'speedup': baseline_time / combined_time,
                'accuracy': combined_comparison
            }
        }

        return result


def create_sample_audio_if_needed():
    """테스트용 샘플 오디오 생성 (실제 오디오가 없을 경우)"""
    test_dir = Path("test_audio")
    test_dir.mkdir(exist_ok=True)

    sample_file = test_dir / "sample_1sec.wav"

    if not sample_file.exists():
        print(f"\n⚠️  테스트 오디오가 없습니다. 샘플 오디오를 생성합니다...")
        print(f"   경로: {sample_file}")

        # 1초 샘플 오디오 생성
        import soundfile as sf
        audio = np.random.randn(SAMPLE_RATE).astype(np.float32) * 0.1
        sf.write(sample_file, audio, SAMPLE_RATE)
        print(f"   ✅ 생성 완료")

    return sample_file


def main():
    print("="*80)
    print("실제 오디오 파일 정확도 및 성능 테스트")
    print("="*80)
    print(f"\n설정:")
    print(f"  Device: {DEVICE}")
    print(f"  샘플링 레이트: {SAMPLE_RATE}Hz")

    tester = AccuracyTester()

    # 테스트 오디오 파일 찾기
    test_audio_dir = Path("test_audio")

    if test_audio_dir.exists():
        audio_files = list(test_audio_dir.glob("*.wav")) + list(test_audio_dir.glob("*.mp3"))
    else:
        audio_files = []

    # 테스트 오디오가 없으면 샘플 생성
    if not audio_files:
        print(f"\n⚠️  '{test_audio_dir}' 폴더에 오디오 파일이 없습니다.")
        print(f"   다음 형식의 파일을 추가하세요: .wav, .mp3")
        print(f"   또는 샘플 오디오를 생성합니다...\n")

        sample_file = create_sample_audio_if_needed()
        audio_files = [sample_file]

    # 각 오디오 파일 테스트
    all_results = []

    for audio_file in audio_files:
        try:
            result = tester.test_audio_file(
                str(audio_file),
                chunk_size=1.0,  # 1초 청크
                batch_size=8
            )
            all_results.append(result)
        except Exception as e:
            print(f"\n❌ 오류 발생: {audio_file}")
            print(f"   {e}")

    # 전체 결과 저장
    if all_results:
        output_file = f"accuracy_test_results_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(all_results, f, ensure_ascii=False, indent=2)

        print(f"\n{'='*80}")
        print(f"✅ 결과 저장: {output_file}")
        print(f"{'='*80}")

    # 요약
    if all_results:
        print(f"\n{'='*80}")
        print("전체 요약")
        print(f"{'='*80}")

        print(f"\n테스트 파일 수: {len(all_results)}개")

        for method in ['fp16', 'batch', 'combined']:
            avg_accuracy = np.mean([r[method]['accuracy']['exact_match_rate'] for r in all_results])
            avg_speedup = np.mean([r[method]['speedup'] for r in all_results])

            print(f"\n{method.upper()}:")
            print(f"  평균 정확도: {avg_accuracy:.1f}%")
            print(f"  평균 속도 향상: {avg_speedup:.2f}x")


if __name__ == "__main__":
    main()
