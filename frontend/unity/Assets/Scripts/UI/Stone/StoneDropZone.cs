using UnityEngine;
using UnityEngine.EventSystems;

public class StoneDropZone : MonoBehaviour, IDropHandler
{
    [Tooltip("드롭 후 붙일 부모 (비우면 이 DropZone transform 사용)")]
    public Transform slotParent; // Inspector에서 자신의 Transform을 드래그해서 지정

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        var draggable = eventData.pointerDrag.GetComponent<StoneDraggable>();
        if (draggable == null)
            return;

        Transform targetParent = slotParent != null ? slotParent : transform;
        eventData.pointerDrag.transform.SetParent(targetParent, false);

        if (eventData.pointerDrag.TryGetComponent(out RectTransform rt))
            rt.anchoredPosition = Vector2.zero;
    }
}