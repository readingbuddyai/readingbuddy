# Korean Pronunciation AI Server

> Wav2Vec2 + LoRA 기반 한국어 어린이 음성 인식 추론 서버

난독증 아동의 한글 발음을 음소 단위로 분석하는 FastAPI 기반 AI 서버입니다.

---

## 주요 기능

- **자모/음절/단어 검사**: ㄱ, ㅏ부터 "감자", "사과"까지 다단계 발음 분석
- **유사도 기반 평가**: 음소 간 유사도 계산으로 정확한 피드백
- **실시간 추론**: GPU 가속으로 평균 0.1초 이내 응답
- **프로덕션 최적화**: Prometheus 모니터링, 헬스체크, 로그 로테이션

---

## API 엔드포인트

### 1. 발음 검사 API

| 엔드포인트 | 설명 | 예시 입력 |
|-----------|------|----------|
| `POST /check/jamo` | 자모 단위 검사 | target="ㄱ" |
| `POST /check/syllable` | 음절 단위 검사 | target="가" |
| `POST /check/word` | 단어 단위 검사 | target="감자" |

### 2. 시스템 API

| 엔드포인트 | 설명 |
|-----------|------|
| `GET /health/` | 헬스체크 (GPU 상태 포함) |
| `GET /metrics` | Prometheus 메트릭 |
| `GET /` | 루트 (API 상태 확인) |

---

## 기술 스택

| Category | Technology |
|----------|-----------|
| **Framework** | FastAPI 0.115.4, Uvicorn 0.30.6 |
| **AI Model** | Wav2Vec2-XLS-R-300M + LoRA (r=32) |
| **ML** | PyTorch 2.5.1, Transformers 4.44.2, PEFT 0.14+ |
| **Audio** | librosa, soundfile, pydub, noisereduce |
| **Deployment** | Docker, NVIDIA Container Toolkit, CUDA 12.4 |
| **Monitoring** | Prometheus, Grafana |

---

## 추론 파이프라인

```
Audio File (WAV/MP3)
    │
    ▼
[Audio Preprocessing]
  - Format detection
  - Noise reduction
  - Resampling to 16kHz
    │
    ▼
[Wav2Vec2 + LoRA]
  - Feature extraction
  - CTC decoding
    │
    ▼
[Phoneme Similarity]
  - Jamo decomposition
  - Similarity calculation
    │
    ▼
[Response]
  {
    "is_correct": true,
    "feedback": "완벽해요!",
    "decoded_tokens": ["ㄱ", "ㅏ"]
  }
```

---

## 프로젝트 구조

```
ai/
├── app/
│   ├── main.py                 # FastAPI 앱 + 메트릭 + 로깅
│   ├── schemas.py              # Pydantic 스키마
│   ├── core/
│   │   └── config.py           # 환경 변수 설정
│   ├── routers/
│   │   ├── phoneme.py          # 발음 검사 API
│   │   └── health.py           # 헬스체크
│   └── services/
│       ├── inference.py        # Wav2Vec2 + LoRA 추론
│       ├── phoneme_similarity.py  # 유사도 계산
│       └── utils_audio.py      # 오디오 전처리
│
├── tests/                      # pytest 테스트
├── models/                     # AI 모델 (gitignore)
│   ├── base/                   # Wav2Vec2 베이스 (1.2GB)
│   └── lora/                   # LoRA 어댑터 (23MB)
│
├── monitoring/
│   └── prometheus.yml          # Prometheus 설정
│
├── Dockerfile
├── docker-compose.yml
├── requirements.txt
└── README.md
```

---

## 빠른 시작

### 1. 환경 요구사항

- Docker & Docker Compose
- NVIDIA GPU (선택사항, CPU도 가능)
- CUDA 12.4+ (GPU 사용 시)

### 2. 모델 다운로드

```bash
# 베이스 모델 (Wav2Vec2)
mkdir -p models/base
# slplab/wav2vec2_korean 모델을 models/base/slplab_wav2vec2_korean/에 배치

# LoRA 어댑터
mkdir -p models/lora
# 학습된 LoRA 가중치를 models/lora/final_model/에 배치
```

### 3. Docker로 실행

```bash
# 빌드 및 실행
docker-compose up -d

# 로그 확인
docker-compose logs -f ai-server

# 헬스체크
curl http://localhost:8000/health/
```

