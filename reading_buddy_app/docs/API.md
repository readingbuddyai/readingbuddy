# 백엔드 API 명세서 (앱 개발용)

## 기본 정보
- **Base URL**: https://readingbuddyai.co.kr
- **인증 방식**: JWT (Access Token + Refresh Token)
- **Content-Type**: application/json (음성 파일 업로드는 multipart/form-data)

## 응답 형식 (공통)
모든 API는 다음과 같은 공통 응답 형식을 사용합니다:

**성공 응답:**
```json
{
  "status": "success",
  "message": "성공 메시지",
  "data": { /* 실제 데이터 */ }
}
```

**실패 응답:**
```json
{
  "status": "error",
  "message": "에러 메시지"
}
```

---

## 1. 인증 (Authentication)

모든 인증 API는 `/api/user` 경로를 사용합니다.

### 1.1 일반 로그인
**POST** `/api/user/login`

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "로그인을 성공적으로 하였습니다.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**설명:**
- Access Token 유효기간: 24시간 (86400000ms)
- Refresh Token 유효기간: 7일 (604800000ms)

---

### 1.2 VR 기기 로그인 (Device Code Flow)

#### Step 1: VR 기기에서 인증 코드 생성
**GET** `/api/user/activation`

**Response (201 Created):**
```json
{
  "status": "success",
  "message": "인증 코드가 생성 되었습니다.",
  "data": {
    "authCode": "ABCD1234EF"
  }
}
```

**설명:**
- 10자리 랜덤 코드 생성 (혼동되는 문자 제외: I, O, 0, 1 등)
- 유효시간: 1분
- VR 기기 화면에 이 코드를 표시

---

#### Step 2: 앱에서 인증 코드 입력
**POST** `/api/user/auth-device`

**Request Headers:**
```
Authorization: Bearer {accessToken}
```

**Request Body:**
```json
{
  "deviceAuthCode": "ABCD1234EF"
}
```

**Response (202 Accepted):**
```json
{
  "status": "success",
  "message": "기기가 인증 되었습니다.",
  "data": null
}
```

**설명:**
- 사용자가 앱에 로그인한 상태여야 함
- VR 기기에 표시된 코드를 앱에서 입력
- 인증 성공 시 세션에 userId 저장

---

#### Step 3: VR 기기에서 토큰 획득 (폴링)
**POST** `/api/user/polling`

**Request Body:**
```json
{
  "deviceAuthCode": "ABCD1234EF"
}
```

**Response (인증 전 - 500):**
```json
{
  "status": "error",
  "message": "인증 처리되지 않았습니다."
}
```

**Response (인증 완료 - 202 Accepted):**
```json
{
  "status": "success",
  "message": "기기가 인증 되었습니다.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**설명:**
- VR 기기에서 주기적으로 호출 (예: 2~3초마다)
- 앱에서 인증하기 전까지 에러 반환
- 인증 완료되면 토큰 발급 후 세션 삭제

---

### 1.3 토큰 재발급
**POST** `/api/user/reissue-token`

**Request Body:**
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1
}
```

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "토큰을 성공적으로 발행 하였습니다.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

---

### 1.4 회원가입
**POST** `/api/user/signup`

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "password123",
  "nickname": "닉네임"
}
```

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "회원가입을 성공적으로 하였습니다.",
  "data": null
}
```

**설명:**
- 이메일 형식 검증 필요
- 비밀번호: 영문과 숫자 필수 포함, 특수문자는 !@#$%&*만 사용 가능

---

### 1.5 출석 체크
**POST** `/api/user/attend`

**Request Headers:**
```
Authorization: Bearer {accessToken}
```

**Response (202 Accepted):**
```json
{
  "status": "success",
  "message": "출석이 완료 되었습니다.",
  "data": null
}
```

**설명:**
- 자정이 지나면 이 API를 호출하여 출석 체크
- 인증 필요

---

## 2. 대시보드 (Dashboard)

모든 대시보드 API는 인증이 필요하며 `/api/dashboard` 경로를 사용합니다.

**Request Headers:**
```
Authorization: Bearer {accessToken}
```

---

### 2.1 KC 숙련도 변화 추이 조회 ⭐ 신규
**GET** `/api/dashboard/kc/mastery-trend`

