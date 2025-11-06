using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        if (eventData?.pointerDrag == null)
            return;

        GameObject stone = eventData.pointerDrag;
        if (stone.GetComponent<StoneDraggable>() == null)
            return;

        int stoneNumber = ExtractNumberFromName(stone.name);

        if (slotNumber == 0)
        {
            PlaceStoneAtMatchingSlot(stone, stoneNumber);
        }
        else
        {
            if (stoneNumber != slotNumber)
            {
                Debug.LogWarning($"[StoneDropZone] 번호 불일치: Stone_{stoneNumber}을 Slot_{slotNumber}에 드롭할 수 없습니다.");
                return;
            }

            SnapStoneToSlot(stone, this);
        }

        CountStonesAndReport();
    }

    private void PlaceStoneAtMatchingSlot(GameObject stone, int stoneNumber)
    {
        StoneDropZone matchingSlot = FindSlotByNumber(stoneNumber);
        SnapStoneToSlot(stone, matchingSlot);

        if (matchingSlot != null)
        {
            Debug.Log($"[StoneDropZone] Stone_{stoneNumber}을 Slot_{stoneNumber} 위치에 배치");
        }
        else
        {
            Debug.LogWarning($"[StoneDropZone] Slot_{stoneNumber}을 찾을 수 없습니다.");
        }
    }

    private void SnapStoneToSlot(GameObject stone, StoneDropZone targetSlot)
    {
        if (stone == null)
            return;

        Transform container = slotParent != null ? slotParent : transform;

        if (container == null)
        {
            Debug.LogWarning("[StoneDropZone] 유효한 부모 컨테이너를 찾지 못했습니다.");
            return;
        }

        stone.transform.SetParent(container, false);
        stone.transform.SetAsLastSibling();

        if (container.TryGetComponent(out LayoutGroup layoutGroup) && layoutGroup != null)
        {
            if (stone.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement.ignoreLayout = true;
            }
            else
            {
                layoutElement = stone.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
            }
        }

        RectTransform referenceSlot = targetSlot != null ? targetSlot.GetComponent<RectTransform>() : null;

        if (stone.TryGetComponent(out RectTransform stoneRT))
        {
            stoneRT.anchorMin = new Vector2(0.5f, 0.5f);
            stoneRT.anchorMax = new Vector2(0.5f, 0.5f);
            stoneRT.anchoredPosition = Vector2.zero;
            stoneRT.localScale = Vector3.one;

            if (referenceSlot != null)
            {
                stoneRT.position = referenceSlot.position;
                stoneRT.rotation = referenceSlot.rotation;
            }
            else
            {
                stoneRT.localRotation = Quaternion.identity;
            }
        }

        if (stone.TryGetComponent(out CanvasGroup cg))
        {
            cg.blocksRaycasts = true;
            cg.alpha = 1f;
        }
    }

    private StoneDropZone FindSlotByNumber(int number)
    {
        if (number <= 0)
            return null;

        StoneDropZone[] candidates = GetSearchableSlots();
        if (candidates == null || candidates.Length == 0)
            return null;

        foreach (var slot in candidates)
        {
            if (slot == null || slot == this)
                continue;

            int slotNum = slot.slotNumber;
            if (slotNum == 0)
            {
                slotNum = ExtractNumberFromName(slot.gameObject.name);
            }

            if (slotNum == number)
                return slot;
        }

        return null;
    }

    private StoneDropZone[] GetSearchableSlots()
    {
        Transform searchRoot = stoneSlotsParent;

        if (searchRoot == null && slotParent != null)
            searchRoot = slotParent;

        if (searchRoot == null && stageController != null && stageController.stoneBoard != null)
            searchRoot = stageController.stoneBoard.transform;

        if (searchRoot != null)
            return searchRoot.GetComponentsInChildren<StoneDropZone>(true);

        return FindObjectsOfType<StoneDropZone>(true);
    }

    private void CountStonesAndReport()
    {
        Transform targetParent = slotParent != null ? slotParent : transform;

        if (targetParent == null)
        {
            Debug.LogWarning("[StoneDropZone] slotParent(CountDisplay)가 설정되지 않았습니다.");
            return;
        }

        int stoneCount = CountStoneDraggablesRecursive(targetParent);

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

    private int CountStoneDraggablesRecursive(Transform root)
    {
        int count = 0;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);

            if (child.GetComponent<StoneDraggable>() != null &&
                child.GetComponent<StoneDropZone>() == null)
            {
                count++;
            }

            if (child.childCount > 0)
                count += CountStoneDraggablesRecursive(child);
        }

        return count;
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