using UnityEngine;

public class PanelClickHandler : MonoBehaviour
{
    private SceneFlowManager sceneFlow;

    private void Start()
    {
        sceneFlow = SceneFlowManager.I;
        if (sceneFlow == null)
            Debug.LogError("❌ SceneFlowManager를 찾을 수 없습니다. Persistent 씬이 유지되는지 확인하세요.");
    }

    public void OnPanelClick(string sceneName)
    {
        if (sceneFlow == null)
        {
            Debug.LogError("❌ SceneFlowManager 연결 안됨!");
            return;
        }

        Debug.Log($"🟢 Panel clicked → {sceneName}");
        sceneFlow.LoadScene(sceneName);
    }
}
