using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum PhonemeType { Initial, Medial, Final }

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PhonemeDraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler, ICancelHandler
{
    public string symbol;
    public PhonemeType type = PhonemeType.Initial;
    public TMP_Text label;
    public TMP_FontAsset fontAsset;
    [Tooltip("드래그 중 임시로 붙일 부모 (보통 Canvas 루트)")]
    public Transform dragRoot;
    [Range(0.5f, 3.0f)] public float dragSensitivity = 1.0f;

    private CanvasGroup _cg;
    private Transform _originParent;
    private Vector3 _originPos;
    private RectTransform _rt;
    private Vector2 _originAnchoredPos;
    private bool _createdRuntimeLabel;
    private bool _snapped;
    private int _originSiblingIndex;
    private Canvas _parentCanvas;
    private Vector3 _dragWorldOffset;

    // Helper: Initial/Final are consonants; Medial is vowel
    public bool IsConsonant => type == PhonemeType.Initial || type == PhonemeType.Final;
    public bool IsVowel => type == PhonemeType.Medial;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _rt = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();
        EnsureLabel();
    }

    void OnEnable()
    {
        EnsureLabel();
    }

    private void EnsureLabel()
    {
        if (!label)
            label = GetComponentInChildren<TMP_Text>(true);
        // If a label is assigned but not a child of this tile, reparent so it renders on top of the tile
        if (label && label.transform.parent != transform)
        {
            label.rectTransform.SetParent(transform, false);
        }
        if (!label)
        {
            // Create a child TMP label at runtime if missing
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero;
            label = go.AddComponent<TextMeshProUGUI>();
            _createdRuntimeLabel = true;
        }
        if (label)
        {
            if (fontAsset) label.font = fontAsset;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center;
            label.text = symbol;
            var c = label.color; label.color = new Color(c.r, c.g, c.b, 1f);
            // Ensure text renders above the background
            label.transform.SetAsLastSibling();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_cg == null) _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _originParent = transform.parent;
        _originPos = transform.position;
        _originSiblingIndex = transform.GetSiblingIndex();
        if (_rt)
            _originAnchoredPos = _rt.anchoredPosition;
        _cg.blocksRaycasts = false;
        _snapped = false;

        if (dragRoot != null)
            transform.SetParent(dragRoot, true);

        // 포인터 월드 좌표와의 오프셋 계산 (카메라/월드 스페이스 캔버스 대응)
        if (TryGetPointerWorldPosition(eventData.pointerPressRaycast, eventData, out var pointerWorld))
            _dragWorldOffset = _rt.position - pointerWorld;
        else
            _dragWorldOffset = Vector3.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (TryGetPointerWorldPosition(eventData.pointerCurrentRaycast, eventData, out var pointerWorld))
        {
            _rt.position = pointerWorld + _dragWorldOffset;
        }
        else
        {
            // 폴백: 스크린 델타를 앵커 좌표에 적용
            _rt.anchoredPosition += eventData.delta * dragSensitivity;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_cg == null) _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _cg.blocksRaycasts = true;
        // If we already snapped into a valid slot, keep it there. Otherwise, return to origin.
        if (!_snapped)
            ReturnToOrigin();
    }

    public void SnapToSlot(Transform slotTransform)
    {
        if (slotTransform == null) return;
        transform.SetParent(slotTransform, false);
        if (_rt == null) _rt = GetComponent<RectTransform>();
        var slotRt = slotTransform.GetComponent<RectTransform>();
        if (_rt && slotRt)
        {
            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.anchoredPosition = Vector2.zero;
            _rt.localScale = Vector3.one;
        }
        else
        {
            transform.position = slotTransform.position;
        }
        _snapped = true;
    }

    public void ReturnToOrigin()
    {
        var parentRt = _originParent as RectTransform;
        transform.SetParent(_originParent, false);
        transform.SetSiblingIndex(Mathf.Clamp(_originSiblingIndex, 0, _originParent.childCount - 1));
        if (_rt)
            _rt.anchoredPosition = _originAnchoredPos;
        else
            transform.position = _originPos;
        if (parentRt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
        _snapped = false;
    }

    // XR UI Input Module 또는 드래그 손실 상황에서 EndDrag가 누락될 수 있으므로 보강
    public void OnPointerUp(PointerEventData eventData)
    {
        // 드래그 중이었다면 드래그 종료 처리 보강
        if (_cg != null && !_cg.blocksRaycasts)
        {
            OnEndDrag(eventData);
        }
    }

    public void OnCancel(BaseEventData eventData)
    {
        // 이벤트 취소 시 원복 보장
        ReturnToOrigin();
        if (_cg != null) _cg.blocksRaycasts = true;
    }

    private bool TryGetPointerWorldPosition(RaycastResult raycastResult, PointerEventData eventData, out Vector3 worldPoint)
    {
        if (raycastResult.gameObject != null)
        {
            worldPoint = raycastResult.worldPosition;
            return true;
        }

        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null && _parentCanvas != null)
            eventCamera = _parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _rt,
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
