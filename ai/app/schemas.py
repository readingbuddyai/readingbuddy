"""API 요청/응답 스키마 정의"""
from pydantic import BaseModel, Field
from typing import List, Literal


class PronunciationCheckResponse(BaseModel):
    """발음 검사 공통 응답 모델"""
    type: Literal["jamo", "syllable", "word"] = Field(..., description="검사 유형")
    target: str = Field(..., description="목표 발음 텍스트")
    decoded_tokens: List[str] = Field(..., description="인식된 토큰 리스트")
    is_correct: bool = Field(..., description="정확한 발음 여부")
    feedback: str = Field(..., description="사용자 피드백 메시지")


class JamoCheckResponse(PronunciationCheckResponse):
    """자모 단위 검사 응답"""
    type: Literal["jamo"] = "jamo"

    class Config:
        json_schema_extra = {
            "example": {
                "type": "jamo",
                "target": "ㄱ",
                "decoded_tokens": ["G", "EU"],
                "is_correct": True,
                "feedback": "'ㄱ' 발음이 정확해요!"
            }
        }


class SyllableCheckResponse(PronunciationCheckResponse):
    """음절 단위 검사 응답"""
    type: Literal["syllable"] = "syllable"
    decomposed: List[str] = Field(..., description="분해된 자모 리스트")

    class Config:
        json_schema_extra = {
            "example": {
                "type": "syllable",
                "target": "가",
                "decomposed": ["ㄱ", "ㅏ"],
                "decoded_tokens": ["G", "A"],
                "is_correct": True,
                "feedback": "'가' 발음이 정확해요!"
            }
        }


class WordCheckResponse(PronunciationCheckResponse):
    """단어 단위 검사 응답"""
    type: Literal["word"] = "word"
    syllables: List[List[str]] = Field(..., description="각 음절의 자모 분해 리스트")

    class Config:
        json_schema_extra = {
            "example": {
                "type": "word",
                "target": "감자",
                "syllables": [["ㄱ", "ㅏ", "ㅁ"], ["ㅈ", "ㅏ"]],
                "decoded_tokens": ["G", "A", "M", "J", "A"],
                "is_correct": True,
                "feedback": "'감자' 발음이 정확해요!"
            }
        }


class HealthCheckResponse(BaseModel):
    """헬스체크 응답"""
    status: str = Field(..., description="서버 상태")
    device: str = Field(..., description="사용 중인 디바이스 (cuda/cpu)")

    class Config:
        json_schema_extra = {
            "example": {
                "status": "ok",
                "device": "cuda"
            }
        }


class ErrorResponse(BaseModel):
    """에러 응답"""
    detail: str = Field(..., description="에러 상세 메시지")

    class Config:
        json_schema_extra = {
            "example": {
                "detail": "오디오 파일을 읽을 수 없습니다."
            }
        }
