using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportHoverScale : MonoBehaviour
{
    [Header("Visual Target (ex: Cylinder)")]
    public Transform visual;

    [Header("Scale Settings")]
    public float hoverScale = 1.15f;  // 얼마나 커질지
    public float smoothSpeed = 8f;    // 커지는 속도

    private XRBaseInteractable interactable;
    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (interactable == null)
        {
            Debug.LogWarning("[TeleportHoverScale] XRBaseInteractable is required on this object.", this);
            enabled = false;
            return;
        }

        if (visual != null)
            originalScale = visual.localScale;
        else
        {
            visual = transform;
            originalScale = transform.localScale;
        }

        targetScale = originalScale;
    }

    void OnEnable()
    {
        if (interactable == null)
            return;

        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    void OnDisable()
    {
        if (interactable == null)
            return;

        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    void Update()
    {
        if (visual != null)
        {
            visual.localScale = Vector3.Lerp(
                visual.localScale,
                targetScale,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        targetScale = originalScale * hoverScale;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        targetScale = originalScale;
    }
}
