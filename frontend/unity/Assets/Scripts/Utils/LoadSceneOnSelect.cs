using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(XRSimpleInteractable))]
public class LoadSceneOnSelect : MonoBehaviour
{
    [Header("Build Settings에 등록된 씬 이름")]
    public string targetSceneName;

    private XRSimpleInteractable interactable;
    private bool _isLoading; // 중복 로드 가드

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        interactable.activated.RemoveListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs _)
    {
        TryLoad();
    }

    private void TryLoad()
    {
        if (_isLoading) return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("[LoadSceneOnSelect] targetSceneName이 비었습니다.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError($"[LoadSceneOnSelect] '{targetSceneName}' 로드 불가. " +
                           $"Build Settings(Scenes In Build) 등록/이름 확인");
            return;
        }

        _isLoading = true;
        interactable.enabled = false;
        Debug.Log($"[LoadSceneOnSelect] Loading → {targetSceneName}");
        Utils.GlobalSfxManager.Instance?.PlaySceneTransitionSfx();
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        // 1️⃣ SceneRouter.cs 사용 (ActiveScene 전환 포함)
        yield return SceneRouter.LoadContent(targetSceneName);

        _isLoading = false;
        interactable.enabled = true;
    }
}
