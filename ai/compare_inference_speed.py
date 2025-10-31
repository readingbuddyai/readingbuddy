"""
SafeTensors vs ONNX 추론 속도 비교 스크립트
"""
import time
import numpy as np
import torch
from transformers import Wav2Vec2ForCTC, Wav2Vec2Processor
import onnxruntime as ort
from pathlib import Path

# 모델 경로
MODEL_DIR = Path("models/slplab_wav2vec2_korean")
SAFETENSORS_PATH = MODEL_DIR / "model.safetensors"
ONNX_PATH = MODEL_DIR / "model.onnx"

# 테스트 설정
NUM_WARMUP = 5  # 워밍업 횟수
NUM_ITERATIONS = 50  # 측정 횟수
SAMPLE_RATE = 16000
AUDIO_DURATION = 3  # 초


def create_dummy_audio(duration_sec=AUDIO_DURATION):
    """더미 오디오 데이터 생성"""
    num_samples = int(SAMPLE_RATE * duration_sec)
    # 랜덤 노이즈 생성 (-1.0 ~ 1.0)
    audio = np.random.randn(num_samples).astype(np.float32)
    return audio


def benchmark_safetensors():
    """SafeTensors (PyTorch) 모델 벤치마크"""
    print("\n" + "=" * 60)
    print("SafeTensors (PyTorch) 모델 로딩 및 벤치마크")
    print("=" * 60)

    # 모델 및 프로세서 로드
    load_start = time.time()
    processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
    model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_DIR))
    model.eval()

    # CPU vs GPU
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model.to(device)
    print(f"Device: {device}")

    load_time = time.time() - load_start
    print(f"모델 로드 시간: {load_time:.4f}초")

    # 더미 오디오 생성
    audio = create_dummy_audio()

    # 워밍업
    print(f"\n워밍업 중... ({NUM_WARMUP}회)")
    with torch.no_grad():
        for _ in range(NUM_WARMUP):
            inputs = processor(audio, sampling_rate=SAMPLE_RATE, return_tensors="pt")
            input_values = inputs.input_values.to(device)
            _ = model(input_values)

    # 추론 시간 측정
    print(f"추론 시간 측정 중... ({NUM_ITERATIONS}회)")
    inference_times = []

    with torch.no_grad():
        for i in range(NUM_ITERATIONS):
            inputs = processor(audio, sampling_rate=SAMPLE_RATE, return_tensors="pt")
            input_values = inputs.input_values.to(device)

            start_time = time.time()
            outputs = model(input_values)
            end_time = time.time()

            inference_times.append(end_time - start_time)

            if (i + 1) % 10 == 0:
                print(f"  진행: {i + 1}/{NUM_ITERATIONS}")

    # 통계
    inference_times = np.array(inference_times)
    return {
        'model_type': 'SafeTensors (PyTorch)',
        'device': device,
        'load_time': load_time,
        'mean': np.mean(inference_times),
        'std': np.std(inference_times),
        'min': np.min(inference_times),
        'max': np.max(inference_times),
        'median': np.median(inference_times),
        'all_times': inference_times
    }


