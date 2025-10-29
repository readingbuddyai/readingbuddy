using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class LoadSceneOnSelect : MonoBehaviour
{
    [Header("Build Settings에 등록된 씬 이름(대소문자 정확히)")]
    public string targetSceneName;

    private XRSimpleInteractable interactable;
    private bool _isLoading; // 중복 로드 가드

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
        // 필요 시: interactable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        // 필요 시: interactable.activated.RemoveListener(OnActivated);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        TryLoad();
    }

    // 보조 버튼으로 쓰고 싶다면
    // private void OnActivated(ActivateEventArgs args) => TryLoad();

    private void TryLoad()
    {
        if (_isLoading) return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("[LoadSceneOnSelect] targetSceneName이 비었습니다.");
            return;
        }

        Debug.Log($"[LoadSceneOnSelect] Select! 요청 씬 = '{targetSceneName}'");

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError($"[LoadSceneOnSelect] '{targetSceneName}' 로드 불가. " +
                           $"Build Settings(Scenes In Build)에 등록/이름 정확도 확인");
            return;
        }

        _isLoading = true;
        Debug.Log($"[LoadSceneOnSelect] Loading → {targetSceneName}");
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}
