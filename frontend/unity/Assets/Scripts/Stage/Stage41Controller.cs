using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage 4.1 컨트롤러 (모든 stage=4 규격)
/// - GET  /api/train/set?stage=4&count=N
/// - POST /api/train/stage/start?stage=4&totalProblems=N
/// - POST /api/train/check/voice?stageSessionId=&stage=4&problemNumber=
///   (multipart: audio=voice.wav)
/// - POST /api/train/attempt (drag 보정 시 시도 기록)
/// - POST /api/train/stage/complete?stageSessionId=
/// 게임 흐름은 사용자 제공 4.1 시나리오에 따름.
/// </summary>
public partial class Stage41Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "https://readingbuddyai.co.kr";
    [Tooltip("set에 사용할 stage 값 (4.1)")]
    public string stageSet = "4.1";
    [Tooltip("start/voice/attempt/complete 등에 사용할 stage 값 (4)")]
    public string stageTwoPart = "4.1";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";

    [Header("세션")]
    [Tooltip("/api/train/stage/start 응답의 stageSessionId")]
    public string stageSessionId = "";

    [Header("UI 참조")]
    public Text progressText;
    public Font uiFont;
    public Image guideImage;
    public RectTransform guideRect;
    public GameObject guide3DCharacter;
    public TMP_Text wordText;
    public TMP_Text choseongText;
    public TMP_Text jungseongText;
    public TMP_Text jongseongText;
    public GameObject choseongBox;
    public GameObject jungseongBox;
    public GameObject jongseongBox;
    [Header("Tutorial Slots (Optional - if not set, uses gameplay slots)")]
    [Tooltip("튜토리얼 전용 초성 슬롯. 설정하지 않으면 choseongBox를 사용합니다.")]
    public GameObject tutorialChoseongBox;
    [Tooltip("튜토리얼 전용 중성 슬롯. 설정하지 않으면 jungseongBox를 사용합니다.")]
    public GameObject tutorialJungseongBox;
    [Tooltip("튜토리얼 전용 종성 슬롯. 설정하지 않으면 jongseongBox를 사용합니다.")]
    public GameObject tutorialJongseongBox;
    public GameObject micIndicator;
    [Tooltip("보기 상자(자모 후보) 컨테이너(공용, 선택)")]
    public GameObject choicesContainer;
    [Tooltip("자음 후보 상자(초성/종성 오답 시 표시)")]
    public GameObject consonantChoicesContainer;
    [Tooltip("모음 후보 상자(중성 오답 시 표시)")]
    public GameObject vowelChoicesContainer;

    [Header("추가 학습 시나리오")]
    public AudioClip clipRemedialNeedPractice;
    public AudioClip clipRemedialPracticeIntro;
    public AudioClip clipRemedialFirstEncourage;
    public AudioClip clipRemedialSecondEncourage;
    public AudioClip clipRemedialPerfect;
    public AudioClip clipRemedialNextLesson;
    [Tooltip("격려 멘트 후 대기 시간(초)")]
    public float remedialEncouragePauseSeconds = 3f;

    [Header("추가 학습 리소스")]
    public Image remedialImage;
    public List<RemedialPracticeResource> remedialResources = new List<RemedialPracticeResource>();

    [Header("End Modal (Stage Complete)")]
    public AudioClip clipStageCompleteVoice;   // 종료 시 추가 재생할 음성
    public Button againButtonPrefab;
    public Button lobbyButtonPrefab;
    public Button optionButtonPrefab;
    public Vector2 endModalButtonSize = new Vector2(600f, 300f);
    public Vector2 optionButtonPreferredSize = new Vector2(1200f, 600f);
    public TMP_FontAsset tmpFont;

    [Header("가이드 이동/축소")]
    public Vector2 guideStartSize = new Vector2(1500, 1500);
    public Vector2 guideEndSize = new Vector2(600, 600);
    public Vector2 guideEndAnchoredPos = new Vector2(650, -350);
    public float guideMoveDuration = 1.5f;
    public bool guideMoveOnlyOnce = true;
    public bool enableGuideMoveBetweenQuestions = false;
    private bool _guideMoved;

    [Header("오디오 재생")]
    public AudioSource audioSource;
    public AudioClip sfxStart;
    public AudioClip sfxNext;

    [Header("대사 오디오")]
    public AudioClip clipIntroListenPhonemes;    // [4.1.2]
    public AudioClip clipFocusListen;            // [4.1.3]
    public AudioClip clipPromptFirstPiece;       // [4.1.4]
    public AudioClip clipGreat;                  // [4.1.5]
    public AudioClip clipPromptSecondPiece;      // [4.1.6]
    public AudioClip clipPromptFinalPiece;       // [4.1.7]
    public AudioClip clipAllShine;               // [4.1.8]
    public AudioClip clipNeedMorePower;          // [4.1.9]
    public AudioClip clipFindCorrect;            // [4.1.10]
    public AudioClip clipGoodThatsIt;            // [4.1.11]
    public AudioClip clipAlmostThere;             // 재녹음 피드백 (비정확)
    public AudioClip clipTryAgain;               // [4.1.12]
    public AudioClip clipFinalizeSpell;          // [4.1.13]

    [Header("녹음 설정")]
    public int recordSeconds = 3;
    public int recordSampleRate = 44100;

    [Header("박스 포커스/점멸")]
    [Range(0f, 1f)] public float dimAlpha = 0.4f;
    [Range(0f, 1f)] public float blinkAlphaMin = 0.4f;
    [Range(0f, 1f)] public float blinkAlphaMax = 1.0f;
    public float blinkPeriod = 0.6f; // sec per cycle

    [Header("개발/우회")]
    public bool bypassStartRequest = false;
    public bool bypassVoiceUpload = false;
    public bool logVerbose = true;

    // 상태
    private int _currentProblemNumber = 0; // 1-based
    private bool _awaitingUserArrangement;
    private List<string> _segmentReplies = new List<string>();
    private List<bool> _segmentCorrects = new List<bool>();
    private List<List<string>> _segmentReplyCandidates = new List<List<string>>();
    private List<string> _expectedPhonemes = new List<string>();
    private int _expectedSegmentCount = 0; // 2 or 3
    private int _attemptCountForProblem = 0;
    private int _pendingVoiceUploads = 0; // async voice uploads in flight
    private bool _isRecording = false;    // 3초 녹음 진행 여부
    private int _currentCorrectionSlot = -1; // 드래그 교정 대상 슬롯(0/1/2)
    private int[] _attemptsPerSlot = new int[3]; // 슬롯별 드래그 시도 횟수
    private Coroutine _blinkCo;
    private GameObject _focusedBox;
    private string _currentWordVoiceUrl;
    private bool[] _finalizedSlots = new bool[3]; // 각 슬롯(초/중/종) 최종 확정 여부
    private readonly bool[] _segmentVoiceReplyReady = new bool[3];
    private readonly bool[] _segmentVoiceReplyCorrect = new bool[3];

    #region DTOs
    [Serializable]
    public class QuestionDto
    {
        public int questionId;          // 선택
        public string problemWord;
        public string voiceUrl;     // 듣기용
        public int answerCnt;           // 2 또는 3
        public string imageUrl;         // 선택
        public List<string> phonemes;   // index 0=초,1=중,2=종
    }

    [Serializable]
    public class QuestionData
    {
        public string stageSessionId;
        public List<QuestionDto> problems;
        public int totalProblems;
    }

    [Serializable]
    public class QuestionListResponse
    {
        public bool success;
        public string message;
        public QuestionData data;
    }

    [Serializable]
    private class StartStageData { public string stageSessionId; public string stage; public int totalProblems; public string startAt; }
    [Serializable]
    private class StartStageResp { public bool success = true; public string message; public StartStageData data; }

    [Serializable]
    private class VoiceReplyData { public string reply; public bool isReplyCorrect; public float accuracy; public string audioUrl; }
    [Serializable]
    private class VoiceReplyResp { public bool success = true; public string message; public VoiceReplyData data; }
    #endregion

    private void Start()
    {
        StartCoroutine(InitializeWithAuth());
    }

    private IEnumerator InitializeWithAuth()
    {
        Debug.Log("[Stage41] Waiting for AuthManager...");
        float timeout = 5f;
        float elapsed = 0f;
        while (AuthManager.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (AuthManager.Instance == null)
        {
            Debug.LogError("[Stage41] ⚠️ AuthManager.Instance is null after timeout! Returning to Home.");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(SceneId.Home);
            else
                SceneManager.LoadScene("Home");
            yield break;
        }

        Debug.Log("[Stage41] AuthManager found!");

        if (!AuthManager.Instance.IsLoggedIn())
        {
            Debug.LogError("[Stage41] ⚠️ User is not logged in! Returning to Home.");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(SceneId.Home);
            else
                SceneManager.LoadScene("Home");
            yield break;
        }

        Debug.Log("[Stage41] User is logged in!");

        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);

        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
        {
            authToken = AuthManager.Instance.GetAccessToken();
            Debug.Log("[Stage41] ✓ Access token retrieved from AuthManager");
        }
        else
        {
            authToken = EnvConfig.ResolveAuthToken(authToken);
            Debug.Log("[Stage41] Using authToken from EnvConfig (fallback)");
        }

        if (guideRect && guideStartSize.sqrMagnitude > 0)
            guideRect.sizeDelta = guideStartSize;

        ConfigureStageModules();
        _tutorialController?.PrepareForStageStart();

        ResetUI();
        StartCoroutine(RunStage());
    }

    private void ResetUI()
    {
        if (progressText) progressText.text = string.Empty;
        if (wordText) wordText.text = string.Empty;
        if (choseongText) choseongText.text = string.Empty;
        if (jungseongText) jungseongText.text = string.Empty;
        if (jongseongText) jongseongText.text = string.Empty;
        if (choseongBox) choseongBox.SetActive(true);
        if (jungseongBox) jungseongBox.SetActive(true);
        if (jongseongBox) jongseongBox.SetActive(true);
        if (micIndicator) micIndicator.SetActive(false);
        if (choicesContainer) choicesContainer.SetActive(false);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
        if (remedialImage)
        {
            remedialImage.enabled = false;
            remedialImage.sprite = null;
        }
        FocusBox(null);
        SetAllBoxAlpha(dimAlpha);
    }

    // 메인 스테이지 진행 코루틴
    private IEnumerator RunStage()
    {
        _tutorialController?.ResetAfterStageRestart();

        yield return PlayClip(sfxStart);

        if (_tutorialController != null)
        {
            yield return _tutorialController.RunIntroSequence();
            yield return _tutorialController.RunIntroTutorial();
            RestoreTutorialChoiceTiles();
        }
        SetGameplaySlotsActive(false);
        HandleTutorialClearAllSlots();

        if (logVerbose)
        {
            bool panelActive = _tutorialController != null && _tutorialController.introTutorialPanel != null && _tutorialController.introTutorialPanel.activeSelf;
            bool guideActive = guide3DCharacter != null && guide3DCharacter.activeSelf;
            bool choicesActive = choicesContainer != null && choicesContainer.activeSelf;
            bool consonantChoicesActive = consonantChoicesContainer != null && consonantChoicesContainer.activeSelf;
            bool vowelChoicesActive = vowelChoicesContainer != null && vowelChoicesContainer.activeSelf;
            bool choseongBoxActive = choseongBox != null && choseongBox.activeSelf;
            bool jungseongBoxActive = jungseongBox != null && jungseongBox.activeSelf;
            bool jongseongBoxActive = jongseongBox != null && jongseongBox.activeSelf;

            Debug.Log(
                $"[Stage41] Before IntroListenPhonemes: panelActive={panelActive}, guideActive={guideActive}, " +
                $"choicesActive={choicesActive}, consonantChoicesActive={consonantChoicesActive}, " +
                $"vowelChoicesActive={vowelChoicesActive}, choseongBoxActive={choseongBoxActive}, " +
                $"jungseongBoxActive={jungseongBoxActive}, jongseongBoxActive={jongseongBoxActive}");
        }

        yield return PlayClip(clipIntroListenPhonemes);  // [4.1.2]

        // 본 훈련 시작 시 패널 표시
        if (_tutorialController != null)
        {
            yield return _tutorialController.ShowPanel(false);
            if (logVerbose)
                Debug.Log("[Stage41] Panel shown at start of main training");
        }

        EnsureGameplayUiVisible();
        // 본 훈련 슬롯 텍스트 초기화 (튜토리얼 슬롯과 분리되어 있으면 자동으로 초기화됨)
        SetSlotText(0, string.Empty);
        SetSlotText(1, string.Empty);
        SetSlotText(2, string.Empty);

        if (!bypassStartRequest && string.IsNullOrWhiteSpace(stageSessionId))
            yield return StartStageSession();

        _supplementController?.Clear();

        List<QuestionDto> questions = null;
        yield return StartCoroutine(FetchQuestions(result => questions = result));
        if (logVerbose)
            Debug.Log($"[Stage41] set 완료 → count={(questions != null ? questions.Count : 0)}");
        if (questions == null || questions.Count == 0)
        {
            _supplementQuestionController.SetQuestions(null);
            yield break;
        }

        UpdateSupplementQuestions(questions);
        _questionController.SetQuestions(questions);

        if ((enableGuideMoveBetweenQuestions || !_guideMoved) && guideRect)
            yield return MoveGuideToCorner();

        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            _currentProblemNumber = i + 1;
            _attemptCountForProblem = 0;
            _segmentReplies.Clear();
            _segmentCorrects.Clear();
            _segmentReplyCandidates.Clear();
            _expectedPhonemes = (q.phonemes != null) ? new List<string>(q.phonemes) : new List<string>();
            _expectedSegmentCount = (q.answerCnt > 0) ? q.answerCnt : (_expectedPhonemes != null ? _expectedPhonemes.Count : 3);
            Array.Clear(_finalizedSlots, 0, _finalizedSlots.Length);

            SetProgressLabel(_currentProblemNumber, questions.Count);
            if (wordText) wordText.text = q.problemWord ?? string.Empty;
            _currentWordVoiceUrl = q.voiceUrl;
            ClearPhonemeBoxes();
            if (logVerbose)
            {
                Debug.Log($"[Stage41] After ClearPhonemeBoxes: 초='{choseongText?.text}', 중='{jungseongText?.text}', 종='{jongseongText?.text}'");
            }

            // [4.1.3] 집중 안내 + 듣기
            yield return PlayClip(clipFocusListen);
            if (logVerbose)
                Debug.Log($"[Stage41] 문제 {_currentProblemNumber}: word='{q.problemWord}', voiceUrl='{q.voiceUrl}', answerCnt={_expectedSegmentCount}, phonemes='{(q.phonemes != null ? string.Join("", q.phonemes) : "<null>")}'");
            yield return PlayVoiceUrl(q.voiceUrl);

            // 초성
            FocusBox(choseongBox);
            yield return PlayClip(clipPromptFirstPiece); // [4.1.4]
            PrepareSegmentVoiceFeedback(0);
            // fire-and-forget voice upload for 초성
            _pendingVoiceUploads++;
            _isRecording = true; // ensure wait condition blocks until recording finishes
            StartCoroutine(Co_WrapRecordUpload(0));
            // 녹음(3초) 종료를 보장한 뒤 칭찬 재생
            yield return new WaitForSeconds(recordSeconds + 0.15f);
            yield return WaitForSegmentVoiceFeedback(0);
            var clipForSegment0 = GetSegmentFeedbackClip(IsSegmentVoiceReplyCorrect(0));
            if (clipForSegment0)
                yield return PlayClip(clipForSegment0);  // [4.1.11]
            // proceed immediately without waiting response

            // 중성
            if (_expectedSegmentCount >= 2)
            {
                FocusBox(jungseongBox);
                yield return PlayClip(clipPromptSecondPiece); // [4.1.6]
                PrepareSegmentVoiceFeedback(1);
                // fire-and-forget voice upload for 중성
                _pendingVoiceUploads++;
                _isRecording = true;
                StartCoroutine(Co_WrapRecordUpload(1));
                yield return new WaitForSeconds(recordSeconds + 0.15f);
                yield return WaitForSegmentVoiceFeedback(1);
                var clipForSegment1 = GetSegmentFeedbackClip(IsSegmentVoiceReplyCorrect(1));
                if (clipForSegment1)
                    yield return PlayClip(clipForSegment1);  // [4.1.11]
                // proceed immediately without waiting response
            }

            // 종성
            if (_expectedSegmentCount >= 3)
            {
                FocusBox(jongseongBox);
                yield return PlayClip(clipPromptFinalPiece); // [4.1.7]
                PrepareSegmentVoiceFeedback(2);
                // fire-and-forget voice upload for 종성
                _pendingVoiceUploads++;
                _isRecording = true;
                StartCoroutine(Co_WrapRecordUpload(2));
                yield return new WaitForSeconds(recordSeconds + 0.15f);
                yield return WaitForSegmentVoiceFeedback(2);
                var clipForSegment2 = GetSegmentFeedbackClip(IsSegmentVoiceReplyCorrect(2));
                if (clipForSegment2)
                    yield return PlayClip(clipForSegment2);  // [4.1.11]
                // proceed immediately without waiting response
            }

            FocusBox(null);

            // wait for all pending voice uploads to complete before evaluating correctness
            yield return new WaitUntil(() => _pendingVoiceUploads == 0);

            FillSlotsFromRepliesWithDim();

            // 모두 정답 여부
            bool allCorrect = EvaluateCorrectness();
            // Send final combined attempt per new spec
            string combinedAnswer = string.Join("", _segmentReplies);
            StartCoroutine(SendAttemptLogNew(
                problemNumber: _currentProblemNumber,
                attemptNumber: 1,
                problem: wordText ? wordText.text : string.Empty,
                answer: combinedAnswer,
                isCorrect: allCorrect,
                isReplyCorrect: allCorrect,
                audioUrl: string.Empty
            ));
            if (allCorrect)
            {
                yield return PlayClip(clipAllShine); // [4.1.8]
                SetAllBoxAlpha(1f);
                if (choicesContainer) choicesContainer.SetActive(false);
                if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
                if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
            }
            else
            {
                yield return PlayClip(clipNeedMorePower); // [4.1.9]
                // 안내 멘트는 CorrectionFlow 내에서 슬롯별로 1회씩 재생
                // 부분 정답은 채워두고(맞은 칸 고정), 드롭 교정 플로우를 순차 진행
                ApplyPartialFill();
                // 패널 표시는 CorrectionFlow에서 현재 슬롯에 맞게 제어
                yield return StartCoroutine(CorrectionFlow());
                if (choicesContainer) choicesContainer.SetActive(false);
                if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
                if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
            }

            if (i < questions.Count - 1)
                yield return PlayClip(sfxNext);
        }
        if (!string.IsNullOrWhiteSpace(stageSessionId))
        {
            _supplementController?.Clear();
            yield return CompleteStageSession();
            if (_supplementController != null && _supplementController.RemedialPhonemes.Count > 0)
                yield return RunRemedialSequence();
        }

        // 완전 종료용 오디오 재생
        if (clipStageCompleteVoice)
        {
            yield return PlayClip(clipStageCompleteVoice);
        }

        // 종료 모달 표시
        ShowEndModal();
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

    public void OnClickAgainButton()
    {
        Debug.Log("[Stage41] 다시 학습하기 버튼 클릭됨");
        var modal = GameObject.Find("EndModal");
        if (modal) Destroy(modal);
        RestartStage();
    }

    public void OnClickLobbyButton()
    {
        Debug.Log("[Stage41] 로비로 나가기 버튼 클릭됨");
        var modal = GameObject.Find("EndModal");
        if (modal) Destroy(modal);
        GoToLobby();
    }

    private void RestartStage()
    {
        StopAllCoroutines();
        stageSessionId = string.Empty;
        _supplementController?.Clear();
        ResetUI();
        StartCoroutine(RunStage());
    }

    private void GoToLobby()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(SceneId.Lobby);
        else
            SceneManager.LoadScene("Lobby");
    }

    // 드래그로 재배열 완료 시 외부에서 호출: 초/중/종 순
    public void SetUserArrangement(string initial, string medial, string finalPhoneme)
    {
        var arranged = new List<string>();
        if (!string.IsNullOrEmpty(initial)) arranged.Add(initial);
        if (!string.IsNullOrEmpty(medial)) arranged.Add(medial);
        if (!string.IsNullOrEmpty(finalPhoneme)) arranged.Add(finalPhoneme);

        bool correct = ComparePhonemeOrder(arranged, _expectedPhonemes);
        _attemptCountForProblem++;

        string selectedAnswer = string.Join("", arranged);
            StartCoroutine(SendAttemptLogNew(
            problemNumber: _currentProblemNumber,
            attemptNumber: 1,
            problem: wordText ? wordText.text : string.Empty,
            answer: selectedAnswer,
            isCorrect: correct,
            isReplyCorrect: correct,
            audioUrl: string.Empty
        ));

        if (correct)
        {
            StartCoroutine(PlayClip(clipGoodThatsIt)); // [4.1.11]
            _awaitingUserArrangement = false;
            StartCoroutine(PlayClip(clipAllShine));    // [4.1.8]
            SetAllBoxAlpha(1f);
        }
        else
        {
            StartCoroutine(PlayClipThenReplayWord(clipTryAgain));    // [4.1.12]
        }
    }

    public event Action OnStageComplete;

    #region Helpers / Network
    private void SetProgressLabel(int index, int total)
    {
        if (progressText) progressText.text = $"{index} / {total}";
    }

    private void ClearPhonemeBoxes()
    {
        if (choseongText) choseongText.text = string.Empty;
        if (jungseongText) jungseongText.text = string.Empty;
        if (jongseongText) jongseongText.text = string.Empty;
        SetAllBoxAlpha(dimAlpha);
    }

    public void OnSegmentRecognitionHypotheses(int segmentIndex, IList<string> candidates)
    {
        if (_expectedPhonemes == null) return;
        if (segmentIndex < 0 || segmentIndex >= _expectedSegmentCount) return;
        if (candidates == null) return;

        EnsureSegmentListCapacity(segmentIndex);

        var store = _segmentReplyCandidates[segmentIndex];
        store.Clear();
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            store.Add(candidate);
        }

        string best = SelectBestCandidateForSlot(segmentIndex, store);
        if (string.IsNullOrEmpty(best))
        {
            return;
        }

        _segmentReplies[segmentIndex] = best;
        if (logVerbose)
            Debug.Log($"[Stage41] OnSegmentRecognitionHypotheses slot={segmentIndex} best='{best}'");
        if (logVerbose)
            Debug.Log($"[Stage41] SetSlotText idx={segmentIndex} text='{best}' (from OnSegmentRecognitionHypotheses)");
        SetSlotText(segmentIndex, best);

        if (!_finalizedSlots[segmentIndex])
            SetSlotAlpha(segmentIndex, dimAlpha);
    }

    private void EnsureSegmentListCapacity(int segmentIndex)
    {
        while (_segmentReplies.Count <= segmentIndex) _segmentReplies.Add(string.Empty);
        while (_segmentCorrects.Count <= segmentIndex) _segmentCorrects.Add(false);
        while (_segmentReplyCandidates.Count <= segmentIndex) _segmentReplyCandidates.Add(new List<string>());
    }

    private void EnsureExpectedPhonemeCapacity(int segmentIndex)
    {
        if (_expectedPhonemes == null)
            _expectedPhonemes = new List<string>();
        while (_expectedPhonemes.Count <= segmentIndex) _expectedPhonemes.Add(string.Empty);
    }

    private string SelectBestCandidateForSlot(int segmentIndex, IList<string> candidates)
    {
        if (candidates == null || candidates.Count == 0) return string.Empty;

        string expected = GetTargetPhonemeAnswer(segmentIndex);
        string normalizedExpected = NormalizePhoneme((expected ?? string.Empty).Trim());

        string bestCandidate = string.Empty;
        int bestScore = int.MinValue;

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            string normalized = NormalizePhoneme(candidate.Trim());
            if (string.IsNullOrEmpty(normalized)) continue;

            if (string.Equals(normalized, normalizedExpected, StringComparison.Ordinal))
            {
                return normalized;
            }

            int similarityScore = ComputePhonemeSimilarityScore(normalizedExpected, normalized);
            if (similarityScore > bestScore)
            {
                bestScore = similarityScore;
                bestCandidate = normalized;
            }
        }

        return bestCandidate;
    }

    private static int ComputePhonemeSimilarityScore(string expected, string candidate)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(candidate))
            return int.MinValue;

        // Prefer prefixes that match the beginning of the expected value.
        int prefixMatches = 0;
        int maxCheck = Mathf.Min(expected.Length, candidate.Length);
        for (int i = 0; i < maxCheck; i++)
        {
            if (expected[i] == candidate[i]) prefixMatches++;
            else break;
        }

        int distance = ComputeLevenshteinDistance(expected, candidate);
        // Higher score is better: more prefix matches, smaller distance.
        return prefixMatches * 10 - distance;
    }

    private static int ComputeLevenshteinDistance(string a, string b)
    {
        if (a == null) return b?.Length ?? 0;
        if (b == null) return a.Length;

        int lenA = a.Length;
        int lenB = b.Length;

        if (lenA == 0) return lenB;
        if (lenB == 0) return lenA;

        var d = new int[lenA + 1, lenB + 1];

        for (int i = 0; i <= lenA; i++) d[i, 0] = i;
        for (int j = 0; j <= lenB; j++) d[0, j] = j;

        for (int i = 1; i <= lenA; i++)
        {
            for (int j = 1; j <= lenB; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                int deletion = d[i - 1, j] + 1;
                int insertion = d[i, j - 1] + 1;
                int substitution = d[i - 1, j - 1] + cost;
                d[i, j] = Mathf.Min(Mathf.Min(deletion, insertion), substitution);
            }
        }

        return d[lenA, lenB];
    }

    private void HandleTutorialPrefillSlot(string args)
    {
        if (!TryParseTutorialSlotCommand(args, out int slotIndex, out var rawCandidates))
        {
            if (logVerbose)
                Debug.LogWarning($"[Stage41][Tutorial] 잘못된 prefill 명령: '{args}'");
            return;
        }

        slotIndex = Mathf.Clamp(slotIndex, 0, 2);

        var normalizedCandidates = new List<string>();
        foreach (var candidate in rawCandidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;
            string normalized = NormalizePhoneme(candidate.Trim());
            if (!string.IsNullOrEmpty(normalized))
                normalizedCandidates.Add(normalized);
        }

        EnsureExpectedPhonemeCapacity(slotIndex);
        if (normalizedCandidates.Count > 0)
            _expectedPhonemes[slotIndex] = normalizedCandidates[0];

        _expectedSegmentCount = Mathf.Max(_expectedSegmentCount, slotIndex + 1);

        if (normalizedCandidates.Count == 0)
        {
            string fallback = GetTargetPhonemeAnswer(slotIndex);
            if (!string.IsNullOrEmpty(fallback))
                normalizedCandidates.Add(fallback);
        }

        if (normalizedCandidates.Count == 0)
        {
            if (logVerbose)
                Debug.LogWarning("[Stage41][Tutorial] 후보가 없어 슬롯을 채울 수 없습니다.");
            return;
        }

        if (logVerbose)
            Debug.Log($"[Stage41][Tutorial] Prefill slot {slotIndex} with '{string.Join(",", normalizedCandidates)}'");
        
        // 튜토리얼 슬롯에만 텍스트 설정 (본 훈련 슬롯은 건드리지 않음)
        EnsureSegmentListCapacity(slotIndex);
        var store = _segmentReplyCandidates[slotIndex];
        store.Clear();
        foreach (var candidate in normalizedCandidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            store.Add(candidate);
        }
        
        string best = SelectBestCandidateForSlot(slotIndex, store);
        if (string.IsNullOrEmpty(best))
        {
            if (logVerbose)
                Debug.LogWarning($"[Stage41][Tutorial] Prefill: best candidate를 찾을 수 없습니다.");
            return;
        }
        
        // 튜토리얼 슬롯에만 텍스트 설정
        var tutorialSlot = ResolveTutorialSlotObject(IndexToSlotTarget(slotIndex));
        if (tutorialSlot != null)
        {
            var tutorialText = tutorialSlot.GetComponentInChildren<TMP_Text>(true);
            if (tutorialText != null)
            {
                tutorialText.text = best;
                if (logVerbose)
                    Debug.Log($"[Stage41][Tutorial] Set tutorial slot {slotIndex} text to '{best}'");
            }
        }
        else
        {
            if (logVerbose)
                Debug.LogWarning($"[Stage41][Tutorial] Prefill: tutorial slot {slotIndex}을 찾을 수 없습니다.");
        }
    }

    private void HandleTutorialClearSlot(string args)
    {
        if (!TryParseTutorialSlotCommand(args, out int slotIndex, out _))
        {
            if (logVerbose)
                Debug.LogWarning($"[Stage41][Tutorial] 잘못된 clear 명령: '{args}'");
            return;
        }

        if (slotIndex < 0 || slotIndex > 2)
            return;

        EnsureSegmentListCapacity(slotIndex);
        _segmentReplyCandidates[slotIndex].Clear();
        _segmentReplies[slotIndex] = string.Empty;
        if (slotIndex < _segmentCorrects.Count)
            _segmentCorrects[slotIndex] = false;
        if (slotIndex < _expectedPhonemes.Count)
            _expectedPhonemes[slotIndex] = string.Empty;
        if (slotIndex < _finalizedSlots.Length)
            _finalizedSlots[slotIndex] = false;

        var slotHolder = ResolveTutorialSlotObject(IndexToSlotTarget(slotIndex));
        if (slotHolder != null)
        {
            var draggables = slotHolder.GetComponentsInChildren<PhonemeDraggableUI>(true);
            foreach (var draggable in draggables)
            {
                if (draggable == null) continue;
                draggable.ReturnToOrigin();
            }
            ForceRefreshContainerLayout(slotHolder);
        }

        SetSlotText(slotIndex, string.Empty);
        SetSlotAlpha(slotIndex, dimAlpha);
    }

    private void HandleTutorialToggleTileVisibility(string args, bool visible)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            if (logVerbose)
                Debug.LogWarning("[Stage41][Tutorial] tile key가 비어 있습니다.");
            return;
        }

        string key = args.Trim();
        var tile = ResolveTutorialChoiceTile(key);
        if (tile == null)
        {
            if (logVerbose)
                Debug.LogWarning($"[Stage41][Tutorial] '{key}' 키에 해당하는 타일을 찾을 수 없습니다.");
            return;
        }

        if (tile.gameObject.activeSelf != visible)
        {
            tile.gameObject.SetActive(visible);
            ForceRefreshContainerLayout(choicesContainer);
            ForceRefreshContainerLayout(consonantChoicesContainer);
            ForceRefreshContainerLayout(vowelChoicesContainer);
        }
    }

    private bool TryParseTutorialSlotCommand(string args, out int slotIndex, out List<string> rawCandidates)
    {
        slotIndex = -1;
        rawCandidates = new List<string>();
        if (string.IsNullOrWhiteSpace(args))
            return false;

        string slotPart = args;
        string candidatePart = string.Empty;

        int colon = args.IndexOf(':');
        if (colon >= 0)
        {
            slotPart = args.Substring(0, colon);
            candidatePart = args.Substring(colon + 1);
        }
        else
        {
            int comma = args.IndexOf(',');
            if (comma >= 0)
            {
                slotPart = args.Substring(0, comma);
                candidatePart = args.Substring(comma + 1);
            }
        }

        slotPart = slotPart.Trim();
        if (!TryMapSlotIdentifier(slotPart, out slotIndex))
            return false;

        if (!string.IsNullOrWhiteSpace(candidatePart))
        {
            var parts = candidatePart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part?.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    rawCandidates.Add(trimmed);
            }
        }

        return true;
    }

    private bool TryMapSlotIdentifier(string token, out int slotIndex)
    {
        slotIndex = -1;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // numeric index 0/1/2
        if (int.TryParse(token, out slotIndex))
        {
            if (slotIndex >= 0 && slotIndex <= 2)
                return true;
            slotIndex = -1;
            return false;
        }

        string normalized = token.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "초성":
            case "choseong":
            case "initial":
            case "initials":
            case "initialconsonant":
            case "초성슬롯":
                slotIndex = 0;
                return true;
            case "중성":
            case "jungseong":
            case "medial":
            case "medials":
            case "vowel":
            case "모음":
            case "중성슬롯":
                slotIndex = 1;
                return true;
            case "종성":
            case "jongseong":
            case "final":
            case "finals":
            case "받침":
            case "종성슬롯":
                slotIndex = 2;
                return true;
            default:
                return false;
        }
    }

    private StageTutorialSlotTarget IndexToSlotTarget(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return StageTutorialSlotTarget.Choseong;
            case 1: return StageTutorialSlotTarget.Jungseong;
            case 2: return StageTutorialSlotTarget.Jongsung;
            default: return StageTutorialSlotTarget.None;
        }
    }

    private void UpdatePhonemeBoxTexts()
    {
        if (choseongText) choseongText.text = _segmentReplies.Count >= 1 ? _segmentReplies[0] : "";
        if (jungseongText) jungseongText.text = _segmentReplies.Count >= 2 ? _segmentReplies[1] : "";
        if (jongseongText) jongseongText.text = _segmentReplies.Count >= 3 ? _segmentReplies[2] : "";
    }

    // 부분 정답 반영: 맞은 슬롯은 텍스트/밝기, 틀린 슬롯은 비움/희미
    private void ApplyPartialFill()
    {
        for (int i = 0; i < _expectedSegmentCount; i++)
        {
            bool ok = (i < _segmentCorrects.Count) && _segmentCorrects[i];
            string reply = (i < _segmentReplies.Count) ? _segmentReplies[i] : string.Empty;
            if (logVerbose)
                Debug.Log($"[Stage41] SetSlotText idx={i} text='{(ok ? reply : string.Empty)}' (from ApplyPartialFill)");
            SetSlotText(i, ok ? reply : string.Empty);
            _finalizedSlots[i] = ok;
            SetSlotAlpha(i, ok ? 1f : dimAlpha);
        }
        // 필요 없는 세그먼트(예: answerCnt=2)의 종성은 비워둠
        for (int i = _expectedSegmentCount; i < 3; i++)
        {
            if (logVerbose)
                Debug.Log($"[Stage41] SetSlotText idx={i} text='' (from ApplyPartialFill extra)");
            SetSlotText(i, string.Empty);
            _finalizedSlots[i] = true; // 없는 슬롯은 완료로 처리
            SetSlotAlpha(i, dimAlpha);
        }
    }

    // After all voice replies arrive, fill every slot with its reply text.
    // Wrong slots are dimmed; right slots are bright and finalized.
    private void FillSlotsFromRepliesWithDim()
    {
        for (int i = 0; i < _expectedSegmentCount; i++)
        {
            string reply = (i < _segmentReplies.Count) ? _segmentReplies[i] : string.Empty;
            bool ok = (i < _segmentCorrects.Count) && _segmentCorrects[i];
            if (logVerbose)
                Debug.Log($"[Stage41] SetSlotText idx={i} text='{reply}' (from FillSlotsFromRepliesWithDim)");
            SetSlotText(i, reply);
            SetSlotAlpha(i, ok ? 1f : dimAlpha);
            _finalizedSlots[i] = ok;
        }
        for (int i = _expectedSegmentCount; i < 3; i++)
        {
            if (logVerbose)
                Debug.Log($"[Stage41] SetSlotText idx={i} text='' (from FillSlotsFromRepliesWithDim extra)");
            SetSlotText(i, string.Empty);
            _finalizedSlots[i] = true;
            SetSlotAlpha(i, dimAlpha);
        }
    }

    private void SetSlotText(int idx, string text)
    {
        if (idx == 0 && choseongText) choseongText.text = text ?? string.Empty;
        else if (idx == 1 && jungseongText) jungseongText.text = text ?? string.Empty;
        else if (idx == 2 && jongseongText) jongseongText.text = text ?? string.Empty;
        if (logVerbose)
            Debug.Log($"[Stage41] SetSlotText idx={idx} now='{GetSlotText(idx)}'");
    }

    private string GetSlotText(int idx)
    {
        if (idx == 0 && choseongText) return choseongText.text;
        if (idx == 1 && jungseongText) return jungseongText.text;
        if (idx == 2 && jongseongText) return jongseongText.text;
        return string.Empty;
    }

    private void SetSlotAlpha(int idx, float a)
    {
        if (idx == 0) SetBoxAlpha(choseongBox, a);
        else if (idx == 1) SetBoxAlpha(jungseongBox, a);
        else if (idx == 2) SetBoxAlpha(jongseongBox, a);
    }

    private void FocusBox(GameObject box)
    {
        if (_blinkCo != null)
        {
            StopCoroutine(_blinkCo);
            _blinkCo = null;
        }
        _focusedBox = box;
        SetAllBoxAlpha(dimAlpha);
        if (box != null)
            _blinkCo = StartCoroutine(BlinkBox(box));
    }

    private void SetAllBoxAlpha(float a)
    {
        SetBoxAlpha(choseongBox, a);
        SetBoxAlpha(jungseongBox, a);
        SetBoxAlpha(jongseongBox, a);
    }

    private void SetBoxAlpha(GameObject box, float a)
    {
        if (!box) return;
        var cg = box.GetComponent<CanvasGroup>() ?? box.AddComponent<CanvasGroup>();
        cg.alpha = Mathf.Clamp01(a);
    }

    private IEnumerator BlinkBox(GameObject box)
    {
        var cg = box.GetComponent<CanvasGroup>() ?? box.AddComponent<CanvasGroup>();
        float t = 0f;
        while (_focusedBox == box)
        {
            t += Time.deltaTime;
            float phase = Mathf.Sin((t / Mathf.Max(0.01f, blinkPeriod)) * Mathf.PI * 2f) * 0.5f + 0.5f;
            cg.alpha = Mathf.Lerp(blinkAlphaMin, blinkAlphaMax, phase);
            yield return null;
        }
        cg.alpha = dimAlpha;
    }

    private IEnumerator MoveGuideToCorner()
    {
        if (!guideRect) yield break;
        _guideMoved = true;
        var startSize = guideRect.sizeDelta;
        var startPos = guideRect.anchoredPosition;
        var endSize = guideEndSize;
        var endPos = guideEndAnchoredPos;
        float t = 0f;
        while (t < guideMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / guideMoveDuration);
            guideRect.sizeDelta = Vector2.Lerp(startSize, endSize, k);
            guideRect.anchoredPosition = Vector2.Lerp(startPos, endPos, k);
            yield return null;
        }
        guideRect.sizeDelta = endSize;
        guideRect.anchoredPosition = endPos;
    }

    private IEnumerator PlayClip(AudioClip clip)
    {
        if (!clip || !audioSource) yield break;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        yield return new WaitWhile(() => audioSource.isPlaying);
    }

    private IEnumerator PlayVoiceUrl(string voiceUrl)
    {
        if (string.IsNullOrEmpty(voiceUrl) || !audioSource) yield break;
        string safeUrl = EncodePlusInPath(voiceUrl);
        var audioType = GuessAudioType(safeUrl);
        using (var req = UnityWebRequestMultimedia.GetAudioClip(safeUrl, audioType))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            var clip = DownloadHandlerAudioClip.GetContent(req);
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
    }

    private IEnumerator ReplayCurrentWord()
    {
        if (string.IsNullOrEmpty(_currentWordVoiceUrl)) yield break;
        yield return PlayVoiceUrl(_currentWordVoiceUrl);
    }

    private IEnumerator PlayClipThenReplayWord(AudioClip clip)
    {
        if (!clip) yield break;
        yield return PlayClip(clip);
        yield return ReplayCurrentWord();
    }

    // Wrapper: runs the upload coroutine and ensures pending counter is decreased
    private IEnumerator Co_WrapRecordUpload(int segmentIndex)
    {
        yield return RecordAndUploadPhonemeSegment(segmentIndex, (ok, reply) => RecordSegmentVoiceFeedback(segmentIndex, ok));
        _pendingVoiceUploads = Mathf.Max(0, _pendingVoiceUploads - 1);
    }

    private void PrepareSegmentVoiceFeedback(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= _segmentVoiceReplyReady.Length)
            return;
        _segmentVoiceReplyReady[segmentIndex] = false;
        _segmentVoiceReplyCorrect[segmentIndex] = false;
    }

    private void RecordSegmentVoiceFeedback(int segmentIndex, bool isCorrect)
    {
        if (segmentIndex < 0 || segmentIndex >= _segmentVoiceReplyReady.Length)
            return;
        _segmentVoiceReplyReady[segmentIndex] = true;
        _segmentVoiceReplyCorrect[segmentIndex] = isCorrect;
    }

    private IEnumerator WaitForSegmentVoiceFeedback(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= _segmentVoiceReplyReady.Length)
            yield break;
        while (!_segmentVoiceReplyReady[segmentIndex])
            yield return null;
    }

    private bool IsSegmentVoiceReplyCorrect(int segmentIndex)
    {
        return segmentIndex >= 0 && segmentIndex < _segmentVoiceReplyCorrect.Length && _segmentVoiceReplyCorrect[segmentIndex];
    }

    private AudioClip GetSegmentFeedbackClip(bool isReplyCorrect)
    {
        if (!isReplyCorrect)
            return clipAlmostThere ?? clipGoodThatsIt;
        return clipGoodThatsIt ?? clipAlmostThere;
    }

    // '+'가 경로에 포함될 때 안전하게 인코딩
    private static string EncodePlusInPath(string url)
    {
        if (string.IsNullOrEmpty(url)) return url ?? string.Empty;
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                path = path.Replace("+", "%2B");
                var builder = new UriBuilder(uri) { Path = path };
                return builder.Uri.ToString();
            }
        }
        catch { }
        return url.Replace("+", "%2B");
    }

    // URL 확장자로 AudioType 추정
    private static AudioType GuessAudioType(string url)
    {
        string u = (url ?? string.Empty).ToLowerInvariant();
        if (u.Contains(".wav")) return AudioType.WAV;
        if (u.Contains(".mp3")) return AudioType.MPEG;
        if (u.Contains(".ogg")) return AudioType.OGGVORBIS;
        return AudioType.MPEG;
    }

    private IEnumerator FetchQuestions(Action<List<QuestionDto>> onDone)
    {
        yield return FetchQuestionsWithSession(onDone);
        yield break;
#if LEGACY_STAGE41_FALLBACK
        string url = ComposeUrl($"/api/train/set?stage={UnityWebRequest.EscapeURL(stageSet)}&count={count}&stageSessionId={UnityWebRequest.EscapeURL(stageSessionId ?? string.Empty)}");
        using (var req = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                if (logVerbose)
                    Debug.LogWarning($"[Stage41] set 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={req.downloadHandler.text}");
                onDone?.Invoke(null);
                yield break;
            }
            var json = req.downloadHandler.text;
            if (logVerbose) Debug.Log($"[Stage41] set 응답: {json}");
            List<QuestionDto> list = null;
            try
            {
                var parsed = JsonUtility.FromJson<QuestionListResponse>(json);
                if (parsed != null && parsed.data != null && parsed.data.problems != null)
                    list = parsed.data.problems;
            }
            catch { }
            onDone?.Invoke(list);
        }
