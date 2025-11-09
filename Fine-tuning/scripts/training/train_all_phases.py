#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Wav2Vec2-XLS-R-300M 어린이 음성 파인튜닝 - 전체 Phase 통합
Phase 1 (30h) → Phase 2 (90h) → Phase 3 (148h) → Phase 4 (선택)
LoRA + 하위층 동결 + Curriculum Learning
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
from typing import Dict, List, Union, Optional
from pathlib import Path
from torch.utils.data import IterableDataset, DataLoader
from transformers import (
    Wav2Vec2FeatureExtractor,
    Wav2Vec2Processor,
    Wav2Vec2ForCTC,
    TrainingArguments,
    Trainer,
    EarlyStoppingCallback,
    TrainerCallback
)
from peft import LoraConfig, get_peft_model, TaskType
from datetime import datetime

# 한국어 G2P (기존 파일에서 import)
sys.path.append('/home/j-k13a206/finetunning')
from korean_g2p import KoreanG2P


# =====================
# Phase 설정
# =====================
PHASE_CONFIG = {
    1: {
        'name': 'phase1_30h',
        'train_ratio': 0.20,  # Train의 20%
        'epochs': 10,
        'learning_rate': 5e-4,
        'warmup_steps': 500,
        'target_wer': 0.25,
        'augmentation_strength': 'weak',
    },
    2: {
        'name': 'phase2_90h',
        'train_ratio': 0.60,  # Train의 60%
        'epochs': 8,
        'learning_rate': 3e-4,
        'warmup_steps': 1000,
        'target_wer': 0.18,
        'augmentation_strength': 'strong',
    },
    3: {
        'name': 'phase3_148h',
        'train_ratio': 1.0,  # Train 100%
        'epochs': 5,
        'learning_rate': 1e-4,
        'warmup_steps': 500,
        'target_wer': 0.15,
        'augmentation_strength': 'medium',
    },
    4: {
        'name': 'phase4_full_finetune',
        'train_ratio': 1.0,
        'epochs': 2,
        'learning_rate': 5e-5,
        'warmup_steps': 100,
        'target_wer': 0.14,
        'augmentation_strength': 'none',
        'unfreeze_all': True,  # 전체 모델 해동
    }
}

# 공통 설정
COMMON_CONFIG = {
    'gpu': '3',
    'batch_size': 16,
    'gradient_accumulation_steps': 1,
    'weight_decay': 0.01,
    'gradient_clip': 1.0,
    'num_workers': 0,  # IterableDataset은 single-process가 더 안정적
    'prefetch_factor': None,
    'pin_memory': False,

    # 경로
    'data_dir': '/home/j-k13a206/data/child_subset_100h',
    'model_path': '/home/j-k13a206/models/wav2vec2-korean-phoneme',
    'output_base_dir': '/home/j-k13a206/fine_tunining_new/checkpoints',

    # LoRA 설정
    'lora_r': 16,
    'lora_alpha': 32,
    'lora_dropout': 0.1,
    'freeze_layers': list(range(0, 8)),  # 0~7층 동결

    # Early Stopping
    'early_stopping_patience': 3,
    'early_stopping_threshold': 0.005,
}


