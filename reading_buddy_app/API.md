# Reading Buddy API 문서

## 기본 정보
- **Base URL**: `https://readingbuddyai.co.kr`
- **인증**: JWT Bearer Token (Authorization 헤더 필요)
- **날짜 형식**: `yyMMdd` (예: 250112는 2025년 1월 12일)
- **응답 형식**: 모든 응답은 ApiResponse 래퍼로 감싸져 있음

### 공통 응답 구조
```json
{
  "success": true,
  "message": "응답 메시지",
  "data": { /* 실제 데이터 */ }
}
```

---

## Dashboard API

### 1. KC 숙련도 변화 추이 조회
특정 KC(Knowledge Component)의 시간별 숙련도 변화 추이를 조회합니다.

**Endpoint**: `GET /api/dashboard/kc/mastery-trend`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `kcId` | Long | ✓ | Knowledge Component ID | 1 |
| `startdate` | String | ✗ | 시작 날짜 (yyMMdd) | 250101 |
| `enddate` | String | ✗ | 종료 날짜 (yyMMdd) | 250131 |

**참고**:
- `startdate`와 `enddate`는 둘 다 입력하거나 둘 다 생략해야 함
- 미입력시 최근 한 달(오늘부터 한 달 전) 데이터를 조회

**Response**: `KcMasteryTrendResponse`
```json
{
  "success": true,
  "message": "KC 숙련도 변화 추이가 조회되었습니다.",
  "data": {
    "kcId": 1,
    "kcCategory": "MONOPHTHONG_1",
    "stage": "1.1.1",
    "masteryTrend": [
      {
        "p_l": 0.85,
        "p_t": 0.90,
        "p_g": 0.15,
        "p_s": 0.10,
        "updatedAt": "2025-01-12T10:30:00"
      }
    ]
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `kcId` | Long | No | Knowledge Component ID |
| `kcCategory` | String | No | KC 카테고리 (예: MONOPHTHONG_1) |
| `stage` | String | No | 스테이지 정보 (예: 1.1.1) |
| `masteryTrend` | List<MasteryPoint> | No | 숙련도 변화 이력 리스트 |

**MasteryPoint 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `p_l` | Float | No | P(Learn) - 학습 확률 |
| `p_t` | Float | No | P(Train) - 숙련 확률 |
| `p_g` | Float | No | P(Guess) - 추측 확률 |
| `p_s` | Float | No | P(Slip) - 실수 확률 |
| `updatedAt` | DateTime | No | 업데이트 시간 (ISO 8601) |

**Dart Model 매핑**:
```dart
// JSON 필드명과 Dart 필드명 매핑
@JsonKey(name: 'p_l') final double pLearn;
@JsonKey(name: 'p_t') final double pTrain;
@JsonKey(name: 'p_g') final double pGuess;
@JsonKey(name: 'p_s') final double pSlip;
```

**사용 예시**:
```dart
// 특정 기간 조회
final response = await apiClient.get(
  '/api/dashboard/kc/mastery-trend',
  queryParameters: {
    'kcId': 1,
    'startdate': '250101',
    'enddate': '250131',
  },
);

// 최근 한 달 조회 (날짜 생략)
final response = await apiClient.get(
  '/api/dashboard/kc/mastery-trend',
  queryParameters: {'kcId': 1},
);
```

---

### 2. Stage별 KC 숙련도 변화 추이 조회
특정 stage에 속한 모든 KC의 시간별 숙련도 변화 추이를 조회합니다.

**Endpoint**: `GET /api/dashboard/stage/kc-mastery-trend`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `stage` | String | ✓ | 스테이지 정보 | 1.1.1 |
| `startdate` | String | ✗ | 시작 날짜 (yyMMdd) | 250101 |
| `enddate` | String | ✗ | 종료 날짜 (yyMMdd) | 250131 |

**참고**:
- 날짜 규칙은 위와 동일
- stage 값 예시: "1.1.1", "1.1.2", "1.2.1", "1.2.2", "2", "3", "4"

**Response**: `StageKcMasteryTrendResponse`
```json
{
  "success": true,
  "message": "Stage별 KC 숙련도 변화 추이가 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "kcTrends": [
      {
        "kcId": 1,
        "kcCategory": "MONOPHTHONG_1",
        "kcDescription": "단모음 ㅏ",
        "masteryTrend": [
          {
            "pLearn": 0.85,
            "pTrain": 0.90,
            "pGuess": 0.15,
            "pSlip": 0.10,
            "updatedAt": "2025-01-12T10:30:00"
          }
        ]
      }
    ]
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `stage` | String | No | 스테이지 정보 |
| `kcTrends` | List<KcTrend> | No | KC별 추이 리스트 |

