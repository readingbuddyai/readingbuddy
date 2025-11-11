using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Stage.UI;

public partial class Stage41Controller
{
    [Header("Tutorial (Optional)")]
    public StageTutorialProfile tutorialProfile;
    public Sprite introTutorialImage;
    public List<StageTutorialController.IntroOption> introOptions = new List<StageTutorialController.IntroOption>();
    public StageTutorialController.IntroOptionCursor introOptionCursor;
    public PanelAnimator introTutorialPanelAnimator;
    public GameObject introTutorialPanel;
    [Tooltip("튜토리얼 이후 사용자의 입력을 기다릴지 여부")] public bool requireTriggerAfterTutorial = false;
    [Range(0.05f, 1f)] public float tutorialTriggerThreshold = 0.6f;
    public KeyCode tutorialFallbackKey = KeyCode.Space;
    [Min(0f)] public float tutorialClipGapSeconds = 0.9f;

    private StageSessionController _sessionController;
    private StageTutorialController _tutorialController;
    private StageTutorialDependencies _tutorialDependencies;
    private StageAudioController _audioController;
    private StageAudioDependencies _audioDependencies;
    private readonly StageQuestionController<QuestionDto> _questionController = new StageQuestionController<QuestionDto>();

    private void ConfigureStageModules()
    {
        ConfigureSessionController();
        ConfigureAudioController();
        ConfigureTutorialController();
    }

    private StageSessionController ConfigureSessionController()
    {
        if (_sessionController == null)
            _sessionController = new StageSessionController();

        _sessionController.Configure(baseUrl, authToken);
        if (logVerbose)
            _sessionController.Log = message => Debug.Log(message);
        else
            _sessionController.Log = null;
        _sessionController.LogWarning = message => Debug.LogWarning(message);
        _sessionController.LogError = message => Debug.LogError(message);
        return _sessionController;
    }

    private void ConfigureAudioController()
    {
        if (_audioController == null)
            _audioController = new StageAudioController();
        if (_audioDependencies == null)
            _audioDependencies = new StageAudioDependencies();

        _audioDependencies.AudioSource = audioSource;
        _audioDependencies.Log = message => { if (logVerbose) Debug.Log(message); };
        _audioDependencies.LogWarning = message => Debug.LogWarning(message);
        _audioController.Initialize(_audioDependencies);
    }

    private void ConfigureTutorialController()
    {
        if (_tutorialController == null)
            _tutorialController = new StageTutorialController();
        if (_tutorialDependencies == null)
            _tutorialDependencies = new StageTutorialDependencies();

        _tutorialDependencies.PlayClip = clip => PlayClip(clip);
        _tutorialDependencies.StartCoroutine = routine => StartCoroutine(routine);
        _tutorialDependencies.StopCoroutine = routine =>
        {
            if (routine != null)
                StopCoroutine(routine);
        };
        _tutorialDependencies.ProgressText = progressText;
        _tutorialDependencies.EnsureProgressText = null;
        _tutorialDependencies.MainImage = guideImage;
        _tutorialDependencies.OptionsContainer = choicesContainer != null ? choicesContainer.GetComponent<RectTransform>() : null;
        _tutorialDependencies.ChoicesRoot = choicesContainer;
        _tutorialDependencies.ChoicesContainer = choicesContainer != null ? choicesContainer.GetComponent<RectTransform>() : null;
        _tutorialDependencies.ManageOptionsContainerContents = false;
        _tutorialDependencies.ManageChoicesVisibility = true;
        _tutorialDependencies.ManageSlotsVisibility = false;
        _tutorialDependencies.CorrectSfx = clipGreat;
        _tutorialDependencies.MoveCursorSmooth = null;
        _tutorialDependencies.PulseOption = (rect, scale, duration, loops) => PulseOption(rect, scale, duration, loops);
        _tutorialDependencies.ChoseongSlot = choseongBox != null ? choseongBox.GetComponent<RectTransform>() : null;
        _tutorialDependencies.JungseongSlot = jungseongBox != null ? jungseongBox.GetComponent<RectTransform>() : null;
        _tutorialDependencies.JongsungSlot = jongseongBox != null ? jongseongBox.GetComponent<RectTransform>() : null;
        _tutorialDependencies.ResolveChoiceTile = ResolveTutorialChoiceTile;
        _tutorialDependencies.ResolveSlotTarget = ResolveTutorialSlotTarget;
        _tutorialDependencies.ToggleChoices = show => Co_TutorialToggleChoices(show);
        _tutorialDependencies.ToggleSlots = show => Co_TutorialToggleSlots(show);
        _tutorialDependencies.PulseSlot = (slotTarget, scale, duration, loops) => Co_TutorialPulseSlot(slotTarget, scale, duration, loops);
        _tutorialDependencies.AnimateChoiceDrag = (key, slotTarget, seconds, curve, keepInSlot) => Co_TutorialAnimateChoiceDrag(key, slotTarget, seconds, curve, keepInSlot);
        _tutorialDependencies.ChoiceDragRoot = choicesContainer != null ? choicesContainer.transform : null;
        _tutorialDependencies.Log = message => { if (logVerbose) Debug.Log(message); };
        _tutorialDependencies.LogWarning = message => Debug.LogWarning(message);
        _tutorialDependencies.VerboseLogging = logVerbose;

        if (tutorialProfile != null)
        {
            _tutorialController.ApplyProfile(tutorialProfile);
        }
        else
        {
            _tutorialController.introTutorialImage = introTutorialImage;
            _tutorialController.introOptions = introOptions != null ? introOptions : new List<StageTutorialController.IntroOption>();
            _tutorialController.requireTriggerAfterTutorial = requireTriggerAfterTutorial;
            _tutorialController.tutorialTriggerThreshold = tutorialTriggerThreshold;
            _tutorialController.tutorialFallbackKey = tutorialFallbackKey;
            _tutorialController.tutorialClipGapSeconds = tutorialClipGapSeconds;
        }

        _tutorialController.introOptionCursor = introOptionCursor;
        _tutorialController.introTutorialPanelAnimator = introTutorialPanelAnimator;
        _tutorialController.introTutorialPanel = introTutorialPanel;
        _tutorialController.Initialize(_tutorialDependencies);
    }