**Query Parameters:**
- `kcId` (required): Knowledge Component ID
- `startdate` (optional): 조회 시작 날짜 (yyMMdd 형식, 예: "250101", 미입력시 한 달 전)
- `enddate` (optional): 조회 종료 날짜 (yyMMdd 형식, 예: "250131", 미입력시 오늘)

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "KC 숙련도 변화 추이가 조회되었습니다.",
  "data": {
    "kcId": 1,
    "kcCategory": "모음",
    "stage": "1.1.1",
    "masteryTrend": [
      {
        "p_l": 0.75,
        "p_t": 0.3,
        "p_g": 0.25,
        "p_s": 0.1,
        "updatedAt": "2025-01-15T10:30:00"
      },
      {
        "p_l": 0.82,
        "p_t": 0.35,
        "p_g": 0.25,
        "p_s": 0.1,
        "updatedAt": "2025-01-16T14:20:00"
      }
    ]
  }
}
```

**설명:**
- 특정 KC의 시간별 숙련도(p_l) 변화 추이를 조회
- p_l: 학습 확률 (Learning probability)
- p_t: 전이 확률 (Transit probability)
- p_g: 추측 확률 (Guess probability)
- p_s: 실수 확률 (Slip probability)
- 기간을 지정하지 않으면 최근 한 달 이력을 조회
- 시작 날짜와 종료 날짜는 함께 입력하거나 함께 생략해야 함

---

### 2.2 Stage별 현재 숙련도 조회 ⭐ 신규
**GET** `/api/dashboard/stage/mastery`

**Query Parameters:**
- `stage` (required): 스테이지 정보 (예: "1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4")
- `startdate` (optional): 조회 시작 날짜 (yyMMdd 형식)
- `enddate` (optional): 조회 종료 날짜 (yyMMdd 형식)

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "Stage 숙련도가 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "kcMasteries": [
      {
        "kcId": 1,
        "kcCategory": "모음",
        "pLearn": 0.75,
        "pTrain": 0.3,
        "pGuess": 0.25,
        "pSlip": 0.1,
        "updatedAt": "2025-01-15T10:30:00"
      },
      {
        "kcId": 2,
        "kcCategory": "모음",
        "pLearn": 0.68,
        "pTrain": 0.28,
        "pGuess": 0.25,
        "pSlip": 0.1,
        "updatedAt": "2025-01-15T10:30:00"
      }
    ],
    "averageMastery": 0.715
  }
}
```

**설명:**
- 특정 stage에 속한 모든 KC의 현재 숙련도를 조회
- averageMastery: 해당 stage의 평균 숙련도

---

### 2.3 스테이지 통계 정보
**GET** `/api/dashboard/stage/info`

**Query Parameters:**
- `stage`: 스테이지 정보 (예: "1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4")

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "스테이지 정보가 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "totalTryCount": 150,
    "totalCorrectCount": 120,
    "totalWrongCount": 30
  }
}
```

**설명:**
- 해당 스테이지의 누적 통계

---

### 2.4 스테이지 평균 시도 횟수
**GET** `/api/dashboard/stage/try-avg`

**Query Parameters:**
- `stage`: 스테이지 정보

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "스테이지 평균 시도 횟수가 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "averageTryCount": 3.5,
    "totalSessions": 10
  }
}
```

**설명:**
- 해당 스테이지를 플레이한 세션별 평균 시도 횟수

---

### 2.5 스테이지 최근 정답률
**GET** `/api/dashboard/stage/correct-rate`

**Query Parameters:**
- `stage`: 스테이지 정보

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "스테이지 정답률이 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "correctRate": 85.5,
    "correctCount": 17,
    "wrongCount": 3,
    "totalProblems": 20,
    "completedAt": "2025-11-06T15:30:00"
  }
}
```

**설명:**
- 가장 최근에 플레이한 세션의 정답률

---

### 2.6 출석 기록 조회

#### 2.6.1 기간별 조회
**GET** `/api/dashboard/attendance?startdate={yyMMdd}&enddate={yyMMdd}`

**Query Parameters:**
- `startdate`: 시작 날짜 (예: "250101")
- `enddate`: 종료 날짜 (예: "250131")

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "출석 기록 조회가 완료되었습니다.",
  "data": {
    "periodData": {
      "attendDates": [
        {
          "attendDate": "2025-01-01",
          "playtime": "15:30"
        },
        {
          "attendDate": "2025-01-02",
          "playtime": "20:15"
        }
      ],
      "totalAttendDays": 2
    },
    "dailyData": null
  }
}
```

