// ContentSceneMarker.cs

using UnityEngine;

public class ContentSceneMarker : MonoBehaviour
{
    public void OnEnable()
    {
        // 이 씬이 로드되면 현재 콘텐츠로 등록
        SceneRouter.CurrentContent = gameObject.scene.name;
        Debug.Log($"ContentSceneMarker: {gameObject.scene.name} 등록됨");
    }
}