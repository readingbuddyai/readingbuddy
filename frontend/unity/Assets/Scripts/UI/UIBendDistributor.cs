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

    void OnEnable()  { Apply(); StartCoroutine(RefreshTMPNextFrame()); }
    void OnValidate() { Apply(); StartCoroutine(RefreshTMPNextFrame()); }

    public void Apply()
    {
        if (!isActiveAndEnabled) return;
        if (referenceRect == null) referenceRect = transform as RectTransform;

        var graphics = GetComponentsInChildren<Graphic>(includeInactive);
        foreach (var g in graphics)
        {
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
            t.havePropertiesChanged = true;
            t.ForceMeshUpdate(true, true);
            t.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }
    }
}
