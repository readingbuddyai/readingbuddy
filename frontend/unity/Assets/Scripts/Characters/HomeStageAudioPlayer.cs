using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; 
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HomeStageAudioPlayer : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource audioSource;

    [Header("오디오 클립 세트")]
    public AudioClip[] firstVisitClips;
    public AudioClip[] returnVisitClips;

    [Header("설정")]
    public float delayBetweenClips = 0.3f;
    private static readonly HashSet<string> sPlayedStages = new();
    private MageHomeCutscene activeCutscene;
    private bool _playedOnce = false; // 중복 재생 방지

    private void OnEnable()
    {
        // 1) 스테이지 적용 이벤트 구독
        HomeStageInitializer.OnStageApplied += HandleStageApplied;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // 2) 이미 한 번 적용된 상태(Sticky)가 있으면 즉시 시작
        if (HomeStageInitializer.TryGetLastApplied(out var stage, out var profile))
        {
            // ★ 이미 이 스테이지에서 오디오를 틀었다면 즉시 종료
            if (sPlayedStages.Contains(stage)) return;

            // 캐릭터가 실제로 켜져있을 때만 시도
            if (IsAnyCharacterActive())
                HandleStageApplied(stage, profile);
        }
    }
    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        StopAllCoroutines();
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        _playedOnce = false; // 같은 세션에서 다른 스테이지가 올 수도 있으니 인스턴스 플래그는 리셋
    }

    private void OnDisable()
    {
        HomeStageInitializer.OnStageApplied -= HandleStageApplied;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void HandleStageApplied(string stage, HomeStageInitializer.HomeProfile profile)
    {
        if (_playedOnce || sPlayedStages.Contains(stage)) return;
        if (!IsAnyCharacterActive()) return;

        // 캐릭터가 ToggleCharacters로 활성화된 이후 시점
        activeCutscene = FindActiveCutsceneByStage(stage);
        if (activeCutscene == null || !activeCutscene.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[AudioPlayer] 활성 컷신을 찾지 못해 오디오를 생략합니다.");
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("[AudioPlayer] AudioSource가 없습니다.");
                return;
            }
        }

        StartCoroutine(PlaySequence(stage));
    }

    private IEnumerator PlaySequence(string stage)
    {
        _playedOnce = true;
        sPlayedStages.Add(stage);

        // 첫 방문 판별
        bool isFirstVisit = stage == "마지막으로 플레이한 스테이지가 없습니다";
        AudioClip[] clipsToPlay = isFirstVisit ? firstVisitClips : returnVisitClips;

        if (clipsToPlay == null || clipsToPlay.Length == 0)
        {
            Debug.Log("[AudioPlayer] 재생할 클립이 없습니다.");
            yield break;
        }

        Debug.Log($"[AudioPlayer] {(isFirstVisit ? "첫 방문" : "재방문")} 오디오 재생 시작");

        for (int i = 0; i < clipsToPlay.Length; i++)
        {
            var clip = clipsToPlay[i];
            if (clip == null) continue;

            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[AudioPlayer] Clip {i} 재생 시작: {clip.name}");

            // 컷신 트리거 타이밍 (기존 Start() 로직 동일)
            if (isFirstVisit && i == 3)
            {
                yield return new WaitForSeconds(clip.length * 0.5f);
                activeCutscene.StartRunNow();
            }
            else if (!isFirstVisit && i == 0)
            {
                yield return new WaitForSeconds(clip.length * 0.5f);
                activeCutscene.StartRunNow();
            }

            // 클립 종료까지 대기
            yield return new WaitWhile(() => audioSource.isPlaying);
            if (delayBetweenClips > 0f)
                yield return new WaitForSeconds(delayBetweenClips);
        }

        Debug.Log("[AudioPlayer] 모든 오디오 클립 재생 완료");
    }

    private MageHomeCutscene FindActiveCutsceneByStage(string stage)
    {
        // 스테이지 첫 글자로 캐릭터 선택 (HomeStageInitializer와 동일 기준)
        char first = string.IsNullOrEmpty(stage) ? '1' : stage[0];
        string targetName = "mage";
        if (first == '2' || first == '3') targetName = "stage2char";
        else if (first == '4') targetName = "stage4char";

        var all = FindObjectsOfType<MageHomeCutscene>(true);
        foreach (var cutscene in all)
        {
            if (cutscene.gameObject.name == targetName)
            {
                Debug.Log($"[AudioPlayer] 자동 연결 성공 → {targetName}");
                return cutscene;
            }
        }

        Debug.LogWarning($"[AudioPlayer] '{targetName}' 캐릭터를 찾지 못했습니다.");
        return null;
    }
        
    // 클래스 내 아무 곳
    private bool IsAnyCharacterActive()
    {
        // 이름 기준으로 찾습니다. 필요하면 이름을 프로젝트에 맞게 조정하세요.
        var mage  = GameObject.Find("mage");
        var s2    = GameObject.Find("stage2char");
        var s4    = GameObject.Find("stage4char");

        // activeInHierarchy로 실제 활성(부모까지 포함) 여부 확인
        if (mage != null  && mage.activeInHierarchy)  return true;
        if (s2   != null  && s2.activeInHierarchy)    return true;
        if (s4   != null  && s4.activeInHierarchy)    return true;
        return false;
    }
}