    private System.Collections.IEnumerator Co_TutorialToggleChoices(bool show)
    {
        if (choicesContainer != null)
        {
            choicesContainer.SetActive(show);
            if (show)
            {
                var rt = choicesContainer.GetComponent<RectTransform>();
                if (rt)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
            }
        }
        yield break;
    }

    private System.Collections.IEnumerator Co_TutorialToggleSlots(bool show)
    {
        if (choseongBox) choseongBox.SetActive(show);
        if (jungseongBox) jungseongBox.SetActive(show);
        if (jongseongBox) jongseongBox.SetActive(show);
        yield break;
    }

    private System.Collections.IEnumerator Co_TutorialPulseSlot(StageTutorialSlotTarget target, float scale, float duration, int loops)
    {
        var rect = ResolveTutorialSlotTarget(target);
        if (rect == null)
            yield break;
        yield return PulseOption(rect, scale, duration, loops);
    }

    private System.Collections.IEnumerator Co_TutorialAnimateChoiceDrag(string key, StageTutorialSlotTarget target, float seconds, AnimationCurve curve, bool keepInSlot)
    {
        var tile = ResolveTutorialChoiceTile(key);
        var slot = ResolveTutorialSlotTarget(target);
        if (tile == null || slot == null)
            yield break;
        yield return Co_AnimateTutorialChoice(tile, slot, seconds, curve, keepInSlot);
    }

    private RectTransform ResolveTutorialSlotTarget(StageTutorialSlotTarget target)
    {
        switch (target)
        {
            case StageTutorialSlotTarget.Choseong:
                return choseongBox != null ? choseongBox.GetComponent<RectTransform>() : null;
            case StageTutorialSlotTarget.Jungseong:
                return jungseongBox != null ? jungseongBox.GetComponent<RectTransform>() : null;
            case StageTutorialSlotTarget.Jongsung:
                return jongseongBox != null ? jongseongBox.GetComponent<RectTransform>() : null;
            default:
                return null;
        }
    }

    private RectTransform ResolveTutorialChoiceTile(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || choicesContainer == null)
            return null;

        RectTransform SearchIn(GameObject container)
        {
            if (container == null) return null;
            foreach (Transform child in container.transform)
            {
                if (child == null) continue;
                if (string.Equals(child.name, key, StringComparison.OrdinalIgnoreCase))
                    return child.GetComponent<RectTransform>();

                var text = child.GetComponentInChildren<TMP_Text>();
                if (text != null && string.Equals(text.text, key, StringComparison.OrdinalIgnoreCase))
                    return child.GetComponent<RectTransform>();
            }
            return null;
        }