**KcTrend 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `kcId` | Long | No | KC ID |
| `kcCategory` | String | No | KC 카테고리 |
| `kcDescription` | String | No | KC 설명 |
| `masteryTrend` | List<MasteryPoint> | No | 숙련도 변화 이력 |

**MasteryPoint 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `pLearn` | Float | No | P(Learn) - 학습 확률 |
| `pTrain` | Float | No | P(Train) - 숙련 확률 |
| `pGuess` | Float | No | P(Guess) - 추측 확률 |
| `pSlip` | Float | No | P(Slip) - 실수 확률 |
| `updatedAt` | DateTime | No | 업데이트 시간 |

**Dart Model 특징**:
- 이 응답에서는 camelCase로 통일되어 있어 별도 @JsonKey 매핑 불필요

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/stage/kc-mastery-trend',
  queryParameters: {
    'stage': '1.1.1',
    'startdate': '250101',
    'enddate': '250131',
  },
);
```

---

### 3. Stage별 현재 숙련도 조회
특정 stage에 속한 모든 KC의 현재 숙련도를 조회합니다.

**Endpoint**: `GET /api/dashboard/stage/mastery`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `stage` | String | ✓ | 스테이지 정보 | 1.1.1 |
| `startdate` | String | ✗ | 시작 날짜 (yyMMdd) | 250101 |
| `enddate` | String | ✗ | 종료 날짜 (yyMMdd) | 250131 |

**Response**: `StageMasteryResponse`
```json
{
  "success": true,
  "message": "Stage 숙련도가 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "kcMasteries": [
      {
        "kcId": 1,
        "kcCategory": "MONOPHTHONG_1",
        "pLearn": 0.85,
        "pTrain": 0.90,
        "pGuess": 0.15,
        "pSlip": 0.10,
        "updatedAt": "2025-01-12T10:30:00"
      }
    ],
    "averageMastery": 0.82
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `stage` | String | No | 스테이지 정보 |
| `kcMasteries` | List<KcMastery> | No | KC별 현재 숙련도 리스트 |
| `averageMastery` | Double | No | 전체 평균 숙련도 |

**KcMastery 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `kcId` | Long | No | KC ID |
| `kcCategory` | String | No | KC 카테고리 |
| `pLearn` | Float | No | P(Learn) - 학습 확률 |
| `pTrain` | Float | No | P(Train) - 숙련 확률 |
| `pGuess` | Float | No | P(Guess) - 추측 확률 |
| `pSlip` | Float | No | P(Slip) - 실수 확률 |
| `updatedAt` | DateTime | No | 업데이트 시간 |

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/stage/mastery',
  queryParameters: {'stage': '1.1.1'},
);

// 평균 숙련도 접근
final avgMastery = response.data.averageMastery;
```

---

### 4. 스테이지 통계 정보 조회
특정 스테이지의 전체 통계 정보(시도 횟수, 정답률 등)를 조회합니다.

**Endpoint**: `GET /api/dashboard/stage/info`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `stage` | String | ✓ | 스테이지 정보 | 1.1.1 |

**Response**: `StageInfoResponse`
```json
{
  "success": true,
  "message": "스테이지 정보가 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "totalProblemCount": 100,
    "correctProblemCount": 85,
    "correctRate": 0.85
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `stage` | String | No | 스테이지 정보 |
| `totalProblemCount` | Integer | No | 전체 문제 시도 횟수 |
| `correctProblemCount` | Integer | No | 정답 개수 |
| `correctRate` | Double | No | 정답률 (0.0 ~ 1.0) |

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/stage/info',
  queryParameters: {'stage': '1.1.1'},
);

