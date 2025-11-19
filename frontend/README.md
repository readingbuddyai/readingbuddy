# Reading Buddy - VR Frontend

> Unity 기반 VR 한글 학습 게임

난독증 아동을 위한 몰입형 VR 한글 학습 환경을 제공하는 Unity 프로젝트입니다.

---

## 프로젝트 소개

Reading Buddy VR은 Meta Quest를 타겟으로 하는 VR 기반 한글 학습 게임입니다. 마법사가 되어 주문(한글)을 외우는 스토리 기반 학습을 통해 아동이 자연스럽게 한글을 익힐 수 있도록 설계되었습니다.

## 주요 기능

### 1. 단계별 학습 시스템
- **자모 학습 (1단계)**: 자음(ㄱ, ㄴ, ㄷ...)과 모음(ㅏ, ㅓ, ㅗ...) 개별 발음 연습
- **음절 학습 (2단계)**: 자모 조합 음절("가", "나", "다"...) 학습
- **단어 학습 (3-4단계)**: 실제 단어("감자", "사과"...) 발음 및 조합 연습

### 2. VR 인터랙션
- **음성 인식**: 마이크를 통한 실시간 발음 입력
- **손 제스처**: Meta Quest 컨트롤러를 이용한 직관적 UI 조작
- **Drag & Drop**: 자모 돌멩이를 드래그하여 음절 조합
- **VFX 피드백**: 정답 시 화려한 마법 효과, 오답 시 재도전 안내

### 3. AI 발음 분석 연동
- **실시간 피드백**: AI 서버를 통한 발음 정확도 즉시 분석
- **음소 단위 평가**: 자모별 발음 정확도 세밀하게 체크
- **유사도 계산**: 발음이 틀렸을 때 어느 부분이 틀렸는지 안내

### 4. 학습 진행도 관리
- **BKT 알고리즘**: Bayesian Knowledge Tracing 기반 개인별 숙련도 추적
- **세션 관리**: 학습 시작/종료 시간, 정답률, 시도 횟수 자동 기록
- **서버 동기화**: 백엔드와 실시간 학습 데이터 동기화

### 5. 간편 로그인
- **디바이스 코드 로그인**: 모바일 앱에서 생성한 코드로 VR 기기 간편 로그인
- **JWT 토큰 관리**: 자동 인증 및 세션 유지

---

## 기술 스택

| 카테고리 | 기술 |
|---------|------|
| 엔진 | Unity 2022.3.62f2 (LTS) |
| 언어 | C# |
| VR SDK | XR Interaction Toolkit 2.6.5, Meta XR SDK (OpenXR 1.14.3) |
| 렌더 파이프라인 | Universal Render Pipeline (URP) 14.0.12 |
| UI | TextMesh Pro 3.0.7, Unity UI (UGUI) |
| 오디오 | Unity Audio System |
| 네트워킹 | UnityWebRequest (REST API) |
| 타겟 플랫폼 | Meta Quest 2/3/Pro (Android) |

---

## 프로젝트 구조

