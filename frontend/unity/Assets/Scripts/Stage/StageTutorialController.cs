using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR;
using Stage.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

[Serializable]
public class StageTutorialController
{
    [Header("Intro Tutorial Assets")]
    public Sprite introTutorialImage;
    public List<IntroOption> introOptions = new List<IntroOption>();
    public IntroOptionCursor introOptionCursor;
    public PanelAnimator introTutorialPanelAnimator;
    public GameObject introTutorialPanel;

    [Header("Guide Character")]
    public GameObject guide3DCharacter;
    [Min(0f)] public float guideHideLeadSeconds = 0.5f;
    public bool showGuideWhenPanelOff = true;
    [Min(0f)] public float guideShowDelayAfterPanelOff = 0f;

    [Header("Tutorial Settings")]
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

    private StageTutorialDependencies _deps;
    private Coroutine _pendingShowPanel;
#if ENABLE_INPUT_SYSTEM
    private AxisControl _rightTriggerAxis;
    private ButtonControl _rightTriggerButton;
#endif
    private readonly List<UnityEngine.XR.InputDevice> _rightHandDevices = new List<UnityEngine.XR.InputDevice>();
    private Coroutine _guideShowCoroutine;
    private readonly List<StageTutorialStep> _profileSteps = new List<StageTutorialStep>();
    private float _defaultClipGapSeconds = 0.9f;

    public void ApplyProfile(StageTutorialProfile profile)
    {
        if (profile == null)
            return;

        introTutorialImage = profile.introTutorialImage;

        introOptions = new List<IntroOption>();
        if (profile.introOptions != null)
        {
            for (int i = 0; i < profile.introOptions.Count; i++)
            {
                var option = profile.introOptions[i];
                if (option == null)
                    continue;
                introOptions.Add(new IntroOption
                {
                    label = option.label,
                    isCorrect = option.isCorrect
                });
            }
        }

        guideHideLeadSeconds = Mathf.Max(0f, profile.guideHideLeadSeconds);
        showGuideWhenPanelOff = profile.showGuideWhenPanelOff;
        guideShowDelayAfterPanelOff = Mathf.Max(0f, profile.guideShowDelayAfterPanelOff);

        tutorialTriggerThreshold = Mathf.Clamp01(profile.tutorialTriggerThreshold);
        tutorialFallbackKey = profile.tutorialFallbackKey;

        _defaultClipGapSeconds = Mathf.Max(0f, profile.defaultClipGapSeconds);
        tutorialClipGapSeconds = _defaultClipGapSeconds;

        requireTriggerAfterTutorial = false;

        _profileSteps.Clear();
        if (profile.steps != null)
        {
            for (int i = 0; i < profile.steps.Count; i++)
            {
                var step = profile.steps[i];
                if (step != null)
                    _profileSteps.Add(step);
            }
        }
    }

    public void Initialize(StageTutorialDependencies deps)
    {
        if (deps == null) throw new ArgumentNullException(nameof(deps));
        _deps = deps;
    }

    public void PrepareForStageStart()
    {
        EnsureInitialized();
        HidePanel(true);
        StopPendingPanelCoroutine();
        if (guide3DCharacter)
            guide3DCharacter.SetActive(true);
    }

    public void ResetAfterStageRestart()
    {
        EnsureInitialized();
        HidePanel(true);
        StopPendingPanelCoroutine();
        ClearIntroOptionButtons();
    }

    public IEnumerator RunIntroSequence()
    {
        EnsureInitialized();
        if (HasProfileSteps(StageTutorialStepPhase.Intro))
        {
            yield return RunSteps(StageTutorialStepPhase.Intro);
            yield break;
        }

        yield return PlayClipSafe(introClip1);
        yield return PlayClipSafe(introClip2);
        yield return PlayClipSafe(introClip3);
    }

