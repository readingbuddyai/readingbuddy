using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class StoneDropZoneBase<TStageController> : MonoBehaviour, IDropHandler
    where TStageController : MonoBehaviour
{
    [Tooltip("드롭 후 붙일 부모 (CountDisplay)")]
    [SerializeField] protected Transform slotParent;

    [Tooltip("Stage Controller 참조 (비워 두면 자동 탐색)")]
    [SerializeField] protected TStageController stageController;

    [Tooltip("이 Slot의 번호 (예: Slot_1이면 1, CountDisplay면 0으로 두기)")]
    [SerializeField] protected int slotNumber = 0;

    [Tooltip("StoneSlots 부모 (숫자 매칭을 위해 Slot들을 찾을 때 사용)")]
    [SerializeField] protected Transform stoneSlotsParent;

    protected virtual string LogPrefix => typeof(TStageController).Name;

    public Transform SlotParent => slotParent;
    public int SlotNumber => slotNumber;
    public Transform StoneSlotsParent => stoneSlotsParent;

    protected virtual void Awake()
    {
        ResolveStageControllerIfNeeded();
        AutoAssignSlotNumber();
        AutoAssignStoneSlotsParent();
    }

    protected virtual void Start()
    {
        ResolveStageControllerIfNeeded();
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
            var matchingSlot = FindSlotByNumber(stoneNumber);
            SnapStoneToSlot(stone, matchingSlot);

            if (matchingSlot != null)
                Debug.Log($"[{LogPrefix}] Stone_{stoneNumber}을 Slot_{stoneNumber} 위치에 배치");
            else
                Debug.LogWarning($"[{LogPrefix}] Slot_{stoneNumber}을 찾을 수 없습니다.");
        }
        else
        {
            if (stoneNumber != slotNumber)
            {
                Debug.LogWarning($"[{LogPrefix}] 번호 불일치: Stone_{stoneNumber}을 Slot_{slotNumber}에 드롭할 수 없습니다.");
                return;
            }

            SnapStoneToSlot(stone, this);
        }

        CountStonesAndReport();
    }

    private void AutoAssignSlotNumber()
    {
        if (slotNumber != 0)
            return;

        slotNumber = ExtractNumberFromName(gameObject.name);
    }

    private void AutoAssignStoneSlotsParent()
    {
        if (stoneSlotsParent != null)
            return;

        Transform parent = transform.parent;
        if (parent != null && parent.name.Contains("StoneSlots"))
            stoneSlotsParent = parent;
    }

    private void ResolveStageControllerIfNeeded()
    {
        if (stageController == null)
            stageController = ResolveStageController();
    }

    protected virtual TStageController ResolveStageController()
    {
        var parentController = GetComponentInParent<TStageController>();
        if (parentController != null)
            return parentController;

        var controllers = FindObjectsOfType<TStageController>(true);
        if (controllers == null || controllers.Length == 0)
            return null;

        if (controllers.Length == 1)
            return controllers[0];

        var candidates = GetCandidateTransforms();

        var boardMatch = FindControllerWhoseBoardContains(controllers, candidates);
        if (boardMatch != null)
            return boardMatch;

        var closest = FindClosestControllerByBoard(controllers, candidates);
        if (closest != null)
            return closest;

        return controllers[0];
    }

    private IReadOnlyList<Transform> GetCandidateTransforms()
    {
        var result = new List<Transform>(3);
        if (transform != null)
            result.Add(transform);
        if (slotParent != null && !result.Contains(slotParent))
            result.Add(slotParent);
        if (stoneSlotsParent != null && !result.Contains(stoneSlotsParent))
            result.Add(stoneSlotsParent);
        return result;
    }

    private StoneDropZoneBase<TStageController> FindSlotByNumber(int number)
    {
        if (number <= 0)
            return null;

        var candidates = GetSearchableSlots();
        if (candidates == null || candidates.Length == 0)
            return null;

        foreach (var slot in candidates)
        {
            if (slot == null || slot == this)
                continue;

            int slotNum = slot.slotNumber != 0
                ? slot.slotNumber
                : ExtractNumberFromName(slot.gameObject.name);

            if (slotNum == number)
                return slot;
        }

        return null;
    }

    private StoneDropZoneBase<TStageController>[] GetSearchableSlots()
    {
        Transform searchRoot = stoneSlotsParent;

        if (searchRoot == null && slotParent != null)
            searchRoot = slotParent;

        if (searchRoot == null)
            searchRoot = GetBoardTransform(stageController);

        if (searchRoot != null)
            return searchRoot.GetComponentsInChildren<StoneDropZoneBase<TStageController>>(true);

        return FindObjectsOfType<StoneDropZoneBase<TStageController>>(true);
    }

    private void SnapStoneToSlot(GameObject stone, StoneDropZoneBase<TStageController> targetSlot)
    {
        if (stone == null)
            return;

        Transform container = slotParent != null ? slotParent : transform;
        if (container == null)
        {
            Debug.LogWarning($"[{LogPrefix}] 유효한 부모 컨테이너를 찾지 못했습니다.");
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

    private void CountStonesAndReport()
    {
        Transform targetParent = slotParent != null ? slotParent : transform;

        if (targetParent == null)
        {
            Debug.LogWarning($"[{LogPrefix}] slotParent(CountDisplay)가 설정되지 않았습니다.");
            return;
        }

        int stoneCount = CountStoneDraggablesRecursive(targetParent);

        if (TryReportStoneCount(stoneCount))
        {
            Debug.Log($"[{LogPrefix}] 드롭된 Stone 개수: {stoneCount} (CountDisplay: {targetParent.name})");
        }
        else
        {
            Debug.LogWarning($"[{LogPrefix}] Stage Controller를 찾을 수 없습니다.");
        }
    }

    private int CountStoneDraggablesRecursive(Transform root)
    {
        int count = 0;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);

            if (child.GetComponent<StoneDraggable>() != null &&
                child.GetComponent<StoneDropZoneBase<TStageController>>() == null)
            {
                count++;
            }

            if (child.childCount > 0)
                count += CountStoneDraggablesRecursive(child);
        }

        return count;
    }

    private bool TryReportStoneCount(int stoneCount)
    {
        var controller = stageController ?? ResolveStageController();
        if (controller == null)
            return false;

        stageController = controller;
        ReportStoneCountToStage(controller, stoneCount);
        return true;
    }

    private TStageController FindControllerWhoseBoardContains(TStageController[] controllers, IReadOnlyList<Transform> targets)
    {
        foreach (var controller in controllers)
        {
            var board = GetBoardTransform(controller);
            if (board == null)
                continue;

            foreach (var target in targets)
            {
                if (target != null && target.IsChildOf(board))
                    return controller;
            }
        }

        return null;
    }

    private TStageController FindClosestControllerByBoard(TStageController[] controllers, IReadOnlyList<Transform> targets)
    {
        TStageController closest = null;
        float bestDistance = float.MaxValue;

        foreach (var controller in controllers)
        {
            var board = GetBoardTransform(controller);
            if (board == null)
                continue;

            foreach (var target in targets)
            {
                if (target == null)
                    continue;

                float distance = (board.position - target.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closest = controller;
                }
            }
        }

        return closest;
    }

    private Transform GetBoardTransform(TStageController controller)
    {
        return controller == null ? null : GetStoneBoardTransform(controller);
    }

    protected abstract void ReportStoneCountToStage(TStageController controller, int count);
    protected abstract Transform GetStoneBoardTransform(TStageController controller);

    private int ExtractNumberFromName(string name)
    {
        Match match = Regex.Match(name, @"_(\d+)");
        if (match.Success && match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var number))
            return number;
        return 0;
    }

    public void GetSharedConfiguration(out Transform slotParentRef, out int slotNumberRef, out Transform stoneSlotsParentRef)
    {
        slotParentRef = slotParent;
        slotNumberRef = slotNumber;
        stoneSlotsParentRef = stoneSlotsParent;
    }

    public void SetSharedConfiguration(Transform slotParentRef, int slotNumberRef, Transform stoneSlotsParentRef)
    {
        slotParent = slotParentRef;
        slotNumber = slotNumberRef;
        stoneSlotsParent = stoneSlotsParentRef;
    }

    public void SetStageControllerReference(TStageController controller)
    {
        stageController = controller;
    }
}

public class StoneDropZone : StoneDropZoneBase<Stage20Controller>
{
    protected override string LogPrefix => "Stage20DropZone";

    protected override void ReportStoneCountToStage(Stage20Controller controller, int count)
    {
        controller?.ReportStoneCount(count);
    }

    protected override Transform GetStoneBoardTransform(Stage20Controller controller)
    {
        return controller != null && controller.stoneBoard != null
            ? controller.stoneBoard.transform
            : null;
    }
}
