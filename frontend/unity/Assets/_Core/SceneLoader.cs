using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Utils;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private GameObject kaleidoscopeEffect;
    [SerializeField] private float fadeDuration = 0.35f;

    private void Awake()
    {
        // 싱글톤 (Persistent 씬 유지)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;

        if (kaleidoscopeEffect != null)
            kaleidoscopeEffect.SetActive(false);
    }

    // 씬 전환 함수
    public void LoadScene(string sceneName)
    {
        Debug.Log($"[SceneLoader] LoadScene requested → {sceneName}");
        GlobalSfxManager.Instance?.PlaySceneTransitionSfx();
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        Debug.Log($"[SceneLoader] Begin transition → {sceneName}");
        yield return PlayTransitionIn();

        // 이미 같은 씬이 로드되어 있는지 확인
        Scene existingScene = SceneManager.GetSceneByName(sceneName);
        if (existingScene.IsValid() && existingScene.isLoaded)
        {
            Debug.Log($"[SceneLoader] 씬 '{sceneName}'이 이미 로드되어 있습니다. 중복 로드를 건너뜁니다.");
            
            // 기존 씬 중 Persistent와 현재 씬 제외하고 언로드
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != "_Persistent" && scene.name != sceneName)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }

            // 활성 씬 변경
            SceneManager.SetActiveScene(existingScene);
            Debug.Log($"[SceneLoader] 재사용 씬 활성화 완료 → {sceneName}");
            yield return PlayTransitionOut();
            Debug.Log($"[SceneLoader] Transition out 완료 (기존 씬) → {sceneName}");
            yield break;
        }

        // 새 씬 Additive 로드
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // 기존 씬 중 Persistent 제외하고 언로드
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != "_Persistent" && scene.name != sceneName)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        // 활성 씬 변경
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);
        Debug.Log($"[SceneLoader] 새 씬 로드/활성화 완료 → {sceneName}");

        yield return PlayTransitionOut();
        Debug.Log($"[SceneLoader] Transition out 완료 (새 씬) → {sceneName}");
    }

    private IEnumerator PlayTransitionIn()
    {
        if (kaleidoscopeEffect != null)
            kaleidoscopeEffect.SetActive(true);

        Debug.Log("[SceneLoader] PlayTransitionIn start");
        yield return CoFade(1f);
        Debug.Log("[SceneLoader] PlayTransitionIn done");
    }

    private IEnumerator PlayTransitionOut()
    {
        yield return CoFade(0f);

        Debug.Log("[SceneLoader] PlayTransitionOut done");
        if (kaleidoscopeEffect != null)
            kaleidoscopeEffect.SetActive(false);
    }

    private IEnumerator CoFade(float target)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float start = fadeCanvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = target;
    }
}
