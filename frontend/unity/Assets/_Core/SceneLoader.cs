using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Utils;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private GameObject kaleidoscopeEffect;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float transitionHold = 0.5f;

    private ParticleSystem[] kaleidoscopeParticles;

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
        {
            kaleidoscopeEffect.SetActive(false);
            kaleidoscopeParticles = kaleidoscopeEffect.GetComponentsInChildren<ParticleSystem>(true);
        }
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

        // 로딩 직후 잠깐 기다려서 이펙트가 더 잘 보이도록 유지
        yield return new WaitForSecondsRealtime(transitionHold);

        // fade out 전에 이펙트를 끄면 fade in 구간에서만 보임
        SetKaleidoscopeActive(false);

        yield return PlayTransitionOut();
        Debug.Log($"[SceneLoader] Transition out 완료 (새 씬) → {sceneName}");
    }

    private IEnumerator PlayTransitionIn()
    {
        SetKaleidoscopeActive(true);

        Debug.Log("[SceneLoader] PlayTransitionIn start");
        yield return CoFade(1f);
        Debug.Log("[SceneLoader] PlayTransitionIn done");
    }

    private IEnumerator PlayTransitionOut()
    {
        yield return CoFade(0f);

        Debug.Log("[SceneLoader] PlayTransitionOut done");
        SetKaleidoscopeActive(false);
    }

    private IEnumerator CoFade(float target)
    {
        if (fadeCanvasGroup == null)
            yield break;

        Debug.Log($"[SceneLoader] CoFade start ({fadeCanvasGroup.alpha} -> {target})");
        float start = fadeCanvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = target;
        Debug.Log($"[SceneLoader] CoFade end ({fadeCanvasGroup.alpha})");
    }

    private void SetKaleidoscopeActive(bool active)
    {
        if (kaleidoscopeEffect == null)
            return;

        kaleidoscopeEffect.SetActive(active);
        if (kaleidoscopeParticles == null || kaleidoscopeParticles.Length == 0)
            return;

        if (active)
        {
            foreach (var ps in kaleidoscopeParticles)
            {
                ps.Clear();
                ps.Play(true);
            }
        }
        else
        {
            foreach (var ps in kaleidoscopeParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