# =====================
# IterableDataset (On-the-fly loading)
# =====================
class StreamingChildSpeechDataset(IterableDataset):
    """스트리밍 방식으로 데이터 로딩"""

    def __init__(
        self,
        data_dir: str,
        split: str,  # "1.Training", "2.Validation_split", "3.Test"
        processor: Wav2Vec2Processor,
        g2p: KoreanG2P,
        max_samples: Optional[int] = None,
        train_ratio: float = 1.0,  # Phase별 사용 비율
    ):
        self.data_dir = Path(data_dir)
        self.split = split
        self.processor = processor
        self.g2p = g2p
        self.max_samples = max_samples
        self.train_ratio = train_ratio

        # JSON 파일 수집
        split_dir = self.data_dir / split
        json_pattern = str(split_dir / "**/*.json")
        self.json_files = sorted(glob.glob(json_pattern, recursive=True))

        # Train인 경우 비율만큼만 사용
        if "Training" in split and train_ratio < 1.0:
            n_samples = int(len(self.json_files) * train_ratio)
            self.json_files = self.json_files[:n_samples]

        if self.max_samples:
            self.json_files = self.json_files[:self.max_samples]

        print(f"[{split}] {len(self.json_files):,}개 샘플 로드 (ratio: {train_ratio:.1%})")

        self.stats = {
            'success': 0,
            'riff_errors': 0,
            'too_short': 0,
            'invalid_audio': 0,
            'empty_text': 0,
            'empty_phonemes': 0,
            'processing_errors': 0,
            'other_errors': 0,
        }

    def __iter__(self):
        """배치 단위로 샘플 생성"""
        for json_path in self.json_files:
            try:
                sample = self._load_sample(json_path)
                if sample is not None:
                    self.stats['success'] += 1
                    yield sample
            except Exception as e:
                self.stats['other_errors'] += 1
                continue

    def _load_sample(self, json_path: str) -> Optional[Dict]:
        """단일 샘플 로딩"""
        try:
            # JSON 로드
            with open(json_path, 'r', encoding='utf-8') as f:
                metadata = json.load(f)

            # WAV 경로 구성
            filename = metadata['File']['FileName']
            json_path_obj = Path(json_path)

            # 라벨링데이터 → 원천데이터로 경로 변환
            parts = json_path_obj.parts
            try:
                labeling_idx = parts.index("라벨링데이터")
                relative_parts = parts[labeling_idx + 1:]
            except ValueError:
                self.stats['other_errors'] += 1
                return None

            wav_path = self.data_dir / self.split / "원천데이터" / Path(*relative_parts[:-1]) / filename

            if not wav_path.exists():
                self.stats['other_errors'] += 1
                return None

            # 오디오 로드
            try:
                speech, sr = sf.read(str(wav_path))
            except Exception as e:
                if 'RIFF' in str(e):
                    self.stats['riff_errors'] += 1
                else:
                    self.stats['invalid_audio'] += 1
                return None

            # 너무 짧은 오디오 필터링
            if len(speech) < 1600:  # 0.1초
                self.stats['too_short'] += 1
                return None

            # 텍스트 추출
            text = metadata['Transcription']['LabelText'].strip()
            if not text:
                self.stats['empty_text'] += 1
                return None

            # 음소 변환
            phonemes = self.g2p.text_to_phonemes(text)
            if not phonemes:
                self.stats['empty_phonemes'] += 1
                return None

            # Feature 추출
            input_values = self.processor(
                speech,
                sampling_rate=16000,
                return_tensors="pt",
                padding=False
            ).input_values.squeeze(0)

            # 레이블 인코딩 (tokenizer 직접 사용)
            labels = self.processor.tokenizer(phonemes).input_ids

            return {
                'input_values': input_values,
                'labels': labels,
            }

        except Exception as e:
            self.stats['processing_errors'] += 1
            return None


@dataclass
class DataCollatorCTCWithPadding:
    """CTC용 Data Collator"""
    processor: Wav2Vec2Processor
    padding: Union[bool, str] = True

    def __call__(self, features: List[Dict]) -> Dict[str, torch.Tensor]:
        # 디버깅: features 확인
        if not features:
            raise ValueError(f"features가 비어있습니다! features={features}")

        # None과 빈 dict 필터링
        orig_len = len(features)
        features = [f for f in features if f is not None and f != {} and isinstance(f, dict)]

        if not features:
            raise ValueError(f"모든 features가 None 또는 빈 dict입니다! (원본 {orig_len}개)")

        # features에서 필요한 키만 추출
        input_values = [f["input_values"] for f in features if "input_values" in f]
        labels = [f["labels"] for f in features if "labels" in f]

        if not input_values or not labels:
            # 디버깅: 어떤 feature가 문제인지 확인
            feature_keys_list = [list(f.keys()) for f in features[:5]]
            raise ValueError(f"input_values 또는 labels가 비어있습니다. features={len(features)}, input_values={len(input_values)}, labels={len(labels)}, feature_keys_sample={feature_keys_list}")

        # input_values 패딩
        max_length = max(len(x) for x in input_values)
        padded_inputs = []
        for x in input_values:
            padding_length = max_length - len(x)
            padded = torch.nn.functional.pad(x, (0, padding_length), value=0.0)
            padded_inputs.append(padded)
        batch_input_values = torch.stack(padded_inputs)

        # labels 패딩
        max_label_length = max(len(l) for l in labels)
        padded_labels = []
        for l in labels:
            padding_length = max_label_length - len(l)
            padded = l + [-100] * padding_length
            padded_labels.append(padded)
        batch_labels = torch.tensor(padded_labels, dtype=torch.long)

        # 명시적으로 input_values와 labels만 반환
        result = {
            "input_values": batch_input_values,
            "labels": batch_labels,
        }

        return result


