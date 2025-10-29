using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class LoadSceneOnSelect : MonoBehaviour
{
    [Header("Build Settings에 등록된 씬 이름")]
    public string targetSceneName;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        // 트리거(Select) 눌렀을 때
        interactable.selectEntered.AddListener(OnSelectEntered);
        // 혹시 'Activate'(보조 버튼)로 바꾸고 싶다면 아래 주석 해제하고 위는 주석
        // interactable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        // interactable.activated.RemoveListener(OnActivated);
    }

    private void OnSelectEntered(SelectEnterEventArgs _)
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("[LoadSceneOnSelect] targetSceneName이 비었습니다.");
        }
    }

    // 보조 버튼(Activate)로 쓰고 싶을 때
    // private void OnActivated(ActivateEventArgs _)
    // {
    //     OnSelectEntered(null);
    // }
}
