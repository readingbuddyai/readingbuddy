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
    public float dragSensitivity = 1.0f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 startAnchoredPos;
    private Canvas parentCanvas;
    private Vector2 dragOffset; // 드래그 시작 시 클릭 위치와 오브젝트 중심의 오프셋

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 부모 Canvas 찾기
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        startAnchoredPos = rectTransform.anchoredPosition;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;

        if (dragRoot != null)
            transform.SetParent(dragRoot, true);

        // 드래그 시작 시 오프셋 계산 (포인터 위치와 오브젝트 중심의 차이)
        if (parentCanvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out Vector2 localPoint);
            
            dragOffset = rectTransform.anchoredPosition - localPoint;
        }
        else
        {
            dragOffset = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentCanvas != null)
        {
            // 포인터의 현재 위치를 로컬 좌표로 변환
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    parentCanvas.worldCamera,
                    out Vector2 localPoint);
                
                // 오프셋을 적용하여 오브젝트 위치 설정
                rectTransform.anchoredPosition = localPoint + dragOffset;
            }
            else
            {
                // 폴백: 델타 사용 (감도 적용)
                rectTransform.anchoredPosition += eventData.delta * dragSensitivity;
            }
        }
        else
        {
            // Canvas를 찾을 수 없으면 델타 사용 (감도 적용)
            rectTransform.anchoredPosition += eventData.delta * dragSensitivity;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (transform.parent == dragRoot)
        {
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = startAnchoredPos;
        }
    }
}