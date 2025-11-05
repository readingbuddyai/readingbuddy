using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PhonemeSlotUI : MonoBehaviour, IDropHandler
{
    [Tooltip("0=초성, 1=중성, 2=종성")]
    public int slotIndex = 0;
    public TMP_Text boxText;
    public Stage41Controller controller;

    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<PhonemeDraggableUI>() : null;
        if (drag == null) return;
        var symbol = drag.symbol ?? string.Empty;

        // 컨트롤러에 위임하여 정답/오답 판정과 연출, 로깅 처리
        if (controller != null)
        {
            controller.OnUserDrop(slotIndex, symbol);
        }

        // 슬롯 텍스트는 컨트롤러가 결정(정답 시 채움)
    }
}

