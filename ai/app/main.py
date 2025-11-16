from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from app.routers import phoneme, health
import logging
import logging.handlers
from contextlib import asynccontextmanager
from pathlib import Path
import time
from prometheus_client import Counter, Histogram, Gauge, make_asgi_app

# 로깅 디렉토리 생성
LOG_DIR = Path("/app/logs")
LOG_DIR.mkdir(exist_ok=True)

# 로깅 설정
LOGGING_CONFIG = {
    "version": 1,
    "disable_existing_loggers": False,
    "formatters": {
        "default": {
            "format": "%(levelname)s: %(message)s",
        },
        "detailed": {
            "format": "%(asctime)s - %(name)s - %(levelname)s - %(message)s",
        },
    },
    "handlers": {
        "console": {
            "class": "logging.StreamHandler",
            "level": "INFO",
            "formatter": "default",
        },
        "file": {
            "class": "logging.handlers.RotatingFileHandler",
            "filename": str(LOG_DIR / "app.log"),
            "maxBytes": 10485760,  # 10MB
            "backupCount": 5,
            "level": "INFO",
            "formatter": "detailed",
        },
    },
    "root": {
        "level": "INFO",
        "handlers": ["console", "file"],
    },
}

logging.config.dictConfig(LOGGING_CONFIG)
logger = logging.getLogger(__name__)

# Prometheus 메트릭 정의
REQUEST_COUNT = Counter(
    'api_requests_total',
    'Total API requests',
    ['method', 'endpoint', 'status']
)
REQUEST_DURATION = Histogram(
    'api_request_duration_seconds',
    'API request duration in seconds',
    ['method', 'endpoint']
)
INFERENCE_DURATION = Histogram(
    'inference_duration_seconds',
    'Inference duration in seconds'
)
ACTIVE_REQUESTS = Gauge(
    'active_requests',
    'Number of active requests'
)

@asynccontextmanager
async def lifespan(app: FastAPI):
    # 시작 이벤트: 필요한 라이브러리 미리 로드
    logger.info("서버 시작: 필수 라이브러리 초기화 중...")

    try:
        # soundfile 미리 로드
        logger.info("soundfile 초기화...")
        import soundfile
        logger.info("soundfile 준비 완료")
    except Exception as e:
        logger.warning(f"soundfile 초기화 실패 (무시됨): {e}")

    try:
        # pydub 미리 로드
        logger.info("pydub 초기화...")
        from pydub import AudioSegment
        logger.info("pydub 준비 완료")
    except Exception as e:
        logger.warning(f"pydub 초기화 실패 (무시됨): {e}")

    # AI 모델 미리 로드
    try:
        logger.info("AI 모델 로딩 시작...")
        from app.services import inference
        logger.info(f"AI 모델 로딩 완료 - Device: {inference.DEVICE}, LoRA: {inference.USE_LORA}")
    except Exception as e:
        logger.error(f"AI 모델 로딩 실패: {e}")
        raise  # 모델 로딩 실패 시 서버 시작 중단

    logger.info("서버 시작 완료")
    yield
    # 종료 이벤트
    logger.info("서버 종료 중...")

app = FastAPI(
    title="Korean Pronunciation Checker",
    version="0.3.0",
    lifespan=lifespan
)

# CORS 설정 (환경별 분리)
from app.core.config import settings as app_settings

if app_settings.ENV == "production":
    # 프로덕션: 특정 origin만 허용
    allowed_origins = [origin.strip() for origin in app_settings.ALLOWED_ORIGINS.split(",")]
    logger.info(f"CORS 설정 (프로덕션) - Allowed origins: {allowed_origins}")
    app.add_middleware(
        CORSMiddleware,
        allow_origins=allowed_origins,
        allow_credentials=True,
        allow_methods=["GET", "POST"],
        allow_headers=["*"],
    )
else:
    # 개발: 모든 origin 허용
    logger.info("CORS 설정 (개발) - All origins allowed")
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

# Prometheus 메트릭 엔드포인트
metrics_app = make_asgi_app()
app.mount("/metrics", metrics_app)

# 미들웨어: 요청 메트릭 수집
@app.middleware("http")
async def metrics_middleware(request: Request, call_next):
    ACTIVE_REQUESTS.inc()
    start_time = time.time()

    response = await call_next(request)

    duration = time.time() - start_time
    ACTIVE_REQUESTS.dec()

    # 메트릭 기록
    REQUEST_COUNT.labels(
        method=request.method,
        endpoint=request.url.path,
        status=response.status_code
    ).inc()

    REQUEST_DURATION.labels(
        method=request.method,
        endpoint=request.url.path
    ).observe(duration)

    return response

# 라우터 등록
app.include_router(health.router)
app.include_router(phoneme.router)

@app.get("/")
def root():
    return {"message": "API is running"}
 