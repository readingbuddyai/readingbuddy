using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HomeStageAudioPlayer : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource audioSource;

    [Header("오디오 클립 세트")]
    public AudioClip[] firstVisitClips;
    public AudioClip[] returnVisitClips;

    [Header("설정")]
    [Tooltip("클립 사이 간격 (초)")]
    public float delayBetweenClips = 0.3f;

    // 씬 재진입 시 중복 재생 방지용
    private static bool playedOnce = false;

    private MageHomeCutscene activeCutscene;

    private void Start()
    {
        // 이미 한 번 재생된 적 있으면 바로 종료
        if (playedOnce)
        {
            Debug.Log("[AudioPlayer] 이미 HomeStageAudioPlayer가 한 번 재생되어 다시 실행하지 않습니다.");
            return;
        }

        StartCoroutine(PlayStageAudioSequence());
    }

    private IEnumerator PlayStageAudioSequence()
    {
        // 1. 캐시된 스테이지 확인
        string stage = "";
        try { stage = HomeStageInitializer.LastStage; }
        catch { }

        if (string.IsNullOrEmpty(stage))
            stage = PlayerPrefs.GetString("lastStage", "");

        bool isFirstVisit = stage == "마지막으로 플레이한 스테이지가 없습니다";

        // 2. 현재 스테이지에 맞는 캐릭터 컷신 자동 탐색
        activeCutscene = FindActiveCutsceneByStage(stage);
        if (activeCutscene == null)
        {
            Debug.Log("[AudioPlayer] 활성 캐릭터 컷신을 찾지 못했습니다. 재생 생략.");
            yield break;
        }

        // 캐릭터가 비활성 상태라면 아무 것도 하지 않음
        if (!activeCutscene.gameObject.activeInHierarchy)
        {
            Debug.Log("[AudioPlayer] 캐릭터가 비활성 상태이므로 오디오 재생을 생략합니다.");
            yield break;
        }

        // 3. 오디오 세트 선택
        AudioClip[] clipsToPlay = isFirstVisit ? firstVisitClips : returnVisitClips;
        Debug.Log($"[AudioPlayer] {(isFirstVisit ? "첫 방문" : "재방문")} 오디오 재생 시작");

        // 재생 플래그 설정 (한 번만 실행되도록)
        playedOnce = true;

        // 4. 오디오 순차 재생
        for (int i = 0; i < clipsToPlay.Length; i++)
        {
            var clip = clipsToPlay[i];
            if (clip == null) continue;

            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[AudioPlayer] Clip {i} 재생 시작: {clip.name}");

            // 특정 시점에 캐릭터 컷신 트리거
            if (isFirstVisit && i == 3)
            {
                yield return new WaitForSeconds(clip.length * 0.5f);
                TriggerCharacterRun();
            }
            else if (!isFirstVisit && i == 0)
            {
                yield return new WaitForSeconds(clip.length * 0.5f);
                TriggerCharacterRun();
            }

            yield return new WaitWhile(() => audioSource.isPlaying);
            if (delayBetweenClips > 0f)
                yield return new WaitForSeconds(delayBetweenClips);
        }

        Debug.Log("[AudioPlayer] 모든 오디오 클립 재생 완료");
    }

    private void TriggerCharacterRun()
    {
        if (activeCutscene == null)
        {
            Debug.LogWarning("[AudioPlayer] 컷신이 지정되지 않아 실행할 수 없습니다.");
            return;
        }

        activeCutscene.StartRunNow();
        Debug.Log($"[AudioPlayer] {activeCutscene.gameObject.name} 컷신 실행됨");
    }

    // 현재 스테이지 정보를 기반으로 캐릭터 컷신 자동 탐색
    private MageHomeCutscene FindActiveCutsceneByStage(string stage)
    {
        MageHomeCutscene[] all = FindObjectsOfType<MageHomeCutscene>(true);
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("[AudioPlayer] MageHomeCutscene이 씬에 존재하지 않습니다.");
            return null;
        }

        char first = string.IsNullOrEmpty(stage) ? '1' : stage[0];
        string targetName = "mage";
        if (first == '2' || first == '3') targetName = "stage2char";
        else if (first == '4') targetName = "stage4char";

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

    // 필요 시 다른 코드에서 호출해서 초기화할 수 있도록
    public static void ResetAudioFlag()
    {
        playedOnce = false;
    }
}
