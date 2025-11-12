using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class XRPanelScrollAnimator : MonoBehaviour
{
    [Header("References")]
    public ScrollRect scrollRect;
    public RectTransform[] panels;

    [Header("Scroll Settings")]
    public float scrollSpeed = 3f;
    public float inputThreshold = 0.6f;

    [Header("Scale Animation")]
    public float focusedScale = 1.1f;
    public float scaleSpeed = 5f;

    private int currentPanel = 0;
    private float targetPosition = 0f;
    private float lastInputTime;

    void Start()
    {
        targetPosition = 0f;
    }

    void Update()
    {
        Vector2 input = ReadJoystickInput();

        // 조이스틱 입력 감지
        if (input.x > inputThreshold && Time.time - lastInputTime > 0.4f)
        {
            MoveNext();
            lastInputTime = Time.time;
        }
        else if (input.x < -inputThreshold && Time.time - lastInputTime > 0.4f)
        {
            MovePrev();
            lastInputTime = Time.time;
        }

        // 스크롤 이동 보간
        scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
            scrollRect.horizontalNormalizedPosition,
            targetPosition,
            Time.deltaTime * scrollSpeed
        );

        // 현재 패널 확대 애니메이션
        AnimateFocus();
    }

    Vector2 ReadJoystickInput()
    {
        // PC Gamepad
        if (Gamepad.current != null)
            return Gamepad.current.rightStick.ReadValue();

        // XR Controller 입력 (오른손 스틱)
        var control = InputSystem.FindControl("<XRController>{RightHand}/thumbstick") as InputControl<Vector2>;
        if (control != null)
            return control.ReadValue();

        return Vector2.zero;
    }

    void MoveNext()
    {
        if (currentPanel < panels.Length - 1)
        {
            currentPanel++;
            targetPosition = (float)currentPanel / (panels.Length - 1);
        }
    }

    void MovePrev()
    {
        if (currentPanel > 0)
        {
            currentPanel--;
            targetPosition = (float)currentPanel / (panels.Length - 1);
        }
    }

    void AnimateFocus()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            float targetScale = (i == currentPanel) ? focusedScale : 1f;
            panels[i].localScale = Vector3.Lerp(
                panels[i].localScale,
                Vector3.one * targetScale,
                Time.deltaTime * scaleSpeed
            );
        }
    }
}
