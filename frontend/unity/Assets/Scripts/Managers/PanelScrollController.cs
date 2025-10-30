using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PanelScrollController : MonoBehaviour
{
    [Header("References")]
    public ScrollRect scrollRect; // Scroll View 연결
    public InputActionReference scrollAction; // UIInteraction/RightJoystickScroll 연결
    public float scrollSpeed = 0.5f;

    private void OnEnable()
    {
        scrollAction.action.Enable();
    }

    private void OnDisable()
    {
        scrollAction.action.Disable();
    }

    private void Update()
    {
        // ✅ InputActionReference에서 Vector2 읽기
        Vector2 input = scrollAction.action.ReadValue<Vector2>();

        // ✅ 오른쪽 조이스틱 X축 입력으로 스크롤
        float horizontal = input.x * scrollSpeed * Time.deltaTime;

        // ✅ ScrollRect 스크롤 값 적용
        scrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + horizontal);
    }
}
