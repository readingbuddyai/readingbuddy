using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 씬/스테이지별 BGM 설정을 한 곳에 관리하는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Scene Stage BGM Config", fileName = "SceneStageBgmConfig")]
    public class SceneStageBgmConfig : ScriptableObject
    {
        [Tooltip("씬/스테이지 이름과 매핑되는 BGM 설정 목록")]
        public SceneBgmEntry[] entries;
        public AudioClip defaultClip;
        [Range(0f, 1f)] public float defaultVolume = 1f;
        public bool defaultLoop = true;

        public bool TryGetEntry(string name, out SceneBgmEntry entry)
        {
            entry = null;
            if (entries == null)
                return false;
            for (int i = 0; i < entries.Length; i++)
            {
                var candidate = entries[i];
                if (candidate != null && candidate.IsMatch(name))
                {
                    entry = candidate;
                    return true;
                }
            }
            return false;
        }
    }

    [System.Serializable]
    public class SceneBgmEntry
    {
        [Tooltip("씬 이름 또는 스테이지 명칭. 대소문자 구분 없음.")]
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = true;

        public bool IsMatch(string name)
        {
            return !string.IsNullOrWhiteSpace(key) &&
                   !string.IsNullOrWhiteSpace(name) &&
                   string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}