    public IEnumerator RunIntroTutorial()
    {
        EnsureInitialized();

        if (HasProfileSteps(StageTutorialStepPhase.Tutorial))
        {
            yield return RunSteps(StageTutorialStepPhase.Tutorial);
            yield break;
        }

        bool usedImage = false;

        SetProgressText(string.Empty);
        yield return ShowPanel(false);
        LogVerbose("[StageTutorial] Tutorial panel ON (1.1.2.1)");

        if (introTutorialImage != null && _deps.MainImage != null)
        {
            _deps.MainImage.sprite = introTutorialImage;
            _deps.MainImage.enabled = true;
            usedImage = true;
        }

        yield return PlayIntroClip(introClip4, "[StageTutorial] Play clip 1.1.2.2");
        yield return PlayIntroClip(introClip5, "[StageTutorial] Play clip 1.1.2.3");
        yield return PlayDemoClip(introDemoClip1, "[StageTutorial] Play clip 1.1.2.4 (demo)");
        yield return PlayIntroClip(introClip6, "[StageTutorial] Play clip 1.1.2.5");
        yield return PlayDemoClip(introDemoClip2, "[StageTutorial] Play clip 1.1.2.6 (demo)");
        yield return PlayIntroClip(introClip7, "[StageTutorial] Play clip 1.1.2.7");

        if (_deps.OptionsContainer != null)
        {
            _deps.OptionsContainer.gameObject.SetActive(true);
            SetupIntroOptions(false);

            if (introOptionCursor != null && introOptionCursor.handCursor != null)
            {
                var cursorGo = introOptionCursor.handCursor;
                cursorGo.SetActive(true);

                if (introOptionCursor.wrongOptionTransform != null)
                {
                    LogVerbose("[StageTutorial] Cursor moving to wrong option");
                    yield return ExecuteCoroutine(_deps.MoveCursorSmooth?.Invoke(
                        cursorGo.transform,
                        introOptionCursor.wrongOptionTransform,
                        introOptionCursor.cursorMoveSeconds,
                        introOptionCursor.cursorMoveCurve));
                    if (introOptionCursor.wrongHoverSeconds > 0f)
                        yield return new WaitForSeconds(introOptionCursor.wrongHoverSeconds);
                }

                if (introOptionCursor.correctOptionTransform != null)
                {
                    LogVerbose("[StageTutorial] Cursor moving to correct option");
                    yield return ExecuteCoroutine(_deps.MoveCursorSmooth?.Invoke(
                        cursorGo.transform,
                        introOptionCursor.correctOptionTransform,
                        introOptionCursor.cursorMoveSeconds,
                        introOptionCursor.cursorMoveCurve));

                    if (introOptionCursor.enableCorrectPulse)
                    {
                        yield return ExecuteCoroutine(_deps.PulseOption?.Invoke(
                            introOptionCursor.correctOptionTransform,
                            introOptionCursor.correctPulseScale,
                            introOptionCursor.correctPulseDuration,
                            introOptionCursor.correctPulseLoops));
                    }

                    if (introOptionCursor.correctHoverSeconds > 0f)
                        yield return new WaitForSeconds(introOptionCursor.correctHoverSeconds);
                }

                cursorGo.SetActive(false);
            }

            LogVerbose("[StageTutorial] Play correct SFX");
            yield return PlayClipSafe(_deps.CorrectSfx);

            HidePanel();
            LogVerbose("[StageTutorial] Tutorial panel OFF after correct SFX (1.1.2.7)");

            _deps.OptionsContainer.gameObject.SetActive(false);
            ClearIntroOptionButtons();
        }

        if (usedImage && _deps.MainImage != null)
        {
            _deps.MainImage.enabled = false;
            _deps.MainImage.sprite = null;
        }

        yield return PlayIntroClip(introClip8, "[StageTutorial] Play clip 1.1.2.8");
        yield return PlayIntroClip(introClip9, "[StageTutorial] Play clip 1.1.2.9");
        yield return PlayIntroClip(introClip10, "[StageTutorial] Play clip 1.1.2.10");
        yield return PlayIntroClip(introClip11, "[StageTutorial] Play clip 1.1.2.11");

        if (requireTriggerAfterTutorial)
        {
            LogVerbose("[StageTutorial] Waiting for right trigger input to continue");
            yield return WaitForRightTriggerPress(true);
        }

        yield return ShowPanel(false);
        LogVerbose("[StageTutorial] Tutorial panel ON (after trigger)");

        SetProgressText(string.Empty);
        LogVerbose("[StageTutorial] Tutorial end");
    }

    private bool HasProfileSteps(StageTutorialStepPhase phase)
    {
        if (_profileSteps == null || _profileSteps.Count == 0)
            return false;

        for (int i = 0; i < _profileSteps.Count; i++)
        {
            var step = _profileSteps[i];
            if (step != null && step.phase == phase)
                return true;
        }

        return false;
    }

