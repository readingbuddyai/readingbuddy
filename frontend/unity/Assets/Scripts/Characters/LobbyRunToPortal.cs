using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LobbyRunToPortal : MonoBehaviour
{
    [Header("References")]
    [Tooltip("캐릭터의 Animator (Run/Idle 전환용)")]
    [SerializeField] private Animator animator;

    [Tooltip("이동할 목표 Transform (Portal)")]
    [SerializeField] public Transform target;

    [Header("Movement Settings")]
    [Tooltip("달리기 속도 (m/s)")]
    [SerializeField] private float runSpeed = 2.5f;
    [Tooltip("도착 판정 거리 (m)")]
    [SerializeField] private float stopDistance = 0.25f;
    [Tooltip("시작 전 대기 시간 (초)")]
    [SerializeField] private float waitBeforeStart = 0.5f;
    [Tooltip("목표를 향해 회전하는 시간 (초)")]
    [SerializeField] private float rotateDuration = 0.6f;

    [Header("Animator Trigger Names")]
    [Tooltip("달리기 시작 트리거 이름")]
    [SerializeField] private string runTriggerName = "Run";
    [Tooltip("도착 후 Idle로 돌아올 트리거 이름 (선택)")]
    [SerializeField] private string stopTriggerName = "Idle";

    [Header("Behavior Options")]
    [Tooltip("도착 후 오브젝트 비활성화 여부")]
    [SerializeField] private bool deactivateOnArrive = false;
    [Tooltip("비활성화까지의 지연 (초)")]
    [SerializeField] private float vanishDelay = 0.5f;
    [Tooltip("카메라에 안 보여도 애니메이션 유지")]
    [SerializeField] private bool forceAlwaysAnimate = true;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (forceAlwaysAnimate && animator)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (target != null)
            StartCoroutine(CoRunToPortal());
    }

    private IEnumerator CoRunToPortal()
    {
        if (waitBeforeStart > 0f)
            yield return new WaitForSeconds(waitBeforeStart);

        if (target == null || animator == null)
            yield break;

        // 회전
        Vector3 flatDir = target.position - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion startRot = transform.rotation;
            Quaternion endRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            float t = 0f;
            while (t < rotateDuration)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRot, endRot, t / rotateDuration);
                yield return null;
            }
            transform.rotation = endRot;
        }

        // 달리기 시작
        if (!string.IsNullOrEmpty(runTriggerName))
        {
            animator.ResetTrigger(runTriggerName);
            animator.SetTrigger(runTriggerName);
        }

        // 이동
        while (true)
        {
            Vector3 pos = transform.position;
            Vector3 dest = target.position; dest.y = pos.y;
            float dist = Vector3.Distance(pos, dest);
            if (dist <= stopDistance) break;

            Vector3 dir = (dest - pos).normalized;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            transform.position = Vector3.MoveTowards(pos, dest, runSpeed * Time.deltaTime);
            yield return null;
        }

        // 도착 후 처리
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
