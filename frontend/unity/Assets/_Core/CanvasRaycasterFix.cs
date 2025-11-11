using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// TrackedDeviceGraphicRaycaster의 KeyNotFoundException 버그 우회
/// Unity XR Interaction Toolkit 2.6.5 버그 수정
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasRaycasterFix : MonoBehaviour
{
    private TrackedDeviceGraphicRaycaster raycaster;

    private void Awake()
    {
        raycaster = GetComponent<TrackedDeviceGraphicRaycaster>();

        // Graphic Raycaster와 TrackedDeviceGraphicRaycaster 중복 체크
        var graphicRaycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (graphicRaycaster != null && raycaster != null)
        {
            Debug.LogWarning($"[CanvasRaycasterFix] {gameObject.name}에 GraphicRaycaster와 TrackedDeviceGraphicRaycaster가 동시에 있습니다. GraphicRaycaster를 제거합니다.");
            Destroy(graphicRaycaster);
        }
    }

    private void OnDestroy()
    {
        // TrackedDeviceGraphicRaycaster가 OnDisable에서 에러 발생하는 것을 방지
        if (raycaster != null)
        {
            try
            {
                raycaster.enabled = false;
            }
            catch (System.Exception e)
            {
                // 에러 무시 (이미 딕셔너리에서 제거됨)
                Debug.LogWarning($"[CanvasRaycasterFix] {gameObject.name} raycaster 비활성화 실패 (무시됨): {e.Message}");
            }
        }
    }
}