    private IEnumerator RunSteps(StageTutorialStepPhase phase)
    {
        if (_profileSteps == null || _profileSteps.Count == 0)
            yield break;

        for (int i = 0; i < _profileSteps.Count; i++)
        {
            var step = _profileSteps[i];
            if (step == null || step.phase != phase)
                continue;

            yield return ExecuteStep(step);
        }
    }

    private IEnumerator ExecuteStep(StageTutorialStep step)
    {
        switch (step.action)
        {
            case StageTutorialActionType.PlayClip:
                yield return ExecutePlayClip(step);
                break;
            case StageTutorialActionType.WaitSeconds:
                if (step.waitSeconds > 0f)
                    yield return new WaitForSeconds(step.waitSeconds);
                break;
            case StageTutorialActionType.ShowPanel:
                yield return ShowPanel(step.panelImmediate);
                break;
            case StageTutorialActionType.HidePanel:
                HidePanel(step.panelImmediate);
                break;
            case StageTutorialActionType.SetMainImage:
                ExecuteSetMainImage(step.image);
                break;
            case StageTutorialActionType.ClearMainImage:
                ClearMainImage();
                break;
            case StageTutorialActionType.ShowOptions:
                yield return ShowIntroOptions(step.boolValue);
                break;
            case StageTutorialActionType.HideOptions:
                HideIntroOptions();
                break;
            case StageTutorialActionType.SetCursorActive:
                SetCursorActive(step.boolValue);
                break;
            case StageTutorialActionType.MoveCursor:
                yield return MoveCursorTo(step);
                break;
            case StageTutorialActionType.PulseOption:
                yield return PulseOptionAt(step);
                break;
            case StageTutorialActionType.AwaitTrigger:
                yield return WaitForRightTriggerPress(step.awaitRelease);
                break;
            case StageTutorialActionType.SetProgressText:
                SetProgressText(step.progressText ?? string.Empty);
                break;
            case StageTutorialActionType.CustomAction:
                yield return ExecuteCustomAction(step.customActionId);
                break;
            default:
                LogWarning($"[StageTutorial] Unknown tutorial action: {step.action}");
                break;
        }

        if (step.action != StageTutorialActionType.PlayClip &&
            step.action != StageTutorialActionType.WaitSeconds &&
            step.waitSeconds > 0f)
            yield return new WaitForSeconds(step.waitSeconds);
    }

    private IEnumerator ExecutePlayClip(StageTutorialStep step)
    {
        if (step.audioClip == null)
            yield break;

        yield return PlayClipSafe(step.audioClip);

        float gap = step.waitSeconds > 0f ? step.waitSeconds : _defaultClipGapSeconds;
        if (gap > 0f)
            yield return new WaitForSeconds(gap);
    }

