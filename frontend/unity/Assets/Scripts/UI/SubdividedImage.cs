using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Subdivided Image")]
public class SubdividedImage : Image
{
    [Range(1,128)] public int segmentsX = 32;
    [Range(1,128)] public int segmentsY = 18;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = GetPixelAdjustedRect();
        Vector2 size = r.size;
        Vector2 min = r.min;

        int vx = Mathf.Max(1, segmentsX);
        int vy = Mathf.Max(1, segmentsY);

        // 그리드 정점/UV 생성
        for (int y = 0; y <= vy; y++)
        {
            float ty = (float)y / vy;
            float py = min.y + size.y * ty;
            for (int x = 0; x <= vx; x++)
            {
                float tx = (float)x / vx;
                float px = min.x + size.x * tx;

                var v = UIVertex.simpleVert;
                v.position = new Vector3(px, py, 0);
                // 단색/일반 스프라이트 UV
                Vector2 uv = (overrideSprite != null)
                    ? new Vector2(Mathf.Lerp(overrideSprite.uv[3].x, overrideSprite.uv[0].x, tx),
                                  Mathf.Lerp(overrideSprite.uv[3].y, overrideSprite.uv[0].y, ty))
                    : new Vector2(tx, ty);
                v.uv0 = uv;
                v.color = color;
                vh.AddVert(v);
            }
        }

        // 인덱스
        for (int y = 0; y < vy; y++)
        {
            for (int x = 0; x < vx; x++)
            {
                int i0 = y * (vx + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (vx + 1);
                int i3 = i2 + 1;
                vh.AddTriangle(i0, i2, i1);
                vh.AddTriangle(i1, i2, i3);
            }
        }
    }
}
