using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 씬 전역에서 재사용 가능한 BGM 관리 유틸.
    /// 싱글턴으로 동작하며 DontDestroyOnLoad 오브젝트에 붙여두고 사용하세요.
    /// </summary>
    public class BgmManager : MonoBehaviour
    {
        public static BgmManager Instance { get; private set; }

        [Header("Audio Source")]
        [Tooltip("BGM 재생에 사용할 AudioSource. 비워두면 동일 오브젝트에서 자동으로 찾습니다.")]
        public AudioSource audioSource;

        [Header("Fade Settings")]
        [Tooltip("Play/Stop 시 기본 페이드 시간(초)")]
        [Min(0f)]
        public float defaultFadeSeconds = 0.6f;

        [Header("Library")]
        [Tooltip("씬 전역에서 사용할 수 있는 BGM 목록")]
        public List<BgmEntry> bgmLibrary = new List<BgmEntry>();

        private readonly Dictionary<string, BgmEntry> _bgmLookup = new Dictionary<string, BgmEntry>(StringComparer.OrdinalIgnoreCase);
        private Coroutine _fadeCoroutine;
        private string _currentKey;

        [Serializable]
        public class BgmEntry
        {
            public string key;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float volume = 1f;
        }

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
                audioSource.loop = true;
            }

            RebuildLookup();
        }

        public void RebuildLookup()
        {
            _bgmLookup.Clear();
            for (int i = 0; i < bgmLibrary.Count; i++)
            {
                var entry = bgmLibrary[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.clip == null)
                    continue;
                _bgmLookup[entry.key] = entry;
            }
        }

        public void PlayBgm(string key, float fadeSeconds = -1f)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                StopBgm(fadeSeconds);
                return;
            }

            if (!_bgmLookup.TryGetValue(key, out var entry))
            {
                Debug.LogWarning($"[BgmManager] 키 '{key}'에 해당하는 BGM을 찾을 수 없습니다.");
                return;
            }

            PlayBgm(entry.clip, entry.volume, fadeSeconds, key);
        }

        public void PlayBgm(AudioClip clip, float targetVolume = 1f, float fadeSeconds = -1f, string key = null)
        {
            if (!clip)
            {
                StopBgm(fadeSeconds);
                return;
            }

            if (!audioSource)
            {
                Debug.LogWarning("[BgmManager] AudioSource가 없습니다.");
                return;
            }

            if (audioSource.clip == clip && audioSource.isPlaying)
            {
                _currentKey = key;
                return;
            }

            fadeSeconds = ResolveFadeSeconds(fadeSeconds);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeInNewClip(clip, Mathf.Clamp01(targetVolume), fadeSeconds, key));
        }

        public void StopBgm(float fadeSeconds = -1f)
        {
            if (!audioSource || !audioSource.isPlaying)
                return;

            fadeSeconds = ResolveFadeSeconds(fadeSeconds);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            if (fadeSeconds <= 0f)
            {
                audioSource.Stop();
                audioSource.clip = null;
                _currentKey = null;
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeOutCurrentClip(fadeSeconds));
        }

        public string GetCurrentKey() => _currentKey;

        private float ResolveFadeSeconds(float fadeSeconds)
        {
            if (fadeSeconds < 0f)
                fadeSeconds = defaultFadeSeconds;
            return Mathf.Max(0f, fadeSeconds);
        }

        private IEnumerator FadeInNewClip(AudioClip newClip, float targetVolume, float fadeSeconds, string key)
        {
            float originalVolume = audioSource.volume;
            float elapsed = 0f;

            if (audioSource.isPlaying && fadeSeconds > 0f)
            {
                while (elapsed < fadeSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeSeconds);
                    audioSource.volume = Mathf.Lerp(originalVolume, 0f, t);
                    yield return null;
                }
            }

            audioSource.Stop();
            audioSource.clip = newClip;
            audioSource.volume = 0f;
            audioSource.loop = true;
            audioSource.Play();

            _currentKey = key;

            elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                audioSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            audioSource.volume = targetVolume;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutCurrentClip(float fadeSeconds)
        {
            float startVolume = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = 0f;
            _currentKey = null;
            _fadeCoroutine = null;
        }
    }
}

