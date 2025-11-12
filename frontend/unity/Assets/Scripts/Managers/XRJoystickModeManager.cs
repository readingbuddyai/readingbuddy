using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class XRJoystickModeManager : MonoBehaviour
{
    [Header("References")]
    public ActionBasedContinuousTurnProvider turnProvider;
    public InputActionReference scrollAction;
    public Canvas panelCanvas;

    private void Start()
    {
        // ✅ 자동으로 Persistent 안의 TurnProvider 찾기
        if (turnProvider == null)
            turnProvider = FindObjectOfType<ActionBasedContinuousTurnProvider>(true);

        if (turnProvider == null)
            Debug.LogWarning("⚠️ TurnProvider not found! Check if XR Origin is loaded.");

        SetUIMode(true);
    }

    public void SetUIMode(bool enable)
    {
        if (turnProvider != null)
            turnProvider.enabled = !enable; // 회전 끄기/켜기

        if (scrollAction != null && scrollAction.action != null)
        {
            if (enable) scrollAction.action.Enable();
            else scrollAction.action.Disable();
        }

        Debug.Log(enable ? "🟣 UI 모드 활성화 (조이스틱 = 스크롤)" : "🟢 회전 복귀");
    }
}
