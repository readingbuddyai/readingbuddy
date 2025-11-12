using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Stage.UI;
using UnityEngine.Networking;

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
    private StageSupplementController _supplementController;
    private StageSupplementDependencies _supplementDependencies;
    private StageSessionController.StageCompleteResult _lastCompleteResult;
    private readonly StageQuestionController<QuestionDto> _questionController = new StageQuestionController<QuestionDto>();
    private readonly StageQuestionController<StageQuestionModels.QuestionDto> _supplementQuestionController = new StageQuestionController<StageQuestionModels.QuestionDto>();

    private void ConfigureStageModules()
    {
        ConfigureSessionController();
        ConfigureAudioController();
        ConfigureTutorialController();
        ConfigureSupplementController();
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
        _tutorialDependencies.ToggleChoices = (show, slotTarget) => Co_TutorialToggleChoices(show, slotTarget);
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
        _tutorialController.guide3DCharacter = guide3DCharacter;
        _tutorialController.Initialize(_tutorialDependencies);
    }

    private void ConfigureSupplementController()
    {
        if (_supplementController == null)
            _supplementController = new StageSupplementController();
        if (_supplementDependencies == null)
            _supplementDependencies = new StageSupplementDependencies();

        _supplementDependencies.QuestionController = _supplementQuestionController;
        _supplementDependencies.MainImage = remedialImage;
        _supplementDependencies.ProgressText = progressText;
        _supplementDependencies.PlayClip = clip => PlayClip(clip);
        _supplementDependencies.PlayVoiceUrl = url => PlayVoiceUrl(url);
        _supplementDependencies.LoadAndShowImage = url => LoadSupplementImage(url);
        _supplementDependencies.Log = message => { if (logVerbose) Debug.Log(message); };
        _supplementDependencies.LogWarning = message => Debug.LogWarning(message);
        _supplementDependencies.VerboseLogging = logVerbose;

        _supplementController.Initialize(_supplementDependencies);
        _supplementController.remedialResources = remedialResources ?? new List<RemedialPracticeResource>();
        _supplementController.clipRemedialNeedPractice = clipRemedialNeedPractice;
        _supplementController.clipRemedialPracticeIntro = clipRemedialPracticeIntro;
        _supplementController.clipRemedialFirstEncourage = clipRemedialFirstEncourage;
        _supplementController.clipRemedialSecondEncourage = clipRemedialSecondEncourage;
        _supplementController.clipRemedialPerfect = clipRemedialPerfect;
        _supplementController.clipRemedialNextLesson = clipRemedialNextLesson;
        _supplementController.remedialEncouragePauseSeconds = remedialEncouragePauseSeconds;
    }

    private System.Collections.IEnumerator Co_TutorialToggleChoices(bool show, StageTutorialSlotTarget slotTarget)
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

        bool showConsonants = false;
        bool showVowels = false;

        if (show)
        {
            if (slotTarget == StageTutorialSlotTarget.Choseong ||
                slotTarget == StageTutorialSlotTarget.Jongsung ||
                slotTarget == StageTutorialSlotTarget.Jongseong)
            {
                showConsonants = true;
            }
            else if (slotTarget == StageTutorialSlotTarget.Jungseong)
            {
                showVowels = true;
            }
            else
            {
                showConsonants = true;
                showVowels = true;
            }
        }

        if (consonantChoicesContainer != null)
        {
            consonantChoicesContainer.SetActive(showConsonants);
            if (showConsonants)
            {
                var rt = consonantChoicesContainer.GetComponent<RectTransform>();
                if (rt)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
            }
        }

        if (vowelChoicesContainer != null)
        {
            vowelChoicesContainer.SetActive(showVowels);
            if (showVowels)
            {
                var rt = vowelChoicesContainer.GetComponent<RectTransform>();
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
        var box = ResolveTutorialSlotObject(target);
        if (box == null)
            yield break;

        var cg = box.GetComponent<CanvasGroup>() ?? box.AddComponent<CanvasGroup>();
        float minAlpha = blinkAlphaMin;
        float maxAlpha = blinkAlphaMax;
        float period = blinkPeriod;
        loops = Mathf.Max(1, loops);
        float singleDuration = duration > 0f ? duration : blinkPeriod;

        float originalAlpha = cg.alpha;
        for (int i = 0; i < loops; i++)
        {
            float t = 0f;
            while (t < singleDuration)
            {
                t += Time.deltaTime;
                float phase = Mathf.Sin((t / Mathf.Max(0.01f, period)) * Mathf.PI * 2f) * 0.5f + 0.5f;
                cg.alpha = Mathf.Lerp(minAlpha, maxAlpha, phase);
                yield return null;
            }
        }
        cg.alpha = originalAlpha;
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

    private GameObject ResolveTutorialSlotObject(StageTutorialSlotTarget target)
    {
        switch (target)
        {
            case StageTutorialSlotTarget.Choseong:
                return choseongBox;
            case StageTutorialSlotTarget.Jungseong:
                return jungseongBox;
            case StageTutorialSlotTarget.Jongsung:
                return jongseongBox;
            default:
                return null;
        }
    }

    private RectTransform ResolveTutorialChoiceTile(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string trimmedKey = key.Trim();
        string normalizedKey = NormalizeChoiceKey(trimmedKey);
        var rootStops = new HashSet<Transform>();
        if (choicesContainer != null) rootStops.Add(choicesContainer.transform);
        if (consonantChoicesContainer != null) rootStops.Add(consonantChoicesContainer.transform);
        if (vowelChoicesContainer != null) rootStops.Add(vowelChoicesContainer.transform);

        RectTransform SearchIn(GameObject container)
        {
            if (container == null)
                return null;
            return FindChoiceTileRecursive(container.transform, trimmedKey, normalizedKey, rootStops);
        }

        var hit = SearchIn(choicesContainer);
        if (hit != null) return hit;
        hit = SearchIn(consonantChoicesContainer);
        if (hit != null) return hit;
        hit = SearchIn(vowelChoicesContainer);
        return hit;
    }

    private RectTransform FindChoiceTileRecursive(Transform parent, string trimmedKey, string normalizedKey, HashSet<Transform> rootStops)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child == null)
                continue;

            var rect = TryMatchChoiceTransform(child, trimmedKey, normalizedKey, rootStops);
            if (rect != null)
                return rect;

            var nested = FindChoiceTileRecursive(child, trimmedKey, normalizedKey, rootStops);
            if (nested != null)
                return nested;
        }
        return null;
    }

    private RectTransform TryMatchChoiceTransform(Transform candidate, string trimmedKey, string normalizedKey, HashSet<Transform> rootStops)
    {
        if (candidate == null)
            return null;

        if (MatchesChoiceKey(candidate.name, trimmedKey, normalizedKey))
        {
            var rect = candidate as RectTransform;
            return rect != null ? rect : candidate.GetComponent<RectTransform>();
        }

        var text = candidate.GetComponent<TMP_Text>();
        if (text != null && MatchesChoiceKey(text.text, trimmedKey, normalizedKey))
        {
            var owner = candidate;
            while (owner != null && !rootStops.Contains(owner))
            {
                if (MatchesChoiceKey(owner.name, trimmedKey, normalizedKey))
                {
                    var ownerRect = owner as RectTransform;
                    return ownerRect != null ? ownerRect : owner.GetComponent<RectTransform>();
                }
                owner = owner.parent;
            }

            var textRect = candidate as RectTransform;
            return textRect != null ? textRect : candidate.GetComponent<RectTransform>();
        }

        return null;
    }

    private bool MatchesChoiceKey(string candidateValue, string originalKey, string normalizedKey)
    {
        if (string.IsNullOrWhiteSpace(candidateValue))
            return false;

        string trimmedCandidate = candidateValue.Trim();
        if (string.Equals(trimmedCandidate, originalKey, StringComparison.OrdinalIgnoreCase))
            return true;

        string normalizedCandidate = NormalizeChoiceKey(trimmedCandidate);
        if (!string.IsNullOrEmpty(normalizedCandidate) && !string.IsNullOrEmpty(normalizedKey))
        {
            if (string.Equals(normalizedCandidate, normalizedKey, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (!string.IsNullOrEmpty(normalizedKey))
        {
            if (string.Equals(trimmedCandidate, $"Tile_{normalizedKey}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private string NormalizeChoiceKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string result = value.Trim();
        if (result.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
            result = result.Substring(0, result.Length - "(Clone)".Length).TrimEnd();

        if (result.StartsWith("Tile_", StringComparison.OrdinalIgnoreCase))
            result = result.Substring("Tile_".Length);

        return result.Trim();
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
        _lastCompleteResult = null;
        yield return session.CompleteStageSession(stageSessionId, r => result = r);
        if (result != null)
        {
            if (!string.IsNullOrWhiteSpace(result.StageSessionId))
                stageSessionId = result.StageSessionId;
            if (result.VoiceResultTokens.Count > 0)
            {
                ConfigureSupplementController();
                _supplementController?.SetRemedialTokens(result.VoiceResultTokens);
            }
            else
            {
                _supplementController?.Clear();
            }
            _lastCompleteResult = result;
        }
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

    private IEnumerator LoadSupplementImage(string imageUrl)
    {
        if (remedialImage == null || string.IsNullOrWhiteSpace(imageUrl))
            yield break;

        remedialImage.enabled = false;
        remedialImage.sprite = null;

        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                if (logVerbose)
                    Debug.LogWarning($"[Stage41] 보충 이미지 로드 실패: {req.error} (code={req.responseCode}) URL={imageUrl}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
                yield break;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            remedialImage.sprite = sprite;
            remedialImage.preserveAspect = true;
            remedialImage.enabled = true;
        }
    }

    private void UpdateSupplementQuestions(IEnumerable<QuestionDto> source)
    {
        var list = new List<StageQuestionModels.QuestionDto>();
        if (source != null)
        {
            foreach (var q in source)
            {
                if (q == null) continue;
                list.Add(new StageQuestionModels.QuestionDto
                {
                    questionId = q.questionId,
                    problemWord = q.problemWord,
                    value = q.problemWord,
                    unicode = q.problemWord,
                    voiceUrl = q.voiceUrl,
                    imageUrl = q.imageUrl,
                    options = null,
                    id = 0,
                    phonemeId = 0
                });
            }
        }
        _supplementQuestionController.SetQuestions(list);
    }
}
