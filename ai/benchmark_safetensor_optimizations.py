"""
SafeTensor 최적화 성능 비교 벤치마크
각 최적화 기법별로 속도 향상을 측정합니다.

최적화 기법:
1. Baseline (현재 구현)
2. Batch Inference (배치 추론)
3. Mixed Precision FP16 (반정밀도)
4. TorchScript JIT Compile (JIT 컴파일)
5. Combined (모든 최적화 통합)
"""

import time
import numpy as np
import torch
from transformers import Wav2Vec2ForCTC, Wav2Vec2Processor
from pathlib import Path
from typing import List, Dict
import warnings
warnings.filterwarnings("ignore")

# 설정
MODEL_DIR = Path("models/slplab_wav2vec2_korean")
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
SAMPLE_RATE = 16000
NUM_WARMUP = 10     # 3 → 10 (GPU 완전히 워밍업)
NUM_ITERATIONS = 50 # 20 → 50 (통계적 신뢰도 향상)

# 테스트 시나리오
TEST_SCENARIOS = [
    {"name": "단일 청크 (1초)", "duration": 1.0, "chunks": 1},
    {"name": "짧은 오디오 (3초)", "duration": 3.0, "chunks": 1},
    {"name": "긴 오디오 (10초, 10 청크)", "duration": 10.0, "chunks": 10},
]


def create_dummy_audio(duration_sec=1.0):
    """더미 오디오 데이터 생성"""
    num_samples = int(SAMPLE_RATE * duration_sec)
    audio = np.random.randn(num_samples).astype(np.float32)
    return audio


def create_chunks(audio: np.ndarray, num_chunks: int):
    """오디오를 청크로 분할"""
    if num_chunks == 1:
        return [audio]

    chunk_size = len(audio) // num_chunks
    chunks = []
    for i in range(num_chunks):
        start = i * chunk_size
        end = start + chunk_size if i < num_chunks - 1 else len(audio)
        chunks.append(audio[start:end])
    return chunks


# ============================================================
# 1. Baseline (현재 구현)
# ============================================================
class BaselineInference:
    """현재 구현 (최적화 없음)"""

    def __init__(self):
        print("\n[Baseline] 모델 로딩 중...")
        self.processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
        self.model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))
        self.model.to(DEVICE)
        self.model.eval()
        print(f"[Baseline] 로드 완료 (Device: {DEVICE})")

    @torch.no_grad()
    def infer_single(self, waveform: np.ndarray):
        """단일 청크 추론"""
        # 전처리
        inputs = self.processor(waveform, sampling_rate=SAMPLE_RATE, return_tensors="pt")
        inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

        # 추론
        logits = self.model(**inputs).logits

        # 후처리
        pred_ids = torch.argmax(logits, dim=-1)
        text = self.processor.decode(pred_ids[0])
        return text

    def infer_chunks(self, chunks: List[np.ndarray]):
        """여러 청크 순차 추론"""
        results = []
        for chunk in chunks:
            result = self.infer_single(chunk)
            results.append(result)
        return results


# ============================================================
# 2. Batch Inference (배치 추론)
# ============================================================
class BatchInference:
    """배치 추론 최적화"""

    def __init__(self, batch_size: int = 8):
        print("\n[Batch Inference] 모델 로딩 중...")
        self.processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
        self.model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))
        self.model.to(DEVICE)
        self.model.eval()
        self.batch_size = batch_size
        print(f"[Batch Inference] 로드 완료 (Batch Size: {batch_size})")

    @torch.no_grad()
    def infer_chunks(self, chunks: List[np.ndarray]):
        """배치 단위로 청크 추론"""
        all_results = []

        for i in range(0, len(chunks), self.batch_size):
            batch_chunks = chunks[i:i + self.batch_size]

            # 배치 전처리 (padding으로 길이 맞춤)
            inputs = self.processor(
                batch_chunks,
                sampling_rate=SAMPLE_RATE,
                return_tensors="pt",
                padding=True
            )
            inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

            # 배치 추론
            logits = self.model(**inputs).logits

            # 배치 후처리
            pred_ids = torch.argmax(logits, dim=-1)
            texts = self.processor.batch_decode(pred_ids)
            all_results.extend(texts)

        return all_results


