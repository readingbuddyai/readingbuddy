# Reading Buddy

> 난독증 아동을 위한 VR 기반 한글 학습 시스템

---

## 서비스 소개

**Reading Buddy**는 난독증을 가진 어린이들을 위한 VR 기반 한글 학습 플랫폼입니다.

AI 음성 인식 기술을 활용하여 자음과 모음 발음을 정확하게 분석하고, 몰입감 있는 VR 게임 환경에서 재미있게 한글을 익힐 수 있도록 돕습니다.

### 서비스 배경

난독증(Dyslexia)은 읽기와 쓰기에 어려움을 겪는 학습 장애로, 전체 아동의 약 5-10%가 경험합니다.

특히 한글은 자음과 모음을 조합하는 독특한 구조로 인해 난독증 아동에게 학습 난이도가 높습니다. 기존 학습 방법은 반복적이고 지루하여 학습 의욕을 떨어뜨리는 문제가 있습니다.

**Reading Buddy**는 VR 기술과 AI를 결합하여:
- 몰입감 있는 훈련 환경에서 자연스럽게 한글을 학습
- 음소 단위 발음 분석으로 정확한 피드백 제공
- BKT(Bayesian Knowledge Tracing) 알고리즘으로 개인별 학습 진도 관리
- 모바일 앱을 통한 학습 현황 추적 및 취약 음소 분석

---

## 주요 기능

### 1. VR 한글 학습 게임
- **자모 중심 학습**: 자음(ㄱ, ㄴ, ㄷ...), 모음(ㅏ, ㅓ, ㅗ...) 개별 발음 연습
- **스테이지별 난이도**: 자모 → 음절 → 단어로 점진적 학습
- **실시간 음성 인식**: AI 기반 발음 분석 및 즉각적 피드백
- **게임화된 학습**: 마법사가 되어 주문(한글)을 외우는 재미있는 스토리
- **BKT 기반 진도 관리**: 개인별 음소 숙련도(KC)를 추적하여 맞춤형 문제 제공

### 2. 모바일 컴패니언 앱
- **간편 VR 로그인**: 4자리 코드로 VR 기기와 빠른 연결
- **학습 현황 대시보드**: 출석 기록, 스테이지별 성취도, 일일 학습 시간
- **취약 음소 분석**: 개인별로 어려워하는 자음/모음 파악 및 추천 학습
- **KC 숙련도 그래프**: BKT 기반 학습 진행도 시각화
- **학습 통계**: 정답률, 평균 시도 횟수, 음소별 성공률

### 3. AI 발음 분석 엔진
- **Wav2Vec2 + LoRA**: 한국어 어린이 음성(3500시간) 특화 파인튜닝
- **음소 단위 분석**: 자모(ㄱ, ㄴ, ㅏ, ㅓ) 개별 발음 정확도 평가
- **음절/단어 지원**: "가", "나", "감자", "사과" 등 다양한 단위 분석
- **실시간 피드백**: 평균 0.1초 이내 발음 교정 피드백
- **성능 지표**:
  - PER: 36.67% → 14.55% (60.3% 개선)
  - CER: 26.40% → 10.51% (60.2% 개선)
  - 베이스 모델 대비 2.5배 정확도 향상

---

## 사용법

### VR 학습 시작하기

