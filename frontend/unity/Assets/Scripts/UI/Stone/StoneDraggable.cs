using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class StoneDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("드래그 중 임시로 붙일 부모 (보통 Canvas 루트)")]
    public Transform dragRoot;
    
    [Tooltip("드래그 감도 (1.0 = 기본, 높을수록 더 빠르게 반응)")]
    [Range(0.5f, 3.0f)]
    public float dragSensitivity = 0.8f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 startAnchoredPos;
    private Canvas parentCanvas;
    private Vector3 dragWorldOffset; // 드래그 시작 시 클릭 위치와 오브젝트 중심의 월드 오프셋

    private Transform initialParent;
    private Vector2 initialAnchoredPos;
    private int initialSiblingIndex;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private bool hasInitialState;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 부모 Canvas 찾기
        parentCanvas = GetComponentInParent<Canvas>();

        CaptureInitialState();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        startAnchoredPos = rectTransform.anchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }

        if (dragRoot != null)
            transform.SetParent(dragRoot, true);

        if (TryGetPointerWorldPosition(eventData.pointerPressRaycast, eventData, out Vector3 pointerWorld))
        {
            dragWorldOffset = rectTransform.position - pointerWorld;
        }
        else
        {
            dragWorldOffset = Vector3.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (TryGetPointerWorldPosition(eventData.pointerCurrentRaycast, eventData, out Vector3 pointerWorld))
        {
            rectTransform.position = pointerWorld + dragWorldOffset;
        }
        else
        {
            // 포인터 월드 좌표를 얻지 못하면 델타 이동으로 폴백
            rectTransform.anchoredPosition += eventData.delta * dragSensitivity;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (transform.parent == dragRoot)
        {
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = startAnchoredPos;
        }
    }

    public void ResetToInitialState()
    {
        if (!hasInitialState || rectTransform == null)
            return;

        transform.SetParent(initialParent, false);
        transform.SetSiblingIndex(initialSiblingIndex);
        rectTransform.anchoredPosition = initialAnchoredPos;
        rectTransform.localRotation = initialLocalRotation;
        rectTransform.localScale = initialLocalScale;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    private void CaptureInitialState()
    {
        if (rectTransform == null)
            return;

        initialParent = transform.parent;
        initialSiblingIndex = transform.GetSiblingIndex();
        initialAnchoredPos = rectTransform.anchoredPosition;
        initialLocalRotation = rectTransform.localRotation;
        initialLocalScale = rectTransform.localScale;
        hasInitialState = initialParent != null;
    }

    private bool TryGetPointerWorldPosition(RaycastResult raycastResult, PointerEventData eventData, out Vector3 worldPoint)
    {
        if (raycastResult.gameObject != null)
        {
            worldPoint = raycastResult.worldPosition;
            return true;
        }

        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null && parentCanvas != null)
            eventCamera = parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                eventData.position,
                eventCamera,
                out worldPoint))
        {
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }
}