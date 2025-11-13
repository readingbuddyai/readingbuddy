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
    private HorizontalLayoutGroup horizontalLayoutGroup;
    private int lastPadding = -1;

    void Start()
    {
        panels = new RectTransform[content.childCount];
        for (int i = 0; i < content.childCount; i++)
            panels[i] = content.GetChild(i).GetComponent<RectTransform>();
        horizontalLayoutGroup = content.GetComponent<HorizontalLayoutGroup>();
        AdjustContentPadding(force: true);
    }

    void Update()
    {
        // Viewport �߽� (������ǥ ����)
        viewportCenter = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);

        foreach (var panel in panels)
        {
            Vector2 panelCenter = panel.TransformPoint(panel.rect.center);
            float distance = Vector2.Distance(viewportCenter, panelCenter);
            float t = Mathf.Clamp01(1f - (distance / 1500f));
            float targetScale = Mathf.Lerp(1f, maxScale, t);
            panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one * targetScale, Time.deltaTime * scaleSpeed);
        }

        AdjustContentPadding();
    }

    private void AdjustContentPadding(bool force = false)
    {
        if (horizontalLayoutGroup == null || scrollRect == null || scrollRect.viewport == null || panels.Length == 0)
            return;

        float viewportWidth = scrollRect.viewport.rect.width;
        if (viewportWidth <= 0f)
            return;

        RectTransform referencePanel = panels[0];
        float panelWidth = referencePanel.rect.width;
        if (panelWidth <= 0f)
            panelWidth = LayoutUtility.GetPreferredWidth(referencePanel);
        if (panelWidth <= 0f)
            return;

        int padding = Mathf.RoundToInt(Mathf.Max(0f, (viewportWidth - panelWidth) * 0.5f));
        if (!force && padding == lastPadding)
            return;

        lastPadding = padding;

        if (horizontalLayoutGroup.padding.left == padding && horizontalLayoutGroup.padding.right == padding)
            return;

        horizontalLayoutGroup.padding.left = padding;
        horizontalLayoutGroup.padding.right = padding;
        LayoutRebuilder.MarkLayoutForRebuild(content);
    }

}
