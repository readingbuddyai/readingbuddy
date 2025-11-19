# Reading Buddy - Backend API Server

> Spring Boot 기반 RESTful API 서버

난독증 아동을 위한 VR 한글 학습 시스템의 백엔드 API 서버입니다.

---

## 프로젝트 소개

Reading Buddy Backend는 VR 클라이언트와 모바일 앱을 지원하는 Spring Boot 기반 RESTful API 서버입니다. 사용자 인증, 학습 세션 관리, 학습 데이터 분석, BKT 알고리즘 기반 개인 맞춤형 학습 제공 등의 기능을 담당합니다.

## 주요 기능

### 1. 인증 및 사용자 관리
- **JWT 기반 인증**: Access Token + Refresh Token
- **디바이스 코드 로그인**: VR 기기 간편 로그인 (4자리 코드)
- **회원가입/로그인**: 이메일 기반 사용자 인증
- **사용자 프로필 관리**: 사용자 정보 조회 및 수정

### 2. 학습 세션 관리
- **세션 시작/종료**: 스테이지별 학습 세션 생성 및 종료
- **문제 출제**: 사용자별 맞춤형 문제 제공
- **학습 기록**: 정답/오답, 시도 횟수, 소요 시간 기록
- **진행도 추적**: 스테이지별 학습 진행 상황 관리

### 3. BKT (Bayesian Knowledge Tracing) 알고리즘
- **KC 숙련도 추적**: 음소별 학습 숙련도 계산
- **개인 맞춤형 학습**: 취약 음소 중심 문제 출제
- **학습 진행도 예측**: 다음 학습 단계 추천

### 4. 대시보드 및 통계
- **출석 현황**: 일별 학습 시간 및 출석 기록
- **학습 분석**: 스테이지별 통계, 정답률, 평균 시도 횟수
- **음소 분석**: 취약 음소 및 시도 횟수 랭킹
- **KC 숙련도 그래프**: BKT 기반 학습 진행도 시각화

### 5. 모니터링
- **Spring Boot Actuator**: 애플리케이션 헬스체크
- **Prometheus 메트릭**: JVM, HTTP, DB 커넥션 풀 메트릭
- **API 문서**: Swagger/OpenAPI 자동 생성

---

## 기술 스택

| 카테고리 | 기술 |
|---------|------|
| 언어 | Java 21 |
| 프레임워크 | Spring Boot 3.5.6 |
| 보안 | Spring Security, JWT (jjwt 0.12.3) |
| 데이터베이스 | PostgreSQL, Spring Data JPA |
| 빌드 도구 | Gradle 8.x |
| 모니터링 | Spring Boot Actuator, Micrometer, Prometheus |
| API 문서 | Swagger/OpenAPI (springdoc 2.8.9) |
| 배포 | Docker, Docker Compose |
| 유틸리티 | Lombok, Bouncy Castle |

---

## 프로젝트 구조

```
backend/
├── src/
│   ├── main/
│   │   ├── java/com/readingbuddy/backend/
│   │   │   ├── BackendApplication.java      # 메인 애플리케이션
│   │   │   │
│   │   │   ├── config/                      # 설정
│   │   │   │   ├── SecurityConfig.java      # Spring Security 설정
│   │   │   │   ├── SwaggerConfig.java       # Swagger 설정
│   │   │   │   └── ...
│   │   │   │
│   │   │   ├── auth/                        # 인증/인가
│   │   │   │   ├── jwt/                     # JWT 토큰 처리
│   │   │   │   ├── service/                 # 인증 서비스
│   │   │   │   ├── dto/                     # 인증 DTO
│   │   │   │   ├── entity/                  # 인증 엔티티
│   │   │   │   └── repository/              # 인증 레포지토리
│   │   │   │
│   │   │   ├── domain/                      # 도메인 모듈
│   │   │   │   ├── user/                    # 사용자 관리
│   │   │   │   │   ├── controller/          # UserController
│   │   │   │   │   ├── service/             # UserService
│   │   │   │   │   ├── entity/              # User 엔티티
│   │   │   │   │   ├── repository/          # UserRepository
│   │   │   │   │   └── dto/                 # User DTO
│   │   │   │   │
│   │   │   │   ├── train/                   # 학습 관리
│   │   │   │   │   ├── controller/          # TrainController
│   │   │   │   │   ├── service/             # TrainService
│   │   │   │   │   ├── entity/              # Session, Problem 엔티티
│   │   │   │   │   ├── repository/          # TrainRepository
│   │   │   │   │   └── dto/                 # Train DTO
│   │   │   │   │
│   │   │   │   ├── dashboard/               # 대시보드/통계
│   │   │   │   │   ├── controller/          # DashboardController
│   │   │   │   │   ├── service/             # DashboardService
│   │   │   │   │   ├── repository/          # DashboardRepository
│   │   │   │   │   └── dto/                 # Dashboard DTO
│   │   │   │   │
│   │   │   │   └── bkt/                     # BKT 알고리즘
│   │   │   │       ├── service/             # BKT 계산 서비스
│   │   │   │       ├── entity/              # KC 엔티티
│   │   │   │       ├── repository/          # BKT Repository
│   │   │   │       └── enums/               # BKT 상수
│   │   │   │
│   │   │   └── common/                      # 공통 유틸리티
│   │   │       ├── util/                    # 유틸리티 클래스
│   │   │       ├── properties/              # 설정 프로퍼티
│   │   │       └── service/                 # 공통 서비스
│   │   │
│   │   └── resources/
│   │       ├── application.properties       # 애플리케이션 설정
│   │       └── data.sql                     # 초기 데이터
│   │
│   └── test/                                # 테스트
│
├── build.gradle                             # Gradle 빌드 설정
├── Dockerfile                               # Docker 이미지
├── docker-compose.yml                       # Docker Compose 설정
└── README.md                                # 프로젝트 문서
```

