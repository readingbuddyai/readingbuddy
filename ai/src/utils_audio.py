import torchaudio
import torch
import numpy as np

def load_audio_to_mono_16k(file_like) -> np.ndarray:
    wav, sr = torchaudio.load(file_like)
    if sr != 16000:
        wav = torchaudio.functional.resample(wav, sr, 16000)
    if wav.shape[0] > 1:
        wav = torch.mean(wav, dim=0, keepdim=True)
    return wav.squeeze(0).numpy()
