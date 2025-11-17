using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class StageSessionController
{
    public string BaseUrl { get; private set; }
    public string AuthToken { get; private set; }

    public Action<string> Log = Debug.Log;
    public Action<string> LogWarning = Debug.LogWarning;
    public Action<string> LogError = Debug.LogError;

    public StageSessionController(string baseUrl = "", string authToken = "")
    {
        Configure(baseUrl, authToken);
    }

    public void Configure(string baseUrl, string authToken)
    {
        BaseUrl = baseUrl ?? string.Empty;
        AuthToken = authToken ?? string.Empty;
    }

    public void SetBaseUrl(string baseUrl)
    {
        BaseUrl = baseUrl ?? string.Empty;
    }

    public void SetAuthToken(string token)
    {
        AuthToken = token ?? string.Empty;
    }

    public IEnumerator StartStageSession(string stageParamSource, int totalProblems, Action<StageStartResult> callback)
    {
        var result = new StageStartResult();
        string stageParam = UnityWebRequest.EscapeURL(stageParamSource ?? string.Empty);
        string url = ComposeUrl($"/api/train/stage/start?stage={stageParam}&totalProblems={Mathf.Max(1, totalProblems)}");
        var payload = new StartStageBody
        {
            stage = stageParamSource ?? string.Empty,
            totalProblems = Mathf.Max(1, totalProblems)
        };
        string payloadJson = JsonUtility.ToJson(payload);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(payloadJson) ? "{}" : payloadJson);

        // 디버깅: 요청 전 토큰 상태 확인
        bool hasToken = !string.IsNullOrWhiteSpace(AuthToken);
        string tokenPreview = hasToken && AuthToken.Length > 20
            ? $"{AuthToken.Substring(0, 10)}...{AuthToken.Substring(AuthToken.Length - 10)}"
            : (hasToken ? AuthToken : "EMPTY");
        Log?.Invoke($"[StageSession] StartStageSession 준비: URL={url}, AuthToken 있음={hasToken}, 길이={AuthToken?.Length ?? 0}, 미리보기={tokenPreview}");

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = new UploadHandlerRaw(payloadBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            result.ResponseCode = req.responseCode;
            result.RawBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogError?.Invoke($"[StageSession] stage/start 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={result.RawBody}");
                
                // 403 또는 401 에러는 토큰 만료로 간주
                if (req.responseCode == 403 || req.responseCode == 401)
                {
                    LogWarning?.Invoke($"[StageSession] ⚠️ 토큰 만료 감지 (code={req.responseCode}). AuthManager에 알림.");
                    if (AuthManager.Instance != null)
                    {
                        AuthManager.Instance.HandleTokenExpired();
                    }
                    else
                    {
                        LogError?.Invoke("[StageSession] ❌ AuthManager.Instance가 null입니다!");
                    }
                }
                
                callback?.Invoke(result);
                yield break;
            }
        }

        try
        {
            var resp = JsonUtility.FromJson<StartStageResponse>(result.RawBody);
            if (resp != null && resp.data != null && !string.IsNullOrWhiteSpace(resp.data.stageSessionId))
            {
                result.Success = true;
                result.StageSessionId = resp.data.stageSessionId;
                Log?.Invoke($"[StageSession] stageSessionId 발급: {result.StageSessionId}");
            }
            else
            {
                LogError?.Invoke($"[StageSession] stage/start 응답 파싱 실패\nRaw={result.RawBody}");
            }
        }
        catch (Exception e)
        {
            LogError?.Invoke($"[StageSession] stage/start 파싱 예외: {e.Message}\nRaw={result.RawBody}");
        }

        callback?.Invoke(result);
    }

    public IEnumerator FetchQuestionSet(string stage, int count, string stageSessionId, Action<QuestionSetResult> callback)
    {
        var result = new QuestionSetResult();
        string url = ComposeUrl($"/api/train/set?stage={UnityWebRequest.EscapeURL(stage ?? string.Empty)}&count={Mathf.Max(1, count)}");
        if (!string.IsNullOrWhiteSpace(stageSessionId))
            url += $"&stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}";

        // 네트워크 상태 및 URL 확인 로그
        Log?.Invoke($"[StageSession] FetchQuestionSet 시작: BaseUrl={BaseUrl}, URL={url}");
        
        // URL 유효성 검사
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            LogError?.Invoke($"[StageSession] ❌ BaseUrl이 비어있습니다! DNS 해석을 시도할 수 없습니다.");
            result.ResponseCode = 0;
            callback?.Invoke(result);
            yield break;
        }
        
        // BaseUrl에서 호스트 추출 및 검증
        try
        {
            var uri = new System.Uri(BaseUrl);
            string host = uri.Host;
            Log?.Invoke($"[StageSession] 호스트 추출: {host} (전체 BaseUrl: {BaseUrl})");
            
            if (string.IsNullOrWhiteSpace(host))
            {
                LogError?.Invoke($"[StageSession] ❌ BaseUrl에서 호스트를 추출할 수 없습니다: {BaseUrl}");
                result.ResponseCode = 0;
                callback?.Invoke(result);
                yield break;
            }
        }
        catch (Exception ex)
        {
            LogError?.Invoke($"[StageSession] ❌ BaseUrl 파싱 실패: {ex.Message}\nBaseUrl={BaseUrl}");
            result.ResponseCode = 0;
            callback?.Invoke(result);
            yield break;
        }
        
        // 네트워크 연결 확인
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            LogError?.Invoke($"[StageSession] ⚠️ 네트워크 연결이 없습니다. (NetworkReachability.NotReachable)\nURL={url}");
            result.ResponseCode = 0;
            callback?.Invoke(result);
            yield break;
        }
        
        Log?.Invoke($"[StageSession] 네트워크 상태: {Application.internetReachability}");

        using (var req = UnityWebRequest.Get(url))
        {
            // 타임아웃 설정 (30초)
            req.timeout = 30;
            
            ApplyCommonHeaders(req);
            
            Log?.Invoke($"[StageSession] 요청 전송 중... URL={url}");
            yield return req.SendWebRequest();

            result.ResponseCode = req.responseCode;
            result.RawBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
            {
                string errorDetails = $"[StageSession] 문제 요청 실패: {req.error} (code={req.responseCode})\n" +
                                    $"URL={url}\n" +
                                    $"BaseUrl={BaseUrl}\n" +
                                    $"NetworkReachability={Application.internetReachability}\n" +
                                    $"Result={req.result}\n" +
                                    $"Body={result.RawBody}";
                
                LogError?.Invoke(errorDetails);
                
                // 네트워크 관련 오류인 경우 추가 정보 제공
                if (req.result == UnityWebRequest.Result.ConnectionError || 
                    req.result == UnityWebRequest.Result.DataProcessingError)
                {
                    LogError?.Invoke($"[StageSession] 네트워크 오류 상세:\n" +
                                   $"- ConnectionError: {req.result == UnityWebRequest.Result.ConnectionError}\n" +
                                   $"- DataProcessingError: {req.result == UnityWebRequest.Result.DataProcessingError}\n" +
                                   $"- Error: {req.error}\n" +
                                   $"- 네트워크 상태: {Application.internetReachability}");
                }
                
                // 403 또는 401 에러는 토큰 만료로 간주
                if (req.responseCode == 403 || req.responseCode == 401)
                {
                    LogWarning?.Invoke($"[StageSession] ⚠️ 토큰 만료 감지 (code={req.responseCode}). AuthManager에 알림.");
                    if (AuthManager.Instance != null)
                        AuthManager.Instance.HandleTokenExpired();
                }
                
                callback?.Invoke(result);
                yield break;
            }
        }

        result.Success = true;
        Log?.Invoke($"[StageSession] 문제 수신 성공 (len={result.RawBody?.Length ?? 0})");
        Log?.Invoke($"[StageSession] /api/train/set 응답: stage={stage}, count={count}, stageSessionId={stageSessionId ?? string.Empty}, body={result.RawBody}");
        callback?.Invoke(result);
    }

    public IEnumerator CheckVoice(string stageSessionId, string stage, int problemNumber, string answerValue, byte[] wavData, Action<VoiceCheckResult> callback)
    {
        var result = new VoiceCheckResult();
        string stageParam = stage ?? string.Empty;
        string qs =
            $"stageSessionId={UnityWebRequest.EscapeURL(stageSessionId ?? string.Empty)}" +
            $"&stage={UnityWebRequest.EscapeURL(stageParam)}" +
            $"&problemNumber={UnityWebRequest.EscapeURL(Mathf.Max(1, problemNumber).ToString())}" +
            $"&answer={UnityWebRequest.EscapeURL(answerValue ?? string.Empty)}";
        string url = ComposeUrl($"/api/train/check/voice?{qs}");

        var form = new WWWForm();
        form.AddBinaryData("audio", wavData ?? Array.Empty<byte>(), "voice.wav", "audio/wav");

        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            req.chunkedTransfer = false;
            yield return req.SendWebRequest();

            result.ResponseCode = req.responseCode;
            result.RawBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogWarning?.Invoke($"[StageSession] 음성 업로드 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={result.RawBody}");
                
                // 403 또는 401 에러는 토큰 만료로 간주
                if (req.responseCode == 403 || req.responseCode == 401)
                {
                    LogWarning?.Invoke($"[StageSession] ⚠️ 토큰 만료 감지 (code={req.responseCode}). AuthManager에 알림.");
                    if (AuthManager.Instance != null)
                        AuthManager.Instance.HandleTokenExpired();
                }
                
                callback?.Invoke(result);
                yield break;
            }
        }

        result.Success = true;
        Log?.Invoke($"[StageSession] check/voice success\nBody={result.RawBody}");
        callback?.Invoke(result);
    }

    public IEnumerator CompleteStageSession(string stageSessionId, Action<StageCompleteResult> callback)
    {
        var result = new StageCompleteResult
        {
            StageSessionId = stageSessionId ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(stageSessionId))
        {
            LogWarning?.Invoke("[StageSession] stageSessionId가 비어 있어 complete 요청을 건너뜁니다.");
            callback?.Invoke(result);
            yield break;
        }

        string url = ComposeUrl($"/api/train/stage/complete?stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}");

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            ApplyCommonHeaders(req);
            req.uploadHandler = null;
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            result.ResponseCode = req.responseCode;
            result.RawBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogWarning?.Invoke($"[StageSession] stage/complete 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={result.RawBody}");
                
                // 403 또는 401 에러는 토큰 만료로 간주
                if (req.responseCode == 403 || req.responseCode == 401)
                {
                    LogWarning?.Invoke($"[StageSession] ⚠️ 토큰 만료 감지 (code={req.responseCode}). AuthManager에 알림.");
                    if (AuthManager.Instance != null)
                        AuthManager.Instance.HandleTokenExpired();
                }
                
                callback?.Invoke(result);
                yield break;
            }
        }

        result.Success = true;
        Log?.Invoke($"[StageSession] stage/complete OK\nBody={result.RawBody}");

        if (!string.IsNullOrWhiteSpace(result.RawBody))
        {
            try
            {
                bool parsed = false;
                var resp = JsonUtility.FromJson<CompleteStageResponse>(result.RawBody);
                if (resp != null && resp.data != null)
                {
                    if (!string.IsNullOrWhiteSpace(resp.data.stageSessionId))
                        result.StageSessionId = resp.data.stageSessionId;

                    if (resp.data.voiceResult != null && resp.data.voiceResult.Count > 0)
                    {
                        foreach (var token in resp.data.voiceResult)
                        {
                            if (string.IsNullOrWhiteSpace(token)) continue;
                            result.VoiceResultTokens.Add(token.Trim());
                        }
                        parsed = true;
                    }
                }

                if (!parsed)
                {
                    var respInt = JsonUtility.FromJson<CompleteStageResponseInt>(result.RawBody);
                    if (respInt != null && respInt.data != null && respInt.data.voiceResult != null && respInt.data.voiceResult.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(respInt.data.stageSessionId))
                            result.StageSessionId = respInt.data.stageSessionId;

                        foreach (var idx in respInt.data.voiceResult)
                        {
                            result.VoiceResultIndices.Add(idx);
                            result.VoiceResultTokens.Add(idx.ToString());
                        }
                        parsed = true;
                    }
                }

                if (parsed)
                    Log?.Invoke($"[StageSession] stage/complete voiceResult 수신: {result.VoiceResultTokens.Count}개");
            }
            catch (Exception e)
            {
                LogWarning?.Invoke($"[StageSession] stage/complete voiceResult 파싱 실패: {e.Message}\nRaw={result.RawBody}");
            }
        }

        callback?.Invoke(result);
    }

    public IEnumerator LogAttempt(string stageSessionId, string stage, int problemNumber, int attemptNumber, string selectedAnswer, bool isCorrect, string problemWord, string phonemes, bool includeReplyResult, Action<AttemptLogResult> callback)
    {
        var result = new AttemptLogResult();
        string url = ComposeUrl("/api/train/attempt");
        string ssid = stageSessionId ?? string.Empty;
        string stg = stage ?? string.Empty;
        string ans = selectedAnswer ?? string.Empty;
        string problem = problemWord ?? string.Empty;
        string audioUrl = string.Empty;
        string json = "{" +
                      "\"stageSessionId\":\"" + JsonEscape(ssid) + "\"," +
                      "\"problemNumber\":" + Mathf.Max(1, problemNumber) + "," +
                      "\"stage\":\"" + JsonEscape(stg) + "\"," +
                      "\"problem\":\"" + JsonEscape(problem) + "\"," +
                      "\"audioUrl\":\"" + JsonEscape(audioUrl) + "\"," +
                      "\"isCorrect\":" + (isCorrect ? "true" : "false") + "," +
                      "\"isReplyCorrect\":" + (includeReplyResult ? (isCorrect ? "true" : "false") : "null") + "," +
                      "\"attemptNumber\":" + Mathf.Max(1, attemptNumber) + "," +
                      "\"answer\":\"" + JsonEscape(ans) + "\"" +
                      (string.IsNullOrEmpty(phonemes) ? string.Empty : ",\"phonemes\":\"" + JsonEscape(phonemes) + "\"") +
                      "}";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();

            result.ResponseCode = req.responseCode;
            result.RawBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogWarning?.Invoke($"[StageSession] attempt 로깅 실패: {req.error} (code={req.responseCode})\nURL={url}\nBody={json}\nResp={result.RawBody}");
                
                // 403 또는 401 에러는 토큰 만료로 간주
                if (req.responseCode == 403 || req.responseCode == 401)
                {
                    LogWarning?.Invoke($"[StageSession] ⚠️ 토큰 만료 감지 (code={req.responseCode}). AuthManager에 알림.");
                    if (AuthManager.Instance != null)
                        AuthManager.Instance.HandleTokenExpired();
                }
                
                callback?.Invoke(result);
                yield break;
            }
        }

        result.Success = true;
        Log?.Invoke($"[StageSession] attempt 로깅 OK: problem={problemNumber}, attempt={attemptNumber}, correct={isCorrect}");
        callback?.Invoke(result);
    }

    public void ApplyCommonHeaders(UnityWebRequest req)
    {
        if (req == null) return;
        if (!string.IsNullOrWhiteSpace(AuthToken))
        {
            var tokenTrim = AuthToken.Trim();
            if (tokenTrim.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                tokenTrim = tokenTrim.Substring(7).Trim();
            req.SetRequestHeader("Authorization", $"Bearer {tokenTrim}");
            string preview = tokenTrim.Length > 20
                ? $"{tokenTrim.Substring(0, 10)}...{tokenTrim.Substring(tokenTrim.Length - 10)}"
                : tokenTrim;
            Log?.Invoke($"[StageSession] Auth header attached: Bearer {preview} (len={tokenTrim.Length})");
        }
        else
        {
            LogWarning?.Invoke("[StageSession] ⚠️ AuthToken이 비어있어 Authorization 헤더를 추가하지 않습니다. 403 에러가 발생할 수 있습니다.");
            Log?.Invoke($"[StageSession] 디버깅: BaseUrl={BaseUrl}, AuthToken null={AuthToken == null}, empty={string.IsNullOrEmpty(AuthToken)}");
        }
        req.SetRequestHeader("Accept", "application/json");
    }

    public string ComposeUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            LogWarning?.Invoke($"[StageSession] ⚠️ BaseUrl이 비어있습니다! path만 반환합니다: {path}");
            return path;
        }
        if (string.IsNullOrEmpty(path)) return BaseUrl;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        string baseUrl = BaseUrl;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        if (path.StartsWith("/")) path = path.Substring(1);
        string composed = baseUrl + path;
        Log?.Invoke($"[StageSession] URL 구성: BaseUrl={BaseUrl}, path={path}, 최종={composed}");
        return composed;
    }

    private static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [Serializable]
    private class StartStageBody
    {
        public string stage;
        public int totalProblems;
    }

    [Serializable]
    private class StartStageData
    {
        public string stageSessionId;
    }

    [Serializable]
    private class StartStageResponse
    {
        public bool success;
        public string message;
        public StartStageData data;
    }

    [Serializable]
    private class CompleteStageData
    {
        public string stageSessionId;
        public List<string> voiceResult;
    }

    [Serializable]
    private class CompleteStageResponse
    {
        public bool success;
        public string message;
        public CompleteStageData data;
    }

    [Serializable]
    private class CompleteStageDataInt
    {
        public string stageSessionId;
        public List<int> voiceResult;
    }

    [Serializable]
    private class CompleteStageResponseInt
    {
        public bool success;
        public string message;
        public CompleteStageDataInt data;
    }

    public class StageSessionRequestResult
    {
        public bool Success;
        public long ResponseCode;
        public string RawBody;
    }

    public class StageStartResult : StageSessionRequestResult
    {
        public string StageSessionId;
    }

    public class QuestionSetResult : StageSessionRequestResult
    {
    }

    public class VoiceCheckResult : StageSessionRequestResult
    {
    }

    public class StageCompleteResult : StageSessionRequestResult
    {
        public string StageSessionId;
        public readonly List<string> VoiceResultTokens = new List<string>();
        public readonly List<int> VoiceResultIndices = new List<int>();
    }

    public class AttemptLogResult : StageSessionRequestResult
    {
    }
}