#endif
    }

    private IEnumerator StartStageSession()
    {
        yield return StartStageSessionWithSession();
        yield break;
#if LEGACY_STAGE41_FALLBACK
        string url = ComposeUrl($"/api/train/stage/start?stage={UnityWebRequest.EscapeURL(stageTwoPart)}&totalProblems={count}");
        if (logVerbose) Debug.Log($"[Stage41] POST {url}");
        using (var req = UnityWebRequest.PostWwwForm(url, ""))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            try
            {
                var resp = JsonUtility.FromJson<StartStageResp>(req.downloadHandler.text);
                if (resp != null && resp.data != null && !string.IsNullOrWhiteSpace(resp.data.stageSessionId))
                {
                    stageSessionId = resp.data.stageSessionId;
                    if (logVerbose) Debug.Log($"[Stage41] stage/start OK → stageSessionId={stageSessionId}");
                }
                else if (logVerbose)
                {
                    Debug.LogWarning($"[Stage41] stage/start 응답 파싱 실패 또는 stageSessionId 없음: {req.downloadHandler.text}");
                }
            }
            catch { }
        }
#endif
    }

    private IEnumerator CompleteStageSession()
    {
        yield return CompleteStageSessionWithSession();
        yield break;
#if LEGACY_STAGE41_FALLBACK
        if (string.IsNullOrWhiteSpace(stageSessionId)) yield break;
        string url = ComposeUrl($"/api/train/stage/complete?stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}");
        if (logVerbose) Debug.Log($"[Stage41] POST {url}");
        using (var req = UnityWebRequest.PostWwwForm(url, ""))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (logVerbose) Debug.Log($"[Stage41] stage/complete 응답: code={req.responseCode} body={req.downloadHandler.text}");
        }
