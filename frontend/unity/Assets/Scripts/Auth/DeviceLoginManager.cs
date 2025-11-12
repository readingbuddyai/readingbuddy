using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Device Code 방식의 VR 로그인을 담당하는 매니저
/// VR_DEVICE_LOGIN_GUIDE.md 참고
/// </summary>
public class DeviceLoginManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string baseUrl = "";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI deviceCodeText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject loadingPanel;

    [Header("Settings")]
    [SerializeField] private float pollingInterval = 3f;
    [SerializeField] private float codeExpiryTime = 60f;
    [SerializeField] private TMP_FontAsset koreanFont; // 한글 폰트 (옵션)

    private string deviceAuthCode;
    private Coroutine pollingCoroutine;
    private bool isPolling = false;

    private void Awake()
    {
        // UI 참조가 없으면 자동 생성
        if (deviceCodeText == null || statusText == null)
        {
            CreateUI();
        }
    }

    private void Start()
    {
        // EnvConfig로 baseUrl 설정
        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);

        // baseUrl 로그로 확인
        Debug.Log($"[DeviceLogin] Resolved baseUrl: '{baseUrl}'");

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
            retryButton.gameObject.SetActive(false);
        }

        // 로그인 상태에 따라 UI 표시/숨김
        CheckLoginStatus();
    }

    /// <summary>
    /// UI 자동 생성 (참조가 없을 경우)
    /// </summary>
    private void CreateUI()
    {
        Debug.Log("[DeviceLogin] Creating UI automatically...");

        // RectTransform 추가 (Canvas 하위 UI로 동작)
        RectTransform panelRect = gameObject.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            panelRect = gameObject.AddComponent<RectTransform>();
        }

        // 전체 화면 크기로 설정
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // 배경 패널 (반투명 검정)
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(transform, false);

        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.9f); // 반투명 검정

        // 콘텐츠 컨테이너
        GameObject container = new GameObject("Container");
        container.transform.SetParent(transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(800, 600);
        containerRect.anchoredPosition = Vector2.zero;

        // 타이틀 텍스트
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(container.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(700, 100);
        titleRect.anchoredPosition = new Vector2(0, -80);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "VR 로그인";
        titleText.fontSize = 60;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        if (koreanFont != null) titleText.font = koreanFont;

        // Device Code 텍스트
        GameObject codeObj = new GameObject("DeviceCode");
        codeObj.transform.SetParent(container.transform, false);

        RectTransform codeRect = codeObj.AddComponent<RectTransform>();
        codeRect.anchorMin = new Vector2(0.5f, 0.5f);
        codeRect.anchorMax = new Vector2(0.5f, 0.5f);
        codeRect.sizeDelta = new Vector2(700, 120);
        codeRect.anchoredPosition = new Vector2(0, 50);

        deviceCodeText = codeObj.AddComponent<TextMeshProUGUI>();
        deviceCodeText.text = "-- -- -- --";
        deviceCodeText.fontSize = 80;
        deviceCodeText.fontStyle = FontStyles.Bold;
        deviceCodeText.alignment = TextAlignmentOptions.Center;
        deviceCodeText.color = Color.yellow;
        if (koreanFont != null) deviceCodeText.font = koreanFont;

        // 상태 텍스트
        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(container.transform, false);

        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(700, 150);
        statusRect.anchoredPosition = new Vector2(0, -100);

        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "코드 생성 중...";
        statusText.fontSize = 40;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        if (koreanFont != null) statusText.font = koreanFont;

        // 재시도 버튼
        GameObject buttonObj = new GameObject("RetryButton");
        buttonObj.transform.SetParent(container.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(300, 80);
        buttonRect.anchoredPosition = new Vector2(0, 60);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        retryButton = buttonObj.AddComponent<Button>();
        retryButton.targetGraphic = buttonImage;

        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "다시 시도";
        buttonText.fontSize = 32;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        if (koreanFont != null) buttonText.font = koreanFont;

        Debug.Log("[DeviceLogin] UI created successfully!");
    }

    /// <summary>
    /// 로그인 상태 확인 및 UI 표시
    /// </summary>
    private void CheckLoginStatus()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
        {
            Debug.Log("[DeviceLogin] Already logged in. Hiding login UI.");
            HideLoginUI();
        }
        else
        {
            Debug.Log("[DeviceLogin] Not logged in. Showing login UI.");
            ShowLoginUI();
            StartDeviceLogin();
        }
    }

    /// <summary>
    /// 로그인 UI 표시
    /// </summary>
    private void ShowLoginUI()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 로그인 UI 숨김
    /// </summary>
    private void HideLoginUI()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Device Login 프로세스 시작
    /// </summary>
    public void StartDeviceLogin()
    {
        UpdateStatus("코드 생성 중...", Color.yellow);
        ShowLoading(true);

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
        }

        StartCoroutine(RequestDeviceCode());
    }

    /// <summary>
    /// Step 1: Device Code 생성 요청
    /// GET /api/user/activation
    /// </summary>
    private IEnumerator RequestDeviceCode()
    {
        string url = $"{baseUrl}/api/user/activation";
        Debug.Log($"[DeviceLogin] Requesting device code from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[DeviceLogin] Response received: {responseText}");

                try
                {
                    DeviceCodeResponse response = JsonUtility.FromJson<DeviceCodeResponse>(responseText);

                    if (response.success && response.data != null)
                    {
                        deviceAuthCode = response.data.authCode;
                        Debug.Log($"[DeviceLogin] Device Code received: '{deviceAuthCode}' (length={deviceAuthCode?.Length ?? 0})");

                        if (string.IsNullOrEmpty(deviceAuthCode))
                        {
                            OnError("서버에서 빈 Device Code를 반환했습니다.");
                            yield break;
                        }

                        DisplayDeviceCode(deviceAuthCode);
                        StartPolling();
                    }
                    else
                    {
                        OnError($"코드 생성 실패: {response.message}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DeviceLogin] Response text: {responseText}");
                    OnError($"응답 파싱 오류: {e.Message}");
                }
            }
            else
            {
                OnError($"네트워크 오류: {request.error}");
            }
        }

        ShowLoading(false);
    }

    /// <summary>
    /// VR 화면에 Device Code 표시
    /// </summary>
    private void DisplayDeviceCode(string code)
    {
        if (deviceCodeText != null)
        {
            // 코드 길이 확인
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[DeviceLogin] Device code is null or empty!");
                deviceCodeText.text = "------";
                return;
            }

            // 10자리 코드인 경우 5자씩 끊어서 표시 (가독성 향상)
            if (code.Length >= 10)
            {
                string formattedCode = code.Substring(0, 5) + " " + code.Substring(5);
                deviceCodeText.text = formattedCode;
            }
            else if (code.Length >= 5)
            {
                // 5~9자리인 경우 반으로 나눔
                int half = code.Length / 2;
                string formattedCode = code.Substring(0, half) + " " + code.Substring(half);
                deviceCodeText.text = formattedCode;
            }
            else
            {
                // 5자 미만이면 그냥 표시
                deviceCodeText.text = code;
            }

            Debug.Log($"[DeviceLogin] Displaying code: length={code.Length}, code={code}");
        }

        UpdateStatus("모바일 앱을 열고\n위 코드를 입력해주세요", Color.white);
    }

    /// <summary>
    /// Step 2: Polling 시작
    /// POST /api/user/polling (3초마다 반복)
    /// </summary>
    private void StartPolling()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
        }

        isPolling = true;
        pollingCoroutine = StartCoroutine(PollForAuthorization());
    }

    /// <summary>
    /// 인증 확인 Polling
    /// </summary>
    private IEnumerator PollForAuthorization()
    {
        float elapsedTime = 0f;

        while (elapsedTime < codeExpiryTime && isPolling)
        {
            yield return new WaitForSeconds(pollingInterval);
            elapsedTime += pollingInterval;

            string url = $"{baseUrl}/api/user/polling";
            PollingRequest requestBody = new PollingRequest { deviceAuthCode = deviceAuthCode };
            string json = JsonUtility.ToJson(requestBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        TokenResponse response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);

                        if (response.success && response.data != null)
                        {
                            // 인증 성공!
                            OnAuthenticationSuccess(response.data.accessToken, response.data.refreshToken);
                            yield break;
                        }
                        else
                        {
                            // 아직 인증 안 됨 (계속 대기)
                            Debug.Log($"[DeviceLogin] Waiting for authentication... ({elapsedTime:F0}s)");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[DeviceLogin] Polling response parse error: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DeviceLogin] Polling request failed: {request.error}");
                }
            }
        }

        // 시간 초과
        if (isPolling)
        {
            OnAuthenticationTimeout();
        }
    }

    public static event System.Action OnAccessTokenReady; // 액세스 토큰이 준비되었음을 알리는 이벤트
    /// <summary>
    /// 인증 성공 처리
    /// </summary>
    private void OnAuthenticationSuccess(string accessToken, string refreshToken)
    {
        isPolling = false;

        Debug.Log("[DeviceLogin] Authentication successful!");

        // 토큰 검증
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("[DeviceLogin] ⚠️ accessToken is null or empty!");
        }
        else
        {
            string preview = accessToken.Length > 40
                ? $"{accessToken.Substring(0, 20)}...{accessToken.Substring(accessToken.Length - 20)}"
                : accessToken;
            Debug.Log($"[DeviceLogin] Access Token received: len={accessToken.Length}, preview={preview}");
            Debug.Log($"[DeviceLogin] Token starts with: {accessToken.Substring(0, System.Math.Min(10, accessToken.Length))}");
        }

        UpdateStatus("로그인 성공!", Color.green);

        // AuthManager에 토큰 저장
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.SaveTokens(accessToken, refreshToken);
        }
        else
        {
            Debug.LogError("[DeviceLogin] AuthManager.Instance is null!");
        }
        // 2) ★ 이벤트 발행 (홈 초기화가 이 신호를 기다린다)
        OnAccessTokenReady?.Invoke();
        // 1초 후 로그인 UI 숨기기
        StartCoroutine(HideLoginUIAfterDelay(1f));
    }

    /// <summary>
    /// 인증 타임아웃 처리
    /// </summary>
    private void OnAuthenticationTimeout()
    {
        isPolling = false;

        Debug.LogWarning("[DeviceLogin] Authentication timeout.");
        UpdateStatus("인증 시간이 만료되었습니다.\n다시 시도해주세요.", Color.red);

        if (deviceCodeText != null)
        {
            deviceCodeText.text = "------";
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 에러 처리
    /// </summary>
    private void OnError(string errorMessage)
    {
        isPolling = false;

        Debug.LogError($"[DeviceLogin] Error: {errorMessage}");
        UpdateStatus($"오류가 발생했습니다:\n{errorMessage}", Color.red);

        if (deviceCodeText != null)
        {
            deviceCodeText.text = "------";
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 재시도 버튼 클릭
    /// </summary>
    private void OnRetryButtonClicked()
    {
        StartDeviceLogin();
    }

    /// <summary>
    /// 로그인 UI 숨기기 (딜레이 후)
    /// </summary>
    private IEnumerator HideLoginUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoginUI();
        Debug.Log("[DeviceLogin] Login UI hidden. User can now use Home scene.");
    }

    /// <summary>
    /// 상태 메시지 업데이트
    /// </summary>
    private void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }

        Debug.Log($"[DeviceLogin] {message}");
    }

    /// <summary>
    /// 로딩 패널 표시/숨김
    /// </summary>
    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
    }

    private void OnDestroy()
    {
        isPolling = false;

        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
        }
    }

    #region API Response Classes

    [System.Serializable]
    private class DeviceCodeResponse
    {
        public bool success;
        public string message;
        public DeviceCodeData data;
    }

    [System.Serializable]
    private class DeviceCodeData
    {
        public string authCode;
    }

    [System.Serializable]
    private class PollingRequest
    {
        public string deviceAuthCode;
    }

    [System.Serializable]
    private class TokenResponse
    {
        public bool success;
        public string message;
        public TokenData data;
    }

    [System.Serializable]
    private class TokenData
    {
        public string accessToken;
        public string refreshToken;
    }

    #endregion
}