```
frontend/
├── unity/                          # Unity 프로젝트 루트
│   ├── Assets/                     # 게임 에셋
│   │   ├── Scenes/                 # Unity 씬 파일
│   │   │   ├── _Persistent.unity   # 글로벌 매니저 (XR Origin, Audio 등)
│   │   │   ├── Home.unity          # 홈 스테이지 (시작 화면)
│   │   │   ├── Lobby.unity         # 로비 (스테이지 선택)
│   │   │   ├── 1.1.unity           # 스테이지 1-1 (자음 학습)
│   │   │   ├── 1.2.unity           # 스테이지 1-2 (모음 학습)
│   │   │   ├── 2.1.unity           # 스테이지 2-1 (자음 음절)
│   │   │   ├── 2.2.unity           # 스테이지 2-2 (모음 음절)
│   │   │   ├── 4.1.unity           # 스테이지 4-1 (단어 조합)
│   │   │   └── 4.2.unity           # 스테이지 4-2 (단어 학습)
│   │   │
│   │   ├── Scripts/                # C# 스크립트
│   │   │   ├── Auth/               # 인증 관련
│   │   │   │   └── DeviceLoginManager.cs
│   │   │   ├── Managers/           # 게임 매니저
│   │   │   │   ├── SceneFlowManager.cs          # 씬 전환 관리
│   │   │   │   ├── GlobalLeftTriggerModal.cs    # 일시정지 메뉴
│   │   │   │   ├── XROriginSceneManager.cs      # XR Origin 관리
│   │   │   │   └── ...
│   │   │   ├── Stage/              # 스테이지 컨트롤러
│   │   │   │   ├── Stage11Controller.cs         # 1-1 로직
│   │   │   │   ├── Stage12Controller.cs         # 1-2 로직
│   │   │   │   ├── Stage20Controller.cs         # 2단계 공통
│   │   │   │   ├── Stage41Controller.cs         # 4-1 로직
│   │   │   │   ├── Stage42Controller.cs         # 4-2 로직
│   │   │   │   ├── StageQuestionController.cs   # 문제 출제
│   │   │   │   ├── StageSessionController.cs    # 세션 관리
│   │   │   │   ├── StageTutorialController.cs   # 튜토리얼
│   │   │   │   └── UI/                          # 스테이지 UI
│   │   │   │       ├── PhonemeDraggableUI.cs
│   │   │   │       └── PhonemeSlotUI.cs
│   │   │   ├── UI/                 # 공통 UI
│   │   │   │   ├── HomeSceneUI.cs
│   │   │   │   ├── LobbySceneUI.cs
│   │   │   │   └── Stone/          # Drag & Drop 시스템
│   │   │   ├── Utils/              # 유틸리티
│   │   │   │   ├── ApiAuthHelper.cs             # API 인증
│   │   │   │   ├── BgmManager.cs                # 배경음악
│   │   │   │   ├── GlobalSfxManager.cs          # 효과음
│   │   │   │   └── WavUtility.cs                # WAV 인코딩
│   │   │   └── XR/                 # XR 설정
│   │   │       └── ForceOpenXRStart.cs
│   │   │
│   │   ├── Prefabs/                # 프리팹
│   │   ├── Materials/              # 머티리얼
│   │   ├── Animations/             # 애니메이션
│   │   ├── Sound/                  # 효과음
│   │   ├── Voice/                  # 음성 파일
│   │   ├── Images/                 # UI 이미지
│   │   ├── FullOpaqueSpell/        # VFX 에셋
│   │   └── XR/                     # XR 설정 파일
│   │
│   ├── ProjectSettings/            # 프로젝트 설정
│   └── Packages/                   # 패키지 의존성
│
└── README.md                       # 프론트엔드 문서
```

---

## 환경 요구사항

### 개발 환경
- **Unity**: 2022.3.62f2 LTS 이상
- **OS**: Windows 10/11 또는 macOS
- **IDE**: Visual Studio 2022 또는 Visual Studio Code
- **Meta Quest Link**: VR 디버깅용

### 빌드 타겟
- **Platform**: Android
- **VR Device**: Meta Quest 2/3/Pro
- **API Level**: Android 10.0 (API 29) 이상

---

## 빠른 시작

### 1. Unity 설치

1. Unity Hub 다운로드 및 설치
2. Unity 2022.3.62f2 LTS 설치
3. Android Build Support 모듈 추가

### 2. 프로젝트 열기

```bash
# 저장소 클론
git clone https://lab.ssafy.com/s13-final/S13P31A206.git
cd S13P31A206/frontend/unity
```

Unity Hub에서 **Add** → `frontend/unity` 폴더 선택

### 3. 에디터에서 실행

1. Unity에서 프로젝트 열기
2. [Scenes/_Persistent.unity](unity/Assets/Scenes/_Persistent.unity) 씬 로드
3. [Scenes/Home.unity](unity/Assets/Scenes/Home.unity) 씬 추가 로드 (Additive)
4. Play 버튼 클릭

### 4. 서버 URL 설정

[Assets/Scripts/Utils/ApiAuthHelper.cs](unity/Assets/Scripts/Utils/ApiAuthHelper.cs)에서 서버 주소 확인

---

## 빌드 및 배포

### Android (Meta Quest) 빌드

1. **Build Settings 설정**
   - File → Build Settings
   - Platform: Android 선택
   - Switch Platform

2. **Player Settings 확인**
   - Edit → Project Settings → Player
   - Company Name, Product Name 설정
   - Package Name: com.readingbuddy.vr
   - Minimum API Level: Android 10.0 (API 29)

3. **XR 설정 확인**
   - Project Settings → XR Plug-in Management
   - Android 탭에서 OpenXR 체크
   - Meta Quest Support 활성화

4. **빌드 실행**
   - File → Build Settings → Build And Run

