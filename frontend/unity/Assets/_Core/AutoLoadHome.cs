using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(100)]
public class AutoLoadHome : MonoBehaviour
{
    [SerializeField] string firstScene = SceneId.Home;
    [SerializeField] bool loadOnPlay = true;

    private IEnumerator Start()
    {
        Debug.Log($"[AutoLoadHome] active={SceneManager.GetActiveScene().name}, loadOnPlay={loadOnPlay}");
        if (!loadOnPlay) yield break;

        if (SceneManager.GetActiveScene().name != SceneId.Persistent)
        {
            Debug.Log("[AutoLoadHome] Not in _Persistent. Skip.");
            yield break;
        }

        // SceneLoader 준비될 때까지 대기
        yield return new WaitUntil(() => SceneLoader.Instance != null);
        Debug.Log("[AutoLoadHome] SceneLoader ready");

        // 이미 로드되어 있지 않으면 로드
        if (!SceneManager.GetSceneByName(firstScene).isLoaded)
        {
            Debug.Log($"[AutoLoadHome] Load {firstScene}");
            SceneLoader.Instance.LoadScene(firstScene);
        }
        else
        {
            Debug.Log($"[AutoLoadHome] {firstScene} already loaded");
        }
    }
}
