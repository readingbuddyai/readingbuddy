using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using QuestionDto = StageQuestionModels.QuestionDto;

[Serializable]
public class RemedialPracticeResource
{
    [Tooltip("stage/complete 응답의 voiceResult 항목과 매칭할 값 (예: phonemeId 혹은 'ㅏ')")]
    public string key;
    [Tooltip("보충 학습에 사용할 이미지(없으면 기존 문제 이미지 활용)")]
    public Sprite image;
    [Tooltip("보충 학습에 사용할 로컬 오디오 클립(없으면 remote URL 또는 기존 문제 음성 사용)")]
    public AudioClip localAudioClip;
    [Tooltip("보충 학습에 사용할 원격 오디오 URL(있을 경우 우선 사용)")]
    public string remoteAudioUrl;
}

public class StageSupplementController
{
    public List<RemedialPracticeResource> remedialResources = new List<RemedialPracticeResource>();
    public AudioClip clipRemedialNeedPractice;
    public AudioClip clipRemedialPracticeIntro;
    public AudioClip clipRemedialFirstEncourage;
    public AudioClip clipRemedialSecondEncourage;
    public AudioClip clipRemedialPerfect;
    public AudioClip clipRemedialNextLesson;
    public float remedialEncouragePauseSeconds = 3f;

    private StageSupplementDependencies _deps;
    private readonly List<string> _remedialPhonemes = new List<string>();
    private readonly HashSet<string> _remedialPhonemeSet = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlyList<string> RemedialPhonemes => _remedialPhonemes;

    public void Initialize(StageSupplementDependencies deps)
    {
        if (deps == null) throw new ArgumentNullException(nameof(deps));
        _deps = deps;
    }

    public void Clear()
    {
        _remedialPhonemes.Clear();
        _remedialPhonemeSet.Clear();
    }

    public void SetRemedialTokens(IEnumerable<string> tokens)
    {
        Clear();
        if (tokens == null) return;
        foreach (var token in tokens)
            AddRemedialKeyFromToken(token);
    }

    public void AddRemedialKeyFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        token = token.Trim();
        if (int.TryParse(token, out int questionIndex))
        {
            if (AddRemedialKeyFromQuestionIndex(questionIndex))
                return;
        }