1. **VR 기기 착용 및 앱 실행**
2. **모바일 앱에서 로그인 코드 입력** (4자리)
3. **스테이지 선택 및 학습 시작**
4. **발음 학습 진행**
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
                    ┌─────────────────────┐
                    │   Client Layer      │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
    ┌─────────▼─────────┐      │      ┌────────▼─────────┐
    │  VR (Unity)       │      │      │  Mobile (Flutter)│
    │  - Learning Game  │      │      │  - Dashboard     │
    │  - Voice Input    │      │      │  - Statistics    │
    │  - Real Feedback  │      │      │  - VR Login      │
    └─────────┬─────────┘      │      └────────┬─────────┘
              │                │                │
              └────────────────┼────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │  API Gateway        │
                    │  (Traefik)          │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
    ┌─────────▼─────────┐      │      ┌────────▼─────────┐
    │  Backend          │      │      │  AI Server       │
    │  (Spring Boot)    │      │      │  (FastAPI)       │
    │  - Auth (JWT)     │      │      │  - Wav2Vec2      │
    │  - Data Mgmt      │      │      │  - LoRA          │
    │  - Statistics     │      │      │  - Inference     │
    │  - KC Tracking    │      │      │  - Phoneme Check │
    └─────────┬─────────┘      │      └────────┬─────────┘
              │                │                │
              └────────────────┼────────────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
    ┌─────────▼─────────┐      │      ┌────────▼─────────┐
    │  MySQL            │      │      │  Redis           │
    │  - User Data      │      │      │  - Session       │
    │  - Learning Log   │      │      │  - VR Code       │
    │  - KC Data        │      │      │  - Cache         │
    └───────────────────┘      │      └──────────────────┘
                               │
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
│   │   ├── routers/       # 라우터 (자모/음절/단어 검사)
│   │   └── services/      # 추론 서비스 (Wav2Vec2 + LoRA)
│   ├── models/            # Wav2Vec2 + LoRA 모델 (1.2GB + 23MB)
│   ├── Dockerfile
│   └── README.md          # AI 서버 상세 문서
│
├── Fine-tuning/           # AI 모델 학습 파이프라인
│   ├── scripts/           # 학습/평가 스크립트
│   │   ├── training/      # Phase 1→2→3 학습
│   │   └── evaluation/    # WER/CER 평가
│   ├── docs/              # 학습 계획 및 분석 문서
│   ├── results/           # 실험 결과 (WER 14.8%)
│   └── README.md          # Fine-tuning 상세 문서
│
├── monitoring/            # 모니터링 (Prometheus + Grafana)
│   ├── prometheus/        # Prometheus 설정
│   ├── grafana/           # Grafana 대시보드
│   └── docker-compose.yml
│
├── dummy-scripts/         # 유틸리티 스크립트
│   ├── generate_voice.py  # gTTS 기반 음성 파일 생성 및 S3 업로드
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
| **Mobile App** | Flutter 기반 학습 현황 대시보드 앱 | [reading_buddy_app/README.md](reading_buddy_app/README.md) |
| **AI Server** | Wav2Vec2 + LoRA 기반 한국어 발음 분석 API | [ai/README.md](ai/README.md) |
| **Fine-tuning** | 어린이 음성 특화 AI 모델 학습 파이프라인 | [Fine-tuning/README.md](Fine-tuning/README.md) |
| **Scripts** | 학습용 음성 데이터 생성 유틸리티 (gTTS + S3) | [dummy-scripts/README.md](dummy-scripts/README.md) |

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

#### 1. LoRA Fine-tuning의 극적인 성능 향상
- **PER (Phoneme Error Rate)**: 36.67% → 14.55% (60.3% 개선)
- **CER (Character Error Rate)**: 26.40% → 10.51% (60.2% 개선)
- **상대적 오류율 감소**: 60%
- **실질적 의미**: 베이스 모델 대비 2.5배 정확도 향상

#### 2. 목표 달성 여부
- **LoRA r32 Final 모델**: 목표 달성 (PER < 15%) ✅
- **Base 모델**: 목표 미달성 (PER > 18%) ❌

#### 3. 학습 방법 및 최적화
- **학습 방법**: 3단계 Curriculum Learning (30h → 90h → 148h)
- **최적화**: LoRA (r=32) - 효율적 파인튜닝
- **데이터**: 한국어 어린이 음성 100시간으로 효과적인 도메인 적응 성공

#### 4. 결론
LoRA r32 fine-tuning이 아동 음성 인식에 매우 효과적임을 확인했습니다. 베이스 모델 단독으로는 실용화가 어려운 성능(PER 36.67%)이었지만, LoRA 파인튜닝 후 실용 수준(PER 14.55%)에 도달했습니다.

### 난독증 아동 학습 효과
- **음소 중심 접근**: 자음/모음을 개별적으로 학습하여 난독증 아동에게 효과적
- **실시간 피드백**: 즉각적인 발음 교정으로 학습 동기 향상
- **개인 맞춤형 학습**: BKT 기반 KC 숙련도 추적으로 각 아동의 속도에 맞춘 학습
- **몰입형 VR 환경**: 게임화로 지루함 없이 반복 학습 가능
- **취약 음소 집중**: 개인별로 어려워하는 자음/모음 집중 훈련

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