#endif
    }

    private IEnumerator RecordAndUploadPhonemeSegment(int segmentIndex)
    {
        if (bypassVoiceUpload)
        {
            _segmentReplies.Add("*");
            _segmentCorrects.Add(true);
            yield break;
        }

        _isRecording = true;
        _isRecording = true;
        if (micIndicator) micIndicator.SetActive(true);
        // 한 프레임 먼저 켜서 표시 지연을 최소화
        yield return null;
        var clip = StartMic(recordSeconds, recordSampleRate);
        // 마이크 실제 시작까지 최대 0.5초 대기 (환경에 따라 지연 존재)
        float waitStarted = 0f;
        while (Microphone.GetPosition(null) <= 0 && waitStarted < 0.5f)
        {
            waitStarted += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(recordSeconds);
        if (micIndicator) micIndicator.SetActive(false);
        Microphone.End(null);
        _isRecording = false;
        _isRecording = false;

        var wav = WavUtility.FromAudioClip(clip);
        string targetAns0 = GetTargetPhonemeAnswer(segmentIndex);
        StartCoroutine(SendAttemptLogNew(
            problemNumber: _currentProblemNumber,
            attemptNumber: 1,
            problem: wordText ? wordText.text : string.Empty,
            answer: string.Empty,
            isCorrect: false,
            isReplyCorrect: false,
            audioUrl: string.Empty
        ));

        yield return UploadVoiceSegmentWithSession(segmentIndex, wav, targetAns0, (ok, reply) => { });
        yield break;
#if LEGACY_STAGE41_FALLBACK
        // Debug.Log($"[Stage41] check/voice answer(raw) segment={segmentIndex}: '{targetAns0}'");
        string qs = $"stageSessionId={UnityWebRequest.EscapeURL(stageSessionId ?? string.Empty)}&stage={UnityWebRequest.EscapeURL(stageTwoPart ?? string.Empty)}&problemNumber={UnityWebRequest.EscapeURL(Mathf.Max(1, _currentProblemNumber).ToString())}&answer={UnityWebRequest.EscapeURL(targetAns0)}";
        string url = ComposeUrl($"/api/train/check/voice?{qs}");
        // Debug.Log($"[Stage41] check/voice 요청 URL: {url}");
        Debug.Log($"[Stage41] check/voice 요청 URL(한글): {System.Uri.UnescapeDataString(url)}");
        // if (logVerbose) Debug.Log($"[Stage41] POST {url} (multipart audio/wav)");
        var form = new WWWForm();
        form.AddBinaryData("audio", wav, "voice.wav", "audio/wav");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            req.chunkedTransfer = false;
            // Immediately send attempt for this voice step (do not wait for response)
            // 서버는 is_correct NOT NULL이므로, 판단 전 단계는 false로 보냅니다.
            StartCoroutine(SendAttemptLogNew(
                problemNumber: _currentProblemNumber,
                attemptNumber: 1,
                problem: wordText ? wordText.text : string.Empty,
                answer: string.Empty,
                isCorrect: false,
                isReplyCorrect: false,
                audioUrl: string.Empty
            ));
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                if (logVerbose) Debug.LogWarning($"[Stage41] check/voice 실패: {req.error} (code={req.responseCode})\\nURL={url}\\nBody={req.downloadHandler.text}");
                yield break;
            }

            var respText = req.downloadHandler.text;
            if (logVerbose) Debug.Log($"[Stage41] check/voice 응답: {respText}");
            try
            {
                var parsed = JsonUtility.FromJson<VoiceReplyResp>(respText);
                string reply = parsed?.data?.reply ?? string.Empty;
                bool ok = parsed?.data?.isReplyCorrect ?? false;

                // 이미 알고 있는 정답으로 대체 (reply 무시)
                string correctPhoneme = GetTargetPhonemeAnswer(segmentIndex);
                _segmentReplies.Add(correctPhoneme);
                _segmentCorrects.Add(ok);

                // 정답이면 즉시 해당 slot에 표시
                if (ok)
                {
                    string normalized = NormalizePhoneme(correctPhoneme);
                    SetSlotText(segmentIndex, normalized);
                    SetSlotAlpha(segmentIndex, 1f);
                    _finalizedSlots[segmentIndex] = true;
                    // Debug.Log($"[Stage41] segment {segmentIndex} 정답 '{normalized}' → 슬롯에 표시됨");
                }

                if (logVerbose) Debug.Log($"[Stage41] segment {segmentIndex} → reply='{reply}', correct={ok}");
            }
            catch { }
        }
