using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CastLoopController : MonoBehaviour
{
    [SerializeField] private string castTrigger = "CastLoop";
    [SerializeField] private float castDuration = 2f;

    private Animator _animator;
    private Coroutine _castRoutine;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayCastLoop()
    {
        if (_castRoutine != null)
            StopCoroutine(_castRoutine);
        _castRoutine = StartCoroutine(CastRoutine());
    }

    private IEnumerator CastRoutine()
    {
        _animator.SetTrigger(castTrigger);
        yield return new WaitForSeconds(castDuration);
        _castRoutine = null;
    }
    private void Start()
    {
        StartCoroutine(DelayedCastLoop());
    }

    private IEnumerator DelayedCastLoop()
    {
        yield return new WaitForSeconds(5f);  // 씬 시작 후 5초 대기
        PlayCastLoop();                       // CastLoop 트리거 발동
    }

    }