# Reading Buddy

> VR 기반 한글 학습 시스템

---

## 서비스 소개

**Reading Buddy**는 VR 환경에서 어린이들이 재미있게 한글을 학습할 수 있는 교육 플랫폼입니다.

AI 음성 인식 기술을 활용하여 발음을 분석하고, 게임화된 학습 경험을 통해 한글 습득을 돕습니다.

### 서비스 배경

최근 COVID-19 이후 비대면 교육의 중요성이 부각되면서, 교육 콘텐츠의 디지털화가 가속화되고 있습니다.

특히 한글 학습은 어린이 교육의 기초이지만, 기존 학습 방법은 단조롭고 흥미를 잃기 쉽다는 문제가 있습니다.

**Reading Buddy**는 VR 기술과 AI를 결합하여 몰입감 있는 학습 환경을 제공하고,<br>
개인별 발음 분석을 통해 맞춤형 피드백을 제공하여 효과적인 한글 학습을 지원합니다.

---

## 주요 기능

### VR 한글 학습 게임
- **스테이지별 학습**: 자음/모음부터 단어까지 단계적 학습
- **실시간 음성 인식**: AI 기반 발음 분석 및 피드백
- **게임화된 학습**: 마법사 컨셉의 재미있는 학습 경험
- **KC 기반 진도 관리**: BKT(Bayesian Knowledge Tracing) 알고리즘으로 학습 진행도 추적

### 모바일 컴패니언 앱
- **간편 VR 로그인**: 10자리 코드로 VR 기기 빠른 연결
- **학습 현황 대시보드**: 출석, 스테이지별 통계, 음소 분석
- **취약 음소 분석**: 개인별 발음 약점 파악
- **학습 진행도 추이**: KC 숙련도 기반 학습 성장 그래프

### AI 발음 분석
- **Wav2Vec2 + LoRA**: 한국어 어린이 음성 특화 모델
- **실시간 음소 단위 분석**: 정확한 발음 평가
- **맞춤형 피드백**: 틀린 발음에 대한 즉각적 피드백

---

## 사용법

### VR 학습 시작하기

1. **VR 기기 착용 및 앱 실행**
2. **모바일 앱에서 로그인 코드 입력** (10자리)
3. **스테이지 선택 및 학습 시작**
4. **마법 지팡이로 발음 학습**
5. **학습 결과 확인 및 다음 단계 진행**

### 모바일 앱 사용하기

1. **회원가입 및 로그인**
2. **VR 기기 연결** (코드 입력)
3. **대시보드에서 학습 현황 확인**
4. **취약 음소 분석으로 학습 포인트 파악**

---

## 기술 스택

