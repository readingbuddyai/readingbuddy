using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Stage 4.2 전용 슬롯 드롭 핸들러 (공유 UI를 건드리지 않기 위해 별도 스크립트)
public class PhonemeSlotUI42 : MonoBehaviour, IDropHandler
{
    [Tooltip("0=초성, 1=중성, 2=종성")]
    public int slotIndex = 0;
    public TMP_Text boxText;
    public Stage42Controller controller;

    void Awake()
    {
        AutoAssignSlotIndex();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAssignSlotIndex();
    }
#endif

    private void AutoAssignSlotIndex()
    {
        if (!controller) return;
        var t = transform;
        if (controller.choseongBox && (t == controller.choseongBox.transform || t.IsChildOf(controller.choseongBox.transform)))
        {
            slotIndex = 0;
        }
        else if (controller.jungseongBox && (t == controller.jungseongBox.transform || t.IsChildOf(controller.jungseongBox.transform)))
        {
            slotIndex = 1;
        }
        else if (controller.jongseongBox && (t == controller.jongseongBox.transform || t.IsChildOf(controller.jongseongBox.transform)))
        {
            slotIndex = 2;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<PhonemeDraggableUI>() : null;
        if (drag == null) return;
        if (controller == null) return;

        // 드롭 허용 여부(초기/교정 단계 게이트)
        if (!controller.CanAcceptDropToSlot(slotIndex)) return;

        var symbol = drag.symbol ?? string.Empty;
        controller.OnUserDrop(slotIndex, symbol);
        // 타일은 원위치로 돌아가고, 슬롯 텍스트는 컨트롤러가 채웁니다.
    }
}