---

## 환경 요구사항

### 개발 환경
- **Java**: JDK 21 이상
- **Gradle**: 8.x
- **IDE**: IntelliJ IDEA 또는 Eclipse
- **데이터베이스**: PostgreSQL 15+

### 실행 환경
- **Docker**: 20.10+
- **Docker Compose**: 2.0+

---

## 빠른 시작

### 1. 저장소 클론

```bash
git clone https://lab.ssafy.com/s13-final/S13P31A206.git
cd S13P31A206/backend
```

### 2. 환경 변수 설정

`.env` 파일 생성:

```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=readingbuddy
DB_USERNAME=postgres
DB_PASSWORD=your_password

# JWT
JWT_SECRET=your_jwt_secret_key_min_256_bits
JWT_ACCESS_EXPIRATION=3600000
JWT_REFRESH_EXPIRATION=604800000

# Server
SERVER_PORT=8080
```

### 3. 데이터베이스 설정

PostgreSQL 설치 및 데이터베이스 생성:

```sql
CREATE DATABASE readingbuddy;
CREATE USER readingbuddy_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE readingbuddy TO readingbuddy_user;
```

### 4. 애플리케이션 실행

#### Gradle로 실행
```bash
# 빌드
./gradlew build

# 실행
./gradlew bootRun
```

#### Docker Compose로 실행
```bash
docker-compose up -d
```

### 5. API 문서 확인

애플리케이션 실행 후:
- **Swagger UI**: http://localhost:8080/swagger-ui.html
- **API Docs**: http://localhost:8080/api-docs
- **Actuator**: http://localhost:8080/actuator

---

## API 엔드포인트

### 인증 API

| 메서드 | 엔드포인트 | 설명 |
|--------|-----------|------|
| POST | `/auth/signup` | 회원가입 |
| POST | `/auth/login` | 로그인 |
| POST | `/auth/refresh` | 토큰 갱신 |
| POST | `/auth/device/request` | VR 디바이스 코드 요청 |
| POST | `/auth/device/verify` | VR 디바이스 코드 인증 |

### 사용자 API

| 메서드 | 엔드포인트 | 설명 |
|--------|-----------|------|
| GET | `/users/me` | 내 정보 조회 |
| PUT | `/users/me` | 내 정보 수정 |
| DELETE | `/users/me` | 회원 탈퇴 |

### 학습 세션 API

| 메서드 | 엔드포인트 | 설명 |
|--------|-----------|------|
| POST | `/sessions/start` | 세션 시작 |
| POST | `/sessions/end` | 세션 종료 |
| GET | `/sessions/{id}` | 세션 조회 |
| POST | `/sessions/{id}/problems` | 문제 제출 |

### 대시보드 API

| 메서드 | 엔드포인트 | 설명 |
|--------|-----------|------|
| GET | `/dashboard/attendance` | 출석 현황 |
| GET | `/dashboard/statistics` | 학습 통계 |
| GET | `/dashboard/phonemes` | 음소 분석 |
| GET | `/dashboard/kc` | KC 숙련도 |

---

## 개발 가이드

### 새 도메인 추가하기

1. **엔티티 생성**
```java
@Entity
@Table(name = "example")
@Getter
@NoArgsConstructor
public class Example {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String name;
}
```

2. **레포지토리 생성**
```java
public interface ExampleRepository extends JpaRepository<Example, Long> {
    Optional<Example> findByName(String name);
}
```

3. **서비스 생성**
```java
@Service
@RequiredArgsConstructor
public class ExampleService {
    private final ExampleRepository exampleRepository;

    public ExampleDto getExample(Long id) {
        return exampleRepository.findById(id)
            .map(ExampleDto::from)
            .orElseThrow(() -> new EntityNotFoundException("Example not found"));
    }
}
```

