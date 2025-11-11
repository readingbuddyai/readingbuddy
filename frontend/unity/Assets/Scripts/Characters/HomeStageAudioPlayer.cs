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
    public float delayBetweenClips = 0.3f;

    private MageHomeCutscene activeCutscene;

    private IEnumerator Start()
    {
        // 씬이 완전히 로드되고 캐릭터가 Awake/Start를 끝낼 때까지 잠시 대기
        yield return new WaitForSeconds(0.8f);

        // 1. 스테이지 정보 확인
        string stage = "";
        try { stage = HomeStageInitializer.LastStage; }
        catch { }
        if (string.IsNullOrEmpty(stage))
            stage = PlayerPrefs.GetString("lastStage", "");

        // 2. 컷신 자동 탐색 (씬 내에서)
        activeCutscene = FindActiveCutsceneByStage(stage);

        if (activeCutscene == null)
        {
            Debug.LogWarning("[AudioPlayer] 컷신을 자동으로 찾지 못했습니다. 종료합니다.");
            yield break;
        }

        // 캐릭터가 비활성 상태면 재생하지 않음
        if (!activeCutscene.gameObject.activeInHierarchy)
        {
            Debug.Log("[AudioPlayer] 캐릭터가 비활성 상태이므로 오디오 재생을 생략합니다.");
            yield break;
        }

        // 3. 방문 여부 판별
        bool isFirstVisit = stage == "마지막으로 플레이한 스테이지가 없습니다";
        AudioClip[] clipsToPlay = isFirstVisit ? firstVisitClips : returnVisitClips;

        Debug.Log($"[AudioPlayer] {(isFirstVisit ? "첫 방문" : "재방문")} 오디오 재생 시작");

        // 4. 순차 재생
        for (int i = 0; i < clipsToPlay.Length; i++)
        {
            var clip = clipsToPlay[i];
            if (clip == null) continue;

            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[AudioPlayer] Clip {i} 재생 시작: {clip.name}");

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

            yield return new WaitWhile(() => audioSource.isPlaying);
            if (delayBetweenClips > 0f)
                yield return new WaitForSeconds(delayBetweenClips);
        }

        Debug.Log("[AudioPlayer] 모든 오디오 클립 재생 완료");
    }

    private MageHomeCutscene FindActiveCutsceneByStage(string stage)
    {
        MageHomeCutscene[] all = FindObjectsOfType<MageHomeCutscene>(true);
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("[AudioPlayer] 씬에 MageHomeCutscene이 없습니다.");
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
}
