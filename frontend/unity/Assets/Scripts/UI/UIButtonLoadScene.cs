using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonLoadScene : MonoBehaviour
{
    [Header("이 버튼이 이동할 씬 이름 (Build Settings 등록 필수)")]
    public string targetSceneName = "Home";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[UIButtonLoadScene] targetSceneName이 비어 있음");
            return;
        }

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[UIButtonLoadScene] SceneLoader.Instance == null → _Persistent 씬에 SceneLoader가 존재해야 합니다!");
            return;
        }

        Debug.Log($"[UIButtonLoadScene] '{targetSceneName}' 로드 요청 (→ SceneLoader 통해 Additive 로드)");
        SceneLoader.Instance.LoadScene(targetSceneName);
    }
}
