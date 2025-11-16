using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 인증 토큰을 관리하는 싱글톤 매니저
/// PlayerPrefs를 사용하여 VR 기기를 껐다 켜도 로그인 상태 유지
/// </summary>
public class AuthManager : MonoBehaviour
{
    private static AuthManager _instance;
    public static AuthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AuthManager>();
            }
            return _instance;
        }
    }

    private const string KEY_ACCESS_TOKEN = "AccessToken";
    private const string KEY_REFRESH_TOKEN = "RefreshToken";
    private const string KEY_USER_ID = "UserId";
    private const string KEY_USER_NAME = "UserName";

    private string _accessToken;
    private string _refreshToken;
    private int _userId;
    private string _userName;
    private string _baseUrl = "";
    private bool _isRefreshing = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 저장된 토큰 로드
        LoadTokens();
    }

    /// <summary>
    /// 로그인 상태 확인
    /// </summary>
    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(_accessToken);
    }

    /// <summary>
    /// Base URL 설정 (토큰 갱신 시 사용)
    /// </summary>
    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl ?? string.Empty;
    }

    /// <summary>
    /// 토큰 만료 시 Refresh Token으로 갱신 시도
    /// 401/403 에러 발생 시 Stage 컨트롤러에서 호출
    /// </summary>
    public void HandleTokenExpired()
    {
        Debug.LogWarning("[AuthManager] Token expired or unauthorized. Attempting to refresh...");
        
        // 이미 갱신 중이면 로그아웃
        if (_isRefreshing)
        {
            Debug.LogWarning("[AuthManager] Already refreshing token. Logging out...");
            LogoutAndReturnHome();
            return;
        }

        // Refresh Token이 없으면 로그아웃
        if (string.IsNullOrWhiteSpace(_refreshToken))
        {
            Debug.LogWarning("[AuthManager] No refresh token available. Logging out...");
            LogoutAndReturnHome();
            return;
        }

        // Refresh Token으로 새 Access Token 발급 시도
        StartCoroutine(RefreshAccessToken());
    }

    /// <summary>
    /// Refresh Token을 사용하여 Access Token 갱신
    /// </summary>
    private IEnumerator RefreshAccessToken()
    {
        _isRefreshing = true;
        Debug.Log("[AuthManager] 🔄 Refresh token으로 새로운 access token 발급 시도...");

        // Base URL이 없으면 EnvConfig에서 가져오기
        string baseUrl = _baseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = EnvConfig.ResolveBaseUrl("");
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogError("[AuthManager] ❌ Base URL이 없어 토큰 갱신을 시도할 수 없습니다.");
            _isRefreshing = false;
            LogoutAndReturnHome();
            yield break;
        }

        string url = $"{baseUrl}/api/user/refresh";
        var payload = new RefreshTokenRequest { refreshToken = _refreshToken };
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
            {
                try
                {
                    var response = JsonUtility.FromJson<RefreshTokenResponse>(req.downloadHandler.text);
                    if (response != null && response.success && response.data != null && !string.IsNullOrWhiteSpace(response.data.accessToken))
                    {
                        string newAccessToken = response.data.accessToken;
                        string newRefreshToken = response.data.refreshToken ?? _refreshToken; // 새 refresh token이 있으면 사용, 없으면 기존 것 유지

                        Debug.Log($"[AuthManager] ✅ 토큰 갱신 성공! (새 access token 길이={newAccessToken.Length})");
                        
                        // 새 토큰 저장
                        UpdateAccessToken(newAccessToken);
                        if (!string.IsNullOrWhiteSpace(newRefreshToken) && newRefreshToken != _refreshToken)
                        {
                            _refreshToken = newRefreshToken;
                            PlayerPrefs.SetString(KEY_REFRESH_TOKEN, newRefreshToken);
                            PlayerPrefs.Save();
                        }

                        _isRefreshing = false;
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AuthManager] 토큰 갱신 응답 파싱 실패: {e.Message}\n응답={req.downloadHandler.text}");
                }
            }

            // 토큰 갱신 실패
            Debug.LogError($"[AuthManager] ❌ 토큰 갱신 실패: code={req.responseCode}, error={req.error}\n응답={req.downloadHandler?.text ?? ""}");
            _isRefreshing = false;
            LogoutAndReturnHome();
        }
    }

    /// <summary>
    /// 로그아웃하고 Home으로 이동
    /// </summary>
    private void LogoutAndReturnHome()
    {
        Logout();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(SceneId.Home);
        }
        else
        {
            Debug.LogError("[AuthManager] SceneLoader.Instance is null!");
        }
    }

    /// <summary>
    /// Access Token 가져오기
    /// </summary>
    public string GetAccessToken()
    {
        // 토큰 검증 디버깅
        if (string.IsNullOrEmpty(_accessToken))
        {
            Debug.LogWarning("[AuthManager] GetAccessToken() called but _accessToken is null or empty!");
        }
        else
        {
            string preview = _accessToken.Length > 40
                ? $"{_accessToken.Substring(0, 20)}...{_accessToken.Substring(_accessToken.Length - 20)}"
                : _accessToken;
            Debug.Log($"[AuthManager] GetAccessToken() → len={_accessToken.Length}, preview={preview}");
        }

        return _accessToken;
    }

    /// <summary>
    /// Refresh Token 가져오기
    /// </summary>
    public string GetRefreshToken()
    {
        return _refreshToken;
    }

    /// <summary>
    /// 사용자 ID 가져오기
    /// </summary>
    public int GetUserId()
    {
        return _userId;
    }

    /// <summary>
    /// 사용자 이름 가져오기
    /// </summary>
    public string GetUserName()
    {
        return _userName;
    }

    /// <summary>
    /// 로그인 성공 시 토큰 저장
    /// </summary>
    public void SaveTokens(string accessToken, string refreshToken, int userId = 0, string userName = "")
    {
        // 토큰 검증
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("[AuthManager] ⚠️ Attempting to save null or empty accessToken!");
        }
        else
        {
            string preview = accessToken.Length > 40
                ? $"{accessToken.Substring(0, 20)}...{accessToken.Substring(accessToken.Length - 20)}"
                : accessToken;
            Debug.Log($"[AuthManager] Saving accessToken: len={accessToken.Length}, preview={preview}");
        }

        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _userId = userId;
        _userName = userName;

        PlayerPrefs.SetString(KEY_ACCESS_TOKEN, accessToken);
        PlayerPrefs.SetString(KEY_REFRESH_TOKEN, refreshToken);
        PlayerPrefs.SetInt(KEY_USER_ID, userId);
        PlayerPrefs.SetString(KEY_USER_NAME, userName);
        PlayerPrefs.Save();

        Debug.Log($"[AuthManager] Tokens saved to PlayerPrefs. User: {userName} (ID: {userId})");

        // PlayerPrefs에 제대로 저장되었는지 즉시 확인
        string savedToken = PlayerPrefs.GetString(KEY_ACCESS_TOKEN, "");
        if (savedToken != accessToken)
        {
            Debug.LogError($"[AuthManager] ⚠️ PlayerPrefs verification failed! Saved token doesn't match!");
        }
        else
        {
            Debug.Log($"[AuthManager] ✓ PlayerPrefs verification passed (len={savedToken.Length})");
        }
    }

    /// <summary>
    /// PlayerPrefs에서 토큰 로드
    /// </summary>
    private void LoadTokens()
    {
        _accessToken = PlayerPrefs.GetString(KEY_ACCESS_TOKEN, "");
        _refreshToken = PlayerPrefs.GetString(KEY_REFRESH_TOKEN, "");
        _userId = PlayerPrefs.GetInt(KEY_USER_ID, 0);
        _userName = PlayerPrefs.GetString(KEY_USER_NAME, "");

        if (IsLoggedIn())
        {
            Debug.Log($"[AuthManager] Tokens loaded. User: {_userName} (ID: {_userId})");
        }
        else
        {
            Debug.Log("[AuthManager] No saved tokens found.");
        }
    }

    /// <summary>
    /// 로그아웃 (토큰 삭제)
    /// </summary>
    public void Logout()
    {
        _accessToken = "";
        _refreshToken = "";
        _userId = 0;
        _userName = "";

        PlayerPrefs.DeleteKey(KEY_ACCESS_TOKEN);
        PlayerPrefs.DeleteKey(KEY_REFRESH_TOKEN);
        PlayerPrefs.DeleteKey(KEY_USER_ID);
        PlayerPrefs.DeleteKey(KEY_USER_NAME);
        PlayerPrefs.Save();

        Debug.Log("[AuthManager] User logged out. Tokens cleared.");
    }

    /// <summary>
    /// Access Token만 업데이트 (Refresh 시)
    /// </summary>
    public void UpdateAccessToken(string newAccessToken)
    {
        _accessToken = newAccessToken;
        PlayerPrefs.SetString(KEY_ACCESS_TOKEN, newAccessToken);
        PlayerPrefs.Save();

        Debug.Log("[AuthManager] Access token updated.");
    }

    [Serializable]
    private class RefreshTokenRequest
    {
        public string refreshToken;
    }

    [Serializable]
    private class RefreshTokenResponse
    {
        public bool success;
        public string message;
        public RefreshTokenData data;
    }

    [Serializable]
    private class RefreshTokenData
    {
        public string accessToken;
        public string refreshToken;
    }
}
