"""API 엔드포인트 테스트"""
import pytest
from httpx import AsyncClient
from app.main import app
import io
import numpy as np
import soundfile as sf


@pytest.fixture
def test_audio_wav():
    """테스트용 WAV 오디오 생성"""
    # 1초짜리 랜덤 오디오
    audio_data = np.random.randn(16000).astype(np.float32) * 0.1
    buffer = io.BytesIO()
    sf.write(buffer, audio_data, 16000, format='WAV')
    buffer.seek(0)
    return buffer


@pytest.mark.asyncio
class TestHealthEndpoint:
    """헬스체크 엔드포인트 테스트"""

    async def test_health_check(self):
        """헬스체크 성공 테스트"""
        async with AsyncClient(app=app, base_url="http://test") as ac:
            response = await ac.get("/health/")

        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "ok"
        assert "device" in data
        assert data["device"] in ["cuda", "cpu"]


@pytest.mark.asyncio
class TestPronunciationEndpoints:
    """발음 체크 엔드포인트 테스트"""

    async def test_jamo_check_valid(self, test_audio_wav):
        """자모 체크 - 유효한 요청"""
        async with AsyncClient(app=app, base_url="http://test") as ac:
            files = {"file": ("test.wav", test_audio_wav, "audio/wav")}
            data = {"target": "ㄱ"}
            response = await ac.post("/check/jamo", files=files, data=data)

        assert response.status_code == 200
        result = response.json()
        assert result["type"] == "jamo"
        assert result["target"] == "ㄱ"
        assert "decoded_tokens" in result
        assert "is_correct" in result
        assert "feedback" in result

    async def test_jamo_check_invalid_target(self, test_audio_wav):
        """자모 체크 - 잘못된 target (여러 글자)"""
        async with AsyncClient(app=app, base_url="http://test") as ac:
            files = {"file": ("test.wav", test_audio_wav, "audio/wav")}
            data = {"target": "ㄱㄴ"}  # 2글자
            response = await ac.post("/check/jamo", files=files, data=data)

        assert response.status_code == 400

    async def test_syllable_check_valid(self, test_audio_wav):
        """음절 체크 - 유효한 요청"""
        async with AsyncClient(app=app, base_url="http://test") as ac:
            files = {"file": ("test.wav", test_audio_wav, "audio/wav")}
            data = {"target": "가"}
            response = await ac.post("/check/syllable", files=files, data=data)

        assert response.status_code == 200
        result = response.json()
        assert result["type"] == "syllable"
        assert result["target"] == "가"
        assert "decomposed" in result

    async def test_word_check_valid(self, test_audio_wav):
        """단어 체크 - 유효한 요청"""
        async with AsyncClient(app=app, base_url="http://test") as ac:
            files = {"file": ("test.wav", test_audio_wav, "audio/wav")}
            data = {"target": "감자"}
            response = await ac.post("/check/word", files=files, data=data)

        assert response.status_code == 200
        result = response.json()
        assert result["type"] == "word"
        assert result["target"] == "감자"
        assert "syllables" in result

    async def test_invalid_file_type(self):
        """잘못된 파일 형식 테스트"""
        async with AsyncClient(app=app, base_url="http://test") as ac:
            # 텍스트 파일 전송
            files = {"file": ("test.txt", io.BytesIO(b"not audio"), "text/plain")}
            data = {"target": "ㄱ"}
            response = await ac.post("/check/jamo", files=files, data=data)

        assert response.status_code == 400


@pytest.mark.asyncio
class TestMetricsEndpoint:
    """메트릭 엔드포인트 테스트"""

    async def test_metrics_available(self):
        """메트릭 엔드포인트 접근 가능 테스트"""
        async with AsyncClient(app=app, base_url="http://test") as ac:
            response = await ac.get("/metrics")

        assert response.status_code == 200
        assert "api_requests_total" in response.text or "# HELP" in response.text
