# benchmark_onnx.py
import argparse, time, statistics as stats, numpy as np, onnxruntime as ort

def make_session(path, device, tuned=False):
    providers = []
    if device == "cuda":
        cuda_opts = {
            "device_id": 0,
            "arena_extend_strategy": "kSameAsRequested",
            "cudnn_conv_algo_search": "HEURISTIC",
            "do_copy_in_default_stream": True,
        }
        providers.append(("CUDAExecutionProvider", cuda_opts))
    providers.append("CPUExecutionProvider")

    if tuned:
        so = ort.SessionOptions()
        so.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
        so.enable_mem_pattern = True
        so.enable_cpu_mem_arena = True
        return ort.InferenceSession(path, so, providers=providers)
    else:
        return ort.InferenceSession(path, providers=providers)

def run_baseline(session, x):
    return session.run(None, {"audio": x})[0]

def run_iobinding(session, x):
    io = session.io_binding()
    x = np.ascontiguousarray(x.astype(np.float32))
    in_ov = ort.OrtValue.ortvalue_from_numpy(x, "cuda", 0)
    io.bind_input("audio", in_ov.device_name(), 0, np.float32, in_ov.shape(), in_ov.data_ptr())
    io.bind_output("logits", "cuda", 0)
    session.run_with_iobinding(io)
    return io.get_outputs()[0].numpy()

def bench(run_fn, warmup=10, runs=100):
    for _ in range(warmup):
        run_fn()
    times = []
    for _ in range(runs):
        t0 = time.perf_counter()
        run_fn()
        times.append((time.perf_counter() - t0)*1000.0)  # ms
    return {
        "mean_ms": stats.fmean(times),
        "p50_ms": np.percentile(times, 50),
        "p95_ms": np.percentile(times, 95),
        "runs": runs,
    }

def gen_audio(batch, secs, sr=16000, seed=42, dtype=np.float32):
    rng = np.random.default_rng(seed)
    T = int(sr*secs)
    x = rng.standard_normal((batch, T), dtype=dtype) * 0.1
    return np.ascontiguousarray(x.astype(np.float32))

def compare(a, b, rtol=1e-4, atol=1e-4):
    if a.shape != b.shape:
        return False, f"shape {a.shape} vs {b.shape}"
    return np.allclose(a, b, rtol=rtol, atol=atol), f"max|Δ|={np.max(np.abs(a-b)):.6f}"

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", required=True)
    ap.add_argument("--model_fp16")
    ap.add_argument("--device", choices=["cpu","cuda"], default="cuda")
    ap.add_argument("--batch", type=int, default=1)
    ap.add_argument("--secs", type=float, default=1.0)
    ap.add_argument("--runs", type=int, default=100)
    ap.add_argument("--rtol", type=float, default=1e-4)
    ap.add_argument("--atol", type=float, default=1e-4)
    args = ap.parse_args()

    print(f"Providers available: {ort.get_available_providers()}")
    x = gen_audio(args.batch, args.secs)

    # A) Baseline (기본 세션)
    sess_base = make_session(args.model, args.device, tuned=False)
    out_base = run_baseline(sess_base, x)
    res_base = bench(lambda: run_baseline(sess_base, x), runs=args.runs)
    print("\n[Baseline]    ", res_base)

    # B) Tuned SessionOptions (ENABLE_ALL 등)
    sess_tuned = make_session(args.model, args.device, tuned=True)
    out_tuned = run_baseline(sess_tuned, x)
    ok, msg = compare(out_base, out_tuned, args.rtol, args.atol)
    print(f"[Tuned vs Base] equal={ok} ({msg})")
    res_tuned = bench(lambda: run_baseline(sess_tuned, x), runs=args.runs)
    print("[Tuned]       ", res_tuned)

    # C) IOBinding (CUDA일 때만)
    if args.device == "cuda":
        out_io = run_iobinding(sess_tuned, x)
        ok, msg = compare(out_tuned, out_io, args.rtol, args.atol)
        print(f"[IOBinding vs Tuned] equal={ok} ({msg})")
        res_io = bench(lambda: run_iobinding(sess_tuned, x), runs=args.runs)
        print("[IOBinding]   ", res_io)

    # D) FP16 모델(있으면) – 정확도/성능 체크
    if args.model_fp16:
        sess_fp16 = make_session(args.model_fp16, args.device, tuned=True)
        out_fp16 = run_baseline(sess_fp16, x)
        ok, msg = compare(out_tuned, out_fp16, args.rtol, args.atol)
        print(f"[FP16 vs TunedFP32] equal={ok} ({msg})")
        res_fp16 = bench(lambda: run_baseline(sess_fp16, x), runs=args.runs)
        print("[FP16]        ", res_fp16)

    # 간단 처리량 지표(배치/초)
    def tput(res): return args.batch * args.runs / (res["mean_ms"]/1000.0) if res else 0
    print("\nThroughput (samples/sec):")
    print(f"  Baseline  : {tput(res_base):.2f}")
    print(f"  Tuned     : {tput(res_tuned):.2f}")
    if args.device == "cuda":
        print(f"  IOBinding : {tput(res_io):.2f}")
    if args.model_fp16:
        print(f"  FP16      : {tput(res_fp16):.2f}")

if __name__ == "__main__":
    main()