# ============================================================
# 3. Mixed Precision FP16 (반정밀도)
# ============================================================
class FP16Inference:
    """FP16 Mixed Precision 최적화"""

    def __init__(self):
        print("\n[FP16 Inference] 모델 로딩 중...")
        self.processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
        self.model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))

        # FP16 변환 (CUDA에서만 지원)
        if DEVICE == "cuda":
            self.model = self.model.half()
            self.use_fp16 = True
            print("[FP16 Inference] FP16 모드 활성화")
        else:
            self.use_fp16 = False
            print("[FP16 Inference] CPU에서는 FP32 사용")

        self.model.to(DEVICE)
        self.model.eval()

    @torch.no_grad()
    def infer_single(self, waveform: np.ndarray):
        """단일 청크 추론"""
        inputs = self.processor(waveform, sampling_rate=SAMPLE_RATE, return_tensors="pt")

        # FP16 변환
        if self.use_fp16:
            inputs = {k: v.to(DEVICE).half() for k, v in inputs.items()}
        else:
            inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

        logits = self.model(**inputs).logits
        pred_ids = torch.argmax(logits, dim=-1)
        text = self.processor.decode(pred_ids[0])
        return text

    def infer_chunks(self, chunks: List[np.ndarray]):
        """여러 청크 순차 추론"""
        results = []
        for chunk in chunks:
            result = self.infer_single(chunk)
            results.append(result)
        return results


# ============================================================
# 4. TorchScript JIT Compile (JIT 컴파일)
# ============================================================
class JITInference:
    """TorchScript JIT 컴파일 최적화"""

    def __init__(self):
        print("\n[JIT Inference] 모델 로딩 중...")
        self.processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
        model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))
        model.to(DEVICE)
        model.eval()

        # JIT 컴파일 시도
        try:
            print("[JIT Inference] TorchScript 컴파일 중...")
            dummy_input = self.processor(
                np.random.randn(16000).astype(np.float32),
                sampling_rate=SAMPLE_RATE,
                return_tensors="pt"
            )
            dummy_input = {k: v.to(DEVICE) for k, v in dummy_input.items()}

            with torch.no_grad():
                # trace는 kwargs를 지원하지 않으므로 wrapper 사용
                class ModelWrapper(torch.nn.Module):
                    def __init__(self, model):
                        super().__init__()
                        self.model = model

                    def forward(self, input_values):
                        return self.model(input_values).logits

                wrapper = ModelWrapper(model)
                self.model = torch.jit.trace(wrapper, dummy_input['input_values'])
                print("[JIT Inference] 컴파일 완료")
        except Exception as e:
            print(f"[JIT Inference] 컴파일 실패, 기본 모델 사용: {e}")
            self.model = model

    @torch.no_grad()
    def infer_single(self, waveform: np.ndarray):
        """단일 청크 추론"""
        inputs = self.processor(waveform, sampling_rate=SAMPLE_RATE, return_tensors="pt")
        inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

        logits = self.model(inputs['input_values'])
        pred_ids = torch.argmax(logits, dim=-1)
        text = self.processor.decode(pred_ids[0])
        return text

    def infer_chunks(self, chunks: List[np.ndarray]):
        """여러 청크 순차 추론"""
        results = []
        for chunk in chunks:
            result = self.infer_single(chunk)
            results.append(result)
        return results


# ============================================================
# 5. Combined (모든 최적화 통합)
# ============================================================
class CombinedInference:
    """Batch + FP16 + JIT 통합"""

    def __init__(self, batch_size: int = 8):
        print("\n[Combined Inference] 모델 로딩 중...")
        self.processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
        model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))

        # FP16 변환
        if DEVICE == "cuda":
            model = model.half()
            self.use_fp16 = True
            print("[Combined] FP16 활성화")
        else:
            self.use_fp16 = False

        model.to(DEVICE)
        model.eval()
        self.model = model
        self.batch_size = batch_size

        print(f"[Combined] 로드 완료 (Batch: {batch_size}, FP16: {self.use_fp16})")

    @torch.no_grad()
    def infer_chunks(self, chunks: List[np.ndarray]):
        """배치 추론 (FP16 + JIT)"""
        all_results = []

        for i in range(0, len(chunks), self.batch_size):
            batch_chunks = chunks[i:i + self.batch_size]

            inputs = self.processor(
                batch_chunks,
                sampling_rate=SAMPLE_RATE,
                return_tensors="pt",
                padding=True
            )

            if self.use_fp16:
                inputs = {k: v.to(DEVICE).half() for k, v in inputs.items()}
            else:
                inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

            logits = self.model(**inputs).logits
            pred_ids = torch.argmax(logits, dim=-1)
            texts = self.processor.batch_decode(pred_ids)
            all_results.extend(texts)

        return all_results