        var hit = SearchIn(choicesContainer);
        if (hit != null) return hit;
        hit = SearchIn(consonantChoicesContainer);
        if (hit != null) return hit;
        hit = SearchIn(vowelChoicesContainer);
        return hit;
    }

    private System.Collections.IEnumerator Co_AnimateTutorialChoice(RectTransform tile, RectTransform slot, float seconds, AnimationCurve curve, bool keepInSlot)
    {
        if (tile == null || slot == null)
            yield break;

        Transform originalParent = tile.parent;
        int originalIndex = tile.GetSiblingIndex();
        Vector3 startPos = tile.position;
        Vector3 endPos = slot.position;
        float duration = Mathf.Max(0.01f, seconds);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : t;
            tile.position = Vector3.Lerp(startPos, endPos, eased);
            yield return null;
        }

        tile.position = endPos;

        if (!keepInSlot)
        {
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = curve != null ? curve.Evaluate(t) : t;
                tile.position = Vector3.Lerp(endPos, startPos, eased);
                yield return null;
            }
            tile.position = startPos;
            tile.SetParent(originalParent, false);
            tile.SetSiblingIndex(originalIndex);
        }
        else
        {
            tile.SetParent(slot, false);
            tile.anchoredPosition = Vector2.zero;
        }
    }

    private System.Collections.IEnumerator PulseOption(RectTransform target, float scale, float duration, int loops)
    {
        if (target == null)
            yield break;
        loops = Mathf.Max(1, loops);
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * scale;
        float singleDuration = Mathf.Max(0.01f, duration);
        for (int i = 0; i < loops; i++)
        {
            float elapsed = 0f;
            while (elapsed < singleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / singleDuration);
                float eased = Mathf.Sin(t * Mathf.PI);
                target.localScale = Vector3.Lerp(originalScale, targetScale, eased);
                yield return null;
            }
        }
        target.localScale = originalScale;
    }

    private IEnumerator FetchQuestionsWithSession(Action<List<QuestionDto>> onDone)
    {
        var session = ConfigureSessionController();
        StageSessionController.QuestionSetResult result = null;
        yield return session.FetchQuestionSet(stageSet, count, stageSessionId, r => result = r);

        if (result == null || !result.Success)
        {
            if (logVerbose && result != null)
                Debug.LogWarning($"[Stage41] set 실패: code={result.ResponseCode} body={result.RawBody}");
            onDone?.Invoke(null);
            yield break;
        }

        List<QuestionDto> list = null;
        try
        {
            if (logVerbose) Debug.Log($"[Stage41] set 응답: {result.RawBody}");
            var parsed = JsonUtility.FromJson<QuestionListResponse>(result.RawBody ?? string.Empty);
            if (parsed != null && parsed.data != null && parsed.data.problems != null)
                list = parsed.data.problems;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Stage41] set 응답 파싱 실패: {e.Message}");
        }

        onDone?.Invoke(list);
    }

    private IEnumerator StartStageSessionWithSession()
    {
        var session = ConfigureSessionController();
        StageSessionController.StageStartResult result = null;
        yield return session.StartStageSession(stageTwoPart, count, r => result = r);
        if (result != null && result.Success && !string.IsNullOrWhiteSpace(result.StageSessionId))
        {
            stageSessionId = result.StageSessionId;
        }
        else if (result != null && logVerbose)
        {
            Debug.LogWarning($"[Stage41] stage/start 실패 또는 stageSessionId 미수신: code={result.ResponseCode} body={result.RawBody}");
        }
    }

    private IEnumerator CompleteStageSessionWithSession()
    {
        if (string.IsNullOrWhiteSpace(stageSessionId)) yield break;
        var session = ConfigureSessionController();
        StageSessionController.StageCompleteResult result = null;
        yield return session.CompleteStageSession(stageSessionId, r => result = r);
        if (result != null && logVerbose)
        {
            Debug.Log($"[Stage41] stage/complete 응답: code={result.ResponseCode} body={result.RawBody}");
        }
    }

    private IEnumerator UploadVoiceSegmentWithSession(int segmentIndex, byte[] wavData, string expectedAnswer, Action<bool, string> onDone)
    {
        var session = ConfigureSessionController();
        StageSessionController.VoiceCheckResult result = null;
        yield return session.CheckVoice(stageSessionId, stageTwoPart, _currentProblemNumber, expectedAnswer, wavData, r => result = r);

        if (result == null || !result.Success)
        {
            if (logVerbose && result != null)
                Debug.LogWarning($"[Stage41] check/voice 실패: code={result.ResponseCode} body={result.RawBody}");
            onDone?.Invoke(false, string.Empty);
            yield break;
        }

        if (logVerbose) Debug.Log($"[Stage41] check/voice 응답: {result.RawBody}");
        try
        {
            var parsed = JsonUtility.FromJson<VoiceReplyResp>(result.RawBody ?? string.Empty);
            string reply = parsed?.data?.reply ?? string.Empty;
            bool ok = parsed?.data?.isReplyCorrect ?? false;

            string correctPhoneme = GetTargetPhonemeAnswer(segmentIndex);
            while (_segmentReplies.Count <= segmentIndex) _segmentReplies.Add(string.Empty);
            while (_segmentCorrects.Count <= segmentIndex) _segmentCorrects.Add(false);
            _segmentReplies[segmentIndex] = correctPhoneme;
            _segmentCorrects[segmentIndex] = ok;

            if (ok)
            {
                string normalized = NormalizePhoneme(correctPhoneme);
                SetSlotText(segmentIndex, normalized);
                SetSlotAlpha(segmentIndex, 1f);
                _finalizedSlots[segmentIndex] = true;
            }

            if (logVerbose) Debug.Log($"[Stage41] segment {segmentIndex} → reply='{reply}', correct={ok}");
            onDone?.Invoke(ok, correctPhoneme);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Stage41] check/voice 응답 파싱 실패: {e.Message}");
            onDone?.Invoke(false, string.Empty);
        }
    }
}
