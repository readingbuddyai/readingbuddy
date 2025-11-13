using UnityEngine;

public class Stage30StoneDropZone : StoneDropZoneBase<Stage30Controller>
{
    protected override string LogPrefix => "Stage30DropZone";

    protected override void ReportStoneCountToStage(Stage30Controller controller, int count)
    {
        controller?.ReportStoneCount(count);
    }

    protected override Transform GetStoneBoardTransform(Stage30Controller controller)
    {
        return controller != null && controller.stoneBoard != null
            ? controller.stoneBoard.transform
            : null;
    }
}