def benchmark_onnx():
    """ONNX 모델 벤치마크"""
    print("\n" + "=" * 60)
    print("ONNX 모델 로딩 및 벤치마크")
    print("=" * 60)

    # ONNX Runtime 세션 옵션 설정
    sess_options = ort.SessionOptions()
    sess_options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL

    # Provider 설정 (GPU가 있으면 CUDA, 없으면 CPU)
    providers = ['CUDAExecutionProvider', 'CPUExecutionProvider'] if ort.get_device() == 'GPU' else ['CPUExecutionProvider']

    # 모델 로드
    load_start = time.time()
    session = ort.InferenceSession(str(ONNX_PATH), sess_options, providers=providers)
    processor = Wav2Vec2Processor.from_pretrained(str(MODEL_DIR))
    load_time = time.time() - load_start

    print(f"Providers: {session.get_providers()}")
    print(f"모델 로드 시간: {load_time:.4f}초")

    # 입력/출력 이름 확인
    input_name = session.get_inputs()[0].name
    output_name = session.get_outputs()[0].name

    # 더미 오디오 생성
    audio = create_dummy_audio()

    # 워밍업
    print(f"\n워밍업 중... ({NUM_WARMUP}회)")
    for _ in range(NUM_WARMUP):
        inputs = processor(audio, sampling_rate=SAMPLE_RATE, return_tensors="np")
        input_values = inputs.input_values
        _ = session.run([output_name], {input_name: input_values})

    # 추론 시간 측정
    print(f"추론 시간 측정 중... ({NUM_ITERATIONS}회)")
    inference_times = []

    for i in range(NUM_ITERATIONS):
        inputs = processor(audio, sampling_rate=SAMPLE_RATE, return_tensors="np")
        input_values = inputs.input_values

        start_time = time.time()
        outputs = session.run([output_name], {input_name: input_values})
        end_time = time.time()

        inference_times.append(end_time - start_time)

        if (i + 1) % 10 == 0:
            print(f"  진행: {i + 1}/{NUM_ITERATIONS}")

    # 통계
    inference_times = np.array(inference_times)
    return {
        'model_type': 'ONNX',
        'device': session.get_providers()[0],
        'load_time': load_time,
        'mean': np.mean(inference_times),
        'std': np.std(inference_times),
        'min': np.min(inference_times),
        'max': np.max(inference_times),
        'median': np.median(inference_times),
        'all_times': inference_times
    }


def print_results(results):
    """결과 출력"""
    print("\n" + "=" * 60)
    print("벤치마크 결과")
    print("=" * 60)

    for result in results:
        print(f"\n[{result['model_type']}]")
        print(f"  Device/Provider: {result['device']}")
        print(f"  모델 로드 시간: {result['load_time']:.4f}초")
        print(f"  평균 추론 시간: {result['mean']*1000:.2f}ms")
        print(f"  표준편차: {result['std']*1000:.2f}ms")
        print(f"  최소 시간: {result['min']*1000:.2f}ms")
        print(f"  최대 시간: {result['max']*1000:.2f}ms")
        print(f"  중앙값: {result['median']*1000:.2f}ms")

    # 비교
    if len(results) == 2:
        print("\n" + "=" * 60)
        print("비교 분석")
        print("=" * 60)

        pytorch_mean = results[0]['mean']
        onnx_mean = results[1]['mean']

        speedup = pytorch_mean / onnx_mean
        faster_model = "ONNX" if speedup > 1 else "SafeTensors (PyTorch)"
        speedup_percent = abs((1 - speedup) * 100)

        print(f"\n{faster_model}가 {speedup_percent:.2f}% 더 빠릅니다.")
        print(f"속도 비율: {speedup:.2f}x")
        print(f"\nPyTorch 평균: {pytorch_mean*1000:.2f}ms")
        print(f"ONNX 평균: {onnx_mean*1000:.2f}ms")


def main():
    print("=" * 60)
    print("Wav2Vec2 모델 추론 속도 비교")
    print("SafeTensors (PyTorch) vs ONNX")
    print("=" * 60)
    print(f"\n설정:")
    print(f"  오디오 길이: {AUDIO_DURATION}초")
    print(f"  샘플링 레이트: {SAMPLE_RATE}Hz")
    print(f"  워밍업 횟수: {NUM_WARMUP}")
    print(f"  측정 횟수: {NUM_ITERATIONS}")

    results = []

    try:
        # SafeTensors 벤치마크
        safetensors_result = benchmark_safetensors()
        results.append(safetensors_result)
    except Exception as e:
        print(f"\nSafeTensors 벤치마크 실패: {e}")

    try:
        # ONNX 벤치마크
        onnx_result = benchmark_onnx()
        results.append(onnx_result)
    except Exception as e:
        print(f"\nONNX 벤치마크 실패: {e}")

    # 결과 출력
    if results:
        print_results(results)
    else:
        print("\n벤치마크 결과가 없습니다.")


if __name__ == "__main__":
    main()
