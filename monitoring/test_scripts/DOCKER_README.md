# Docker로 테스트 실행하기

Docker를 사용하여 테스트 스크립트를 격리된 환경에서 실행할 수 있습니다.

## 파일 구조

```
test_scripts/
├── Dockerfile                # Docker 이미지 빌드 설정
├── docker-compose.yml        # Docker Compose 설정
├── .env.example              # 환경변수 예제
├── .dockerignore             # Docker 빌드 제외 파일
└── run_auto_test.py          # 자동 테스트 스크립트
```

## 사용 방법

### 1. 환경변수 설정

`.env.example`을 `.env`로 복사하고 수정:

```bash
cp .env.example .env
```

`.env` 파일 예시:
```env
BASE_URL=http://host.docker.internal:8080
NUM_USERS=20
MAX_WORKERS=10
STAGE=4.1
PROBLEM_COUNT=5
AUTO_SIGNUP=true
```

### 2. Docker 이미지 빌드

```bash
docker-compose build
```

### 3. 테스트 실행

#### 옵션 1: 단일 유저 테스트 (인터랙티브)

```bash
docker-compose --profile single up test-single-user
```

메뉴가 표시되며 직접 선택할 수 있습니다.

#### 옵션 2: 멀티 유저 테스트 (인터랙티브)

```bash
docker-compose --profile multi up test-multi-user
```

메뉴가 표시되며 직접 선택할 수 있습니다.

#### 옵션 3: 자동 부하 테스트 (비인터랙티브)

```bash
docker-compose --profile auto up test-load-auto
```

환경변수로 설정된 값으로 자동 실행됩니다.

### 4. 커스텀 설정으로 자동 테스트

환경변수를 직접 지정하여 실행:

```bash
docker-compose --profile auto run --rm \
  -e NUM_USERS=50 \
  -e MAX_WORKERS=20 \
  -e STAGE=4.2 \
  -e PROBLEM_COUNT=10 \
  test-load-auto
```

## Docker 명령어 참고

### 직접 Docker 사용 (docker-compose 없이)

#### 이미지 빌드
```bash
docker build -t test-scripts .
```

#### 단일 유저 테스트 실행
```bash
docker run -it --rm \
  -e BASE_URL=http://host.docker.internal:8080 \
  test-scripts python test_stage4_full.py
```

#### 멀티 유저 테스트 실행
```bash
docker run -it --rm \
  -e BASE_URL=http://host.docker.internal:8080 \
  test-scripts python test_multi_user.py
```

#### 자동 부하 테스트 실행
```bash
docker run --rm \
  -e BASE_URL=http://host.docker.internal:8080 \
  -e NUM_USERS=20 \
  -e MAX_WORKERS=10 \
  -e STAGE=4.1 \
  -e PROBLEM_COUNT=5 \
  -e AUTO_SIGNUP=true \
  test-scripts python run_auto_test.py
```

## 환경변수 설명

| 환경변수 | 설명 | 기본값 | 예시 |
|---------|------|--------|------|
| `BASE_URL` | Backend API 서버 URL | `http://localhost:8080` | `http://host.docker.internal:8080` |
| `NUM_USERS` | 테스트할 총 유저 수 | `20` | `50` |
| `MAX_WORKERS` | 동시 접속 스레드 수 | `10` | `20` |
| `STAGE` | 테스트할 스테이지 | `4.1` | `4.2` |
| `PROBLEM_COUNT` | 문제 개수 | `5` | `10` |
| `AUTO_SIGNUP` | 자동 회원가입 여부 | `true` | `false` |

## 네트워크 설정

### Windows/Mac
- `host.docker.internal`: 호스트 머신의 localhost를 가리킴
- Backend API가 localhost:8080에서 실행 중이면 그대로 사용 가능

### Linux
- `host.docker.internal`이 지원되지 않음
- 대안:
  1. 호스트 IP 직접 사용: `http://192.168.x.x:8080`
  2. Docker host network 사용:
     ```bash
     docker run --network host ...
     ```
  3. docker-compose에서 extra_hosts 사용:
     ```yaml
     extra_hosts:
       - "host.docker.internal:host-gateway"
     ```

## CI/CD 통합 예제

### GitHub Actions

```yaml
name: Load Test

on:
  push:
    branches: [ main ]

jobs:
  load-test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Build Docker image
      run: docker build -t test-scripts ./monitoring/test_scripts

    - name: Run load test
      run: |
        docker run --rm \
          -e BASE_URL=${{ secrets.API_BASE_URL }} \
          -e NUM_USERS=50 \
          -e MAX_WORKERS=20 \
          -e STAGE=4.1 \
          -e PROBLEM_COUNT=5 \
          test-scripts python run_auto_test.py
```

### GitLab CI

```yaml
load-test:
  stage: test
  image: docker:latest
  services:
    - docker:dind
  script:
    - cd monitoring/test_scripts
    - docker build -t test-scripts .
    - docker run --rm
        -e BASE_URL=$API_BASE_URL
        -e NUM_USERS=50
        -e MAX_WORKERS=20
        test-scripts python run_auto_test.py
```

## 트러블슈팅

### 1. Backend 서버에 연결할 수 없음

**문제**: `Connection refused` 에러

**해결**:
- Windows/Mac: `BASE_URL=http://host.docker.internal:8080` 사용
- Linux: 호스트 IP를 직접 사용하거나 `--add-host=host.docker.internal:host-gateway` 옵션 추가

### 2. 회원가입 실패

**문제**: 409 Conflict 또는 이미 존재하는 계정

**해결**:
- 정상 동작입니다 (AUTO_SIGNUP=true일 때 자동으로 스킵)
- 수동으로 계정 삭제가 필요하면 Backend DB 초기화

### 3. Docker 빌드 실패

**문제**: 패키지 설치 오류

**해결**:
```bash
# 캐시 없이 다시 빌드
docker-compose build --no-cache
```

## 성능 최적화

### 멀티 컨테이너로 부하 분산

여러 컨테이너를 동시에 실행하여 더 큰 부하 생성:

```bash
# 3개의 컨테이너로 총 60명 유저 테스트
for i in {1..3}; do
  docker-compose --profile auto run -d \
    -e NUM_USERS=20 \
    -e MAX_WORKERS=10 \
    test-load-auto
done
```

### 리소스 제한

```yaml
# docker-compose.yml
services:
  test-load-auto:
    # ...
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 1G
        reservations:
          cpus: '1.0'
          memory: 512M
```

## 로그 확인

```bash
# 실시간 로그
docker-compose --profile auto logs -f test-load-auto

# 특정 컨테이너 로그
docker logs test-multi-user

# 로그를 파일로 저장
docker-compose --profile auto logs test-load-auto > test-results.log
```

## 정리

```bash
# 컨테이너 중지 및 삭제
docker-compose down

# 이미지까지 삭제
docker-compose down --rmi all

# 볼륨까지 모두 삭제
docker-compose down -v --rmi all
```