---

#### 2.6.2 일별 조회
**GET** `/api/dashboard/attendance?date={yyMMdd}`

**Query Parameters:**
- `date`: 조회할 날짜 (예: "250101")

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "일별 출석 현황 조회가 완료되었습니다.",
  "data": {
    "periodData": null,
    "dailyData": {
      "attendDate": "2025-01-01",
      "playtime": "15:30",
      "attended": true
    }
  }
}
```

**설명:**
- playtime 형식: "분:초"
- attended: 해당 날짜에 출석했는지 여부

---

### 2.7 틀린 음소 랭킹
**GET** `/api/dashboard/mistake/phonemes/rank`

**Query Parameters:**
- `limit`: 조회할 개수 (예: 10)

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "틀린 음소 랭킹이 조회되었습니다. ",
  "data": [
    {
      "phonemeId": 1,
      "value": "ㄱ",
      "category": "자음",
      "wrongCnt": 15
    },
    {
      "phonemeId": 2,
      "value": "ㅏ",
      "category": "모음",
      "wrongCnt": 12
    }
  ]
}
```

**설명:**
- 사용자가 가장 많이 틀린 음소를 내림차순으로 반환

---

### 2.8 시도 횟수가 많은 음소 랭킹
**GET** `/api/dashboard/try/phonemes/rank`

**Query Parameters:**
- `limit`: 조회할 개수 (예: 10)

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "시도 횟수가 많은 음소 랭킹이 조회되었습니다.",
  "data": [
    {
      "phonemeId": 1,
      "value": "ㄱ",
      "category": "자음",
      "tryCnt": 50
    },
    {
      "phonemeId": 2,
      "value": "ㅏ",
      "category": "모음",
      "tryCnt": 45
    }
  ]
}
```

**설명:**
- 사용자가 가장 많이 시도한 음소를 내림차순으로 반환

---

## 3. 훈련 (Train) ⭐ 신규 섹션

모든 훈련 API는 인증이 필요하며 `/api/train` 경로를 사용합니다.

**Request Headers:**
```
Authorization: Bearer {accessToken}
```

---

### 3.1 훈련 스테이지 시작
**POST** `/api/train/stage/start`

**Query Parameters:**
- `stage`: 스테이지 정보 (예: "1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4.1", "4.2")
- `totalProblems`: 총 문제 개수

**Response (201 Created):**
```json
{
  "status": "success",
  "message": "스테이지가 시작되었습니다.",
  "data": {
    "stageSessionId": "uuid-string",
    "stage": "1.1.1",
    "totalProblems": 5,
    "startAt": "2025-11-07T10:30:00"
  }
}
```

**설명:**
- 새로운 훈련 세션을 생성하고 stageSessionId 반환
- 반환된 stageSessionId는 이후 API 호출에 사용

---

### 3.2 훈련 문제 세트 생성
**GET** `/api/train/set`

**Query Parameters:**
- `stage`: 문제 단계 (1.1.1: 모음 기초, 1.1.2: 모음 심화, 1.2.1: 자음 기초, 1.2.2: 자음 심화, 2: 음절 개수, 3, 4.1, 4.2: 음소 개수)
- `count`: 문제 개수 (기본값: 5)
- `stageSessionId`: 스테이지 세션 ID (Stage 3, 4일 때 필수)

**Response (201 Created):**
```json
{
  "status": "success",
  "message": "모음 기초 단계 문제가 생성되었습니다.",
  "data": {
    "problems": [
      {
        "problemWord": "ㅏ"
      },
      {
        "problemWord": "ㅓ"
      },
      {
        "problemWord": "ㅗ"
      }
    ]
  }
}
```

**설명:**
- stage에 따라 다른 타입의 문제 생성
- Stage 1.1.1: 모음 기초 단계
- Stage 1.1.2: 모음 심화 단계
- Stage 1.2.1: 자음 기초 단계
- Stage 1.2.2: 자음 심화 단계
- Stage 2: 음절 개수 세기
- Stage 3, 4.1, 4.2: 음소 개수 세기

---

### 3.3 음성 검증
**POST** `/api/train/check/voice`

**Content-Type:** multipart/form-data

**Request Form Data:**
- `audio`: MultipartFile (음성 파일)
- `stageSessionId`: String (스테이지 세션 ID)
- `stage`: String (스테이지)
- `problemNumber`: Integer (문제 번호)
- `answer`: String (정답)

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "음성 인식이 완료되었습니다.",
  "data": {
    "reply": ["ㄱ", "ㅏ"],
    "isReplyCorrect": true
  }
}
```