# =====================
# 로깅 Callback
# =====================
class DetailedLoggingCallback(TrainerCallback):
    """상세 로깅"""

    def __init__(self, log_dir: str, phase_name: str):
        self.log_dir = Path(log_dir)
        self.log_dir.mkdir(parents=True, exist_ok=True)
        self.log_file = self.log_dir / f"{phase_name}_training.log"
        self.phase_name = phase_name
        self.start_time = None
        self.training_logs = []
        self.eval_logs = []

    def on_train_begin(self, args, state, control, **kwargs):
        self.start_time = datetime.now()
        with open(self.log_file, 'w', encoding='utf-8') as f:
            f.write(f"# {self.phase_name} Training Log\n\n")
            f.write(f"**시작 시간**: {self.start_time.strftime('%Y-%m-%d %H:%M:%S')}\n\n")

    def on_log(self, args, state, control, logs=None, **kwargs):
        if logs:
            if 'loss' in logs:
                self.training_logs.append({
                    'step': state.global_step,
                    'loss': logs['loss'],
                    'learning_rate': logs.get('learning_rate', 0)
                })

    def on_evaluate(self, args, state, control, metrics=None, **kwargs):
        if metrics:
            self.eval_logs.append({
                'step': state.global_step,
                'eval_loss': metrics.get('eval_loss', 0),
                'metrics': metrics
            })

            with open(self.log_file, 'a', encoding='utf-8') as f:
                f.write(f"\n### Step {state.global_step}\n")
                f.write(f"- Eval Loss: {metrics.get('eval_loss', 0):.4f}\n")
                for k, v in metrics.items():
                    if k != 'eval_loss':
                        f.write(f"- {k}: {v}\n")

    def on_train_end(self, args, state, control, **kwargs):
        end_time = datetime.now()
        duration = (end_time - self.start_time).total_seconds()

        with open(self.log_file, 'a', encoding='utf-8') as f:
            f.write(f"\n---\n\n")
            f.write(f"**종료 시간**: {end_time.strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"**총 시간**: {int(duration//3600)}h {int((duration%3600)//60)}m\n\n")

            if self.eval_logs:
                best_loss = min(log['eval_loss'] for log in self.eval_logs)
                f.write(f"**최고 Eval Loss**: {best_loss:.4f}\n")


# =====================
# LoRA 적용
# =====================
def apply_lora_to_model(model, config):
    """모델에 LoRA 적용"""

    lora_config = LoraConfig(
        r=config['lora_r'],
        lora_alpha=config['lora_alpha'],
        target_modules=["q_proj", "k_proj", "v_proj"],
        lora_dropout=config['lora_dropout'],
        bias="none",
        # TaskType 제거 - Wav2Vec2ForCTC는 특별한 task_type 불필요
    )

    model = get_peft_model(model, lora_config)

    print(f"\n✅ LoRA 적용 완료")
    model.print_trainable_parameters()

    return model


def freeze_layers(model, freeze_layer_indices):
    """특정 층 동결"""
    for i in freeze_layer_indices:
        for param in model.wav2vec2.encoder.layers[i].parameters():
            param.requires_grad = False

    print(f"✅ {len(freeze_layer_indices)}개 층 동결 완료 (0~{max(freeze_layer_indices)})")


# =====================
# Phase 실행 함수
# =====================
def run_phase(
    phase_num: int,
    resume_from: Optional[str] = None,
    config: Dict = None,
):
    """단일 Phase 실행"""

    if config is None:
        config = COMMON_CONFIG

    phase_config = PHASE_CONFIG[phase_num]
    phase_name = phase_config['name']

    print("=" * 80)
    print(f"🚀 Phase {phase_num} 시작: {phase_name}")
    print("=" * 80)

    # GPU 설정
    os.environ['CUDA_VISIBLE_DEVICES'] = config['gpu']
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    print(f"Device: {device}")

    # 출력 디렉토리
    output_dir = Path(config['output_base_dir']) / phase_name
    output_dir.mkdir(parents=True, exist_ok=True)

    # Processor 로드
    print("\n📦 Processor 로드 중...")
    processor = Wav2Vec2Processor.from_pretrained(config['model_path'])
    g2p = KoreanG2P()

    # 모델 로드
    print("\n🤖 모델 로드 중...")
    # Phase 2+는 Trainer.train()에서 resume_from_checkpoint로 로드
    # 여기서는 항상 베이스 모델만 로드
    model = Wav2Vec2ForCTC.from_pretrained(
        config['model_path'],
        attention_dropout=0.1,
        hidden_dropout=0.1,
        feat_proj_dropout=0.0,
        mask_time_prob=0.05,
        layerdrop=0.1,
        ctc_loss_reduction="mean",
        pad_token_id=processor.tokenizer.pad_token_id,
        vocab_size=len(processor.tokenizer),
    )

    # LoRA 적용 (Phase 1만, Phase 2+는 체크포인트에서 로드)
    if phase_num == 1 or resume_from:
        if resume_from:
            print(f"  Phase {phase_num}: LoRA 설정 적용 (가중치는 체크포인트에서 로드)")
        model = apply_lora_to_model(model, config)
        freeze_layers(model, config['freeze_layers'])

    # Feature Extractor 동결
    model.freeze_feature_encoder()

    # Phase 4는 전체 해동
    if phase_num == 4 and phase_config.get('unfreeze_all', False):
        print("\n🔓 Phase 4: 전체 모델 해동")
        for param in model.parameters():
            param.requires_grad = True

    model.to(device)

    # 데이터셋 로드
    print(f"\n📊 데이터셋 로드 중... (Train ratio: {phase_config['train_ratio']:.1%})")

    train_dataset = StreamingChildSpeechDataset(
        data_dir=config['data_dir'],
        split="1.Training",
        processor=processor,
        g2p=g2p,
        train_ratio=phase_config['train_ratio'],
    )

    val_dataset = StreamingChildSpeechDataset(
        data_dir=config['data_dir'],
        split="2.Validation_split",
        processor=processor,
        g2p=g2p,
    )

    # Data Collator
    data_collator = DataCollatorCTCWithPadding(processor=processor)

    # max_steps 계산 (IterableDataset용)
    num_train_samples = len(train_dataset.json_files)
    steps_per_epoch = num_train_samples // (config['batch_size'] * config['gradient_accumulation_steps'])
    max_steps = steps_per_epoch * phase_config['epochs']

    print(f"\n📈 학습 스텝 계산:")
    print(f"  Train 샘플: {num_train_samples:,}개")
    print(f"  Steps per epoch: {steps_per_epoch:,}")
    print(f"  Total max_steps: {max_steps:,}")

    # Training Arguments
    training_args = TrainingArguments(
        output_dir=str(output_dir),
        per_device_train_batch_size=config['batch_size'],
        per_device_eval_batch_size=config['batch_size'],
        gradient_accumulation_steps=config['gradient_accumulation_steps'],
        eval_strategy="steps",
        eval_steps=500,
        save_steps=500,
        logging_steps=50,
        learning_rate=phase_config['learning_rate'],
        warmup_steps=phase_config['warmup_steps'],
        max_steps=max_steps,
        weight_decay=config['weight_decay'],
        max_grad_norm=config['gradient_clip'],
        fp16=True,
        dataloader_num_workers=config['num_workers'],
        dataloader_pin_memory=config['pin_memory'],
        load_best_model_at_end=True,
        metric_for_best_model="eval_loss",
        greater_is_better=False,
        save_total_limit=2,
        report_to="none",
        remove_unused_columns=True,
    )

    # Callbacks
    callbacks = [
        EarlyStoppingCallback(
            early_stopping_patience=config['early_stopping_patience'],
            early_stopping_threshold=config['early_stopping_threshold'],
        ),
        DetailedLoggingCallback(
            log_dir=str(output_dir),
            phase_name=phase_name,
        ),
    ]

    # Trainer
    trainer = Trainer(
        model=model,
        args=training_args,
        data_collator=data_collator,
        train_dataset=train_dataset,
        eval_dataset=val_dataset,
        callbacks=callbacks,
    )

    # 학습 시작
    print(f"\n🏃 Phase {phase_num} 학습 시작!")
    print(f"  목표 WER: < {phase_config['target_wer']*100:.0f}%")
    print(f"  Epochs: {phase_config['epochs']}")
    print(f"  Learning Rate: {phase_config['learning_rate']}")
    if resume_from:
        print(f"  체크포인트에서 재개: {resume_from}")
    print("-" * 80)

    # Phase 2+는 resume_from_checkpoint 사용
    if resume_from:
        trainer.train(resume_from_checkpoint=resume_from)
    else:
        trainer.train()

    # 최종 저장
    final_model_path = output_dir / "final_model"
    trainer.save_model(str(final_model_path))
    processor.save_pretrained(str(final_model_path))

    print(f"\n✅ Phase {phase_num} 완료!")
    print(f"  모델 저장: {final_model_path}")
    print(f"  Best 모델: {output_dir / 'checkpoint-best'}")
    print("=" * 80 + "\n")

    return str(output_dir / "checkpoint-best")


# =====================
# 메인 실행
# =====================
def main():
    parser = argparse.ArgumentParser(description="Wav2Vec2 Multi-Phase Training")
    parser.add_argument(
        '--phase',
        type=str,
        default='1',
        help='실행할 Phase: 1, 2, 3, 4, or all (기본값: 1)'
    )
    parser.add_argument(
        '--resume_from',
        type=str,
        default=None,
        help='재개할 체크포인트 경로 (Phase 2, 3, 4 시작 시)'
    )
    parser.add_argument(
        '--gpu',
        type=str,
        default='3',
        help='사용할 GPU ID (기본값: 3)'
    )

    args = parser.parse_args()

    # GPU 설정 오버라이드
    COMMON_CONFIG['gpu'] = args.gpu

    print("\n" + "=" * 80)
    print("🎯 Wav2Vec2-XLS-R-300M 어린이 음성 파인튜닝")
    print("=" * 80)
    print(f"실행 Phase: {args.phase}")
    print(f"GPU: {args.gpu}")
    print(f"데이터: {COMMON_CONFIG['data_dir']}")
    print("=" * 80 + "\n")

    if args.phase == 'all':
        # 전체 Phase 순차 실행
        print("🔥 전체 Phase 순차 실행 (1 → 2 → 3)\n")

        # Phase 1
        checkpoint_phase1 = run_phase(1, config=COMMON_CONFIG)

        # Phase 2
        checkpoint_phase2 = run_phase(2, resume_from=checkpoint_phase1, config=COMMON_CONFIG)

        # Phase 3
        checkpoint_phase3 = run_phase(3, resume_from=checkpoint_phase2, config=COMMON_CONFIG)

        print("\n" + "=" * 80)
        print("🎉 전체 Phase 완료!")
        print("=" * 80)
        print(f"Phase 1 체크포인트: {checkpoint_phase1}")
        print(f"Phase 2 체크포인트: {checkpoint_phase2}")
        print(f"Phase 3 체크포인트: {checkpoint_phase3}")
        print("=" * 80)

    else:
        # 단일 Phase 실행
        phase_num = int(args.phase)

        if phase_num not in [1, 2, 3, 4]:
            print("❌ 오류: Phase는 1, 2, 3, 4, 또는 'all'이어야 합니다.")
            sys.exit(1)

        if phase_num > 1 and not args.resume_from:
            print(f"⚠️  경고: Phase {phase_num}는 이전 체크포인트가 필요합니다.")
            print(f"   --resume_from <checkpoint_path> 옵션을 사용하세요.")
            sys.exit(1)

        checkpoint = run_phase(phase_num, resume_from=args.resume_from, config=COMMON_CONFIG)

        print(f"\n✅ Phase {phase_num} 완료!")
        print(f"체크포인트: {checkpoint}")


if __name__ == "__main__":
    main()
