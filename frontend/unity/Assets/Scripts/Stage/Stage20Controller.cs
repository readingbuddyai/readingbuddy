using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class Stage20Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "";
    public string stage = "2.2";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";

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

        yield return RecordAndUpload(problem);

        if (clipPerfect) yield return PlayClip(clipPerfect);
        if (clipMagicFeel) yield return PlayClip(clipMagicFeel);

        if (clipCountInstruction) yield return PlayClip(clipCountInstruction);
        if (!string.IsNullOrEmpty(problem.wordVoiceUrl))
            yield return PlayVoiceUrl(problem.wordVoiceUrl);

        yield return RunStoneRound(problem.wordLength);
    }

    private void UpdateProgress(int index, int total, QuestionDto problem)
    {
        string label = $"{index}/{total}";
        if (!string.IsNullOrWhiteSpace(problem.problemWord))
            label += $" {problem.problemWord}";

        if (progressText)
            progressText.text = label;

        if (progressTextTMP)
        {
            progressTextTMP.enableAutoSizing = false;
            progressTextTMP.overflowMode = TextOverflowModes.Overflow;
            progressTextTMP.text = label;
        }
    }

    private IEnumerator RunStoneRound(int expectedCount)
    {
        int attempts = 0;
        bool solved = false;

        while (attempts < maxStoneAttempts && !solved)
        {
            pendingStoneCount = null;
            waitingForStoneCount = true;

            if (stoneBoard)
                stoneBoard.SetActive(true);
            if (countdownText)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = Mathf.CeilToInt(countdownSeconds).ToString();
            }

            onStoneRoundBegin?.Invoke(expectedCount);

            float remaining = countdownSeconds;
            while (remaining > 0f && waitingForStoneCount)
            {
                if (countdownText)
                    countdownText.text = Mathf.CeilToInt(remaining).ToString();

                if (pendingStoneCount.HasValue)
                {
                    waitingForStoneCount = false;
                    break;
                }

                remaining -= Time.deltaTime;
                yield return null;
            }

            waitingForStoneCount = false;
            pendingStoneCount = null;
            if (countdownText)
            {
                countdownText.text = string.Empty;
                countdownText.gameObject.SetActive(false);
            }

            int submitted = pendingStoneCount ?? -1;

            if (countdownText)
            {
                countdownText.text = string.Empty;
                countdownText.gameObject.SetActive(false);
            }

            if (stoneBoard)
                stoneBoard.SetActive(false);
            onStoneRoundEnd?.Invoke();

            if (submitted == expectedCount)
            {
                solved = true;
                if (clipCountCorrect1) yield return PlayClip(clipCountCorrect1);
                if (clipCountCorrect2) yield return PlayClip(clipCountCorrect2);
            }
            else
            {
                attempts++;
                if (clipCountWrong1) yield return PlayClip(clipCountWrong1);
                if (clipCountWrong2) yield return PlayClip(clipCountWrong2);
            }
        }
    }

    public void ReportStoneCount(int count)
    {
        if (!waitingForStoneCount) return;
        pendingStoneCount = count;
        waitingForStoneCount = false;
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
                Debug.LogError($"[Stage20] 문제 요청 실패: {req.error}\nURL={url}");
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

            if (parsed == null || parsed.data == null || parsed.data.problems == null || parsed.data.problems.Count == 0)
            {
                Debug.LogError("[Stage20] 문제 데이터가 비어 있습니다.");
                yield break;
            }

            if (!string.IsNullOrEmpty(parsed.data.sessionId))
            {
                foreach (var problem in parsed.data.problems)
                    problem.sessionId = parsed.data.sessionId;
            }

            onCompleted?.Invoke(parsed.data.problems);
        }
    }

    private IEnumerator RecordAndUpload(QuestionDto problem)
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

        string url = ComposeUrl("/api/train/check/voice");
        var form = new WWWForm();
        form.AddField("sessionId", problem.sessionId ?? string.Empty);
        form.AddField("stage", stage ?? string.Empty);
        form.AddField("problemWord", problem.problemWord ?? string.Empty);
        form.AddField("questionId", problem.questionId);
        form.AddBinaryData("file", wav, "voice.wav", "audio/wav");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage20] 음성 업로드 실패: {req.error}");
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

        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        if (path.StartsWith("/")) path = path.Substring(1);
        return baseUrl + path;
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
