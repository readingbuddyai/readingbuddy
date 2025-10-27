using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager I { get; private set; }

    [Header("Transition")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 0.35f;

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
        if (fadeCanvas != null)
            fadeCanvas.alpha = 0f;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(CoLoadScene(sceneName));
    }

    private IEnumerator CoLoadScene(string sceneName)
    {
        // 1. È­¸é ¾îµÓ°Ô
        yield return CoFade(1f);

        // 2. »õ ¾À ºñµ¿±â ·Îµå
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
            yield return null;
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // 3. ´Ù½Ã ¹à°Ô
        yield return CoFade(0f);
    }

    private IEnumerator CoFade(float target)
    {
        if (fadeCanvas == null) yield break;
        float start = fadeCanvas.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = target;
    }
}

