using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class StageAudioController
{
    private StageAudioDependencies _deps;

    public void Initialize(StageAudioDependencies deps)
    {
        _deps = deps ?? throw new ArgumentNullException(nameof(deps));
    }

    public IEnumerator PlayClip(AudioClip clip)
    {
        EnsureInitialized();
        var source = _deps.AudioSource;
        if (!clip || !source)
            yield break;

        source.Stop();
        source.clip = clip;
        source.Play();
        yield return new WaitWhile(() => source.isPlaying);
    }

    public IEnumerator PlayVoiceUrl(string voiceUrl)
    {
        EnsureInitialized();
        var source = _deps.AudioSource;
        if (string.IsNullOrEmpty(voiceUrl) || !source)
            yield break;

        string safeUrl = EncodePlusInPath(voiceUrl);
        var audioType = GuessAudioType(safeUrl);
        using (var req = UnityWebRequestMultimedia.GetAudioClip(safeUrl, audioType))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                _deps.LogWarning?.Invoke($"[StageAudio] 음성 로드 실패: {req.error}\nURL(raw)={voiceUrl}\nURL(safe)={safeUrl}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            source.Stop();
            source.clip = clip;
            source.Play();
            yield return new WaitWhile(() => source.isPlaying);
        }
    }

    public void Stop()
    {
        if (_deps?.AudioSource)
            _deps.AudioSource.Stop();
    }

    public AudioType GuessAudioType(string url)
    {
        if (string.IsNullOrEmpty(url))
            return AudioType.UNKNOWN;
        url = url.ToLowerInvariant();
        if (url.EndsWith(".mp3")) return AudioType.MPEG;
        if (url.EndsWith(".wav") || url.EndsWith(".wave")) return AudioType.WAV;
        if (url.EndsWith(".ogg")) return AudioType.OGGVORBIS;
        return AudioType.UNKNOWN;
    }

    public string EncodePlusInPath(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        try
        {
            int q = url.IndexOf('?');
            int schemeIdx = url.IndexOf("://");
            int pathStart;
            if (schemeIdx >= 0)
            {
                pathStart = url.IndexOf('/', schemeIdx + 3);
                if (pathStart < 0)
                    return url;
            }
            else
            {
                pathStart = 0;
            }

            int pathEnd = (q >= 0) ? q : url.Length;
            if (pathEnd <= pathStart) return url;

            string prefix = url.Substring(0, pathStart);
            string path = url.Substring(pathStart, pathEnd - pathStart);
            string suffix = (q >= 0) ? url.Substring(q) : string.Empty;

            path = path.Replace("+", "%2B");
            return prefix + path + suffix;
        }
        catch
        {
            return url;
        }
    }

    private void EnsureInitialized()
    {
        if (_deps == null)
            throw new InvalidOperationException("StageAudioController is not initialized. Call Initialize() first.");
    }
}

public class StageAudioDependencies
{
    public AudioSource AudioSource;
    public Action<string> Log;
    public Action<string> LogWarning;
}

