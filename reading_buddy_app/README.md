# Reading Buddy - 모바일 앱

VR 한글 학습 시스템의 모바일 컴패니언 앱

## 프로젝트 개요

Reading Buddy는 VR 기기에서 진행되는 한글 학습을 지원하는 모바일 앱입니다.

### 주요 기능

1. **로그인 및 회원가입**
   - 이메일/비밀번호 기반 인증
   - JWT 토큰 관리 (자동 갱신)
   - 자동 로그인

2. **VR 기기 로그인 지원**
   - VR에서 생성된 10자리 코드 입력
   - 간편한 VR 기기 인증

3. **대시보드**
   - 출석 달력 및 학습 현황
   - 스테이지별 학습 통계
   - 취약 음소 분석
   - 학습 트렌드 그래프

## 기술 스택

- **Flutter** 3.0+
- **Dart** 3.0+
- **상태 관리**: Riverpod
- **네트워킹**: Dio + Retrofit
- **로컬 저장소**:
  - flutter_secure_storage (토큰)
  - shared_preferences (일반 데이터)
- **라우팅**: go_router
- **차트**: fl_chart
- **달력**: table_calendar

## 프로젝트 구조

```
lib/
├── core/                      # 핵심 기능
│   ├── config/                # 설정
│   ├── constants/             # 상수 (API, Storage)
│   ├── network/               # 네트워크 (Dio, ApiClient)
│   ├── storage/               # 로컬 저장소 (TokenStorage)
│   ├── theme/                 # 테마 (3가지 색상 옵션)
│   ├── router/                # 라우팅
│   ├── providers/             # 공통 Provider
│   └── utils/                 # 유틸리티
│
├── features/                  # 기능별 모듈
│   ├── auth/                  # 인증
│   │   ├── data/              # 데이터 레이어
│   │   │   ├── datasources/   # API 호출
│   │   │   ├── models/        # 데이터 모델
│   │   │   └── repositories/  # Repository 구현
│   │   ├── domain/            # 도메인 레이어
│   │   │   ├── entities/      # 엔티티
│   │   │   ├── repositories/  # Repository 인터페이스
│   │   │   └── usecases/      # 비즈니스 로직
│   │   └── presentation/      # 프레젠테이션 레이어
│   │       ├── providers/     # Riverpod Provider
│   │       ├── screens/       # 화면
│   │       └── widgets/       # 위젯
│   │
│   └── dashboard/             # 대시보드
│       ├── data/
│       ├── domain/
│       └── presentation/
│
└── main.dart                  # 앱 진입점
```

### Clean Architecture 원칙

- **Data Layer**: API 통신, 로컬 저장소 접근
- **Domain Layer**: 비즈니스 로직, 인터페이스 정의
- **Presentation Layer**: UI, 상태 관리

## 설치 및 실행

### 1. Flutter 설치

Flutter 공식 사이트에서 설치: https://docs.flutter.dev/get-started/install

```bash
flutter --version
```

### 2. 프로젝트 클론

```bash
cd c:\Users\kyn05\Desktop\어플\app\reading_buddy_app
```

### 3. 패키지 설치

```bash
flutter pub get
```

### 4. 코드 생성 (Retrofit, JSON Serialization)

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### 5. 서버 URL 설정

`lib/core/constants/api_constants.dart` 파일에서 서버 URL을 설정하세요:

```dart
static const String baseUrl = 'http://your-server-url.com';
```

### 6. 앱 실행

```bash
# Android
flutter run

# iOS
flutter run

# 특정 기기 선택
flutter devices
flutter run -d <device-id>
```

## 빌드

### Android APK

```bash
flutter build apk --release
```

### iOS IPA

```bash
flutter build ios --release
```

## 주요 파일 설명

### Core

