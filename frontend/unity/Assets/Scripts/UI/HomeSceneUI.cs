using UnityEngine;
using UnityEngine.UI;

public class HomeSceneUI : MonoBehaviour
{
    [Header("Canvas 안의 버튼을 드래그해서 연결하세요")]
    [SerializeField] private Button toLobbyButton;
    [SerializeField] private Button toTestButton;
    [SerializeField] private Button toRoomButton;

    private void Start()
    {
        if (toLobbyButton) toLobbyButton.onClick.AddListener(() => Load(SceneId.Lobby));
        else Debug.LogWarning("[HomeSceneUI] Lobby 버튼 미연결");

        if (toTestButton)  toTestButton.onClick.AddListener(() => Load(SceneId.Test));
        else Debug.LogWarning("[HomeSceneUI] Test 버튼 미연결");

        if (toRoomButton)  toRoomButton.onClick.AddListener(() => Load(SceneId.Room));
        else Debug.LogWarning("[HomeSceneUI] Room 버튼 미연결");
    }

    private void Load(string sceneName)
    {
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[HomeSceneUI] SceneLoader.Instance가 없습니다! (_Persistent에서 재생하세요)");
            return;
        }
        Debug.Log($"[HomeSceneUI] 이동 → {sceneName}");
        SceneLoader.Instance.LoadScene(sceneName);
    }
}
