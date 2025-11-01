using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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
    public string stage = "1.1.2";
    public int count = 5;
    [Tooltip("Authorization: Bearer {token}")]
    public string authToken = "";

    [Header("UI 참조")]
    public Text progressText;
    public Image mainImage;
    public RectTransform optionsContainer;
    public Button optionButtonPrefab;

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
    public Vector2 optionButtonPreferredSize = new Vector2(1200f, 600f);

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

        StartCoroutine(RunStage());
    }

    private IEnumerator RunStage()
    {
        yield return PlayClip(sfxStart);
        yield return RunIntroSequence();

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

        yield return PlayClip(clipNextLesson);
    }

    private IEnumerator RunIntroSequence()
    {
        yield return PlayClip(clipIntroGreeting);
        yield return PlayClip(clipIntroPraise);
        yield return PlayClip(clipIntroChallenge);
    }

    private IEnumerator RunOneQuestion(int index, int total, QuestionDto q)
    {
        if (progressText)
        {
            var suffix = string.IsNullOrWhiteSpace(q.targetPhoneme) ? string.Empty : $" · {q.targetPhoneme}";
            progressText.text = $"문제 {index}/{total}{suffix}";
        }

        yield return LoadAndShowImage(q.imageUrl);

        yield return PlayClip(clipListen);
        yield return PlayVoiceUrl(q.voiceUrl);

        yield return PlayClip(clipPromptRepeat);
        yield return RecordAndUpload(q);

        yield return PlayClip(clipPowerUp);
        yield return PlayClip(clipMatchPrompt);

        yield return ShowOptionsUntilCorrect(q);
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
            rt.sizeDelta = new Vector2(0f, Mathf.Max(optionsHeight, optionButtonPreferredSize.y + 20f));
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

            if (!string.IsNullOrEmpty(parsed.data.sessionId))
            {
                Debug.Log($"[Stage12] 세션 ID 수신: {parsed.data.sessionId}");
                foreach (var problem in parsed.data.problems)
                    problem.sessionId = parsed.data.sessionId;
            }
            else
            {
                Debug.LogWarning("[Stage12] 응답에 sessionId가 없습니다.");
            }

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

            onCompleted?.Invoke(parsed.data.problems);
        }
    }

    private IEnumerator ShowOptionsUntilCorrect(QuestionDto q)
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

        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);

        bool answered = false;
        bool correct = false;

        void SetupOption(WordOptionDto opt)
        {
            var btn = Instantiate(optionButtonPrefab, optionsContainer);
            var text = btn.GetComponentInChildren<Text>();
            if (text) text.text = opt.word;

            var rt = btn.GetComponent<RectTransform>();
            if (rt) rt.sizeDelta = optionButtonPreferredSize;

            var layout = btn.GetComponent<LayoutElement>();
            if (layout)
            {
                layout.preferredWidth = optionButtonPreferredSize.x;
                layout.preferredHeight = optionButtonPreferredSize.y;
                layout.layoutPriority = Mathf.Max(layout.layoutPriority, 1);
            }

            btn.onClick.AddListener(() =>
            {
                answered = true;
                correct = opt.answer;
            });
        }

        foreach (var opt in q.options ?? Enumerable.Empty<WordOptionDto>())
            SetupOption(opt);

        if (optionsContainer.childCount == 0)
        {
            Debug.LogWarning("[Stage12] 선택지가 없습니다.");
            yield break;
        }

        while (true)
        {
            yield return new WaitUntil(() => answered);

            if (correct)
            {
                yield return PlayClip(clipCorrect);
                break;
            }
            else
            {
                yield return PlayClip(clipWrong);
                answered = false;
            }
        }

        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);
    }

    private IEnumerator LoadAndShowImage(string imageUrl)
    {
        if (mainImage == null || string.IsNullOrEmpty(imageUrl))
            yield break;

        mainImage.enabled = false;
        mainImage.sprite = null;

        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            ApplyCommonHeaders(req);
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
            yield break;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(voiceUrl, GuessAudioType(voiceUrl)))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage12] 음성 로드 실패: {req.error}\nURL={voiceUrl}");
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

        string url = ComposeUrl("/api/train/check/voice");
        var form = new WWWForm();
        form.AddField("sessionId", q.sessionId ?? string.Empty);
        form.AddField("stage", stage ?? string.Empty);
        form.AddField("problemId", q.questionId);
        form.AddBinaryData("file", wav, "voice.wav", "audio/wav");

        Debug.Log($"[Stage12] 업로드 파라미터 → sessionId={q.sessionId ?? "<null>"}, stage={stage}, problemId={q.questionId}");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage12] 음성 업로드 실패: {req.error}");
            }
            else
            {
                CollectFeedback(req.downloadHandler.text);
            }
        }
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
            var headerValue = $"Bearer {authToken}";
            Debug.Log($"[Stage12] Authorization 헤더 설정: {headerValue}");
            req.SetRequestHeader("Authorization", $"Bearer {authToken}");
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
