using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Text;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
using UnityEngine.XR;
using Stage.UI;
using QuestionDto = StageQuestionModels.QuestionDto;
using OptionDto = StageQuestionModels.OptionDto;

// Stage 1.1 진행 컨트롤러
// - GET: /api/train/set?stage=1.1.1&count=5
// - POST: /api/train/stage/start?stage=1.1&totalProblems=5 (헤더에 토큰 포함)
// - GET: /api/train/set?stage=1.1.1&count=5
// - POST: /api/train/check/voice?stageSessionId=&stage=&problemId= (multipart: audio=voice.wav, 헤더에 토큰 포함)
// - POST: /api/train/stage/complete?sessionId=... (헤더에 토큰 포함)
// 흐름(문항당):
//  1) 상단에 "문제 i/5" 표시, 중앙 이미지(imageUrl) 표시
//  흐름(문항당):
//  - [1.1.3] 앞에 떠오른 마법 그림을 잘 보고, 나랑 함께 주문을 외워보자!
//  - voiceUrl 재생
//  - [1.1.4] 이제 너 차례야, 주문을 들려줘!
//  - 사용자 음성 발신(POST) - 3초간 녹음
//  - [1.1.5] 우와~ 정말 멋지게 외웠는걸!
//  4) 정답이면 "최고야" 재생 후 다음 문항, 오답이면 "다시 한번 골라볼까?" 재생 후 재선택 대기
    public class Stage11Controller : MonoBehaviour
    {
        [Header("API 설정")]
        public string baseUrl = ""; // 빈 값이면 절대경로/상대경로 그대로 사용
        public string stage = "1.1.1";
        [Tooltip("stage/start, check/voice 등 2레벨 스테이지 파라미터가 필요한 요청에 사용됩니다. 비워두면 stage 값이 사용됩니다.")]
        public string stageTwoPart = "1.1";
        public int count = 5;
        [Tooltip("Authorization: Bearer {token}")]
        public string authToken = ""; // 필요 시 토큰
        [Header("세션")]
        [Tooltip("/api/train/stage/start 응답의 stageSessionId. 미설정 시 업로드 403 가능")]
        public string stageSessionId = "";

    [Header("옵션 라벨")]
    public OptionLabelMode optionLabelMode = OptionLabelMode.ValueThenUnicode;

    [Header("Fonts")]
    public Font uiFont;                  // UGUI Text용 폰트 (예: GmarketSansTTFMedium.ttf)
    public TMP_FontAsset tmpFont;        // TMP용 폰트 (예: GmarketSansTTFMedium SDF.asset)

    [Header("UI 참조")]
    public Text progressText;            // 상단 "문제 1/5"
    public Image mainImage;              // 중앙 큰 이미지
    public RectTransform optionsContainer; // 하단 옵션 버튼 부모
    public Button optionButtonPrefab;    // 동적 생성용 버튼 프리팹 (Text 자식 포함)

    [Header("Intro Tutorial")]
    public StageTutorialProfile tutorialProfile;
    public Sprite introTutorialImage;
    public List<IntroOption> introOptions = new List<IntroOption>();
    public IntroOptionCursor introOptionCursor;
    [Tooltip("튜토리얼 패널 연출용 컴포넌트 (선택)")]
    public PanelAnimator introTutorialPanelAnimator;
    [Tooltip("PanelAnimator가 없을 때 직접 제어할 패널 오브젝트")]
    public GameObject introTutorialPanel;
    [Header("Intro Tutorial Controls")]
    public bool requireTriggerAfterTutorial = true;
    [Range(0.05f, 1f)]
    public float tutorialTriggerThreshold = 0.6f;
    [Tooltip("에디터 테스트용 키 입력. XR 입력이 없을 때 이 키를 눌러도 튜토리얼이 끝납니다.")]
    public KeyCode tutorialFallbackKey = KeyCode.Space;
    [Tooltip("튜토리얼 클립 사이 대기 시간(초)")]
    [Min(0f)]
    public float tutorialClipGapSeconds = 0.9f;

    [Header("Guide Character (Level 1)")]
    [Tooltip("패널이 꺼져 있을 때 표시할 3D 캐릭터 오브젝트")]
    public GameObject guide3DCharacter;
    [Tooltip("패널을 켜기 직전에 캐릭터를 미리 숨길 선행 시간(초)")]
    [Min(0f)] public float guideHideLeadSeconds = 0.5f;
    [Tooltip("패널이 꺼진 직후 캐릭터를 다시 보이게 할 지")]
    public bool showGuideWhenPanelOff = true;
    [Tooltip("패널 OFF 직후 캐릭터 표시까지 추가 지연(초)")]
    [Min(0f)] public float guideShowDelayAfterPanelOff = 0f;

    [Header("Mic Indicator")]
    [Tooltip("[1.1.4] 종료 직후부터 녹음 3초 동안 표시될 마이크 아이콘 오브젝트")]
    public GameObject micIndicator;

    [Header("오디오 재생")]
    public AudioSource audioSource;      // 안내/피드백/효과음 재생용
    // 시작/전환 효과음
    public AudioClip sfxStart;           // (시작 효과음)
    public AudioClip sfxNext;            // (다음 문제로 넘어가는 효과음)

    // 도입 대사
    [HideInInspector] public AudioClip introClip1;         // [1.1.1] 안녕~ 꼬마 마법사!
    [HideInInspector] public AudioClip introClip2;         // [1.1.2] 지금부터 ‘마법 주문’ 수업을 시작할 거야!
    [HideInInspector] public AudioClip introClip3;
    [HideInInspector] public AudioClip introClip4;
    [HideInInspector] public AudioClip introClip5;
    [HideInInspector] public AudioClip introClip6;
    [HideInInspector] public AudioClip introClip7;
    [HideInInspector] public AudioClip introClip8;
    [HideInInspector] public AudioClip introClip9;
    [HideInInspector] public AudioClip introClip10;
    [HideInInspector] public AudioClip introClip11;
    [HideInInspector] public AudioClip introDemoClip1;
    [HideInInspector] public AudioClip introDemoClip2;

    // 각 문제 흐름 대사
    public AudioClip clipSeeAndChant;    // [1.1.3] 앞에 떠오른 마법 그림을 잘 보고...
    public AudioClip clipYourTurn;       // [1.1.4] 이제 너 차례야, 주문을 들려줘!
    public AudioClip clipGreat;          // [1.1.5] 우와~ 정말 멋지게 외웠는걸!
    public AudioClip clipChoose;         // [1.1.6] 두 개 중 어떤 소리였는지 맞춰볼래?
    [Tooltip("[1.1.3]과 voiceUrl 사이 대기 시간(초)")]
    [Min(0f)]
    public float questionVoiceDelaySeconds = 0.9f;

    // 정답/오답 피드백
    public AudioClip sfxCorrectClip;     // [1.1.7.1] 완벽해!
    public AudioClip sfxWrongClip;       // [1.1.7.2] 아이쿠! 다시 한 번 집중해 볼까?

    [Header("추가 학습 시나리오")]
    public AudioClip clipRemedialNeedPractice;          // [1.1.8.1] 부족한 발음 안내
    public AudioClip clipRemedialPracticeIntro;         // [1.1.8.1.1] 연습 제안
    public AudioClip clipRemedialFirstEncourage;        // [1.1.8.1.2] 첫 번째 격려
    public AudioClip clipRemedialSecondEncourage;       // [1.1.8.1.3] 두 번째 격려
    public AudioClip clipRemedialPerfect;               // [1.1.8.2] 완벽 안내
    public AudioClip clipRemedialNextLesson;            // [1.0.1] 다음 수업 안내
    [Tooltip("격려 음성 이후 대기 시간(초). 아이의 응답을 기다리는 느낌을 줍니다.")]
    public float remedialEncouragePauseSeconds = 3f;

    [Header("추가 학습 리소스")]
    [Tooltip("voiceResult 항목과 매칭될 보충 학습 리소스(이미지/오디오). key는 stage/complete 응답값과 비교합니다.")]
    public List<RemedialPracticeResource> remedialResources = new List<RemedialPracticeResource>();

    [Header("마이크 설정")]
    public int recordSeconds = 3;        // 발음 녹음 시간
    public int recordSampleRate = 44100; // 발음 샘플레이트
        [Range(0,5)] public int maxWrongAttempts = 2; // 오답 허용 횟수 (기본 2)

        [Header("가이드 이미지(도입/전환 연출)")]
        public RectTransform guideImage;       // 중앙 안내 이미지(선택)
        public Vector2 guideStartSize = new Vector2(1500, 1500);
        public Vector2 guideEndSize   = new Vector2(800, 800);
        public float guideMoveDuration = 1.5f; // sfxNext가 재생되는 동안 살며시 이동/축소
        public bool guideMoveOnlyOnce  = true; // 최초 1회만 이동할지
        [Tooltip("문제 전환 시 가이드 다시 이동 여부")]
        public bool enableGuideMoveBetweenQuestions = false;
        private bool _guideMoved;
        private Coroutine _guideMoveCo;
        private bool _guideLocked;
        private Vector2 _guideFinalPos;
        private Vector2 _guideFinalSize;
        private StageSessionController _sessionController;
        private readonly StageQuestionController<QuestionDto> _questionController = new StageQuestionController<QuestionDto>();
        private StageTutorialController _tutorialController;
        private StageTutorialDependencies _tutorialDependencies;
        private StageAudioController _audioController;
        private StageAudioDependencies _audioDependencies;
        private StageSupplementController _supplementController;
        private StageSupplementDependencies _supplementDependencies;
        private int _currentProblemNumber;
        [Header("Auto Layout (겹침 방지)")]
        [Tooltip("실행 시 메인 이미지/옵션 영역을 자동 배치합니다.")]
        public bool applyAutoLayout = true;
        [Tooltip("옵션 영역 높이(px)")]
        public float optionsHeight = 220f;
        [Tooltip("옵션 영역 하단 여백(px)")]
        public float optionsBottomMargin = 40f;
        [Tooltip("이미지 좌우 여백(px)")]
        public float imageSideMargin = 80f;
        [Tooltip("이미지 상단/하단 여백(px)")]
        public float imageVerticalMargin = 40f;
        [Tooltip("메인 이미지 고정 크기(px)")]
        public Vector2 imageFixedSize = new Vector2(1500f, 1500f);
        [Tooltip("옵션 버튼 권장 크기(px)")]
        public Vector2 optionButtonPreferredSize = new Vector2(1200f, 600f);

        [Header("End Modal Buttons")]
        [Tooltip("끝 모달 '다시 학습하기' 버튼 프리팹")]
        public Button againButtonPrefab;
        [Tooltip("끝 모달 '로비로 나가기' 버튼 프리팹")]
        public Button lobbyButtonPrefab;
        [Tooltip("끝 모달 버튼 크기(px). 0이면 옵션 버튼 크기 사용")]
        public Vector2 endModalButtonSize = new Vector2(600f, 300f);

        [Header("Options Layout")]
        [Tooltip("옵션 버튼 간 간격(px)")]
        public float optionSpacing = 20f;
        [Tooltip("옵션 컨테이너 패딩(px)")]
        public int optionsPaddingLeft = 20;
        public int optionsPaddingTop = 20;
        public int optionsPaddingRight = 20;
        public int optionsPaddingBottom = 20;

        [Header("개발용 우회")]
        [Tooltip("/api/train/stage/start 요청을 건너뛰고 문제 GET만 진행합니다.")]
        public bool bypassStartRequest = true;
        [Tooltip("음성 녹음 및 /api/train/check/voice 업로드를 건너뜁니다.")]
        public bool bypassVoiceUpload = true;

        [Header("진단/로그")]
        [Tooltip("수신한 문제 전체를 상세 로그로 출력합니다.")]
        public bool logQuestionsVerbose = true;
        [Tooltip("튜토리얼 등 세부 진행 로그를 출력합니다.")]
        public bool verboseLogging = false;
        [Tooltip("이미지 로드 실패 시 자리표시 이미지를 중앙에 표시합니다.")]
        public bool showPlaceholderOnImageFail = true;

    [System.Serializable]
    public enum OptionLabelMode { UnicodeOnly, ValueOnly, UnicodeThenValue, ValueThenUnicode }

    [Serializable]
    public class IntroOption : StageTutorialController.IntroOption { }

    [Serializable]
    public class IntroOptionCursor : StageTutorialController.IntroOptionCursor { }

    [Serializable]
    public class QuestionListResponse
    {
        public bool success;
        public string message;
        public List<QuestionDto> data;
    }

    private void Start()
    {
        StartCoroutine(InitializeWithAuth());
    }

    /// <summary>
    /// AuthManager 연동 초기화
    /// </summary>
    private IEnumerator InitializeWithAuth()
    {
        Debug.Log("[Stage11] Waiting for AuthManager...");

        // AuthManager가 준비될 때까지 대기 (최대 5초)
        float timeout = 5f;
        float elapsed = 0f;
        while (AuthManager.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (AuthManager.Instance == null)
        {
            Debug.LogError("[Stage11] ⚠️ AuthManager.Instance is null after timeout! Returning to Home.");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(SceneId.Home);
            }
            yield break;
        }

        Debug.Log("[Stage11] AuthManager found!");

        // 로그인 상태 확인
        if (!AuthManager.Instance.IsLoggedIn())
        {
            Debug.LogError("[Stage11] ⚠️ User is not logged in! Returning to Home.");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(SceneId.Home);
            }
            yield break;
        }

        Debug.Log("[Stage11] User is logged in!");

        // baseUrl 자동 해석 (ENV > Resources > Inspector)
        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);

        // AuthManager에서 토큰 가져오기 (우선순위)
        // EnvConfig는 fallback으로만 사용
        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
        {
            authToken = AuthManager.Instance.GetAccessToken();
            Debug.Log("[Stage11] ✓ Access token retrieved from AuthManager");
        }
        else
        {
            authToken = EnvConfig.ResolveAuthToken(authToken);
            Debug.Log("[Stage11] Using authToken from EnvConfig (fallback)");
        }

        if (applyAutoLayout)
            TryApplyAutoLayout();
        // 가이드 시작 크기는 최초 1회만 적용
        if (guideImage && guideStartSize.sqrMagnitude > 0)
            guideImage.sizeDelta = guideStartSize;
        // 초입에는 메인 이미지와 옵션 영역을 숨깁니다.
        if (mainImage)
        {
            mainImage.enabled = false;
            mainImage.sprite = null;
        }
        if (optionsContainer)
        {
            optionsContainer.gameObject.SetActive(false);
        }
        ConfigureAudioController();
        ConfigureTutorialController();
        ConfigureSupplementController();
        _tutorialController?.PrepareForStageStart();
        if (micIndicator)
        {
            micIndicator.SetActive(false);
        }
        StartCoroutine(RunStage());
    }

    private StageSessionController GetSessionController()
    {
        if (_sessionController == null)
            _sessionController = new StageSessionController();

        _sessionController.Configure(baseUrl, authToken);
        _sessionController.Log = Debug.Log;
        _sessionController.LogWarning = Debug.LogWarning;
        _sessionController.LogError = Debug.LogError;
        return _sessionController;
    }

    private void ConfigureAudioController()
    {
        if (_audioController == null)
            _audioController = new StageAudioController();

        if (_audioDependencies == null)
            _audioDependencies = new StageAudioDependencies();

        _audioDependencies.AudioSource = audioSource;
        _audioDependencies.Log = message => Debug.Log(message);
        _audioDependencies.LogWarning = message => Debug.LogWarning(message);

        _audioController.Initialize(_audioDependencies);
    }

    private void ConfigureTutorialController()
    {
        if (_tutorialController == null)
            _tutorialController = new StageTutorialController();

        if (_tutorialDependencies == null)
        {
            _tutorialDependencies = new StageTutorialDependencies
            {
                PlayClip = PlayClip,
                StartCoroutine = routine => StartCoroutine(routine),
                StopCoroutine = routine =>
                {
                    if (routine != null)
                        StopCoroutine(routine);
                },
                ProgressText = progressText,
                EnsureProgressText = EnsureProgressText,
                MainImage = mainImage,
                OptionsContainer = optionsContainer,
                OptionButtonPrefab = optionButtonPrefab,
                CorrectSfx = sfxCorrectClip,
                MoveCursorSmooth = (cursor, target, seconds, curve) => MoveCursorSmooth(cursor, target, seconds, curve),
                PulseOption = (rect, scale, duration, loops) => PulseOption(rect, scale, duration, loops),
                Log = message => Debug.Log(message),
                LogWarning = message => Debug.LogWarning(message),
                VerboseLogging = verboseLogging
            };
        }
        else
        {
            _tutorialDependencies.PlayClip = PlayClip;
            _tutorialDependencies.StartCoroutine = routine => StartCoroutine(routine);
            _tutorialDependencies.StopCoroutine = routine =>
            {
                if (routine != null)
                    StopCoroutine(routine);
            };
            _tutorialDependencies.ProgressText = progressText;
            _tutorialDependencies.EnsureProgressText = EnsureProgressText;
            _tutorialDependencies.MainImage = mainImage;
            _tutorialDependencies.OptionsContainer = optionsContainer;
            _tutorialDependencies.OptionButtonPrefab = optionButtonPrefab;
            _tutorialDependencies.CorrectSfx = sfxCorrectClip;
            _tutorialDependencies.MoveCursorSmooth = (cursor, target, seconds, curve) => MoveCursorSmooth(cursor, target, seconds, curve);
            _tutorialDependencies.PulseOption = (rect, scale, duration, loops) => PulseOption(rect, scale, duration, loops);
            _tutorialDependencies.Log = message => Debug.Log(message);
            _tutorialDependencies.LogWarning = message => Debug.LogWarning(message);
            _tutorialDependencies.VerboseLogging = verboseLogging;
        }

        if (tutorialProfile != null)
        {
            _tutorialController.ApplyProfile(tutorialProfile);
        }
        else
        {
            _tutorialController.introTutorialImage = introTutorialImage;
            if (introOptions != null)
                _tutorialController.introOptions = introOptions.ConvertAll<StageTutorialController.IntroOption>(io => (StageTutorialController.IntroOption)io);
            else
                _tutorialController.introOptions = new List<StageTutorialController.IntroOption>();
            _tutorialController.guideHideLeadSeconds = guideHideLeadSeconds;
            _tutorialController.showGuideWhenPanelOff = showGuideWhenPanelOff;
            _tutorialController.guideShowDelayAfterPanelOff = guideShowDelayAfterPanelOff;
            _tutorialController.requireTriggerAfterTutorial = requireTriggerAfterTutorial;
            _tutorialController.tutorialTriggerThreshold = tutorialTriggerThreshold;
            _tutorialController.tutorialFallbackKey = tutorialFallbackKey;
            _tutorialController.tutorialClipGapSeconds = tutorialClipGapSeconds;
            _tutorialController.introClip1 = introClip1;
            _tutorialController.introClip2 = introClip2;
            _tutorialController.introClip3 = introClip3;
            _tutorialController.introClip4 = introClip4;
            _tutorialController.introClip5 = introClip5;
            _tutorialController.introClip6 = introClip6;
            _tutorialController.introClip7 = introClip7;
            _tutorialController.introClip8 = introClip8;
            _tutorialController.introClip9 = introClip9;
            _tutorialController.introClip10 = introClip10;
            _tutorialController.introClip11 = introClip11;
            _tutorialController.introDemoClip1 = introDemoClip1;
            _tutorialController.introDemoClip2 = introDemoClip2;
        }

        _tutorialController.introOptionCursor = introOptionCursor;
        _tutorialController.introTutorialPanelAnimator = introTutorialPanelAnimator;
        _tutorialController.introTutorialPanel = introTutorialPanel;
        _tutorialController.guide3DCharacter = guide3DCharacter;

        _tutorialController.Initialize(_tutorialDependencies);

        if (_tutorialDependencies.OptionButtonPrefab != null && optionButtonPrefab == null)
            optionButtonPrefab = _tutorialDependencies.OptionButtonPrefab;
    }

    private void ConfigureSupplementController()
    {
        if (_supplementController == null)
            _supplementController = new StageSupplementController();

        if (_supplementDependencies == null)
        {
            _supplementDependencies = new StageSupplementDependencies();
        }

        _supplementDependencies.QuestionController = _questionController;
        _supplementDependencies.MainImage = mainImage;
        _supplementDependencies.ProgressText = progressText;
        _supplementDependencies.PlayClip = clip => PlayClip(clip);
        _supplementDependencies.PlayVoiceUrl = url => PlayVoiceUrl(url);
        _supplementDependencies.LoadAndShowImage = url => LoadAndShowImage(url);
        _supplementDependencies.Log = message => Debug.Log(message);
        _supplementDependencies.LogWarning = message => Debug.LogWarning(message);
        _supplementDependencies.VerboseLogging = verboseLogging;

        _supplementController.Initialize(_supplementDependencies);
        _supplementController.remedialResources = remedialResources ?? new List<RemedialPracticeResource>();
        _supplementController.clipRemedialNeedPractice = clipRemedialNeedPractice;
        _supplementController.clipRemedialPracticeIntro = clipRemedialPracticeIntro;
        _supplementController.clipRemedialFirstEncourage = clipRemedialFirstEncourage;
        _supplementController.clipRemedialSecondEncourage = clipRemedialSecondEncourage;
        _supplementController.clipRemedialPerfect = clipRemedialPerfect;
        _supplementController.clipRemedialNextLesson = clipRemedialNextLesson;
        _supplementController.remedialEncouragePauseSeconds = remedialEncouragePauseSeconds;
    }

    private Text EnsureProgressText()
    {
        if (progressText) return progressText;
        var go = GameObject.Find("ProgressText");
        if (go)
        {
            var t = go.GetComponent<Text>();
            if (t) { progressText = t; return t; }
        }
        var canvas = FindObjectOfType<Canvas>();
        if (!canvas) return null;
        var obj = new GameObject("ProgressText", typeof(RectTransform), typeof(Text));
        obj.layer = canvas.gameObject.layer;
        var rt = obj.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(600f, 120f);
        var text = obj.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 120;
        text.color = Color.white;
        text.font = uiFont ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        progressText = text;
        return progressText;
    }

    private void TryApplyAutoLayout()
    {
        // 옵션 컨테이너: 화면 하단에 가로로 늘려 배치
        if (optionsContainer)
        {
            var rt = optionsContainer;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, optionsBottomMargin);
            // 버튼 높이에 맞춰 컨테이너 높이 보정
            float minNeeded = optionButtonPreferredSize.y + 20f; // 상단 여백 여지
            float h = Mathf.Max(optionsHeight, minNeeded);
            rt.sizeDelta = new Vector2(0f, h);

            // 컨테이너에 레이아웃 그룹이 있으면 패딩/스페이싱 적용
            ApplyOptionsLayoutConfig();
        }

        // 메인 이미지: 하단 옵션 영역을 피해 위쪽/중앙 영역에 배치(가로 스트레치)
        if (mainImage)
        {
            var mrt = mainImage.rectTransform;
            var parent = mrt.parent as RectTransform;
            float bottomReserved = optionsBottomMargin + Mathf.Max(optionsHeight, optionButtonPreferredSize.y + 20f) + imageVerticalMargin;
            // 고정 크기 + 중앙 앵커로 배치, 하단 여유만큼 위로 올림
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.pivot     = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = imageFixedSize;
            float yOffset = bottomReserved * 0.5f;
            mrt.anchoredPosition = new Vector2(0f, yOffset);

            // 이미지 클릭 방해 방지(선택)
            mainImage.raycastTarget = false;
            mainImage.preserveAspect = true;
        }
    }

    private IEnumerator RunStage()
    {
        ConfigureTutorialController();
        _tutorialController?.ResetAfterStageRestart();

        // 새 실행 시작 시 상태 초기화
        _guideMoved = false;
        if (_guideMoveCo != null) { StopCoroutine(_guideMoveCo); _guideMoveCo = null; }
        if (optionsContainer) optionsContainer.gameObject.SetActive(false);
        if (mainImage)
        {
            mainImage.enabled = false;
            mainImage.sprite = null;
        }
        // 0) 시작 효과음
        ConfigureSupplementController();
        _supplementController?.Clear();
        _questionController.Clear();
        yield return PlayClip(sfxStart);

        // 0-1) 도입 대사 (가이드 이미지는 고정, 이동은 sfxNext 타이밍에 수행)
        if (_tutorialController != null)
        {
            yield return _tutorialController.RunIntroSequence();
            yield return _tutorialController.RunIntroTutorial();
        }
        else
        {
            if (introClip1) yield return PlayClip(introClip1);
            if (introClip2) yield return PlayClip(introClip2);
            if (introClip3) yield return PlayClip(introClip3);
            if (introClip4) yield return PlayClip(introClip4);
        }
        if (guideImage && _guideMoveCo == null && (!_guideMoved || !guideMoveOnlyOnce))
        {
            Debug.Log("[Stage11] Guide move: trigger after intro");
            _guideMoveCo = StartCoroutine(MoveGuideAndScaleOverTime(guideMoveDuration));
            _guideMoved = true;
        }

        var sessionController = GetSessionController();

        // 0-2) 세션 시작 호출로 stageSessionId 확보 (테스트 시 우회 가능)
        if (!bypassStartRequest && string.IsNullOrWhiteSpace(stageSessionId))
        {
            string stageParamSource = string.IsNullOrWhiteSpace(stageTwoPart) ? stage : stageTwoPart;
            StageSessionController.StageStartResult startResult = null;
            yield return sessionController.StartStageSession(stageParamSource, count, r => startResult = r);
            if (startResult != null && startResult.Success && !string.IsNullOrWhiteSpace(startResult.StageSessionId))
            {
                stageSessionId = startResult.StageSessionId;
            }
            else if (string.IsNullOrWhiteSpace(stageSessionId))
            {
                Debug.LogWarning("[Stage11] stageSessionId 발급 실패. bypassStartRequest=true 이므로 계속 진행합니다.");
            }
        }

        // 문제 요청
        StageSessionController.QuestionSetResult questionResult = null;
        yield return sessionController.FetchQuestionSet(stage, count, stageSessionId, r => questionResult = r);
        if (questionResult == null || !questionResult.Success)
        {
            Debug.LogError($"[Stage11] 문제 요청 실패: 응답 코드={questionResult?.ResponseCode}\nRaw={questionResult?.RawBody}");
            yield break;
        }

        var json = questionResult.RawBody;
        var questions = ExtractQuestions(json);
        if (questions == null || questions.Count == 0)
        {
            Debug.LogError($"[Stage11] 응답 파싱 실패 또는 데이터 없음\nRaw={json}");
            yield break;
        }
        else
        {
            Debug.Log($"[Stage11] 문제 수신: {questions.Count}개");
            if (logQuestionsVerbose)
            {
                for (int qi = 0; qi < questions.Count; qi++)
                {
                    var qd = questions[qi];
                    string opts = (qd.options != null) ? string.Join(", ", qd.options.Select(o => o.value)) : "(no options)";
                    Debug.Log($"[Stage11] Q{qi + 1}: id={qd.id}, qid={qd.questionId}, phonemeId={qd.phonemeId}, value={qd.value}, imageUrl={qd.imageUrl}, voiceUrl={qd.voiceUrl}, options=[{opts}]");
                }
            }
        }

        _questionController.SetQuestions(questions);

        int totalQuestions = _questionController.Count;
        for (int i = 0; i < totalQuestions; i++)
        {
            int questionNumber = i + 1;
            _questionController.SetCurrentQuestionNumber(questionNumber);
            var q = _questionController.GetQuestionByNumber(questionNumber);
            yield return RunOneQuestion(questionNumber, totalQuestions, q);
            // 다음 문제로 넘어가는 효과음 (마지막 문제 제외)
            if (i < totalQuestions - 1)
            {
                // sfxNext 재생과 동시에 가이드 이미지 이동/축소(최초 1회)
                if (enableGuideMoveBetweenQuestions && guideImage && _guideMoveCo == null && (!_guideMoved || !guideMoveOnlyOnce))
                {
                    Debug.Log("[Stage11] Guide move: trigger between questions");
                    _guideMoveCo = StartCoroutine(MoveGuideAndScaleOverTime(guideMoveDuration));
                    _guideMoved = true;
                }
                yield return PlayClip(sfxNext);
            }
        }

        // 세션 완료 보고 (best-effort)
        _supplementController?.Clear();
        StageSessionController.StageCompleteResult completeResult = null;
        yield return sessionController.CompleteStageSession(stageSessionId, r => completeResult = r);
        if (completeResult != null)
        {
            if (!string.IsNullOrWhiteSpace(completeResult.StageSessionId))
                stageSessionId = completeResult.StageSessionId;
            if (completeResult.VoiceResultTokens.Count > 0)
            {
                _supplementController?.SetRemedialTokens(completeResult.VoiceResultTokens);
            }
        }
        yield return RunRemedialSequence();
        ShowEndModal();
    }

    // JsonUtility는 루트에 배열을 직접 파싱하지 못하므로 래퍼 클래스로 우회
    [Serializable]
    private class QuestionListWrapper
    {
        public bool success;
        public string message;
        public List<QuestionDto> data;
    }

    // /api/train/set 이 data 아래에 questions 배열을 둘 수 있는 경우를 대비한 보조 모델
    [Serializable]
    private class QuestionSet
    {
        public List<QuestionDto> questions;
        public List<QuestionDto> problems; // 서버가 problems 키를 사용하는 경우 대응
    }

    [Serializable]
    private class QuestionSetResponse
    {
        public bool success;
        public string message;
        public QuestionSet data;
    }

    // 일부 서버가 data를 문자열(JSON)로 감싸서 반환하는 경우 대응
    [Serializable]
    private class QuestionStringDataWrapper
    {
        public bool success;
        public string message;
        public string data; // JSON string
    }

    private string WrapJson(string raw)
    {
        // 서버가 이미 { success, message, data:[...] } 형태라면 그대로 사용
        // 아닌 경우를 대비한 방어 로직은 생략
        return raw;
    }

    // userId 관련 토큰 파싱 로직 제거됨

    // 서버 응답 형태가 몇 가지 변형일 수 있으므로 유연하게 파싱
    private List<QuestionDto> ExtractQuestions(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // 1) 기본 형태: { success, message, data: [ ... ] }
        try
        {
            var list = JsonUtility.FromJson<QuestionListWrapper>(WrapJson(raw));
            if (list != null && list.data != null && list.data.Count > 0)
                return list.data;
        }
        catch { }

        // 2) set 형태: { success, message, data: { questions: [ ... ] } } 또는 { problems: [ ... ] }
        try
        {
            var set = JsonUtility.FromJson<QuestionSetResponse>(raw);
            if (set != null && set.data != null)
            {
                if (set.data.questions != null && set.data.questions.Count > 0)
                    return set.data.questions;
                if (set.data.problems != null && set.data.problems.Count > 0)
                    return set.data.problems;
            }
        }
        catch { }

        // 2-b) data가 문자열(JSON)로 들어온 경우 처리
        try
        {
            var strWrap = JsonUtility.FromJson<QuestionStringDataWrapper>(raw);
            if (strWrap != null && !string.IsNullOrWhiteSpace(strWrap.data))
            {
                var inner = strWrap.data.Trim();
                // 문자열 내의 이스케이프가 제거되지 않았다면 그대로 시도
                if (inner.StartsWith("["))
                {
                    var wrapped = $"{{\"success\":true,\"message\":\"\",\"data\":{inner}}}";
                    var list = JsonUtility.FromJson<QuestionListWrapper>(wrapped);
                    if (list != null && list.data != null && list.data.Count > 0)
                        return list.data;
                }
                else if (inner.StartsWith("{"))
                {
                    var set2 = JsonUtility.FromJson<QuestionSetResponse>("{\"success\":true,\"message\":\"\",\"data\":" + inner + "}");
                    if (set2 != null && set2.data != null)
                    {
                        if (set2.data.questions != null && set2.data.questions.Count > 0)
                            return set2.data.questions;
                        if (set2.data.problems != null && set2.data.problems.Count > 0)
                            return set2.data.problems;
                    }
                }
            }
        }
        catch { }

        // 3) 루트가 배열인 경우: [ ... ]
        try
        {
            var trimmed = raw.TrimStart();
            if (trimmed.StartsWith("["))
            {
                var wrapped = $"{{\"success\":true,\"message\":\"\",\"data\":{trimmed}}}";
                var list = JsonUtility.FromJson<QuestionListWrapper>(wrapped);
                if (list != null && list.data != null && list.data.Count > 0)
                    return list.data;
            }
        }
        catch { }

        return null;
    }

    private IEnumerator RunOneQuestion(int index, int total, QuestionDto q)
    {
        // 현재 문제 번호 저장 (attempt 로깅용)
        _currentProblemNumber = index;
        // 진행도 표시
        if (progressText) progressText.text = $"문제 {index}/{total}";

        // 이미지 로드 및 표시
        var pt = EnsureProgressText();
        if (pt != null) pt.text = $"{index} / {total}";
        yield return LoadAndShowImage(q.imageUrl);

        // 1) [1.1.3] 안내 대사
        yield return PlayClip(clipSeeAndChant);

        if (questionVoiceDelaySeconds > 0f)
            yield return new WaitForSeconds(questionVoiceDelaySeconds);

        // voiceUrl 재생
        yield return PlayVoiceUrl(q.voiceUrl);

        // 2) [1.1.4] 이제 너 차례야 → 녹음 업로드
        yield return PlayClip(clipYourTurn);
        if (!bypassVoiceUpload)
        {
            if (micIndicator) micIndicator.SetActive(true);
            yield return RecordAndUpload(q);
            if (micIndicator) micIndicator.SetActive(false);
        }
        else
        {
            if (micIndicator) micIndicator.SetActive(true);
            yield return new WaitForSeconds(recordSeconds);
            if (micIndicator) micIndicator.SetActive(false);
        }

        // 3) [1.1.5] 칭찬 대사
        yield return PlayClip(clipGreat);

        // 4) [1.1.6] 선택 유도 대사 → 옵션 선택
        yield return PlayClip(clipChoose);
        yield return ShowOptionsUntilCorrect(q);
    }

    private IEnumerator LoadAndShowImage(string imageUrl)
    {
        if (mainImage != null)
        {
            // 로드 전에는 보이지 않게
            mainImage.enabled = false;
            mainImage.sprite = null;
        }

        if (string.IsNullOrEmpty(imageUrl) || mainImage == null)
            yield break;

        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                var body = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogWarning($"[Stage11] 이미지 로드 실패: {req.error} (code={req.responseCode})\nURL={imageUrl}\nBody={body}");
                if (showPlaceholderOnImageFail && mainImage != null)
                {
                    var texPh = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    var col = new Color(0.2f, 0.6f, 0.9f, 0.25f);
                    var arr = new Color[64 * 64];
                    for (int i = 0; i < arr.Length; i++) arr[i] = col;
                    texPh.SetPixels(arr);
                    texPh.Apply();
                    var spr = Sprite.Create(texPh, new Rect(0, 0, texPh.width, texPh.height), new Vector2(0.5f, 0.5f));
                    mainImage.sprite = spr;
                    mainImage.preserveAspect = true;
                    mainImage.enabled = true;
                    Debug.Log("[Stage11] 자리표시 이미지 표시 (로드 실패)");
                }
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            mainImage.sprite = sprite;
            mainImage.preserveAspect = true;
            mainImage.enabled = true; // 로드 후 표시
            Debug.Log($"[Stage11] 이미지 로드 OK: {imageUrl} ({tex.width}x{tex.height})");
        }
    }

    private IEnumerator PlayClip(AudioClip clip)
    {
        if (_audioController == null)
            yield break;

        yield return _audioController.PlayClip(clip);
    }

    private IEnumerator PlayVoiceUrl(string voiceUrl)
    {
        if (_audioController == null)
            yield break;

        yield return _audioController.PlayVoiceUrl(voiceUrl);
    }

    private void ApplyOptionsLayoutConfig()
    {
        if (!optionsContainer) return;
        var h = optionsContainer.GetComponent<HorizontalLayoutGroup>();
        var v = optionsContainer.GetComponent<VerticalLayoutGroup>();
        var g = optionsContainer.GetComponent<GridLayoutGroup>();
        var padding = new RectOffset(optionsPaddingLeft, optionsPaddingRight, optionsPaddingTop, optionsPaddingBottom);
        if (h)
        {
            h.padding = padding;
            h.spacing = optionSpacing;
        }
        if (v)
        {
            v.padding = padding;
            v.spacing = optionSpacing;
        }
        if (g)
        {
            g.padding = padding;
            g.spacing = new Vector2(optionSpacing, optionSpacing);
        }
    }

    // URL의 경로 부분에 포함된 '+'를 '%2B'로 치환한다.
    // 쿼리스트링(서명 등)은 변경하지 않도록 주의.
    private static string NormalizeField(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim();
        try
        {
            s = Regex.Replace(s, @"\\u([0-9A-Fa-f]{4})", m =>
            {
                int code = Convert.ToInt32(m.Groups[1].Value, 16);
                return char.ConvertFromUtf32(code);
            });
            s = Regex.Replace(s, @"(?i)U\+([0-9A-Fa-f]{4,6})", m =>
            {
                int code = Convert.ToInt32(m.Groups[1].Value, 16);
                return char.ConvertFromUtf32(code);
            });
        }
        catch { }
        return s;
    }

    private static string NormalizeForCompare(string s)
    {
        s = NormalizeField(s);
        if (string.IsNullOrEmpty(s)) return string.Empty;
        try { s = s.Normalize(NormalizationForm.FormKC).Trim(); } catch { }
        return s;
    }

    private string ComposeOptionLabel(OptionDto opt)
    {
        string uni = NormalizeField(opt != null ? (opt.unicode ?? string.Empty) : string.Empty);
        string val = NormalizeField(opt != null ? (opt.value ?? string.Empty) : string.Empty);
        switch (optionLabelMode)
        {
            case OptionLabelMode.UnicodeOnly:
                return string.IsNullOrEmpty(uni) ? val : uni;
            case OptionLabelMode.ValueOnly:
                return string.IsNullOrEmpty(val) ? uni : val;
            case OptionLabelMode.UnicodeThenValue:
                return string.IsNullOrEmpty(uni) ? val : (string.IsNullOrEmpty(val) ? uni : ($"{uni} {val}"));
            case OptionLabelMode.ValueThenUnicode:
            default:
                return string.IsNullOrEmpty(val) ? uni : (string.IsNullOrEmpty(uni) ? val : ($"{val} {uni}"));
        }
    }

    private IEnumerator RecordAndUpload(QuestionDto q)
    {
        // 마이크 녹음
        var clip = StartMic(recordSeconds, recordSampleRate);
        yield return new WaitForSeconds(recordSeconds);
        var wav = WavUtility.FromAudioClip(clip);

        // 업로드 (Swagger)
        // POST /api/train/check/voice?stageSessionId=&stage=&problemNumber=
        if (string.IsNullOrWhiteSpace(stageSessionId))
        {
            Debug.LogWarning("[Stage11] stageSessionId가 비어 있습니다. 업로드 403이 발생할 수 있습니다. /api/train/stage/start 호출로 stageSessionId를 발급받으세요.");
        }
        // stage는 서버 요구 사항에 맞춰 전체(예: 1.1.1)로 전송
        string stageForUpload = GetStageForVoiceUpload(q);
        int problemNumber = Mathf.Max(1, _currentProblemNumber);
        string answerValue = ResolveAnswerValue(q);
        var sessionController = GetSessionController();
        Debug.Log($"[Stage11] check/voice request (answer={answerValue}, bytes={wav?.Length ?? 0})");
        yield return sessionController.CheckVoice(
            stageSessionId,
            stageForUpload,
            problemNumber,
            answerValue,
            wav,
            null);
    }

    private AudioClip StartMic(int seconds, int sampleRate)
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Stage11] 마이크 장치가 없습니다.");
            return null;
        }
        var clip = Microphone.Start(null, false, seconds, sampleRate);
        return clip;
    }

    private IEnumerator ShowOptionsUntilCorrect(QuestionDto q)
    {
        // 옵션 UI 구성
        if (optionsContainer == null)
        {
            Debug.LogError("[Stage11] optionsContainer가 연결되지 않았습니다.");
            yield break;
        }
        if (optionButtonPrefab == null)
        {
            // Resources에서 기본 프리팹 시도 로드
            var loaded = Resources.Load<Button>("UI/OptionButton");
            if (loaded != null)
            {
                optionButtonPrefab = loaded;
            }
            else
            {
                Debug.LogError("[Stage11] optionButtonPrefab이 연결되지 않았고, Resources/UI/OptionButton.prefab 로드 실패.");
                yield break;
            }
        }
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);
        // Show options container only during selection phase
        optionsContainer.gameObject.SetActive(true);
        optionsContainer.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);
        optionsContainer.SetAsLastSibling();

        bool answered = false;
        bool correct = false;
        int wrongCount = 0;
        int attemptCount = 0;
        OptionDto lastSelected = null;

        // 정답 값(표시/로깅용): phonemeId와 일치하는 옵션의 value를 우선 사용, 없으면 q.value
        string correctPhonemeValue = null;
        if (q != null && q.options != null)
        {
            var match = q.options.FirstOrDefault(o => o.id == q.phonemeId);
            if (match != null) correctPhonemeValue = NormalizeField(match.value);
        }
        if (string.IsNullOrEmpty(correctPhonemeValue)) correctPhonemeValue = NormalizeField(q.value);

        var sessionController = GetSessionController();

        void SetupOne(OptionDto opt)
        {
            var btn = Instantiate(optionButtonPrefab, optionsContainer);
            var text = btn.GetComponentInChildren<Text>();
            var tmp  = btn.GetComponentInChildren<TMP_Text>();
            string label = ComposeOptionLabel(opt);
            if (text)
            {
                text.text = label;
                if (uiFont) text.font = uiFont;
            }
            else if (tmp)
            {
                tmp.text = label;
                if (tmpFont) tmp.font = tmpFont;
                try { tmp.fontStyle &= ~FontStyles.Underline; } catch { }
            }
            // 버튼 크기 강제 설정 (LayoutElement와 RectTransform 동시 적용)
            var rt = btn.GetComponent<RectTransform>();
            if (rt) rt.sizeDelta = optionButtonPreferredSize;
            var le = btn.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le)
            {
                le.preferredWidth  = optionButtonPreferredSize.x;
                le.preferredHeight = optionButtonPreferredSize.y;
                le.layoutPriority = Mathf.Max(le.layoutPriority, 1);
            }
            btn.gameObject.SetActive(true);
            btn.onClick.AddListener(() =>
            {
                answered = true;
                attemptCount++;
                lastSelected = opt;
                // 1) 우선순위: phonemeId와 옵션 id 일치 여부로 정답 판정
                if (q.phonemeId != 0)
                {
                    correct = (opt.id == q.phonemeId);
                    Debug.Log($"[Stage11] 선택: opt.id={opt.id}, phonemeId={q.phonemeId}, match={correct}");
                }
                // 2) 폴백: 값/유니코드 문자열 비교
                if (!correct && q.phonemeId == 0)
                {
                    var chosenCandidates = new List<string>();
                    if (!string.IsNullOrEmpty(opt.value)) chosenCandidates.Add(NormalizeForCompare(opt.value));
                    if (!string.IsNullOrEmpty(opt.unicode)) chosenCandidates.Add(NormalizeForCompare(opt.unicode));
                    var answerCandidates = new List<string>();
                    if (!string.IsNullOrEmpty(q.value)) answerCandidates.Add(NormalizeForCompare(q.value));
                    if (!string.IsNullOrEmpty(q.unicode)) answerCandidates.Add(NormalizeForCompare(q.unicode));
                    correct = chosenCandidates.Any(cc => answerCandidates.Any(ac => string.Equals(cc, ac, System.StringComparison.Ordinal)));
                    if (!correct)
                    {
                        Debug.Log($"[Stage11] 비교 불일치 chosen=[{string.Join(",", chosenCandidates)}] answer=[{string.Join(",", answerCandidates)}]");
                    }
                }
            });
        }

        if (q.options == null || q.options.Count == 0)
        {
            Debug.LogError("[Stage11] 옵션이 비어 있습니다. 버튼을 표시할 수 없습니다.");
            optionsContainer.gameObject.SetActive(false);
            yield break;
        }
        Debug.Log($"[Stage11] 옵션 표시: {q.options.Count}개");
        foreach (var opt in q.options) SetupOne(opt);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);

        // 선택 대기 → 피드백 → 정답일 때까지 반복
        while (true)
        {
            yield return new WaitUntil(() => answered);

            // 선택 시도 로깅: /api/train/attempt
            if (lastSelected != null)
            {
                string selectedVal = NormalizeField(lastSelected.value);
                int attemptNumber = attemptCount; // 1부터 증가
                bool includeReplyResult = attemptNumber > 1;
                yield return sessionController.LogAttempt(
                    stageSessionId,
                    GetStageForAttempt(q, lastSelected),
                    _currentProblemNumber,
                    attemptNumber,
                    selectedVal,
                    correct,
                    q != null ? q.problemWord : null,
                    correctPhonemeValue,
                    includeReplyResult,
                    null);
            }

            if (correct)
            {
                yield return PlayClip(sfxCorrectClip);
                break; // 다음 문제로
            }
            else
            {
                yield return PlayClip(sfxWrongClip);
                wrongCount++;
                if (wrongCount >= maxWrongAttempts)
                {
                    // 오답 허용 횟수 초과 → 다음 문제로 진행
                    break;
                }
                answered = false; // 다시 선택 대기
            }
        }

        // 옵션 정리(선택사항)
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);
        optionsContainer.gameObject.SetActive(false);
    }

    private string ResolveAnswerValue(QuestionDto q)
    {
        if (q == null) return string.Empty;
        string normalized = NormalizeField(q.value);
        if (!string.IsNullOrEmpty(normalized)) return normalized;

        if (q.options != null && q.options.Count > 0)
        {
            OptionDto priority = null;
            if (q.phonemeId != 0)
            {
                priority = q.options.FirstOrDefault(o => o != null && o.id == q.phonemeId);
            }
            if (priority == null)
            {
                priority = q.options.FirstOrDefault(o => o != null && !string.IsNullOrWhiteSpace(o.value));
            }
            if (priority == null)
            {
                priority = q.options.FirstOrDefault(o => o != null && !string.IsNullOrWhiteSpace(o.unicode));
            }
            if (priority != null)
            {
                string val = NormalizeField(priority.value);
                if (!string.IsNullOrEmpty(val))
                    return val;
                string unicodeVal = NormalizeField(priority.unicode);
                if (!string.IsNullOrEmpty(unicodeVal))
                    return unicodeVal;
            }
        }

        return string.Empty;
    }

    private void LateUpdate()
    {
        if (_guideLocked && guideImage)
        {
            guideImage.anchoredPosition = _guideFinalPos;
            guideImage.sizeDelta = _guideFinalSize;
        }
    }

    protected virtual string GetStageForVoiceUpload(QuestionDto q)
    {
        return !string.IsNullOrWhiteSpace(stage) ? stage : stageTwoPart;
    }

    protected virtual string GetStageForAttempt(QuestionDto q, OptionDto selectedOption)
    {
        return stage;
    }

    private void ShowEndModal()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (!canvas) return;

        // 배경 오버레이
        var overlay = new GameObject("EndModal", typeof(RectTransform), typeof(Image));
        overlay.layer = canvas.gameObject.layer;
        var rt = overlay.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var bg = overlay.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = true;

        // 패널
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.layer = canvas.gameObject.layer;
        var prt = panel.GetComponent<RectTransform>();
        prt.SetParent(overlay.transform, false);
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(2200, 1500);
        var pbg = panel.GetComponent<Image>();
        pbg.color = new Color(0.15f, 0.2f, 0.28f, 0.95f);

        // 타이틀 텍스트
        var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.layer = canvas.gameObject.layer;
        var trt = title.GetComponent<RectTransform>();
        trt.SetParent(panel.transform, false);
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -80f);
        trt.sizeDelta = new Vector2(1000, 150);
        var t = title.GetComponent<Text>();
        t.text = "학습이 끝났어요!";
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = 100;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.font = uiFont ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 버튼들
        Vector2 btnSize = (endModalButtonSize.sqrMagnitude > 0f) ? endModalButtonSize : optionButtonPreferredSize;
        float gap = 40f;

        Button ResolveButton(Button preferred, string[] resourcePaths, out bool isCustom)
        {
            // custom: inspector or resources provided
            if (preferred)
            {
                isCustom = true;
                return preferred;
            }
            foreach (var path in resourcePaths)
            {
                var loaded = Resources.Load<Button>(path);
                if (loaded)
                {
                    isCustom = true;
                    return loaded;
                }
                var go = Resources.Load<GameObject>(path);
                if (go)
                {
                    var childBtn = go.GetComponentInChildren<Button>(true) ?? go.GetComponent<Button>();
                    if (childBtn)
                    {
                        isCustom = true;
                        return childBtn;
                    }
                }
            }
            isCustom = false;
            return optionButtonPrefab;
        }

        // 다시 학습하기
        bool againCustom;
        var btn1 = Instantiate(ResolveButton(againButtonPrefab, new[]{"againbutton","UI/againbutton","Images/againbutton"}, out againCustom), panel.transform as RectTransform);
        var btn1rt = btn1.GetComponent<RectTransform>();
        btn1rt.anchorMin = new Vector2(0.5f, 0.5f);
        btn1rt.anchorMax = new Vector2(0.5f, 0.5f);
        btn1rt.pivot = new Vector2(1f, 0.5f);
        btn1rt.sizeDelta = btnSize;
        btn1rt.anchoredPosition = new Vector2(-gap*0.5f, -100f);
        if (!againCustom)
        {
            var txt1 = btn1.GetComponentInChildren<Text>();
            var tmp1 = btn1.GetComponentInChildren<TMP_Text>();
            if (txt1) { txt1.text = "다시 학습하기"; if (uiFont) txt1.font = uiFont; }
            else if (tmp1) { tmp1.text = "다시 학습하기"; if (tmpFont) tmp1.font = tmpFont; }
        }
        btn1.onClick.AddListener(() => { Destroy(overlay); RestartStage(); });

        // 로비로 나가기
        bool lobbyCustom;
        var btn2 = Instantiate(ResolveButton(lobbyButtonPrefab, new[]{"lobbybutton","UI/lobbybutton","Images/lobbybutton"}, out lobbyCustom), panel.transform as RectTransform);
        var btn2rt = btn2.GetComponent<RectTransform>();
        btn2rt.anchorMin = new Vector2(0.5f, 0.5f);
        btn2rt.anchorMax = new Vector2(0.5f, 0.5f);
        btn2rt.pivot = new Vector2(0f, 0.5f);
        btn2rt.sizeDelta = btnSize;
        btn2rt.anchoredPosition = new Vector2(gap*0.5f, -100f);
        if (!lobbyCustom)
        {
            var txt2 = btn2.GetComponentInChildren<Text>();
            var tmp2 = btn2.GetComponentInChildren<TMP_Text>();
            if (txt2) { txt2.text = "로비로 나가기"; if (uiFont) txt2.font = uiFont; }
            else if (tmp2) { tmp2.text = "로비로 나가기"; if (tmpFont) tmp2.font = tmpFont; }
        }
        btn2.onClick.AddListener(() => { Destroy(overlay); GoToLobby(); });
    }

    private void RestartStage()
    {
        StopAllCoroutines();
        // 상태 리셋
        if (optionsContainer)
        {
            foreach (Transform child in optionsContainer)
                Destroy(child.gameObject);
            optionsContainer.gameObject.SetActive(false);
        }
        ConfigureTutorialController();
        _tutorialController?.ResetAfterStageRestart();
        if (mainImage)
        {
            mainImage.enabled = false;
            mainImage.sprite = null;
        }
        _guideLocked = false;
        _guideMoveCo = null;
        _guideMoved = false;
        stageSessionId = string.Empty;
        StartCoroutine(RunStage());
    }

    private void GoToLobby()
    {
        if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene(SceneId.Lobby);
        else SceneManager.LoadScene(SceneId.Lobby);
    }

    // 도입 시퀀스: [1.1.1] + [1.1.2] 오디오만 재생 (이미지 이동은 sfxNext 타이밍)
    private IEnumerator RunRemedialSequence()
    {
        ConfigureSupplementController();
        if (_supplementController == null)
            yield break;
        yield return _supplementController.RunRemedialSequence();
    }

    private IEnumerator MoveCursorSmooth(Transform cursorTransform, RectTransform target, float moveSeconds, AnimationCurve curve)
    {
        if (!cursorTransform || !target)
            yield break;

        Vector3 start = cursorTransform.position;
        Vector3 end = target.position;

        if (moveSeconds <= 0f)
        {
            cursorTransform.position = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < moveSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveSeconds);
            float eased = curve != null ? curve.Evaluate(t) : t;
            cursorTransform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        cursorTransform.position = end;
    }

    private IEnumerator PulseOption(RectTransform rect, float scaleMultiplier, float totalDuration, int loops)
    {
        if (!rect || loops <= 0 || totalDuration <= 0f || Mathf.Approximately(scaleMultiplier, 1f))
            yield break;

        Vector3 originalScale = rect.localScale;
        float halfDuration = totalDuration / (loops * 2f);
        for (int i = 0; i < loops; i++)
        {
            yield return LerpRectScale(rect, originalScale, originalScale * scaleMultiplier, halfDuration);
            yield return LerpRectScale(rect, originalScale * scaleMultiplier, originalScale, halfDuration);
        }
        rect.localScale = originalScale;
    }

    private IEnumerator LerpRectScale(RectTransform rect, Vector3 from, Vector3 to, float duration)
    {
        if (!rect)
            yield break;

        if (duration <= 0f)
        {
            rect.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        rect.localScale = to;
    }

    private IEnumerator MoveGuideAndScaleOverTime(float duration)
    {
        var rt = guideImage;
        if (!rt) yield break;

        // 목표 위치/크기 계산
        var startPos = rt.anchoredPosition;
        var endPos   = ComputeBottomRightAnchoredPosition(rt);
        var startSize = rt.sizeDelta;
        var endSize   = guideEndSize;

        Debug.Log($"[Stage11] Guide move start pos={startPos} size={startSize} -> end pos={endPos} size={endSize} dur={duration}");

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = Mathf.SmoothStep(0, 1, u);
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, k);
            rt.sizeDelta        = Vector2.LerpUnclamped(startSize, endSize, k);
            yield return null;
        }

        rt.anchoredPosition = endPos;
        rt.sizeDelta = endSize;
        Debug.Log("[Stage11] Guide move end");
        _guideMoveCo = null;
        // 이동 완료 후 위치/크기 고정
        _guideLocked = true;
        _guideFinalPos = endPos;
        _guideFinalSize = endSize;
    }

    private static Vector2 ComputeBottomRightAnchoredPosition(RectTransform rt)
    {
        var parent = rt.parent as RectTransform;
        if (!parent)
            return rt.anchoredPosition;
        Vector2 parentHalf = parent.rect.size * 0.5f;
        Vector2 selfHalf = rt.rect.size * 0.5f;
        return new Vector2(parentHalf.x - selfHalf.x, -(parentHalf.y - selfHalf.y));
    }

}
