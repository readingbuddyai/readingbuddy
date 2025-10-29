using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class SceneRouter
{
    public static string CurrentContent;

    public static IEnumerator LoadContent(string sceneName)
    {
        // 이전 콘텐츠 씬을 언로드
        if (!string.IsNullOrEmpty(CurrentContent) &&
            SceneManager.GetSceneByName(CurrentContent).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(CurrentContent);
        }

        // 새 씬을 Additive로 로드
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        // 활성 씬 전환
        var loaded = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loaded);
        CurrentContent = sceneName;
    }
}
