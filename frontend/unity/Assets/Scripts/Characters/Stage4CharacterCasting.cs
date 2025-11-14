using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Stage4CharacterCasting : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Tooltip("Animator에서 CastingLoop를 트리거하는 파라미터 이름")]
    [SerializeField] private string castingTrigger = "CastingLoop";
    [Tooltip("씬이 시작되고 난 뒤 idle 상태를 유지할 시간")]
    [SerializeField] private float initialIdleDelay = 5f;
    [Tooltip("CastingLoop 애니메이션을 유지할 시간 (초)")]
    [SerializeField] private float castingDuration = 2f;

    private Coroutine _routine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        StartCastingSequence();
    }

    private void OnDisable()
    {
        if (_routine != null)
            StopCoroutine(_routine);
    }

    private void StartCastingSequence()
    {
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(CastingRoutine());
    }

    private IEnumerator CastingRoutine()
    {
        if (!animator)
            yield break;

        yield return new WaitForSeconds(initialIdleDelay);
        animator.ResetTrigger(castingTrigger);
        animator.SetTrigger(castingTrigger);
        yield return new WaitForSeconds(castingDuration);
        _routine = null;
    }
}
