using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class SkyboxEntry
{
    public string sceneName;              // 예: "Home", "Lobby", ...
    public Material skybox;               // 적용할 스카이박스
    public bool applyExposure = true;     // 노출값 적용할지
    [Range(-10f, 10f)] public float exposure = 1f;     // 기본 1
    public bool applyRotation = false;    // 회전 적용할지
    [Range(0f, 360f)] public float rotation = 0f;      // 파노라마용
}

public class SkyboxManager : MonoBehaviour
{
    [Tooltip("씬 이름 ↔ 스카이박스 매핑 표")]
    public List<SkyboxEntry> entries = new List<SkyboxEntry>();

    void OnEnable()  { SceneManager.activeSceneChanged += OnActiveSceneChanged; }
    void OnDisable() { SceneManager.activeSceneChanged -= OnActiveSceneChanged; }

    void OnActiveSceneChanged(Scene prev, Scene next)
    {
        // 활성 씬 이름으로 매핑 찾기
        var e = entries.Find(x => x.sceneName == next.name);
        if (e == null || e.skybox == null) return;

        // 원본 보호: 런타임 인스턴스로 교체
        var runtimeSky = new Material(e.skybox) { name = e.skybox.name + " (Runtime)" };
        RenderSettings.skybox = runtimeSky;

        // 선택 적용 + 안전장치(최소값 클램프)
        if (e.applyExposure && runtimeSky.HasProperty("_Exposure"))
        {
            float safeExpo = Mathf.Max(e.exposure, 0.001f); // 0으로 못 떨어지게
            runtimeSky.SetFloat("_Exposure", safeExpo);
        }

        if (e.applyRotation && runtimeSky.HasProperty("_Rotation"))
        {
            runtimeSky.SetFloat("_Rotation", e.rotation);
        }

        DynamicGI.UpdateEnvironment(); // 라이팅 갱신
    }
}
