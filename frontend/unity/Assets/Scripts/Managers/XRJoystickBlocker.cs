using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq; // <-- 배열 탐색을 위해 필요

public class XRJoystickInputBlocker : MonoBehaviour
{
    private ActionBasedContinuousMoveProvider moveProvider;
    private ActionBasedContinuousTurnProvider turnProvider;

    // 🚫 조이스틱을 막을 씬 이름 리스트
    [SerializeField]
    private string[] blockedScenes = { "Level1", "1.1", "1.2" };

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;
        FindProviders();
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void FindProviders()
    {
        moveProvider = FindObjectOfType<ActionBasedContinuousMoveProvider>(true);
        turnProvider = FindObjectOfType<ActionBasedContinuousTurnProvider>(true);

        if (moveProvider == null)
            Debug.LogWarning("🚨 MoveProvider not found!");
        if (turnProvider == null)
            Debug.LogWarning("🚨 TurnProvider not found!");
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (moveProvider == null || turnProvider == null)
            FindProviders();

        // 🎯 blockedScenes 배열 안에 해당 씬 이름이 포함되어 있으면 차단
        bool allowMove = !blockedScenes.Contains(newScene.name);
        ToggleJoystickInputs(allowMove);
    }

    private void ToggleJoystickInputs(bool enabled)
    {
        if (moveProvider != null && moveProvider.leftHandMoveAction.action != null)
        {
            if (enabled) moveProvider.leftHandMoveAction.action.Enable();
            else moveProvider.leftHandMoveAction.action.Disable();
        }

        if (moveProvider != null && moveProvider.rightHandMoveAction.action != null)
        {
            if (enabled) moveProvider.rightHandMoveAction.action.Enable();
            else moveProvider.rightHandMoveAction.action.Disable();
        }

        if (turnProvider != null && turnProvider.leftHandTurnAction.action != null)
        {
            if (enabled) turnProvider.leftHandTurnAction.action.Enable();
            else turnProvider.leftHandTurnAction.action.Disable();
        }

        if (turnProvider != null && turnProvider.rightHandTurnAction.action != null)
        {
            if (enabled) turnProvider.rightHandTurnAction.action.Enable();
            else turnProvider.rightHandTurnAction.action.Disable();
        }

        Debug.Log(enabled ? "🟢 조이스틱 입력 허용" : "🔴 조이스틱 입력 차단");
    }
}
