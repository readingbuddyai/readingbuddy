using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Stage 4.1 진행 컨트롤러 (분절 마법)
/// 흐름 개요:
/// - start 요청 (stage=4, totalProblems=count)
/// - set 요청   (stage=4, count)
/// - 문제별 반복:
///   - 시작 효과음, 가이드 이미지 중앙 표시 → 안내 멘트 [4.1.1][4.1.2]
///   - 가이드 이미지 축소/이동
///   - [4.1.3] 집중 안내 → 문제 음성(voiceUrl) 재생
///   - 초성 상자 포커스 → [4.1.4] → 3초 녹음 업로드 → [4.1.5]
///   - 중성 상자 포커스 → [4.1.6] → 3초 녹음 업로드 → [4.1.5]
///   - (받침 있을 때) 종성 상자 포커스 → [4.1.7] → 3초 녹음 업로드 → [4.1.5]
///   - 받아온 음소를 칸에 채움 → 모두 정답 [4.1.8] / 아니면 [4.1.9][4.1.10]
///   - 드래그로 수정 시도 → attempt 요청 → 정답 [4.1.11]/오답 [4.1.12] → 전부 맞추면 [4.1.8]
/// - 완료 후 [4.1.13]
///
/// 서버 규약(기본 가정: Stage11/12와 동일 패턴)
/// - POST /api/train/stage/start?stage=4&totalProblems={count}
/// - GET  /api/train/set?stage=4&count={count}
/// - POST /api/train/check/voice?stageSessionId=&stage=4&problemNumber= (multipart: audio=voice.wav)
/// - POST /api/train/attempt (JSON: stageSessionId, stage, problemNumber, attemptNumber, phonemes, selectedAnswer, isCorrect, word)
/// - POST /api/train/stage/complete?stageSessionId=
/// </summary>
public class Stage41Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "https://readingbuddyai.co.kr/";
    [Tooltip("set에 사용할 스테이지 (세 부분: 4)")]
    public string stageSet = "4";
    [Tooltip("start/voice/attempt/complete 등에 사용할 2단계 stage 값 (예: 4)")]
    public string stageTwoPart = "4";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";

    [Header("세션")]
    [Tooltip("/api/train/stage/start 응답의 stageSessionId")]
    public string stageSessionId = "";

    [Header("UI 참조")]
    public Text progressText;            // 상단 "i / N"
    public Image guideImage;             // 가이드 이미지
    public TMP_Text wordText;            // 문제 단어 표시(선택)
    public TMP_Text choseongText;        // 초성 박스 내 텍스트
    public TMP_Text jungseongText;       // 중성 박스 내 텍스트
    public TMP_Text jongseongText;       // 종성 박스 내 텍스트
    public GameObject choseongBox;       // 초성 상자(포커스 효과용)
    public GameObject jungseongBox;      // 중성 상자(포커스 효과용)
    public GameObject jongseongBox;      // 종성 상자(포커스 효과용)
    public GameObject micIndicator;      // 녹음 중 인디케이터

    [Header("가이드 이동/축소")]
    public RectTransform guideRect;      // guideImage의 RectTransform
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

    // 멘트 오디오 (필요한 클립을 인스펙터에서 연결)
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

    [Header("개발/우회")]
    public bool bypassStartRequest = true;   // 개발 시 세션 시작 생략
    public bool bypassVoiceUpload = false;   // 개발 시 음성 업로드 생략
    public bool logVerbose = true;

    // 상태
    private int _currentProblemNumber = 0; // 1-based
    private bool _awaitingUserArrangement;
    private List<string> _recognizedPhonemes = new List<string>(); // 사용자 음성 인식 결과
    private List<string> _expectedPhonemes = new List<string>();   // 정답(서버/세트에서 제공 시)
    private int _attemptCountForProblem = 0;

    #region DTOs
    [Serializable]
    public class QuestionDto
    {
        public int questionId;       // 문제 식별자
        public string problemWord;   // 단어 표기
        public string voiceUrl;      // 음절 단위 안내 음성
        public string imageUrl;      // (선택)
        public List<string> phonemes; // 정답 음소 (서버가 제공 시)
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
        public List<QuestionDto> questions; // 일부 서버 케이스 대응
        public List<QuestionDto> problems;  // 변형 대응
    }

    [Serializable]
    private class StartStageData { public string stageSessionId; public string stage; public int totalProblems; public string startAt; }
    [Serializable]
    private class StartStageResp { public bool success = true; public string message; public StartStageData data; }

    [Serializable]
    private class VoiceCheckData { public List<string> phonemes; }
    [Serializable]
    private class VoiceCheckResp { public bool success = true; public string message; public VoiceCheckData data; public List<string> phonemes; }
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
        if (jongseongText) jongseongText.text = string.Empty;
        if (micIndicator) micIndicator.SetActive(false);
        FocusBox(null);
    }

    private IEnumerator RunStage()
    {
        yield return PlayClip(sfxStart);
        yield return PlayClip(clipIntroAdvancedMagic);   // [4.1.1]
        yield return PlayClip(clipIntroListenPhonemes);  // [4.1.2]

        if (!bypassStartRequest && string.IsNullOrWhiteSpace(stageSessionId))
        {
            yield return StartStageSession();
        }

        // 문제 세트 로드
        List<QuestionDto> questions = null;
        yield return StartCoroutine(FetchQuestions(result => questions = result));

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[Stage41] 문제 세트를 불러오지 못했습니다.");
            yield break;
        }

        if ((enableGuideMoveBetweenQuestions || !_guideMoved) && guideRect)
        {
            yield return MoveGuideToCorner();
        }

        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            _currentProblemNumber = i + 1;
            _attemptCountForProblem = 0;
            _recognizedPhonemes.Clear();
            _expectedPhonemes = (q.phonemes != null) ? new List<string>(q.phonemes) : new List<string>();

            SetProgressLabel(_currentProblemNumber, questions.Count);
            if (wordText) wordText.text = q.problemWord ?? string.Empty;
            ClearPhonemeBoxes();

            // [4.1.3] 집중 안내 + 음절 안내 음성 재생
            yield return PlayClip(clipFocusListen);
            yield return PlayVoiceUrl(q.voiceUrl);

            // 초성
            FocusBox(choseongBox);
            yield return PlayClip(clipPromptFirstPiece); // [4.1.4]
            yield return RecordAndUploadPhonemeSegment();
            yield return PlayClip(clipGreat);            // [4.1.5]
            UpdatePhonemeBoxTexts();

            // 중성
            FocusBox(jungseongBox);
            yield return PlayClip(clipPromptSecondPiece); // [4.1.6]
            yield return RecordAndUploadPhonemeSegment();
            yield return PlayClip(clipGreat);             // [4.1.5]
            UpdatePhonemeBoxTexts();

            // 종성(필요 시)
            bool needJongseong = (_expectedPhonemes != null && _expectedPhonemes.Count == 3);
            if (!needJongseong)
            {
                // 정답 정보가 없을 때: 세그먼트 응답이 2개 미만이면 계속, 2개면 스킵 판단
                needJongseong = _recognizedPhonemes.Count < 3; // 첫 두 번 업로드 후 부족하면 한 번 더 시도
            }
            if (needJongseong)
            {
                FocusBox(jongseongBox);
                yield return PlayClip(clipPromptFinalPiece); // [4.1.7]
                yield return RecordAndUploadPhonemeSegment();
                yield return PlayClip(clipGreat);            // [4.1.5]
                UpdatePhonemeBoxTexts();
            }

            FocusBox(null);

            // 정오 판정 및 보정 루프
            bool allCorrect = EvaluateCorrectness();
            if (allCorrect)
            {
                yield return PlayClip(clipAllShine); // [4.1.8]
            }
            else
            {
                yield return PlayClip(clipNeedMorePower); // [4.1.9]
                yield return PlayClip(clipFindCorrect);    // [4.1.10]

                // UI 드래그 보정 대기 루프(외부에서 SetUserArrangement 호출)
                _awaitingUserArrangement = true;
                while (_awaitingUserArrangement)
                    yield return null;
            }

            if (i < questions.Count - 1)
                yield return PlayClip(sfxNext);
        }

        // 세션 종료
        if (!string.IsNullOrWhiteSpace(stageSessionId))
            yield return CompleteStageSession();

        yield return PlayClip(clipFinalizeSpell); // [4.1.13]
        // 완료 모달은 별도 UI에서 처리하도록 훅만 남김
        OnStageComplete?.Invoke();
    }

    // 외부(UI)에서 드래그로 재배열 후 호출: 초/중/종성 순으로 전달 (종성이 없으면 null/빈값)
    public void SetUserArrangement(string initial, string medial, string finalPhoneme)
    {
        var arranged = new List<string>();
        if (!string.IsNullOrEmpty(initial)) arranged.Add(initial);
        if (!string.IsNullOrEmpty(medial)) arranged.Add(medial);
        if (!string.IsNullOrEmpty(finalPhoneme)) arranged.Add(finalPhoneme);

        bool correct = ComparePhonemeOrder(arranged, _expectedPhonemes);
        _attemptCountForProblem++;

        // attempt 로깅
        string phonemeStr = string.Join("", _expectedPhonemes ?? new List<string>());
        string selectedAnswer = string.Join("", arranged);
        StartCoroutine(SendAttemptLog(_currentProblemNumber, _attemptCountForProblem, phonemeStr, selectedAnswer, correct, wordText ? wordText.text : null));

        if (correct)
        {
            StartCoroutine(PlayClip(clipGoodThatsIt)); // [4.1.11]
            _awaitingUserArrangement = false;
            StartCoroutine(PlayClip(clipAllShine));    // [4.1.8]
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
        if (jongseongText) jongseongText.text = string.Empty;
    }

    private void UpdatePhonemeBoxTexts()
    {
        if (_recognizedPhonemes == null) return;
        if (choseongText) choseongText.text = _recognizedPhonemes.Count >= 1 ? _recognizedPhonemes[0] : "";
        if (jungseongText) jungseongText.text = _recognizedPhonemes.Count >= 2 ? _recognizedPhonemes[1] : "";
        if (jongseongText)  jongseongText.text  = _recognizedPhonemes.Count >= 3 ? _recognizedPhonemes[2] : "";
    }

    private void FocusBox(GameObject box)
    {
        if (choseongBox) choseongBox.SetActive(choseongBox == box);
        if (jungseongBox) jungseongBox.SetActive(jungseongBox == box);
        if (jongseongBox)  jongseongBox.SetActive(jongseongBox  == box);
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
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage41] 음성 로드 실패: {req.error} url={voiceUrl}");
                yield break;
            }
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
                Debug.LogWarning($"[Stage41] set 실패: {req.error} code={req.responseCode} url={url}");
                onDone?.Invoke(null);
                yield break;
            }
            var json = req.downloadHandler.text;
            List<QuestionDto> list = null;
            try
            {
                var parsed = JsonUtility.FromJson<QuestionListResponse>(json);
                if (parsed != null)
                {
                    if (parsed.data != null && parsed.data.problems != null && parsed.data.problems.Count > 0)
                        list = parsed.data.problems;
                    else if (parsed.problems != null && parsed.problems.Count > 0)
                        list = parsed.problems;
                    else if (parsed.questions != null && parsed.questions.Count > 0)
                        list = parsed.questions;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Stage41] set 파싱 실패: {e.Message}");
            }
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
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage41] stage/start 실패: {req.error} code={req.responseCode} url={url}");
                yield break;
            }
            try
            {
                var resp = JsonUtility.FromJson<StartStageResp>(req.downloadHandler.text);
                if (resp != null && resp.data != null && !string.IsNullOrWhiteSpace(resp.data.stageSessionId))
                {
                    stageSessionId = resp.data.stageSessionId;
                    if (logVerbose) Debug.Log($"[Stage41] stageSessionId 수신: {stageSessionId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Stage41] stage/start 파싱 실패: {e.Message}");
            }
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
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage41] stage/complete 실패: {req.error} code={req.responseCode} url={url}");
            }
        }
    }

    private IEnumerator RecordAndUploadPhonemeSegment()
    {
        if (bypassVoiceUpload)
        {
            // 개발 편의: 임시 더미 음소 추가
            _recognizedPhonemes.Add("*");
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
            if (req.result != UnityWebRequest.Result.Success)
            {
                var body = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogWarning($"[Stage41] 음성 업로드 실패: {req.error} code={req.responseCode}\nURL={url}\nBody={body}");
                yield break;
            }

            var respText = req.downloadHandler.text;
            try
            {
                var parsed = JsonUtility.FromJson<VoiceCheckResp>(respText);
                List<string> phs = null;
                if (parsed != null)
                {
                    if (parsed.data != null && parsed.data.phonemes != null && parsed.data.phonemes.Count > 0)
                        phs = parsed.data.phonemes;
                    else if (parsed.phonemes != null && parsed.phonemes.Count > 0)
                        phs = parsed.phonemes;
                }
                if (phs != null && phs.Count > 0)
                {
                    foreach (var p in phs)
                        _recognizedPhonemes.Add(Normalize(p));
                }
                if (logVerbose) Debug.Log($"[Stage41] 인식 결과 누적: [{string.Join(",", _recognizedPhonemes)}]");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Stage41] 음성 응답 파싱 실패: {e.Message}");
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

        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            ApplyCommonHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage41] attempt 실패: {req.error} code={req.responseCode}\nURL={url}\nBody={json}\nResp={req.downloadHandler.text}");
            }
            else if (logVerbose)
            {
                Debug.Log($"[Stage41] attempt OK: problem={problemNumber}, attempt={attemptNumber}, correct={isCorrect}");
            }
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

    private static string Normalize(string s)
    {
        return (s ?? "").Trim();
    }

    private bool EvaluateCorrectness()
    {
        if (_expectedPhonemes == null || _expectedPhonemes.Count == 0) return false; // 정답 미제공 시 외부 보정 플로우로 진입
        return ComparePhonemeOrder(_recognizedPhonemes, _expectedPhonemes);
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
        catch
        {
            return url.Replace("+", "%2B");
        }
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