        AddRemedialKeyInternal(token);
    }

    public IEnumerator RunRemedialSequence()
    {
        EnsureInitialized();
        bool hasRemedial = _remedialPhonemes.Count > 0;
        if (!hasRemedial)
        {
            if (clipRemedialPerfect)
                yield return _deps.PlayClip(clipRemedialPerfect);
            if (clipRemedialNextLesson)
                yield return _deps.PlayClip(clipRemedialNextLesson);
            yield break;
        }

        if (clipRemedialNeedPractice && _deps.PlayClip != null)
            yield return _deps.PlayClip(clipRemedialNeedPractice);

        if (clipRemedialPracticeIntro && _deps.PlayClip != null)
            yield return _deps.PlayClip(clipRemedialPracticeIntro);

        for (int i = 0; i < _remedialPhonemes.Count; i++)
        {
            string phonemeKey = _remedialPhonemes[i];
            var resource = ResolveRemedialResource(phonemeKey, out QuestionDto fallbackQuestion);
            yield return ShowRemedialPractice(phonemeKey, resource, fallbackQuestion);

            AudioClip encourageClip = null;
            if (i == 0 && clipRemedialFirstEncourage)
                encourageClip = clipRemedialFirstEncourage;
            else if (clipRemedialSecondEncourage)
                encourageClip = clipRemedialSecondEncourage;

            if (encourageClip && _deps.PlayClip != null)
            {
                yield return _deps.PlayClip(encourageClip);
                if (remedialEncouragePauseSeconds > 0f)
                    yield return new WaitForSeconds(remedialEncouragePauseSeconds);
            }
        }

        if (clipRemedialNextLesson && _deps.PlayClip != null)
            yield return _deps.PlayClip(clipRemedialNextLesson);

        if (_deps.MainImage != null)
        {
            _deps.MainImage.enabled = false;
            _deps.MainImage.sprite = null;
        }
    }

    private bool AddRemedialKeyFromQuestionIndex(int questionIndex)
    {
        if (questionIndex <= 0)
            return false;

        if (_deps.QuestionController == null || _deps.QuestionController.Count == 0)
        {
            _deps.LogWarning?.Invoke($"[StageSupplement] voiceResult 인덱스 {questionIndex} 처리 실패: 저장된 문제가 없습니다.");
            return false;
        }

        var question = _deps.QuestionController.GetQuestionByNumber(questionIndex);
        if (question == null)
        {
            _deps.LogWarning?.Invoke($"[StageSupplement] voiceResult 인덱스 {questionIndex} 처리 실패: 범위를 벗어났습니다(총 {_deps.QuestionController.Count}문항).");
            return false;
        }

        AddRemedialKeysForQuestion(question);
        return true;
    }

    private void AddRemedialKeysForQuestion(QuestionDto question)
    {
        if (question == null)
            return;

        AddRemedialKeyInternal(question.value);
        AddRemedialKeyInternal(question.unicode);

        if (question.phonemeId != 0)
            AddRemedialKeyInternal(question.phonemeId.ToString());

        if (question.id != 0)
            AddRemedialKeyInternal(question.id.ToString());

        if (question.questionId != 0)
            AddRemedialKeyInternal(question.questionId.ToString());
    }

    private void AddRemedialKeyInternal(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        string normalized = NormalizeRemedialKey(key);
        if (string.IsNullOrEmpty(normalized))
            return;

        if (_remedialPhonemeSet.Add(normalized))
            _remedialPhonemes.Add(normalized);
    }

    private RemedialPracticeResource ResolveRemedialResource(string key, out QuestionDto matchedQuestion)
    {
        matchedQuestion = null;
        string normalizedKey = NormalizeRemedialKey(key);
        if (string.IsNullOrEmpty(normalizedKey))
            return null;

        RemedialPracticeResource foundResource = null;
        if (remedialResources != null)
        {
            for (int i = 0; i < remedialResources.Count; i++)
            {
                var res = remedialResources[i];
                if (res == null || string.IsNullOrWhiteSpace(res.key)) continue;
                if (string.Equals(NormalizeRemedialKey(res.key), normalizedKey, StringComparison.OrdinalIgnoreCase))
                {
                    foundResource = res;
                    break;
                }
            }
        }

        var questionList = _deps.QuestionController?.Questions;
        if (questionList != null)
        {
            for (int i = 0; i < questionList.Count; i++)
            {
                var q = questionList[i];
                if (q == null) continue;

                if (!string.IsNullOrEmpty(q.value) && string.Equals(NormalizeRemedialKey(q.value), normalizedKey, StringComparison.OrdinalIgnoreCase))
                {
                    matchedQuestion = q;
                    break;
                }
                if (!string.IsNullOrEmpty(q.unicode) && string.Equals(NormalizeRemedialKey(q.unicode), normalizedKey, StringComparison.OrdinalIgnoreCase))
                {
                    matchedQuestion = q;
                    break;
                }
                if (q.phonemeId != 0 && string.Equals(q.phonemeId.ToString(), key, StringComparison.OrdinalIgnoreCase))
                {
                    matchedQuestion = q;
                    break;
                }
                if (q.options != null)
                {
                    for (int oi = 0; oi < q.options.Count; oi++)
                    {
                        var opt = q.options[oi];
                        if (opt == null) continue;
                        if (!string.IsNullOrEmpty(opt.value) && string.Equals(NormalizeRemedialKey(opt.value), normalizedKey, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedQuestion = q;
                            break;
                        }
                        if (!string.IsNullOrEmpty(opt.unicode) && string.Equals(NormalizeRemedialKey(opt.unicode), normalizedKey, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedQuestion = q;
                            break;
                        }
                        if (opt.id != 0 && string.Equals(opt.id.ToString(), key, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedQuestion = q;
                            break;
                        }
                    }
                }
                if (matchedQuestion != null)
                    break;
            }
        }

        return foundResource;
    }

    private IEnumerator ShowRemedialPractice(string key, RemedialPracticeResource resource, QuestionDto fallbackQuestion)
    {
        EnsureInitialized();
        if (_deps.VerboseLogging)
            _deps.Log?.Invoke($"[StageSupplement] Remedial practice start (key={key})");

        if (_deps.ProgressText)
            _deps.ProgressText.text = string.Empty;

        if (_deps.MainImage)
        {
            if (resource != null && resource.image != null)
            {
                _deps.MainImage.sprite = resource.image;
                _deps.MainImage.preserveAspect = true;
                _deps.MainImage.enabled = true;
            }
            else if (fallbackQuestion != null && !string.IsNullOrEmpty(fallbackQuestion.imageUrl))
            {
                yield return ExecuteCoroutine(_deps.LoadAndShowImage?.Invoke(fallbackQuestion.imageUrl));
            }
            else
            {
                _deps.MainImage.enabled = false;
                _deps.MainImage.sprite = null;
            }
        }

        bool audioPlayed = false;
        if (resource != null)
        {
            if (!string.IsNullOrEmpty(resource.remoteAudioUrl))
            {
                if (_deps.PlayVoiceUrl != null)
                {
                    yield return ExecuteCoroutine(_deps.PlayVoiceUrl.Invoke(resource.remoteAudioUrl));
                    audioPlayed = true;
                }
            }
            else if (resource.localAudioClip)
            {
                if (_deps.PlayClip != null)
                {
                    yield return _deps.PlayClip(resource.localAudioClip);
                    audioPlayed = true;
                }
            }
        }

        if (!audioPlayed && fallbackQuestion != null && !string.IsNullOrEmpty(fallbackQuestion.voiceUrl))
        {
            if (_deps.PlayVoiceUrl != null)
            {
                yield return ExecuteCoroutine(_deps.PlayVoiceUrl.Invoke(fallbackQuestion.voiceUrl));
                audioPlayed = true;
            }
        }

        if (!audioPlayed)
            yield return null;
    }

    private string NormalizeRemedialKey(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        string s = source.Trim();
        try
        {
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\\u([0-9A-Fa-f]{4})", m =>
            {
                int code = Convert.ToInt32(m.Groups[1].Value, 16);
                return char.ConvertFromUtf32(code);
            });
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)U\+([0-9A-Fa-f]{4,6})", m =>
            {
                int code = Convert.ToInt32(m.Groups[1].Value, 16);
                return char.ConvertFromUtf32(code);
            });
        }
        catch { }

        try { s = s.Normalize(NormalizationForm.FormKC); } catch { }
        return s;
    }

    private IEnumerator ExecuteCoroutine(IEnumerator routine)
    {
        if (routine == null)
            yield break;
        yield return routine;
    }

    private void EnsureInitialized()
    {
        if (_deps == null)
            throw new InvalidOperationException("StageSupplementController is not initialized. Call Initialize() first.");
    }
}

public class StageSupplementDependencies
{
    public StageQuestionController<QuestionDto> QuestionController;
    public Image MainImage;
    public Text ProgressText;
    public Func<AudioClip, IEnumerator> PlayClip;
    public Func<string, IEnumerator> PlayVoiceUrl;
    public Func<string, IEnumerator> LoadAndShowImage;
    public Action<string> Log;
    public Action<string> LogWarning;
    public bool VerboseLogging;
}

