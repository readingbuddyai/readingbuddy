using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class XROriginSceneManager : MonoBehaviour
{
    private XROrigin xrOrigin;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        // 🚀 XROrigin 위치 리셋을 다음 프레임으로 미룸
        StartCoroutine(CoResetOrigin(newScene));
    }

    private IEnumerator CoResetOrigin(Scene newScene)
    {
        // 🔸 씬이 완전히 로드될 때까지 1~2프레임 대기
        yield return null;
        yield return new WaitForEndOfFrame();

        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>(true);

        if (xrOrigin == null)
        {
            Debug.LogWarning("⚠️ XR Origin not found after scene load.");
            yield break;
        }

        // 🧭 PlayerSpawn 위치 탐색
        GameObject spawn = GameObject.FindWithTag("PlayerSpawn");

        if (spawn != null)
        {
            xrOrigin.transform.SetPositionAndRotation(
                spawn.transform.position,
                spawn.transform.rotation
            );
            Debug.Log($"📍 XR Origin moved to spawn point in {newScene.name}");
        }
        else
        {
            xrOrigin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Debug.LogWarning($"⚠️ PlayerSpawn not found in {newScene.name}, reset to (0,0,0)");
        }
    }
}
