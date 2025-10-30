using UnityEngine;
using UnityEngine.UI;

public class PanelClickHandler : MonoBehaviour
{
    [Tooltip("이 패널 클릭 시 이동할 씬 이름")]
    public string targetScene;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnPanelClicked);
    }

    void OnPanelClicked()
    {
        Debug.Log($"🟢 Panel clicked → {targetScene}");
        SceneFlowManager.I.LoadScene(targetScene);
    }
}
