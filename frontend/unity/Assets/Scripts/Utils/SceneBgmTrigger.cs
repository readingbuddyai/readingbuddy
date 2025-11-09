using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 씬 진입 시 BgmManager에 특정 BGM 재생/정지를 요청하는 헬퍼.
    /// 씬마다 필요할 때만 배치하면 됩니다.
    /// </summary>
    public class SceneBgmTrigger : MonoBehaviour
    {
        [Header("Trigger Options")]
        [Tooltip("씬이 시작될 때 자동으로 BGM을 적용할지 여부")]
        public bool applyOnStart = true;

        [Tooltip("Start 대신 OnEnable에서 적용하고 싶으면 체크 해제 후 직접 Apply를 호출하세요.")]
        public bool applyOnEnable = false;

        [Header("BGM Selection")]
        [Tooltip("BgmManager 라이브러리에 등록된 키. 값이 비어 있으면 StopBgm이 호출됩니다.")]
        public string bgmKey;

        [Tooltip("Play 호출 시 사용할 페이드 시간(초). 음수면 BgmManager 기본값을 사용합니다.")]
        public float fadeSeconds = -1f;

        [Tooltip("true면 apply 시점에 즉시 StopBgm(fade)를 호출합니다.")]
        public bool stopInsteadOfPlay = false;

        private void Start()
        {
            if (applyOnStart)
                Apply();
        }

        private void OnEnable()
        {
            if (!applyOnStart && applyOnEnable)
                Apply();
        }

        public void Apply()
        {
            if (!BgmManager.Instance)
            {
                Debug.LogWarning("[SceneBgmTrigger] BgmManager가 씬에 존재하지 않습니다.");
                return;
            }

            if (stopInsteadOfPlay)
            {
                BgmManager.Instance.StopBgm(fadeSeconds);
                return;
            }

            if (string.IsNullOrWhiteSpace(bgmKey))
            {
                BgmManager.Instance.StopBgm(fadeSeconds);
            }
            else
            {
                BgmManager.Instance.PlayBgm(bgmKey, fadeSeconds);
            }
        }
    }
}

