using UnityEngine;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

public class StoneDropZone : MonoBehaviour, IDropHandler
{
    [Tooltip("드롭 후 붙일 부모 (CountDisplay로 지정)")]
    public Transform slotParent; // Inspector에서 CountDisplay Transform을 드래그해서 지정
    
    [Tooltip("Stage20Controller 참조 (비우면 자동으로 찾습니다)")]
    public Stage20Controller stageController; // Inspector에서 할당하거나 자동으로 찾음
    
    [Tooltip("이 Slot의 번호 (예: Slot_1이면 1, CountDisplay면 0으로 두기)")]
    public int slotNumber = 0; // Inspector에서 설정하거나 자동으로 파싱
    
    [Tooltip("StoneSlots 부모 (숫자 매칭을 위해 Slot들을 찾을 때 사용)")]
    public Transform stoneSlotsParent; // StoneSlots 오브젝트의 Transform

    private void Awake()
    {
        // Stage20Controller가 할당되지 않았으면 자동으로 찾기
        if (stageController == null)
            stageController = FindObjectOfType<Stage20Controller>();
        
        // slotNumber가 0이면 이름에서 자동으로 파싱 (예: "Slot_1" -> 1)
        if (slotNumber == 0)
        {
            slotNumber = ExtractNumberFromName(gameObject.name);
        }
        
        // stoneSlotsParent가 없으면 부모에서 찾기
        if (stoneSlotsParent == null)
        {
            // 현재 오브젝트의 부모가 StoneSlots일 수 있음
            Transform parent = transform.parent;
            if (parent != null && parent.name.Contains("StoneSlots"))
            {
                stoneSlotsParent = parent;
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        var draggable = eventData.pointerDrag.GetComponent<StoneDraggable>();
        if (draggable == null)
            return;

        // Stone의 번호 추출 (예: "Stone_1" -> 1)
        int stoneNumber = ExtractNumberFromName(eventData.pointerDrag.name);
        
        // CountDisplay에서 드롭된 경우 (slotNumber == 0)
        if (slotNumber == 0)
        {
            // 해당 번호에 맞는 Slot을 찾아서 그 위치에 배치
            PlaceStoneAtMatchingSlot(eventData.pointerDrag, stoneNumber);
        }
        else
        {
            // 개별 Slot에 드롭된 경우: 숫자가 일치하는지 확인
            if (stoneNumber != slotNumber)
            {
                Debug.LogWarning($"[StoneDropZone] 번호 불일치: Stone_{stoneNumber}을 Slot_{slotNumber}에 드롭할 수 없습니다.");
                return; // 드롭 거부
            }
            
            // CountDisplay로 이동
            Transform targetParent = slotParent != null ? slotParent : transform;
            eventData.pointerDrag.transform.SetParent(targetParent, false);

            if (eventData.pointerDrag.TryGetComponent(out RectTransform rt))
            {
                // 이 Slot의 위치에 Stone을 배치
                RectTransform slotRT = GetComponent<RectTransform>();
                if (slotRT != null)
                {
                    rt.anchoredPosition = slotRT.anchoredPosition;
                }
                else
                {
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }

        // 드롭된 Stone 개수를 세어서 Stage20Controller에 알림
        CountStonesAndReport();
    }

    private void PlaceStoneAtMatchingSlot(GameObject stone, int stoneNumber)
    {
        // CountDisplay로 이동
        Transform targetParent = slotParent != null ? slotParent : transform;
        stone.transform.SetParent(targetParent, false);

        // 해당 번호에 맞는 Slot 찾기
        StoneDropZone matchingSlot = FindSlotByNumber(stoneNumber);
        
        if (matchingSlot != null)
        {
            RectTransform slotRT = matchingSlot.GetComponent<RectTransform>();
            RectTransform stoneRT = stone.GetComponent<RectTransform>();
            
            if (slotRT != null && stoneRT != null)
            {
                // Slot의 위치에 Stone 배치 (CountDisplay 기준)
                stoneRT.anchoredPosition = slotRT.anchoredPosition;
                Debug.Log($"[StoneDropZone] Stone_{stoneNumber}을 Slot_{stoneNumber} 위치에 배치");
            }
            else
            {
                stoneRT.anchoredPosition = Vector2.zero;
            }
        }
        else
        {
            Debug.LogWarning($"[StoneDropZone] Slot_{stoneNumber}을 찾을 수 없습니다.");
            if (stone.TryGetComponent(out RectTransform rt))
                rt.anchoredPosition = Vector2.zero;
        }
    }

    private StoneDropZone FindSlotByNumber(int number)
    {
        // stoneSlotsParent에서 모든 Slot 찾기
        Transform searchParent = stoneSlotsParent != null ? stoneSlotsParent : transform.parent;
        if (searchParent == null)
            return null;

        // 모든 자식 중에서 StoneDropZone 컴포넌트를 가진 것 찾기
        StoneDropZone[] allSlots = searchParent.GetComponentsInChildren<StoneDropZone>();
        
        foreach (var slot in allSlots)
        {
            // CountDisplay는 제외 (slotNumber가 0이 아닌 것만)
            if (slot.slotNumber == number && slot != this)
            {
                return slot;
            }
        }

        // slotNumber가 0이면 이름에서도 확인
        foreach (var slot in allSlots)
        {
            if (slot == this) continue; // 자기 자신 제외
            
            int slotNum = slot.slotNumber;
            if (slotNum == 0)
            {
                slotNum = ExtractNumberFromName(slot.gameObject.name);
            }
            
            if (slotNum == number)
            {
                return slot;
            }
        }

        return null;
    }

    private void CountStonesAndReport()
    {
        // CountDisplay의 자식 중 StoneDraggable만 카운트 (Slot 제외)
        Transform targetParent = slotParent != null ? slotParent : transform;
        
        if (targetParent == null)
        {
            Debug.LogWarning("[StoneDropZone] slotParent(CountDisplay)가 설정되지 않았습니다.");
            return;
        }

        int stoneCount = 0;

        // targetParent(CountDisplay)의 자식 중 StoneDraggable 컴포넌트를 가진 것만 카운트
        // StoneDropZone(Slot)은 제외
        for (int i = 0; i < targetParent.childCount; i++)
        {
            var child = targetParent.GetChild(i);
            // StoneDraggable이 있고, StoneDropZone이 아닌 것만 카운트
            if (child.GetComponent<StoneDraggable>() != null && 
                child.GetComponent<StoneDropZone>() == null)
            {
                stoneCount++;
            }
        }

        // Stage20Controller에 개수 알림
        if (stageController != null)
        {
            stageController.ReportStoneCount(stoneCount);
            Debug.Log($"[StoneDropZone] 드롭된 Stone 개수: {stoneCount} (CountDisplay: {targetParent.name})");
        }
        else
        {
            Debug.LogWarning("[StoneDropZone] Stage20Controller를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 오브젝트 이름에서 숫자 추출 (예: "Stone_1" -> 1, "Slot_2" -> 2)
    /// </summary>
    private int ExtractNumberFromName(string name)
    {
        // 정규식으로 "_숫자" 패턴 찾기
        Match match = Regex.Match(name, @"_(\d+)");
        if (match.Success && match.Groups.Count > 1)
        {
            if (int.TryParse(match.Groups[1].Value, out int number))
                return number;
        }
        return 0; // 숫자를 찾지 못하면 0 반환
    }
}