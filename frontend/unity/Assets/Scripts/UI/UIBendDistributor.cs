using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[ExecuteAlways]
public class UIBendDistributor : MonoBehaviour
{
    public UICylinderBend.Axis axis = UICylinderBend.Axis.Horizontal;
    [Range(0f, 180f)] public float totalAngle = 80f;
    public float radius = 0f;
    public bool invertDepth = false;
    public bool includeInactive = true;

    public RectTransform referenceRect;

    void OnEnable()
    {
        Apply();

        // 플레이 중일 때만 코루틴 돌리기 (에디터 모드 NRE 방지)
        if (Application.isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(RefreshTMPNextFrame());
        }
    }

    void OnValidate()
    {
        Apply();

        // 인스펙터 값 바꿀 때도, 플레이 중일 때만 코루틴 실행
        if (Application.isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(RefreshTMPNextFrame());
        }
    }

    public void Apply()
    {
        if (!isActiveAndEnabled) return;
        if (referenceRect == null) referenceRect = transform as RectTransform;

        var graphics = GetComponentsInChildren<Graphic>(includeInactive);
        foreach (var g in graphics)
        {
            if (g == null) continue;

            var tmp = g.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                var tBend = g.GetComponent<TMPUICylinderBend>() ?? g.gameObject.AddComponent<TMPUICylinderBend>();
                tBend.axis = axis;
                tBend.totalAngle = totalAngle;
                tBend.radius = radius;
                tBend.invertDepth = invertDepth;
                tBend.useReferenceRect = true;
                tBend.referenceRect = referenceRect;
                continue;
            }

            var bend = g.GetComponent<UICylinderBend>() ?? g.gameObject.AddComponent<UICylinderBend>();
            bend.axis = axis;
            bend.totalAngle = totalAngle;
            bend.radius = radius;
            bend.invertDepth = invertDepth;
            bend.useReferenceRect = true;
            bend.referenceRect = referenceRect;

            g.SetVerticesDirty();
        }
    }

    IEnumerator RefreshTMPNextFrame()
    {
        // Distributor 주입 후 한 프레임 기다렸다가 강제 재생성
        yield return null;

        Canvas.ForceUpdateCanvases();

        var tmps = GetComponentsInChildren<TMP_Text>(includeInactive);
        foreach (var t in tmps)
        {
            // 코루틴 도는 동안 삭제/비활성된 TMP 방어
            if (t == null) continue;
            if (!t.gameObject.activeInHierarchy) continue;

            t.havePropertiesChanged = true;
            t.ForceMeshUpdate(true, true);
            t.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }
    }
}
