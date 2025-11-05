from fastapi import APIRouter
from app.schemas import HealthCheckResponse

router = APIRouter(prefix="/health", tags=["Health"])

@router.get("/", response_model=HealthCheckResponse)
def health_check():
    """
    서버 헬스체크

    서버 상태와 사용 중인 디바이스 정보를 반환합니다.
    """
    import torch
    device = "cuda" if torch.cuda.is_available() else "cpu"
    return HealthCheckResponse(status="ok", device=device)