    private void ExecuteSetMainImage(Sprite imageOverride)
    {
        if (_deps.MainImage == null)
            return;

        var sprite = imageOverride != null ? imageOverride : introTutorialImage;
        _deps.MainImage.sprite = sprite;
        _deps.MainImage.enabled = sprite != null;

        if (sprite != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_deps.MainImage.rectTransform);
            _deps.MainImage.gameObject.SetActive(true);
        }
        else if (_deps.MainImage != null)
        {
            _deps.MainImage.gameObject.SetActive(false);
        }
    }

    private void ClearMainImage()
    {
        if (_deps.MainImage == null)
            return;

        _deps.MainImage.sprite = null;
        _deps.MainImage.enabled = false;
        _deps.MainImage.gameObject.SetActive(false);
    }

    private IEnumerator ShowIntroOptions(bool interactable)
    {
        if (_deps.OptionsContainer == null)
            yield break;

        _deps.OptionsContainer.gameObject.SetActive(true);
        _deps.OptionsContainer.SetAsLastSibling();
        if (_deps.ManageOptionsContainerContents)
        {
            SetupIntroOptions(interactable);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_deps.OptionsContainer);
            yield return null;
        }
    }

    private void HideIntroOptions()
    {
        if (_deps.OptionsContainer == null)
            return;
        if (_deps.ManageOptionsContainerContents)
            ClearIntroOptionButtons();
        _deps.OptionsContainer.gameObject.SetActive(false);
    }

    private void SetCursorActive(bool active)
    {
        if (introOptionCursor?.handCursor != null)
            introOptionCursor.handCursor.SetActive(active);
    }

    private IEnumerator MoveCursorTo(StageTutorialStep step)
    {
        if (introOptionCursor == null || introOptionCursor.handCursor == null)
            yield break;
        var target = ResolveCursorTarget(step.cursorTarget);
        if (target == null)
            yield break;

        introOptionCursor.handCursor.SetActive(true);

        float seconds = step.cursorMoveSeconds > 0f ? step.cursorMoveSeconds : introOptionCursor.cursorMoveSeconds;
        var curve = step.cursorMoveCurve ?? introOptionCursor.cursorMoveCurve;
        yield return ExecuteCoroutine(_deps.MoveCursorSmooth?.Invoke(
            introOptionCursor.handCursor.transform,
            target,
            seconds,
            curve));

        float hover = step.cursorTarget == StageTutorialCursorTarget.CorrectOption
            ? introOptionCursor.correctHoverSeconds
            : introOptionCursor.wrongHoverSeconds;
        if (hover > 0f)
            yield return new WaitForSeconds(hover);
    }

    private IEnumerator PulseOptionAt(StageTutorialStep step)
    {
        if (_deps.PulseOption == null)
            yield break;

        var target = ResolveCursorTarget(step.cursorTarget);
        if (target == null)
            yield break;

        bool shouldPulse = step.enablePulse || (introOptionCursor?.enableCorrectPulse ?? false);
        if (!shouldPulse)
            yield break;

        float scale = step.enablePulse ? step.pulseScale : introOptionCursor?.correctPulseScale ?? 1.1f;
        float duration = step.enablePulse ? step.pulseDuration : introOptionCursor?.correctPulseDuration ?? 0.35f;
        int loops = step.enablePulse ? step.pulseLoops : introOptionCursor?.correctPulseLoops ?? 1;

        yield return ExecuteCoroutine(_deps.PulseOption?.Invoke(
            target,
            scale,
            duration,
            loops));
    }

    private RectTransform ResolveCursorTarget(StageTutorialCursorTarget target)
    {
        switch (target)
        {
            case StageTutorialCursorTarget.CorrectOption:
                return introOptionCursor?.correctOptionTransform;
            case StageTutorialCursorTarget.WrongOption:
                return introOptionCursor?.wrongOptionTransform;
            case StageTutorialCursorTarget.DoneButton:
                return introOptionCursor?.doneButtonTransform;
            default:
                return null;
        }
    }

    private IEnumerator ExecuteCustomAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId) || _deps.ExecuteCustomStep == null)
            yield break;

        yield return ExecuteCoroutine(_deps.ExecuteCustomStep(actionId));
    }

    public IEnumerator ShowPanel(bool immediate)
    {
        EnsureInitialized();
        StopPendingPanelCoroutine();

        if (guide3DCharacter && guideHideLeadSeconds > 0f && !immediate)
        {
            if (_deps.StartCoroutine != null)
            {
                var routine = Co_ShowPanelWithGuideHide(immediate);
                _pendingShowPanel = _deps.StartCoroutine(routine);
                yield break;
            }

            yield return Co_ShowPanelWithGuideHide(immediate);
            yield break;
        }

        ApplyShowPanel(immediate);
        yield break;
    }

    public void HidePanel(bool immediate = false)
    {
        EnsureInitialized();
        StopPendingPanelCoroutine();

        if (introTutorialPanelAnimator != null)
        {
            LogVerbose($"[StageTutorial] HidePanel via PanelAnimator (immediate={immediate})");
            introTutorialPanelAnimator.Hide(immediate);
        }
        else if (introTutorialPanel != null)
        {
            introTutorialPanel.SetActive(false);
            LogVerbose($"[StageTutorial] HidePanel via SetActive (immediate={immediate})");
        }

        if (guide3DCharacter && showGuideWhenPanelOff)
        {
            if (_guideShowCoroutine != null && _deps.StopCoroutine != null)
                _deps.StopCoroutine(_guideShowCoroutine);

            if (guideShowDelayAfterPanelOff > 0f && _deps.StartCoroutine != null)
            {
                _guideShowCoroutine = _deps.StartCoroutine(Co_ShowGuideAfterDelay(guideShowDelayAfterPanelOff));
            }
            else
            {
                guide3DCharacter.SetActive(true);
            }
        }
    }

    private IEnumerator Co_ShowPanelWithGuideHide(bool immediate)
    {
        if (guide3DCharacter)
            guide3DCharacter.SetActive(false);

        yield return new WaitForSeconds(guideHideLeadSeconds);
        ApplyShowPanel(immediate);
        _pendingShowPanel = null;
    }

    private IEnumerator Co_ShowGuideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (guide3DCharacter)
            guide3DCharacter.SetActive(true);
        _guideShowCoroutine = null;
    }

    private void ApplyShowPanel(bool immediate)
    {
        if (guide3DCharacter)
            guide3DCharacter.SetActive(false);

        if (introTutorialPanelAnimator != null)
        {
            LogVerbose($"[StageTutorial] ShowPanel via PanelAnimator (immediate={immediate})");
            introTutorialPanelAnimator.Show(immediate);
        }
        else if (introTutorialPanel != null)
        {
            introTutorialPanel.SetActive(true);
            LogVerbose($"[StageTutorial] ShowPanel via SetActive (immediate={immediate})");
        }
        else
        {
            LogWarning("[StageTutorial] ShowPanel called but no panel assigned");
        }
    }

    private IEnumerator PlayIntroClip(AudioClip clip, string logLabel)
    {
        if (clip == null)
            yield break;

        LogVerbose(logLabel);
        yield return PlayClipSafe(clip);
        if (tutorialClipGapSeconds > 0f)
            yield return new WaitForSeconds(tutorialClipGapSeconds);
    }

    private IEnumerator PlayDemoClip(AudioClip clip, string logLabel)
    {
        if (clip == null)
            yield break;

        LogVerbose(logLabel);
        yield return PlayClipSafe(clip);
        if (tutorialClipGapSeconds > 0f)
            yield return new WaitForSeconds(tutorialClipGapSeconds);
    }

    private IEnumerator PlayClipSafe(AudioClip clip)
    {
        if (clip == null || _deps.PlayClip == null)
            yield break;

        yield return _deps.PlayClip(clip);
    }

    private IEnumerator WaitForRightTriggerPress(bool awaitRelease)
    {
        bool wasPressed = CheckRightTriggerPressed();
        if (awaitRelease && wasPressed)
        {
            LogVerbose("[StageTutorial] Waiting for trigger release before monitoring press");
            while (CheckRightTriggerPressed())
                yield return null;
            wasPressed = false;
        }

        while (true)
        {
            bool pressed = CheckRightTriggerPressed();
            if (pressed && !wasPressed)
            {
                LogVerbose("[StageTutorial] Right trigger detected");
                break;
            }
            wasPressed = pressed;
            yield return null;
        }

        while (awaitRelease && CheckRightTriggerPressed())
            yield return null;
    }

    private bool CheckRightTriggerPressed()
    {
        float threshold = Mathf.Clamp01(tutorialTriggerThreshold);

#if ENABLE_INPUT_SYSTEM
        ResolveRightTriggerControls();
        if (_rightTriggerAxis != null && _rightTriggerAxis.ReadValue() >= threshold)
            return true;
        if (_rightTriggerButton != null && _rightTriggerButton.isPressed)
            return true;
#endif

        _rightHandDevices.Clear();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, _rightHandDevices);
        for (int i = 0; i < _rightHandDevices.Count; i++)
        {
            var device = _rightHandDevices[i];
            if (!device.isValid) continue;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerButton) && triggerButton)
                return true;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue) && triggerValue >= threshold)
                return true;
        }

        if (tutorialFallbackKey != KeyCode.None && Input.GetKey(tutorialFallbackKey))
            return true;

        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private void ResolveRightTriggerControls()
    {
        if (_rightTriggerAxis == null || _rightTriggerAxis.device == null || !_rightTriggerAxis.device.added)
            _rightTriggerAxis = InputSystem.FindControl("<XRController>{RightHand}/trigger") as AxisControl;
        if (_rightTriggerButton == null || _rightTriggerButton.device == null || !_rightTriggerButton.device.added)
            _rightTriggerButton = InputSystem.FindControl("<XRController>{RightHand}/triggerPressed") as ButtonControl;
    }
