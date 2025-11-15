# 백엔드 분석 요약

## 프로젝트 개요
- **프로젝트명**: Reading Buddy
- **목적**: VR 기반 한글 학습 시스템
- **기술 스택**: Spring Boot, PostgreSQL, JWT, S3
- **주요 기능**: VR 스테이지 학습, 음소 연습, BKT 기반 학습 추적

---

## 도메인 구조

### 1. Auth (인증)
**위치**: `com.readingbuddy.backend.auth`

**주요 클래스:**
- `AuthService`: 로그인, 토큰 관리, Device Code 인증
- `DeviceSessionManager`: VR 기기 인증 코드 관리
- `JWTUtil`: JWT 토큰 생성 및 검증
- `RefreshToken`: Refresh Token 엔티티

**주요 기능:**
1. 일반 로그인 (이메일/비밀번호)
2. Device Code Flow (VR 기기 로그인)
   - VR에서 코드 생성 → 앱에서 코드 입력 → VR에서 폴링으로 토큰 획득
3. 토큰 재발급

**Device Code 메커니즘:**
- 10자리 랜덤 코드 생성 (혼동 문자 제외)
- 유효시간: 1분
- ConcurrentHashMap으로 세션 관리
- 주기적으로 만료된 세션 정리

---

### 2. User (사용자)
**위치**: `com.readingbuddy.backend.domain.user`

**엔티티:**
```java
User {
    Long id;
    String email;      // unique, not null
    String password;   // 암호화됨
    String nickname;   // unique, 최대 10자
}
```

**관련 히스토리:**
- `AttendHistories`: 출석 기록 (날짜, 플레이 시간)
- `TrainedStageHistories`: 스테이지 플레이 기록
- `TrainedProblemHistories`: 문제 풀이 기록

---

### 3. Dashboard (대시보드)
**위치**: `com.readingbuddy.backend.domain.dashboard`

**제공 데이터:**

#### 3.1 스테이지 통계
- 총 시도 횟수
- 총 정답/오답 개수
- 평균 시도 횟수
- 최근 세션 정답률

#### 3.2 출석 현황
- 일별 출석 여부
- 플레이 시간 (분:초 형식)
- 기간별 출석 일수

#### 3.3 음소 분석
- 틀린 음소 랭킹 (내림차순)
- 시도 횟수가 많은 음소 랭킹 (내림차순)

**데이터 특징:**
- 모든 통계는 사용자별로 집계
- JWT에서 userId 자동 추출
- 기간 조회는 yyMMdd 형식 사용

---

### 4. Train (학습)
**위치**: `com.readingbuddy.backend.domain.train`

**주요 개념:**
- **Phonemes**: 음소 (자음, 모음)
- **Letters**: 글자
- **Words**: 단어
- **Stage**: 학습 단계 (1.1.1, 1.1.2, 1.2.1, 1.2.2, 2, 3, 4)

**학습 데이터:**
- 스테이지별 문제 생성
- 음성 체크 (VR에서 발음 인식)
- 시도 횟수, 정답률 기록

**참고:**
- VR에서만 학습 진행
- 앱에서는 결과만 조회

---

### 5. BKT (Bayesian Knowledge Tracing)
**위치**: `com.readingbuddy.backend.domain.bkt`

**주요 개념:**
- **KnowledgeComponent (KC)**: 지식 구성요소
- **UserKcMastery**: 사용자의 KC별 숙달도
- **BKT 알고리즘**: 사용자의 학습 수준을 확률적으로 추적

**앱 연관성:**
- 직접적인 API 없음
- 대시보드 통계로 간접 확인 가능
- VR에서 문제 난이도 조정에 활용

---

## 인증 플로우

### 일반 로그인
```
사용자 (앱)
  ↓ POST /api/user/login {email, password}
백엔드
  ↓ 검증 후 토큰 생성
  ↓ RefreshToken DB 저장 (IP, UserAgent 포함)
사용자 (앱)
  ← {accessToken, refreshToken}
```

### Device Code 로그인
```
VR 기기
  ↓ GET /api/user/activation
백엔드
  ↓ 10자리 코드 생성, 세션 저장
VR 기기
  ← {authCode: "ABCD1234EF"}
  ↓ 화면에 코드 표시

사용자 (앱)
  ↓ POST /api/user/auth-device {deviceAuthCode}
  ↓ + Authorization: Bearer {accessToken}
백엔드
  ↓ 세션에 userId 저장, isAuthorized = true
사용자 (앱)
  ← 인증 성공

VR 기기
  ↓ POST /api/user/polling {deviceAuthCode} (2~3초마다)
백엔드
  ↓ 인증 확인 → 토큰 발급, 세션 삭제
VR 기기
  ← {accessToken, refreshToken}
```

