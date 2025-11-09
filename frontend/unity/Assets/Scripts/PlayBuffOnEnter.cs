using UnityEngine;

[DisallowMultipleComponent]
public class PlayBuffOnEnter : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float delay = 1f;

    private static readonly int Buff = Animator.StringToHash("Buff");
    private Coroutine playRoutine;

    private void Reset()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }
        playRoutine = StartCoroutine(PlayAfterDelay());
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    private System.Collections.IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        Play();
    }

    public void Play()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator)
        {
            animator.SetTrigger(Buff);
            // Alternatively: animator.CrossFade("Mage@Buff", 0.1f);
        }
    }

    // Optional: allow changing delay at runtime
    public void SetDelay(float seconds)
    {
        delay = seconds;
    }
}

