# Test Scripts - 리팩토링 버전

Backend API 부하 테스트 및 기능 테스트 스크립트 모음

## 파일 구조

```
test_scripts/
├── common/                    # 공통 모듈 디렉토리
│   ├── __init__.py           # 패키지 초기화
│   ├── api_client.py         # 공통 API 클라이언트 모듈
│   └── utils.py              # 공통 유틸리티 함수
│
├── config.py                  # 설정 파일
├── requirements.txt           # 패키지 의존성
│
├── test_stage4_full.py       # 단일 유저 전체 플로우 테스트
├── test_multi_user.py        # 멀티 유저 부하 테스트
└── test_stage4.py            # Prometheus 메트릭 포함 버전
```

## 주요 변경사항

### 공통 모듈화

**common/api_client.py**
- 모든 API 호출 로직을 `TrainAPIClient` 클래스로 통합
- signup, login, start_stage, get_problem_set, submit_attempt, complete_stage

**common/utils.py**
- 공통 출력 함수: print_header, print_success, print_error, print_info
- 테스트 유저 생성: generate_test_users

**common/__init__.py**
- 패키지 초기화 및 공통 모듈 export

### 코드 간소화

**이전 (중복 코드)**
```python
# test_stage4_full.py
def login(self):
    response = self.session.post(...)
    # 50줄의 중복 코드

# test_multi_user.py
def login(self):
    response = self.session.post(...)
    # 50줄의 중복 코드
```

**이후 (공통 모듈)**
```python
# common/api_client.py
class TrainAPIClient:
    def login(self, email, password):
        # 한 곳에서만 관리

# test_stage4_full.py & test_multi_user.py
from common import TrainAPIClient

client = TrainAPIClient(base_url)
client.login(email, password)
```

## 사용 방법

### 방법 1: 로컬 Python 환경

#### 1. 패키지 설치
```bash
cd monitoring/test_scripts
pip install -r requirements.txt
```

#### 2. 설정 파일 수정 (config.py)
```python
BASE_URL = "http://localhost:8080"

TEST_USER = {
    "email": "user@example.com",
    "password": "password!@123",
    "nickname": "testuser"
}
```

#### 3. 단일 유저 테스트
```bash
python test_stage4_full.py
```
- Stage 4.1 / 4.2 개별 테스트
- 커스텀 문제 개수 설정
- 반복 테스트

#### 4. 멀티 유저 부하 테스트
```bash
python test_multi_user.py
```
- 빠른 테스트: 5명, 동시 5명
- 중간 테스트: 20명, 동시 10명
- 부하 테스트: 50명, 동시 20명
- 커스텀 설정
- 회원 일괄 가입

---

### 방법 2: Docker 사용 (권장)

Docker를 사용하면 환경 설정 없이 바로 테스트를 실행할 수 있습니다.

#### 1. 환경변수 설정
```bash
cp .env.example .env
# .env 파일 수정 (BASE_URL 등)
```

#### 2. 이미지 빌드
```bash
docker-compose build
```

#### 3. 테스트 실행

**인터랙티브 모드 (메뉴 선택)**
```bash
# 단일 유저 테스트
docker-compose --profile single up test-single-user

# 멀티 유저 테스트
docker-compose --profile multi up test-multi-user
```

**자동 모드 (환경변수 설정값으로 자동 실행)**
```bash
docker-compose --profile auto up test-load-auto
```

**커스텀 설정으로 실행**
```bash
docker-compose --profile auto run --rm \
  -e NUM_USERS=50 \
  -e MAX_WORKERS=20 \
  -e STAGE=4.2 \
  -e PROBLEM_COUNT=10 \
  test-load-auto
```

📖 **자세한 Docker 사용법은 [DOCKER_README.md](DOCKER_README.md) 참고**

## 기능 비교

| 기능 | 구버전 | 리팩토링 버전 |
|------|--------|---------------|
| 코드 라인 수 | ~850줄 | ~550줄 (35% 감소) |
| 중복 코드 | 많음 (API 호출 로직 중복) | 없음 (공통 모듈화) |
| 유지보수성 | 낮음 | 높음 |
| 확장성 | 어려움 | 쉬움 |
| 가독성 | 보통 | 좋음 |

## API 클라이언트 사용 예제

```python
from common import TrainAPIClient
import config

# 클라이언트 생성
client = TrainAPIClient(config.BASE_URL)

# 회원가입
client.signup("test@example.com", "password123", "testnick")

# 로그인
if client.login("test@example.com", "password123"):
    print("로그인 성공!")

    # 스테이지 시작
    session_id = client.start_stage("4.1", 5)

    # 문제 세트 생성
    problems = client.get_problem_set("4.1", 5, session_id)

    # 문제 시도
    for i, problem in enumerate(problems, 1):
        client.submit_attempt(session_id, "4.1", i, problem['koreanChar'], True)

    # 스테이지 완료
    client.complete_stage(session_id)

# 클라이언트 종료
client.close()
```

## 테스트 시나리오

### 단일 유저 테스트
1. 로그인
2. Stage 시작
3. 문제 생성 (5문제)
4. 각 문제 시도 (랜덤 정답/오답)
5. Stage 완료

### 멀티 유저 테스트
1. N명의 테스트 유저 생성 (user1@, user2@, ...)
2. (옵션) 자동 회원가입
3. M명씩 동시 접속
4. 각 유저가 독립적으로 전체 플로우 실행
5. 통계 수집 및 출력

## 성능 측정 지표

- **총 유저 수**: 테스트한 유저 수
- **동시 접속 수**: ThreadPoolExecutor의 max_workers
- **성공률**: 성공한 유저 / 전체 유저
- **요청 성공률**: 성공한 요청 / 전체 요청
- **평균 소요 시간**: 유저당 평균 완료 시간
- **처리량 (Throughput)**: users/sec

## 디렉토리 구조 상세

```
monitoring/test_scripts/
│
├── common/                         # 공통 모듈 패키지
│   ├── __init__.py                # 패키지 초기화
│   │   └── TrainAPIClient, print_*, generate_test_users export
│   │
│   ├── api_client.py              # API 클라이언트
│   │   └── class TrainAPIClient
│   │       ├── signup()
│   │       ├── login()
│   │       ├── start_stage()
│   │       ├── get_problem_set()
│   │       ├── submit_attempt()
│   │       └── complete_stage()
│   │
│   └── utils.py                   # 유틸리티 함수
│       ├── print_header()
│       ├── print_success()
│       ├── print_error()
│       ├── print_info()
│       └── generate_test_users()
│
├── config.py                       # 설정 (BASE_URL, TEST_USER)
├── requirements.txt                # 패키지 의존성
│
├── test_stage4_full.py            # 단일 유저 테스트 (common 사용)
├── test_multi_user.py             # 멀티 유저 테스트 (common 사용)
└── test_stage4.py                 # Prometheus 메트릭 버전
```

## 향후 개선 사항

- [ ] Prometheus 메트릭 통합 (test_stage4.py의 메트릭 기능)
- [ ] 비동기 처리 (asyncio) 지원
- [ ] 더 다양한 테스트 시나리오
- [ ] HTML 리포트 생성
- [ ] CI/CD 파이프라인 통합
