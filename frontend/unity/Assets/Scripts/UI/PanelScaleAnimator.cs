using UnityEngine;
using UnityEngine.UI;

public class PanelScaleAnimator : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform content;
    public float maxScale = 1.1f;   // 중앙 패널 확대 비율
    public float scaleSpeed = 8f;   // 보간 속도

    private RectTransform[] panels;
    private Vector2 viewportCenter;

    void Start()
    {
        panels = new RectTransform[content.childCount];
        for (int i = 0; i < content.childCount; i++)
            panels[i] = content.GetChild(i).GetComponent<RectTransform>();
    }

    void Update()
    {
        // Viewport 중심 (월드좌표 기준)
        viewportCenter = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);

        foreach (var panel in panels)
        {
            // 각 패널 중심 위치
            Vector2 panelCenter = panel.TransformPoint(panel.rect.center);
            float distance = Vector2.Distance(viewportCenter, panelCenter);

            // 가까울수록 scale 커지게 계산
            float t = Mathf.Clamp01(1f - (distance / 1500f)); // 거리 범위 조정
            float targetScale = Mathf.Lerp(1f, maxScale, t);

            // 부드러운 보간
            panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one * targetScale, Time.deltaTime * scaleSpeed);
        }
    }
}
