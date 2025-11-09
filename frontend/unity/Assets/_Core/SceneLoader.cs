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