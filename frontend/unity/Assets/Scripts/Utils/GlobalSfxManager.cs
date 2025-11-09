using System.Collections;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 프로젝트 전역에서 공통으로 사용하는 효과음(SFX)을 관리하는 매니저.
    /// BgmManager와 마찬가지로 _Persistent 씬 등에서 DontDestroyOnLoad 오브젝트에 붙여 사용합니다.
    /// </summary>
    public class GlobalSfxManager : MonoBehaviour
    {
        public static GlobalSfxManager Instance { get; private set; }

        [Header("Audio Source")]
        [Tooltip("SFX 재생에 사용할 AudioSource. 비워두면 동일 오브젝트에서 자동으로 찾습니다.")]
        public AudioSource audioSource;

        [Header("Predefined Clips")]
        [Tooltip("프로젝트 시작(최초 Persistent 씬 진입) 시 재생할 효과음")]
        public AudioClip startupClip;

        [Tooltip("씬 전환 시작 또는 완료 시 재생할 효과음")]
        public AudioClip sceneTransitionClip;

        [Tooltip("씬 전환 효과음 재생 시 기본 볼륨")]
        [Range(0f, 1f)]
        public float sceneTransitionVolume = 1f;

        [Tooltip("프로젝트 시작 효과음 재생 시 기본 볼륨")]
        [Range(0f, 1f)]
        public float startupVolume = 1f;

        [Header("Options")]
        [Tooltip("Awake 시 startupClip을 바로 재생할지 여부 (Persistent 씬에서 한 번만 실행)")]
        public bool playStartupOnAwake = true;
        [Tooltip("첫 번째 씬 전환 요청은 효과음을 생략할지 여부")]
        public bool skipFirstSceneTransitionSfx = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!audioSource)
                audioSource = GetComponent<AudioSource>();

            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }

            if (playStartupOnAwake)
                PlayStartupSfx();
        }

        /// <summary>
        /// 프로젝트 시작 시점에서 호출하는 효과음 재생.
        /// Awake에서 자동으로 호출되지만, 필요하면 외부에서 다시 호출할 수도 있습니다.
        /// </summary>
        public void PlayStartupSfx()
        {
            PlayOneShot(startupClip, startupVolume);
        }

        /// <summary>
        /// 씬 전환 시점에서 호출할 효과음 재생.
        /// SceneManager 이벤트나 전환 매니저에서 호출해 주세요.
        /// </summary>
        public void PlaySceneTransitionSfx()
        {
            if (_skipNextSceneTransitionSfxOnce)
            {
                _skipNextSceneTransitionSfxOnce = false;
                return;
            }
            PlayOneShot(sceneTransitionClip, sceneTransitionVolume);
        }

        /// <summary>
        /// 임의의 AudioClip을 전역 SFX AudioSource에서 재생합니다.
        /// </summary>
        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (!clip || !audioSource)
                return;

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary>
        /// 현재 재생 중인 효과음을 즉시 정지합니다.
        /// </summary>
        public void Stop()
        {
            if (audioSource)
                audioSource.Stop();
        }

        /// <summary>
        /// 최소 지속 시간이 지나기 전까지 효과음이 끊기지 않도록 보호하는 헬퍼.
        /// 필요할 때만 사용하세요.
        /// </summary>
        public IEnumerator PlayOneShotWithMinimumDuration(AudioClip clip, float volume, float minimumDuration)
        {
            if (!clip || !audioSource)
                yield break;

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            float elapsed = 0f;
            while (elapsed < minimumDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void SkipNextSceneTransitionSfx()
        {
            _skipNextSceneTransitionSfxOnce = true;
        }

        private bool _skipNextSceneTransitionSfxOnce;

        private void OnEnable()
        {
            if (skipFirstSceneTransitionSfx)
                _skipNextSceneTransitionSfxOnce = true;
        }
    }
}

