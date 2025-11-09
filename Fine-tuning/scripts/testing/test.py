#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Wav2Vec2 한국어 음소 인식 모델 파인튜닝 (진짜 스트리밍 버전)
IterableDataset으로 배치 단위 on-the-fly 로딩
"""

import os
import sys
import json
import argparse
import glob
import numpy as np
import torch
import soundfile as sf
from dataclasses import dataclass
from typing import Dict, List, Union, Optional, Iterator
from torch.utils.data import IterableDataset, DataLoader
from transformers import (
    AutoTokenizer,
    Wav2Vec2FeatureExtractor,
    Wav2Vec2Processor,
    Wav2Vec2ForCTC,
    TrainingArguments,
    Trainer,
    EarlyStoppingCallback,
    TrainerCallback
)
from multiprocessing import cpu_count
from datetime import datetime

from korean_g2p import KoreanG2P


# =====================
# User Config (Top-Level)
# - Edit these defaults instead of passing CLI flags.
# - `gpu` can be a string like "0" or "0,1" or a list like [0,1].
# =====================
USER_CONFIG = {
  # ============================================================
  # GPU 설정 - 1개 최적 (멀티 GPU는 오히려 느림!)
  # ============================================================
  "gpu": "1",

  # ============================================================
  # 배치 크기
  # ============================================================
  "batch_size": 16,  # GPU 1개, 총 16

  # ============================================================
  # 학습 설정
  # ============================================================
  "epochs": 5,
  "learning_rate": 5e-5,

  # ============================================================
  # 경로 - 새로운 디렉토리
  # ============================================================
  "extracted_dir": "/home/j-k13a206/data/child_extracted",
  "model_path": "/home/j-k13a206/models/wav2vec2-korean-phoneme",
  "output_dir": "/home/j-k13a206/finetunning/output_2025_11_05",  # 🔥 새 디렉토리

  # ============================================================
  # Trainer 설정
  # ============================================================
  "warmup_steps": 500,
  "save_steps": 5000,  # eval_steps와 동일 (early stopping 요구사항)
  "eval_steps": 5000,
  "logging_steps": 50,  # 자주 모니터링

  # ============================================================
  # Dataset
  # ============================================================
  "max_val_samples": None,  # 전체 사용
  "train_subdir": "1.Training",
  "val_subdir": "2.Validation",

  # ============================================================
  # 모델
  # ============================================================
  "freeze_feature_encoder": True,

  # ============================================================
  # 🔥 데이터 로딩 최적화 - CPU 풀가동
  # ============================================================
  "num_workers": 48,  # 96코어의 50% (안정적)
  # 더 공격적으로: 64 (96코어의 67%)

  "prefetch_factor": 10,  # 충분한 버퍼링
  # 더 공격적으로: 12 또는 16

  "pin_memory": True,
}


def _normalize_gpu_ids(gpu_spec) -> str:
    """Return a CUDA_VISIBLE_DEVICES string from various specs.

    Accepts: "0", "0,1", [0,1], (0,1), etc.
    Returns: e.g., "0" or "0,1". Empty string if invalid/None.
    """
    if gpu_spec is None:
        return ""
    if isinstance(gpu_spec, (list, tuple)):
        try:
            return ",".join(str(int(x)) for x in gpu_spec)
        except Exception:
            return ""
    # treat as string
    s = str(gpu_spec).strip()
    s = s.replace(" ", "")
    # basic validation: allow digits and commas
    if not s:
        return ""
    return s


class MarkdownLoggingCallback(TrainerCallback):
    """
    학습 과정을 마크다운 파일로 로깅하는 커스텀 콜백
    """

    def __init__(self, log_file_path: str):
        """
        Args:
            log_file_path: 로그 파일 경로 (.md)
        """
        self.log_file_path = log_file_path
        self.start_time = None
        self.training_logs = []
        self.eval_logs = []

        # 로그 디렉토리 생성
        os.makedirs(os.path.dirname(log_file_path), exist_ok=True)

        # 초기 로그 파일 생성
        self._write_header()

    def _write_header(self):
        """마크다운 헤더 작성"""
        with open(self.log_file_path, 'w', encoding='utf-8') as f:
            f.write(f"# Wav2Vec2 Fine-tuning Log\n\n")
            f.write(f"**시작 시간**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            f.write("---\n\n")

    def on_train_begin(self, args, state, control, **kwargs):
        """학습 시작 시 호출"""
        self.start_time = datetime.now()

        with open(self.log_file_path, 'a', encoding='utf-8') as f:
            f.write("## 학습 설정\n\n")
            f.write(f"- **학습률**: {args.learning_rate}\n")
            f.write(f"- **배치 크기**: {args.per_device_train_batch_size}\n")
            f.write(f"- **에폭 수**: {args.num_train_epochs}\n")
            f.write(f"- **Warmup Steps**: {args.warmup_steps}\n")
            f.write(f"- **Save Steps**: {args.save_steps}\n")
            f.write(f"- **Eval Steps**: {args.eval_steps}\n")
            f.write(f"- **FP16**: {args.fp16}\n")
            f.write("\n---\n\n")
            f.write("## 학습 진행 로그\n\n")
            f.write("| Step | Epoch | Loss | Learning Rate | Time |\n")
            f.write("|------|-------|------|---------------|------|\n")

    def on_log(self, args, state, control, logs=None, **kwargs):
        """로그 발생 시 호출"""
        if logs is None:
            return

        # 학습 loss 로깅
        if 'loss' in logs:
            step = state.global_step
            epoch = logs.get('epoch', 0)
            loss = logs.get('loss', 0)
            lr = logs.get('learning_rate', 0)

            elapsed = (datetime.now() - self.start_time).total_seconds()
            elapsed_str = f"{int(elapsed//3600):02d}:{int((elapsed%3600)//60):02d}:{int(elapsed%60):02d}"

            self.training_logs.append({
                'step': step,
                'epoch': epoch,
                'loss': loss,
                'lr': lr,
                'time': elapsed_str
            })

            with open(self.log_file_path, 'a', encoding='utf-8') as f:
                f.write(f"| {step} | {epoch:.2f} | {loss:.4f} | {lr:.2e} | {elapsed_str} |\n")

    def on_evaluate(self, args, state, control, metrics=None, **kwargs):
        """평가 시 호출"""
        if metrics is None:
            return

        step = state.global_step
        eval_loss = metrics.get('eval_loss', 0)

        self.eval_logs.append({
            'step': step,
            'eval_loss': eval_loss,
            'metrics': metrics
        })

        with open(self.log_file_path, 'a', encoding='utf-8') as f:
            f.write(f"\n### 평가 결과 (Step {step})\n\n")
            f.write(f"- **Eval Loss**: {eval_loss:.4f}\n")
            for key, value in metrics.items():
                if key != 'eval_loss':
                    f.write(f"- **{key}**: {value}\n")
            f.write("\n")

    def on_train_end(self, args, state, control, **kwargs):
        """학습 종료 시 호출"""
        end_time = datetime.now()
        total_time = (end_time - self.start_time).total_seconds()

        # train_dataset에서 통계 가져오기 (있으면)
        train_dataset = kwargs.get('train_dataloader', None)
        dataset_stats = None
        if train_dataset and hasattr(train_dataset.dataset, 'stats'):
            dataset_stats = train_dataset.dataset.stats

        with open(self.log_file_path, 'a', encoding='utf-8') as f:
            f.write("\n---\n\n")
            f.write("## 학습 완료\n\n")
            f.write(f"**종료 시간**: {end_time.strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            f.write(f"**총 학습 시간**: {int(total_time//3600)}시간 {int((total_time%3600)//60)}분 {int(total_time%60)}초\n\n")

            # 최종 통계
            if self.training_logs:
                final_loss = self.training_logs[-1]['loss']
                f.write(f"**최종 Training Loss**: {final_loss:.4f}\n\n")

            if self.eval_logs:
                best_eval_loss = min(log['eval_loss'] for log in self.eval_logs)
                f.write(f"**최고 Eval Loss**: {best_eval_loss:.4f}\n\n")

            # 데이터 로딩 통계
            if dataset_stats:
                f.write("### 데이터 로딩 통계\n\n")
                total_processed = sum(dataset_stats.values())
                success = dataset_stats.get('success', 0)
                f.write(f"- **전체 처리 파일**: {total_processed:,}개\n")
                f.write(f"- **성공**: {success:,}개 ({success/max(total_processed,1)*100:.1f}%)\n")

                total_failures = total_processed - success
                if total_failures > 0:
                    f.write(f"- **실패**: {total_failures:,}개 ({total_failures/max(total_processed,1)*100:.1f}%)\n")
                    f.write(f"  - RIFF 오류: {dataset_stats.get('riff_errors', 0):,}개\n")
                    f.write(f"  - 너무 짧음: {dataset_stats.get('too_short', 0):,}개\n")
                    f.write(f"  - 잘못된 오디오: {dataset_stats.get('invalid_audio', 0):,}개\n")
                    f.write(f"  - 빈 텍스트: {dataset_stats.get('empty_text', 0):,}개\n")
                    f.write(f"  - 빈 음소: {dataset_stats.get('empty_phonemes', 0):,}개\n")
                    f.write(f"  - 전처리 오류: {dataset_stats.get('processing_errors', 0):,}개\n")
                    f.write(f"  - 기타 오류: {dataset_stats.get('other_errors', 0):,}개\n")
                f.write("\n")

            # Loss 그래프용 데이터
            f.write("### Training Loss 추이\n\n")
            f.write("```\n")
            for log in self.training_logs[::10]:  # 10개마다 샘플링
                f.write(f"Step {log['step']:6d}: {log['loss']:.4f}\n")
            f.write("```\n\n")

            if self.eval_logs:
                f.write("### Evaluation Loss 추이\n\n")
                f.write("```\n")
                for log in self.eval_logs:
                    f.write(f"Step {log['step']:6d}: {log['eval_loss']:.4f}\n")
                f.write("```\n")


class PercentProgressCallback(TrainerCallback):
    """
    학습 진행률을 5% 단위로 텍스트 파일에 기록.
    nohup로 실행할 때 가벼운 진행 기록을 남기기 위한 용도.
    """

    def __init__(self, file_path: str, step_percent: int = 5, keep_history: bool = True):
        self.file_path = file_path
        self.step_percent = max(1, int(step_percent))
        self.keep_history = keep_history
        self.last_written = -1
        os.makedirs(os.path.dirname(self.file_path), exist_ok=True)

    def _write(self, percent: int, state):
        mode = 'a' if self.keep_history else 'w'
        with open(self.file_path, mode, encoding='utf-8') as f:
            f.write(
                f"{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  "
                f"step {getattr(state, 'global_step', 0)}/{getattr(state, 'max_steps', 0)}  "
                f"{percent}%\n"
            )

    def on_train_begin(self, args, state, control, **kwargs):
        self.last_written = -1
        if getattr(state, 'max_steps', 0):
            self._write(0, state)
            self.last_written = 0

    def on_step_end(self, args, state, control, **kwargs):
        max_steps = getattr(state, 'max_steps', 0) or 0
        if max_steps <= 0:
            return
        percent = int((getattr(state, 'global_step', 0) / max_steps) * 100)
        next_threshold = ((self.last_written // self.step_percent) + 1) * self.step_percent
        if percent >= next_threshold and next_threshold <= 100:
            self._write(next_threshold, state)
            self.last_written = next_threshold

    def on_train_end(self, args, state, control, **kwargs):
        if getattr(state, 'max_steps', 0) and (self.last_written < 100):
            self._write(100, state)
            self.last_written = 100

@dataclass
class DataCollatorCTCWithPadding:
    """
    CTC 학습을 위한 Data Collator
    오디오와 레이블을 패딩하여 배치 생성
    """
    processor: Wav2Vec2Processor
    padding: Union[bool, str] = True
    max_length: Optional[int] = None
    max_length_labels: Optional[int] = None
    pad_to_multiple_of: Optional[int] = None
    pad_to_multiple_of_labels: Optional[int] = None

    def __call__(self, features: List[Dict[str, Union[List[int], torch.Tensor]]]) -> Dict[str, torch.Tensor]:
        # 입력과 레이블을 분리
        input_features = [{"input_values": feature["input_values"]} for feature in features]
        label_features = [{"input_ids": feature["labels"]} for feature in features]

        # 오디오 패딩
        batch = self.processor.pad(
            input_features,
            padding=self.padding,
            max_length=self.max_length,
            pad_to_multiple_of=self.pad_to_multiple_of,
            return_tensors="pt",
        )

        # 레이블 패딩
        labels_batch = self.processor.tokenizer.pad(
            label_features,
            padding=self.padding,
            max_length=self.max_length_labels,
            pad_to_multiple_of=self.pad_to_multiple_of_labels,
            return_tensors="pt",
        )

        # 패딩 토큰을 -100으로 교체 (loss 계산시 무시)
        labels = labels_batch["input_ids"].masked_fill(
            labels_batch.attention_mask.ne(1), -100
        )

        batch["labels"] = labels

        return batch


class StreamingAudioDataset(IterableDataset):
    """
    스트리밍 방식 오디오 데이터셋
    파일 경로만 저장하고, 배치 단위로 on-the-fly 로드
    """

    def __init__(
        self,
        file_pairs: List[tuple],
        processor: Wav2Vec2Processor,
        g2p: KoreanG2P,
        shuffle: bool = False,
        seed: int = 42
    ):
        """
        Args:
            file_pairs: (audio_path, json_path) 튜플 리스트
            processor: Wav2Vec2Processor
            g2p: KoreanG2P 객체
            shuffle: 셔플 여부
            seed: 랜덤 시드
        """
        self.file_pairs = file_pairs
        self.processor = processor
        self.g2p = g2p
        self.shuffle = shuffle
        self.seed = seed

        # 통계 (개선: 더 자세한 분류)
        self.stats = {
            "success": 0,
            "riff_errors": 0,
            "too_short": 0,
            "invalid_audio": 0,  # NaN/Inf/Zero
            "empty_text": 0,
            "empty_phonemes": 0,
            "processing_errors": 0,
            "other_errors": 0
        }

    def __iter__(self) -> Iterator[Dict]:
        """배치 단위로 데이터를 yield"""
        file_pairs = self.file_pairs.copy()

        # 셔플
        if self.shuffle:
            import random
            rng = random.Random(self.seed)
            rng.shuffle(file_pairs)

        # 🔥 멀티프로세싱 worker 지원: 각 worker가 다른 데이터 처리
        worker_info = torch.utils.data.get_worker_info()
        if worker_info is not None:
            # Worker별로 데이터 분할
            worker_id = worker_info.id
            num_workers = worker_info.num_workers
            # 각 worker는 자신의 ID에 해당하는 데이터만 처리
            file_pairs = [fp for i, fp in enumerate(file_pairs) if i % num_workers == worker_id]

        for audio_path, json_path in file_pairs:
            try:
                # JSON 로드
                with open(json_path, 'r', encoding='utf-8') as f:
                    json_data = json.load(f)

                # 텍스트 추출
                text = json_data.get('Transcription', {}).get('LabelText', '')
                if not text or len(text.strip()) == 0:
                    self.stats["empty_text"] += 1
                    continue

                # 오디오 로드
                audio, sr = sf.read(audio_path)

                # ✅ 오디오 검증 1: 최소 길이 체크 (0.1초 = 1600 샘플 at 16kHz)
                if len(audio) < 1600:
                    self.stats["too_short"] += 1
                    continue

                # ✅ 오디오 검증 2: NaN/Inf 체크
                if np.isnan(audio).any() or np.isinf(audio).any():
                    self.stats["invalid_audio"] += 1
                    continue

                # ✅ 오디오 검증 3: 모든 값이 0인지 체크
                if np.abs(audio).max() < 1e-8:
                    self.stats["invalid_audio"] += 1
                    continue

                # 음소 변환
                phonemes = self.g2p.text_to_phonemes(text, apply_rules=True)

                # 음소 검증
                if not phonemes or len(phonemes.strip()) == 0:
                    self.stats["empty_phonemes"] += 1
                    continue

                # 오디오 전처리
                inputs = self.processor(
                    audio,
                    sampling_rate=sr,
                    return_tensors="pt",
                    padding=False
                )

                # ✅ 전처리 후 길이 체크
                if inputs.input_values.shape[1] < 1:
                    self.stats["processing_errors"] += 1
                    continue

                # 음소를 토큰 ID로 변환
                ids = self.processor.tokenizer(phonemes, add_special_tokens=False).input_ids
                if isinstance(ids, list) and len(ids) > 0 and isinstance(ids[0], list):
                    ids = ids[0]
                labels = ids

                # ✅ 레이블 길이 체크
                if len(labels) < 1:
                    self.stats["processing_errors"] += 1
                    continue

                self.stats["success"] += 1

                yield {
                    "input_values": inputs.input_values[0],
                    "labels": labels
                }

            except Exception as e:
                error_msg = str(e).lower()
                if "riff" in error_msg:
                    self.stats["riff_errors"] += 1
                else:
                    self.stats["other_errors"] += 1
                continue

    def __len__(self):
        """데이터셋 크기 (파일 개수)"""
        return len(self.file_pairs)


class Wav2Vec2StreamingFinetuner:
    """Wav2Vec2 파인튜닝 클래스 (스트리밍 버전)"""

    def __init__(
        self,
        model_path: str,
        output_dir: str = "./results",
        freeze_feature_encoder: bool = True
    ):
        """
        Args:
            model_path: 사전학습된 모델 경로
            output_dir: 결과 저장 경로
            freeze_feature_encoder: Feature Encoder 동결 여부
        """
        self.model_path = model_path
        self.output_dir = output_dir
        self.freeze_feature_encoder = freeze_feature_encoder

        # G2P 초기화
        self.g2p = KoreanG2P()

        # 프로세서 및 모델 로드
        print("모델 로드 중...")
        tokenizer = AutoTokenizer.from_pretrained(model_path)
        feature_extractor = Wav2Vec2FeatureExtractor.from_pretrained(model_path)
        self.processor = Wav2Vec2Processor(
            feature_extractor=feature_extractor,
            tokenizer=tokenizer
        )
        self.model = Wav2Vec2ForCTC.from_pretrained(model_path)

        # Feature Encoder 동결
        if self.freeze_feature_encoder:
            print("Feature Encoder 동결 (CTC 헤드만 학습)")
            self.model.freeze_feature_encoder()
            for param in self.model.wav2vec2.parameters():
                param.requires_grad = False

        # vocab 로드
        with open(os.path.join(model_path, 'vocab.json'), 'r') as f:
            self.vocab = json.load(f)

        print(f"모델 로드 완료! Vocab size: {len(self.vocab)}")

    def find_file_pairs(self, data_dirs: List[str]) -> List[tuple]:
        """
        압축 해제된 디렉토리에서 오디오-JSON 파일 쌍 찾기

        Args:
            data_dirs: 데이터 디렉토리 리스트

        Returns:
            (audio_path, json_path) 튜플 리스트
        """
        file_pairs = []

        for data_dir in data_dirs:
            audio_dir = os.path.join(data_dir, "원천데이터")
            label_dir = os.path.join(data_dir, "라벨링데이터")

            # JSON 파일 찾기
            json_files = glob.glob(f"{label_dir}/**/*.json", recursive=True)

            for json_path in json_files:
                # 대응하는 오디오 파일 경로 생성
                rel_path = os.path.relpath(json_path, label_dir)
                audio_path = os.path.join(audio_dir, rel_path.replace('.json', '.wav'))

                if os.path.exists(audio_path):
                    file_pairs.append((audio_path, json_path))

        return file_pairs

    def create_streaming_dataset(
        self,
        data_dirs: List[str],
        max_samples: Optional[int] = None,
        shuffle: bool = False
    ) -> StreamingAudioDataset:
        """
        스트리밍 데이터셋 생성 (파일 경로만 준비, 실제 로딩 안함)

        Args:
            data_dirs: 데이터 디렉토리 리스트
            max_samples: 최대 샘플 수
            shuffle: 셔플 여부

        Returns:
            StreamingAudioDataset
        """
        print("파일 목록 수집 중...")
        file_pairs = self.find_file_pairs(data_dirs)

        if max_samples:
            file_pairs = file_pairs[:max_samples]

        print(f"총 {len(file_pairs):,}개 파일 발견")
        print("✓ 파일 경로만 준비 완료 (실제 로딩은 학습 중 배치 단위로 수행)\n")

        return StreamingAudioDataset(
            file_pairs=file_pairs,
            processor=self.processor,
            g2p=self.g2p,
            shuffle=shuffle
        )

    def train(
        self,
        train_dataset: StreamingAudioDataset,
        eval_dataset: Optional[StreamingAudioDataset] = None,
        num_epochs: int = 10,
        batch_size: int = 4,
        learning_rate: float = 3e-4,
        warmup_steps: int = 500,
        eval_steps: int = 100,
        save_steps: int = 500,
        logging_steps: int = 10,
        log_file_path: Optional[str] = None,
        progress_file_path: Optional[str] = None,
    ):
        """
        모델 학습 (스트리밍 방식)

        Args:
            log_file_path: 마크다운 로그 파일 경로 (None이면 log/finetune_wav2vec2_streaming_part.md 사용)
        """
        print("\n" + "=" * 60)
        print("학습 설정")
        print("=" * 60)
        print(f"  학습 샘플 수: {len(train_dataset):,}개")
        if eval_dataset:
            print(f"  평가 샘플 수: {len(eval_dataset):,}개")
        print(f"  에폭: {num_epochs}")
        print(f"  배치 크기: {batch_size}")
        print(f"  학습률: {learning_rate}")
        print(f"  Feature Encoder 동결: {self.freeze_feature_encoder}")
        print("=" * 60)
        print("\n✨ 스트리밍 모드: 학습 시작과 동시에 데이터 로드!")
        print("   → 초기 대기 시간 없음")
        print("   → 배치 단위로 파일 읽기 → 전처리 → 학습\n")

        # 로그 파일 경로 설정
        if log_file_path is None:
            log_file_path = os.path.join(os.getcwd(), "log", "finetune_wav2vec2_streaming_part.md")

        print(f"📝 학습 로그 저장: {log_file_path}\n")

        # Data Collator
        data_collator = DataCollatorCTCWithPadding(
            processor=self.processor,
            padding=True
        )

        # Training Arguments
        training_args = TrainingArguments(
            output_dir=self.output_dir,
            group_by_length=False,  # IterableDataset에서는 False
            per_device_train_batch_size=batch_size,
            per_device_eval_batch_size=batch_size,
            eval_strategy="steps" if eval_dataset else "no",
            num_train_epochs=num_epochs,
            fp16=torch.cuda.is_available(),
            save_steps=save_steps,
            eval_steps=eval_steps if eval_dataset else None,
            logging_steps=logging_steps,
            learning_rate=learning_rate,
            warmup_steps=warmup_steps,
            save_total_limit=2,
            push_to_hub=False,
            remove_unused_columns=False,
            # Early Stopping 관련 설정
            load_best_model_at_end=True if eval_dataset else False,
            metric_for_best_model="loss",
            greater_is_better=False,
            # IterableDataset 설정
            max_steps=-1,  # epoch 기반으로 학습
            # 🔥 데이터 로딩 최적화
            dataloader_num_workers=USER_CONFIG.get("num_workers", 0),
            dataloader_prefetch_factor=USER_CONFIG.get("prefetch_factor", 2) if USER_CONFIG.get("num_workers", 0) > 0 else None,
            dataloader_pin_memory=USER_CONFIG.get("pin_memory", True),
        )

        # Callbacks
        # Progress file (5% step logging)
        if progress_file_path is None:
            progress_file_path = os.path.join(os.getcwd(), "log", "train_progress.txt")

        callbacks = [MarkdownLoggingCallback(log_file_path), PercentProgressCallback(progress_file_path, step_percent=5, keep_history=True)]
        if eval_dataset:
            # patience를 15로 증가 (대규모 데이터에서는 더 긴 patience 필요)
            callbacks.append(EarlyStoppingCallback(early_stopping_patience=15))

        trainer = Trainer(
            model=self.model,
            data_collator=data_collator,
            args=training_args,
            train_dataset=train_dataset,
            eval_dataset=eval_dataset,
            tokenizer=self.processor,
            callbacks=callbacks,
        )

        # 학습 시작
        print("학습 시작...\n")
        trainer.train()

        # 모델 저장
        print(f"\n모델 저장: {self.output_dir}/final")
        trainer.save_model(f"{self.output_dir}/final")
        self.processor.save_pretrained(f"{self.output_dir}/final")

        # 통계 출력 (개선: 더 자세한 분류)
        print("\n" + "=" * 60)
        print("학습 완료 - 데이터 로딩 통계")
        print("=" * 60)
        total_processed = sum(train_dataset.stats.values())
        print(f"전체 처리 파일: {total_processed:,}개")
        print(f"\n✓ 성공: {train_dataset.stats['success']:,}개 "
              f"({train_dataset.stats['success']/max(total_processed,1)*100:.1f}%)")

        # 실패 통계
        total_failures = total_processed - train_dataset.stats['success']
        if total_failures > 0:
            print(f"\n✗ 실패: {total_failures:,}개 ({total_failures/max(total_processed,1)*100:.1f}%)")
            print(f"  - RIFF 오류: {train_dataset.stats['riff_errors']:,}개")
            print(f"  - 너무 짧음 (<0.1초): {train_dataset.stats['too_short']:,}개")
            print(f"  - 잘못된 오디오 (NaN/Inf/Zero): {train_dataset.stats['invalid_audio']:,}개")
            print(f"  - 빈 텍스트: {train_dataset.stats['empty_text']:,}개")
            print(f"  - 빈 음소: {train_dataset.stats['empty_phonemes']:,}개")
            print(f"  - 전처리 오류: {train_dataset.stats['processing_errors']:,}개")
            print(f"  - 기타 오류: {train_dataset.stats['other_errors']:,}개")
        print("=" * 60)

        print(f"\n✓ 학습 완료!")
        print(f"📊 로그 파일: {log_file_path}")


def main():
    """메인 함수"""
    # 명령행 인자 파싱 (USER_CONFIG를 기본값으로 사용)
    parser = argparse.ArgumentParser(
        description='Wav2Vec2 한국어 어린이 음성 파인튜닝 (스트리밍 버전)',
        formatter_class=argparse.ArgumentDefaultsHelpFormatter
    )
    parser.add_argument('--gpu', type=str, default=USER_CONFIG["gpu"],
                        help='사용할 GPU 번호 (예: 0 또는 1,2,3)')
    parser.add_argument('--batch_size', type=int, default=USER_CONFIG["batch_size"],
                        help='배치 크기 (GPU당)')
    parser.add_argument('--epochs', type=int, default=USER_CONFIG["epochs"],
                        help='학습 에폭 수')
    parser.add_argument('--lr', type=float, default=USER_CONFIG["learning_rate"],
                        help='학습률')
    parser.add_argument('--extracted_dir', type=str, default=USER_CONFIG["extracted_dir"],
                        help='압축 해제된 데이터 디렉토리')
    parser.add_argument('--model_path', type=str, default=USER_CONFIG["model_path"],
                        help='사전학습된 모델 경로')
    parser.add_argument('--output_dir', type=str, default=USER_CONFIG["output_dir"],
                        help='출력 디렉토리')
    parser.add_argument('--warmup_steps', type=int, default=USER_CONFIG["warmup_steps"],
                        help='Warmup steps')
    parser.add_argument('--save_steps', type=int, default=USER_CONFIG["save_steps"],
                        help='모델 저장 주기 (steps)')
    parser.add_argument('--eval_steps', type=int, default=USER_CONFIG["eval_steps"],
                        help='평가 주기 (steps)')
    parser.add_argument('--logging_steps', type=int, default=USER_CONFIG["logging_steps"],
                        help='로깅 주기 (steps)')
    parser.add_argument('--max_val_samples', type=int, default=USER_CONFIG["max_val_samples"],
                        help='Validation 최대 샘플 수 (None이면 전체)')
    args = parser.parse_args()

    # GPU 설정 (개선된 로직)
    gpu_str = _normalize_gpu_ids(args.gpu)
    if gpu_str:
        os.environ["CUDA_VISIBLE_DEVICES"] = gpu_str
        num_gpus = len(gpu_str.split(','))
    else:
        num_gpus = torch.cuda.device_count() if torch.cuda.is_available() else 0

    print("=" * 60)
    print("Wav2Vec2 파인튜닝 - 스트리밍 버전 (진짜 효율적!)")
    print("=" * 60)
    print(f"사용 GPU: {gpu_str if gpu_str else 'CPU'} ({num_gpus}개)")
    print(f"배치 크기: {args.batch_size} (GPU당)")
    print(f"Effective 배치: {args.batch_size * max(num_gpus, 1)}")
    print(f"에폭: {args.epochs}")
    print(f"학습률: {args.lr}")
    print(f"모델 경로: {args.model_path}")
    print(f"출력 디렉토리: {args.output_dir}")
    print("=" * 60)

    # 압축 해제된 데이터 디렉토리 확인
    train_dir = os.path.join(args.extracted_dir, USER_CONFIG["train_subdir"])
    val_dir = os.path.join(args.extracted_dir, USER_CONFIG["val_subdir"])

    if not os.path.exists(train_dir):
        print(f"\n❌ 오류: {train_dir} 디렉토리가 없습니다!")
        print(f"\n먼저 다음 스크립트를 실행하세요:")
        print(f"  bash extract_data.sh")
        sys.exit(1)

    # Finetuner 초기화
    finetuner = Wav2Vec2StreamingFinetuner(
        model_path=args.model_path,
        output_dir=args.output_dir,
        freeze_feature_encoder=USER_CONFIG["freeze_feature_encoder"]
    )

    # Training 데이터셋 생성 (파일 경로만 준비)
    print("\n" + "=" * 60)
    print("Training 데이터셋 준비")
    print("=" * 60)
    train_dataset = finetuner.create_streaming_dataset(
        data_dirs=[train_dir],
        max_samples=None,  # 전체 사용
        shuffle=True
    )

    # Validation 데이터셋 생성
    print("=" * 60)
    print("Validation 데이터셋 준비")
    print("=" * 60)
    validation_dataset = finetuner.create_streaming_dataset(
        data_dirs=[val_dir],
        max_samples=args.max_val_samples,
        shuffle=False
    )

    # 학습
    finetuner.train(
        train_dataset=train_dataset,
        eval_dataset=validation_dataset,
        num_epochs=args.epochs,
        batch_size=args.batch_size,
        learning_rate=args.lr,
        warmup_steps=args.warmup_steps,
        save_steps=args.save_steps,
        eval_steps=args.eval_steps,
        logging_steps=args.logging_steps,
    )


if __name__ == "__main__":
    main()