#endif
    }

    // 콜백을 통해 인식 결과와 정오를 반환하는 오버로드
    private IEnumerator RecordAndUploadPhonemeSegment(int segmentIndex, System.Action<bool, string> onDone)
    {
        if (bypassVoiceUpload)
        {
            while (_segmentReplies.Count <= segmentIndex) _segmentReplies.Add(string.Empty);
            while (_segmentCorrects.Count <= segmentIndex) _segmentCorrects.Add(false);
            _segmentReplies[segmentIndex] = "*";
            _segmentCorrects[segmentIndex] = true;
            // attempt (voice) per new spec (mock path)
            StartCoroutine(SendAttemptLogNew(
                problemNumber: _currentProblemNumber,
                attemptNumber: 1,
                problem: wordText ? wordText.text : string.Empty,
                answer: string.Empty,
                isCorrect: false,
                isReplyCorrect: true,
                audioUrl: string.Empty
            ));
            // no immediate UI fill; will fill after all segments complete
            onDone?.Invoke(true, "*");
            yield break;
        }

        if (micIndicator) micIndicator.SetActive(true);
        // 한 프레임 먼저 켜서 표시 지연을 최소화
        yield return null;
        var clip = StartMic(recordSeconds, recordSampleRate);
        // 마이크 실제 시작까지 최대 0.5초 대기
        float waitStarted2 = 0f;
        while (Microphone.GetPosition(null) <= 0 && waitStarted2 < 0.5f)
        {
            waitStarted2 += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(recordSeconds);
        if (micIndicator) micIndicator.SetActive(false);
        Microphone.End(null);

        var wav = WavUtility.FromAudioClip(clip);
        string targetAns1 = GetTargetPhonemeAnswer(segmentIndex);
        // Debug.Log($"[Stage41] check/voice answer(raw) segment={segmentIndex}: '{targetAns1}'");
        string qs = $"stageSessionId={UnityWebRequest.EscapeURL(stageSessionId ?? string.Empty)}&stage={UnityWebRequest.EscapeURL(stageTwoPart ?? string.Empty)}&problemNumber={UnityWebRequest.EscapeURL(Mathf.Max(1, _currentProblemNumber).ToString())}&answer={UnityWebRequest.EscapeURL(targetAns1)}";
        string url = ComposeUrl($"/api/train/check/voice?{qs}");
        // Debug.Log($"[Stage41] check/voice 요청 URL: {url}");
        Debug.Log($"[Stage41] check/voice 요청 URL(한글): {System.Uri.UnescapeDataString(url)}");
        // if (logVerbose) Debug.Log($"[Stage41] POST {url} (multipart audio/wav)");
        var form = new WWWForm();
        form.AddBinaryData("audio", wav, "voice.wav", "audio/wav");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            req.chunkedTransfer = false;
            // Immediately send attempt for this voice step (do not wait for response)
            // 서버 NOT NULL 제약 대응: 판단 전 값은 false로 기록
            StartCoroutine(SendAttemptLogNew(
                problemNumber: _currentProblemNumber,
                attemptNumber: 1,
                problem: wordText ? wordText.text : string.Empty,
                answer: string.Empty,
                isCorrect: false,
                isReplyCorrect: false,
                audioUrl: string.Empty
            ));
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                if (logVerbose) Debug.LogWarning($"[Stage41] check/voice 실패: {req.error} (code={req.responseCode})\\nURL={url}\\nBody={req.downloadHandler.text}");
                onDone?.Invoke(false, string.Empty);
                yield break;
            }

            var respText = req.downloadHandler.text;
            if (logVerbose) Debug.Log($"[Stage41] check/voice 응답: {respText}");
            try
            {
                var parsed = JsonUtility.FromJson<VoiceReplyResp>(respText);
                bool ok = parsed?.data?.isReplyCorrect ?? false;

                // 우리가 보낸 정답 phoneme
                string correctPhoneme = GetTargetPhonemeAnswer(segmentIndex);

                // 리스트 크기 보정
                while (_segmentReplies.Count <= segmentIndex) _segmentReplies.Add(string.Empty);
                while (_segmentCorrects.Count <= segmentIndex) _segmentCorrects.Add(false);

                // reply는 절대 사용하지 않음. isReplyCorrect true면 우리가 보낸 targetAns로 채움
                _segmentReplies[segmentIndex] = ok ? correctPhoneme : string.Empty;
                _segmentCorrects[segmentIndex] = ok;

                // 정답이면 즉시 UI에 표시
                if (ok)
                {
                    string normalized = NormalizePhoneme(correctPhoneme);
                    SetSlotText(segmentIndex, normalized);
                    SetSlotAlpha(segmentIndex, 1f);
                    _finalizedSlots[segmentIndex] = true;
                    // Debug.Log($"[Stage41] segment {segmentIndex} 정답 '{normalized}' → 슬롯 표시 완료");
                }

                if (logVerbose)
                    Debug.Log($"[Stage41] segment {segmentIndex} isReplyCorrect={ok}, target='{correctPhoneme}'");
                onDone?.Invoke(ok, parsed?.data?.reply ?? string.Empty);
            }
            catch (Exception e)
            {
                if (logVerbose) Debug.LogWarning($"[Stage41] check/voice 응답 파싱 실패: {e.Message}");
                onDone?.Invoke(false, string.Empty);
            }
        }
    }

    private IEnumerator SendAttemptLog(int problemNumber, int attemptNumber, string phonemes, string selectedAnswer, bool isCorrect, string word)
    {
        string url = ComposeUrl("/api/train/attempt");
        string ssid = stageSessionId ?? string.Empty;
        string stageStr = stageTwoPart ?? string.Empty;
        string json = "{" +
                      "\"stageSessionId\":\"" + JsonEscape(ssid) + "\"," +
                      "\"stage\":\"" + JsonEscape(stageStr) + "\"," +
                      "\"problemNumber\":" + problemNumber + "," +
                      "\"attemptNumber\":" + attemptNumber + "," +
                      "\"phonemes\":\"" + JsonEscape(phonemes ?? "") + "\"," +
                      "\"selectedAnswer\":\"" + JsonEscape(selectedAnswer ?? "") + "\"," +
                      "\"isCorrect\":" + (isCorrect ? "true" : "false") + "," +
                      "\"word\":\"" + JsonEscape(word ?? "") + "\"" +
                      "}";

        if (logVerbose) Debug.Log($"[Stage41] POST {url}\nBody={json}");
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            ApplyCommonHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (logVerbose) Debug.Log($"[Stage41] attempt 응답: code={req.responseCode} body={req.downloadHandler.text}");
        }
    }

    // New attempt payload per updated spec
    private IEnumerator SendAttemptLogNew(int problemNumber, int attemptNumber, string problem, string answer, bool? isCorrect, bool? isReplyCorrect, string audioUrl)
    {
        string url = ComposeUrl("/api/train/attempt");
        string ssid = stageSessionId ?? string.Empty;
        string stageStr = stageTwoPart ?? string.Empty;

        // 서버가 NOT NULL 제약을 가진 경우 null을 빈 문자열로 보내면 DB에서 null로 간주될 수 있으므로,
        // 클라이언트에서 항상 true/false 값을 직렬화합니다.
        bool ic = isCorrect.HasValue ? isCorrect.Value : false;
        bool irc = isReplyCorrect.HasValue ? isReplyCorrect.Value : false;

        string json = "{" +
                      "\"stageSessionId\":\"" + JsonEscape(ssid) + "\"," +
                      "\"problemNumber\":" + problemNumber + "," +
                      "\"stage\":\"" + JsonEscape(stageStr) + "\"," +
                      "\"problem\":\"" + JsonEscape(problem ?? "") + "\"," +
                      "\"answer\":\"" + JsonEscape(answer ?? "") + "\"," +
                      "\"isCorrect\":" + (ic ? "true" : "false") + "," +
                      "\"isReplyCorrect\":" + (irc ? "true" : "false") + "," +
                      "\"attemptNumber\":" + attemptNumber + "," +
                      "\"audioUrl\":\"" + JsonEscape(audioUrl ?? "") + "\"" +
                      "}";

        if (logVerbose) Debug.Log($"[Stage41] POST {url}\nBody={json}");
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            ApplyCommonHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (logVerbose) Debug.Log($"[Stage41] attempt (응답): code={req.responseCode} body={req.downloadHandler.text}");
        }
    }

    private string ComposeUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return path;
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');
        if (!path.StartsWith("/")) path = "/" + path;
        return baseUrl + path;
    }

    private void ApplyCommonHeaders(UnityWebRequest req)
    {
        if (!string.IsNullOrWhiteSpace(authToken))
        {
            var tokenTrim = authToken.Trim();
            if (tokenTrim.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                tokenTrim = tokenTrim.Substring(7).Trim();
            req.SetRequestHeader("Authorization", $"Bearer {tokenTrim}");
        }
        req.SetRequestHeader("Accept", "application/json");
    }

    // Returns normalized expected target phoneme for the segment (초성/중성/종성)
    private string GetTargetPhonemeAnswer(int segmentIndex)
    {
        if (_expectedPhonemes == null) return string.Empty;
        if (segmentIndex < 0 || segmentIndex >= _expectedPhonemes.Count) return string.Empty;
        var raw = _expectedPhonemes[segmentIndex] ?? string.Empty;
        // Normalize to standard jamo like 'ㄱ','ㅗ','ㅁ'
        return NormalizePhoneme(raw).Trim();
    }

    private AudioClip StartMic(int seconds, int sampleRate)
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Stage41] 마이크 장치가 없습니다.");
            return null;
        }
        var clip = Microphone.Start(null, false, seconds, sampleRate);
        return clip;
    }

    private static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static bool ComparePhonemeOrder(List<string> a, List<string> b)
    {
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            var aa = NormalizePhoneme((a[i] ?? string.Empty).Trim());
            var bb = NormalizePhoneme((b[i] ?? string.Empty).Trim());
            if (!string.Equals(aa, bb, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    // 현재 드롭을 받아들일 수 있는지(교정 진행 중, 해당 슬롯 차례/미완료) 공개
    public bool CanAcceptDropToSlot(int slotIndex)
    {
        if (!_awaitingUserArrangement) return false;
        if (slotIndex < 0 || slotIndex >= 3) return false;
        if (_currentCorrectionSlot >= 0 && slotIndex != _currentCorrectionSlot) return false;
        if (slotIndex < _finalizedSlots.Length && _finalizedSlots[slotIndex]) return false;
        if (_expectedPhonemes == null || slotIndex >= _expectedPhonemes.Count) return false;
        return true;
    }

    // 외부(UI)에서 슬롯 정오 확인에 사용할 수 있게 공개 메서드 제공
    public bool IsCorrectForSlot(int slotIndex, string symbol)
    {
        if (_expectedPhonemes == null || slotIndex < 0 || slotIndex >= _expectedPhonemes.Count)
            return false;
        var expected = _expectedPhonemes[slotIndex] ?? string.Empty;
        return string.Equals(NormalizePhoneme(symbol ?? string.Empty).Trim(), NormalizePhoneme(expected).Trim(), StringComparison.Ordinal);
    }

    // 서로 다른 자모 코드포인트(초성/중성/종성)과 호환 자모를 동일하게 비교하도록 정규화
    private static string NormalizePhoneme(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
            sb.Append(NormalizePhonemeChar(ch));
        // Merge common compound vowels/consonants so both 'ㅗㅏ' and 'ㅘ' normalize the same
        var comp = sb.ToString();
        comp = MergeCompoundJamo(comp);
        return comp;
    }

    private static char NormalizePhonemeChar(char ch)
    {
        switch (ch)
        {
            // 초성 ᄀ..ᄒ → 호환 자모 ㄱ..ㅎ
            case '\u1100': return '\u3131'; case '\u1101': return '\u3132'; case '\u1102': return '\u3134';
            case '\u1103': return '\u3137'; case '\u1104': return '\u3138'; case '\u1105': return '\u3139';
            case '\u1106': return '\u3141'; case '\u1107': return '\u3142'; case '\u1108': return '\u3143';
            case '\u1109': return '\u3145'; case '\u110A': return '\u3146'; case '\u110B': return '\u3147';
            case '\u110C': return '\u3148'; case '\u110D': return '\u3149'; case '\u110E': return '\u314A';
            case '\u110F': return '\u314B'; case '\u1110': return '\u314C'; case '\u1111': return '\u314D'; case '\u1112': return '\u314E';
            // 중성 ᅡ..ᅵ → ㅏ..ㅣ
            case '\u1161': return '\u314F'; case '\u1162': return '\u3150'; case '\u1163': return '\u3151'; case '\u1164': return '\u3152';
            case '\u1165': return '\u3153'; case '\u1166': return '\u3154'; case '\u1167': return '\u3155'; case '\u1168': return '\u3156';
            case '\u1169': return '\u3157'; case '\u116A': return '\u3158'; case '\u116B': return '\u3159'; case '\u116C': return '\u315A';
            case '\u116D': return '\u315B'; case '\u116E': return '\u315C'; case '\u116F': return '\u315D'; case '\u1170': return '\u315E';
            case '\u1171': return '\u315F'; case '\u1172': return '\u3160'; case '\u1173': return '\u3161'; case '\u1174': return '\u3162'; case '\u1175': return '\u3163';
            // 종성 ᆨ..ᇂ → 호환 자모
            case '\u11A8': return '\u3131'; case '\u11A9': return '\u3132'; case '\u11AA': return '\u3133';
            case '\u11AB': return '\u3134'; case '\u11AC': return '\u3135'; case '\u11AD': return '\u3136';
            case '\u11AE': return '\u3137'; case '\u11AF': return '\u3139'; case '\u11B0': return '\u313A'; case '\u11B1': return '\u313B';
            case '\u11B2': return '\u313C'; case '\u11B3': return '\u313D'; case '\u11B4': return '\u313E'; case '\u11B5': return '\u313F'; case '\u11B6': return '\u3140';
            case '\u11B7': return '\u3141'; case '\u11B8': return '\u3142'; case '\u11B9': return '\u3144'; case '\u11BA': return '\u3145'; case '\u11BB': return '\u3146';
            case '\u11BC': return '\u3147'; case '\u11BD': return '\u3148'; case '\u11BE': return '\u314A'; case '\u11BF': return '\u314B'; case '\u11C0': return '\u314C';
            case '\u11C1': return '\u314D'; case '\u11C2': return '\u314E';
        }
        return ch;
    }

    // Normalize sequences like 'ㅗㅏ' → 'ㅘ', 'ㄱㅅ' → 'ㄳ', etc.
    private static string MergeCompoundJamo(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        // Vowel compounds
        s = s.Replace("ㅗㅏ", "ㅘ"); // 3158
        s = s.Replace("ㅗㅐ", "ㅙ"); // 3159
        s = s.Replace("ㅗㅣ", "ㅚ"); // 315A
        s = s.Replace("ㅜㅓ", "ㅝ"); // 315D
        s = s.Replace("ㅜㅔ", "ㅞ"); // 315E
        s = s.Replace("ㅜㅣ", "ㅟ"); // 315F
        s = s.Replace("ㅡㅣ", "ㅢ"); // 3162

        // Final consonant compounds (batchim)
        s = s.Replace("ㄱㅅ", "ㄳ"); // 3133
        s = s.Replace("ㄴㅈ", "ㄵ"); // 3135
        s = s.Replace("ㄴㅎ", "ㄶ"); // 3136
        s = s.Replace("ㄹㄱ", "ㄺ"); // 313A
        s = s.Replace("ㄹㅁ", "ㄻ"); // 313B
        s = s.Replace("ㄹㅂ", "ㄼ"); // 313C
        s = s.Replace("ㄹㅅ", "ㄽ"); // 313D
        s = s.Replace("ㄹㅌ", "ㄾ"); // 313E
        s = s.Replace("ㄹㅍ", "ㄿ"); // 313F
        s = s.Replace("ㄹㅎ", "ㅀ"); // 3140
        s = s.Replace("ㅂㅅ", "ㅄ"); // 3144

        return s;
    }

    private bool EvaluateCorrectness()
    {
        if (_segmentCorrects == null || _segmentCorrects.Count < _expectedSegmentCount) return false;
        for (int i = 0; i < _expectedSegmentCount; i++)
        {
            if (i >= _segmentCorrects.Count || !_segmentCorrects[i]) return false;
        }
        return true;
    }

    // 오답 슬롯에 맞춰 보기 상자(자음/모음) 표시
    private void ShowChoicePanelsForWrong()
    {
        bool wrongInitial = (_expectedSegmentCount >= 1) && !(_segmentCorrects.Count >= 1 && _segmentCorrects[0]);
        bool wrongMedial  = (_expectedSegmentCount >= 2) && !(_segmentCorrects.Count >= 2 && _segmentCorrects[1]);
        bool wrongFinal   = (_expectedSegmentCount >= 3) && !(_segmentCorrects.Count >= 3 && _segmentCorrects[2]);

        bool showConsonants = wrongInitial || wrongFinal;
        bool showVowels = wrongMedial;
        bool showAny = showConsonants || showVowels;

        if (logVerbose)
            Debug.Log($"[Stage41] ShowChoicePanels: wrongInit={wrongInitial}, wrongMedial={wrongMedial}, wrongFinal={wrongFinal}, any={showAny}");

        if (choicesContainer)
        {
            choicesContainer.SetActive(showAny);
            var rt = choicesContainer.GetComponent<RectTransform>();
            if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
        if (consonantChoicesContainer)
        {
            consonantChoicesContainer.SetActive(showConsonants);
            var rtC = consonantChoicesContainer.GetComponent<RectTransform>();
            if (rtC) LayoutRebuilder.ForceRebuildLayoutImmediate(rtC);
        }
        if (vowelChoicesContainer)
        {
            vowelChoicesContainer.SetActive(showVowels);
            var rtV = vowelChoicesContainer.GetComponent<RectTransform>();
            if (rtV) LayoutRebuilder.ForceRebuildLayoutImmediate(rtV);
        }
    }

    // 모든 필요한 슬롯(초/중/종)이 완료됐는지 확인
    private bool AreAllSlotsFinalized()
    {
        int needed = Mathf.Max(0, _expectedSegmentCount);
        for (int i = 0; i < needed && i < _finalizedSlots.Length; i++)
        {
            if (!_finalizedSlots[i]) return false;
        }
        return needed > 0;
    }

    // 틀린 슬롯을 순차적으로 교정하는 코루틴 (초성→중성→종성)
    private IEnumerator CorrectionFlow()
    {
        // 슬롯별 시도횟수 초기화
        for (int i = 0; i < _attemptsPerSlot.Length; i++) _attemptsPerSlot[i] = 0;
        // 순서대로 대상 슬롯 탐색
        for (int slot = 0; slot < _expectedSegmentCount; slot++)
        {
            if (_finalizedSlots[slot]) continue; // 이미 맞춘 슬롯은 건너뜀
            _currentCorrectionSlot = slot;
            // 포커스 & 안내 멘트
            if (slot == 0 && choseongBox) FocusBox(choseongBox);
            else if (slot == 1 && jungseongBox) FocusBox(jungseongBox);
            else if (slot == 2 && jongseongBox) FocusBox(jongseongBox);
            // 현재 슬롯에 맞는 보기 상자만 노출
            ShowChoicePanelsForSlot(slot);
            _awaitingUserArrangement = true; // 드래그를 즉시 허용
            yield return PlayClip(clipFindCorrect); // [4.1.10]
            yield return ReplayCurrentWord();
            // 사용자가 올바르게 배치하거나 3회 실패로 자동완료될 때까지 대기
            while (_awaitingUserArrangement) yield return null;
        }
        _currentCorrectionSlot = -1;
        FocusBox(null);
        // 모든 교정 종료 시 보기 숨김
        HideChoicePanels();
    }

    // 현재 교정 중인 슬롯에 맞춰 보기 상자를 표시
    // slotIndex: 0=초성(자음), 1=중성(모음), 2=종성(자음)
    private void ShowChoicePanelsForSlot(int slotIndex)
    {
        bool showConsonants = (slotIndex == 0) || (slotIndex == 2);
        bool showVowels = (slotIndex == 1);
        bool showAny = showConsonants || showVowels;

        if (choicesContainer) choicesContainer.SetActive(showAny);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(showConsonants);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(showVowels);

        // 레이아웃 갱신
        if (choicesContainer)
        {
            var rt = choicesContainer.GetComponent<RectTransform>();
            if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
        if (consonantChoicesContainer)
        {
            var rtC = consonantChoicesContainer.GetComponent<RectTransform>();
            if (rtC) LayoutRebuilder.ForceRebuildLayoutImmediate(rtC);
        }
        if (vowelChoicesContainer)
        {
            var rtV = vowelChoicesContainer.GetComponent<RectTransform>();
            if (rtV) LayoutRebuilder.ForceRebuildLayoutImmediate(rtV);
        }
    }

    // 보기 상자 모두 숨김
    private void HideChoicePanels()
    {
        if (choicesContainer) choicesContainer.SetActive(false);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
    }

    private void EnsureGameplayUiVisible()
    {
        if (choseongBox) choseongBox.SetActive(true);
        if (jungseongBox) jungseongBox.SetActive(true);
        if (jongseongBox) jongseongBox.SetActive(true);
        // choices remain hidden until the stage logic explicitly shows them
    }

    private void SetGameplaySlotsActive(bool active)
    {
        if (choseongBox) choseongBox.SetActive(active);
        if (jungseongBox) jungseongBox.SetActive(active);
        if (jongseongBox) jongseongBox.SetActive(active);
    }

    // 드래그 타일이 슬롯에 떨어졌을 때 호출 (PhonemeSlotUI에서 연결)
        public void OnUserDrop(int slotIndex, string symbol)
    {
        if (!_awaitingUserArrangement) return; // 드롭을 기다리는 단계가 아님
        // 현재 교정 대상 슬롯이 지정된 경우, 그 슬롯만 허용
        if (_currentCorrectionSlot >= 0 && slotIndex != _currentCorrectionSlot) return;
        // 유효성
        if (slotIndex < 0 || slotIndex >= 3) return;
        if (_expectedPhonemes == null || slotIndex >= _expectedPhonemes.Count) return;
        string expected = _expectedPhonemes[slotIndex];
        bool correct = string.Equals(NormalizePhoneme((symbol ?? string.Empty).Trim()), NormalizePhoneme((expected ?? string.Empty).Trim()), StringComparison.Ordinal);

        // attempt 로깅 (드래그 교정)
        if (_currentCorrectionSlot >= 0)
            _attemptsPerSlot[_currentCorrectionSlot] = Mathf.Clamp(_attemptsPerSlot[_currentCorrectionSlot] + 1, 1, 3);
        _attemptCountForProblem++;
        int attemptNum = (_currentCorrectionSlot >= 0) ? _attemptsPerSlot[_currentCorrectionSlot] : Mathf.Min(_attemptCountForProblem, 3);
        StartCoroutine(SendAttemptLogNew(
            problemNumber: _currentProblemNumber,
            attemptNumber: attemptNum,
            problem: wordText ? wordText.text : string.Empty,
            answer: symbol,
            isCorrect: correct,
            isReplyCorrect: false,
            audioUrl: string.Empty
        ));

        if (!correct)
        {
            // 3회 실패 시: 위로 멘트 [4.1.13] 재생을 끝까지 듣고 자동 정답 처리
            if (_currentCorrectionSlot >= 0 && _attemptsPerSlot[_currentCorrectionSlot] >= 3)
            {
                StartCoroutine(Co_FinalizeSlotAfterComfort(slotIndex, expected));
            }
            else
            {
                StartCoroutine(PlayClipThenReplayWord(clipTryAgain)); // [4.1.12]
            }
            return;
        }

        // 정답이면 해당 슬롯 채우기
        SetSlotText(slotIndex, NormalizePhoneme(expected));
        SetSlotAlpha(slotIndex, 1f);
        _finalizedSlots[slotIndex] = true;
        StartCoroutine(PlayClip(clipGoodThatsIt)); // [4.1.11]
        if (_currentCorrectionSlot >= 0)
            _awaitingUserArrangement = false;

        // 모두 완료 시 즉시 다음으로 진행
        if (AreAllSlotsFinalized())
        {
            _awaitingUserArrangement = false;
            SetAllBoxAlpha(1f);
            StartCoroutine(PlayClip(clipGreat)); // [4.1.5]
        }

    } 
    
    // 3회 실패 후 위로 멘트(4.1.13)를 재생한 뒤 해당 슬롯을 자동 정답 처리
    private IEnumerator Co_FinalizeSlotAfterComfort(int slotIndex, string expectedRaw)
    {
        yield return PlayClip(clipFinalizeSpell); // [4.1.13]
        SetSlotText(slotIndex, NormalizePhoneme(expectedRaw));
        SetSlotAlpha(slotIndex, 1f);
        _finalizedSlots[slotIndex] = true;
        _awaitingUserArrangement = false;
    }

    private IEnumerator RunRemedialSequence()
    {
        if (_supplementController == null)
            yield break;
        yield return _supplementController.RunRemedialSequence();
    }
    #endregion

    private void UpdateSupplementQuestionsStub(IEnumerable<QuestionDto> source)
    {
        // placeholder stub retained intentionally
    }

    private void HandleTutorialClearAllSlots()
    {
        SetSlotText(0, string.Empty);
        SetSlotText(1, string.Empty);
        SetSlotText(2, string.Empty);
        SetSlotAlpha(0, dimAlpha);
        SetSlotAlpha(1, dimAlpha);
        SetSlotAlpha(2, dimAlpha);

        _segmentReplies.Clear();
        _segmentCorrects.Clear();
        _segmentReplyCandidates.Clear();
        _expectedPhonemes.Clear();
        Array.Clear(_finalizedSlots, 0, _finalizedSlots.Length);

        HandleTutorialClearAllSlotObjects();

        if (logVerbose)
        {
            // 튜토리얼 슬롯이 있으면 그것을, 없으면 본 훈련 슬롯을 로그에 표시
            var tutorialChoseong = tutorialChoseongBox != null ? tutorialChoseongBox : choseongBox;
            var tutorialJungseong = tutorialJungseongBox != null ? tutorialJungseongBox : jungseongBox;
            var tutorialJongseong = tutorialJongseongBox != null ? tutorialJongseongBox : jongseongBox;
            Debug.Log($"[Stage41][Tutorial] ClearAllSlots → repliesCount={_segmentReplies.Count}, expectedCount={_expectedPhonemes.Count}, choseongChildren={GetChildCount(tutorialChoseong)}, jungseongChildren={GetChildCount(tutorialJungseong)}, jongseongChildren={GetChildCount(tutorialJongseong)}");
        }
    }

    private int GetChildCount(GameObject slot)
    {
        if (slot == null) return 0;
        return slot.transform.childCount;
    }
}