**설명:**
- 음성 파일을 S3에 업로드하고 AI 서버로 전송
- AI 서버에서 음성 인식 결과를 반환
- reply: 인식된 음소 배열
- isReplyCorrect: 정답 여부

---

### 3.4 문제 시도 기록
**POST** `/api/train/attempt`

**Request Body:**
```json
{
  "stageSessionId": "uuid-string",
  "problemNumber": 1,
  "stage": "1.1.1",
  "problem": "ㅏ",
  "answer": "ㅏ",
  "audioUrl": "https://s3.amazonaws.com/...",
  "isCorrect": true,
  "isReplyCorrect": true,
  "attemptNumber": 1
}
```

**Response (201 Created):**
```json
{
  "status": "success",
  "message": "문제 풀이가 기록되었습니다.",
  "data": {
    "attemptId": 123,
    "stageSessionId": "uuid-string",
    "problemNumber": 1,
    "stage": "1.1.1",
    "problem": "ㅏ",
    "answer": "ㅏ",
    "audioUrl": "https://s3.amazonaws.com/...",
    "isCorrect": true,
    "isReplyCorrect": true,
    "attemptNumber": 1
  }
}
```

**설명:**
- 개별 문제의 시도 결과를 DB에 저장
- attemptNumber: 해당 문제에 대한 시도 횟수

---

### 3.5 스테이지 완료
**POST** `/api/train/stage/complete`

**Query Parameters:**
- `stageSessionId`: 스테이지 세션 ID

**Response (200 OK):**
```json
{
  "status": "success",
  "message": "스테이지가 완료되었습니다.",
  "data": {
    "stageSessionId": "uuid-string",
    "voiceResult": [1, 3, 5]
  }
}
```

**설명:**
- 세션의 모든 시도 기록을 집계하고 통계를 업데이트
- voiceResult: 음성 인식이 정확했던 문제 번호 목록

---

## 4. 사용자 정보

### 4.1 User 엔티티 구조
```json
{
  "id": 1,
  "email": "user@example.com",
  "nickname": "닉네임"
}
```

**설명:**
- JWT에서 userId, email, nickname을 추출 가능
- password는 응답에 포함되지 않음

---

## 5. 에러 응답 형식

모든 에러는 다음 형식으로 반환됩니다:

```json
{
  "status": "error",
  "message": "에러 메시지"
}
```

**주요 에러 케이스:**
- 로그인 정보 불일치: "로그인 정보에 일치하는 회원이 없습니다."
- 토큰 만료: "만료된 리프레시 토큰입니다."
- 유효하지 않은 토큰: "유효하지 않은 리프레시 토큰입니다."
- Device code 만료: "시간이 만료되었습니다."
- Device code 미인증: "인증 처리되지 않았습니다."
- 잘못된 device code: "해당 코드는 유효하지 않습니다."
- 날짜 형식 오류: "날짜 형식이 올바르지 않습니다. yyMMdd 형식으로 입력해주세요. (예: 250101)"
- 날짜 범위 오류: "시작 날짜는 종료 날짜보다 이전이어야 합니다."
- 음성 파일 누락: "음성 파일이 비어있습니다."
- 유효하지 않은 스테이지: "유효하지 않은 단계입니다."

---

## 6. 참고 사항

### 6.1 스테이지 구조
프로젝트는 VR 기반 한글 학습 시스템으로, 다음과 같은 스테이지가 있습니다:
- **1.1.1**: 모음 기초
- **1.1.2**: 모음 심화
- **1.2.1**: 자음 기초
- **1.2.2**: 자음 심화
- **2**: 음절 개수 세기
- **3**: 음소 개수 세기
- **4.1, 4.2**: 음소 개수 세기 (고급)