---

## 데이터베이스 구조

### 주요 테이블
1. **users**: 사용자 기본 정보
2. **refresh_tokens**: Refresh Token 관리
3. **attend_histories**: 출석 기록
4. **trained_stage_histories**: 스테이지 플레이 기록
5. **trained_problem_histories**: 문제 풀이 기록
6. **phonemes**: 음소 마스터 데이터
7. **letters**: 글자 마스터 데이터
8. **words**: 단어 마스터 데이터
9. **knowledge_components**: 지식 구성요소
10. **user_kc_mastery**: 사용자별 KC 숙달도

---

## 보안

### JWT 설정
- Secret Key: 환경변수로 관리
- Access Token 유효기간: 24시간
- Refresh Token 유효기간: 7일

### Refresh Token 관리
- DB에 저장 (토큰 값, IP, UserAgent)
- 같은 IP/UserAgent에서 재로그인 시 토큰 rotation
- 만료 시간 변조 방지 검증

### Device Code 보안
- 1분 후 자동 만료
- 일회성 사용 (인증 완료 시 세션 삭제)
- 혼동 문자 제외로 입력 오류 최소화

---

## S3 연동
- 용도: 음성 파일, 이미지 등 저장
- 리전, AccessKey, SecretKey 환경변수로 관리
- 앱에서 직접 접근할 일은 없을 것으로 예상

---

## 앱 개발 시 고려사항

### 1. 로그인 상태 관리
- Access Token 만료 시 자동 재발급
- Refresh Token도 만료되면 재로그인 필요
- Device Code 로그인은 VR 최초 설정용

### 2. 대시보드 데이터 갱신
- 사용자가 VR에서 학습 완료 후 앱에서 확인
- Pull-to-refresh로 최신 데이터 반영
- 실시간 동기화는 없음 (폴링 또는 수동 갱신)

### 3. 스테이지 이해
- 스테이지는 문자열 형태 ("1.1.1", "2", 등)
- 스테이지별로 난이도와 학습 내용이 다름
- 대시보드에서 스테이지 선택 UI 필요

### 4. 음소 분석 활용
- 사용자의 취약점 파악
- 틀린 음소 TOP 5~10 표시
- 시도 횟수가 많은 음소 = 어려워하는 음소

### 5. 출석 현황 시각화
- 달력 형태로 표시 (출석 날짜 하이라이트)
- 플레이 시간 누적 그래프
- 연속 출석 일수 표시

---

## API 엔드포인트 요약

### 인증
- `POST /api/user/signup`: 회원가입
- `POST /api/user/login`: 로그인
- `POST /api/user/reissue-token`: 토큰 재발급
- `GET /api/user/activation`: Device Code 생성 (VR용)
- `POST /api/user/auth-device`: Device Code 인증 (앱용)
- `POST /api/user/polling`: Device Code 폴링 (VR용)

### 대시보드
- `GET /api/dashboard/stage/info`: 스테이지 통계
- `GET /api/dashboard/stage/try-avg`: 스테이지 평균 시도 횟수
- `GET /api/dashboard/stage/correct-rate`: 스테이지 정답률
- `GET /api/dashboard/attendance`: 출석 기록
- `GET /api/dashboard/mistake/phonemes/rank`: 틀린 음소 랭킹
- `GET /api/dashboard/try/phonemes/rank`: 시도 많은 음소 랭킹

---

## 향후 확장 가능성

### 앱에서 추가할 만한 기능
1. **푸시 알림**: 학습 리마인더, 연속 출석 격려
2. **목표 설정**: 일일/주간 학습 목표
3. **친구 기능**: 랭킹, 경쟁 (백엔드 API 추가 필요)
4. **학습 리포트**: 주간/월간 학습 분석 (현재 데이터로 구현 가능)
5. **음소별 상세 분석**: 특정 음소의 시간별 정답률 변화

### 백엔드에 요청할 만한 API
1. **전체 스테이지 요약**: 한 번의 호출로 모든 스테이지 통계
2. **주간 학습 요약**: 최근 7일간의 통계
3. **사용자 프로필 조회**: 닉네임, 이메일 확인
4. **닉네임 변경**: 프로필 수정 기능

---

## 환경 변수
백엔드 서버 실행 시 필요한 환경 변수:
- `DB_URL`: PostgreSQL 접속 URL
- `DB_USERNAME`: DB 사용자명
- `DB_PASSWORD`: DB 비밀번호
- `JWT_SECRET`: JWT 시크릿 키
- `REGION`: AWS 리전
- `ACCESS_KEY`: AWS Access Key
- `SECRET_KEY`: AWS Secret Key
- `BUCKET_NAME`: S3 버킷 이름

앱 개발 시 서버 URL만 설정하면 됩니다.