- **`core/constants/api_constants.dart`**: API 엔드포인트 정의
- **`core/network/dio_client.dart`**: Dio 설정, 인터셉터 (토큰 자동 추가, 401 처리)
- **`core/network/api_client.dart`**: Retrofit API 클라이언트
- **`core/storage/token_storage.dart`**: 토큰 및 사용자 정보 저장
- **`core/theme/app_theme.dart`**: 3가지 테마 (warm, professional, energetic)
- **`core/providers/providers.dart`**: 전역 Provider 정의

### Auth

- **`features/auth/presentation/screens/login_screen.dart`**: 로그인 화면
- **`features/auth/presentation/screens/signup_screen.dart`**: 회원가입 화면
- **`features/auth/presentation/screens/device_auth_screen.dart`**: VR 기기 인증 화면
- **`features/auth/presentation/providers/auth_provider.dart`**: 인증 상태 관리

### Dashboard

- **`features/dashboard/presentation/screens/main_screen.dart`**: 메인 화면 (탭 네비게이션)
- **`features/dashboard/presentation/screens/home_screen.dart`**: 홈 (출석 달력)
- **`features/dashboard/presentation/screens/analysis_screen.dart`**: 학습 분석
- **`features/dashboard/presentation/screens/attendance_screen.dart`**: 출석 상세
- **`features/dashboard/presentation/screens/profile_screen.dart`**: 프로필

## 개발 가이드

### 새로운 기능 추가

1. **기능 폴더 생성**: `features/your_feature/`
2. **레이어별 구조 생성**: `data/`, `domain/`, `presentation/`
3. **API 모델 작성**: `data/models/` (JSON Serialization)
4. **Repository 작성**: `data/repositories/` (구현), `domain/repositories/` (인터페이스)
5. **Provider 작성**: `presentation/providers/`
6. **UI 작성**: `presentation/screens/`, `presentation/widgets/`

### API 추가

1. **ApiClient 수정** (`core/network/api_client.dart`):
   ```dart
   @GET('/api/your-endpoint')
   Future<ApiResponse<YourModel>> yourMethod();
   ```

2. **코드 생성**:
   ```bash
   flutter pub run build_runner build --delete-conflicting-outputs
   ```

### 테마 변경

`lib/core/theme/app_theme.dart`에서 색상 수정 또는 새 테마 추가

## 디버깅

### 로그 확인

- **Logger**: 네트워크 요청/응답 자동 로깅
- **개발자 도구**: `flutter run` 시 DevTools 사용 가능

### 문제 해결

1. **패키지 문제**:
   ```bash
   flutter pub cache repair
   flutter clean
   flutter pub get
   ```

2. **빌드 오류**:
   ```bash
   flutter pub run build_runner clean
   flutter pub run build_runner build --delete-conflicting-outputs
   ```

3. **토큰 관련 오류**: SecureStorage 초기화 문제 확인

## 향후 개발 계획

### Phase 2: 대시보드 강화
- [ ] 실제 데이터와 연동
- [ ] 출석 달력 구현 (table_calendar)
- [ ] 스테이지별 통계 차트 (fl_chart)
- [ ] 취약 음소 분석 시각화
- [ ] Pull-to-refresh

### Phase 3: UX 개선
- [ ] 애니메이션 추가
- [ ] 오프라인 모드 (캐시)
- [ ] 푸시 알림
- [ ] 다크 모드
- [ ] 다국어 지원

### Phase 4: 확장 기능
- [ ] 프로필 수정
- [ ] 학습 목표 설정
- [ ] 주간/월간 리포트
- [ ] 배지 시스템

## 테스트

```bash
# 유닛 테스트
flutter test

# 위젯 테스트
flutter test test/widget_test.dart

# 통합 테스트
flutter drive --target=test_driver/app.dart
```

## 기여

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 라이선스

이 프로젝트는 교육 목적으로 만들어졌습니다.

## 문의

프로젝트 관련 문의는 이슈를 생성해주세요.

---

**Made with ❤️ by Reading Buddy Team**
