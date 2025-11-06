using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage 1.2 진행 컨트롤러
/// - GET: /api/train/set?stage=1.2&count=5
/// - POST: /api/train/check/voice (multipart: questionId, file=voice.wav)
/// 흐름:
///  1) 시작 효과음 → 인트로 3줄 재생
///  2) 문제 5회 반복 (각 문제마다 청취 → 사용자의 녹음 업로드 → 칭찬 → 단어 맞추기)
///  3) 부족한 발음이 존재하면 보정 시퀀스, 없으면 완료 멘트
///  4) 다음 수업 안내 멘트
/// </summary>
public class Stage12Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "";
    public string stage = "1.1.2"; // set API에 사용
    [Tooltip("start/voice/attempt/complete 등에 사용할 2단계 stage 값 (예: 1.2)")]
    public string stageTwoPart = "1.2";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";
    [Header("세션")]
    [Tooltip("/api/train/stage/start 응답의 stageSessionId")]
    public string stageSessionId = "";

    [Header("UI 참조")]
    public Text progressText;
    public TMP_Text progressTextTMP;
    public Image mainImage;
    public RectTransform optionsContainer;
    public Button optionButtonPrefab;
    
    [Header("Mic Indicator")]
    [Tooltip("사용자 녹음 3초 동안 표시될 마이크 아이콘 오브젝트")]
    public GameObject micIndicator;
    
    [Header("Fonts")]
    public Font uiFont; // UGUI Text용 폰트(없으면 Arial 기본 사용)

    [Header("Option 시각 리소스")]
    public Sprite correctOptionSprite;
    public Sprite wrongOptionSprite;
    public Color correctTextColor = Color.white;
    public Color wrongTextColor = Color.white;
    [Tooltip("단어를 표시할 Text (선택)")]
    public TMP_Text optionWordText;
    public string trueButtonLabel = "";
    public string falseButtonLabel = "";

    [Header("오디오 재생")]
    public AudioSource audioSource;
    public AudioClip sfxStart;
    public AudioClip sfxNext;
    public AudioClip clipIntroGreeting;      // [1.2.1]
    public AudioClip clipIntroPraise;        // [1.2.1]
    public AudioClip clipIntroChallenge;     // [1.2.2]
    public AudioClip clipListen;             // [1.2.3]
    public AudioClip clipPromptRepeat;       // [1.2.4]
    public AudioClip clipPowerUp;            // [1.2.5]
    public AudioClip clipMatchPrompt;        // [1.2.6]
    public AudioClip clipCorrect;            // [1.2.7.1]
    public AudioClip clipWrong;              // [1.2.7.2]
    public AudioClip clipNeedMorePower;      // [1.2.8.1]
    public AudioClip clipRetryEncourage;     // [1.2.8.1.1]
    public AudioClip clipRetryTryAgain;      // [1.2.8.1.2]
    public AudioClip clipRetryFinalPush;     // [1.2.8.1.3]
    public AudioClip clipTrainingComplete;   // [1.2.8.2]
    public AudioClip clipNextLesson;         // [1.0.2]

    [Header("마이크 설정")]
    public int recordSeconds = 3;
    public int recordSampleRate = 44100;

    [Header("Auto Layout")]
    public bool applyAutoLayout = true;
    public float optionsHeight = 220f;
    public float optionsBottomMargin = 40f;
    public Vector2 imageFixedSize = new Vector2(1500f, 1500f);
    public Vector2 optionButtonPreferredSize = new Vector2(800f, 400f);
    public Vector2 gridSpacing = new Vector2(40f, 40f);
    private int _currentProblemNumber = 0; // 현재 문제 번호 (voice 업로드용)

    [Header("진단/로그")]
    [Tooltip("문제/옵션/버튼 등 상세 로그를 출력합니다.")]
    public bool logQuestionsVerbose = true;

    [Header("개발용 우회")]
    public bool bypassStartRequest = true;

    [Serializable]
    public class WordOptionDto
    {
        public int wordId;
        public string word;
        public string voiceUrl;
        public bool answer;
    }

    [Serializable]
    public class QuestionDto
    {
        public int questionId;
        public string sessionId;
        public string problemWord;
        public string targetPhoneme;
        public string imageUrl;
        public string voiceUrl;
        public List<WordOptionDto> options;
    }

    [Serializable]
    public class QuestionData
    {
        // 서버가 stageSessionId 키를 반환하는 경우를 우선 사용
        public string stageSessionId;
        // 하위 호환: 예전 응답에서 sessionId로 내려오는 경우 대응
        public string sessionId;
        public List<QuestionDto> problems;
    }

    [Serializable]
    public class QuestionListResponse
    {
        public bool success;
        public string message;
        public QuestionData data;
    }

    [Serializable]
    public class PronunciationFeedback
    {
        public string phoneme;    // 예: "ㅏ"
        public string label;      // 표시용 문자열(선택)
        public string voiceUrl;   // 가이드 음성 URL
        public string imageUrl;   // 모음 이미지 URL
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

    private readonly List<PronunciationFeedback> _accumulatedFeedback = new List<PronunciationFeedback>();
    private readonly HashSet<string> _feedbackKeys = new HashSet<string>();

    private void Start()
    {
        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);
        authToken = EnvConfig.ResolveAuthToken(authToken);

        if (applyAutoLayout)
            TryApplyAutoLayout();

        ResetOptionsUI();
        if (micIndicator)
            micIndicator.SetActive(false);
        StartCoroutine(RunStage());
    }

    private IEnumerator RunStage()
    {
        yield return PlayClip(sfxStart);
        yield return RunIntroSequence();

        // 세션 시작 (stageTwoPart 사용)
        if (!bypassStartRequest && string.IsNullOrWhiteSpace(stageSessionId))
        {
            yield return StartStageSession();
        }

        List<QuestionDto> questions = null;
        yield return StartCoroutine(FetchQuestions(result => questions = result));

        if (questions != null && questions.Count > 0)
        {
            for (int i = 0; i < questions.Count; i++)
            {
                yield return RunOneQuestion(i + 1, questions.Count, questions[i]);

                if (i < questions.Count - 1)
                    yield return PlayClip(sfxNext);
            }

            yield return ProcessAccumulatedFeedback();
        }

        // 세션 완료 보고 (best-effort)
        if (!string.IsNullOrWhiteSpace(stageSessionId))
            yield return CompleteStageSession();

        yield return PlayClip(clipNextLesson);
        // 학습 완료 모달 표시
        ShowEndModal();
    }

    private IEnumerator RunIntroSequence()
    {
        yield return PlayClip(clipIntroGreeting);
        yield return PlayClip(clipIntroPraise);
        yield return PlayClip(clipIntroChallenge);
    }

    private IEnumerator RunOneQuestion(int index, int total, QuestionDto q)
    {
        _currentProblemNumber = index;
        // Stage 1.1 스타일로 상단 진행도 표시: "i / total"
        SetProgressLabel(index, total);
        if (progressTextTMP)
        {
            progressTextTMP.enableAutoSizing = false;
            progressTextTMP.overflowMode = TextOverflowModes.Overflow;
            progressTextTMP.text = $"{index} / {total}";
        }

        yield return LoadAndShowImage(q.imageUrl);

        // 옵션을 보여줄 때 이미지가 가리지 않도록 레이캐스트를 잠시 끕니다.
        if (mainImage != null)
            mainImage.raycastTarget = false;

        yield return PlayClip(clipListen);
        yield return PlayVoiceUrl(q.voiceUrl);

        yield return PlayClip(clipPromptRepeat);
        if (micIndicator) micIndicator.SetActive(true);
        yield return RecordAndUpload(q);
        if (micIndicator) micIndicator.SetActive(false);

        yield return PlayClip(clipPowerUp);
        yield return PlayClip(clipMatchPrompt);

        yield return ShowOptionsSequence(q);

        if (mainImage != null)
            mainImage.raycastTarget = false;
    }

    // Stage 1.1 스타일 진행도 텍스트 생성/설정
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
        text.fontSize = 100;
        text.color = Color.white;
        text.font = uiFont ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        progressText = text;
        return progressText;
    }

    private void SetProgressLabel(int index, int total)
    {
        var t = EnsureProgressText();
        if (!t) return;
        t.text = $"{index} / {total}";
    }

    private void TryApplyAutoLayout()
    {
        if (optionsContainer)
        {
            var rt = optionsContainer;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, optionsBottomMargin);
            rt.sizeDelta = new Vector2(0f, Mathf.Max(optionsHeight, optionButtonPreferredSize.y + gridSpacing.y + 20f));

            var grid = optionsContainer.GetComponent<GridLayoutGroup>();
            if (grid)
            {
                grid.cellSize = optionButtonPreferredSize;
                grid.spacing = gridSpacing;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
            }
            else
            {
                Debug.LogWarning("[Stage12] OptionsContainer에 GridLayoutGroup이 없습니다. 버튼 정렬이 올바르지 않을 수 있습니다.");
            }
        }

        if (mainImage)
        {
            var mrt = mainImage.rectTransform;
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = imageFixedSize;
            float bottomReserved = optionsBottomMargin + Mathf.Max(optionsHeight, optionButtonPreferredSize.y + 20f);
            mrt.anchoredPosition = new Vector2(0f, bottomReserved * 0.5f);
            mainImage.preserveAspect = true;
            mainImage.raycastTarget = false;
        }
    }

    private IEnumerator FetchQuestions(Action<List<QuestionDto>> onCompleted)
    {
        string url = ComposeUrl($"/api/train/set?stage={UnityWebRequest.EscapeURL(stage)}&count={count}");

        using (var req = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Stage12] 문제 요청 실패: {req.error}\nURL={url}");
                yield break;
            }

            var json = req.downloadHandler.text;
            QuestionListResponse parsed = null;

            try
            {
                parsed = JsonUtility.FromJson<QuestionListResponse>(json);
                if (logQuestionsVerbose)
                    Debug.Log($"[Stage12] 질문 응답 JSON: {json}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stage12] 문제 파싱 실패: {ex.Message}\nJSON={json}");
            }

            if (parsed == null || parsed.data == null || parsed.data.problems == null || parsed.data.problems.Count == 0)
            {
                Debug.LogError("[Stage12] 문제 데이터가 비어 있습니다.");
                yield break;
            }

            // 우선 stageSessionId를 사용하고, 없으면 sessionId로 폴백
            if (!string.IsNullOrEmpty(parsed.data.stageSessionId))
            {
                stageSessionId = parsed.data.stageSessionId;
                Debug.Log($"[Stage12] stageSessionId 수신: {stageSessionId}");
                foreach (var problem in parsed.data.problems)
                    problem.sessionId = stageSessionId;
            }
            else if (!string.IsNullOrEmpty(parsed.data.sessionId))
            {
                stageSessionId = parsed.data.sessionId;
                Debug.Log($"[Stage12] sessionId 수신(폴백 사용): {stageSessionId}");
                foreach (var problem in parsed.data.problems)
                    problem.sessionId = stageSessionId;
            }
            else
            {
                // set 응답에 세션 키가 없는 서버도 있음 → 기존 stageSessionId가 있으면 재사용
                if (!string.IsNullOrWhiteSpace(stageSessionId))
                {
                    if (logQuestionsVerbose)
                        Debug.Log("[Stage12] set 응답에 stageSessionId/sessionId 없음 → 기존 stageSessionId 재사용");
                    foreach (var problem in parsed.data.problems)
                        problem.sessionId = stageSessionId;
                }
                else
                {
                    Debug.LogWarning("[Stage12] 응답에 stageSessionId/sessionId가 없습니다.");
                }
            }

            if (logQuestionsVerbose)
            {
                for (int i = 0; i < parsed.data.problems.Count; i++)
                {
                    var problem = parsed.data.problems[i];
                    var optionSummary = problem.options == null
                        ? "<no options>"
                        : string.Join(", ", problem.options.Select(o => $"{o.word}:{o.answer}"));
                    Debug.Log(
                        $"[Stage12] Problem {i + 1}/{parsed.data.problems.Count} → questionId={problem.questionId}, targetPhoneme={problem.targetPhoneme}, problemWord={problem.problemWord}, options=[{optionSummary}]"
                    );
                }
            }

            onCompleted?.Invoke(parsed.data.problems);
        }
    }

    private IEnumerator ShowOptionsSequence(QuestionDto q)
    {
        if (optionsContainer == null)
        {
            Debug.LogError("[Stage12] optionsContainer가 연결되지 않았습니다.");
            yield break;
        }

        if (optionButtonPrefab == null)
        {
            var loaded = Resources.Load<Button>("UI/OptionButton");
            if (loaded != null)
            {
                optionButtonPrefab = loaded;
            }
            else
            {
                Debug.LogError("[Stage12] optionButtonPrefab이 필요합니다.");
                yield break;
            }
        }

        ResetOptionsUI(false);

        foreach (var opt in q.options ?? Enumerable.Empty<WordOptionDto>())
        {
            yield return ShowSingleOption(opt, q.targetPhoneme);
        }

        ResetOptionsUI();
    }

    private IEnumerator ShowSingleOption(WordOptionDto opt, string targetPhoneme)
    {
        if (optionWordText)
        {
            optionWordText.text = opt.word;
            optionWordText.enableWordWrapping = false;
            optionWordText.alignment = TextAlignmentOptions.Center;
            optionWordText.overflowMode = TextOverflowModes.Overflow;
            optionWordText.gameObject.SetActive(true);
        }

        ClearOptionButtons();
        optionsContainer.gameObject.SetActive(true);

        bool answered = false;
        bool selectionIsCorrect = false;
        int attemptCount = 0;

        void OnAnswered(bool isCorrect)
        {
            answered = true;
            attemptCount++;
            selectionIsCorrect = isCorrect;
        }

        CreateChoiceButton(trueButtonLabel, correctOptionSprite, correctTextColor, () => OnAnswered(opt.answer));
        CreateChoiceButton(falseButtonLabel, wrongOptionSprite, wrongTextColor, () => OnAnswered(!opt.answer));

        if (optionsContainer.childCount == 0)
        {
            Debug.LogWarning("[Stage12] O/X 버튼 생성에 실패했습니다.");
            yield break;
        }

        while (true)
        {
            yield return new WaitUntil(() => answered);

            // attempt 로깅 (Stage11 규격)
            // - phonemes: 해당 문제의 targetPhoneme
            // - selectedAnswer: 현재 단어(opt.word)
            string phonemes = targetPhoneme ?? string.Empty;
            string selected = opt.word ?? string.Empty;
            yield return SendAttemptLog(_currentProblemNumber, attemptCount, phonemes, selected, selectionIsCorrect, opt.word);

            if (selectionIsCorrect)
            {
                yield return PlayClip(clipCorrect);
                break;
            }

            yield return PlayClip(clipWrong);
            answered = false;
        }

        ClearOptionButtons();
        optionsContainer.gameObject.SetActive(false);
    }

    private void CreateChoiceButton(string label, Sprite sprite, Color textColor, Action onClicked)
    {
        if (optionButtonPrefab == null || optionsContainer == null)
            return;

        var btn = Instantiate(optionButtonPrefab, optionsContainer);
        btn.gameObject.SetActive(true);
        if (logQuestionsVerbose)
            Debug.Log($"[Stage12] 버튼 생성 → label={label}, prefab={optionButtonPrefab.name}, parent={optionsContainer.name}");

        var tmpText = btn.GetComponentInChildren<TMP_Text>();
        if (tmpText)
        {
            tmpText.text = label;
            tmpText.color = textColor;
            if (logQuestionsVerbose)
                Debug.Log($"[Stage12] TMP 라벨 적용 → '{label}', font={tmpText.font?.name}");
        }
        else
        {
            var legacyText = btn.GetComponentInChildren<Text>();
            if (legacyText)
            {
                legacyText.text = label;
                legacyText.color = textColor;
                if (logQuestionsVerbose)
                    Debug.Log($"[Stage12] UI.Text 라벨 적용 → '{label}'");
            }
            else
            {
                Debug.LogWarning("[Stage12] 버튼에 Text 컴포넌트를 찾지 못했습니다.");
            }
        }

        var image = btn.GetComponent<Image>();
        if (image && sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        var rt = btn.GetComponent<RectTransform>();
        if (rt)
        {
            rt.sizeDelta = optionButtonPreferredSize;
            rt.localScale = Vector3.one;
        }

        var layout = btn.GetComponent<LayoutElement>();
        if (layout)
        {
            layout.preferredWidth = optionButtonPreferredSize.x;
            layout.preferredHeight = optionButtonPreferredSize.y;
            layout.layoutPriority = Mathf.Max(layout.layoutPriority, 1);
        }

        var grid = optionsContainer.GetComponent<GridLayoutGroup>();
        if (grid)
        {
            grid.cellSize = optionButtonPreferredSize;
            grid.spacing = gridSpacing;
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClicked?.Invoke());
    }

    private void ClearOptionButtons()
    {
        if (optionsContainer == null)
            return;
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);
    }

    private void ResetOptionsUI(bool clearWord = true)
    {
        ClearOptionButtons();
        if (optionsContainer)
            optionsContainer.gameObject.SetActive(false);
        if (clearWord && optionWordText)
        {
            optionWordText.text = string.Empty;
            optionWordText.gameObject.SetActive(false);
        }
    }

    private IEnumerator LoadAndShowImage(string imageUrl)
    {
        if (mainImage == null || string.IsNullOrEmpty(imageUrl))
            yield break;

        mainImage.enabled = false;
        mainImage.sprite = null;

        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage12] 이미지 로드 실패: {req.error}\nURL={imageUrl}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            mainImage.sprite = sprite;
            mainImage.enabled = true;
        }
    }

    private IEnumerator PlayClip(AudioClip clip)
    {
        if (!clip || !audioSource)
            yield break;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        yield return new WaitWhile(() => audioSource.isPlaying);
    }

    private IEnumerator PlayVoiceUrl(string voiceUrl)
    {
        if (string.IsNullOrEmpty(voiceUrl) || !audioSource)
        {
            Debug.LogWarning(string.IsNullOrEmpty(voiceUrl)
                ? "[Stage12] voiceUrl가 비어 있어 음성을 재생하지 못했습니다."
                : "[Stage12] audioSource가 설정되지 않아 음성을 재생하지 못했습니다.");
            yield break;
        }

        string sanitizedUrl = SanitizeUrl(voiceUrl);
        Debug.Log($"[Stage12] 음성 요청 시작 → {sanitizedUrl}");

        using (var req = UnityWebRequestMultimedia.GetAudioClip(sanitizedUrl, GuessAudioType(sanitizedUrl)))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage12] 음성 로드 실패: {req.error}\nURL={sanitizedUrl}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (logQuestionsVerbose)
                Debug.Log($"[Stage12] 음성 로드 성공 → length={clip.length:F2}s, samples={clip.samples}");
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
    }

    private string SanitizeUrl(string url)
    {
        return string.IsNullOrEmpty(url) ? url : url.Replace("+", "%2B");
    }

    private AudioType GuessAudioType(string url)
    {
        url = url.ToLowerInvariant();
        if (url.EndsWith(".mp3")) return AudioType.MPEG;
        if (url.EndsWith(".wav") || url.EndsWith(".wave")) return AudioType.WAV;
        if (url.EndsWith(".ogg")) return AudioType.OGGVORBIS;
        return AudioType.UNKNOWN;
    }

    private IEnumerator RecordAndUpload(QuestionDto q)
    {
        var clip = StartMic(recordSeconds, recordSampleRate);
        yield return new WaitForSeconds(recordSeconds);

        if (clip == null)
        {
            Debug.LogWarning("[Stage12] 녹음된 오디오가 없습니다.");
            yield break;
        }

        Microphone.End(null);
        var wav = WavUtility.FromAudioClip(clip);

        // Stage11과 동일한 규약: query에 stageSessionId, stage(두 자리), problemNumber 사용
        // stage는 stageTwoPart를 사용(예: 1.2)
        string stageForUpload = string.IsNullOrEmpty(stageTwoPart) ? stage : stageTwoPart;
        int problemNumber = Mathf.Max(1, _currentProblemNumber);
        string sessionForUpload = !string.IsNullOrWhiteSpace(stageSessionId) ? stageSessionId : (q.sessionId ?? string.Empty);
        string qs = $"stageSessionId={UnityWebRequest.EscapeURL(sessionForUpload)}&stage={UnityWebRequest.EscapeURL(stageForUpload ?? string.Empty)}&problemNumber={UnityWebRequest.EscapeURL(problemNumber.ToString())}";
        string url = ComposeUrl($"/api/train/check/voice?{qs}");

        var form = new WWWForm();
        // multipart 필드명은 audio (Stage11과 동일)
        form.AddBinaryData("audio", wav, "voice.wav", "audio/wav");

        Debug.Log($"[Stage12] 업로드 파라미터 → stageSessionId={q.sessionId ?? "<null>"}, stage={stageForUpload}, problemNumber={problemNumber}");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            req.chunkedTransfer = false;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                var body = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogWarning($"[Stage12] 음성 업로드 실패: {req.error} (code={req.responseCode})\\nURL={url}\\nBody={body}");
            }
            else
            {
                CollectFeedback(req.downloadHandler.text);
            }
        }
    }

    // ===== Start/Attempt/Complete (Stage11 스타일) =====
    [Serializable]
    private class StartStageData { public string stageSessionId; public string stage; public int totalProblems; public string startAt; }
    [Serializable]
    private class StartStageResponse { public bool success; public string message; public StartStageData data; }

    private IEnumerator StartStageSession()
    {
        string stageForStart = string.IsNullOrEmpty(stageTwoPart) ? stage : stageTwoPart;
        string url = ComposeUrl($"/api/train/stage/start?stage={UnityWebRequest.EscapeURL(stageForStart)}&totalProblems={count}");
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = null;
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage12] stage/start 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={req.downloadHandler.text}");
                yield break;
            }
            var respJson = req.downloadHandler.text;
            try
            {
                var resp = JsonUtility.FromJson<StartStageResponse>(respJson);
                if (resp != null && resp.data != null && !string.IsNullOrWhiteSpace(resp.data.stageSessionId))
                {
                    stageSessionId = resp.data.stageSessionId;
                    Debug.Log($"[Stage12] stageSessionId 발급: {stageSessionId}");
                }
                else
                {
                    Debug.LogWarning($"[Stage12] stage/start 응답 파싱 실패\nRaw={respJson}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Stage12] stage/start 파싱 예외: {e.Message}\nRaw={respJson}");
            }
        }
    }

    private IEnumerator CompleteStageSession()
    {
        string url = ComposeUrl($"/api/train/stage/complete?stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}");
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = null;
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage12] stage/complete 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={req.downloadHandler.text}");
                yield break;
            }
            Debug.Log("[Stage12] stage/complete OK");
        }
    }

    private IEnumerator SendAttemptLog(int problemNumber, int attemptNumber, string phonemes, string selectedAnswer, bool isCorrect, string word)
    {
        string url = ComposeUrl("/api/train/attempt");
        string stg = string.IsNullOrEmpty(stageTwoPart) ? (stage ?? string.Empty) : stageTwoPart;
        string ssid = stageSessionId ?? string.Empty;
        string ans = selectedAnswer ?? string.Empty;
        string problemWord = word ?? string.Empty;
        string audioUrl = string.Empty;
        bool includeReplyResult = attemptNumber > 1;
        string json = "{" +
                      "\"stageSessionId\":\"" + JsonEscape(ssid) + "\"," +
                      "\"problemNumber\":" + problemNumber + "," +
                      "\"stage\":\"" + JsonEscape(stg) + "\"," +
                      "\"problem\":\"" + JsonEscape(problemWord) + "\"," +
                      "\"audioUrl\":\"" + JsonEscape(audioUrl) + "\"," +
                      "\"isCorrect\":" + (isCorrect ? "true" : "false") + "," +
                      "\"isReplyCorrect\":" + (includeReplyResult ? (isCorrect ? "true" : "false") : "null") + "," +
                      "\"attemptNumber\":" + attemptNumber + "," +
                      "\"answer\":\"" + JsonEscape(ans) + "\"" + "}";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage12] attempt 로깅 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={json}\nResp={req.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[Stage12] attempt 로깅 OK: problem={problemNumber}, attempt={attemptNumber}, correct={isCorrect}");
            }
        }
    }

    private static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    // ===== End Modal (again/lobby) =====
    [Header("End Modal Buttons")]
    [Tooltip("끝 모달 '다시 학습하기' 버튼 프리팹")]
    public Button againButtonPrefab;
    [Tooltip("끝 모달 '로비로 나가기' 버튼 프리팹")]
    public Button lobbyButtonPrefab;
    [Tooltip("끝 모달 버튼 크기(px). 0이면 옵션 버튼 크기 사용")]
    public Vector2 endModalButtonSize = new Vector2(600f, 300f);

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
            if (preferred) { isCustom = true; return preferred; }
            foreach (var path in resourcePaths)
            {
                var loaded = Resources.Load<Button>(path);
                if (loaded) { isCustom = true; return loaded; }
                var go = Resources.Load<GameObject>(path);
                if (go)
                {
                    var childBtn = go.GetComponentInChildren<Button>(true) ?? go.GetComponent<Button>();
                    if (childBtn) { isCustom = true; return childBtn; }
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
            else if (tmp1) { tmp1.text = "다시 학습하기"; }
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
            else if (tmp2) { tmp2.text = "로비로 나가기"; }
        }
        btn2.onClick.AddListener(() => { Destroy(overlay); GoToLobby(); });
    }

    private void RestartStage()
    {
        StopAllCoroutines();
        ResetOptionsUI();
        if (mainImage)
        {
            mainImage.enabled = false;
            mainImage.sprite = null;
        }
        stageSessionId = string.Empty;
        StartCoroutine(RunStage());
    }

    private void GoToLobby()
    {
        if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene(SceneId.Lobby);
        else SceneManager.LoadScene(SceneId.Lobby);
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
            Debug.LogWarning($"[Stage12] 피드백 파싱 실패: {ex.Message}\nJSON={json}");
        }
    }

    private void AddFeedbackRange(List<PronunciationFeedback> list)
    {
        if (list == null)
            return;

        foreach (var item in list)
        {
            if (item == null)
                continue;

            var key = item.phoneme ?? item.label ?? item.voiceUrl ?? Guid.NewGuid().ToString();
            if (_feedbackKeys.Add(key))
                _accumulatedFeedback.Add(item);
        }
    }

    private IEnumerator ProcessAccumulatedFeedback()
    {
        if (_accumulatedFeedback.Count == 0)
        {
            yield return PlayClip(clipTrainingComplete);
            yield break;
        }

        yield return PlayClip(clipNeedMorePower);
        yield return PlayClip(clipRetryEncourage);

        var ordered = _accumulatedFeedback
            .Where(p => p != null)
            .GroupBy(p => p.phoneme ?? p.label ?? "")
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
                yield return PlayClip(clipRetryTryAgain);
            else
                yield return PlayClip(clipRetryFinalPush);
        }
    }

    private AudioClip StartMic(int seconds, int sampleRate)
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Stage12] 마이크 장치가 없습니다.");
            return null;
        }

        return Microphone.Start(null, false, seconds, sampleRate);
    }

    private void ApplyCommonHeaders(UnityWebRequest req)
    {
        if (!string.IsNullOrWhiteSpace(authToken))
        {
            var tokenTrim = authToken.Trim();
            if (tokenTrim.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                tokenTrim = tokenTrim.Substring(7).Trim();
            req.SetRequestHeader("Authorization", $"Bearer {tokenTrim}");
            if (logQuestionsVerbose)
                Debug.Log($"[Stage12] Auth header attached (len={tokenTrim.Length})");
        }
        req.SetRequestHeader("Accept", "application/json");
    }

    private string ComposeUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return path;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;

        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        if (path.StartsWith("/")) path = path.Substring(1);
        return baseUrl + path;
    }
}
