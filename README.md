# Reading Buddy

VR 한글 학습 시스템을 위한 모바일 컴패니언 앱

## 프로젝트 소개

Reading Buddy는 VR 기기에서 진행되는 한글 학습을 지원하는 Flutter 기반 모바일 앱입니다. 사용자는 앱을 통해 VR 기기에 간편하게 로그인하고, 학습 진행 상황과 통계를 확인할 수 있습니다.

## 주요 기능

### 인증
- 이메일/비밀번호 기반 로그인 및 회원가입
- **VR 기기 간편 로그인** (10자리 코드 입력)
- JWT 토큰 자동 관리 및 갱신

### 대시보드
- **출석 현황**: 달력 형식의 출석 기록 및 학습 시간
- **학습 분석**: 스테이지별 통계, 정답률, 평균 시도 횟수
- **음소 분석**: 취약 음소 및 시도 횟수 랭킹
- **KC 숙련도**: BKT 기반 학습 진행도 추이

### 프로필
- 사용자 정보 관리
- 테마 변경 (3가지 색상 옵션)

## 빠른 시작

### 1. Flutter 설치
```bash
# Flutter SDK 설치
# https://docs.flutter.dev/get-started/install

# 설치 확인
flutter doctor
```

### 2. 프로젝트 설정
```bash
cd reading_buddy_app

# 패키지 설치
flutter pub get

# 코드 생성
flutter pub run build_runner build --delete-conflicting-outputs
```

### 3. 서버 URL 설정
`reading_buddy_app/lib/core/constants/api_constants.dart` 파일에서 서버 주소 변경:
```dart
static const String baseUrl = 'https://readingbuddyai.co.kr';
```

### 4. 앱 실행
```bash
# 연결된 기기 확인
flutter devices

# 앱 실행
flutter run
```

## 기술 스택

| 카테고리 | 기술 |
|---------|------|
| 프레임워크 | Flutter 3.0+ |
| 언어 | Dart 3.0+ |
| 상태 관리 | Riverpod |
| 네트워킹 | Dio + Retrofit |
| 로컬 저장소 | flutter_secure_storage, shared_preferences |
| 라우팅 | go_router |
| 차트 | fl_chart |
| 달력 | table_calendar |
| 아키텍처 | Clean Architecture |

## 프로젝트 구조

```
reading_buddy_app/
├── lib/
│   ├── core/                    # 핵심 기능
│   │   ├── constants/           # API, Storage 상수
│   │   ├── network/             # Dio, Retrofit 클라이언트
│   │   ├── storage/             # 토큰 저장소
│   │   ├── theme/               # 앱 테마
│   │   ├── router/              # 라우팅 설정
│   │   └── providers/           # 전역 Provider
│   │
│   ├── features/                # 기능별 모듈 (Clean Architecture)
│   │   ├── auth/                # 인증
│   │   │   ├── data/            # API 모델, Repository 구현
│   │   │   ├── domain/          # Repository 인터페이스
│   │   │   └── presentation/    # Provider, 화면, 위젯
│   │   │
│   │   └── dashboard/           # 대시보드
│   │       ├── data/
│   │       ├── domain/
│   │       └── presentation/
│   │
│   └── main.dart                # 앱 진입점
```

## 문서

프로젝트 관련 상세 문서는 `docs/` 폴더에서 확인할 수 있습니다:

- **[API 명세서](docs/API.md)** - 백엔드 API 상세 명세
- **[개발 가이드](docs/DEVELOPMENT.md)** - 아키텍처, API 추가 방법, 테스트 가이드
- **[백엔드 분석](docs/BACKEND.md)** - 백엔드 구조 및 데이터 모델
- **[디자인 계획](docs/DESIGN.md)** - UI/UX 디자인 가이드

## 주요 화면

### 인증
- 로그인 화면
- 회원가입 화면
- VR 기기 인증 화면 (Device Code 입력)

### 대시보드
- 홈: 출석 현황, 오늘의 학습 통계
- 학습 분석: 스테이지별 통계, 음소 분석
- 출석: 달력 형식의 출석 기록
- 프로필: 사용자 정보, 설정

## 개발 환경

```bash
# 패키지 업데이트
flutter pub upgrade

# 캐시 정리
flutter clean

# 코드 생성 (모델 변경 시)
flutter pub run build_runner build --delete-conflicting-outputs

# 테스트 실행
flutter test
```

## 빌드

### Android
```bash
# APK 빌드
flutter build apk --release

# App Bundle (Play Store)
flutter build appbundle --release
```

### iOS
```bash
# iOS 빌드
flutter build ios --release
```

## 주요 기능 시나리오

### VR 기기 로그인
1. VR 기기에서 "앱으로 로그인" 선택
2. 화면에 10자리 코드 표시
3. 모바일 앱에서 로그인 후 "VR 기기 연결" 선택
4. 코드 입력 및 인증
5. VR 기기에 자동 로그인 완료

### 학습 현황 확인
1. 앱 로그인
2. 홈 화면에서 출석 현황 확인
3. 학습 분석 탭에서 스테이지별 통계 확인
4. 취약 음소 분석으로 학습 포인트 파악

## 라이선스

교육 목적으로 제작된 프로젝트입니다.

## 문의

프로젝트 관련 문의는 이슈를 생성해주세요.

---

**Server**: https://readingbuddyai.co.kr
**Framework**: Flutter 3.0+
**Architecture**: Clean Architecture
