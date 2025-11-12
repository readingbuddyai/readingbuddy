using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PhonemeSlotUI : MonoBehaviour, IDropHandler
{
    [Tooltip("0=초성, 1=중성, 2=종성")]
    public int slotIndex = 0;
    public TMP_Text boxText;
    public Stage41Controller controller;

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

        // 드롭 허용 상태가 아니면 무시 (교정 단계/해당 슬롯 차례/미완료 등)
        if (!controller.CanAcceptDropToSlot(slotIndex)) return;

        var symbol = drag.symbol ?? string.Empty;
        bool correct = controller.IsCorrectForSlot(slotIndex, symbol);

        // 컨트롤러에 시도/정오 처리 위임 (로그/텍스트/다음 단계)
        controller.OnUserDrop(slotIndex, symbol);
        // 상자(타일)는 항상 원 위치로 복귀하고, 슬롯에는 텍스트만 채웁니다.
        // 정답 여부에 관계없이 EndDrag에서 ReturnToOrigin이 실행됩니다.
    }
}




