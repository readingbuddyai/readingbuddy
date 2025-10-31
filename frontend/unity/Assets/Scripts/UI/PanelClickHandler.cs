using UnityEngine;
using System.Collections;

public class PanelClickHandler : MonoBehaviour
{
    public void OnPanelClick(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("❗ 대상 씬 이름이 비어 있습니다.");
            return;
        }

        Debug.Log($"🟢 Panel clicked → {sceneName}");

        // 우선순위 1: SceneLoader 사용 (Additive 로드 + _Persistent 유지)
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(sceneName);
            return;
        }

        // 우선순위 2: SceneRouter 코루틴 직접 호출 (Additive 로드 + Active 전환)
        StartCoroutine(LoadViaRouter(sceneName));
    }

    private IEnumerator LoadViaRouter(string sceneName)
    {
        yield return SceneRouter.LoadContent(sceneName);
    }
}