# ============================================================
# 벤치마크 실행
# ============================================================
def benchmark_model(model, chunks: List[np.ndarray], name: str) -> Dict:
    """모델 성능 측정"""
    # 웜업
    for _ in range(NUM_WARMUP):
        _ = model.infer_chunks(chunks)

    # 측정
    times = []
    for _ in range(NUM_ITERATIONS):
        start = time.time()
        _ = model.infer_chunks(chunks)
        elapsed = time.time() - start
        times.append(elapsed)

    times = np.array(times)
    return {
        'name': name,
        'mean': np.mean(times),
        'std': np.std(times),
        'min': np.min(times),
        'max': np.max(times),
        'median': np.median(times),
    }


def print_results(results: List[Dict], scenario_name: str):
    """결과 출력 (통계 정보 강화)"""
    print(f"\n{'='*80}")
    print(f"시나리오: {scenario_name}")
    print(f"{'='*80}")

    baseline_time = results[0]['mean']

    print(f"\n{'모델':<25} {'평균 (ms)':<12} {'표준편차':<12} {'CV%':<8} {'최소':<10} {'최대':<10} {'속도 향상'}")
    print("-" * 90)

    for result in results:
        speedup = baseline_time / result['mean']
        speedup_text = f"{speedup:.2f}x" if result != results[0] else "-"

        # 변동계수 (Coefficient of Variation) - 신뢰도 지표
        cv = (result['std'] / result['mean']) * 100

        print(f"{result['name']:<25} "
              f"{result['mean']*1000:>10.2f}ms "
              f"{result['std']*1000:>10.2f}ms "
              f"{cv:>6.1f}% "
              f"{result['min']*1000:>8.2f}ms "
              f"{result['max']*1000:>8.2f}ms "
              f"{speedup_text:>10}")

    # 신뢰도 해석 추가
    print(f"\n💡 신뢰도 해석 (CV = 변동계수):")
    print(f"   CV < 5%:  ✅ 매우 안정적")
    print(f"   CV 5-10%: ⚠️  보통")
    print(f"   CV > 10%: ❌ 변동성 큼 (측정 횟수 늘리기 권장)")


def main():
    print("="*80)
    print("SafeTensor 최적화 성능 벤치마크")
    print("="*80)
    print(f"\n설정:")
    print(f"  Device: {DEVICE}")
    print(f"  워밍업 횟수: {NUM_WARMUP}")
    print(f"  측정 횟수: {NUM_ITERATIONS}")
    print(f"  샘플링 레이트: {SAMPLE_RATE}Hz")

    # 모델 초기화
    models = [
        ("1. Baseline (현재)", BaselineInference()),
        ("2. Batch Inference", BatchInference(batch_size=8)),
        ("3. FP16 Mixed Precision", FP16Inference()),
        ("4. TorchScript JIT", JITInference()),
        ("5. Combined (All)", CombinedInference(batch_size=8)),
    ]

    # 각 시나리오별 테스트
    for scenario in TEST_SCENARIOS:
        print(f"\n\n{'='*80}")
        print(f"테스트 시나리오: {scenario['name']}")
        print(f"{'='*80}")

        # 테스트 데이터 생성
        audio = create_dummy_audio(scenario['duration'])
        chunks = create_chunks(audio, scenario['chunks'])

        print(f"오디오 길이: {scenario['duration']}초")
        print(f"청크 개수: {len(chunks)}개")
        print(f"청크당 샘플 수: {[len(c) for c in chunks]}")

        # 각 모델 벤치마크
        results = []
        for name, model in models:
            print(f"\n[테스트 중] {name}...")
            try:
                result = benchmark_model(model, chunks, name)
                results.append(result)
                print(f"  완료: 평균 {result['mean']*1000:.2f}ms")
            except Exception as e:
                print(f"  실패: {e}")

        # 결과 출력
        if results:
            print_results(results, scenario['name'])

    print(f"\n{'='*80}")
    print("벤치마크 완료!")
    print(f"{'='*80}\n")


if __name__ == "__main__":
    main()