### 6.2 음소 (Phoneme)
- 한글의 자음과 모음을 의미
- category: "자음" 또는 "모음"
- value: 실제 음소 값 (예: "ㄱ", "ㅏ")

### 6.3 BKT (Bayesian Knowledge Tracing)
- 백엔드에서 사용자의 학습 수준을 추적하는 알고리즘
- UserKcMastery: 사용자의 지식 구성요소(Knowledge Component) 숙달도
- 앱에서는 대시보드 통계로 간접적으로 확인 가능

### 6.4 KC (Knowledge Component)
- 한글 학습에서의 지식 구성 요소
- 각 음소는 하나의 KC에 해당
- 숙련도 파라미터:
  - **p_l (pLearn)**: 학습 확률 - 사용자가 해당 KC를 학습한 정도
  - **p_t (pTrain)**: 전이 확률 - 모르는 상태에서 아는 상태로 전환될 확률
  - **p_g (pGuess)**: 추측 확률 - 모르지만 맞출 확률
  - **p_s (pSlip)**: 실수 확률 - 알지만 틀릴 확률

### 6.5 JWT 토큰 구조
Access Token에 포함된 정보:
- userId (id)
- email
- nickname

### 6.6 날짜 형식
- 모든 날짜 파라미터는 **yyMMdd** 형식을 사용 (예: "250101")
- 응답의 날짜/시간은 ISO 8601 형식 (예: "2025-11-07T10:30:00")

---

## 7. 앱 개발 권장 사항

### 7.1 토큰 관리
- Access Token은 메모리에 저장 (앱 재시작 시 삭제)
- Refresh Token은 안전한 저장소에 저장 (Keychain/Keystore)
- 401 에러 발생 시 자동으로 토큰 재발급 시도

### 7.2 Device Code 로그인 UX
1. VR 기기에서 "앱으로 로그인" 선택
2. 10자리 코드 표시 (큰 글씨로)
3. 앱에서 코드 입력 화면 제공 (입력하기 쉽게)
4. 인증 성공 시 VR 기기에 즉시 반영

### 7.3 대시보드 화면 구성 제안
- **홈**: 출석 현황 (달력 형태), 최근 학습 시간
- **학습 분석**:
  - 스테이지별 정답률 그래프
  - 평균 시도 횟수 차트
  - KC 숙련도 추이 그래프 (새로 추가)
  - 취약 음소 표시 (틀린 음소 랭킹)
- **프로필**: 닉네임, 이메일, 로그아웃

### 7.4 훈련 플로우 구현
1. **스테이지 시작**: `/api/train/stage/start` 호출하여 stageSessionId 획득
2. **문제 생성**: `/api/train/set` 호출하여 문제 목록 받기
3. **각 문제마다**:
   - 사용자 음성 녹음
   - `/api/train/check/voice` 호출하여 음성 검증
   - `/api/train/attempt` 호출하여 시도 기록
4. **스테이지 완료**: `/api/train/stage/complete` 호출하여 세션 종료

### 7.5 API 호출 최적화
- 대시보드 진입 시 필요한 API 병렬 호출
- 캐싱 활용 (특히 통계 데이터)
- Pull-to-refresh로 최신 데이터 갱신

### 7.6 음성 파일 처리
- 음성 파일은 multipart/form-data로 전송
- 파일 형식: WAV, MP3 등 (백엔드에서 처리 가능한 형식)
- 파일 크기 제한 확인 필요

---

## 8. API 변경 이력

### 2025-11-07 업데이트
1. **응답 형식 통일**: 모든 API가 ApiResponse 래퍼 사용
2. **신규 API 추가**:
   - POST `/api/user/attend` - 출석 체크
   - GET `/api/dashboard/kc/mastery-trend` - KC 숙련도 변화 추이
   - GET `/api/dashboard/stage/mastery` - Stage별 현재 숙련도
   - 전체 Train API 섹션 추가 (훈련 관련)
3. **응답 상태 코드 변경**:
   - GET `/api/user/activation`: 201 Created
   - POST `/api/user/auth-device`: 202 Accepted
   - POST `/api/user/polling`: 202 Accepted (성공 시)
4. **로그인 응답 형식 변경**: TokenResponse가 data 필드에 포함
5. **회원가입 응답 변경**: 200 OK + ApiResponse 형식