### 4. API 사용 예시

**자모 검사:**
```bash
curl -X POST http://localhost:8000/check/jamo \
  -F "file=@audio.wav" \
  -F "target=ㄱ"
```

**응답:**
```json
{
  "type": "jamo",
  "target": "ㄱ",
  "decoded_tokens": ["ㄱ", "ㅏ"],
  "is_correct": true,
  "feedback": "완벽해요! 'ㄱ' 발음이 정확해요!"
}
```

---

## 환경 변수

`.env` 파일 또는 Docker 환경 변수로 설정:

```bash
# 모델 경로
BASE_MODEL_PATH=./models/base/slplab_wav2vec2_korean
LORA_MODEL_PATH=./models/lora/final_model
USE_LORA=True

# 서버 설정
HOST=0.0.0.0
PORT=8000
ENV=production  # or development

# CORS (프로덕션)
ALLOWED_ORIGINS=https://readingbuddyai.co.kr

# 오디오 제한
MAX_AUDIO_LENGTH_SECONDS=30
MAX_FILE_SIZE_MB=10
ALLOWED_AUDIO_EXTENSIONS=.wav,.mp3,.m4a,.flac,.ogg

# 타임존
TZ=Asia/Seoul
```

---

## 모니터링

### Prometheus 메트릭

```bash
# 메트릭 확인
curl http://localhost:8000/metrics
```

**제공 메트릭:**
- `api_requests_total`: 전체 요청 수 (method, endpoint, status별)
- `api_request_duration_seconds`: 요청 처리 시간
- `inference_duration_seconds`: AI 추론 시간
- `active_requests`: 현재 처리 중인 요청 수

### Grafana 대시보드

```bash
# Grafana 접속 (docker-compose 실행 시)
http://localhost:3001
# ID: admin, PW: admin
```

---

## AI 모델 성능

### LoRA Fine-tuning 결과

| 모델 | PER | CER | 학습 데이터 |
|-----|-----|-----|-----------|
| **베이스 모델** | 36.67% | 26.40% | - |
| **LoRA r=32** | **14.55%** | **10.51%** | 100h 어린이 음성 |

**성능 향상:**
- PER: 60.3% 개선 (36.67% → 14.55%)
- CER: 60.2% 개선 (26.40% → 10.51%)
- 베이스 모델 대비 2.5배 정확도 향상

### 추론 성능

| 측정 항목 | GPU | CPU |
|----------|-----|-----|
| 첫 요청 (콜드 스타트) | ~15초 | ~30초 |
| 이후 요청 (1초 오디오) | 0.1~0.3초 | 2~5초 |
| GPU 메모리 사용량 | ~2.5GB | - |

### 학습 방법

- **3단계 Curriculum Learning**: Phase 1 (30h) → Phase 2 (90h) → Phase 3 (148h)
- **LoRA 설정**: r=32, alpha=32
- **데이터**: 한국어 어린이 음성 100시간

자세한 학습 과정은 [Fine-tuning/README.md](../Fine-tuning/README.md)를 참고하세요.

---

## 테스트

```bash
# 전체 테스트 실행
pytest

# 특정 테스트만 실행
pytest tests/test_api.py -v

# 커버리지 리포트
pytest --cov=app --cov-report=html
```

---

## 트러블슈팅

### GPU 메모리 부족

```bash
# USE_LORA=False로 베이스 모델만 사용
export USE_LORA=False
docker-compose restart ai-server
```

### 모델 로딩 실패

```bash
# 모델 경로 확인
docker-compose exec ai-server ls -la /app/models/

# 로그 확인
docker-compose logs ai-server | grep "모델"
```

### CORS 오류

```bash
# 개발 환경에서는 모든 origin 허용
export ENV=development
docker-compose restart ai-server
```

---

## 참고 링크

- **메인 프로젝트**: [README.md](../README.md)
- **Fine-tuning**: [Fine-tuning/README.md](../Fine-tuning/README.md)
- **모바일 앱**: [reading_buddy_app/README.md](../reading_buddy_app/README.md)
- **API Docs**: http://localhost:8000/docs (서버 실행 후)

---

## 라이선스

교육 목적으로 제작된 프로젝트입니다.

---

**Last Updated**: 2025-11-17
**Version**: 0.3.0