// 백분율로 변환
final percentageRate = response.data.correctRate * 100;
print('정답률: ${percentageRate.toStringAsFixed(1)}%');
```

---

### 5. 스테이지 평균 시도 횟수 조회
특정 스테이지의 문제당 평균 시도 횟수를 조회합니다.

**Endpoint**: `GET /api/dashboard/stage/try-avg`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `stage` | String | ✓ | 스테이지 정보 | 1.1.1 |

**Response**: `StageTryAvgResponse`
```json
{
  "success": true,
  "message": "스테이지 평균 시도 횟수가 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "averageTryCount": 2.5,
    "totalSessions": 10
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `stage` | String | No | 스테이지 정보 |
| `averageTryCount` | Double | No | 문제당 평균 시도 횟수 |
| `totalSessions` | Integer | No | 전체 세션 수 |

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/stage/try-avg',
  queryParameters: {'stage': '1.1.1'},
);
```

---

### 6. 스테이지 최근 세션 정답률 조회
가장 최근에 진행한 스테이지 세션의 정답률을 조회합니다.

**Endpoint**: `GET /api/dashboard/stage/correct-rate`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `stage` | String | ✓ | 스테이지 정보 | 1.1.1 |

**Response**: `StageCorrectRateResponse`
```json
{
  "success": true,
  "message": "스테이지 정답률이 조회되었습니다.",
  "data": {
    "stage": "1.1.1",
    "correctRate": 0.85,
    "correctCount": 17,
    "wrongCount": 3,
    "totalProblems": 20,
    "completedAt": "2025-01-12T15:30:00",
    "sessionKey": "user123_1.1.1_20250112_153000"
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `stage` | String | No | 스테이지 정보 |
| `correctRate` | Double | No | 정답률 (0.0 ~ 1.0) |
| `correctCount` | Integer | No | 정답 개수 |
| `wrongCount` | Integer | No | 오답 개수 |
| `totalProblems` | Integer | No | 전체 문제 수 |
| `completedAt` | DateTime | No | 완료 시간 |
| `sessionKey` | String | No | 세션 고유 키 |

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/stage/correct-rate',
  queryParameters: {'stage': '1.1.1'},
);
```

---

### 7. 출석 기록 조회
출석 기록을 기간별 또는 일별로 조회합니다.

**Endpoint**: `GET /api/dashboard/attendance`

**Query Parameters (기간별 조회)**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `startdate` | String | ✓* | 시작 날짜 (yyMMdd) | 250101 |
| `enddate` | String | ✓* | 종료 날짜 (yyMMdd) | 250131 |

**Query Parameters (일별 조회)**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `date` | String | ✓* | 조회할 날짜 (yyMMdd) | 250112 |

**참고**:
- 기간별 조회: `startdate`와 `enddate`를 함께 사용
- 일별 조회: `date` 단독 사용
- 둘 중 하나의 방식을 선택해야 함

**Response (기간별 조회)**: `AttendanceResponse`
```json
{
  "success": true,
  "message": "출석 기록 조회가 완료되었습니다.",
  "data": {
    "periodData": {
      "attendDates": [
        {
          "attendDate": "2025-01-12",
          "playtime": "15:30"
        },
        {
          "attendDate": "2025-01-13",
          "playtime": "22:45"
        }
      ],
      "totalAttendDays": 2
    },
    "dailyData": null
  }
}
```

**Response (일별 조회)**: `AttendanceResponse`
```json
{
  "success": true,
  "message": "일별 출석 현황 조회가 완료되었습니다.",
  "data": {
    "periodData": null,
    "dailyData": {
      "attendDate": "2025-01-12",
      "playtime": "15:30",
      "attended": true
    }
  }
}
```

**필드 상세 설명 (PeriodData)**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `periodData` | PeriodAttendance | Yes | 기간별 조회시에만 존재 |
| `dailyData` | DailyAttendance | Yes | 일별 조회시에만 존재 |

**PeriodAttendance 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `attendDates` | List<AttendDateInfo> | No | 출석 날짜 리스트 |
| `totalAttendDays` | Integer | No | 총 출석 일수 |

**AttendDateInfo 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `attendDate` | LocalDate | No | 출석 날짜 (yyyy-MM-dd) |
| `playtime` | String | No | 플레이 시간 (분:초 형식) |

**DailyAttendance 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `attendDate` | LocalDate | No | 출석 날짜 (yyyy-MM-dd) |
| `playtime` | String | No | 플레이 시간 (분:초 형식) |
| `attended` | Boolean | No | 출석 여부 |

**중요 사항**:
- `playtime`은 "분:초" 형식의 문자열 (예: "15:30"은 15분 30초)
- `attendDate`는 LocalDate 타입으로 "yyyy-MM-dd" 형식
- `periodData`와 `dailyData` 중 하나만 null이 아닌 값을 가짐

**사용 예시**:
```dart
// 기간별 조회
final periodResponse = await apiClient.get(
  '/api/dashboard/attendance',
  queryParameters: {
    'startdate': '250101',
    'enddate': '250131',
  },
);

if (periodResponse.data.periodData != null) {
  final totalDays = periodResponse.data.periodData.totalAttendDays;
  final dates = periodResponse.data.periodData.attendDates;
}

// 일별 조회
final dailyResponse = await apiClient.get(
  '/api/dashboard/attendance',
  queryParameters: {'date': '250112'},
);

if (dailyResponse.data.dailyData != null) {
  final isAttended = dailyResponse.data.dailyData.attended;
  final playtime = dailyResponse.data.dailyData.playtime;
}
```

---

### 8. 모든 KC 평균 숙련도 조회
사용자의 모든 KC에 대한 현재 숙련도와 전체 평균을 조회합니다.

**Endpoint**: `GET /api/dashboard/kc/all-mastery`

**Query Parameters**: 없음 (JWT에서 userId 추출)

**Response**: `AllKcAverageMasteryResponse`
```json
{
  "success": true,
  "message": "모든 KC의 평균 숙련도가 조회되었습니다.",
  "data": {
    "totalKcCount": 40,
    "overallAverageMastery": 0.78,
    "kcMasteries": [
      {
        "kcId": 1,
        "kcCategory": "MONOPHTHONG_1",
        "kcDescription": "단모음 ㅏ",
        "stage": "1.1.1",
        "pLearn": 0.85,
        "pTrain": 0.90,
        "pGuess": 0.15,
        "pSlip": 0.10,
        "updatedAt": "2025-01-12T10:30:00"
      }
    ]
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `totalKcCount` | Integer | No | 전체 KC 개수 |
| `overallAverageMastery` | Double | No | 전체 평균 숙련도 |
| `kcMasteries` | List<KcMasteryInfo> | No | KC별 상세 정보 리스트 |

**KcMasteryInfo 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `kcId` | Long | No | KC ID |
| `kcCategory` | String | No | KC 카테고리 |
| `kcDescription` | String | No | KC 설명 |
| `stage` | String | No | 속한 스테이지 |
| `pLearn` | Float | No | P(Learn) - 학습 확률 |
| `pTrain` | Float | No | P(Train) - 숙련 확률 |
| `pGuess` | Float | No | P(Guess) - 추측 확률 |
| `pSlip` | Float | No | P(Slip) - 실수 확률 |
| `updatedAt` | DateTime | No | 업데이트 시간 |

**사용 예시**:
```dart
final response = await apiClient.get('/api/dashboard/kc/all-mastery');

final overallAvg = response.data.overallAverageMastery;
final kcList = response.data.kcMasteries;

// 스테이지별로 그룹핑
final groupedByStage = <String, List<KcMasteryInfo>>{};
for (var kc in kcList) {
  groupedByStage.putIfAbsent(kc.stage, () => []).add(kc);
}
```

---

### 9. 틀린 음소 랭킹 조회
가장 많이 틀린 음소를 랭킹순으로 조회합니다.

**Endpoint**: `GET /api/dashboard/mistake/phonemes/rank`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `limit` | Integer | ✓ | 조회할 랭킹 개수 | 10 |

**Response**: `List<PhonemesWrongRankResponse>`
```json
{
  "success": true,
  "message": "틀린 음소 랭킹이 조회되었습니다.",
  "data": [
    {
      "phonemeId": 1,
      "value": "ㅏ",
      "category": "MONOPHTHONG",
      "wrongCnt": 25
    },
    {
      "phonemeId": 2,
      "value": "ㅓ",
      "category": "MONOPHTHONG",
      "wrongCnt": 18
    }
  ]
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `phonemeId` | Long | No | 음소 ID |
| `value` | String | No | 음소 값 (예: "ㅏ", "ㄱ") |
| `category` | String | No | 음소 카테고리 (MONOPHTHONG, CONSONANT 등) |
| `wrongCnt` | Long | No | 틀린 횟수 |

**참고**:
- 결과는 `wrongCnt` 내림차순으로 정렬됨
- `limit`만큼만 반환

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/mistake/phonemes/rank',
  queryParameters: {'limit': 10},
);

// 상위 3개만 추출
final top3 = response.data.take(3).toList();
```

---

### 10. 시도 횟수가 많은 음소 랭킹 조회
가장 많이 시도한 음소를 랭킹순으로 조회합니다.

**Endpoint**: `GET /api/dashboard/try/phonemes/rank`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `limit` | Integer | ✓ | 조회할 랭킹 개수 | 10 |

**Response**: `List<PhonemesTryRankResponse>`
```json
{
  "success": true,
  "message": "시도 횟수가 많은 음소 랭킹이 조회되었습니다.",
  "data": [
    {
      "phonemeId": 1,
      "value": "ㅏ",
      "category": "MONOPHTHONG",
      "tryCnt": 50
    },
    {
      "phonemeId": 2,
      "value": "ㅓ",
      "category": "MONOPHTHONG",
      "tryCnt": 45
    }
  ]
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `phonemeId` | Long | No | 음소 ID |
| `value` | String | No | 음소 값 (예: "ㅏ", "ㄱ") |
| `category` | String | No | 음소 카테고리 |
| `tryCnt` | Long | No | 시도 횟수 |

**참고**:
- 결과는 `tryCnt` 내림차순으로 정렬됨

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/try/phonemes/rank',
  queryParameters: {'limit': 10},
);
```

---

### 11. 특정 날짜의 훈련 기록 조회
특정 날짜에 진행한 모든 훈련 세션과 문제 상세 정보를 조회합니다.

**Endpoint**: `GET /api/dashboard/practice/list`

**Query Parameters**:
| 파라미터 | 타입 | 필수 | 설명 | 예시 |
|---------|------|------|------|------|
| `date` | String | ✓ | 조회할 날짜 (yyMMdd) | 250112 |

**Response**: `StageProblemListResponse`
```json
{
  "success": true,
  "message": "일별 훈련 기록이 조회되었습니다.",
  "data": {
    "date": "2025-01-12",
    "session": [
      {
        "trainedStageHistoryId": 1001,
        "stage": "1.1.1",
        "startedAt": "2025-01-12T10:00:00",
        "totalCount": 20,
        "correctCount": 17,
        "wrongCount": 3,
        "problems": [
          {
            "problemId": 5001,
            "problemNumber": 1,
            "problem": "ㅏ",
            "answer": "ㅏ",
            "isCorrect": true,
            "isReplyCorrect": true,
            "attemptNumber": 1,
            "audioUrl": "https://s3.bucket.com/audio/user123_problem5001.mp3",
            "solvedAt": "2025-01-12T10:01:30"
          },
          {
            "problemId": 5002,
            "problemNumber": 2,
            "problem": "ㅓ",
            "answer": "ㅏ",
            "isCorrect": false,
            "isReplyCorrect": false,
            "attemptNumber": 2,
            "audioUrl": "https://s3.bucket.com/audio/user123_problem5002.mp3",
            "solvedAt": "2025-01-12T10:03:15"
          }
        ]
      }
    ]
  }
}
```

**필드 상세 설명**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `date` | LocalDate | No | 조회한 날짜 (yyyy-MM-dd) |
| `session` | List<SessionInfo> | No | 세션 리스트 (여러 번 훈련 가능) |

**SessionInfo 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `trainedStageHistoryId` | Long | No | 훈련 세션 고유 ID |
| `stage` | String | No | 스테이지 정보 |
| `startedAt` | DateTime | No | 시작 시간 |
| `totalCount` | Integer | No | 전체 문제 수 |
| `correctCount` | Integer | No | 정답 개수 |
| `wrongCount` | Integer | No | 오답 개수 |
| `problems` | List<ProblemInfo> | No | 문제 상세 리스트 |

**ProblemInfo 필드**:
| 필드 | 타입 | Nullable | 설명 |
|------|------|----------|------|
| `problemId` | Long | No | 문제 기록 ID |
| `problemNumber` | Integer | No | 문제 번호 (1부터 시작) |
| `problem` | String | No | 출제된 문제 (음소/음절/단어) |
| `answer` | String | No | 사용자의 답변 |
| `isCorrect` | Boolean | No | 정답 여부 |
| `isReplyCorrect` | Boolean | No | 발음 정답 여부 |
| `attemptNumber` | Integer | No | 시도 횟수 |
| `audioUrl` | String | No | 사용자 응답 오디오 URL |
| `solvedAt` | DateTime | No | 풀이 시간 |

**중요 사항**:
- `session`은 리스트로, 한 날짜에 여러 세션 가능
- `problems`는 `problemNumber` 순서대로 정렬됨
- `isCorrect`: 문제 자체의 정답 여부
- `isReplyCorrect`: 발음 평가 결과 (STT 기반)
- `attemptNumber`: 해당 문제를 몇 번째 시도했는지

**사용 예시**:
```dart
final response = await apiClient.get(
  '/api/dashboard/practice/list',
  queryParameters: {'date': '250112'},
);

// 모든 세션 순회
for (var session in response.data.session) {
  print('스테이지: ${session.stage}');
  print('정답률: ${session.correctCount / session.totalCount}');

  // 틀린 문제만 필터링
  final wrongProblems = session.problems
      .where((p) => !p.isCorrect)
      .toList();

  // 오디오 재생
  for (var problem in session.problems) {
    if (problem.audioUrl != null) {
      // 오디오 플레이어로 재생
      await audioPlayer.play(problem.audioUrl);
    }
  }
}
```

---

## 에러 응답

모든 API는 에러 발생시 다음과 같은 형식으로 응답합니다:

```json
{
  "success": false,
  "message": "에러 메시지",
  "data": null
}
```

### 일반적인 에러 코드

| HTTP 코드 | 상황 | 메시지 예시 |
|-----------|------|-------------|
| 400 | 잘못된 요청 | "날짜 형식이 올바르지 않습니다. yyMMdd 형식으로 입력해주세요." |
| 400 | 날짜 유효성 오류 | "시작 날짜는 종료 날짜보다 이전이어야 합니다." |
| 400 | 파라미터 누락 | "조회 타입을 선택해주세요. 기간 조회: startdate & enddate, 일별 조회: date" |
| 401 | 인증 실패 | "인증이 필요합니다." |
| 404 | 리소스 없음 | "해당 KC를 찾을 수 없습니다." |
| 500 | 서버 오류 | "KC 숙련도 변화 추이 조회 중 오류가 발생했습니다." |

---

## Dart 통합 가이드

### 1. JSON Serialization 설정

`pubspec.yaml`:
```yaml
dependencies:
  json_annotation: ^4.8.1

dev_dependencies:
  build_runner: ^2.4.6
  json_serializable: ^6.7.1
```

### 2. 모델 클래스 예시

```dart
import 'package:json_annotation/json_annotation.dart';

part 'kc_mastery_trend_response.g.dart';

@JsonSerializable()
class KcMasteryTrendResponse {
  final int kcId;
  final String kcCategory;
  final String stage;
  final List<MasteryPoint> masteryTrend;

  KcMasteryTrendResponse({
    required this.kcId,
    required this.kcCategory,
    required this.stage,
    required this.masteryTrend,
  });

  factory KcMasteryTrendResponse.fromJson(Map<String, dynamic> json) =>
      _$KcMasteryTrendResponseFromJson(json);

  Map<String, dynamic> toJson() => _$KcMasteryTrendResponseToJson(this);
}

@JsonSerializable()
class MasteryPoint {
  @JsonKey(name: 'p_l')
  final double pLearn;

  @JsonKey(name: 'p_t')
  final double pTrain;

  @JsonKey(name: 'p_g')
  final double pGuess;

  @JsonKey(name: 'p_s')
  final double pSlip;

  final DateTime updatedAt;

  MasteryPoint({
    required this.pLearn,
    required this.pTrain,
    required this.pGuess,
    required this.pSlip,
    required this.updatedAt,
  });

  factory MasteryPoint.fromJson(Map<String, dynamic> json) =>
      _$MasteryPointFromJson(json);

  Map<String, dynamic> toJson() => _$MasteryPointToJson(this);
}
```

### 3. API Response 래퍼 클래스

```dart
@JsonSerializable(genericArgumentFactories: true)
class ApiResponse<T> {
  final bool success;
  final String message;
  final T? data;

  ApiResponse({
    required this.success,
    required this.message,
    this.data,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Object? json) fromJsonT,
  ) =>
      _$ApiResponseFromJson(json, fromJsonT);

  Map<String, dynamic> toJson(Object? Function(T value) toJsonT) =>
      _$ApiResponseToJson(this, toJsonT);
}
```

### 4. API Client 예시

```dart
class DashboardApiClient {
  final Dio _dio;

  DashboardApiClient(this._dio);

  Future<ApiResponse<KcMasteryTrendResponse>> getKcMasteryTrend({
    required int kcId,
    String? startDate,
    String? endDate,
  }) async {
    final response = await _dio.get(
      '/api/dashboard/kc/mastery-trend',
      queryParameters: {
        'kcId': kcId,
        if (startDate != null) 'startdate': startDate,
        if (endDate != null) 'enddate': endDate,
      },
    );

    return ApiResponse.fromJson(
      response.data,
      (json) => KcMasteryTrendResponse.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<AttendanceResponse>> getAttendancePeriod({
    required String startDate,
    required String endDate,
  }) async {
    final response = await _dio.get(
      '/api/dashboard/attendance',
      queryParameters: {
        'startdate': startDate,
        'enddate': endDate,
      },
    );

    return ApiResponse.fromJson(
      response.data,
      (json) => AttendanceResponse.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<AttendanceResponse>> getAttendanceDaily({
    required String date,
  }) async {
    final response = await _dio.get(
      '/api/dashboard/attendance',
      queryParameters: {'date': date},
    );

    return ApiResponse.fromJson(
      response.data,
      (json) => AttendanceResponse.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<List<PhonemesWrongRankResponse>>> getWrongPhonemesRank({
    required int limit,
  }) async {
    final response = await _dio.get(
      '/api/dashboard/mistake/phonemes/rank',
      queryParameters: {'limit': limit},
    );

    return ApiResponse.fromJson(
      response.data,
      (json) => (json as List)
          .map((e) => PhonemesWrongRankResponse.fromJson(e))
          .toList(),
    );
  }
}
```

### 5. 날짜 포맷 유틸리티

```dart
class DateFormatter {
  static const String apiFormat = 'yyMMdd';
  static const String displayFormat = 'yyyy년 MM월 dd일';

  /// DateTime을 API 형식(yyMMdd)으로 변환
  static String toApiFormat(DateTime date) {
    return DateFormat(apiFormat).format(date);
  }

  /// API 형식(yyMMdd)을 DateTime으로 변환
  static DateTime fromApiFormat(String dateString) {
    return DateFormat(apiFormat).parse(dateString);
  }

  /// LocalDate 문자열(yyyy-MM-dd)을 DateTime으로 변환
  static DateTime fromLocalDateString(String dateString) {
    return DateTime.parse(dateString);
  }

  /// 오늘 날짜를 API 형식으로
  static String today() {
    return toApiFormat(DateTime.now());
  }

  /// 한 달 전 날짜를 API 형식으로
  static String oneMonthAgo() {
    final date = DateTime.now().subtract(const Duration(days: 30));
    return toApiFormat(date);
  }
}

// 사용 예시
final today = DateFormatter.today(); // "250112"
final lastMonth = DateFormatter.oneMonthAgo(); // "241212"
```

### 6. 에러 처리 예시

```dart
try {
  final response = await dashboardApi.getKcMasteryTrend(
    kcId: 1,
    startDate: '250101',
    endDate: '250131',
  );

  if (response.success && response.data != null) {
    // 성공 처리
    final masteryData = response.data!;
    print('KC ${masteryData.kcId} 숙련도: ${masteryData.masteryTrend.length}개');
  } else {
    // 실패 메시지 표시
    showSnackBar(response.message);
  }
} on DioException catch (e) {
  if (e.response?.statusCode == 401) {
    // 인증 실패 - 로그인 화면으로
    navigateToLogin();
  } else if (e.response?.statusCode == 400) {
    // 잘못된 요청
    showSnackBar(e.response?.data['message'] ?? '잘못된 요청입니다.');
  } else {
    // 기타 에러
    showSnackBar('네트워크 오류가 발생했습니다.');
  }
}
```

---

## 추가 참고 사항

### 1. JWT 인증 헤더 설정

```dart
final dio = Dio(BaseOptions(
  baseUrl: 'https://readingbuddyai.co.kr',
  headers: {
    'Content-Type': 'application/json',
  },
));

// 인터셉터로 JWT 토큰 자동 추가
dio.interceptors.add(InterceptorsWrapper(
  onRequest: (options, handler) async {
    final token = await getStoredToken(); // SharedPreferences 등에서 가져오기
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  },
));
```

### 2. 날짜 관련 주의사항

- API 쿼리 파라미터: `yyMMdd` 형식 (예: 250112)
- 응답 LocalDate: `yyyy-MM-dd` 형식 (예: 2025-01-12)
- 응답 DateTime: ISO 8601 형식 (예: 2025-01-12T10:30:00)

### 3. Nullable 필드 처리

```dart
// AttendanceResponse의 경우
if (response.data.periodData != null) {
  // 기간별 조회 결과 처리
  final attendDates = response.data.periodData!.attendDates;
} else if (response.data.dailyData != null) {
  // 일별 조회 결과 처리
  final attended = response.data.dailyData!.attended;
}
```

### 4. 페이징 없음

- 현재 모든 Dashboard API는 페이징을 지원하지 않음
- 대신 `limit` 파라미터나 날짜 범위로 데이터 양 조절

### 5. Stage 값 형식

가능한 stage 값:
- 상세 스테이지: `"1.1.1"`, `"1.1.2"`, `"1.2.1"`, `"1.2.2"`
- 일반 스테이지: `"2"`, `"3"`, `"4"`

---

## 문서 버전

- **버전**: 1.0.0
- **최종 수정일**: 2025-01-12
- **작성자**: Claude Code
- **백엔드 커밋**: 621b995
