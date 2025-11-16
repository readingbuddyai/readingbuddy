using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Utils;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

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
    }

    // 씬 전환 함수
    public void LoadScene(string sceneName)
    {
        GlobalSfxManager.Instance?.PlaySceneTransitionSfx();
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
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
    }
}