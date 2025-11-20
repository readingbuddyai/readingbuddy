using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Stage.UI;

/// <summary>
/// Stage 2.2 진행 컨트롤러 (마법 돌 퍼즐 - 확장)
/// Stage11/Stage12와 동일한 책임 분리를 적용하여
/// 세션/오디오/문항 관리 로직을 공통 컨트롤러에 위임한다.
/// </summary>
public class Stage30Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "";
    [Tooltip("문제 세트 조회에 사용할 stage 값 (예: 1.2.2)")]
    public string stage = "1.2.2";
    [Tooltip("stage/start · check/voice 등 2단계 스테이지 값 (예: 2.2)")]
    public string stageTwoPart = "2.2";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";

    [Header("세션")]
    [Tooltip("stage/start 응답의 stageSessionId. 미설정 시 자동 발급을 시도합니다.")]
    public string stageSessionId = "";
    [Tooltip("/api/train/stage/start 요청을 건너뛰고 문제 GET만 진행합니다.")]
    public bool bypassStartRequest = false;
    [Tooltip("음성 녹음 및 /api/train/check/voice 업로드를 건너뜁니다.")]
    public bool bypassVoiceUpload = false;
    [Tooltip("자세한 로그를 출력합니다.")]
    public bool verboseLogging = false;

    [Header("UI 참조")]
    public Text progressText;
    public TMP_Text progressTextTMP;
    public TMP_Text wordLabel;

    [Header("Intro Tutorial")]
    public StageTutorialProfile tutorialProfile;
    public Sprite introTutorialImage;
    public List<StageTutorialController.IntroOption> introOptions = new List<StageTutorialController.IntroOption>();
    public StageTutorialController.IntroOptionCursor introOptionCursor;
    public PanelAnimator introTutorialPanelAnimator;
    public GameObject introTutorialPanel;
    public GameObject guide3DCharacter;
    [Header("Intro Tutorial Controls")]
    public bool requireTriggerAfterTutorial = false;
    [Range(0.05f, 1f)] public float tutorialTriggerThreshold = 0.6f;
    public KeyCode tutorialFallbackKey = KeyCode.Space;
    [Min(0f)] public float tutorialClipGapSeconds = 0.9f;

    [Header("Mic Indicator")]
    [Tooltip("[1.1.4] 종료 직후부터 녹음 3초 동안 표시될 마이크 아이콘 오브젝트")]
    public GameObject micIndicator;

    [Header("오디오 재생")]
    public AudioSource audioSource;
    public AudioClip sfxStart;
    public AudioClip sfxNext;

    [Header("도입 대사")]
    public AudioClip clipGreeting;
    public AudioClip clipLessonIntro;
    public AudioClip clipRecallPreviousLesson;
    public AudioClip clipExplainPhonemeMagic;
    public AudioClip clipInstructionListenCarefully;
    public AudioClip clipInstructionHiddenPieces;
    public AudioClip clipInstructionMoveStones;

    [Header("문항 흐름 대사")]
    public AudioClip clipTeacherChant;
    public AudioClip clipPromptYourTurn;
    public AudioClip clipPromptRepeat;
    public AudioClip clipPraisePrecision;
    public AudioClip clipPromptCountStones;
    public AudioClip clipCorrectCelebration;
    public AudioClip clipRetryEncourageAttempt;

    [Header("마법 돌 퍼즐")]
    public GameObject stoneBoard;
    public TMP_Text countdownText;
    public float countdownSeconds = 10f;
    public int maxStoneAttempts = 2;

    [Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    public IntUnityEvent onStoneRoundBegin;
    public UnityEvent onStoneRoundEnd;
    [Tooltip("퍼즐 시작 시 자동으로 카운트다운을 시작할지 여부")]
    public bool autoStartCountdown = true;

    [Header("발음 피드백")]
    public AudioClip clipFeedbackLackSense;
    public AudioClip clipFeedbackRetryPrompt;
    public AudioClip clipFeedbackTryAgainFirst;
    public AudioClip clipFeedbackTryAgainSecond;
    public AudioClip clipFeedbackGreatJob;
    public AudioClip clipFinalEncouragement;

    [Header("Fonts")]
    public Font uiFont;
    public TMP_FontAsset tmpFont;

    [Header("End Modal (Stage Complete)")]
    public Button againButtonPrefab;
    public Button lobbyButtonPrefab;
    public Button optionButtonPrefab;
    public Vector2 optionButtonPreferredSize = new Vector2(1200f, 600f);
    public Vector2 endModalButtonSize = new Vector2(600f, 300f);

    [Header("튜토리얼 UI")]
    public RectTransform tutorialOptionsContainer;
    public TMP_Text tutorialOptionWordText;
    [Min(0f)] public float tutorialStoneMoveSeconds = 0.6f;
    public AnimationCurve tutorialStoneMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("마이크 설정")]
    public int recordSeconds = 3;
    public int recordSampleRate = 44100;

    private StageSessionController _sessionController;
    private StageAudioController _audioController;
    private StageAudioDependencies _audioDependencies;
    private readonly StageQuestionController<Problem> _questionController = new StageQuestionController<Problem>();
    private StageTutorialController _tutorialController;
    private StageTutorialDependencies _tutorialDependencies;
    private Coroutine _stoneCountdownCoroutine;

    private bool _waitingForStoneCount;
    private int? _pendingStoneCount;
    private int _currentProblemNumber;

    private readonly List<PronunciationFeedback> _accumulatedFeedback = new List<PronunciationFeedback>();
    private readonly HashSet<string> _feedbackKeys = new HashSet<string>();
    private string _tutorialOptionWordCache = string.Empty;
    private bool _tutorialOptionUseWordLabel;

    private void Start()
    {
        StartCoroutine(InitializeWithAuth());
    }

    private IEnumerator InitializeWithAuth()
    {
        Debug.Log("[Stage30] Waiting for AuthManager...");

        float timeout = 5f;
        float elapsed = 0f;
        while (AuthManager.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (AuthManager.Instance == null)
        {
            Debug.LogError("[Stage30] ⚠️ AuthManager.Instance is null after timeout! Returning to Home.");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(SceneId.Home);
            yield break;
        }

        Debug.Log("[Stage30] AuthManager found!");

        if (!AuthManager.Instance.IsLoggedIn())
        {
            Debug.LogError("[Stage30] ⚠️ User is not logged in! Returning to Home.");
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(SceneId.Home);
            yield break;
        }

        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);
        Debug.Log($"[Stage30] ✅ baseUrl 설정 완료: {baseUrl}");

        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
        {
            Debug.Log("[Stage30] 🔑 AuthManager에서 토큰 가져오기 시작...");
            authToken = AuthManager.Instance.GetAccessToken();
            
            if (string.IsNullOrWhiteSpace(authToken))
            {
                Debug.LogError("[Stage30] ❌ AuthManager에서 토큰을 가져왔지만 토큰이 비어있습니다. 403 에러가 발생할 수 있습니다.");
                Debug.LogError($"[Stage30] 디버깅: authToken == null = {authToken == null}, empty = {string.IsNullOrEmpty(authToken)}, whitespace = {string.IsNullOrWhiteSpace(authToken)}");
            }
            else
            {
                string preview = authToken.Length > 20
                    ? $"{authToken.Substring(0, 10)}...{authToken.Substring(authToken.Length - 10)}"
                    : authToken;
                Debug.Log($"[Stage30] ✅ Access token retrieved from AuthManager (len={authToken.Length}, preview={preview})");
            }
        }
        else
        {
            Debug.LogWarning($"[Stage30] ⚠️ AuthManager 없음 또는 로그인 안 됨: Instance={AuthManager.Instance != null}, IsLoggedIn={AuthManager.Instance?.IsLoggedIn() ?? false}");
            authToken = EnvConfig.ResolveAuthToken(authToken);
            if (string.IsNullOrWhiteSpace(authToken))
            {
                Debug.LogWarning("[Stage30] ❌ EnvConfig에서도 토큰을 찾지 못했습니다. 403 에러가 발생할 수 있습니다.");
            }
            else
            {
                string preview = authToken.Length > 20
                    ? $"{authToken.Substring(0, 10)}...{authToken.Substring(authToken.Length - 10)}"
                    : authToken;
                Debug.Log($"[Stage30] ✅ Using authToken from EnvConfig (fallback, len={authToken.Length}, preview={preview})");
            }
        }

        // ConfigureSessionController 호출 직전 최종 확인
        Debug.Log($"[Stage30] 🔍 ConfigureSessionController 호출 직전: authToken null={authToken == null}, empty={string.IsNullOrEmpty(authToken)}, len={authToken?.Length ?? 0}");
        ConfigureSessionController();
        Debug.Log($"[Stage30] ✅ ConfigureSessionController 완료");
        ConfigureAudioController();
        ConfigureTutorialController();
        _tutorialController?.PrepareForStageStart();
        EnsureStage30DropZones();
        ResetStoneUI();
        if (micIndicator)
            micIndicator.SetActive(false);
        StartCoroutine(RunStage());
    }

    private void ConfigureSessionController()
    {
        if (_sessionController == null)
            _sessionController = new StageSessionController();

        _sessionController.Configure(baseUrl, authToken);
        _sessionController.Log = verboseLogging ? (Action<string>)Debug.Log : null;
        _sessionController.LogWarning = Debug.LogWarning;
        _sessionController.LogError = Debug.LogError;
    }

    private StageSessionController GetSessionController()
    {
        if (_sessionController == null)
            _sessionController = new StageSessionController();

        // 매번 최신 baseUrl과 authToken으로 업데이트 (Stage11Controller와 동일한 방식)
        _sessionController.Configure(baseUrl, authToken);
        _sessionController.Log = verboseLogging ? (Action<string>)Debug.Log : null;
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
        _audioDependencies.Log = verboseLogging ? (Action<string>)Debug.Log : null;
        _audioDependencies.LogWarning = Debug.LogWarning;

        _audioController.Initialize(_audioDependencies);
    }

    private void EnsureStage30DropZones()
    {
        if (stoneBoard == null)
            return;

        var legacyZones = stoneBoard.GetComponentsInChildren<StoneDropZone>(true);
        foreach (var legacy in legacyZones)
        {
            if (legacy == null)
                continue;

            legacy.GetSharedConfiguration(out var slotParentRef, out var slotNumberRef, out var stoneSlotsParentRef);

            var stage30Zone = legacy.gameObject.GetComponent<Stage30StoneDropZone>();
            if (stage30Zone == null)
                stage30Zone = legacy.gameObject.AddComponent<Stage30StoneDropZone>();

            stage30Zone.SetSharedConfiguration(slotParentRef, slotNumberRef, stoneSlotsParentRef);
            stage30Zone.SetStageControllerReference(this);

            Destroy(legacy);
        }

        var stage30Zones = stoneBoard.GetComponentsInChildren<Stage30StoneDropZone>(true);
        foreach (var zone in stage30Zones)
        {
            if (zone != null)
                zone.SetStageControllerReference(this);
        }
    }

    private IEnumerator RunStage()
    {
        ConfigureTutorialController();
        _tutorialController?.ResetAfterStageRestart();

        var sessionController = GetSessionController();

        if (!bypassStartRequest && string.IsNullOrWhiteSpace(stageSessionId))
        {
            StageSessionController.StageStartResult startResult = null;
            yield return sessionController.StartStageSession(
                string.IsNullOrWhiteSpace(stageTwoPart) ? stage : stageTwoPart,
                Mathf.Max(1, count),
                r => startResult = r);

            if (startResult == null || !startResult.Success || string.IsNullOrWhiteSpace(startResult.StageSessionId))
            {
                Debug.LogError("[Stage30] stage/start 호출에 실패했습니다. 진행을 중단합니다.");
                yield break;
            }

            stageSessionId = startResult.StageSessionId;
            if (verboseLogging)
                Debug.Log($"[Stage30] stageSessionId 발급: {stageSessionId}");
        }

        if (sfxStart) yield return PlayClip(sfxStart);
        if (_tutorialController != null)
        {
            yield return _tutorialController.RunIntroSequence();
            yield return _tutorialController.RunIntroTutorial();
        }
        else
        {
            yield return RunIntroSequence();
        }

        List<Problem> problems = null;
        yield return FetchProblems(sessionController, result => problems = result);

        if (problems == null || problems.Count == 0)
        {
            Debug.LogError("[Stage30] 문제 데이터를 가져오지 못했습니다.");
            yield break;
        }

        _questionController.SetQuestions(problems);

        int totalQuestions = _questionController.Count;
        for (int i = 0; i < totalQuestions; i++)
        {
            _questionController.SetCurrentQuestionNumber(i + 1);
            var problem = _questionController.GetQuestionByNumber(i + 1);
            yield return RunOneProblem(i + 1, totalQuestions, problem);

            if (i < totalQuestions - 1 && sfxNext)
                yield return PlayClip(sfxNext);
        }

        yield return ProcessAccumulatedFeedback();

        if (!bypassStartRequest && !string.IsNullOrWhiteSpace(stageSessionId))
        {
            StageSessionController.StageCompleteResult completeResult = null;
            yield return sessionController.CompleteStageSession(stageSessionId, r => completeResult = r);

            if (completeResult != null && completeResult.VoiceResultTokens.Count > 0)
            {
                foreach (var token in completeResult.VoiceResultTokens)
                    CollectFeedback(token);
                yield return ProcessAccumulatedFeedback();
            }
        }

        if (clipFinalEncouragement)
            yield return PlayClip(clipFinalEncouragement);

        // 종료 모달 표시
        ShowEndModal();
    }

    private void ShowEndModal()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (!canvas)
        {
            Debug.LogWarning("[Stage30] Canvas를 찾을 수 없습니다.");
            return;
        }

        var overlay = new GameObject("EndModal", typeof(RectTransform), typeof(Image));
        overlay.layer = canvas.gameObject.layer;
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.SetParent(canvas.transform, false);
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.pivot = new Vector2(0.5f, 0.5f);
        overlayRt.anchoredPosition = Vector2.zero;
        overlayRt.sizeDelta = Vector2.zero;
        var overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImage.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.layer = canvas.gameObject.layer;
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.SetParent(overlay.transform, false);
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(2200f, 1500f);
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.15f, 0.2f, 0.28f, 0.95f);

        var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.layer = canvas.gameObject.layer;
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.SetParent(panel.transform, false);
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -80f);
        titleRt.sizeDelta = new Vector2(1000f, 150f);
        var titleText = title.GetComponent<Text>();
        titleText.text = "학습이 끝났어요!";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 100;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.font = uiFont ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        Vector2 btnSize = (endModalButtonSize.sqrMagnitude > 0f) ? endModalButtonSize : optionButtonPreferredSize;
        float gap = 40f;

        Button ResolveButton(Button preferred, string[] resourcePaths, out bool isCustom)
        {
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

        bool againCustom;
        var resolvedAgain = ResolveButton(againButtonPrefab, new[] {"againbutton", "UI/againbutton", "Images/againbutton"}, out againCustom);
        if (resolvedAgain == null)
        {
            Debug.LogError("[Stage30] 다시 학습하기 버튼을 생성할 수 없습니다.");
            Destroy(overlay);
            return;
        }

        var btn1 = Instantiate(resolvedAgain, panel.transform as RectTransform);
        var btn1Rt = btn1.GetComponent<RectTransform>();
        btn1Rt.anchorMin = new Vector2(0.5f, 0.5f);
        btn1Rt.anchorMax = new Vector2(0.5f, 0.5f);
        btn1Rt.pivot = new Vector2(1f, 0.5f);
        btn1Rt.sizeDelta = btnSize;
        btn1Rt.anchoredPosition = new Vector2(-gap * 0.5f, -100f);
        if (!againCustom)
        {
            var txt1 = btn1.GetComponentInChildren<Text>();
            var tmp1 = btn1.GetComponentInChildren<TMP_Text>();
            if (txt1)
            {
                txt1.text = "다시 학습하기";
                if (uiFont) txt1.font = uiFont;
            }
            else if (tmp1)
            {
                tmp1.text = "다시 학습하기";
                if (tmpFont) tmp1.font = tmpFont;
            }
        }
        btn1.onClick.AddListener(() =>
        {
            Destroy(overlay);
            RestartStage();
        });

        bool lobbyCustom;
        var resolvedLobby = ResolveButton(lobbyButtonPrefab, new[] {"lobbybutton", "UI/lobbybutton", "Images/lobbybutton"}, out lobbyCustom);
        if (resolvedLobby == null)
        {
            Debug.LogError("[Stage30] 로비로 나가기 버튼을 생성할 수 없습니다.");
            Destroy(overlay);
            return;
        }

        var btn2 = Instantiate(resolvedLobby, panel.transform as RectTransform);
        var btn2Rt = btn2.GetComponent<RectTransform>();
        btn2Rt.anchorMin = new Vector2(0.5f, 0.5f);
        btn2Rt.anchorMax = new Vector2(0.5f, 0.5f);
        btn2Rt.pivot = new Vector2(0f, 0.5f);
        btn2Rt.sizeDelta = btnSize;
        btn2Rt.anchoredPosition = new Vector2(gap * 0.5f, -100f);
        if (!lobbyCustom)
        {
            var txt2 = btn2.GetComponentInChildren<Text>();
            var tmp2 = btn2.GetComponentInChildren<TMP_Text>();
            if (txt2)
            {
                txt2.text = "로비로 나가기";
                if (uiFont) txt2.font = uiFont;
            }
            else if (tmp2)
            {
                tmp2.text = "로비로 나가기";
                if (tmpFont) tmp2.font = tmpFont;
            }
        }
        btn2.onClick.AddListener(() =>
        {
            Destroy(overlay);
            GoToLobby();
        });
    }

    private void RestartStage()
    {
        StopAllCoroutines();
        _accumulatedFeedback.Clear();
        _feedbackKeys.Clear();
        _waitingForStoneCount = false;
        _pendingStoneCount = null;
        _currentProblemNumber = 0;
        ResetStoneUI();
        stageSessionId = string.Empty;
        ConfigureTutorialController();
        _tutorialController?.ResetAfterStageRestart();
        EnsureStage30DropZones();
        StartCoroutine(RunStage());
    }

    private void GoToLobby()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneId.Lobby);
            return;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneId.Lobby);
    }

    private IEnumerator RunIntroSequence()
    {
        if (clipGreeting) yield return PlayClip(clipGreeting);
        if (clipLessonIntro) yield return PlayClip(clipLessonIntro);
        if (clipRecallPreviousLesson) yield return PlayClip(clipRecallPreviousLesson);
        if (clipExplainPhonemeMagic) yield return PlayClip(clipExplainPhonemeMagic);
        if (clipInstructionListenCarefully) yield return PlayClip(clipInstructionListenCarefully);
        if (clipInstructionHiddenPieces) yield return PlayClip(clipInstructionHiddenPieces);
        if (clipInstructionMoveStones) yield return PlayClip(clipInstructionMoveStones);
    }

    private IEnumerator RunOneProblem(int index, int total, Problem problem)
    {
        ResetStoneUI();
        _currentProblemNumber = index;
        UpdateProgress(index, total, problem);

        if (wordLabel)
        {
            wordLabel.enableWordWrapping = false;
            wordLabel.text = NormalizeWordLabel(problem?.problemWord);
            wordLabel.gameObject.SetActive(true);
        }

        if (clipTeacherChant) yield return PlayClip(clipTeacherChant);
        if (!string.IsNullOrEmpty(problem?.problemVoiceUrl))
            yield return PlayVoiceUrl(problem.problemVoiceUrl);

        if (clipPromptYourTurn) yield return PlayClip(clipPromptYourTurn);
        if (clipPromptRepeat) yield return PlayClip(clipPromptRepeat);

        if (!bypassVoiceUpload)
        {
            if (micIndicator) micIndicator.SetActive(true);
            yield return RecordAndUpload(problem, index);
            if (micIndicator) micIndicator.SetActive(false);
        }
        else
        {
            if (micIndicator) micIndicator.SetActive(true);
            yield return new WaitForSeconds(recordSeconds);
            if (micIndicator) micIndicator.SetActive(false);
        }

        if (clipPraisePrecision) yield return PlayClip(clipPraisePrecision);

        if (clipPromptCountStones) yield return PlayClip(clipPromptCountStones);
        if (!string.IsNullOrEmpty(problem?.problemVoiceUrl))
            yield return PlayVoiceUrl(problem.problemVoiceUrl);

        yield return RunStoneRound(problem, index);
    }

    private void UpdateProgress(int index, int total, Problem problem)
    {
        string label = $"{index}/{total}";
        if (!string.IsNullOrWhiteSpace(problem?.problemWord))
            label += $"\n{problem.problemWord}";

        if (progressText)
            progressText.text = label;

        if (progressTextTMP)
        {
            progressTextTMP.enableAutoSizing = false;
            progressTextTMP.overflowMode = TextOverflowModes.Overflow;
            progressTextTMP.text = label;
        }
    }

    private IEnumerator RunStoneRound(Problem problem, int problemNumber)
    {
        int expectedCount = problem?.answerCnt ?? 0;
        int attempts = 0;
        bool solved = false;
        var sessionController = GetSessionController();

        while (attempts < Mathf.Max(1, maxStoneAttempts) && !solved)
        {
            ResetStonePositions();
            _pendingStoneCount = null;
            _waitingForStoneCount = true;

            if (stoneBoard)
                stoneBoard.SetActive(true);
            if (countdownText)
            {
                countdownText.text = string.Empty;
                countdownText.gameObject.SetActive(false);
            }

            onStoneRoundBegin?.Invoke(expectedCount);

            while (_waitingForStoneCount)
                yield return null;

            int submitted = _pendingStoneCount ?? 0;
            _pendingStoneCount = null;

            if (stoneBoard)
                stoneBoard.SetActive(false);
            onStoneRoundEnd?.Invoke();

            int attemptNumber = attempts + 1;
            bool isCorrect = submitted == expectedCount;

            yield return sessionController.LogAttempt(
                stageSessionId,
                string.IsNullOrWhiteSpace(stageTwoPart) ? stage : stageTwoPart,
                Mathf.Max(1, problemNumber),
                attemptNumber,
                submitted.ToString(),
                isCorrect,
                problem?.problemWord ?? string.Empty,
                expectedCount.ToString(),
                attemptNumber > 1,
                null);

            if (isCorrect)
            {
                solved = true;
                if (clipCorrectCelebration) yield return PlayClip(clipCorrectCelebration);
            }
            else
            {
                if (clipRetryEncourageAttempt) yield return PlayClip(clipRetryEncourageAttempt);
                attempts++;
            }
        }
    }

    public void ReportStoneCount(int count)
    {
        if (!_waitingForStoneCount) return;
        _pendingStoneCount = count;
        if (verboseLogging)
            Debug.Log($"[Stage30] ReportStoneCount → pending={_pendingStoneCount}");
    }

    public void ConfirmStoneCount()
    {
        if (!_waitingForStoneCount)
        {
            if (verboseLogging)
                Debug.Log("[Stage30] ConfirmStoneCount 호출 무시 (waiting=false)");
            return;
        }

        if (!_pendingStoneCount.HasValue)
        {
            Debug.LogWarning("[Stage30] 아직 보고된 돌 개수가 없습니다. 드롭 후 버튼을 눌러 주세요.");
            return;
        }

        _waitingForStoneCount = false;
    }

    private IEnumerator FetchProblems(StageSessionController sessionController, Action<List<Problem>> onCompleted)
    {
        StageSessionController.QuestionSetResult result = null;
        yield return sessionController.FetchQuestionSet(stage, count, stageSessionId, r => result = r);

        if (result == null || !result.Success)
        {
            Debug.LogError($"[Stage30] 문제 요청 실패: code={result?.ResponseCode}");
            onCompleted?.Invoke(null);
            yield break;
        }

        var problems = ParseProblemResponse(result.RawBody);
        onCompleted?.Invoke(problems);
    }

    private List<Problem> ParseProblemResponse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        ProblemListResponse parsed = null;
        try
        {
            parsed = JsonUtility.FromJson<ProblemListResponse>(rawJson);
            if (verboseLogging)
                Debug.Log($"[Stage30] 문제 응답 JSON: {rawJson}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Stage30] 문제 파싱 실패: {ex.Message}\nJSON={rawJson}");
            return null;
        }

        if (parsed?.data == null || parsed.data.problems == null || parsed.data.problems.Count == 0)
        {
            Debug.LogError("[Stage30] 문제 데이터가 비어 있습니다.");
            return null;
        }

        var sessionIdFromResponse = !string.IsNullOrEmpty(parsed.data.stageSessionId)
            ? parsed.data.stageSessionId
            : parsed.data.sessionId;

        if (!string.IsNullOrEmpty(sessionIdFromResponse))
        {
            stageSessionId = sessionIdFromResponse;
            if (verboseLogging)
                Debug.Log($"[Stage30] stageSessionId 갱신: {stageSessionId}");
        }

        if (!string.IsNullOrEmpty(stageSessionId))
        {
            foreach (var problem in parsed.data.problems)
                problem.sessionId = stageSessionId;
        }

        return parsed.data.problems;
    }

    private IEnumerator RecordAndUpload(Problem problem, int problemNumber)
    {
        var clip = StartMic(recordSeconds, recordSampleRate);
        yield return new WaitForSeconds(recordSeconds);

        if (clip == null)
        {
            Debug.LogWarning("[Stage30] 녹음된 오디오가 없습니다.");
            yield break;
        }

        Microphone.End(null);
        var wav = WavUtility.FromAudioClip(clip);

        string stageForUpload = GetStageForVoiceUpload();
        var sessionController = GetSessionController();
        // Stage41처럼 정답 값(problemWord)을 answer 파라미터로 전달
        // Stage30은 problemWord가 유니코드 형식일 수 있으므로 NormalizeWordLabel로 변환
        string rawProblemWord = problem != null ? (problem.problemWord ?? string.Empty) : string.Empty;
        string answerValue = NormalizeWordLabel(rawProblemWord);
        yield return sessionController.CheckVoice(
            stageSessionId,
            stageForUpload,
            Mathf.Max(1, problemNumber),
            answerValue,
            wav,
            result =>
            {
                if (result != null && !string.IsNullOrWhiteSpace(result.RawBody))
                {
                    // 응답 파싱하여 reply 추출
                    try
                    {
                        var parsed = JsonUtility.FromJson<VoiceReplyResp>(result.RawBody);
                        string reply = parsed?.data?.reply ?? string.Empty;
                        bool isReplyCorrect = parsed?.data?.isReplyCorrect ?? false;
                        
                        if (verboseLogging)
                            Debug.Log($"[Stage30] check/voice 응답 - reply='{reply}', isReplyCorrect={isReplyCorrect}");
                    }
                    catch (Exception ex)
                    {
                        if (verboseLogging)
                            Debug.LogWarning($"[Stage30] check/voice 응답 파싱 실패: {ex.Message}");
                    }
                    
                    CollectFeedback(result.RawBody);
                }
            });
    }

    private IEnumerator ProcessAccumulatedFeedback()
    {
        if (_accumulatedFeedback.Count == 0)
        {
            if (clipFeedbackGreatJob) yield return PlayClip(clipFeedbackGreatJob);
            yield break;
        }

        if (clipFeedbackLackSense) yield return PlayClip(clipFeedbackLackSense);
        if (clipFeedbackRetryPrompt) yield return PlayClip(clipFeedbackRetryPrompt);

        var ordered = _accumulatedFeedback
            .Where(p => p != null)
            .GroupBy(p => p.phoneme ?? p.label ?? string.Empty)
            .Select(g => g.First())
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            var feedback = ordered[i];

            if (!string.IsNullOrEmpty(feedback.imageUrl))
                yield return LoadAndShowImage(feedback.imageUrl);

            if (!string.IsNullOrEmpty(feedback.voiceUrl))
                yield return PlayVoiceUrl(feedback.voiceUrl);

            if (i == 0)
            {
                if (clipFeedbackTryAgainFirst) yield return PlayClip(clipFeedbackTryAgainFirst);
            }
            else
            {
                if (clipFeedbackTryAgainSecond) yield return PlayClip(clipFeedbackTryAgainSecond);
            }
        }
    }

    private IEnumerator LoadAndShowImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            yield break;

        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage30] 이미지 로드 실패: {req.error}\nURL={imageUrl}");
                yield break;
            }

            // 현재는 표시용 UI가 없으므로 텍스처만 로드하고 종료합니다.
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

    private string GetStageForVoiceUpload()
    {
        return !string.IsNullOrWhiteSpace(stageTwoPart) ? stageTwoPart : stage;
    }

    private void CollectFeedback(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var response = JsonUtility.FromJson<VoiceFeedbackResponse>(json);
            if (response == null)
                return;

            AddFeedbackRange(response.lacks);
            if (response.data != null)
            {
                AddFeedbackRange(response.data.lacks);
                AddFeedbackRange(response.data.insufficient);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Stage30] 피드백 파싱 실패: {ex.Message}\nJSON={json}");
        }
    }

    private void AddFeedbackRange(List<PronunciationFeedback> list)
    {
        if (list == null) return;

        foreach (var item in list)
        {
            if (item == null) continue;

            var key = item.phoneme ?? item.label ?? item.voiceUrl ?? Guid.NewGuid().ToString();
            if (_feedbackKeys.Add(key))
                _accumulatedFeedback.Add(item);
        }
    }

    private void ResetStoneUI()
    {
        _waitingForStoneCount = false;
        _pendingStoneCount = null;
        if (_stoneCountdownCoroutine != null)
        {
            StopCoroutine(_stoneCountdownCoroutine);
            _stoneCountdownCoroutine = null;
        }
        ResetStonePositions();
        if (stoneBoard) stoneBoard.SetActive(false);
        if (countdownText)
        {
            countdownText.text = string.Empty;
            countdownText.gameObject.SetActive(false);
        }
        if (wordLabel)
        {
            wordLabel.text = string.Empty;
            wordLabel.gameObject.SetActive(false);
        }
    }

    private void ResetStonePositions()
    {
        if (!stoneBoard)
            return;

        var stones = stoneBoard.GetComponentsInChildren<StoneDraggable>(true);
        if (stones == null || stones.Length == 0)
            return;

        foreach (var stone in stones)
        {
            if (stone != null)
                stone.ResetToInitialState();
        }
    }

    private AudioClip StartMic(int seconds, int sampleRate)
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Stage30] 마이크 장치가 없습니다.");
            return null;
        }
        return Microphone.Start(null, false, seconds, sampleRate);
    }

    private void ConfigureTutorialController()
    {
        bool enableTutorial = tutorialProfile != null;
        if (!enableTutorial)
        {
            enableTutorial =
                (introOptions != null && introOptions.Count > 0) ||
                introTutorialImage != null ||
                introOptionCursor != null ||
                introTutorialPanelAnimator != null ||
                introTutorialPanel != null ||
                guide3DCharacter != null;
        }

        if (!enableTutorial)
        {
            _tutorialController = null;
            _tutorialDependencies = null;
            return;
        }

        if (_tutorialController == null)
            _tutorialController = new StageTutorialController();

        if (_tutorialDependencies == null)
            _tutorialDependencies = new StageTutorialDependencies();

        _tutorialDependencies.PlayClip = PlayClip;
        _tutorialDependencies.StartCoroutine = routine => StartCoroutine(routine);
        _tutorialDependencies.StopCoroutine = routine =>
        {
            if (routine != null)
                StopCoroutine(routine);
        };
        _tutorialDependencies.ProgressText = progressText;
        _tutorialDependencies.EnsureProgressText = EnsureProgressText;
        _tutorialDependencies.MainImage = null;
        _tutorialDependencies.OptionsContainer = tutorialOptionsContainer;
        _tutorialDependencies.OptionButtonPrefab = null;
        _tutorialDependencies.CorrectSfx = null;
        _tutorialDependencies.MoveCursorSmooth = null;
        _tutorialDependencies.PulseOption = (rect, scale, duration, loops) => PulseTutorialTarget(rect, scale, duration, loops);
        _tutorialDependencies.ExecuteCustomStep = actionId => ExecuteTutorialCustomStep(actionId);
        _tutorialDependencies.Log = message => Debug.Log(message);
        _tutorialDependencies.LogWarning = message => Debug.LogWarning(message);
        _tutorialDependencies.VerboseLogging = verboseLogging;
        _tutorialDependencies.ManageOptionsContainerContents = tutorialOptionsContainer == null;

        if (tutorialProfile != null)
        {
            _tutorialController.ApplyProfile(tutorialProfile);
            _tutorialController.introOptions = tutorialProfile.introOptions != null
                ? tutorialProfile.introOptions
                    .Where(opt => opt != null)
                    .Select(opt => new StageTutorialController.IntroOption { label = opt.label, isCorrect = opt.isCorrect })
                    .ToList()
                : new List<StageTutorialController.IntroOption>();
            if (tutorialProfile.introTutorialImage)
                _tutorialController.introTutorialImage = tutorialProfile.introTutorialImage;
        }
        else
        {
            _tutorialController.introTutorialImage = introTutorialImage;
            _tutorialController.introOptions = introOptions != null
                ? introOptions
                    .Where(opt => opt != null)
                    .Select(opt => new StageTutorialController.IntroOption { label = opt.label, isCorrect = opt.isCorrect })
                    .ToList()
                : new List<StageTutorialController.IntroOption>();
            _tutorialController.requireTriggerAfterTutorial = requireTriggerAfterTutorial;
            _tutorialController.tutorialTriggerThreshold = tutorialTriggerThreshold;
            _tutorialController.tutorialFallbackKey = tutorialFallbackKey;
            _tutorialController.tutorialClipGapSeconds = tutorialClipGapSeconds;
        }

        _tutorialController.introOptionCursor = introOptionCursor;
        _tutorialController.introTutorialPanelAnimator = introTutorialPanelAnimator;
        _tutorialController.introTutorialPanel = introTutorialPanel;
        _tutorialController.guide3DCharacter = guide3DCharacter;
        _tutorialController.Initialize(_tutorialDependencies);
    }

    private Text EnsureProgressText()
    {
        return progressText;
    }

    private IEnumerator ExecuteTutorialCustomStep(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            yield break;

        string command = actionId;
        string parameter = string.Empty;
        int separator = actionId.IndexOf(':');
        if (separator >= 0)
        {
            command = actionId.Substring(0, separator);
            parameter = actionId.Substring(separator + 1);
        }

        command = command.Trim().ToLowerInvariant();
        parameter = parameter?.Trim() ?? string.Empty;

        switch (command)
        {
            case "showstoneboard":
                if (stoneBoard) stoneBoard.SetActive(true);
                break;
            case "hidestoneboard":
                if (stoneBoard) stoneBoard.SetActive(false);
                break;
            case "resetstoneboard":
                ResetStonePositions();
                break;
            case "startstonecountdown":
                {
                    float seconds = countdownSeconds;
                    if (!string.IsNullOrWhiteSpace(parameter) && float.TryParse(parameter, out var parsed))
                        seconds = parsed;
                    yield return StartStoneCountdown(seconds);
                    break;
                }
            case "setcountdownvisible":
                {
                    bool visible = true;
                    if (!string.IsNullOrWhiteSpace(parameter))
                        bool.TryParse(parameter, out visible);
                    if (countdownText)
                    {
                        countdownText.gameObject.SetActive(visible);
                        if (!visible)
                            countdownText.text = string.Empty;
                    }
                    break;
                }
            case "setoptionword":
            case "setword":
                SetTutorialOptionWord(parameter, true, false);
                break;
            case "setwordlabel":
                SetTutorialOptionWord(parameter, true, true);
                break;
            case "showoptionword":
            case "showwordlabel":
                ApplyTutorialOptionWord(_tutorialOptionWordCache, true);
                break;
            case "hideoptionword":
            case "hidewordlabel":
                ApplyTutorialOptionWord(string.Empty, false);
                break;
            case "movestone":
                yield return MoveStoneForTutorial(parameter);
                break;
            default:
                Debug.LogWarning($"[Stage30] Unknown tutorial custom action: {actionId}");
                break;
        }
    }

    private IEnumerator StartStoneCountdown(float seconds)
    {
        if (countdownText == null || seconds <= 0f)
            yield break;

        if (_stoneCountdownCoroutine != null)
        {
            StopCoroutine(_stoneCountdownCoroutine);
            _stoneCountdownCoroutine = null;
        }

        _stoneCountdownCoroutine = StartCoroutine(StoneCountdownRoutine(seconds));
        yield return _stoneCountdownCoroutine;
        _stoneCountdownCoroutine = null;
    }

    private void SetTutorialOptionWord(string text, bool showImmediately, bool forceWordLabel)
    {
        _tutorialOptionWordCache = NormalizeWordLabel(text);
        _tutorialOptionUseWordLabel = forceWordLabel;
        ApplyTutorialOptionWord(_tutorialOptionWordCache, showImmediately);
    }

    private void ApplyTutorialOptionWord(string text, bool show)
    {
        var target = !_tutorialOptionUseWordLabel && tutorialOptionWordText != null
            ? tutorialOptionWordText
            : wordLabel;
        if (target == null)
            return;

        target.text = NormalizeWordLabel(text);
        target.gameObject.SetActive(show);

        if (show && target.gameObject.activeInHierarchy)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(target.rectTransform);
        }
    }

    private string NormalizeWordLabel(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return Regex.Replace(
            text,
            @"U\+([0-9A-Fa-f]{4,6})",
            match =>
            {
                if (int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
                {
                    try
                    {
                        return char.ConvertFromUtf32(codePoint);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // ignore fall-through
                    }
                }
                return match.Value;
            });
    }

    private IEnumerator MoveStoneForTutorial(string parameter)
    {
        if (stoneBoard == null)
        {
            Debug.LogWarning("[Stage30] MoveStone tutorial action ignored because stoneBoard is not assigned.");
            yield break;
        }

        string stoneKey = parameter;
        string slotKey = string.Empty;

        if (!string.IsNullOrWhiteSpace(parameter))
        {
            var parts = parameter.Split(new[] { '>' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
                stoneKey = parts[0].Trim();
            if (parts.Length > 1)
                slotKey = parts[1].Trim();
        }

        var stone = FindStoneForTutorial(stoneKey);
        if (stone == null)
        {
            Debug.LogWarning($"[Stage30] MoveStone tutorial action could not find stone '{stoneKey}'.");
            yield break;
        }

        var targetSlot = FindSlotForTutorial(slotKey, stone);
        if (targetSlot == null)
        {
            Debug.LogWarning($"[Stage30] MoveStone tutorial action could not find target slot '{slotKey}'.");
            yield break;
        }

        yield return AnimateStoneToSlot(stone, targetSlot);
        AttachStoneToSlot(stone, targetSlot);
        ReportStoneCount(CalculateCurrentStoneCount());
    }

    private IEnumerator AnimateStoneToSlot(StoneDraggable stone, Stage30StoneDropZone slot)
    {
        if (stone == null || slot == null)
            yield break;

        var stoneRT = stone.GetComponent<RectTransform>();
        var slotRT = slot.GetComponent<RectTransform>();

        if (stoneRT == null || slotRT == null)
            yield break;

        float duration = Mathf.Max(0f, tutorialStoneMoveSeconds);
        if (duration <= 0f)
            yield break;

        Vector3 startPosition = stoneRT.position;
        Quaternion startRotation = stoneRT.rotation;
        Vector3 targetPosition = slotRT.position;
        Quaternion targetRotation = slotRT.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(duration > 0f ? elapsed / duration : 1f);
            float eased = tutorialStoneMoveCurve != null ? tutorialStoneMoveCurve.Evaluate(t) : t;
            stoneRT.position = Vector3.Lerp(startPosition, targetPosition, eased);
            stoneRT.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            yield return null;
        }

        stoneRT.position = targetPosition;
        stoneRT.rotation = targetRotation;
    }

    private void AttachStoneToSlot(StoneDraggable stone, Stage30StoneDropZone slot)
    {
        if (stone == null || slot == null)
            return;

        Transform container = slot.SlotParent != null ? slot.SlotParent : slot.transform;
        stone.transform.SetParent(container, false);
        stone.transform.SetAsLastSibling();

        if (stone.TryGetComponent(out LayoutElement existingLayout))
        {
            existingLayout.ignoreLayout = true;
        }
        else if (container.TryGetComponent(out LayoutGroup _))
        {
            var addedLayout = stone.gameObject.AddComponent<LayoutElement>();
            addedLayout.ignoreLayout = true;
        }

        if (stone.TryGetComponent(out RectTransform stoneRT))
        {
            stoneRT.anchorMin = new Vector2(0.5f, 0.5f);
            stoneRT.anchorMax = new Vector2(0.5f, 0.5f);
            stoneRT.anchoredPosition = Vector2.zero;
            stoneRT.localScale = Vector3.one;

            if (slot.TryGetComponent(out RectTransform slotRT))
            {
                stoneRT.position = slotRT.position;
                stoneRT.rotation = slotRT.rotation;
            }
            else
            {
                stoneRT.localRotation = Quaternion.identity;
            }
        }

        if (stone.TryGetComponent(out CanvasGroup cg))
        {
            cg.blocksRaycasts = true;
            cg.alpha = 1f;
        }
    }

    private int CalculateCurrentStoneCount()
    {
        if (stoneBoard == null)
            return 0;

        var dropZones = stoneBoard.GetComponentsInChildren<Stage30StoneDropZone>(true);
        if (dropZones == null || dropZones.Length == 0)
            return 0;

        Transform targetParent = null;
        foreach (var zone in dropZones)
        {
            if (zone == null)
                continue;

            targetParent = zone.SlotParent != null ? zone.SlotParent : zone.transform;
            if (targetParent != null)
                break;
        }

        if (targetParent == null)
            return 0;

        return CountPlacedStonesRecursive(targetParent);
    }

    private int CountPlacedStonesRecursive(Transform root)
    {
        if (root == null)
            return 0;

        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);

            if (child.GetComponent<StoneDraggable>() != null && child.GetComponent<Stage30StoneDropZone>() == null)
                count++;

            if (child.childCount > 0)
                count += CountPlacedStonesRecursive(child);
        }

        return count;
    }

    private IEnumerator PulseTutorialTarget(RectTransform target, float scaleMultiplier, float duration, int loops)
    {
        if (target == null)
            yield break;

        loops = Mathf.Max(1, loops);
        scaleMultiplier = scaleMultiplier <= 0f ? 1f : scaleMultiplier;
        duration = Mathf.Max(0f, duration);

        Vector3 originalScale = target.localScale;
        Vector3 peakScale = originalScale * scaleMultiplier;
        float halfDuration = duration > 0f ? duration * 0.5f : 0f;

        for (int i = 0; i < loops; i++)
        {
            yield return LerpScale(target, target.localScale, peakScale, halfDuration);
            yield return LerpScale(target, target.localScale, originalScale, halfDuration);
        }

        target.localScale = originalScale;
    }

    private IEnumerator LerpScale(RectTransform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = to;
    }

    private StoneDraggable FindStoneForTutorial(string identifier)
    {
        if (stoneBoard == null)
            return null;

        var stones = stoneBoard.GetComponentsInChildren<StoneDraggable>(true);
        if (stones == null || stones.Length == 0)
            return null;

        identifier = string.IsNullOrWhiteSpace(identifier) ? string.Empty : identifier.Trim();
        int targetNumber = ExtractNumberFromName(identifier);

        foreach (var stone in stones)
        {
            if (stone == null)
                continue;

            if (!string.IsNullOrEmpty(identifier) &&
                string.Equals(stone.gameObject.name, identifier, StringComparison.OrdinalIgnoreCase))
            {
                return stone;
            }

            if (targetNumber > 0 && ExtractNumberFromName(stone.gameObject.name) == targetNumber)
                return stone;
        }

        return stones.FirstOrDefault(s => s != null);
    }

    private Stage30StoneDropZone FindSlotForTutorial(string identifier, StoneDraggable stoneFallback)
    {
        if (stoneBoard == null)
            return null;

        var slots = stoneBoard.GetComponentsInChildren<Stage30StoneDropZone>(true);
        if (slots == null || slots.Length == 0)
            return null;

        identifier = string.IsNullOrWhiteSpace(identifier) ? string.Empty : identifier.Trim();
        int targetNumber = ExtractNumberFromName(identifier);

        if (targetNumber == 0 && stoneFallback != null)
            targetNumber = ExtractNumberFromName(stoneFallback.gameObject.name);

        foreach (var slot in slots)
        {
            if (slot == null)
                continue;

            if (!string.IsNullOrEmpty(identifier) &&
                string.Equals(slot.gameObject.name, identifier, StringComparison.OrdinalIgnoreCase))
            {
                return slot;
            }

            if (targetNumber > 0)
            {
                int slotNumberVal = slot.SlotNumber != 0 ? slot.SlotNumber : ExtractNumberFromName(slot.gameObject.name);
                if (slotNumberVal == targetNumber)
                    return slot;
            }
        }

        return slots.FirstOrDefault(s => s != null);
    }

    private int ExtractNumberFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return 0;

        var match = Regex.Match(name, @"_(\d+)");
        if (match.Success && match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var number))
            return number;

        return 0;
    }

    private IEnumerator StoneCountdownRoutine(float seconds)
    {
        if (countdownText == null)
            yield break;

        countdownText.gameObject.SetActive(true);
        float remaining = Mathf.Max(0f, seconds);

        while (remaining > 0f)
        {
            countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        countdownText.text = "0";
        yield return new WaitForSeconds(0.5f);
        countdownText.text = string.Empty;
        countdownText.gameObject.SetActive(false);
    }

    [Serializable]
    private class Problem
    {
        public int questionId;
        public string sessionId;
        public string problemWord;
        public int answerCnt;
        public int wordLength;
        public string problemVoiceUrl;
        public string problemVoiceUrl2;
        public string problemVoiceUrl3;
    }

    [Serializable]
    private class ProblemData
    {
        public string stageSessionId;
        public string sessionId;
        public List<Problem> problems;
    }

    [Serializable]
    private class VoiceReplyData { public string reply; public bool isReplyCorrect; public float accuracy; public string audioUrl; }
    [Serializable]
    private class VoiceReplyResp { public bool success = true; public string message; public VoiceReplyData data; }

    [Serializable]
    private class ProblemListResponse
    {
        public bool success;
        public string message;
        public ProblemData data;
    }

    [Serializable]
    private class VoiceFeedbackData
    {
        public List<PronunciationFeedback> lacks;
        public List<PronunciationFeedback> insufficient;
    }

    [Serializable]
    private class VoiceFeedbackResponse
    {
        public bool success = true;
        public string message;
        public VoiceFeedbackData data;
        public List<PronunciationFeedback> lacks;
    }

    [Serializable]
    public class PronunciationFeedback
    {
        public string phoneme;
        public string label;
        public string voiceUrl;
        public string imageUrl;
    }
}
