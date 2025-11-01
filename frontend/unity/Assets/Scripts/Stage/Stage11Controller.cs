using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Stage 1.1 진행 컨트롤러
// - GET: /api/train/question?stage=1.1&count=5
// - POST: /api/train/check/voice (multipart: questionId, file=voice.wav)
// 흐름(문항당):
//  1) 상단에 "문제 i/5" 표시, 중앙 이미지(imageUrl) 표시
//  2) "앞에 있는 그림을 잘 보고, 소리를 따라해봐~!" 안내 음성 → 마이크 녹음 → 업로드
//  3) "아까 발음했던 소리가 둘 중 어떤건지 맞춰볼래?" 안내 음성 → voiceUrl 재생 → 옵션 버튼 표시
//  4) 정답이면 "최고야" 재생 후 다음 문항, 오답이면 "다시 한번 골라볼까?" 재생 후 재선택 대기
    public class Stage11Controller : MonoBehaviour
    {
    [Header("API 설정")]
    public string baseUrl = ""; // 빈 값이면 절대경로/상대경로 그대로 사용
    public string stage = "1.1";
    public int count = 5;

    [Header("UI 참조")]
    public Text progressText;            // 상단 "문제 1/5"
    public Image mainImage;              // 중앙 큰 이미지
    public RectTransform optionsContainer; // 하단 옵션 버튼 부모
    public Button optionButtonPrefab;    // 동적 생성용 버튼 프리팹 (Text 자식 포함)

    [Header("오디오 재생")]
    public AudioSource audioSource;      // 안내/피드백/효과음 재생용
    // 시작/전환 효과음
    public AudioClip sfxStart;           // (시작 효과음)
    public AudioClip sfxNext;            // (다음 문제로 넘어가는 효과음)

    // 도입 대사
    public AudioClip introClip1;         // [1.1.1] 안녕, 꼬마 마법사!
    public AudioClip introClip2;         // [1.1.2] 지금부터 ‘모음 주문’ 수업을 시작할 거야!

    // 각 문제 흐름 대사
    public AudioClip clipSeeAndChant;    // [1.1.3] 앞에 떠오른 마법 그림을 잘 보고...
    public AudioClip clipYourTurn;       // [1.1.4] 이제 너 차례야, 주문을 들려줘!
    public AudioClip clipGreat;          // [1.1.5] 우와~ 정말 멋지게 외웠는걸!
    public AudioClip clipChoose;         // [1.1.6] 두 개 중 어떤 소리였는지 맞춰볼래?

    // 정답/오답 피드백
    public AudioClip sfxCorrectClip;     // [1.1.7.1] 완벽해!
    public AudioClip sfxWrongClip;       // [1.1.7.2] 아이쿠! 다시 한 번 집중해 볼까?

    [Header("마이크 설정")]
    public int recordSeconds = 3;        // 발음 녹음 시간
    public int recordSampleRate = 44100; // 발음 샘플레이트
        public bool micDuringChoice = true;  // 선택 단계에서도 마이크 ON
        [Range(0,5)] public int maxWrongAttempts = 2; // 오답 허용 횟수 (기본 2)

        [Header("가이드 이미지(도입/전환 연출)")]
        public RectTransform guideImage;       // 중앙 안내 이미지(선택)
        public Vector2 guideStartSize = new Vector2(1500, 1500);
        public Vector2 guideEndSize   = new Vector2(800, 800);
        public float guideMoveDuration = 1.5f; // sfxNext가 재생되는 동안 살며시 이동/축소
        public bool guideMoveOnlyOnce  = true; // 최초 1회만 이동할지
        private bool _guideMoved;

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

    [Serializable]
    public class OptionDto
    {
        public int id;
        public string value;
        public string unicode;
    }

    [Serializable]
    public class QuestionDto
    {
        public int questionId;
        public string value;      // 정답 값(예: "ㅏ")
        public string unicode;
        public string voiceUrl;   // 정답 음성 샘플 URL
        public string imageUrl;   // 입모양 이미지 URL
        public List<OptionDto> options;
    }

    [Serializable]
    public class QuestionListResponse
    {
        public bool success;
        public string message;
        public List<QuestionDto> data;
    }

    private void Start()
    {
        // baseUrl 자동 해석 (ENV > Resources > Inspector)
        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);
        if (applyAutoLayout)
            TryApplyAutoLayout();
        StartCoroutine(RunStage());
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
        // 0) 시작 효과음
        yield return PlayClip(sfxStart);

        // 0-1) 도입 대사 (가이드 이미지는 고정, 이동은 sfxNext 타이밍에 수행)
        yield return RunIntroSequence();

        // 문제 요청
        string url = ComposeUrl($"/api/train/question?stage={UnityWebRequest.EscapeURL(stage)}&count={count}");
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Stage11] 문제 요청 실패: {req.error}\nURL={url}");
                yield break;
            }

            var json = req.downloadHandler.text;
            var list = JsonUtility.FromJson<QuestionListWrapper>(WrapJson(json));
            if (list == null || list.data == null || list.data.Count == 0)
            {
                Debug.LogError("[Stage11] 응답 파싱 실패 또는 데이터 없음");
                yield break;
            }

            for (int i = 0; i < list.data.Count; i++)
            {
                var q = list.data[i];
                yield return RunOneQuestion(i + 1, list.data.Count, q);
                // 다음 문제로 넘어가는 효과음 (마지막 문제 제외)
                if (i < list.data.Count - 1)
                {
                    // sfxNext 재생과 동시에 가이드 이미지 이동/축소(최초 1회)
                    if (guideImage && (!_guideMoved || !guideMoveOnlyOnce))
                    {
                        StartCoroutine(MoveGuideAndScaleOverTime(guideMoveDuration));
                        _guideMoved = true;
                    }
                    yield return PlayClip(sfxNext);
                }
            }
        }
    }

    // JsonUtility는 루트에 배열을 직접 파싱하지 못하므로 래퍼 클래스로 우회
    [Serializable]
    private class QuestionListWrapper
    {
        public bool success;
        public string message;
        public List<QuestionDto> data;
    }

    private string WrapJson(string raw)
    {
        // 서버가 이미 { success, message, data:[...] } 형태라면 그대로 사용
        // 아닌 경우를 대비한 방어 로직은 생략
        return raw;
    }

    private IEnumerator RunOneQuestion(int index, int total, QuestionDto q)
    {
        // 진행도 표시
        if (progressText) progressText.text = $"문제 {index}/{total}";

        // 이미지 로드 및 표시
        yield return LoadAndShowImage(q.imageUrl);

        // 1) [1.1.3] 안내 대사
        yield return PlayClip(clipSeeAndChant);

        // voiceUrl 재생
        yield return PlayVoiceUrl(q.voiceUrl);

        // 2) [1.1.4] 이제 너 차례야 → 녹음 업로드
        yield return PlayClip(clipYourTurn);
        yield return RecordAndUpload(q);

        // 3) [1.1.5] 칭찬 대사
        yield return PlayClip(clipGreat);

        // 4) [1.1.6] 선택 유도 대사
        yield return PlayClip(clipChoose);
        if (micDuringChoice)
        {
            // 선택 단계에서도 짧게 마이크 ON (비차단적)
            StartCoroutine(RecordBackgroundCoroutine(recordSeconds));
        }
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
                Debug.LogWarning($"[Stage11] 이미지 로드 실패: {req.error}\nURL={imageUrl}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            mainImage.sprite = sprite;
            mainImage.preserveAspect = true;
            mainImage.enabled = true; // 로드 후 표시
        }
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

        var audioType = GuessAudioType(voiceUrl);
        using (var req = UnityWebRequestMultimedia.GetAudioClip(voiceUrl, audioType))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage11] 음성 로드 실패: {req.error}\nURL={voiceUrl}");
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
        // 마이크 녹음
        var clip = StartMic(recordSeconds, recordSampleRate);
        yield return new WaitForSeconds(recordSeconds);
        var wav = WavUtility.FromAudioClip(clip);

        // 업로드
        string url = ComposeUrl("/api/train/check/voice");
        var form = new WWWForm();
        form.AddField("questionId", q.questionId);
        // 서버 사양에 따라 필드명(file/formFile 등) 맞춰주세요
        form.AddBinaryData("file", wav, "voice.wav", "audio/wav");

        using (var req = UnityWebRequest.Post(url, form))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Stage11] 음성 업로드 실패: {req.error}");
            }
            else
            {
                Debug.Log($"[Stage11] 업로드 완료: {req.downloadHandler.text}");
            }
        }
    }

    private IEnumerator RecordBackgroundCoroutine(int seconds)
    {
        var clip = StartMic(seconds, recordSampleRate);
        yield return new WaitForSeconds(seconds);
        // 배경 녹음 결과는 사용하지 않음
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

        bool answered = false;
        bool correct = false;
        int wrongCount = 0;

        void SetupOne(OptionDto opt)
        {
            var btn = Instantiate(optionButtonPrefab, optionsContainer);
            var text = btn.GetComponentInChildren<Text>();
            if (text) text.text = opt.value;
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
            btn.onClick.AddListener(() =>
            {
                answered = true;
                correct = string.Equals(opt.value, q.value, StringComparison.Ordinal);
            });
        }

        foreach (var opt in q.options) SetupOne(opt);

        // 선택 대기 → 피드백 → 정답일 때까지 반복
        while (true)
        {
            yield return new WaitUntil(() => answered);

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
    }

    // 도입 시퀀스: [1.1.1] + [1.1.2] 오디오만 재생 (이미지 이동은 sfxNext 타이밍)
    private IEnumerator RunIntroSequence()
    {
        // 시작 크기 세팅(선택)
        if (guideImage && guideStartSize.sqrMagnitude > 0)
            guideImage.sizeDelta = guideStartSize;

        // 도입 대사 재생(연속)
        yield return PlayClip(introClip1);
        yield return PlayClip(introClip2);
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

    private string ComposeUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return path; // 상대/절대 그대로
        if (path.StartsWith("http")) return path;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        if (path.StartsWith("/")) path = path.Substring(1);
        return baseUrl + path;
    }
}
