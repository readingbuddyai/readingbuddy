BASE_URL = "http://localhost:8080"

# 테스트 계정 정보
TEST_USER = {
    "email": "user@example.com",
    "password": "password!@123",
    "nickname": "testuser"
}

# 테스트 설정
TEST_CONFIG = {
    "stage_4_1_count": 5,

    "stage_4_2_count": 5,

    # 부하 테스트 시 API를 반복 호출할 횟수
    "load_test_iterations": 10,

    # 각 요청 사이의 대기 시간 (초)
    "delay_between_requests": 1,
}

# Prometheus 메트릭 서버 설정
METRICS_PORT = 8000

