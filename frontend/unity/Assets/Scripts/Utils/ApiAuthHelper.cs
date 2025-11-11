using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// API 호출 시 인증 토큰을 추가하는 헬퍼 유틸리티
/// </summary>
public static class ApiAuthHelper
{
    /// <summary>
    /// UnityWebRequest에 Authorization 헤더 추가
    /// </summary>
    /// <param name="request">UnityWebRequest 객체</param>
    /// <param name="requireAuth">인증 토큰 필수 여부 (기본: true)</param>
    /// <returns>토큰이 추가된 UnityWebRequest 객체</returns>
    public static UnityWebRequest AddAuthHeader(this UnityWebRequest request, bool requireAuth = true)
    {
        if (AuthManager.Instance == null)
        {
            if (requireAuth)
            {
                Debug.LogError("[ApiAuthHelper] AuthManager.Instance is null!");
            }
            return request;
        }

        string accessToken = AuthManager.Instance.GetAccessToken();

        if (string.IsNullOrEmpty(accessToken))
        {
            if (requireAuth)
            {
                Debug.LogWarning("[ApiAuthHelper] Access token is empty. User may not be logged in.");
            }
            return request;
        }

        request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        return request;
    }

    /// <summary>
    /// API 응답이 401/403 Unauthorized인지 확인하고, 그렇다면 로그아웃 처리
    /// </summary>
    /// <param name="request">UnityWebRequest 객체</param>
    /// <returns>401/403 에러 여부</returns>
    public static bool HandleUnauthorized(this UnityWebRequest request)
    {
        if (request.responseCode == 401 || request.responseCode == 403)
        {
            Debug.LogWarning($"[ApiAuthHelper] {request.responseCode} Unauthorized. Token may be expired.");

            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.HandleTokenExpired();
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// GET 요청 생성 (인증 토큰 자동 추가)
    /// </summary>
    /// <param name="url">Full URL (baseUrl + endpoint)</param>
    /// <param name="requireAuth">인증 토큰 필수 여부</param>
    public static UnityWebRequest CreateAuthenticatedGet(string url, bool requireAuth = true)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.AddAuthHeader(requireAuth);
        return request;
    }

    /// <summary>
    /// POST 요청 생성 (인증 토큰 자동 추가)
    /// </summary>
    /// <param name="url">Full URL (baseUrl + endpoint)</param>
    /// <param name="jsonBody">JSON 바디</param>
    /// <param name="requireAuth">인증 토큰 필수 여부</param>
    public static UnityWebRequest CreateAuthenticatedPost(string url, string jsonBody, bool requireAuth = true)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.AddAuthHeader(requireAuth);
        return request;
    }

    /// <summary>
    /// PUT 요청 생성 (인증 토큰 자동 추가)
    /// </summary>
    /// <param name="url">Full URL (baseUrl + endpoint)</param>
    /// <param name="jsonBody">JSON 바디</param>
    /// <param name="requireAuth">인증 토큰 필수 여부</param>
    public static UnityWebRequest CreateAuthenticatedPut(string url, string jsonBody, bool requireAuth = true)
    {
        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.AddAuthHeader(requireAuth);
        return request;
    }

    /// <summary>
    /// DELETE 요청 생성 (인증 토큰 자동 추가)
    /// </summary>
    /// <param name="url">Full URL (baseUrl + endpoint)</param>
    /// <param name="requireAuth">인증 토큰 필수 여부</param>
    public static UnityWebRequest CreateAuthenticatedDelete(string url, bool requireAuth = true)
    {
        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.AddAuthHeader(requireAuth);
        return request;
    }
}
