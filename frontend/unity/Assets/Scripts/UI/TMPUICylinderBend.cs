using UnityEngine;
using TMPro;
using System.Collections;

[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
public class TMPUICylinderBend : MonoBehaviour
{
    public UICylinderBend.Axis axis = UICylinderBend.Axis.Horizontal;
    [Range(0f,180f)] public float totalAngle = 80f;
    public float radius = 0f;
    public bool invertDepth = false;

    public bool useReferenceRect = true;
    public RectTransform referenceRect;

    public float depthOffset = 0f; // 선택: 살짝 붙이기

    TMP_Text tmp;

    void OnEnable()
    {
        tmp = GetComponent<TMP_Text>();
        tmp.OnPreRenderText += OnPreRenderText;
        StartCoroutine(InitNextFrame()); // 기준 주입 후 1프레임 뒤 강제 갱신
    }

    void OnDisable()
    {
        if (tmp != null) tmp.OnPreRenderText -= OnPreRenderText;
    }

    IEnumerator InitNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (tmp != null)
        {
            tmp.havePropertiesChanged = true;
            tmp.ForceMeshUpdate(true, true);
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }
    }

    void OnValidate()
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        tmp?.ForceMeshUpdate();
    }

    void OnPreRenderText(TMP_TextInfo info)
    {
        var target = (useReferenceRect && referenceRect) ? referenceRect : (transform as RectTransform);
        if (target == null) return;

        var r = target.rect;
        float W = Mathf.Max(0.0001f, r.width);
        float H = Mathf.Max(0.0001f, r.height);
        float ang = Mathf.Max(0.0001f, totalAngle) * Mathf.Deg2Rad;
        float baseLen = (axis == UICylinderBend.Axis.Horizontal) ? W : H;
        float R = (radius > 0f) ? radius : (baseLen / ang);

        for (int mi = 0; mi < info.meshInfo.Length; mi++)
        {
            var meshInfo = info.meshInfo[mi];
            var verts = meshInfo.vertices;
            if (verts == null || verts.Length == 0) continue;

            for (int i = 0; i < meshInfo.vertexCount; i++)
            {
                Vector3 p = verts[i];
                Vector3 world = tmp.rectTransform.TransformPoint(p);
                Vector3 refLocal = target.InverseTransformPoint(world);
                Vector3 outRef = refLocal;

                if (axis == UICylinderBend.Axis.Horizontal)
                {
                    float x = refLocal.x - r.center.x;
                    float t = Mathf.Clamp(x / (W * 0.5f), -1f, 1f);
                    float theta = t * (ang * 0.5f);
                    float nx = Mathf.Sin(theta) * R + r.center.x;
                    float nz = R * (1f - Mathf.Cos(theta));
                    if (invertDepth) nz = -nz;
                    outRef.x = nx; outRef.z = -nz + depthOffset;
                }
                else
                {
                    float y = refLocal.y - r.center.y;
                    float t = Mathf.Clamp(y / (H * 0.5f), -1f, 1f);
                    float theta = t * (ang * 0.5f);
                    float ny = Mathf.Sin(theta) * R + r.center.y;
                    float nz = R * (1f - Mathf.Cos(theta));
                    if (invertDepth) nz = -nz;
                    outRef.y = ny; outRef.z = -nz + depthOffset;
                }

                Vector3 outWorld = target.TransformPoint(outRef);
                verts[i] = tmp.rectTransform.InverseTransformPoint(outWorld);
            }

            meshInfo.mesh.vertices = verts;
            tmp.UpdateGeometry(meshInfo.mesh, mi);
        }
    }
}
