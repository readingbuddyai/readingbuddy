using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

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
public class Stage41Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "";
    [Tooltip("set에 사용할 stage 값 (4)")]
    public string stageSet = "4";
    [Tooltip("start/voice/attempt/complete 등에 사용할 stage 값 (4)")]
    public string stageTwoPart = "4";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";

    [Header("세션")]
    [Tooltip("/api/train/stage/start 응답의 stageSessionId")]
    public string stageSessionId = "";

    [Header("UI 참조")]
    public Text progressText;
    public Image guideImage;
    public RectTransform guideRect;
    public TMP_Text wordText;
    public TMP_Text choseongText;
    public TMP_Text jungseongText;
    public TMP_Text jongseongText;
    public GameObject choseongBox;
    public GameObject jungseongBox;
    public GameObject jongseongBox;
    public GameObject micIndicator;
    [Tooltip("보기 상자(자모 후보) 컨테이너(공용, 선택)")]
    public GameObject choicesContainer;
    [Tooltip("자음 후보 상자(초성/종성 오답 시 표시)")]
    public GameObject consonantChoicesContainer;
    [Tooltip("모음 후보 상자(중성 오답 시 표시)")]
    public GameObject vowelChoicesContainer;

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
    public AudioClip clipIntroAdvancedMagic;     // [4.1.1]
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
    public AudioClip clipTryAgain;               // [4.1.12]
    public AudioClip clipFinalizeSpell;          // [4.1.13]

    [Header("녹음 설정")]
    public int recordSeconds = 3;
    public int recordSampleRate = 44100;

    [Header("박스 포커스/점멸")]
    [Range(0f,1f)] public float dimAlpha = 0.4f;
    [Range(0f,1f)] public float blinkAlphaMin = 0.4f;
    [Range(0f,1f)] public float blinkAlphaMax = 1.0f;
    public float blinkPeriod = 0.6f; // sec per cycle

    [Header("개발/우회")]
    public bool bypassStartRequest = true;
    public bool bypassVoiceUpload = false;
    public bool logVerbose = true;

    // 상태
    private int _currentProblemNumber = 0; // 1-based
    private bool _awaitingUserArrangement;
    private List<string> _segmentReplies = new List<string>();
    private List<bool> _segmentCorrects = new List<bool>();
    private List<string> _expectedPhonemes = new List<string>();
    private int _expectedSegmentCount = 0; // 2 or 3
    private int _attemptCountForProblem = 0;
    private Coroutine _blinkCo;
    private GameObject _focusedBox;
    private bool[] _finalizedSlots = new bool[3]; // 각 슬롯(초/중/종) 최종 확정 여부

    #region DTOs
    [Serializable]
    public class QuestionDto
    {
        public int questionId;          // 선택
        public string problemWord;
        public string slowVoiceUrl;     // 듣기용
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
        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);
        authToken = EnvConfig.ResolveAuthToken(authToken);

        if (guideRect && guideStartSize.sqrMagnitude > 0)
            guideRect.sizeDelta = guideStartSize;

        ResetUI();
        StartCoroutine(RunStage());
    }

    private void ResetUI()
    {
        if (progressText) progressText.text = string.Empty;
        if (wordText) wordText.text = string.Empty;
        if (choseongText) choseongText.text = string.Empty;
        if (jungseongText) jungseongText.text = string.Empty;
        if (jongseongText)  jongseongText.text  = string.Empty;
        if (micIndicator) micIndicator.SetActive(false);
        if (choicesContainer) choicesContainer.SetActive(false);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
        FocusBox(null);
        SetAllBoxAlpha(dimAlpha);
    }

    private IEnumerator RunStage()
    {
        yield return PlayClip(sfxStart);
        yield return PlayClip(clipIntroAdvancedMagic);   // [4.1.1]
        yield return PlayClip(clipIntroListenPhonemes);  // [4.1.2]

        if (!bypassStartRequest && string.IsNullOrWhiteSpace(stageSessionId))
            yield return StartStageSession();

        List<QuestionDto> questions = null;
        yield return StartCoroutine(FetchQuestions(result => questions = result));
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[Stage41] 문제 세트를 불러오지 못했습니다.");
            yield break;
        }

        if ((enableGuideMoveBetweenQuestions || !_guideMoved) && guideRect)
            yield return MoveGuideToCorner();

        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            _currentProblemNumber = i + 1;
            _attemptCountForProblem = 0;
            _segmentReplies.Clear();
            _segmentCorrects.Clear();
            _expectedPhonemes = (q.phonemes != null) ? new List<string>(q.phonemes) : new List<string>();
            _expectedSegmentCount = (q.answerCnt > 0) ? q.answerCnt : (_expectedPhonemes != null ? _expectedPhonemes.Count : 3);
            Array.Clear(_finalizedSlots, 0, _finalizedSlots.Length);

            SetProgressLabel(_currentProblemNumber, questions.Count);
            if (wordText) wordText.text = q.problemWord ?? string.Empty;
            ClearPhonemeBoxes();

            // [4.1.3] 집중 안내 + 듣기
            yield return PlayClip(clipFocusListen);
            yield return PlayVoiceUrl(q.slowVoiceUrl);

            // 초성
            FocusBox(choseongBox);
            yield return PlayClip(clipPromptFirstPiece); // [4.1.4]
            yield return RecordAndUploadPhonemeSegment(0);
            yield return PlayClip(clipGreat);            // [4.1.5]
            UpdatePhonemeBoxTexts();

            // 중성
            if (_expectedSegmentCount >= 2)
            {
                FocusBox(jungseongBox);
                yield return PlayClip(clipPromptSecondPiece); // [4.1.6]
                yield return RecordAndUploadPhonemeSegment(1);
                yield return PlayClip(clipGreat);             // [4.1.5]
                UpdatePhonemeBoxTexts();
            }

            // 종성
            if (_expectedSegmentCount >= 3)
            {
                FocusBox(jongseongBox);
                yield return PlayClip(clipPromptFinalPiece); // [4.1.7]
                yield return RecordAndUploadPhonemeSegment(2);
                yield return PlayClip(clipGreat);            // [4.1.5]
                UpdatePhonemeBoxTexts();
            }

            FocusBox(null);

            // 모두 정답 여부
            bool allCorrect = EvaluateCorrectness();
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
                yield return PlayClip(clipFindCorrect);    // [4.1.10]
                // 부분 정답은 채워 보여주고, 오답 슬롯은 비워둠
                ApplyPartialFill();
                ShowChoicePanelsForWrong();
                _awaitingUserArrangement = true;
                while (_awaitingUserArrangement)
                    yield return null;
                if (choicesContainer) choicesContainer.SetActive(false);
                if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
                if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
            }

            if (i < questions.Count - 1)
                yield return PlayClip(sfxNext);
        }

        if (!string.IsNullOrWhiteSpace(stageSessionId))
            yield return CompleteStageSession();

        yield return PlayClip(clipFinalizeSpell); // [4.1.13]
        OnStageComplete?.Invoke();
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

        string phonemeStr = string.Join("", _expectedPhonemes ?? new List<string>());
        string selectedAnswer = string.Join("", arranged);
        StartCoroutine(SendAttemptLog(_currentProblemNumber, _attemptCountForProblem, phonemeStr, selectedAnswer, correct, wordText ? wordText.text : null));

        if (correct)
        {
            StartCoroutine(PlayClip(clipGoodThatsIt)); // [4.1.11]
            _awaitingUserArrangement = false;
            StartCoroutine(PlayClip(clipAllShine));    // [4.1.8]
            SetAllBoxAlpha(1f);
        }
        else
        {
            StartCoroutine(PlayClip(clipTryAgain));    // [4.1.12]
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
        if (jongseongText)  jongseongText.text  = string.Empty;
        SetAllBoxAlpha(dimAlpha);
    }

    private void UpdatePhonemeBoxTexts()
    {
        if (choseongText) choseongText.text = _segmentReplies.Count >= 1 ? _segmentReplies[0] : "";
        if (jungseongText) jungseongText.text = _segmentReplies.Count >= 2 ? _segmentReplies[1] : "";
        if (jongseongText)  jongseongText.text  = _segmentReplies.Count >= 3 ? _segmentReplies[2] : "";
    }

    // 부분 정답 반영: 맞은 슬롯은 텍스트/밝기, 틀린 슬롯은 비움/희미
    private void ApplyPartialFill()
    {
        for (int i = 0; i < _expectedSegmentCount; i++)
        {
            bool ok = (i < _segmentCorrects.Count) && _segmentCorrects[i];
            string reply = (i < _segmentReplies.Count) ? _segmentReplies[i] : string.Empty;
            SetSlotText(i, ok ? reply : string.Empty);
            _finalizedSlots[i] = ok;
            SetSlotAlpha(i, ok ? 1f : dimAlpha);
        }
        // 필요 없는 세그먼트(예: answerCnt=2)의 종성은 비워둠
        for (int i = _expectedSegmentCount; i < 3; i++)
        {
            SetSlotText(i, string.Empty);
            _finalizedSlots[i] = true; // 없는 슬롯은 완료로 처리
            SetSlotAlpha(i, dimAlpha);
        }
    }

    private void SetSlotText(int idx, string text)
    {
        if (idx == 0 && choseongText) choseongText.text = text ?? string.Empty;
        else if (idx == 1 && jungseongText) jungseongText.text = text ?? string.Empty;
        else if (idx == 2 && jongseongText) jongseongText.text = text ?? string.Empty;
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

    private IEnumerator FetchQuestions(Action<List<QuestionDto>> onDone)
    {
        string url = ComposeUrl($"/api/train/set?stage={UnityWebRequest.EscapeURL(stageSet)}&count={count}");
        using (var req = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                onDone?.Invoke(null);
                yield break;
            }
            var json = req.downloadHandler.text;
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
    }

    private IEnumerator StartStageSession()
    {
        string url = ComposeUrl($"/api/train/stage/start?stage={UnityWebRequest.EscapeURL(stageTwoPart)}&totalProblems={count}");
        using (var req = UnityWebRequest.PostWwwForm(url, ""))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            try
            {
                var resp = JsonUtility.FromJson<StartStageResp>(req.downloadHandler.text);
                if (resp != null && resp.data != null && !string.IsNullOrWhiteSpace(resp.data.stageSessionId))
                    stageSessionId = resp.data.stageSessionId;
            }
            catch { }
        }
    }

    private IEnumerator CompleteStageSession()
    {
        if (string.IsNullOrWhiteSpace(stageSessionId)) yield break;
        string url = ComposeUrl($"/api/train/stage/complete?stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}");
        using (var req = UnityWebRequest.PostWwwForm(url, ""))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
        }
    }

    private IEnumerator RecordAndUploadPhonemeSegment(int segmentIndex)
    {
        if (bypassVoiceUpload)
        {
            _segmentReplies.Add("*");
            _segmentCorrects.Add(true);
            yield break;
        }

        if (micIndicator) micIndicator.SetActive(true);
        var clip = StartMic(recordSeconds, recordSampleRate);
        yield return new WaitForSeconds(recordSeconds);
        if (micIndicator) micIndicator.SetActive(false);

        var wav = WavUtility.FromAudioClip(clip);
        string qs = $"stageSessionId={UnityWebRequest.EscapeURL(stageSessionId ?? string.Empty)}&stage={UnityWebRequest.EscapeURL(stageTwoPart ?? string.Empty)}&problemNumber={UnityWebRequest.EscapeURL(Mathf.Max(1,_currentProblemNumber).ToString())}";
        string url = ComposeUrl($"/api/train/check/voice?{qs}");
        var form = new WWWForm();
        form.AddBinaryData("audio", wav, "voice.wav", "audio/wav");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            req.chunkedTransfer = false;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;

            var respText = req.downloadHandler.text;
            try
            {
                var parsed = JsonUtility.FromJson<VoiceReplyResp>(respText);
                string reply = parsed?.data?.reply ?? string.Empty;
                bool ok = parsed?.data?.isReplyCorrect ?? false;
                _segmentReplies.Add((reply ?? string.Empty).Trim());
                _segmentCorrects.Add(ok);
                if (logVerbose) Debug.Log($"[Stage41] segment {segmentIndex} → reply='{reply}', correct={ok}");
            }
            catch { }
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

        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            ApplyCommonHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
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
            var aa = (a[i] ?? string.Empty).Trim();
            var bb = (b[i] ?? string.Empty).Trim();
            if (!string.Equals(aa, bb, StringComparison.Ordinal)) return false;
        }
        return true;
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

        if (consonantChoicesContainer)
            consonantChoicesContainer.SetActive(wrongInitial || wrongFinal);
        if (vowelChoicesContainer)
            vowelChoicesContainer.SetActive(wrongMedial);
        if (choicesContainer)
            choicesContainer.SetActive((wrongInitial || wrongFinal) || wrongMedial);
    }

    // 드래그 타일이 슬롯에 떨어졌을 때 호출 (PhonemeSlotUI에서 연결)
    public void OnUserDrop(int slotIndex, string symbol)
    {
        if (!_awaitingUserArrangement) return; // 보정 단계 외에는 무시
        // 유효성
        if (slotIndex < 0 || slotIndex >= 3) return;
        if (_expectedPhonemes == null || slotIndex >= _expectedPhonemes.Count) return;
        string expected = _expectedPhonemes[slotIndex];
        bool correct = string.Equals((symbol ?? string.Empty).Trim(), (expected ?? string.Empty).Trim(), StringComparison.Ordinal);

        // attempt 로깅 (슬롯 단위 시도)
        _attemptCountForProblem++;
        string phonemeStr = string.Join("", _expectedPhonemes);
        StartCoroutine(SendAttemptLog(_currentProblemNumber, _attemptCountForProblem, phonemeStr, symbol, correct, wordText ? wordText.text : null));

        if (!correct)
        {
            StartCoroutine(PlayClip(clipTryAgain)); // [4.1.12]
            return;
        }

        // 정답일 때 해당 슬롯 채우고 빛나게
        SetSlotText(slotIndex, expected);
        SetSlotAlpha(slotIndex, 1f);
        _finalizedSlots[slotIndex] = true;
        StartCoroutine(PlayClip(clipGoodThatsIt)); // [4.1.11]

        // 모두 채워졌는지 확인
        bool done = true;
        for (int i = 0; i < _expectedSegmentCount; i++)
            if (!_finalizedSlots[i]) { done = false; break; }

        if (done)
        {
            StartCoroutine(PlayClip(clipAllShine)); // [4.1.8]
            SetAllBoxAlpha(1f);
            _awaitingUserArrangement = false; // 루프 종료
        }
    }

    private static string EncodePlusInPath(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        try
        {
            var uri = new System.Uri(url);
            var path = uri.AbsolutePath.Replace("+", "%2B");
            var rebuilt = uri.Scheme + "://" + uri.Host + (uri.IsDefaultPort ? "" : ":" + uri.Port) + path + uri.Query;
            return rebuilt;
        }
        catch { return url.Replace("+", "%2B"); }
    }

    private static AudioType GuessAudioType(string url)
    {
        if (string.IsNullOrEmpty(url)) return AudioType.WAV;
        var lower = url.ToLowerInvariant();
        if (lower.Contains(".mp3")) return AudioType.MPEG;
        if (lower.Contains(".ogg")) return AudioType.OGGVORBIS;
        if (lower.Contains(".wav")) return AudioType.WAV;
        return AudioType.WAV;
    }
    #endregion
}
