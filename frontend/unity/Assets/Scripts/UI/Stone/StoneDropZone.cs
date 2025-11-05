using UnityEngine;
using UnityEngine.EventSystems;

public class StoneDropZone : MonoBehaviour, IDropHandler
{
    [Tooltip("드롭 후 붙일 부모 (비우면 이 DropZone transform 사용)")]
    public Transform slotParent; // Inspector에서 자신의 Transform을 드래그해서 지정
    
    [Tooltip("Stage20Controller 참조 (비우면 자동으로 찾습니다)")]
    public Stage20Controller stageController; // Inspector에서 할당하거나 자동으로 찾음

    private void Awake()
    {
        // Stage20Controller가 할당되지 않았으면 자동으로 찾기
        if (stageController == null)
            stageController = FindObjectOfType<Stage20Controller>();
    }

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

        // 드롭된 Stone 개수를 세어서 Stage20Controller에 알림
        CountStonesAndReport();
    }

    private void CountStonesAndReport()
    {
        Transform targetParent = slotParent != null ? slotParent : transform;
        int stoneCount = 0;

        // targetParent의 자식 중 StoneDraggable 컴포넌트를 가진 것만 카운트
        for (int i = 0; i < targetParent.childCount; i++)
        {
            var child = targetParent.GetChild(i);
            if (child.GetComponent<StoneDraggable>() != null)
                stoneCount++;
        }

        // Stage20Controller에 개수 알림
        if (stageController != null)
        {
            stageController.ReportStoneCount(stoneCount);
            Debug.Log($"[StoneDropZone] 드롭된 Stone 개수: {stoneCount}");
        }
        else
        {
            Debug.LogWarning("[StoneDropZone] Stage20Controller를 찾을 수 없습니다.");
        }
    }
}