using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

// Stage 1.1 진행 컨트롤러
// - GET: /api/train/set?stage=1.1.1&count=5
// - POST: /api/train/stage/start (body: { userId, stage, totalProblems })
// - GET: /api/train/set?stage=1.1.1&count=5
// - POST: /api/train/check/voice?sessionId=&stage=&problemId= (multipart: audio=voice.wav)
// - POST: /api/train/stage/complete (body: { sessionId })
// 흐름(문항당):
//  1) 상단에 "문제 i/5" 표시, 중앙 이미지(imageUrl) 표시
//  2) "앞에 있는 그림을 잘 보고, 소리를 따라해봐~!" 안내 음성 → 마이크 녹음 → 업로드
//  3) "아까 발음했던 소리가 둘 중 어떤건지 맞춰볼래?" 안내 음성 → voiceUrl 재생 → 옵션 버튼 표시
//  4) 정답이면 "최고야" 재생 후 다음 문항, 오답이면 "다시 한번 골라볼까?" 재생 후 재선택 대기
    public class Stage11Controller : MonoBehaviour
    {
        [Header("API 설정")]
        public string baseUrl = ""; // 빈 값이면 절대경로/상대경로 그대로 사용
        public string stage = "1.1.1";
        public int count = 5;
        [Tooltip("Authorization: Bearer {token}")]
        public string authToken = ""; // 필요 시 토큰
        [Header("세션")]
        [Tooltip("/api/train/stage/start 응답의 sessionId. 미설정 시 업로드 403 가능")]
        public string sessionId = "";
        [Tooltip("스웨거 start 바디에 포함되는 userId (선택)")]
        public int userId = 0;

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
        [Tooltip("문제 전환 시 가이드 다시 이동 여부")]
        public bool enableGuideMoveBetweenQuestions = false;
        private bool _guideMoved;
        private Coroutine _guideMoveCo;
        private bool _guideLocked;
        private Vector2 _guideFinalPos;
        private Vector2 _guideFinalSize;

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

        [Header("개발용 우회")]
        [Tooltip("/api/train/stage/start 요청을 건너뛰고 문제 GET만 진행합니다.")]
        public bool bypassStartRequest = true;
        [Tooltip("음성 녹음 및 /api/train/check/voice 업로드를 건너뜁니다.")]
        public bool bypassVoiceUpload = true;

        [Header("진단/로그")]
        [Tooltip("수신한 문제 전체를 상세 로그로 출력합니다.")]
        public bool logQuestionsVerbose = true;
        [Tooltip("이미지 로드 실패 시 자리표시 이미지를 중앙에 표시합니다.")]
        public bool showPlaceholderOnImageFail = true;

    [System.Serializable]
    public enum OptionLabelMode { UnicodeOnly, ValueOnly, UnicodeThenValue, ValueThenUnicode }

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
        public int id;            // fallback: 일부 응답에서 questionId 대신 id 사용 가능
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
        baseUrl   = EnvConfig.ResolveBaseUrl(baseUrl);
        authToken = EnvConfig.ResolveAuthToken(authToken);
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
        StartCoroutine(RunStage());
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
        text.fontSize = 64;
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
        yield return PlayClip(sfxStart);

        // 0-1) 도입 대사 (가이드 이미지는 고정, 이동은 sfxNext 타이밍에 수행)
        yield return RunIntroSequence();
        if (guideImage && _guideMoveCo == null && (!_guideMoved || !guideMoveOnlyOnce))
        {
            Debug.Log("[Stage11] Guide move: trigger after intro");
            _guideMoveCo = StartCoroutine(MoveGuideAndScaleOverTime(guideMoveDuration));
            _guideMoved = true;
        }

        // 0-2) 세션 시작 호출로 sessionId 확보 (테스트 시 우회 가능)
        if (!bypassStartRequest && string.IsNullOrWhiteSpace(sessionId))
        {
            yield return StartStageSession();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                Debug.LogWarning("[Stage11] sessionId 발급 실패. bypassStartRequest=true 이므로 계속 진행합니다.");
            }
        }

        // 문제 요청
        string url = ComposeUrl($"/api/train/set?stage={UnityWebRequest.EscapeURL(stage)}&count={count}");
        using (var req = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                var body = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogError($"[Stage11] 문제 요청 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={body}");
                yield break;
            }

            var json = req.downloadHandler.text;
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
                        Debug.Log($"[Stage11] Q{qi + 1}: id={qd.id}, qid={qd.questionId}, value={qd.value}, imageUrl={qd.imageUrl}, voiceUrl={qd.voiceUrl}, options=[{opts}]");
                    }
                }
            }

            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                yield return RunOneQuestion(i + 1, questions.Count, q);
                // 다음 문제로 넘어가는 효과음 (마지막 문제 제외)
                if (i < questions.Count - 1)
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
        }

        // 세션 완료 보고 (best-effort)
        yield return CompleteStageSession();
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

    [Serializable]
    private class StartStageBody
    {
        public int userId;
        public string stage;
        public int totalProblems;
    }

    [Serializable]
    private class StartStageData
    {
        public string sessionId;
        public string stage;
        public int totalProblems;
        public string startAt;
    }

    [Serializable]
    private class StartStageResponse
    {
        public bool success;
        public string message;
        public StartStageData data;
    }

    [Serializable]
    private class CompleteStageBody
    {
        public string sessionId;
    }

    [Serializable]
    private class CompleteStageData
    {
        public string sessionId;
        public List<string> voiceResult;
    }

    [Serializable]
    private class CompleteStageResponse
    {
        public bool success;
        public string message;
        public CompleteStageData data;
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

    // 세션 시작: /api/train/stage/start
    private IEnumerator StartStageSession()
    {
        string url = ComposeUrl("/api/train/stage/start");
        int uid = userId > 0 ? userId : TryInferUserIdFromToken();
        var body = new StartStageBody { userId = uid, stage = stage, totalProblems = count };
        var json = JsonUtility.ToJson(body);
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            Debug.Log($"[Stage11] stage/start 요청 바디: {json}");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                var resp = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogError($"[Stage11] stage/start 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={resp}");
                yield break;
            }
            var respJson = req.downloadHandler.text;
            try
            {
                var resp = JsonUtility.FromJson<StartStageResponse>(respJson);
                if (resp != null && resp.data != null && !string.IsNullOrWhiteSpace(resp.data.sessionId))
                {
                    sessionId = resp.data.sessionId;
                    Debug.Log($"[Stage11] sessionId 발급: {sessionId}");
                }
                else
                {
                    Debug.LogError($"[Stage11] stage/start 응답 파싱 실패\nRaw={respJson}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Stage11] stage/start 파싱 예외: {e.Message}\nRaw={respJson}");
            }
        }
    }

    // JWT 토큰에서 userId 유추 (없으면 0)
    private int TryInferUserIdFromToken()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(authToken)) return 0;
            var token = authToken.Trim();
            // "Bearer ..." 형태 방어
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7).Trim();
            var parts = token.Split('.');
            if (parts.Length < 2) return 0;
            string payloadB64 = parts[1];
            // base64url → base64
            payloadB64 = payloadB64.Replace('-', '+').Replace('_', '/');
            switch (payloadB64.Length % 4)
            {
                case 2: payloadB64 += "=="; break;
                case 3: payloadB64 += "="; break;
            }
            var bytes = System.Convert.FromBase64String(payloadB64);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            // 간단 파싱: 숫자 후보 키 찾기
            int val;
            if (TryFindIntValue(json, new []{"userId","id","sub","nameid"}, out val))
                return val;
        }
        catch { }
        return 0;
    }

    private bool TryFindIntValue(string json, string[] keys, out int value)
    {
        value = 0;
        try
        {
            foreach (var k in keys)
            {
                // 매우 단순한 키 검색(정규식 없이)
                var idx = json.IndexOf("\"" + k + "\"", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var colon = json.IndexOf(':', idx);
                if (colon < 0) continue;
                var end = colon + 1;
                // 숫자 부분 추출
                while (end < json.Length && char.IsWhiteSpace(json[end])) end++;
                var start = end;
                while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
                if (end > start)
                {
                    var numStr = json.Substring(start, end - start);
                    if (int.TryParse(numStr, out value)) return true;
                }
            }
        }
        catch { }
        return false;
    }

    // 세션 완료: /api/train/stage/complete
    private IEnumerator CompleteStageSession()
    {
        if (string.IsNullOrWhiteSpace(sessionId)) yield break;
        string url = ComposeUrl("/api/train/stage/complete");
        var body = new CompleteStageBody { sessionId = sessionId };
        var json = JsonUtility.ToJson(body);
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                var resp = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogWarning($"[Stage11] stage/complete 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={resp}");
                yield break;
            }
            Debug.Log("[Stage11] stage/complete OK");
        }
    }

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
        // 진행도 표시
        if (progressText) progressText.text = $"문제 {index}/{total}";

        // 이미지 로드 및 표시
        var pt = EnsureProgressText();
        if (pt != null) pt.text = $"{index} / {total}";
        yield return LoadAndShowImage(q.imageUrl);

        // 1) [1.1.3] 안내 대사
        yield return PlayClip(clipSeeAndChant);

        // voiceUrl 재생
        yield return PlayVoiceUrl(q.voiceUrl);

        // 2) [1.1.4] 이제 너 차례야 → 녹음 업로드
        yield return PlayClip(clipYourTurn);
        if (!bypassVoiceUpload)
            yield return RecordAndUpload(q);
        else
            yield return new WaitForSeconds(recordSeconds);

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
            // 외부(S3/CloudFront 등)일 수 있으므로 인증 헤더는 붙이지 않음
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

        // 업로드 (Swagger)
        // POST /api/train/check/voice?sessionId=&stage=&problemId=
        int qid = q.questionId != 0 ? q.questionId : q.id;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Debug.LogWarning("[Stage11] sessionId가 비어 있습니다. 업로드 403이 발생할 수 있습니다. /api/train/stage/start 호출로 sessionId를 발급받으세요.");
        }
        string qs = $"sessionId={UnityWebRequest.EscapeURL(sessionId ?? string.Empty)}&stage={UnityWebRequest.EscapeURL(stage ?? string.Empty)}&problemId={UnityWebRequest.EscapeURL(qid.ToString())}";
        string url = ComposeUrl($"/api/train/check/voice?{qs}");
        var form = new WWWForm();
        // multipart 필드명은 audio
        form.AddBinaryData("audio", wav, "voice.wav", "audio/wav");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            // 일부 서버/프록시는 chunked 업로드를 거부합니다.
            req.chunkedTransfer = false;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                var body = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogWarning($"[Stage11] 음성 업로드 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={body}");
            }
            else
            {
                Debug.Log($"[Stage11] 업로드 완료: {req.downloadHandler.text}");
            }
        }
    }

    private void ApplyCommonHeaders(UnityWebRequest req)
    {
        if (!string.IsNullOrWhiteSpace(authToken))
        {
            var tokenTrim = authToken.Trim();
            if (tokenTrim.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                tokenTrim = tokenTrim.Substring(7).Trim();
            req.SetRequestHeader("Authorization", $"Bearer {tokenTrim}");
            // 디버그: 토큰 길이만 로깅
            Debug.Log($"[Stage11] Auth header attached (len={tokenTrim.Length})");
        }
        req.SetRequestHeader("Accept", "application/json");
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
        // Show options container only during selection phase
        optionsContainer.gameObject.SetActive(true);
        optionsContainer.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);
        optionsContainer.SetAsLastSibling();

        bool answered = false;
        bool correct = false;
        int wrongCount = 0;

        string ComposeOptionLabel(OptionDto opt)
        {
            string uni = opt != null ? (opt.unicode ?? string.Empty) : string.Empty;
            string val = opt != null ? (opt.value ?? string.Empty) : string.Empty;
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
                var chosenCandidates = new List<string>();
                if (!string.IsNullOrEmpty(opt.value)) chosenCandidates.Add(opt.value.Trim());
                if (!string.IsNullOrEmpty(opt.unicode)) chosenCandidates.Add(opt.unicode.Trim());
                var answerCandidates = new List<string>();
                if (!string.IsNullOrEmpty(q.value)) answerCandidates.Add(q.value.Trim());
                if (!string.IsNullOrEmpty(q.unicode)) answerCandidates.Add(q.unicode.Trim());
                correct = chosenCandidates.Any(cc => answerCandidates.Any(ac => string.Equals(cc, ac, System.StringComparison.Ordinal)));
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

    private void LateUpdate()
    {
        if (_guideLocked && guideImage)
        {
            guideImage.anchoredPosition = _guideFinalPos;
            guideImage.sizeDelta = _guideFinalSize;
        }
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
        prt.sizeDelta = new Vector2(1200, 900);
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
        t.fontSize = 72;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.font = uiFont ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 버튼들
        Vector2 btnSize = optionButtonPreferredSize;
        float gap = 40f;
        // 다시 학습하기
        var btn1 = Instantiate(optionButtonPrefab, panel.transform as RectTransform);
        var btn1rt = btn1.GetComponent<RectTransform>();
        btn1rt.anchorMin = new Vector2(0.5f, 0.5f);
        btn1rt.anchorMax = new Vector2(0.5f, 0.5f);
        btn1rt.pivot = new Vector2(1f, 0.5f);
        btn1rt.sizeDelta = btnSize;
        btn1rt.anchoredPosition = new Vector2(-gap*0.5f, -100f);
        var txt1 = btn1.GetComponentInChildren<Text>();
        var tmp1 = btn1.GetComponentInChildren<TMP_Text>();
        if (txt1) { txt1.text = "다시 학습하기"; if (uiFont) txt1.font = uiFont; }
        else if (tmp1) { tmp1.text = "다시 학습하기"; if (tmpFont) tmp1.font = tmpFont; }
        btn1.onClick.AddListener(() => { Destroy(overlay); RestartStage(); });

        // 로비로 나가기
        var btn2 = Instantiate(optionButtonPrefab, panel.transform as RectTransform);
        var btn2rt = btn2.GetComponent<RectTransform>();
        btn2rt.anchorMin = new Vector2(0.5f, 0.5f);
        btn2rt.anchorMax = new Vector2(0.5f, 0.5f);
        btn2rt.pivot = new Vector2(0f, 0.5f);
        btn2rt.sizeDelta = btnSize;
        btn2rt.anchoredPosition = new Vector2(gap*0.5f, -100f);
        var txt2 = btn2.GetComponentInChildren<Text>();
        var tmp2 = btn2.GetComponentInChildren<TMP_Text>();
        if (txt2) { txt2.text = "로비로 나가기"; if (uiFont) txt2.font = uiFont; }
        else if (tmp2) { tmp2.text = "로비로 나가기"; if (tmpFont) tmp2.font = tmpFont; }
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
        if (mainImage)
        {
            mainImage.enabled = false;
            mainImage.sprite = null;
        }
        _guideLocked = false;
        _guideMoveCo = null;
        _guideMoved = false;
        sessionId = string.Empty;
        StartCoroutine(RunStage());
    }

    private void GoToLobby()
    {
        if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene(SceneId.Lobby);
        else SceneManager.LoadScene(SceneId.Lobby);
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

    private string ComposeUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return path; // 상대/절대 그대로
        if (path.StartsWith("http")) return path;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        if (path.StartsWith("/")) path = path.Substring(1);
        return baseUrl + path;
    }
}
