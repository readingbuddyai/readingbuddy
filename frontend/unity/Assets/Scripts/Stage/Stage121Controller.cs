using UnityEngine;
using QuestionDto = StageQuestionModels.QuestionDto;
using OptionDto = StageQuestionModels.OptionDto;

/// <summary>
/// Stage 2.1 진행 컨트롤러.
/// Stage11Controller의 구조를 그대로 활용하면서 단계별 기본값과
/// API 스테이지 파라미터(2.1)를 덮어쓴다.
/// </summary>
public class Stage121Controller : Stage11Controller
{
    private const string DefaultStage = "1.2.1";
    private const string DefaultStageTwoPart = "2.1";

    protected new void Reset()
    {
        ApplyStageDefaults(force: true);
    }

    private void Awake()
    {
        ApplyStageDefaults(force: false);
    }

    private void OnValidate()
    {
        ApplyStageDefaults(force: false);
    }

    private void ApplyStageDefaults(bool force)
    {
        if (force || string.IsNullOrWhiteSpace(stage) || stage == "1.1.1")
            stage = DefaultStage;

        if (force || string.IsNullOrWhiteSpace(stageTwoPart) || stageTwoPart == "1.1")
            stageTwoPart = DefaultStageTwoPart;

        if (count <= 0)
            count = 5;
    }

    protected override string GetStageForVoiceUpload(QuestionDto q)
    {
        if (!string.IsNullOrWhiteSpace(stageTwoPart))
            return stageTwoPart;
        return base.GetStageForVoiceUpload(q);
    }

    protected override string GetStageForAttempt(QuestionDto q, OptionDto selectedOption)
    {
        if (!string.IsNullOrWhiteSpace(stageTwoPart))
            return stageTwoPart;
        return base.GetStageForAttempt(q, selectedOption);
    }
}

