using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class Stage20Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "";
    public string stage = "2";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";
    [Tooltip("stage/start 호출을 건너뛰려면 켜 두세요")]
    public bool bypassStageStart = false;
    [Tooltip("디버그 로그를 상세히 출력합니다")]
    public bool verboseLogging = false;

    private string stageSessionId;

    [Header("UI 참조")]
    public Text progressText;
    public TMP_Text progressTextTMP;
    public TMP_Text wordLabel;

    [Header("오디오 재생")]
    public AudioSource audioSource;
    public AudioClip sfxStart;
    public AudioClip sfxNext;

    [Header("마이크 설정")]
    public int recordSeconds = 3;
    public int recordSampleRate = 44100;

    [Header("도입 대사")]
    public AudioClip clipHello;
    public AudioClip clipLesson;
    public AudioClip clipExplain;
    public AudioClip clipStoneIntro;

    [Header("문항 흐름 대사")]
    public AudioClip clipTeacherLead;      // [2.4.1]
    public AudioClip clipListenCue;        // [2.4.2]
    public AudioClip clipYourTurn;         // [2.5.1]
    public AudioClip clipRepeatPrompt;     // [2.5.2]
    public AudioClip clipPerfect;          // [2.6.1]
    public AudioClip clipMagicFeel;        // [2.6.2]
    public AudioClip clipCountInstruction; // [2.7]
    public AudioClip clipCountCorrect1;    // [2.8.1.1]
    public AudioClip clipCountCorrect2;    // [2.8.1.2]
    public AudioClip clipCountWrong1;      // [2.8.2.1]
    public AudioClip clipCountWrong2;      // [2.8.2.2]

    [Header("마법 돌 퍼즐")]
    public GameObject stoneBoard;
    public TMP_Text countdownText;
    public float countdownSeconds = 10f;
    public int maxStoneAttempts = 2;

    [System.Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    public IntUnityEvent onStoneRoundBegin;
    public UnityEvent onStoneRoundEnd;
    [Tooltip("퍼즐 시작 시 자동으로 카운트다운을 시작할지 여부")]
    public bool autoStartCountdown = true;

    [Header("발음 피드백")]
    public AudioClip clipNeedsMorePower;      // [2.9.1]
    public AudioClip clipRetryEncourage;      // [2.9.2.1]
    public AudioClip clipRetryTryAgain1;      // [2.9.3.1]
    public AudioClip clipRetryTryAgain2;      // [2.9.3.2]
    public AudioClip clipRetryTryAgain3;      // [2.9.4.1]
    public AudioClip clipRetryTryAgain4;      // [2.9.4.2]
    public AudioClip clipGreatJob1;           // [2.9.5.1]
    public AudioClip clipGreatJob2;           // [2.9.5.2]
    public AudioClip clipReadyNextLesson;     // [2.9.6]

    private bool waitingForStoneCount;
    private int? pendingStoneCount;
    private int _currentProblemNumber;

    private readonly List<PronunciationFeedback> _accumulatedFeedback = new List<PronunciationFeedback>();
    private readonly HashSet<string> _feedbackKeys = new HashSet<string>();

    private void Start()
    {
        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);
        authToken = EnvConfig.ResolveAuthToken(authToken);
        ResetStoneUI();
        StartCoroutine(RunStage());
    }

    private IEnumerator RunStage()
    {
        if (!bypassStageStart)
        {
            yield return StageStart();
            if (string.IsNullOrWhiteSpace(stageSessionId))
            {
                Debug.LogError("[Stage20] stageSessionId를 가져오지 못했습니다. 진행을 중단합니다.");
                yield break;
            }
        }

        if (sfxStart) yield return PlayClip(sfxStart);
        yield return RunIntroSequence();

        List<QuestionDto> questions = null;
        yield return StartCoroutine(FetchQuestions(result => questions = result));

        if (questions != null && questions.Count > 0)
        {
            for (int i = 0; i < questions.Count; i++)
            {
                yield return RunOneProblem(i + 1, questions.Count, questions[i]);

                if (sfxNext && i < questions.Count - 1)
                    yield return PlayClip(sfxNext);
            }

            yield return ProcessAccumulatedFeedback();
        }

        if (clipReadyNextLesson)
            yield return PlayClip(clipReadyNextLesson);

        if (!bypassStageStart && !string.IsNullOrWhiteSpace(stageSessionId))
            yield return StageComplete();
    }

    private IEnumerator RunIntroSequence()
    {
        if (clipHello) yield return PlayClip(clipHello);
        if (clipLesson) yield return PlayClip(clipLesson);
        if (clipExplain) yield return PlayClip(clipExplain);
        if (clipStoneIntro) yield return PlayClip(clipStoneIntro);
    }

    private IEnumerator RunOneProblem(int index, int total, QuestionDto problem)
    {
        ResetStoneUI();
        _currentProblemNumber = index;
        UpdateProgress(index, total, problem);

        if (wordLabel)
        {
            wordLabel.enableWordWrapping = false;
            wordLabel.text = problem.problemWord;
            wordLabel.gameObject.SetActive(true);
        }

        if (clipTeacherLead) yield return PlayClip(clipTeacherLead);
        if (clipListenCue) yield return PlayClip(clipListenCue);
        if (!string.IsNullOrEmpty(problem.wordVoiceUrl))
            yield return PlayVoiceUrl(problem.wordVoiceUrl);

        if (clipYourTurn) yield return PlayClip(clipYourTurn);
        if (clipRepeatPrompt) yield return PlayClip(clipRepeatPrompt);

        yield return RecordAndUpload(problem, index);

        if (clipPerfect) yield return PlayClip(clipPerfect);
        if (clipMagicFeel) yield return PlayClip(clipMagicFeel);

        if (clipCountInstruction) yield return PlayClip(clipCountInstruction);
        if (!string.IsNullOrEmpty(problem.wordVoiceUrl))
            yield return PlayVoiceUrl(problem.wordVoiceUrl);

        yield return RunStoneRound(problem, index);
    }

    private void UpdateProgress(int index, int total, QuestionDto problem)
    {
        string label = $"{index}/{total}";
        if (!string.IsNullOrWhiteSpace(problem.problemWord))
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

    private IEnumerator RunStoneRound(QuestionDto problem, int problemNumber)
    {
        int expectedCount = problem != null ? problem.wordLength : 0;
        int attempts = 0;
        bool solved = false;

        while (attempts < maxStoneAttempts && !solved)
        {
            ResetStonePositions();
            pendingStoneCount = null;
            waitingForStoneCount = true;

            if (stoneBoard)
                stoneBoard.SetActive(true);
            if (countdownText)
            {
                countdownText.text = string.Empty;
                countdownText.gameObject.SetActive(false);
            }

            onStoneRoundBegin?.Invoke(expectedCount);

            while (waitingForStoneCount)
            {
                yield return null;
            }

            int submitted = pendingStoneCount ?? 0;
            pendingStoneCount = null;

            if (stoneBoard)
                stoneBoard.SetActive(false);
            onStoneRoundEnd?.Invoke();

            int attemptNumber = attempts + 1;
            bool isCorrect = (submitted == expectedCount);
            yield return SendAttemptLog(problemNumber, attemptNumber, expectedCount, submitted, isCorrect, problem != null ? problem.problemWord : null);

            if (isCorrect)
            {
                solved = true;
                if (clipCountCorrect1) yield return PlayClip(clipCountCorrect1);
                if (clipCountCorrect2) yield return PlayClip(clipCountCorrect2);
            }
            else
            {
                if (clipCountWrong1) yield return PlayClip(clipCountWrong1);
                if (clipCountWrong2) yield return PlayClip(clipCountWrong2);
                attempts++;
            }
        }
    }

    public void ReportStoneCount(int count)
    {
        if (!waitingForStoneCount) return;
        pendingStoneCount = count;
    }

    public void ConfirmStoneCount()
    {
        if (!waitingForStoneCount)
            return;

        if (!pendingStoneCount.HasValue)
        {
            Debug.LogWarning("[Stage20] 아직 보고된 Stone 개수가 없습니다. 드롭 후 버튼을 눌러 주세요.");
            return;
        }

        waitingForStoneCount = false;
    }

    private IEnumerator FetchQuestions(Action<List<QuestionDto>> onCompleted)
    {
        string sessionQuery = string.IsNullOrWhiteSpace(stageSessionId)
            ? string.Empty
            : $"&stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}";

        string url = ComposeUrl($"/api/train/set?stage={UnityWebRequest.EscapeURL(stage)}&count={count}{sessionQuery}");

        using (var req = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                Debug.LogError($"[Stage20] 문제 요청 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={body}");
                yield break;
            }

            var json = req.downloadHandler.text;
            ProblemListResponse parsed = null;

            try
            {
                parsed = JsonUtility.FromJson<ProblemListResponse>(json);
                Debug.Log($"[Stage20] 질문 응답 JSON: {json}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stage20] 문제 파싱 실패: {ex.Message}\nJSON={json}");
            }

            if (parsed == null || parsed.data == null)
            {
                Debug.LogError("[Stage20] 문제 데이터가 비어 있습니다 (data=null)." );
                yield break;
            }

            string sessionFromResponse = !string.IsNullOrEmpty(parsed.data.stageSessionId)
                ? parsed.data.stageSessionId
                : parsed.data.sessionId;

            if (!string.IsNullOrEmpty(sessionFromResponse))
            {
                stageSessionId = sessionFromResponse;
                if (verboseLogging)
                    Debug.Log($"[Stage20] stageSessionId 수신: {stageSessionId}");
            }

            if (parsed.data.problems == null || parsed.data.problems.Count == 0)
            {
                Debug.LogError("[Stage20] 문제 데이터가 비어 있습니다.");
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(stageSessionId))
            {
                foreach (var problem in parsed.data.problems)
                    problem.sessionId = stageSessionId;
            }

            onCompleted?.Invoke(parsed.data.problems);
        }
    }

    private IEnumerator RecordAndUpload(QuestionDto problem, int problemNumber)
    {
        var clip = StartMic(recordSeconds, recordSampleRate);
        yield return new WaitForSeconds(recordSeconds);

        if (clip == null)
        {
            Debug.LogWarning("[Stage20] 녹음된 오디오가 없습니다.");
            yield break;
        }

        Microphone.End(null);
        var wav = WavUtility.FromAudioClip(clip);

        if (string.IsNullOrWhiteSpace(stageSessionId))
        {
            Debug.LogWarning("[Stage20] stageSessionId가 비어 있습니다. /api/train/stage/start 응답을 확인하세요.");
        }

        int safeProblemNumber = Mathf.Max(1, problemNumber);
        string stageForUpload = stage ?? string.Empty;
        string sessionForUpload = !string.IsNullOrEmpty(stageSessionId) ? stageSessionId : (problem != null ? problem.sessionId ?? string.Empty : string.Empty);
        string qs = $"stageSessionId={UnityWebRequest.EscapeURL(sessionForUpload ?? string.Empty)}&stage={UnityWebRequest.EscapeURL(stageForUpload ?? string.Empty)}&problemNumber={UnityWebRequest.EscapeURL(safeProblemNumber.ToString())}";
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
                Debug.LogWarning($"[Stage20] 음성 업로드 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={req.downloadHandler?.text}");
            }
            else
            {
                CollectFeedback(req.downloadHandler.text);
            }
        }
    }

    private IEnumerator ProcessAccumulatedFeedback()
    {
        if (_accumulatedFeedback.Count == 0)
        {
            if (clipGreatJob1) yield return PlayClip(clipGreatJob1);
            if (clipGreatJob2) yield return PlayClip(clipGreatJob2);
            yield break;
        }

        if (clipNeedsMorePower) yield return PlayClip(clipNeedsMorePower);
        if (clipRetryEncourage) yield return PlayClip(clipRetryEncourage);

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
            {
                if (clipRetryTryAgain1) yield return PlayClip(clipRetryTryAgain1);
                if (clipRetryTryAgain2) yield return PlayClip(clipRetryTryAgain2);
            }
            else
            {
                if (clipRetryTryAgain3) yield return PlayClip(clipRetryTryAgain3);
                if (clipRetryTryAgain4) yield return PlayClip(clipRetryTryAgain4);
            }
        }
    }

    private IEnumerator LoadAndShowImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) yield break;

        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage20] 이미지 로드 실패: {req.error}\nURL={imageUrl}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            // 이미지 표시용 UI가 있다면 여기에서 적용합니다.
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
                ? "[Stage20] voiceUrl가 비어 있어 음성을 재생하지 못했습니다."
                : "[Stage20] audioSource가 설정되지 않아 음성을 재생하지 못했습니다.");
            yield break;
        }

        string sanitizedUrl = SanitizeUrl(voiceUrl);
        Debug.Log($"[Stage20] 음성 요청 시작 → {sanitizedUrl}");

        using (var req = UnityWebRequestMultimedia.GetAudioClip(sanitizedUrl, GuessAudioType(sanitizedUrl)))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage20] 음성 로드 실패: {req.error}\nURL={sanitizedUrl}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
    }

    private AudioType GuessAudioType(string url)
    {
        url = url.ToLowerInvariant();
        if (url.EndsWith(".mp3")) return AudioType.MPEG;
        if (url.EndsWith(".wav") || url.EndsWith(".wave")) return AudioType.WAV;
        if (url.EndsWith(".ogg")) return AudioType.OGGVORBIS;
        return AudioType.UNKNOWN;
    }

    private string SanitizeUrl(string url)
    {
        return string.IsNullOrEmpty(url) ? url : url.Replace("+", "%2B");
    }

    private AudioClip StartMic(int seconds, int sampleRate)
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Stage20] 마이크 장치가 없습니다.");
            return null;
        }

        return Microphone.Start(null, false, seconds, sampleRate);
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
            Debug.LogWarning($"[Stage20] 피드백 파싱 실패: {ex.Message}\nJSON={json}");
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
        waitingForStoneCount = false;
        pendingStoneCount = null;
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

    private void ApplyCommonHeaders(UnityWebRequest req)
    {
        if (!string.IsNullOrWhiteSpace(authToken))
            req.SetRequestHeader("Authorization", $"Bearer {authToken}");
        req.SetRequestHeader("Accept", "application/json");
    }

    private string ComposeUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return path;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;

        string trimmedBase = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        if (path.StartsWith("/")) path = path.Substring(1);
        return trimmedBase + path;
    }

    private IEnumerator StageStart()
    {
        string stageForStart = stage ?? string.Empty;
        string url = ComposeUrl($"/api/train/stage/start?stage={UnityWebRequest.EscapeURL(stageForStart)}&totalProblems={count}");
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = null;
            req.downloadHandler = new DownloadHandlerBuffer();

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Stage20] stage/start 호출 실패: {req.error}\nURL={url}\nResponse={req.downloadHandler.text}");
                yield break;
            }

            var response = JsonUtility.FromJson<StageStartResponse>(req.downloadHandler.text);
            if (response == null || response.data == null || string.IsNullOrWhiteSpace(response.data.stageSessionId))
            {
                Debug.LogError($"[Stage20] stage/start 응답 파싱 실패\nResponse={req.downloadHandler.text}");
                yield break;
            }

            stageSessionId = response.data.stageSessionId;
            if (verboseLogging)
                Debug.Log($"[Stage20] stage/start 완료 → sessionId={stageSessionId}");
        }
    }

    private IEnumerator SendAttemptLog(int problemNumber, int attemptNumber, int expectedCount, int submittedCount, bool isCorrect, string word)
    {
        string url = ComposeUrl("/api/train/attempt");
        string ssid = stageSessionId ?? string.Empty;
        string stg = stage ?? string.Empty;
        // Stage20은 phoneme을 사용하지 않으므로 빈 문자열로 보냄 (다른 Stage와 동일하게)
        // selectedAnswer는 Stone 개수를 문자열로 변환
        string submitted = submittedCount >= 0 ? submittedCount.ToString() : string.Empty;
        string wd = word;

        string json = "{" +
                      "\"stageSessionId\":\"" + JsonEscape(ssid) + "\"," +
                      "\"problemNumber\":" + problemNumber + "," +
                      "\"stage\":\"" + JsonEscape(stg) + "\"," +
                      "\"attemptNumber\":" + attemptNumber + "," +
                      "\"phonemes\":\"" + JsonEscape(string.Empty) + "\"," +
                      "\"selectedAnswer\":\"" + JsonEscape(submitted) + "\"," +
                      "\"word\":" + (wd == null ? "null" : "\"" + JsonEscape(wd) + "\"") + "," +
                      "\"isCorrect\":" + (isCorrect ? "true" : "false") + "," +
                      "\"isReplyCorrect\":null,\"audioUrl\":null}";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyCommonHeaders(req);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage20] attempt 로깅 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={json}\nResp={req.downloadHandler.text}");
            }
            else if (verboseLogging)
            {
                Debug.Log($"[Stage20] attempt 로깅 OK: problem={problemNumber}, attempt={attemptNumber}, correct={isCorrect}");
            }
        }
    }

    private static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private IEnumerator StageComplete()
    {
        if (string.IsNullOrWhiteSpace(stageSessionId)) yield break;

        string url = ComposeUrl($"/api/train/stage/complete?stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}");
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = null;
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage20] stage/complete 호출 실패: {req.error}\nURL={url}\nResponse={req.downloadHandler.text}");
            }
            else if (verboseLogging)
            {
                Debug.Log($"[Stage20] stage/complete 응답 → {req.downloadHandler.text}");
            }
        }
    }

    [Serializable]
    private class QuestionDto
    {
        public int questionId;
        public string sessionId;
        public string problemWord;
        public int wordLength;
        public string wordVoiceUrl;
    }

    [Serializable]
    private class ProblemData
    {
        public string stageSessionId;
        public string sessionId;
        public List<QuestionDto> problems;
    }

    [Serializable]
    private class ProblemListResponse
    {
        public bool success;
        public string message;
        public ProblemData data;
    }

    [Serializable]
    private class StageStartResponse
    {
        public bool success;
        public string message;
        public StageStartData data;
    }

    [Serializable]
    private class StageStartData
    {
        public string stageSessionId;
        public string stage;
        public int totalProblems;
        public string startAt;
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
