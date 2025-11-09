from pathlib import Path
from pydantic_settings import BaseSettings
from typing import Optional
import os


class Settings(BaseSettings):
    """애플리케이션 설정"""

    # 경로 설정
    BASE_DIR: Path = Path(__file__).resolve().parent.parent
    PROJECT_ROOT: Path = BASE_DIR.parent

    # 모델 설정
    BASE_MODEL_PATH: str = "./models/base/slplab_wav2vec2_korean"
    LORA_MODEL_PATH: str = "./models/lora/final_model"
    USE_LORA: bool = True  # LoRA 사용 여부

    # 서버 설정
    HOST: str = "0.0.0.0"
    PORT: int = 8000
    RELOAD: bool = False

    # 오디오 처리 설정
    MAX_AUDIO_LENGTH_SECONDS: int = 30
    MAX_FILE_SIZE_MB: int = 10
    ALLOWED_AUDIO_EXTENSIONS: set = {".wav", ".mp3", ".ogg", ".flac", ".m4a", ".webm"}

    # 보안 설정
    ENV: str = "development"  # development, production
    ALLOWED_ORIGINS: str = "http://localhost:3000,http://localhost:5173"  # 쉼표로 구분

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False
        extra = "ignore"  # 추가 필드 무시 (기존 MODEL_PATH 허용)

    def get_base_model_path(self) -> Path:
        """베이스 모델 경로를 Path 객체로 반환"""
        if os.path.isabs(self.BASE_MODEL_PATH):
            return Path(self.BASE_MODEL_PATH)
        return self.PROJECT_ROOT / self.BASE_MODEL_PATH

    def get_lora_model_path(self) -> Path:
        """LoRA 모델 경로를 Path 객체로 반환"""
        if os.path.isabs(self.LORA_MODEL_PATH):
            return Path(self.LORA_MODEL_PATH)
        return self.PROJECT_ROOT / self.LORA_MODEL_PATH


# 싱글톤 설정 인스턴스
settings = Settings()
