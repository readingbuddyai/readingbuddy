using System;
using UnityEngine;

// Base URL 우선순위 로더
// 1) OS 환경변수 API_BASE_URL
// 2) Resources/api_config.json 의 baseUrl
// 3) 인스펙터에 지정된 값 그대로 사용
public static class EnvConfig
{
    [Serializable]
    private class ApiConfigJson { public string baseUrl; }

    public static string ResolveBaseUrl(string inspectorValue)
    {
        // 1) ENV 우선
        var env = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();

        // 2) Resources (로컬 우선: api_config_local.json → 공용: api_config.json)
        var text = LoadResourceText("api_config_local");
        if (string.IsNullOrWhiteSpace(text))
            text = LoadResourceText("api_config");

        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                var obj = JsonUtility.FromJson<ApiConfigJson>(text);
                if (obj != null && !string.IsNullOrWhiteSpace(obj.baseUrl))
                    return obj.baseUrl.Trim();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EnvConfig] api_config*.json 파싱 실패: {e.Message}");
            }
        }

        // 3) 인스펙터 값
        return inspectorValue;
    }

    private static string LoadResourceText(string name)
    {
        var ta = Resources.Load<TextAsset>(name);
        return ta != null ? ta.text : null;
    }
}