5. **Meta Quest에 설치**
   - Quest를 개발자 모드로 전환
   - USB-C로 PC 연결
   - ADB를 통해 APK 설치

---

## 개발 가이드

### API 연동 예시

**발음 검사 API 호출:**

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class VoiceRecognitionService : MonoBehaviour
{
    private const string API_URL = "https://readingbuddyai.co.kr/check/jamo";

    public IEnumerator CheckPronunciation(byte[] audioData, string target)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "audio.wav", "audio/wav");
        form.AddField("target", target);

        using (UnityWebRequest www = UnityWebRequest.Post(API_URL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                var result = JsonUtility.FromJson<PronunciationResult>(response);
                Debug.Log("Is Correct: " + result.is_correct);
            }
        }
    }
}
```

### 마이크 녹음 및 WAV 변환

```csharp
public class MicrophoneRecorder : MonoBehaviour
{
    private AudioClip recordedClip;

    public void StartRecording()
    {
        recordedClip = Microphone.Start(null, false, 10, 16000);
    }

    public void StopRecording()
    {
        Microphone.End(null);
        byte[] wavData = WavUtility.FromAudioClip(recordedClip);
        StartCoroutine(CheckPronunciation(wavData, "ㄱ"));
    }
}
```

---

## 주요 기능 시나리오

### VR 로그인 프로세스

1. Home 씬 진입
2. DeviceLoginManager 자동 실행
3. 서버에 디바이스 코드 요청
4. 화면에 4자리 코드 표시
5. 사용자가 모바일 앱에서 코드 입력
6. JWT 토큰 받아 저장
7. Lobby 씬으로 전환

### 학습 세션 흐름

1. 스테이지 선택 (Lobby)
2. 세션 시작 (StageSessionController)
3. 문제 출제 (StageQuestionController)
4. 사용자 발음 녹음
5. AI 서버 발음 체크
6. 피드백 표시
7. 세션 종료 및 통계 전송

---

## 디버깅

### XR Device Simulator 사용

1. Window → XR → XR Device Simulator
2. 조작법:
   - WASD: 이동
   - 마우스: 시선 회전
   - Space + 마우스: 컨트롤러 이동

### 로그 확인

**Unity 에디터:**
```csharp
Debug.Log("Normal log");
Debug.LogWarning("Warning");
Debug.LogError("Error");
```

**Android (Quest) 로그:**
```bash
adb logcat -s Unity
```

---

## 트러블슈팅

### Quest에서 앱이 실행되지 않음

**해결**:
1. Meta Quest 앱에서 개발자 모드 활성화
2. Quest 설정 → Unknown Sources 허용

### 마이크 권한 오류

**해결**:
```csharp
#if PLATFORM_ANDROID
using UnityEngine.Android;
if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
{
    Permission.RequestUserPermission(Permission.Microphone);
}
#endif
```

### API 연결 실패

**해결**:
1. 서버 URL 확인
2. 서버 CORS 설정 확인
3. HTTPS 인증서 확인

---

## 참고 자료

### 프로젝트 문서
- **메인 프로젝트**: [README.md](../README.md)
- **모바일 앱**: [reading_buddy_app/README.md](../reading_buddy_app/README.md)
- **AI 서버**: [ai/README.md](../ai/README.md)
- **Fine-tuning**: [Fine-tuning/README.md](../Fine-tuning/README.md)

### Unity 공식 문서
- [Unity XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.6/manual/index.html)
- [Meta Quest Development](https://developer.oculus.com/documentation/unity/)
- [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/index.html)

---

## 팀 구성

### Frontend (VR)

|  | 이름 | 역할 | 주요 담당 업무 | GitHub |
|:-:|:---:|:---:|:---|:---:|
| <img src="https://github.com/amy010510.png" width="60" height="60" /> | **이채연** | 프론트엔드 | VR 게임 로직, 스테이지 설계, AI 연동 | [@amy010510](https://github.com/amy010510) |
| <img src="https://github.com/jinnyujinchoi.png" width="60" height="60" /> | **최유진** | 프론트엔드 | UI/UX, VR 인터랙션, 애니메이션 | [@jinnyujinchoi](https://github.com/jinnyujinchoi) |

---

## 라이선스

교육 목적으로 제작된 프로젝트입니다.

---

## 문의

프로젝트 관련 문의는 GitLab 이슈를 통해 등록해주세요.

---

**Last Updated**: 2025-11-19
**Unity Version**: 2022.3.62f2
**Target Platform**: Meta Quest (Android)