| Category | Technologies |
|----------|-------------|
| **Frontend (VR)** | ![Unity](https://img.shields.io/badge/-Unity-000000?logo=unity) ![C#](https://img.shields.io/badge/-C%23-239120?logo=c-sharp&logoColor=white) ![Meta Quest](https://img.shields.io/badge/-Meta_Quest-0467DF?logo=oculus) |
| **Frontend (Mobile)** | ![Flutter](https://img.shields.io/badge/-Flutter-02569B?logo=flutter) ![Dart](https://img.shields.io/badge/-Dart-0175C2?logo=dart&logoColor=white) ![Riverpod](https://img.shields.io/badge/-Riverpod-00D9FF) |
| **Backend** | ![Spring Boot](https://img.shields.io/badge/-Spring_Boot-6DB33F?logo=spring-boot&logoColor=white) ![Java](https://img.shields.io/badge/-Java-ED8B00?logo=openjdk&logoColor=white) ![Spring Security](https://img.shields.io/badge/-Spring_Security-6DB33F?logo=Spring-Security&logoColor=white) ![MySQL](https://img.shields.io/badge/-MySQL-005C84?logo=mysql&logoColor=white) ![Redis](https://img.shields.io/badge/-Redis-DC382D?logo=redis&logoColor=white) |
| **AI** | ![PyTorch](https://img.shields.io/badge/-PyTorch-EE4C2C?logo=pytorch&logoColor=white) ![Wav2Vec2](https://img.shields.io/badge/-Wav2Vec2-FF6F00) ![LoRA](https://img.shields.io/badge/-LoRA-9C27B0) ![FastAPI](https://img.shields.io/badge/-FastAPI-009688?logo=FastAPI&logoColor=white) |
| **DevOps** | ![Docker](https://img.shields.io/badge/-Docker-2496ED?logo=docker&logoColor=white) ![Jenkins](https://img.shields.io/badge/-Jenkins-D24939?logo=Jenkins&logoColor=white) ![Kubernetes](https://img.shields.io/badge/-Kubernetes-326CE5?logo=kubernetes&logoColor=white) ![Traefik](https://img.shields.io/badge/-Traefik_Proxy-24A1C1?logo=traefikproxy&logoColor=white) |
| **Monitoring** | ![Prometheus](https://img.shields.io/badge/-Prometheus-E6522C?logo=prometheus&logoColor=white) ![Grafana](https://img.shields.io/badge/-Grafana-F46800?logo=grafana&logoColor=white) |
| **Collaboration** | ![Git](https://img.shields.io/badge/-Git-F05032?logo=git&logoColor=white) ![GitLab](https://img.shields.io/badge/-GitLab-FCA121?logo=gitlab&logoColor=white) ![Jira](https://img.shields.io/badge/-Jira-0052CC?logo=jira&logoColor=white) ![Notion](https://img.shields.io/badge/-Notion-000000?logo=notion&logoColor=white) |

---

## 시스템 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                         Client Layer                         │
├──────────────────────────┬──────────────────────────────────┤
│   VR (Unity + Meta Quest) │  Mobile App (Flutter)           │
│   - 한글 학습 게임        │  - 대시보드                      │
│   - 음성 녹음 및 전송     │  - VR 로그인 코드 입력           │
│   - 실시간 피드백         │  - 학습 현황 조회                │
└──────────────────────────┴──────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      API Gateway (Traefik)                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────┬──────────────────────────────────┐
│   Backend (Spring Boot)  │     AI Server (FastAPI)          │
│   - 인증/인가 (JWT)      │     - Wav2Vec2 + LoRA            │
│   - 학습 데이터 관리     │     - 음성 → 음소 변환           │
│   - 통계 및 분석         │     - 발음 정확도 평가           │
│   - KC 관리              │     - 실시간 추론                │
└──────────────────────────┴──────────────────────────────────┘
                            ↓
┌──────────────────────────┬──────────────────────────────────┐
│    MySQL (주 DB)         │     Redis (캐시 & 세션)          │
│    - 사용자 정보         │     - VR 로그인 코드             │
│    - 학습 기록           │     - JWT 토큰 관리              │
│    - KC 데이터           │     - 임시 데이터                │
└──────────────────────────┴──────────────────────────────────┘
```

---

## 프로젝트 구조

```
S13P31A206/
├── frontend/              # VR Unity 프로젝트
│   └── unity/             # Unity 프로젝트 루트
│       ├── Assets/        # 게임 에셋, 스크립트
│       └── ...
│
├── reading_buddy_app/     # Flutter 모바일 앱
│   ├── lib/               # Dart 소스 코드
│   │   ├── features/      # 기능별 모듈 (Clean Architecture)
│   │   ├── core/          # 공통 기능 (네트워크, 라우팅 등)
│   │   └── main.dart
│   └── README.md          # 모바일 앱 상세 문서
│
├── backend/               # Spring Boot API 서버
│   ├── src/               # Java 소스 코드
│   ├── build.gradle       # Gradle 빌드 설정
│   └── ...
│
├── ai/                    # AI 음성 인식 서버
│   ├── app/               # FastAPI 애플리케이션
│   │   ├── main.py        # API 엔드포인트
│   │   ├── routers/       # 라우터
│   │   └── services/      # 추론 서비스
│   ├── models/            # Wav2Vec2 + LoRA 모델
│   ├── Dockerfile
│   └── README.md          # AI 서버 상세 문서
│
├── Fine-tuning/           # AI 모델 학습 파이프라인
│   ├── scripts/           # 학습/평가 스크립트
│   │   ├── training/      # Phase별 학습
│   │   └── evaluation/    # 모델 평가
│   ├── docs/              # 학습 계획 및 분석 문서
│   ├── results/           # 실험 결과
│   └── README.md          # Fine-tuning 상세 문서
│
├── monitoring/            # 모니터링 (Prometheus + Grafana)
│   ├── prometheus/        # Prometheus 설정
│   ├── grafana/           # Grafana 대시보드
│   └── docker-compose.yml
│
├── dummy-scripts/         # 유틸리티 스크립트
│   ├── generate_voice.py  # TTS 음성 파일 생성
│   └── README.md          # 스크립트 사용법
│
└── .gitlab/               # GitLab CI/CD 설정
    └── merge_request_templates/
```

---

## 상세 문서

각 모듈별 상세 문서는 아래 링크를 참고하세요:

| 모듈 | 설명 | 문서 링크 |
|-----|------|----------|
| **Mobile App** | Flutter 기반 모바일 컴패니언 앱 | [reading_buddy_app/README.md](reading_buddy_app/README.md) |
| **AI Server** | Wav2Vec2 + LoRA 음성 인식 API | [ai/README.md](ai/README.md) |
| **Fine-tuning** | AI 모델 학습 파이프라인 및 실험 결과 | [Fine-tuning/README.md](Fine-tuning/README.md) |
| **Scripts** | TTS 음성 생성 유틸리티 | [dummy-scripts/README.md](dummy-scripts/README.md) |

---

## 빠른 시작

### 1. 저장소 클론

```bash
git clone https://lab.ssafy.com/s13-final/S13P31A206.git
cd S13P31A206
```

### 2. 각 모듈 실행

#### 모바일 앱
```bash
cd reading_buddy_app
flutter pub get
flutter run
```

#### AI 서버
```bash
cd ai
docker-compose up -d
```

#### 백엔드
```bash
cd backend
./gradlew bootRun
```

#### 모니터링
```bash
cd monitoring
docker-compose up -d
```

자세한 실행 방법은 각 모듈의 README를 참고하세요.

---

## 주요 성과

### AI 모델 성능
- **WER (Word Error Rate)**: 14.8% (148h 학습 데이터)
- **베이스라인 대비**: 47.7% 성능 향상 (28.3% → 14.8%)
- **학습 방법**: Curriculum Learning (Phase 1→2→3)
- **최적화**: LoRA (r=16) - 파라미터 +2.1M로 효율적 학습

### 학습 효과
- **단계별 학습**: 자음/모음 → 글자 → 단어
- **개인 맞춤형**: BKT 기반 KC 숙련도 추적
- **즉각적 피드백**: 실시간 발음 분석 및 교정

---

## 팀 구성

> 팀원 정보는 추후 업데이트 예정입니다.

---

## 라이선스

교육 목적으로 제작된 프로젝트입니다.

---

## 문의

프로젝트 관련 문의사항은 GitLab 이슈를 통해 등록해주세요.

---

**Last Updated**: 2025-11-16
**Project**: SSAFY 13기 특화 프로젝트
