from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.routers import phoneme, health

app = FastAPI(title="Korean Pronunciation Checker", version="0.3.0")

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
