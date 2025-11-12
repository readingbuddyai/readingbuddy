using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 패널(UI RectTransform)을 가운데에서 시작해 오디오 재생 후 더 빠르게
/// 오른쪽 하단으로 이동시키는 연출 스크립트.
/// Screen Space / World Space Canvas 모두 지원.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MoveToBottomRightWithAudio : MonoBehaviour
{
    [Header("Target UI")]
    public RectTransform target; // null이면 자신의 RectTransform 사용

    [Header("(옵션) 오디오 재생")] 
    public AudioSource audioSource; // null이면 GetComponent로 시도
    public AudioClip clip;          // null이면 audioSource.clip 사용

    [Header("이동 타이밍")]
    [Tooltip("오디오를 먼저 10초(기본) 듣고 난 뒤 이동할지 여부")]
    public bool holdUntilAfterAudio = true;
    [Tooltip("오디오 대기 시간(초). 10초 동안 듣고 난 이후 이동")]
    public float audioWaitSeconds = 10f;

    [Tooltip("(대기 후) 빠른 이동에 사용할 시간(초)")]
    public float fastMoveDuration = 3f;
    public AnimationCurve fastEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("대기 없이 곧바로 이동 시(레거시)")]
    public float duration = 10f; // 기존 동작 호환용
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("기타 설정")]
    public bool stopAudioOnComplete = false;
    [Tooltip("도착 지점을 직접 지정하려면 여기에 RectTransform 할당. 비우면 '오른쪽 하단' 자동 계산")]
    public RectTransform destinationMarker;

    private Vector2 _startPos;
    private Vector2 _endPos;
    private bool _running;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (!target) target = GetComponent<RectTransform>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _startPos = target.anchoredPosition; // 보통 중앙에서 시작
        _endPos = destinationMarker ? destinationMarker.anchoredPosition : ComputeBottomRightAnchoredPosition(target);

        if (!_running)
            StartCoroutine(CoRun());
    }

    private IEnumerator CoRun()
    {
        _running = true;

        // 오디오 재생 시작(있으면)
        if (audioSource)
        {
            if (clip) audioSource.clip = clip;
            if (audioSource.clip) audioSource.Play();
        }

        if (holdUntilAfterAudio)
        {
            // 10초(기본) 동안 들어준 뒤 빠르게 이동
            yield return new WaitForSeconds(audioWaitSeconds);
            yield return MoveRoutine(fastMoveDuration, fastEase);
        }
        else
        {
            // 기존 방식: 바로 이동 시작(오디오와 동시)
            yield return MoveRoutine(duration, ease);
        }

        if (stopAudioOnComplete && audioSource && audioSource.isPlaying)
            audioSource.Stop();

        _running = false;
    }

    private IEnumerator MoveRoutine(float moveDuration, AnimationCurve curve)
    {
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / moveDuration);
            float k = (curve != null) ? curve.Evaluate(u) : u;
            target.anchoredPosition = Vector2.LerpUnclamped(_startPos, _endPos, k);
            yield return null;
        }
        target.anchoredPosition = _endPos;
    }

    // 부모 Rect 기준 오른쪽 하단의 anchoredPosition 계산(앵커가 중앙(0.5,0.5)일 때 적합)
    private static Vector2 ComputeBottomRightAnchoredPosition(RectTransform rt)
    {
        var parent = rt.parent as RectTransform;
        if (!parent)
            return rt.anchoredPosition;

        Vector2 parentHalf = parent.rect.size * 0.5f;
        Vector2 selfHalf = rt.rect.size * 0.5f;
        return new Vector2(parentHalf.x - selfHalf.x, -(parentHalf.y - selfHalf.y));
    }
}
