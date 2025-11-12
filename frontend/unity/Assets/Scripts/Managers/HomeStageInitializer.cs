using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class HomeStageInitializer : MonoBehaviour
{
    [Header("Home 진입 감지")]
    [Tooltip("Home 씬 이름")]
    public string homeSceneName = "Home";

    [Header("API 설정")]
    [Tooltip("API Base URL. 예) https://api.example.com")]
    public string apiBaseUrl = "https://readingbuddyai.co.kr";
    [Tooltip("마지막 스테이지 조회 엔드포인트 경로")]
    public string lastStageEndpoint = "/api/train/last/stage";
    [Header("인증")]
    [Tooltip("PlayerPrefs 에 저장된 토큰 키(로그인 시 자동 저장 예정)")]
    public string tokenPlayerPrefsKey = "AccessToken";
    [Tooltip("개발/테스트용 수동 토큰 (우선 사용)")]
    public string debugToken = "";

    [Header("캐시 설정")]
    [Tooltip("마지막 스테이지를 PlayerPrefs에 저장할 키")]
    public string stageCacheKey = "lastStage";

    [Header("Home 캐릭터 참조")]
    [Tooltip("Home 씬의 mage 오브젝트 (비우면 이름으로 자동 탐색)")]
    public GameObject mageRef;
    [Tooltip("Home 씬의 stage2char 오브젝트 (비우면 이름으로 자동 탐색)")]
    public GameObject stage2Ref;
    [Tooltip("Home 씬의 stage4char 오브젝트 (비우면 이름으로 자동 탐색)")]
    public GameObject stage4Ref;

    private bool _fetchedOnce = false;
    private static bool sDidFirstHomeInit = false; // 앱 시작 후 최초 _Persistent→Home 처리 여부
    private static string sLastStageCached = string.Empty; // 씬 간 사용 가능한 캐시

    public static string LastStage => sLastStageCached;
    
    // ★ 마지막 적용 상태를 보관 (늦게 켜지는 구독자 보완)
    private static bool sStageAppliedOnce = false;
    private static string sLastAppliedStage = "";
    private static HomeProfile sLastAppliedProfile = HomeProfile.Mage;

    // 다른 컴포넌트가 즉시 읽을 수 있게 헬퍼 제공
    public static bool TryGetLastApplied(out string stage, out HomeProfile profile)
    {
        if (sStageAppliedOnce)
        {
            stage = sLastAppliedStage;
            profile = sLastAppliedProfile;
            return true;
        }
        stage = null;
        profile = default;
        return false;
    }

    // 누가 듣게 하려고 선언 (오디오/컷신이 씀)
    public static event System.Action<string, HomeProfile> OnStageApplied;

    private void OnEnable()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // ★ DeviceLoginManager에서 토큰이 준비됐다는 신호를 받기 위해 구독
        DeviceLoginManager.OnAccessTokenReady += HandleTokenReady;
    
        if (string.IsNullOrEmpty(sLastStageCached))
        {
            // PlayerPrefs 캐시 복구 시도
            var cached = PlayerPrefs.GetString(stageCacheKey, string.Empty);
            if (!string.IsNullOrEmpty(cached))
                sLastStageCached = cached;
        }
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        DeviceLoginManager.OnAccessTokenReady -= HandleTokenReady; // ★해제
    }

        private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        try
        {
            if (!previous.IsValid() || !next.IsValid()) return;

            var homeName = string.IsNullOrEmpty(homeSceneName) ? "Home" : homeSceneName;
            if (!string.Equals(next.name, homeName, System.StringComparison.OrdinalIgnoreCase)) return;

            HideAllCharacters(); // 먼저 싹 숨김

            // ★ 이미 로그인 상태면 즉시 /last/stage 1회
            if (!_fetchedOnce && AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn())
            {
                Debug.Log("[HomeStage] 로그인 상태 감지: 즉시 /last/stage 조회");
                StartCoroutine(CoFetchAndApply());
            }
            else
            {
                Debug.Log("[HomeStage] 아직 토큰 없음: 로그인 완료 이벤트 대기");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HomeStage] activeSceneChanged 처리 중 오류: {e.Message}");
        }
    }

    private void HandleTokenReady()
    {
        if (_fetchedOnce) return;
        StartCoroutine(CoWaitHomeAndFetchOnce());
    }

    private IEnumerator CoWaitHomeAndFetchOnce()
    {
        var homeName = string.IsNullOrEmpty(homeSceneName) ? "Home" : homeSceneName;

        // 1) 활성 씬이 Home이 될 때까지 대기 (이벤트가 Home 진입 직후 올 수도 있으니)
        while (SceneManager.GetActiveScene().name != homeName)
            yield return null;

        // 2) 한 프레임 더 대기 → 씬 오브젝트들이 OnEnable/Start를 끝낼 시간 확보
        yield return null;

        // 3) 캐릭터 참조 확보 후
        EnsureCharacterRefs();
        HideAllCharacters();

        // 4) 이제 1회 조회
        StartCoroutine(CoFetchAndApply());
    }

    private IEnumerator CoFetchAndApply()
    {
        _fetchedOnce = true;

        // 1) 토큰 확보: 홈에서 방금 발급됐을 수 있으니 AuthManager 우선
        string token = (AuthManager.Instance != null) ? AuthManager.Instance.GetAccessToken() : "";
        if (string.IsNullOrWhiteSpace(token))
            token = PlayerPrefs.GetString(tokenPlayerPrefsKey, "");

        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[HomeStage] 토큰 없음 → /last/stage 스킵");
            yield break;
        }

        // 2) 요청
        string url = BuildUrl(apiBaseUrl, lastStageEndpoint);
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[HomeStage] GET 실패: {req.responseCode} {req.error}");
                yield break;
            }

            // 3) 파싱 & 캐시
            var json = req.downloadHandler.text;
            LastStageResponse resp = null;
            try { resp = JsonUtility.FromJson<LastStageResponse>(json); } catch {}
            string stageStr = (resp != null && resp.data != null) ? (resp.data.stage ?? "") : "";

            // ⚠️ 읽기전용 프로퍼티에 대입 금지: LastStage = ... ❌
            sLastStageCached = stageStr; // ✅ 백킹필드에 기록
            PlayerPrefs.SetString(stageCacheKey, stageStr);
            PlayerPrefs.Save();

            // 4) 캐릭터 토글
            var profile = ResolveProfile(stageStr);
            ToggleCharacters(profile);

            // ★ Sticky 기록 (늦게 구독해도 즉시 반영 가능)
            sStageAppliedOnce = true;
            sLastAppliedStage = stageStr;
            sLastAppliedProfile = profile;  

            // 5) 적용 완료 브로드캐스트 (오디오/컷신이 이걸 구독)
            OnStageApplied?.Invoke(stageStr, profile);

            Debug.Log($"[HomeStage] stage='{stageStr}' 적용 완료 → profile={profile}");
        }
    }

    private static string BuildUrl(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(baseUrl)) return path ?? string.Empty;
        if (string.IsNullOrEmpty(path)) return baseUrl;
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');
        return path.StartsWith("/") ? baseUrl + path : baseUrl + "/" + path;
    }

    public enum HomeProfile
    {
        Mage,
        Stage2Char,
        Stage4Char
    }

    private void ApplyStage(string stage)
    {
        var profile = ResolveProfile(stage);
        ToggleCharacters(profile);
        Debug.Log($"[HomeStage] stage '{stage}' → {profile}");
    }

    public static HomeProfile ResolveProfile(string stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return HomeProfile.Mage;

        if (string.Equals(stage, "마지막으로 플레이한 스테이지가 없습니다", StringComparison.Ordinal))
            return HomeProfile.Mage;

        // "1.1.1" 형태의 경우 첫 글자 기준으로 분기
        char first = stage[0];
        if (first == '1') return HomeProfile.Mage;
        if (first == '2' || first == '3') return HomeProfile.Stage2Char;
        if (first == '4') return HomeProfile.Stage4Char;
        return HomeProfile.Mage;
    }

    private void ToggleCharacters(HomeProfile profile)
    {
        EnsureCharacterRefs();
        var mage = mageRef;
        var stage2 = stage2Ref;
        var stage4 = stage4Ref;

        void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        switch (profile)
        {
            case HomeProfile.Mage:
                SetActiveSafe(mage, true);
                SetActiveSafe(stage2, false);
                SetActiveSafe(stage4, false);
                break;
            case HomeProfile.Stage2Char:
                SetActiveSafe(mage, false);
                SetActiveSafe(stage2, true);
                SetActiveSafe(stage4, false);
                break;
            case HomeProfile.Stage4Char:
                SetActiveSafe(mage, false);
                SetActiveSafe(stage2, false);
                SetActiveSafe(stage4, true);
                break;
        }
    }

    private void HideAllCharacters()
    {
        EnsureCharacterRefs();
        var mage = mageRef;
        var stage2 = stage2Ref;
        var stage4 = stage4Ref;

        if (mage != null && mage.activeSelf) mage.SetActive(false);
        if (stage2 != null && stage2.activeSelf) stage2.SetActive(false);
        if (stage4 != null && stage4.activeSelf) stage4.SetActive(false);
        Debug.Log("[HomeStage] Home으로 진입했지만 _Persistent에서 온 것이 아니므로 캐릭터를 숨깁니다.");
    }

    private void EnsureCharacterRefs()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid()) return;

        if (mageRef != null && stage2Ref != null && stage4Ref != null) return;

        // 비활성 오브젝트까지 포함해 탐색 (씬 내 객체만)
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            var go = t.gameObject;
            if (!go.scene.IsValid() || go.scene != activeScene) continue;
            string n = go.name;
            if (mageRef == null && string.Equals(n, "mage", StringComparison.Ordinal)) mageRef = go;
            else if (stage2Ref == null && string.Equals(n, "stage2char", StringComparison.Ordinal)) stage2Ref = go;
            else if (stage4Ref == null && string.Equals(n, "stage4char", StringComparison.Ordinal)) stage4Ref = go;
        }
    }

    [Serializable]
    private class LastStageResponse
    {
        public bool success;
        public string message;
        public LastStageData data;
    }

    [Serializable]
    private class LastStageData
    {
        public string stage;
        public string playedAt;
    }
}
