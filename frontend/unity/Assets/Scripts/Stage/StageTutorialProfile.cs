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

    [Header("Tutorial Controls")]
    public bool requireTriggerAfterTutorial = true;
    [Range(0.05f, 1f)] public float tutorialTriggerThreshold = 0.6f;
    public KeyCode tutorialFallbackKey = KeyCode.Space;
    [Min(0f)] public float tutorialClipGapSeconds = 0.9f;

    [Header("Intro Clips")]
    public AudioClip introClip1;
    public AudioClip introClip2;
    public AudioClip introClip3;
    public AudioClip introClip4;
    public AudioClip introClip5;
    public AudioClip introClip6;
    public AudioClip introClip7;
    public AudioClip introClip8;
    public AudioClip introClip9;
    public AudioClip introClip10;
    public AudioClip introClip11;
    public AudioClip introDemoClip1;
    public AudioClip introDemoClip2;
}

[Serializable]
public class StageTutorialOption
{
    public string label;
    public bool isCorrect;
}