#endif

    private void SetupIntroOptions(bool interactable)
    {
        if (_deps.OptionsContainer == null)
            return;
        if (!_deps.ManageOptionsContainerContents)
            return;

        EnsureOptionPrefab();
        ClearIntroOptionButtons();

        RectTransform firstWrong = null;
        RectTransform firstCorrect = null;

        foreach (var option in introOptions)
        {
            if (_deps.OptionButtonPrefab == null)
                break;

            var btn = UnityEngine.Object.Instantiate(_deps.OptionButtonPrefab, _deps.OptionsContainer);
            btn.interactable = interactable;

            var tmpText = btn.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
                tmpText.text = option.label;
            else
            {
                var uguiText = btn.GetComponentInChildren<Text>();
                if (uguiText != null)
                    uguiText.text = option.label;
            }

            var rect = btn.GetComponent<RectTransform>();
            if (option.isCorrect)
                firstCorrect = rect;
            else if (firstWrong == null)
                firstWrong = rect;
        }

        if (introOptionCursor != null)
        {
            if (introOptionCursor.correctOptionTransform == null)
                introOptionCursor.correctOptionTransform = firstCorrect;
            if (introOptionCursor.wrongOptionTransform == null)
                introOptionCursor.wrongOptionTransform = firstWrong;
        }
    }

    private void EnsureOptionPrefab()
    {
        if (!_deps.ManageOptionsContainerContents)
            return;
        if (_deps.OptionButtonPrefab == null)
        {
            _deps.OptionButtonPrefab = Resources.Load<Button>("UI/OptionButton");
        }
    }

    private void ClearIntroOptionButtons()
    {
        if (_deps.OptionsContainer == null)
            return;
        if (!_deps.ManageOptionsContainerContents)
            return;

        foreach (Transform child in _deps.OptionsContainer)
            UnityEngine.Object.Destroy(child.gameObject);
    }

    private IEnumerator ExecuteCoroutine(IEnumerator enumerator)
    {
        if (enumerator == null)
            yield break;

        yield return enumerator;
    }

    private void SetProgressText(string value)
    {
        if (_deps.ProgressText != null)
            _deps.ProgressText.text = value ?? string.Empty;

        if (_deps.EnsureProgressText != null)
        {
            var txt = _deps.EnsureProgressText();
            if (txt != null)
                txt.text = value ?? string.Empty;
        }
    }

    private void StopPendingPanelCoroutine()
    {
        if (_pendingShowPanel != null && _deps.StopCoroutine != null)
        {
            _deps.StopCoroutine(_pendingShowPanel);
            _pendingShowPanel = null;
        }
    }

    private void LogVerbose(string message)
    {
        if (!_deps.VerboseLogging || string.IsNullOrEmpty(message))
            return;
        _deps.Log?.Invoke(message);
    }

    private void LogWarning(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        _deps.LogWarning?.Invoke(message);
    }

    private void EnsureInitialized()
    {
        if (_deps == null)
            throw new InvalidOperationException("StageTutorialController is not initialized. Call Initialize() first.");
    }

    [Serializable]
    public class IntroOption
    {
        public string label;
        public bool isCorrect;
    }

    [Serializable]
    public class IntroOptionCursor
    {
        public GameObject handCursor;
        public RectTransform wrongOptionTransform;
        public RectTransform correctOptionTransform;
        public RectTransform doneButtonTransform;
        public float wrongHoverSeconds = 1f;
        public float correctHoverSeconds = 1f;
        public float cursorMoveSeconds = 0.35f;
        public AnimationCurve cursorMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Correct Pulse")]
        public bool enableCorrectPulse = true;
        public float correctPulseScale = 1.1f;
        public float correctPulseDuration = 0.35f;
        public int correctPulseLoops = 1;
    }
}

public class StageTutorialDependencies
{
    public Func<AudioClip, IEnumerator> PlayClip;
    public Func<IEnumerator, Coroutine> StartCoroutine;
    public Action<Coroutine> StopCoroutine;
    public Text ProgressText;
    public Func<Text> EnsureProgressText;
    public Image MainImage;
    public RectTransform OptionsContainer;
    public Button OptionButtonPrefab;
    public AudioClip CorrectSfx;
    public Func<Transform, RectTransform, float, AnimationCurve, IEnumerator> MoveCursorSmooth;
    public Func<RectTransform, float, float, int, IEnumerator> PulseOption;
    public Func<string, IEnumerator> ExecuteCustomStep;
    public Action<string> Log;
    public Action<string> LogWarning;
    public bool VerboseLogging;
    public bool ManageOptionsContainerContents = true;
}

