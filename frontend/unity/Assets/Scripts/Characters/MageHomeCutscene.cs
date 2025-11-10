using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MageHomeCutscene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform target; // 예: RunTarget_Lobby

    [Header("Timing & Movement")]
    [SerializeField] private float waitBeforeStart = 5f;
    [SerializeField] private float rotateDuration = 0.75f;
    [SerializeField] private float runSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.2f;

    [Header("Animator Triggers")]
    [Tooltip("Idle→Run 전이에 사용할 Trigger 이름")]
    [SerializeField] private string runTriggerName = "Run";
    [Tooltip("도착 후 Idle 등으로 복귀할 때 사용할 Trigger 이름(선택)")]
    [SerializeField] private string stopTriggerName = "";

    [Header("Behavior")]
    [SerializeField] private bool deactivateOnArrive = true;
    [SerializeField] private float vanishDelay = 0f;         // 비활성화 전 딜레이
    [SerializeField] private bool forceAnimatorAlwaysAnimate = true; // 카메라 미렌더 시에도 애니메이션 유지

    private void Reset()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator)
        {
            if (forceAnimatorAlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false; // 이동은 스크립트가 제어
            animator.enabled = true;
        }
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        if (waitBeforeStart > 0f)
            yield return new WaitForSeconds(waitBeforeStart);

        if (!animator || target == null) yield break;

        // 1) 타깃을 바라보도록 회전
        Quaternion startRot = transform.rotation;
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion endRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            float t = 0f;
            float dur = Mathf.Max(0.0001f, rotateDuration);
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRot, endRot, Mathf.Clamp01(t / dur));
                yield return null;
            }
            transform.rotation = endRot;
        }

        // 2) 회전 직후 Run 트리거 발사
        if (!string.IsNullOrEmpty(runTriggerName))
        {
            animator.ResetTrigger(runTriggerName);
            animator.SetTrigger(runTriggerName);
        }

        // 3) 로비까지 이동(항상 타깃을 보며 이동)
        while (true)
        {
            Vector3 pos = transform.position;
            Vector3 flatTarget = target.position; flatTarget.y = pos.y;
            float dist = Vector3.Distance(pos, flatTarget);
            if (dist <= stopDistance) break;

            Vector3 dir = (flatTarget - pos).normalized;
            if (dir.sqrMagnitude > 0f)
            {
                Quaternion face = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, face, 360f * Time.deltaTime);
            }
            transform.position = Vector3.MoveTowards(pos, flatTarget, runSpeed * Time.deltaTime);
            yield return null;
        }

        // 4) 도착 처리
        if (!string.IsNullOrEmpty(stopTriggerName))
        {
            animator.ResetTrigger(stopTriggerName);
            animator.SetTrigger(stopTriggerName);
        }

        if (deactivateOnArrive)
        {
            if (vanishDelay > 0f) yield return new WaitForSeconds(vanishDelay);
            gameObject.SetActive(false);
        }
    }
}

