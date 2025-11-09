using UnityEngine;

/// <summary>
/// Simple helper to play a "wave" animation on the character's Animator.
/// - Option 1 (default): trigger-based (requires an Animator parameter and transitions)
/// - Option 2: direct CrossFade to a state by name
/// Attach this to the character (same object that has the Animator).
/// </summary>
public class PlayWave : MonoBehaviour
{
    public Animator animator;

    [Header("Trigger Mode")]
    public bool useTrigger = true;
    public string triggerName = "Wave";     // Animator parameter (Trigger)

    [Header("State Mode")]
    public string stateName = "Wave";        // Animator state name for CrossFade
    public float fadeDuration = 0.1f;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    public void Play()
    {
        if (!animator) return;
        if (useTrigger)
        {
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
        }
        else
        {
            if (!string.IsNullOrEmpty(stateName))
                animator.CrossFade(stateName, fadeDuration, 0, 0f);
        }
    }

    // Quick test: press G in Play Mode
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) Play();
    }
}

