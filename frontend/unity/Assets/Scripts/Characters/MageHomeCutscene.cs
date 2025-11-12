using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MageHomeCutscene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform target;

    [Header("Movement Settings")]
    [SerializeField] private float rotateDuration = 0.75f;
    [SerializeField] private float runSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.2f;

    [Header("Animator Triggers")]
    [SerializeField] private string runTriggerName = "Run";
    [SerializeField] private string stopTriggerName = "";

    [Header("Behavior")]
    [SerializeField] private bool deactivateOnArrive = true;
    [SerializeField] private float vanishDelay = 0f;
    [SerializeField] private bool forceAnimatorAlwaysAnimate = true;

    private void OnEnable()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator)
        {
            if (forceAnimatorAlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            animator.enabled = true;
        }
    }

    // 외부(오디오 타이밍)에서 호출할 메서드
    public void StartRunNow()
    {
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        if (!animator || target == null) yield break;

        // 타깃 방향으로 회전
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

        // Run 트리거 발동
        if (!string.IsNullOrEmpty(runTriggerName))
        {
            animator.ResetTrigger(runTriggerName);
            animator.SetTrigger(runTriggerName);
        }

        // 이동
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

        // 도착 처리
        if (!string.IsNullOrEmpty(stopTriggerName))
        {
            animator.ResetTrigger(stopTriggerName);
            animator.SetTrigger(stopTriggerName);
        }

        if (deactivateOnArrive)
        {
            if (vanishDelay > 0f)
                yield return new WaitForSeconds(vanishDelay);
            gameObject.SetActive(false);
        }
    }
}
