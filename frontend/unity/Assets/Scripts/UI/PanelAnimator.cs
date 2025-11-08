using System.Collections;
using UnityEngine;

namespace Stage.UI
{
    /// <summary>
    /// Reusable helper that shows/hides a panel with either Animator triggers or CanvasGroup fade.
    /// Attach this to the panel root and call Show/Hide instead of toggling SetActive directly.
    /// </summary>
    public class PanelAnimator : MonoBehaviour
    {
        public enum AnimationMode
        {
            AnimatorTriggers,
            CanvasGroupFade
        }

        [Header("General")]
        [SerializeField] private AnimationMode mode = AnimationMode.AnimatorTriggers;
        [SerializeField] private GameObject targetObject;

        [Header("Animator Mode")]
        [SerializeField] private Animator animator;
        [SerializeField] private string showTrigger = "Show";
        [SerializeField] private string hideTrigger = "Hide";

        [Header("CanvasGroup Fade Mode")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeDuration = 0.3f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool toggleInteractable = true;

        private Coroutine _animationRoutine;

        private void Reset()
        {
            targetObject = gameObject;
            animator = GetComponent<Animator>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (targetObject == null)
                targetObject = gameObject;

            if (mode == AnimationMode.AnimatorTriggers && animator == null)
                animator = targetObject.GetComponent<Animator>();

            if (mode == AnimationMode.CanvasGroupFade && canvasGroup == null)
                canvasGroup = targetObject.GetComponent<CanvasGroup>();

            if (canvasGroup != null && targetObject.activeSelf)
            {
                canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);
                if (toggleInteractable)
                {
                    bool interactive = canvasGroup.alpha > 0.99f;
                    canvasGroup.interactable = interactive;
                    canvasGroup.blocksRaycasts = interactive;
                }
            }
        }

        public void Show(bool immediate = false)
        {
            Play(true, immediate);
        }

        public void Hide(bool immediate = false)
        {
            Play(false, immediate);
        }

        private void Play(bool show, bool immediate)
        {
            if (!gameObject.activeInHierarchy)
                targetObject.SetActive(true);

            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }

            switch (mode)
            {
                case AnimationMode.AnimatorTriggers:
                    PlayAnimator(show, immediate);
                    break;
                case AnimationMode.CanvasGroupFade:
                    _animationRoutine = StartCoroutine(FadeRoutine(show, immediate));
                    break;
            }
        }

        private void PlayAnimator(bool show, bool immediate)
        {
            if (animator == null)
            {
                // Fallback: just toggle active state.
                targetObject.SetActive(show);
                return;
            }

            if (immediate)
            {
                // Animator has no immediate mode, so manually set active state.
                animator.ResetTrigger(show ? hideTrigger : showTrigger);
                animator.Play(0, 0, show ? 1f : 0f);
                animator.Update(0f);
                targetObject.SetActive(show);
            }
            else
            {
                if (show)
                {
                    targetObject.SetActive(true);
                    if (!string.IsNullOrEmpty(showTrigger))
                        animator.SetTrigger(showTrigger);
                }
                else
                {
                    if (!string.IsNullOrEmpty(hideTrigger))
                        animator.SetTrigger(hideTrigger);
                    else
                        targetObject.SetActive(false);
                }
            }
        }

        private IEnumerator FadeRoutine(bool show, bool immediate)
        {
            if (canvasGroup == null)
            {
                targetObject.SetActive(show);
                yield break;
            }

            if (toggleInteractable && show)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            float duration = fadeDuration;
            if (immediate || duration <= 0f)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                if (toggleInteractable)
                {
                    canvasGroup.interactable = show;
                    canvasGroup.blocksRaycasts = show;
                }
                if (!show)
                    targetObject.SetActive(false);
                yield break;
            }

            targetObject.SetActive(true);

            float startAlpha = canvasGroup.alpha;
            float endAlpha = show ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = fadeCurve != null ? fadeCurve.Evaluate(t) : t;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;

            if (toggleInteractable)
            {
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
            }

            if (!show)
                targetObject.SetActive(false);

            _animationRoutine = null;
        }
    }
}

