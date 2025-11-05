from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.routers import phoneme, health
import logging
from contextlib import asynccontextmanager

# 로깅 설정
logging.basicConfig(
    level=logging.INFO,
    format='%(levelname)s: %(message)s'
)

logger = logging.getLogger(__name__)

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

    logger.info("서버 시작 완료")
    yield
    # 종료 이벤트
    logger.info("서버 종료 중...")

app = FastAPI(
    title="Korean Pronunciation Checker",
    version="0.3.0",
    lifespan=lifespan
)

# CORS 설정
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 라우터 등록
app.include_router(health.router)
app.include_router(phoneme.router)

@app.get("/")
def root():
    return {"message": "API is running"}
 