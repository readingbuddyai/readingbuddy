using UnityEngine;
using UnityEngine.EventSystems;

public class SceneCleanup : MonoBehaviour
{
    void Awake()
    {
        // 🎯 EventSystem 중복 제거 (1개만 남기기)
        var systems = FindObjectsOfType<EventSystem>(true);
        for (int i = 1; i < systems.Length; i++)
        {
            Destroy(systems[i].gameObject);
        }

        // 🎧 AudioListener 중복 제거 (첫 번째만 유지)
        var listeners = FindObjectsOfType<AudioListener>(true);
        for (int i = 1; i < listeners.Length; i++)
        {
            listeners[i].enabled = false;
        }
    }
}
