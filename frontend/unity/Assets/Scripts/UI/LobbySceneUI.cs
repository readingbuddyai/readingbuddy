using UnityEngine;
using UnityEngine.UI;

public class LobbySceneUI : MonoBehaviour
{
    [Header("Canvas 안의 버튼을 드래그해서 연결하세요")]
    [SerializeField] private Button toLevel1Button;
    [SerializeField] private Button toLevel2Button;
    [SerializeField] private Button toLevel3Button;
    [SerializeField] private Button toLevel4Button;

    private void Awake()
    {
        Debug.Log("[LobbySceneUI] Awake 실행됨 / 오토 와이어링 시도");
        var t = transform;

        // 인스펙터 미연결 시 자동 탐색 (옵션)
        toLevel1Button ??= t.Find("ToLevel1/Go Level1")?.GetComponent<Button>();
        toLevel2Button ??= t.Find("ToLevel2/Go Level2")?.GetComponent<Button>();
        toLevel3Button ??= t.Find("ToLevel3/Go Level3")?.GetComponent<Button>();
        toLevel4Button ??= t.Find("ToLevel4/Go Level4")?.GetComponent<Button>();
    }

    private void OnEnable()
    {
        Debug.Log("[LobbySceneUI] OnEnable / 리스너 등록");

        if (toLevel1Button) toLevel1Button.onClick.AddListener(OnClickLevel1);
        else Debug.LogWarning("[LobbySceneUI] Level1 버튼 미연결");

        if (toLevel2Button) toLevel2Button.onClick.AddListener(OnClickLevel2);
        else Debug.LogWarning("[LobbySceneUI] Level2 버튼 미연결");

        if (toLevel3Button) toLevel3Button.onClick.AddListener(OnClickLevel3);
        else Debug.LogWarning("[LobbySceneUI] Level3 버튼 미연결");

        if (toLevel4Button) toLevel4Button.onClick.AddListener(OnClickLevel4);
        else Debug.LogWarning("[LobbySceneUI] Level4 버튼 미연결");
    }

    private void OnDisable()
    {
        if (toLevel1Button) toLevel1Button.onClick.RemoveListener(OnClickLevel1);
        if (toLevel2Button) toLevel2Button.onClick.RemoveListener(OnClickLevel2);
        if (toLevel3Button) toLevel3Button.onClick.RemoveListener(OnClickLevel3);
        if (toLevel4Button) toLevel4Button.onClick.RemoveListener(OnClickLevel4);
    }

    private void OnClickLevel1() => Load(SceneId.Level1);
    private void OnClickLevel2() => Load(SceneId.Level2);
    private void OnClickLevel3() => Load(SceneId.Level3);
    private void OnClickLevel4() => Load(SceneId.Level4);

    private void Load(string sceneName)
    {
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[LobbySceneUI] SceneLoader.Instance가 없습니다! (_Persistent에서 재생하세요)");
            return;
        }

        Debug.Log($"[LobbySceneUI] 이동 → {sceneName}");
        SceneLoader.Instance.LoadScene(sceneName);
    }
}
