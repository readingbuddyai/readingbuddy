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
        yield return PlayClipSafe(introClip1);
        yield return PlayClipSafe(introClip2);
        yield return PlayClipSafe(introClip3);
    }

    public IEnumerator RunIntroTutorial()
    {
        EnsureInitialized();
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
            SetupIntroOptions();

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
            yield return WaitForRightTriggerPress();
        }

        yield return ShowPanel(false);
        LogVerbose("[StageTutorial] Tutorial panel ON (after trigger)");

        SetProgressText(string.Empty);
        LogVerbose("[StageTutorial] Tutorial end");
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

    private IEnumerator WaitForRightTriggerPress()
    {
        bool wasPressed = CheckRightTriggerPressed();
        if (wasPressed)
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

        while (CheckRightTriggerPressed())
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

    private void SetupIntroOptions()
    {
        if (_deps.OptionsContainer == null)
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
            btn.interactable = false;

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
        if (_deps.OptionButtonPrefab == null)
        {
            _deps.OptionButtonPrefab = Resources.Load<Button>("UI/OptionButton");
        }
    }

    private void ClearIntroOptionButtons()
    {
        if (_deps.OptionsContainer == null)
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
    public Action<string> Log;
    public Action<string> LogWarning;
    public bool VerboseLogging;
}

