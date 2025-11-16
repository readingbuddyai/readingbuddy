"""오디오 유틸리티 테스트"""
import pytest
from app.services.utils_audio import detect_audio_format


class TestAudioFormatDetection:
    """오디오 포맷 감지 테스트"""

    def test_detect_wav(self):
        """WAV 포맷 감지 테스트"""
        wav_header = b'RIFF\x00\x00\x00\x00WAVE\x00\x00\x00\x00'
        assert detect_audio_format(wav_header) == "wav"

    def test_detect_webm(self):
        """WebM 포맷 감지 테스트"""
        webm_header = b'\x1a\x45\xdf\xa3' + b'\x00' * 12
        assert detect_audio_format(webm_header) == "webm"

    def test_detect_ogg(self):
        """OGG 포맷 감지 테스트"""
        ogg_header = b'OggS' + b'\x00' * 12
        assert detect_audio_format(ogg_header) == "ogg"

    def test_detect_mp3_id3(self):
        """MP3 (ID3 태그) 포맷 감지 테스트"""
        mp3_header = b'ID3' + b'\x00' * 13
        assert detect_audio_format(mp3_header) == "mp3"

    def test_detect_mp3_frame(self):
        """MP3 (프레임) 포맷 감지 테스트"""
        mp3_header = b'\xff\xfb' + b'\x00' * 14
        assert detect_audio_format(mp3_header) == "mp3"

    def test_detect_flac(self):
        """FLAC 포맷 감지 테스트"""
        flac_header = b'fLaC' + b'\x00' * 12
        assert detect_audio_format(flac_header) == "flac"

    def test_detect_m4a(self):
        """M4A 포맷 감지 테스트"""
        m4a_header = b'\x00\x00\x00\x00ftyp' + b'\x00' * 6
        assert detect_audio_format(m4a_header) == "m4a"

    def test_detect_unknown(self):
        """알 수 없는 포맷 테스트"""
        unknown_header = b'XXXX' + b'\x00' * 12
        assert detect_audio_format(unknown_header) == "unknown"

    def test_detect_empty(self):
        """빈 데이터 테스트"""
        assert detect_audio_format(b'') == "unknown"

    def test_detect_short_data(self):
        """짧은 데이터 테스트"""
        assert detect_audio_format(b'ABC') == "unknown"
