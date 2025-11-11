using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stage/Tutorial Profile", fileName = "StageTutorialProfile")]
public class StageTutorialProfile : ScriptableObject
{
    [Header("Intro Tutorial")]
    public Sprite introTutorialImage;
    public List<StageTutorialOption> introOptions = new List<StageTutorialOption>();

    [Header("Guide Settings")]
    [Min(0f)] public float guideHideLeadSeconds = 0.5f;
    public bool showGuideWhenPanelOff = true;
    [Min(0f)] public float guideShowDelayAfterPanelOff = 0f;

    [Header("Input Settings")]
    [Range(0.05f, 1f)] public float tutorialTriggerThreshold = 0.6f;
    public KeyCode tutorialFallbackKey = KeyCode.Space;

    [Header("Timing")]
    [Min(0f)] public float defaultClipGapSeconds = 0.9f;

    [Header("Tutorial Steps")]
    public List<StageTutorialStep> steps = new List<StageTutorialStep>();
}

[Serializable]
public class StageTutorialOption
{
    public string label;
    public bool isCorrect;
}

[Serializable]
public class StageTutorialStep
{
    public string name;
    public StageTutorialStepPhase phase = StageTutorialStepPhase.Tutorial;
    public StageTutorialActionType action = StageTutorialActionType.PlayClip;
    public AudioClip audioClip;
    public Sprite image;
    public float waitSeconds = 0f;
    public bool panelImmediate = false;
    public bool boolValue;
    public StageTutorialCursorTarget cursorTarget = StageTutorialCursorTarget.None;
    public float cursorMoveSeconds = -1f;
    public AnimationCurve cursorMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool enablePulse = false;
    public float pulseScale = 1.1f;
    public float pulseDuration = 0.35f;
    public int pulseLoops = 1;
    public string progressText;
    public string customActionId;
    public bool awaitRelease = true;
}

public enum StageTutorialStepPhase
{
    Intro,
    Tutorial
}

public enum StageTutorialActionType
{
    PlayClip,
    WaitSeconds,
    ShowPanel,
    HidePanel,
    SetMainImage,
    ClearMainImage,
    ShowOptions,
    HideOptions,
    SetCursorActive,
    MoveCursor,
    PulseOption,
    AwaitTrigger,
    SetProgressText,
    CustomAction
}

public enum StageTutorialCursorTarget
{
    None,
    WrongOption,
    CorrectOption
}
