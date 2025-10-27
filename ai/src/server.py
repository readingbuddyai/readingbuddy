from fastapi import FastAPI, UploadFile, Form, HTTPException
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from typing import Dict, List
from pathlib import Path
import json
import os
import torch
import numpy as np
from transformers import Wav2Vec2Processor, Wav2Vec2ForCTC
from .utils_audio import load_audio_to_mono_16k

# os.environ["TRANSFORMERS_OFFLINE"] = "1"

BASE_DIR = Path(__file__).resolve().parent  # .../src
PROJECT_ROOT = BASE_DIR.parent              # 프로젝트 루트
MODEL_PATH = PROJECT_ROOT / "models" / "slplab_wav2vec2_korean"
TARGETS_PATH = BASE_DIR / "targets.json"

DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

app = FastAPI(title="Korean Phoneme Checker", version="0.2.1")

# CORS (원하면 도메인/아이피 넣으세요)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],      # 필요시 특정 도메인으로 제한
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ---------- 모델 로드 ----------
try:
    processor = Wav2Vec2Processor.from_pretrained(str(MODEL_PATH))
    model = Wav2Vec2ForCTC.from_pretrained(str(MODEL_PATH)).to(DEVICE)
    model.eval()
except Exception as e:
    raise RuntimeError(f"모델 로드 실패: {e}")

# ---------- TARGETS 로드 ----------
if not TARGETS_PATH.exists():
    raise FileNotFoundError(f"targets.json이 없습니다: {TARGETS_PATH}")
with open(TARGETS_PATH, "r", encoding="utf-8") as f:
    TARGETS: Dict[str, List[str]] = json.load(f)

SPECIAL_TOKENS = {"<s>", "</s>", "|", "[PAD]", "[UNK]"}  # (3) 디코딩 정제용

# ==========================
# 🔹 Utility functions
# ==========================

def get_vocab_maps():
    vocab_to_id = processor.tokenizer.get_vocab()
    id_to_vocab = {v: k for k, v in vocab_to_id.items()}
    return vocab_to_id, id_to_vocab

@torch.no_grad()
def infer_logits(wave_16k: np.ndarray) -> torch.Tensor:
    inputs = processor(wave_16k, sampling_rate=16000, return_tensors="pt", padding="longest")
    input_values = inputs.input_values.to(DEVICE)
    attn = inputs.attention_mask.to(DEVICE) if "attention_mask" in inputs else None
    logits = model(input_values, attention_mask=attn).logits  # [B, T, V]
    return logits.squeeze(0)

def ctc_decode(logits: torch.Tensor) -> str:
    ids = torch.argmax(logits, dim=-1).detach().cpu().numpy()
    return processor.batch_decode(torch.tensor([ids]))[0]

def clean_tokens(seq: str) -> List[str]:
    """특수토큰 제거 + 공백 분리"""
    toks = [t for t in seq.split() if t and t not in SPECIAL_TOKENS]
    return toks

def transcribe_audio(file: UploadFile) -> Dict:
    """파일을 받아서 디코딩 결과를 반환 (예외 처리 포함)"""
    try:
        # UploadFile은 내부적으로 SpooledTemporaryFile이므로 .file 그대로 전달 가능
        wave = load_audio_to_mono_16k(file.file)  # 16k mono numpy
    except Exception as e:
        raise HTTPException(status_code=422, detail=f"오디오 로딩 실패: {e}")
    logits = infer_logits(wave)
    decoded = ctc_decode(logits)
    return {"decoded_sequence": decoded}

# ==========================
# 🔹 API routes
# ==========================

@app.get("/health")
def health():
    return {"status": "ok", "device": DEVICE}

@app.get("/vocab")
def vocab():
    vocab_to_id, _ = get_vocab_maps()
    return {"size": len(vocab_to_id), "vocab": vocab_to_id}

@app.get("/targets")
def targets(key: str = None):
    """(보너스) TARGETS 조회용 – key 없으면 전체 size만"""
    if key:
        if key not in TARGETS:
            raise HTTPException(status_code=404, detail=f"unknown target_key: {key}")
        return {key: TARGETS[key]}
    return {"size": len(TARGETS)}

@app.post("/check_phoneme")
async def check_phoneme(file: UploadFile, target_key: str = Form(...)):
    """
    사용자가 발음한 음성이 target_key(예: 'ㄱ+ㅏ')와 '정확히' 일치하는지 판별
    - 길이 / 순서 모두 동일해야 정답
    """
    if target_key not in TARGETS:
        return JSONResponse(status_code=400, content={"error": f"unknown target_key: {target_key}"})

    # 1) 추론
    result = transcribe_audio(file)

    # 2) 목표 심볼
    target_symbols: List[str] = TARGETS.get(target_key, [])
    if not isinstance(target_symbols, list) or not all(isinstance(s, str) for s in target_symbols):
        raise HTTPException(status_code=500, detail=f"TARGETS 형식 오류: {target_key} -> {target_symbols}")

    # 3) 디코딩 토큰 정제 + 엄격 매칭 (4)
    decoded_tokens = clean_tokens(result["decoded_sequence"])  # 특수토큰 제거
    is_correct = (decoded_tokens == target_symbols)

    feedback = (
        f"정답이에요! '{target_key}'를 정확히 발음했어요."
        if is_correct else
        f"'{target_key}' 발음이 아니에요. 다시 시도해볼까요?"
    )

    return {
        "target_key": target_key,
        "target_symbols": target_symbols,
        "decoded_sequence": result["decoded_sequence"],
        "decoded_tokens": decoded_tokens,         # (디버깅/확인용)
        "is_correct": is_correct,
        "feedback": feedback
    }
