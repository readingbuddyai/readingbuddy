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

        // 컨트롤러???�임?�여 ?�답/?�답 ?�정�??�출, 로깅 처리
        if (controller != null)
        {
            controller.OnUserDrop(slotIndex, symbol);
        }

        // ?�롯 ?�스?�는 컨트롤러가 결정(?�답 ??채�?)
    }
}