4. **컨트롤러 생성**
```java
@RestController
@RequestMapping("/api/examples")
@RequiredArgsConstructor
public class ExampleController {
    private final ExampleService exampleService;

    @GetMapping("/{id}")
    public ResponseEntity<ExampleDto> getExample(@PathVariable Long id) {
        return ResponseEntity.ok(exampleService.getExample(id));
    }
}
```

### JWT 인증 플로우

1. **로그인 요청**
```bash
POST /auth/login
{
  "email": "user@example.com",
  "password": "password"
}
```

2. **토큰 발급**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

3. **인증 필요한 API 호출**
```bash
GET /users/me
Authorization: Bearer {accessToken}
```

### BKT 알고리즘 적용

```java
@Service
@RequiredArgsConstructor
public class BKTService {

    public double calculateKC(List<ProblemResult> results) {
        // BKT 알고리즘으로 KC 숙련도 계산
        // P(L) = P(L0) + P(T) * (1 - P(L0))
        // ...
    }

    public List<Problem> getRecommendedProblems(Long userId) {
        // 취약 음소 기반 문제 추천
        // ...
    }
}
```

---

## 테스트

### 단위 테스트 실행

```bash
./gradlew test
```

### 통합 테스트 실행

```bash
./gradlew integrationTest
```

### 테스트 커버리지 확인

```bash
./gradlew jacocoTestReport
# 리포트: build/reports/jacoco/test/html/index.html
```

---

## 빌드 및 배포

### Gradle 빌드

```bash
# 테스트 포함 빌드
./gradlew build

# 테스트 제외 빌드
./gradlew build -x test

# JAR 파일 위치
ls -la build/libs/backend-0.0.1-SNAPSHOT.jar
```

### Docker 이미지 빌드

```bash
# 이미지 빌드
docker build -t readingbuddy-backend .

# 컨테이너 실행
docker run -p 8080:8080 --env-file .env readingbuddy-backend
```

### Docker Compose 배포

```bash
# 빌드 및 실행
docker-compose up --build -d

# 로그 확인
docker-compose logs -f backend

# 중단
docker-compose down
```

---

## 모니터링

### Actuator 엔드포인트

- **Health Check**: http://localhost:8080/actuator/health
- **Metrics**: http://localhost:8080/actuator/metrics
- **Prometheus**: http://localhost:8080/actuator/prometheus

### Prometheus & Grafana

[monitoring/README.md](../monitoring/README.md) 참고

---

## 트러블슈팅

### 데이터베이스 연결 실패

**오류:**
```
Connection to localhost:5432 refused
```

**해결:**
1. PostgreSQL 서비스 실행 확인
2. `.env` 파일의 DB 설정 확인
3. 방화벽 및 포트 확인

### JWT 토큰 검증 실패

**오류:**
```
JWT signature does not match locally computed signature
```

**해결:**
1. `JWT_SECRET` 환경변수 확인 (최소 256비트)
2. 토큰 만료 시간 확인
3. Refresh Token으로 재발급

### Port 8080 이미 사용 중

**해결:**
```bash
# 포트 사용 프로세스 확인
lsof -i :8080

# 프로세스 종료
kill -9 {PID}

# 또는 다른 포트 사용
SERVER_PORT=8081 ./gradlew bootRun
```

---

## 참고 자료

### 프로젝트 문서
- **메인 프로젝트**: [README.md](../README.md)
- **VR Frontend**: [frontend/README.md](../frontend/README.md)
- **Mobile App**: [reading_buddy_app/README.md](../reading_buddy_app/README.md)
- **AI Server**: [ai/README.md](../ai/README.md)
- **Monitoring**: [monitoring/README.md](../monitoring/README.md)

### Spring 공식 문서
- [Spring Boot Reference](https://docs.spring.io/spring-boot/docs/current/reference/html/)
- [Spring Security Reference](https://docs.spring.io/spring-security/reference/)
- [Spring Data JPA Reference](https://docs.spring.io/spring-data/jpa/reference/)

---

## 팀 구성

### Backend

|  | 이름 | 역할 | 주요 담당 업무 | GitHub |
|:-:|:---:|:---:|:---|:---:|
| <img src="https://github.com/TaegyunB.png" width="60" height="60" /> | **백태균** | 백엔드 | API 설계, 인증/인가, 데이터베이스 설계 | [@TaegyunB](https://github.com/TaegyunB) |
| <img src="https://github.com/cheesecrust.png" width="60" height="60" /> | **정연수** | 백엔드 | 학습 세션 관리, 대시보드 API | [@cheesecrust](https://github.com/cheesecrust) |
| <img src="https://github.com/MegaZizon.png" width="60" height="60" /> | **지준오** | 백엔드 | BKT 알고리즘, 통계 분석 | [@MegaZizon](https://github.com/MegaZizon) |

---

## 라이선스

교육 목적으로 제작된 프로젝트입니다.

---

## 문의

프로젝트 관련 문의는 GitLab 이슈를 통해 등록해주세요.

---

**Last Updated**: 2025-11-19
**Spring Boot Version**: 3.5.6
**Java Version**: 21
