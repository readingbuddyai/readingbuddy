using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class SceneRouter
{
    // ✅ 현재 로드된 콘텐츠 씬 이름 저장용
    public static string CurrentContent { get; set; }

    // ✅ _Persistent 씬 이름 (필요 시 프로젝트에 맞게 수정)
    private const string PersistentSceneName = "_Persistent";

    public static IEnumerator LoadContent(string sceneName)
    {
        // 1️⃣ Additive 로드
        var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone)
            yield return null;

        // 2️⃣ ActiveScene 전환 (Lighting / Skybox 적용 핵심)
        var loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);

        // 3️⃣ 현재 콘텐츠 이름 갱신
        CurrentContent = sceneName;

        // 4️⃣ 환경광 즉시 갱신 (스카이박스 반영)
        DynamicGI.UpdateEnvironment();

        // 5️⃣ 이전 콘텐츠 씬 언로드 (_Persistent 제외)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != PersistentSceneName && s.name != sceneName)
                SceneManager.UnloadSceneAsync(s);
        }

        Debug.Log($"[SceneRouter] ActiveScene → {sceneName}");
    }
}
