using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, DisallowMultipleComponent]
public class UICylinderBend : BaseMeshEffect
{
    public enum Axis { Horizontal, Vertical }

    [Header("곡률")]
    [Range(0f, 180f)] public float totalAngle = 80f; // 전체 펼침 각(도)
    [Tooltip("0이면 size와 totalAngle로 반지름 자동 산출")]
    public float radius = 0f;
    public Axis axis = Axis.Horizontal;
    public bool invertDepth = false;

    [Header("공유 기준(패널 기준으로 굽힘)")]
    public bool useReferenceRect = true;
    public RectTransform referenceRect; // Panel 등 기준 RectTransform

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        RectTransform target = (useReferenceRect && referenceRect != null)
            ? referenceRect
            : graphic.rectTransform;

        Rect r = target.rect;
        float W = Mathf.Max(0.0001f, r.width);
        float H = Mathf.Max(0.0001f, r.height);

        float ang = Mathf.Max(0.0001f, totalAngle) * Mathf.Deg2Rad;
        float baseLen = (axis == Axis.Horizontal) ? W : H;
        float R = (radius > 0f) ? radius : (baseLen / ang);

        UIVertex v = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);

            // 원래 버텍스 → 월드 → 기준 Rect 로컬
            Vector3 world = graphic.rectTransform.TransformPoint(v.position);
            Vector3 refLocal = target.InverseTransformPoint(world);

            Vector3 outRefLocal = refLocal;

            if (axis == Axis.Horizontal)
            {
                float x = refLocal.x - r.center.x;
                float t = Mathf.Clamp(x / (W * 0.5f), -1f, 1f);
                float theta = t * (ang * 0.5f);
                float nx = Mathf.Sin(theta) * R + r.center.x;
                float nz = R * (1f - Mathf.Cos(theta));
                if (invertDepth) nz = -nz;
                outRefLocal.x = nx;
                outRefLocal.z = -nz;
            }
            else
            {
                float y = refLocal.y - r.center.y;
                float t = Mathf.Clamp(y / (H * 0.5f), -1f, 1f);
                float theta = t * (ang * 0.5f);
                float ny = Mathf.Sin(theta) * R + r.center.y;
                float nz = R * (1f - Mathf.Cos(theta));
                if (invertDepth) nz = -nz;
                outRefLocal.y = ny;
                outRefLocal.z = -nz;
            }

            // 기준 로컬 → 월드 → 원래 로컬
            Vector3 outWorld = target.TransformPoint(outRefLocal);
            Vector3 outLocal = graphic.rectTransform.InverseTransformPoint(outWorld);

            v.position = outLocal;
            vh.SetUIVertex(v, i);
        }
    }

    protected override void OnEnable()  { base.OnEnable();  graphic?.SetVerticesDirty(); }
    protected override void OnDisable() { graphic?.SetVerticesDirty(); base.OnDisable();  }
    private void OnValidate() { graphic?.SetVerticesDirty(); }
}
