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
    public string tokenPlayerPrefsKey = "accessToken";
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

    private void OnEnable()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
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
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        try
        {
            if (!previous.IsValid() || !next.IsValid()) return;

            bool fromPersistent = string.Equals(previous.name, "_Persistent", StringComparison.OrdinalIgnoreCase);
            bool toHome = string.Equals(next.name, string.IsNullOrEmpty(homeSceneName) ? "Home" : homeSceneName, StringComparison.OrdinalIgnoreCase);

            if (!toHome) return;

            // 정책: 앱 부팅 직후 최초 1회(_Persistent→Home)만 캐릭터 활성화.
            // 그 외 모든 경우(재방문 포함)는 Home에서 캐릭터 숨김.
            if (!sDidFirstHomeInit && fromPersistent)
            {
                if (!_fetchedOnce)
                {
                    Debug.Log("[HomeStage] 최초 _Persistent→Home 감지: 스테이지 조회 및 캐릭터 활성화");
                    StartCoroutine(CoFetchAndApply());
                }
            }
            else
            {
                HideAllCharacters();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HomeStage] activeSceneChanged 처리 중 오류: {e.Message}");
        }
    }

    private IEnumerator CoFetchAndApply()
    {
        _fetchedOnce = true; // 중복 방지
        sDidFirstHomeInit = true; // 최초 진입 처리 완료

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            Debug.LogWarning("[HomeStage] apiBaseUrl 미설정. 호출 생략.");
            yield break;
        }

        // 토큰 조회 우선순위: 디버그 토큰 → PlayerPrefs → 환경변수(AUTH_TOKEN)
        string token = string.Empty;
        if (!string.IsNullOrWhiteSpace(debugToken))
        {
            token = debugToken.Trim();
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            token = PlayerPrefs.GetString(tokenPlayerPrefsKey, string.Empty);
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            // 개발 편의를 위한 환경변수 백업 경로
            token = Environment.GetEnvironmentVariable("AUTH_TOKEN") ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[HomeStage] 인증 토큰이 없습니다. PlayerPrefs 또는 AUTH_TOKEN 환경변수를 설정하세요.");
            yield break;
        }

        string url = BuildUrl(apiBaseUrl, lastStageEndpoint);

        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                string failBody = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                Debug.LogWarning($"[HomeStage] GET 실패: {req.responseCode} {req.error}\nURL: {url}\nBody: {failBody}");
                yield break;
            }

            string json = req.downloadHandler.text;
            string ctype = req.GetResponseHeader("content-type");
            Debug.Log($"[HomeStage] GET 성공: {req.responseCode}\nURL: {url}\nContent-Type: {ctype}\nResponse: {json}");
            LastStageResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<LastStageResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HomeStage] JSON 파싱 오류: {e.Message}\n{json}");
            }

            string stageStr = resp != null && resp.data != null ? (resp.data.stage ?? string.Empty) : string.Empty;
            // 캐시 저장 (메모리 + PlayerPrefs)
            sLastStageCached = stageStr;
            try
            {
                if (!string.IsNullOrEmpty(stageCacheKey))
                {
                    PlayerPrefs.SetString(stageCacheKey, stageStr ?? string.Empty);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HomeStage] PlayerPrefs 캐시 저장 실패: {e.Message}");
            }
            ApplyStage(stageStr);
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
        // TODO: 음성 선택 로직 연결 (Voice Manager가 있다면 여기에)
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
