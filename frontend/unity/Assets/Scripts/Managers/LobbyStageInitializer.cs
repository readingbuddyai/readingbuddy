using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class LobbyStageInitializer : MonoBehaviour
{
    [Header("캐시 설정")]
    [Tooltip("HomeStageInitializer가 저장한 PlayerPrefs 키")]
    public string stageCacheKey = "lastStage";

    [Header("Lobby 캐릭터 참조")]
    [Tooltip("Lobby 씬의 mage 오브젝트 (비우면 이름으로 자동 탐색)")]
    public GameObject mageRef;
    [Tooltip("Lobby 씬의 stage2char 오브젝트 (비우면 이름으로 자동 탐색)")]
    public GameObject stage2Ref;
    [Tooltip("Lobby 씬의 stage4char 오브젝트 (비우면 이름으로 자동 탐색)")]
    public GameObject stage4Ref;

    [Header("포털(목적지) 참조 - 선택")]
    [Tooltip("Mage 프로필용 포털 또는 대기 위치")]
    public Transform magePortalRef;
    [Tooltip("Stage2Char 프로필용 포털 또는 대기 위치")]
    public Transform stage2PortalRef;
    [Tooltip("Stage3Char 프로필용 포털 또는 대기 위치")]
    public Transform stage3PortalRef;
    [Tooltip("Stage4Char 프로필용 포털 또는 대기 위치")]
    public Transform stage4PortalRef;

    [Header("이동 설정")]
    [Tooltip("NavMeshAgent가 없을 때 단순 이동 속도(m/s)")]
    public float fallbackMoveSpeed = 3.5f;
    [Tooltip("목적지 도착 판정 거리")]
    public float arriveThreshold = 0.3f;

    private GameObject _activeChar;
    private Transform _target;

    private void Start()
    {
        // 1) 캐시된 스테이지 결정
        string stage = string.Empty;
        try
        {
            stage = HomeStageInitializer.LastStage;
        }
        catch { /* HomeStageInitializer가 없어도 무시 */ }

        if (string.IsNullOrWhiteSpace(stage))
        {
            stage = PlayerPrefs.GetString(stageCacheKey, string.Empty);
        }

        // 2) 프로필 판정 및 캐릭터 토글
        var profile = ResolveProfileSafe(stage);
        ToggleCharactersAndRun(profile, stage);
        Debug.Log($"[LobbyStage] stage '{stage}' → {profile}");

        // 3) 목적지 선택 후 이동 시도
        _target = GetPortalFor(profile, stage);
        if (_activeChar != null && _target != null)
        {
            StartCoroutine(CoMoveToTarget(_activeChar, _target));
        }
        else
        {
            Debug.Log("[LobbyStage] 이동 대상 또는 목적지가 없어 이동을 생략합니다.");
        }
    }

    private IEnumerator CoMoveToTarget(GameObject agentGo, Transform target)
    {
        var agent = agentGo.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            while (agent.enabled && !agent.pathPending)
            {
                if (!agent.hasPath || agent.remainingDistance <= arriveThreshold)
                    break;
                yield return null;
            }
            agent.isStopped = true;
            yield break;
        }

        // NavMeshAgent가 없으면 단순 이동
        var tr = agentGo.transform;
        while (true)
        {
            var pos = tr.position;
            var dest = target.position;
            var dir = dest - pos;
            var dist = dir.magnitude;
            if (dist <= arriveThreshold) break;
            var step = fallbackMoveSpeed * Time.deltaTime;
            tr.position = Vector3.MoveTowards(pos, dest, step);
            yield return null;
        }
    }

    private HomeStageInitializer.HomeProfile ResolveProfileSafe(string stage)
    {
        try
        {
            return HomeStageInitializer.ResolveProfile(stage);
        }
        catch
        {
            // HomeStageInitializer가 없거나 접근 불가한 경우 동일 로직 복제
            if (string.IsNullOrWhiteSpace(stage)) return HomeStageInitializer.HomeProfile.Mage;
            if (string.Equals(stage, "마지막으로 플레이한 스테이지가 없습니다", StringComparison.Ordinal))
                return HomeStageInitializer.HomeProfile.Mage;
            char first = stage[0];
            if (first == '1') return HomeStageInitializer.HomeProfile.Mage;
            if (first == '2' || first == '3') return HomeStageInitializer.HomeProfile.Stage2Char;
            if (first == '4') return HomeStageInitializer.HomeProfile.Stage4Char;
            return HomeStageInitializer.HomeProfile.Mage;
        }
    }

    private Transform GetPortalFor(HomeStageInitializer.HomeProfile profile, string stage)
    {
        // stage 첫 글자 확인
        char first = !string.IsNullOrEmpty(stage) ? stage[0] : '0';

        switch (profile)
        {
            case HomeStageInitializer.HomeProfile.Mage:
                return magePortalRef;

            case HomeStageInitializer.HomeProfile.Stage2Char:
                if (first == '2')
                    return stage2PortalRef;  // 2번 스테이지 목적지
                if (first == '3')
                    return stage3PortalRef;  // 3번 스테이지 목적지 (새로 추가)
                break;

            case HomeStageInitializer.HomeProfile.Stage4Char:
                return stage4PortalRef;
        }

        return null;
    }

    private void ToggleCharactersAndRun(HomeStageInitializer.HomeProfile profile, string stage)
    {
        EnsureCharacterRefs();

        GameObject targetChar = null;
        switch (profile)
        {
            case HomeStageInitializer.HomeProfile.Mage:
                mageRef?.SetActive(true);
                stage2Ref?.SetActive(false);
                stage4Ref?.SetActive(false);
                targetChar = mageRef;
                break;

            case HomeStageInitializer.HomeProfile.Stage2Char:
                mageRef?.SetActive(false);
                stage2Ref?.SetActive(true);
                stage4Ref?.SetActive(false);
                targetChar = stage2Ref;
                break;

            case HomeStageInitializer.HomeProfile.Stage4Char:
                mageRef?.SetActive(false);
                stage2Ref?.SetActive(false);
                stage4Ref?.SetActive(true);
                targetChar = stage4Ref;
                break;
        }

        // 캐릭터에 달린 LobbyRunToPortal 스크립트를 찾는다
        if (targetChar != null)
        {
            var runScript = targetChar.GetComponent<LobbyRunToPortal>();
            if (runScript != null)
            {
                // 스테이지 첫 글자로 목적지 구분
                char first = !string.IsNullOrEmpty(stage) ? stage[0] : '0';

                if (first == '1')
                    runScript.target = magePortalRef;
                else if (first == '2')
                    runScript.target = stage2PortalRef;
                else if (first == '3')
                    runScript.target = stage3PortalRef;   // 새로 추가한 포털
                else if (first == '4')
                    runScript.target = stage4PortalRef;
                else
                    runScript.target = null;

                // 이동 시작
                runScript.enabled = true;
            }
        }
    }

    private Transform ResolveTarget(string stage)
    {
        char first = stage.Length > 0 ? stage[0] : '0';
        if (first == '1') return magePortalRef;
        if (first == '2') return stage2PortalRef;
        if (first == '3') return stage3PortalRef;
        if (first == '4') return stage4PortalRef;
        return null;
    }


    private void EnsureCharacterRefs()
    {
        if (mageRef != null && stage2Ref != null && stage4Ref != null) return;
        // 비활성 포함 씬 내 객체 탐색
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            var go = t.gameObject;
            if (!go.scene.IsValid()) continue;
            if (mageRef == null && string.Equals(go.name, "mage", StringComparison.Ordinal)) mageRef = go;
            else if (stage2Ref == null && string.Equals(go.name, "stage2char", StringComparison.Ordinal)) stage2Ref = go;
            else if (stage4Ref == null && string.Equals(go.name, "stage4char", StringComparison.Ordinal)) stage4Ref = go;
        }
    }
}

