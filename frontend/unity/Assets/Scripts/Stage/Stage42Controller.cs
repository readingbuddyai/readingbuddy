using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Stage 4.2 Controller (합성 마법)
public partial class Stage42Controller : MonoBehaviour
{
    [Header("API 설정")]
    public string baseUrl = "https://readingbuddyai.co.kr";
    public string stageSet = "4.2";       // GET /api/train/set?stage=4&count=N
    public string stageTwoPart = "4.2";    // POST /api/train/stage/* & check/voice
    public int count = 5;
    public string authToken = "";

    [Header("세션")]
    public string stageSessionId = "";

    [Header("UI")]
    public TMP_Text progressText;
    public Image guideImage;
    public RectTransform guideRect;
    public GameObject guide3DCharacter;
    public TMP_Text wordText;
    public TMP_Text choseongText;
    public TMP_Text jungseongText;
    public TMP_Text jongseongText;
    public GameObject choseongBox;
    public GameObject jungseongBox;
    public GameObject jongseongBox;
    public GameObject micIndicator;
    public GameObject choicesContainer;
    public GameObject consonantChoicesContainer;
    public GameObject vowelChoicesContainer;

    [Header("가이드 이동")]
    public Vector2 guideStartSize = new Vector2(1500, 1500);
    public Vector2 guideEndSize = new Vector2(600, 600);
    public Vector2 guideEndAnchoredPos = new Vector2(650, -350);
    public float guideMoveDuration = 1.5f;
    public bool enableGuideMoveBetweenQuestions = false;
    private bool _guideMoved;

    [Header("End Panel (Level1-style)")]
    public Button againButtonPrefab;
    public Button lobbyButtonPrefab;

    [Header("오디오 재생")]
    public AudioSource audioSource;
    public AudioClip sfxStart;
    public AudioClip sfxNext;
    [Range(0f, 1f)] public float slowVoiceVolume = 1.0f;
    [Range(0f, 1f)] public float localClipVolume = 1.0f;
    [Range(0f, 1f)] public float localSfxVolume = 1.0f;

    // 4.2 시나리오 대사 오디오
    public AudioClip clipIntroCompositeMagic;   // [4.2.1]
    public AudioClip clipIntroListenAndChoose;  // [4.2.2]
    public AudioClip clipIntroMakeStrongSpell;  // [4.2.3]
    public AudioClip clipReady;                 // [4.2.4.1]
    public AudioClip clipWillPlaySpell;         // [4.2.4.2]
    public AudioClip clipYourTurnRepeat;        // [4.2.5]
    public AudioClip clipNowFillBox;            // [4.2.6]
    public AudioClip clipCorrectPlaced;         // [4.2.7.1]
    public AudioClip clipWrongTryMatch;         // [4.2.7.2]
    public AudioClip clipThinkAgain;            // [4.2.7.3]
    public AudioClip clipItsOkay;               // [4.1.15] its okay encouragement
    public AudioClip clipMemorizeSpell;         // [4.2.11]
    public AudioClip clipFinishCongrats;        // [4.2.8]
    public AudioClip clipYouAreWizard;          // [4.2.9]
    public AudioClip clipNextTraining;          // [4.2.10]

    [Header("녹음 설정")]
    public int recordSeconds = 3;
    public int recordSampleRate = 44100;

    [Header("연출 설정")]
    [Range(0f, 1f)] public float dimAlpha = 0.4f;
    [Range(0f, 1f)] public float blinkAlphaMin = 0.4f;
    [Range(0f, 1f)] public float blinkAlphaMax = 1.0f;
    public float blinkPeriod = 0.6f;

    [Header("통신/로깅")]
    public bool bypassStartRequest = false;
    public bool logVerbose = true;
    [Tooltip("[4.2.11] 구간에서 check/voice POST 전송 여부 (테스트용)")]
    public bool enableVoicePost = true;

    // 상태
    private int _currentProblemNumber = 0; // 1-based
    private string _currentProblemWord = string.Empty;
    private string _currentSlowVoiceUrl = string.Empty;
    private List<string> _expectedPhonemes = new List<string>(); // index 0=초,1=중,2=종
    private int _expectedSegmentCount = 0; // 2 or 3
    private bool[] _finalizedSlots = new bool[3];
    private Coroutine _blinkCo;
    private GameObject _focusedBox;
    private bool _awaitingUserArrangement;
    private int[] _attemptsPerSlot = new int[3];
    private bool _inInitialFillPhase;
    private int _initialSlotIndex = 0; // 0=초,1=중,2=종
    private bool _inCorrectionPhase;
    private int _currentCorrectionSlot = -1;

    [Serializable]
    public class QuestionDto
    {
        public int questionId;
        public string problemWord;
        public string slowVoiceUrl; // 느린 발음(4.2에서 재생)
        public int answerCnt;
        public string imageUrl;
        public List<string> phonemes; // 0=초,1=중,2=종
    }

    [Serializable]
    public class QuestionData
    {
        public string stageSessionId;
        public List<QuestionDto> problems;
        public int totalProblems;
    }

    [Serializable]
    public class QuestionListResponse
    {
        public bool success;
        public string message;
        public QuestionData data;
    }

    [Serializable]
    private class StartStageData { public string stageSessionId; }
    [Serializable]
    private class StartStageResp { public bool success = true; public string message; public StartStageData data; }

    private void Start()
    {
        baseUrl = EnvConfig.ResolveBaseUrl(baseUrl);
        authToken = EnvConfig.ResolveAuthToken(authToken);
        if (guideRect && guideStartSize.sqrMagnitude > 0)
            guideRect.sizeDelta = guideStartSize;
        if (logVerbose)
        {
            Debug.Log($"[Stage42] Start baseUrl='{baseUrl}', stageSet='{stageSet}', stageTwoPart='{stageTwoPart}', count={count}, bypassStartRequest={bypassStartRequest}");
        }
        ConfigureStageModules();
        _tutorialController?.PrepareForStageStart();

        ResetUI();
        StartCoroutine(RunStage());
    }

    private void EnsureGameplayUiVisible()
    {
        if (choseongBox) choseongBox.SetActive(true);
        if (jungseongBox) jungseongBox.SetActive(true);
        if (jongseongBox) jongseongBox.SetActive(true);
        if (choicesContainer) choicesContainer.SetActive(true);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(true);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(true);
    }

    private void ResetUI()
    {
        if (progressText) progressText.text = string.Empty;
        if (wordText) wordText.text = string.Empty;
        if (choseongText) choseongText.text = string.Empty;
        if (jungseongText) jungseongText.text = string.Empty;
        if (jongseongText) jongseongText.text = string.Empty;
        if (choseongBox) choseongBox.SetActive(true);
        if (jungseongBox) jungseongBox.SetActive(true);
        if (jongseongBox) jongseongBox.SetActive(true);
        if (micIndicator) micIndicator.SetActive(false);
        if (choicesContainer) choicesContainer.SetActive(false);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
        FocusBox(null);
        SetAllBoxAlpha(dimAlpha);
    }

    private IEnumerator RunStage()
    {
        // 도입
        _tutorialController?.ResetAfterStageRestart();
        yield return PlayClip(sfxStart);
        if (_tutorialController != null)
        {
            yield return _tutorialController.RunIntroSequence();
            yield return _tutorialController.RunIntroTutorial();
        }
        EnsureGameplayUiVisible();
        yield return PlayClip(clipIntroCompositeMagic);   // [4.2.1]
        yield return PlayClip(clipIntroListenAndChoose);  // [4.2.2]
        yield return PlayClip(clipIntroMakeStrongSpell);  // [4.2.3]

        // Ensure session for set API (stageSessionId is required by server)
        if (!bypassStartRequest && string.IsNullOrWhiteSpace(stageSessionId))
            yield return StartStageSession();

        // 문제 세트 요청
        List<QuestionDto> questions = null;
        if (logVerbose) Debug.Log("[Stage42] Fetching questions via set API...");
        yield return StartCoroutine(FetchQuestions(result => questions = result));
        if (questions == null || questions.Count == 0) yield break;

        _questionController.SetQuestions(questions);

        if ((!_guideMoved || enableGuideMoveBetweenQuestions) && guideRect)
            yield return MoveGuideToCorner();

        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            _currentProblemNumber = i + 1;
            _currentProblemWord = q.problemWord ?? string.Empty;
            _currentSlowVoiceUrl = q.slowVoiceUrl ?? string.Empty;
            _expectedPhonemes = (q.phonemes != null) ? new List<string>(q.phonemes) : new List<string>();
            _expectedSegmentCount = (q.answerCnt > 0) ? q.answerCnt : (_expectedPhonemes != null ? _expectedPhonemes.Count : 3);
            Array.Clear(_finalizedSlots, 0, _finalizedSlots.Length);
            for (int k = 0; k < _attemptsPerSlot.Length; k++) _attemptsPerSlot[k] = 0;

            SetProgressLabel(_currentProblemNumber, questions.Count);
            if (wordText) wordText.text = q.problemWord ?? string.Empty;
            ClearPhonemeBoxes();

            // [4.2.4] 준비/주문 들려주기 + 문제 음성
            if (clipReady) yield return PlayClip(clipReady);                        // [4.2.4.1]
            if (clipWillPlaySpell) yield return PlayClip(clipWillPlaySpell);        // [4.2.4.2]
            // 4.2는 느린 발음만 재생 (폴백 없음)
            yield return PlayVoiceUrl(q.slowVoiceUrl);

            // [4.2.5] 사용자 모방(3초 녹음, 업로드 없음)
            if (clipYourTurnRepeat) yield return PlayClip(clipYourTurnRepeat);
            yield return RecordMicOnly(recordSeconds, recordSampleRate);

            // [4.2.6] 채우기 안내 → 초/중/종 순차 채우기, 마지막까지 채우면 판정
            if (clipNowFillBox) yield return PlayClip(clipNowFillBox);
            BeginInitialFill();
            // 대기: 초기 채우기 완료 후 판정까지 진행
            while (_inInitialFillPhase) yield return null;
            // Wait for correction flow to finish and all slots finalized
            while (_inCorrectionPhase) yield return null;
            while (!AreAllSlotsFinalized()) yield return null;

            // [4.2.7.1] 정답 멘트(항상 대기하며 재생)
            if (clipCorrectPlaced) yield return PlayClip(clipCorrectPlaced);
            // [4.2.11] 문제별 단어 발음 안내 및 음성 업로드 (테스트 시 업로드 비활성화 가능)
            if (clipMemorizeSpell) yield return PlayClip(clipMemorizeSpell);
            if (enableVoicePost)
                yield return RecordAndUploadFinalVoice();
            else
                yield return RecordMicOnly(recordSeconds, recordSampleRate);

            if (i < questions.Count - 1)
                yield return PlayClip(sfxNext);
        }

        // complete moved to after [4.2.10]

        // [4.2.11] 완성 주문 외우기 → 3초 녹음 업로드
        // 문제별로 처리됨: 스테이지 종료 시에는 생략

        // 마무리 멘트
        if (clipFinishCongrats) yield return PlayClip(clipFinishCongrats); // [4.2.8]
        if (clipYouAreWizard) yield return PlayClip(clipYouAreWizard);     // [4.2.9]
        if (clipNextTraining) yield return PlayClip(clipNextTraining);     // [4.2.10]
        // After all training lines, complete the stage session
        if (!string.IsNullOrWhiteSpace(stageSessionId))
            yield return CompleteStageSession();

        // Show end panel for user choice (restart/lobby)
        ShowEndPanel();
    }

    private IEnumerator CorrectionFlow42()
    {
        _inCorrectionPhase = true;
        bool slowReplayed = false; // replay slow voice once during correction
        for (int slot = 0; slot < _expectedSegmentCount; slot++)
        {
            if (_finalizedSlots[slot]) continue;
            _currentCorrectionSlot = slot;

            // 포커스 및 패널 표시
            if (slot == 0 && choseongBox) FocusBox(choseongBox);
            else if (slot == 1 && jungseongBox) FocusBox(jungseongBox);
            else if (slot == 2 && jongseongBox) FocusBox(jongseongBox);

            ShowChoicePanelsForSlot(slot);
            _awaitingUserArrangement = true;

            if (clipWrongTryMatch) yield return PlayClip(clipWrongTryMatch); // [4.2.7.2]
            if (!slowReplayed && !string.IsNullOrEmpty(_currentSlowVoiceUrl))
            {
                yield return PlayVoiceUrl(_currentSlowVoiceUrl); // replay slow voice once
                slowReplayed = true;
            }

            while (_awaitingUserArrangement)
                yield return null;
        }
        _currentCorrectionSlot = -1;
        _inCorrectionPhase = false;

        FocusBox(null);
        HideChoicePanels();
        if (AreAllSlotsFinalized())
        {
            SetAllBoxAlpha(1f);
            // 정답 멘트[4.2.7.1]은 RunStage에서 중앙집중으로 재생(대기 포함)
        }
    }

    // 슬롯에 맞는 선택 패널(자음/모음) 표시
    private void ShowChoicePanelsForSlot(int slotIndex)
    {
        bool showConsonants = (slotIndex == 0) || (slotIndex == 2);
        bool showVowels = (slotIndex == 1);
        bool showAny = showConsonants || showVowels;

        if (choicesContainer) choicesContainer.SetActive(showAny);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(showConsonants);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(showVowels);

        // 레이아웃 강제 갱신(옵션)
        if (choicesContainer)
        {
            var rt = choicesContainer.GetComponent<RectTransform>();
            if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
        if (consonantChoicesContainer)
        {
            var rtC = consonantChoicesContainer.GetComponent<RectTransform>();
            if (rtC) LayoutRebuilder.ForceRebuildLayoutImmediate(rtC);
        }
        if (vowelChoicesContainer)
        {
            var rtV = vowelChoicesContainer.GetComponent<RectTransform>();
            if (rtV) LayoutRebuilder.ForceRebuildLayoutImmediate(rtV);
        }
    }

    // 모든 선택 패널 숨김
    private void HideChoicePanels()
    {
        if (choicesContainer) choicesContainer.SetActive(false);
        if (consonantChoicesContainer) consonantChoicesContainer.SetActive(false);
        if (vowelChoicesContainer) vowelChoicesContainer.SetActive(false);
    }

    // 필요한 슬롯(초/중/종)이 모두 확정되었는지 확인
    private bool AreAllSlotsFinalized()
    {
        int needed = Mathf.Max(0, _expectedSegmentCount);
        for (int i = 0; i < needed && i < _finalizedSlots.Length; i++)
        {
            if (!_finalizedSlots[i]) return false;
        }
        return needed > 0;
    }

    // 외부(UI)에서 슬롯 드롭 가능 여부 확인
    public bool CanAcceptDropToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return false;
        if (_expectedPhonemes == null || slotIndex >= _expectedPhonemes.Count) return false;
        if (slotIndex < _finalizedSlots.Length && _finalizedSlots[slotIndex]) return false;
        // 초기 채우기 단계에서는 현재 지정된 슬롯만 허용
        if (_inInitialFillPhase) return slotIndex == _initialSlotIndex;
        // 교정 단계에서는 현재 슬롯만 허용
        if (_inCorrectionPhase)
            return _currentCorrectionSlot == slotIndex;
        return _awaitingUserArrangement;
    }

    // 외부(UI)에서 미리 정오 판단용
    public bool IsCorrectForSlot(int slotIndex, string symbol)
    {
        if (_expectedPhonemes == null || slotIndex < 0 || slotIndex >= _expectedPhonemes.Count)
            return false;
        var expected = _expectedPhonemes[slotIndex] ?? string.Empty;
        return string.Equals(NormalizePhoneme(symbol), NormalizePhoneme(expected), StringComparison.Ordinal);
    }

    public void OnUserDrop(int slotIndex, string symbol)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;
        if (_expectedPhonemes == null || slotIndex >= _expectedPhonemes.Count) return;

        string expected = _expectedPhonemes[slotIndex] ?? string.Empty;
        bool attemptCorrect = string.Equals(NormalizePhoneme(symbol), NormalizePhoneme(expected), StringComparison.Ordinal);
        // Per-slot attempt number (초/중/종 각각)
        int attemptNumberForSlot = 1;
        if (_attemptsPerSlot != null && slotIndex >= 0 && slotIndex < _attemptsPerSlot.Length)
            attemptNumberForSlot = Mathf.Clamp(_attemptsPerSlot[slotIndex] + 1, 1, 99);
        // Send attempt log for every drag try
        StartCoroutine(SendAttemptLog42(
            problemNumber: Mathf.Max(1, _currentProblemNumber),
            attemptNumber: attemptNumberForSlot,
            problem: NormalizePhoneme(expected),
            answer: NormalizePhoneme(symbol ?? string.Empty),
            isCorrect: attemptCorrect));
        if (_inInitialFillPhase)
        {
            // 순차 채우기: 현재 지정된 슬롯에만 채움
            if (slotIndex != _initialSlotIndex) return;
            if (audioSource && sfxNext) audioSource.PlayOneShot(sfxNext, Mathf.Clamp01(localSfxVolume));
            SetSlotText(slotIndex, symbol);
            SetSlotAlpha(slotIndex, 1f);

            // 다음 슬롯로 진행
            _initialSlotIndex++;
            if (_initialSlotIndex < _expectedSegmentCount)
            {
                ShowChoicePanelsForSlot(_initialSlotIndex);
                if (_initialSlotIndex == 1 && jungseongBox) FocusBox(jungseongBox);
                else if (_initialSlotIndex == 2 && jongseongBox) FocusBox(jongseongBox);
            }
            else
            {
                // 모든 슬롯 입력 완료 → 최초 판정
                bool allOk = EvaluateAndFinalizeFilled(clearWrongSlots: true);
                _inInitialFillPhase = false;
                if (allOk)
                {
                    // 정답 멘트[4.2.7.1]은 RunStage에서 중앙집중으로 재생(대기 포함)
                    SetAllBoxAlpha(1f);
                }
                else
                {
                    if (clipWrongTryMatch) StartCoroutine(PlayClip(clipWrongTryMatch)); // [4.2.7.2]
                    // 틀린 슬롯만 교정 플로우로 진입
                    StartCoroutine(CorrectionFlow42());
                }
            }
            return;
        }

        // 교정 단계: 현재 슬롯에 대해서만 판단
        if (_inCorrectionPhase && _currentCorrectionSlot >= 0 && slotIndex != _currentCorrectionSlot)
            return;

        bool correct = attemptCorrect;
        _attemptsPerSlot[slotIndex] = Mathf.Clamp(_attemptsPerSlot[slotIndex] + 1, 1, 3);
        if (!correct)
        {
            if (audioSource && sfxNext) audioSource.PlayOneShot(sfxNext, Mathf.Clamp01(localSfxVolume));
            if (_attemptsPerSlot[slotIndex] >= 3)
                StartCoroutine(Co_FinalizeSlot(slotIndex, expected));
            else
                StartCoroutine(Co_ThinkAgainThenReplaySlowVoice()); // [4.2.7.3] + replay slow voice
            return;
        }
        // 정답으로 다시 채워넣은 경우에도 효과음 재생
        if (audioSource && sfxNext) audioSource.PlayOneShot(sfxNext, Mathf.Clamp01(localSfxVolume));
        SetSlotText(slotIndex, NormalizePhoneme(expected));
        SetSlotAlpha(slotIndex, 1f);
        _finalizedSlots[slotIndex] = true;
        _awaitingUserArrangement = false;
    }

    private IEnumerator Co_FinalizeSlot(int slotIndex, string expected)
    {
        if (_attemptsPerSlot != null && slotIndex >= 0 && slotIndex < _attemptsPerSlot.Length && _attemptsPerSlot[slotIndex] >= 3 && clipItsOkay)
            yield return PlayClip(clipItsOkay); // [4.1.15]
        else if (clipWrongTryMatch)
            yield return PlayClip(clipWrongTryMatch); // [4.2.7.2]
        SetSlotText(slotIndex, NormalizePhoneme(expected));
        SetSlotAlpha(slotIndex, 1f);
        _finalizedSlots[slotIndex] = true;
        _awaitingUserArrangement = false;
    }

    private void ShowEndPanel()
    {
        RectTransform canvasRoot = null;
        var cvComp = (guideRect ? guideRect.GetComponentInParent<Canvas>() : null);
        if (cvComp)
            canvasRoot = cvComp.transform as RectTransform;
        else
        {
            var cv = new GameObject("EndCanvas").AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 5000;
            cv.gameObject.AddComponent<CanvasScaler>();
            cv.gameObject.AddComponent<GraphicRaycaster>();
            canvasRoot = cv.transform as RectTransform;
        }

        // overlay
        var overlay = new GameObject("EndOverlay", typeof(RectTransform), typeof(Image));
        var rt = overlay.GetComponent<RectTransform>();
        rt.SetParent(canvasRoot, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = overlay.GetComponent<Image>(); img.color = new Color(0,0,0,0.6f);

        // panel
        var panel = new GameObject("EndPanel", typeof(RectTransform), typeof(Image));
        var prt = panel.GetComponent<RectTransform>();
        prt.SetParent(rt, false);
        prt.sizeDelta = new Vector2(1200f, 800f);
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(1,1,1,0.1f);

        // title text: "학습이 끝났습니다"
        var titleGO = new GameObject("Title", typeof(RectTransform));
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.SetParent(prt, false);
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -80f);
        titleRT.sizeDelta = new Vector2(1000f, 180f);
        var titleTMP = titleGO.AddComponent<TMP_Text>();
        titleTMP.text = "학습이 끝났습니다";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 96f;
        titleTMP.color = Color.white;

        // helper to make button
        Button ResolveButton(Button prefab, string[] resourcePaths, out bool isCustom)
        {
            if (prefab) { isCustom = true; return prefab; }
            // try Resources paths
            foreach (var p in resourcePaths)
            {
                var b = Resources.Load<Button>(p);
                if (b) { isCustom = true; return b; }
                var go = Resources.Load<GameObject>(p);
                if (go)
                {
                    var childBtn = go.GetComponentInChildren<Button>(true) ?? go.GetComponent<Button>();
                    if (childBtn) { isCustom = true; return childBtn; }
                }
            }
            isCustom = false; return null;
        }

        Button MakeBtn(Button prefab, string text, Vector2 pos)
        {
            Button b;
            if (prefab)
            {
                b = Instantiate(prefab, prt);
            }
            else
            {
                var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
                var brt = go.GetComponent<RectTransform>(); brt.SetParent(prt, false);
                go.GetComponent<Image>().color = new Color(0.2f,0.2f,0.2f,0.9f);
                var tgo = new GameObject("Text", typeof(RectTransform));
                var trt = tgo.GetComponent<RectTransform>(); trt.SetParent(brt, false);
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                var tmp = tgo.AddComponent<TMP_Text>(); tmp.text = text; tmp.alignment = TextAlignmentOptions.Center; tmp.fontSize = 72f; tmp.color = Color.white;
                b = go.GetComponent<Button>();
            }
            var r = b.GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(600f, 240f);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            return b;
        }

        // Try resolve buttons from Resources/UI if prefabs not set
        bool againCustom;
        var againResolved = ResolveButton(againButtonPrefab, new[]{
            "UI/againbutton","UI/AgainButton","againbutton","AgainButton"
        }, out againCustom);
        var again = MakeBtn(againResolved, againCustom ? string.Empty : "다시 학습하기", new Vector2(-330f, -200f));
        again.onClick.AddListener(() => { Destroy(overlay); RestartStage(); });

        bool lobbyCustom;
        var lobbyResolved = ResolveButton(lobbyButtonPrefab, new[]{
            "UI/lobbybutton","UI/LobbyButton","lobbybutton","LobbyButton"
        }, out lobbyCustom);
        var lobby = MakeBtn(lobbyResolved, lobbyCustom ? string.Empty : "로비로 가기", new Vector2(330f, -200f));
        lobby.onClick.AddListener(() => { Destroy(overlay); GoToLobby(); });
    }

    private void RestartStage()
    {
        var s = SceneManager.GetActiveScene();
        if (s.IsValid()) SceneManager.LoadScene(s.name);
    }

    private void GoToLobby()
    {
        try { SceneManager.LoadScene("Lobby"); }
        catch { Debug.LogWarning("[Stage42] Lobby scene not found."); }
    }

    private IEnumerator Co_ThinkAgainThenReplaySlowVoice()
    {
        if (clipThinkAgain)
            yield return PlayClip(clipThinkAgain); // [4.2.7.3]
        if (!string.IsNullOrEmpty(_currentSlowVoiceUrl))
            yield return PlayVoiceUrl(_currentSlowVoiceUrl);
    }

    private void BeginInitialFill()
    {
        // 초기 상태: 초성부터 시작(자음 패널만), 이후 중성(모음), 종성(자음)
        _inInitialFillPhase = true;
        _inCorrectionPhase = false;
        _currentCorrectionSlot = -1;
        _initialSlotIndex = 0;
        ShowChoicePanelsForSlot(_initialSlotIndex);
        if (choseongBox) FocusBox(choseongBox);
    }

    private bool AreAllSlotsFilled()
    {
        int needed = Mathf.Max(0, _expectedSegmentCount);
        int cnt = 0;
        if (needed >= 1 && choseongText && !string.IsNullOrEmpty(choseongText.text)) cnt++;
        if (needed >= 2 && jungseongText && !string.IsNullOrEmpty(jungseongText.text)) cnt++;
        if (needed >= 3 && jongseongText && !string.IsNullOrEmpty(jongseongText.text)) cnt++;
        return cnt >= needed;
    }

    private bool EvaluateAndFinalizeFilled(bool clearWrongSlots)
    {
        bool allOk = true;
        for (int i = 0; i < _expectedSegmentCount; i++)
        {
            string user = GetSlotCurrentText(i);
            string expected = _expectedPhonemes[i] ?? string.Empty;
            bool ok = string.Equals(NormalizePhoneme(user), NormalizePhoneme(expected), StringComparison.Ordinal);
            if (ok)
            {
                SetSlotText(i, NormalizePhoneme(expected));
                SetSlotAlpha(i, 1f);
                _finalizedSlots[i] = true;
            }
            else
            {
                if (clearWrongSlots) SetSlotText(i, string.Empty);
                SetSlotAlpha(i, dimAlpha);
                _finalizedSlots[i] = false;
                allOk = false;
            }
        }
        return allOk;
    }

    private string GetSlotCurrentText(int idx)
    {
        if (idx == 0 && choseongText) return choseongText.text ?? string.Empty;
        if (idx == 1 && jungseongText) return jungseongText.text ?? string.Empty;
        if (idx == 2 && jongseongText) return jongseongText.text ?? string.Empty;
        return string.Empty;
    }

    private void SetProgressLabel(int index, int total)
    { if (progressText) progressText.text = $"{index}/{total}"; }

    private void ClearPhonemeBoxes()
    {
        if (choseongText) choseongText.text = string.Empty;
        if (jungseongText) jungseongText.text = string.Empty;
        if (jongseongText) jongseongText.text = string.Empty;
        SetAllBoxAlpha(dimAlpha);
    }

    private void SetSlotText(int idx, string text)
    {
        if (idx == 0 && choseongText) choseongText.text = text ?? string.Empty;
        else if (idx == 1 && jungseongText) jungseongText.text = text ?? string.Empty;
        else if (idx == 2 && jongseongText) jongseongText.text = text ?? string.Empty;
    }

    private void SetSlotAlpha(int idx, float a)
    {
        if (idx == 0) SetBoxAlpha(choseongBox, a);
        else if (idx == 1) SetBoxAlpha(jungseongBox, a);
        else if (idx == 2) SetBoxAlpha(jongseongBox, a);
    }

    private void SetAllBoxAlpha(float a)
    {
        SetBoxAlpha(choseongBox, a);
        SetBoxAlpha(jungseongBox, a);
        SetBoxAlpha(jongseongBox, a);
    }

    private void SetBoxAlpha(GameObject box, float a)
    {
        if (!box) return;
        var cg = box.GetComponent<CanvasGroup>() ?? box.AddComponent<CanvasGroup>();
        cg.alpha = Mathf.Clamp01(a);
    }

    private void FocusBox(GameObject box)
    {
        if (_blinkCo != null)
        {
            StopCoroutine(_blinkCo);
            _blinkCo = null;
        }
        _focusedBox = box;
        SetAllBoxAlpha(dimAlpha);
        if (box != null)
            _blinkCo = StartCoroutine(BlinkBox(box));
    }

    private IEnumerator BlinkBox(GameObject box)
    {
        var cg = box.GetComponent<CanvasGroup>() ?? box.AddComponent<CanvasGroup>();
        float t = 0f;
        while (_focusedBox == box)
        {
            t += Time.deltaTime;
            float phase = Mathf.Sin((t / Mathf.Max(0.01f, blinkPeriod)) * Mathf.PI * 2f) * 0.5f + 0.5f;
            cg.alpha = Mathf.Lerp(blinkAlphaMin, blinkAlphaMax, phase);
            yield return null;
        }
        cg.alpha = dimAlpha;
    }

    private IEnumerator MoveGuideToCorner()
    {
        if (!guideRect) yield break;
        _guideMoved = true;
        var startSize = guideRect.sizeDelta;
        var startPos = guideRect.anchoredPosition;
        var endSize = guideEndSize;
        var endPos = guideEndAnchoredPos;
        float t = 0f;
        while (t < guideMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / guideMoveDuration);
            guideRect.sizeDelta = Vector2.Lerp(startSize, endSize, k);
            guideRect.anchoredPosition = Vector2.Lerp(startPos, endPos, k);
            yield return null;
        }
        guideRect.sizeDelta = endSize;
        guideRect.anchoredPosition = endPos;
    }

    private IEnumerator PlayClip(AudioClip clip)
    {
        if (!clip || !audioSource) yield break;
        audioSource.Stop();
        audioSource.clip = clip;
        float oldVol = audioSource.volume;
        audioSource.volume = Mathf.Clamp01(localClipVolume);
        audioSource.Play();
        yield return new WaitWhile(() => audioSource.isPlaying);
        audioSource.volume = oldVol;
    }

    private static AudioType GuessAudioType(string url)
    {
        string u = (url ?? string.Empty).ToLowerInvariant();
        if (u.Contains(".wav")) return AudioType.WAV;
        if (u.Contains(".mp3")) return AudioType.MPEG;
        if (u.Contains(".ogg")) return AudioType.OGGVORBIS;
        return AudioType.MPEG;
    }

    private IEnumerator PlayVoiceUrl(string voiceUrl)
    {
        if (string.IsNullOrEmpty(voiceUrl) || !audioSource) yield break;
        // 1) 시도: '+'를 %2B로 인코딩한 경로
        string safeUrl = EncodePlusInPath(voiceUrl);
        bool played = false;
        // slow voice volume override preparation
        bool isSlow = false;
        if (!string.IsNullOrEmpty(_currentSlowVoiceUrl))
        {
            string curSafe = EncodePlusInPath(_currentSlowVoiceUrl);
            isSlow = string.Equals(curSafe, safeUrl, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(_currentSlowVoiceUrl, voiceUrl, StringComparison.OrdinalIgnoreCase);
        }
        float originalVolume = audioSource.volume;
        if (logVerbose) Debug.Log($"[Stage42] GET audio {safeUrl}");
        using (var req = UnityWebRequestMultimedia.GetAudioClip(safeUrl, GuessAudioType(voiceUrl)))
        {
            // 외부(S3) 오디오는 인증 헤더 불필요. 공통 헤더 중 Authorization은 생략.
            req.SetRequestHeader("Accept", "audio/*");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(req);
                audioSource.Stop();
                audioSource.clip = clip;
                if (isSlow) audioSource.volume = Mathf.Clamp01(slowVoiceVolume);
                audioSource.Play();
                yield return new WaitWhile(() => audioSource.isPlaying);
                if (isSlow) audioSource.volume = originalVolume;
                played = true;
            }
            else if (logVerbose)
            {
                Debug.LogWarning($"[Stage42] audio GET failed: code={req.responseCode} error={req.error}");
            }
        }

        // 2) 실패 시: 원본 URL로 재시도 (일부 버킷은 '+' 원문을 요구)
        if (!played && !string.Equals(safeUrl, voiceUrl, StringComparison.Ordinal))
        {
            if (logVerbose) Debug.Log($"[Stage42] Retry audio with raw url {voiceUrl}");
            using (var req2 = UnityWebRequestMultimedia.GetAudioClip(voiceUrl, GuessAudioType(voiceUrl)))
            {
                req2.SetRequestHeader("Accept", "audio/*");
                yield return req2.SendWebRequest();
                if (req2.result != UnityWebRequest.Result.Success)
                {
                    if (logVerbose) Debug.LogWarning($"[Stage42] audio retry failed: code={req2.responseCode} error={req2.error}");
                    yield break;
                }
                var clip2 = DownloadHandlerAudioClip.GetContent(req2);
                audioSource.Stop();
                audioSource.clip = clip2;
                if (isSlow) audioSource.volume = Mathf.Clamp01(slowVoiceVolume);
                audioSource.Play();
                yield return new WaitWhile(() => audioSource.isPlaying);
                if (isSlow) audioSource.volume = originalVolume;
            }
        }
    }

    private IEnumerator RecordMicOnly(int seconds, int sampleRate)
    {
        if (micIndicator) micIndicator.SetActive(true);
        yield return null;
        var clip = StartMic(seconds, sampleRate);
        float waited = 0f;
        while (Microphone.GetPosition(null) <= 0 && waited < 0.5f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(seconds);
        if (micIndicator) micIndicator.SetActive(false);
        Microphone.End(null);
    }

    private IEnumerator RecordAndUploadFinalVoice()
    {
        // Ensure stage session exists before upload to avoid 403
        if (string.IsNullOrWhiteSpace(stageSessionId))
        {
            if (!bypassStartRequest)
            {
                if (logVerbose) Debug.Log("[Stage42] stageSessionId is empty; attempting stage/start...");
                yield return StartStageSession();
            }
            if (string.IsNullOrWhiteSpace(stageSessionId))
            {
                if (logVerbose) Debug.LogWarning("[Stage42] stage/start failed or no sessionId; skipping check/voice upload.");
                yield break;
            }
        }
        if (string.IsNullOrWhiteSpace((authToken ?? string.Empty).Trim()))
        {
            if (logVerbose) Debug.LogWarning("[Stage42] authToken is empty; server may respond 401/403.");
        }

        if (micIndicator) micIndicator.SetActive(true);
        yield return null;
        var clip = StartMic(recordSeconds, recordSampleRate);
        float waited = 0f;
        while (Microphone.GetPosition(null) <= 0 && waited < 0.5f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(recordSeconds);
        if (micIndicator) micIndicator.SetActive(false);
        Microphone.End(null);

        var wav = WavUtility.FromAudioClip(clip);
        yield return UploadVoiceWithSession(_currentProblemWord ?? string.Empty, wav, ok =>
        {
            if (logVerbose) Debug.Log($"[Stage42] check/voice result={ok}");
        });
        yield break;
#if LEGACY_STAGE42_FALLBACK
        string url = ComposeUrl($"/api/train/check/voice?stageSessionId={UnityWebRequest.EscapeURL(stageSessionId ?? string.Empty)}&stage={UnityWebRequest.EscapeURL(stageTwoPart ?? string.Empty)}&problemNumber={Mathf.Max(1, _currentProblemNumber)}&answer={UnityWebRequest.EscapeURL(_currentProblemWord ?? string.Empty)}");
        if (logVerbose) Debug.Log($"[Stage42] POST {url} (multipart audio/wav)");
        var form = new WWWForm();
        form.AddBinaryData("audio", wav, "voice.wav", "audio/wav");
        using (var req = UnityWebRequest.Post(url, form))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (logVerbose) Debug.Log($"[Stage42] check/voice resp: code={req.responseCode} body={req.downloadHandler?.text}");
        }
#endif
    }

    private AudioClip StartMic(int seconds, int sampleRate)
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[Stage42] 마이크 없음");
            return null;
        }
        return Microphone.Start(null, false, seconds, sampleRate);
    }

    private string ComposeUrl(string path)
    {
        string b = (baseUrl ?? string.Empty).TrimEnd('/');
        string p = (path ?? string.Empty).TrimStart('/');
        return string.IsNullOrEmpty(b) ? ("/" + p) : ($"{b}/{p}");
    }

    private string EncodePlusInPath(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.Replace("+", "%2B");
            var builder = new UriBuilder(uri) { Path = path };
            return builder.Uri.ToString();
        }
        catch { return url.Replace("+", "%2B"); }
    }

    private void ApplyCommonHeaders(UnityWebRequest req)
    {
        var tokenTrim = (authToken ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(tokenTrim))
            req.SetRequestHeader("Authorization", $"Bearer {tokenTrim}");
        req.SetRequestHeader("Accept", "application/json");
    }

    // Attempt logging for each drag try in Stage 4.2
    private IEnumerator SendAttemptLog42(int problemNumber, int attemptNumber, string problem, string answer, bool isCorrect)
    {
        string url = ComposeUrl("/api/train/attempt");
        string ssid = stageSessionId ?? string.Empty;
        string stg = stageTwoPart ?? string.Empty;
        string prob = problem ?? string.Empty;
        string ans = answer ?? string.Empty;
        string audioUrl = string.Empty; // always empty in 4.2 drag attempts
        string json = "{" +
                      "\"stageSessionId\":\"" + JsonEscape(ssid) + "\"," +
                      "\"problemNumber\":" + problemNumber + "," +
                      "\"stage\":\"" + JsonEscape(stg) + "\"," +
                      "\"problem\":\"" + JsonEscape(prob) + "\"," +
                      "\"answer\":\"" + JsonEscape(ans) + "\"," +
                      "\"audioUrl\":\"" + JsonEscape(audioUrl) + "\"," +
                      "\"isCorrect\":" + (isCorrect ? "true" : "false") + "," +
                      "\"isReplyCorrect\":\"\"," + // empty string as requested
                      "\"attemptNumber\":" + attemptNumber + "}";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyCommonHeaders(req); // Authorization, Accept
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                if (logVerbose) Debug.LogWarning($"[Stage42] attempt 실패: {req.error} (code={req.responseCode})\\nURL={url}\\nBody={json}\\nResp={req.downloadHandler.text}");
            }
            else if (logVerbose)
            {
                Debug.Log($"[Stage42] attempt OK: problem={problemNumber}, attempt={attemptNumber}, correct={isCorrect}, problem='{prob}', answer='{ans}'");
            }
        }
    }

    private static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private IEnumerator FetchQuestions(Action<List<QuestionDto>> onDone)
    {
        yield return FetchQuestionsWithSession(onDone);
        yield break;
#if LEGACY_STAGE42_FALLBACK
        string url = ComposeUrl($"/api/train/set?stage={UnityWebRequest.EscapeURL(stageSet)}&count={count}&stageSessionId={UnityWebRequest.EscapeURL(stageSessionId ?? string.Empty)}");
        if (logVerbose) Debug.Log($"[Stage42] set 요청: {url}");
        using (var req = UnityWebRequest.Get(url))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (logVerbose) Debug.Log($"[Stage42] set resp: code={req.responseCode} body={req.downloadHandler?.text}");
            if (req.result != UnityWebRequest.Result.Success)
            { onDone?.Invoke(null); yield break; }
            try
            {
                var parsed = JsonUtility.FromJson<QuestionListResponse>(req.downloadHandler.text);
                onDone?.Invoke(parsed?.data?.problems);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Stage42] set 응답 파싱 실패: {e.Message}\nRaw={req.downloadHandler.text}");
            }
        }
#endif
    }

    private IEnumerator StartStageSession()
    {
        yield return StartStageSessionWithSession();
        yield break;
#if LEGACY_STAGE42_FALLBACK
        string url = ComposeUrl($"/api/train/stage/start?stage={UnityWebRequest.EscapeURL(stageTwoPart)}&totalProblems={count}");
        if (logVerbose) Debug.Log($"[Stage42] POST {url}");
        using (var req = UnityWebRequest.PostWwwForm(url, ""))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            try
            {
                var resp = JsonUtility.FromJson<StartStageResp>(req.downloadHandler.text);
                if (resp != null && resp.data != null && !string.IsNullOrWhiteSpace(resp.data.stageSessionId))
                    stageSessionId = resp.data.stageSessionId;
                if (logVerbose) Debug.Log($"[Stage42] stage/start resp: code={req.responseCode} body={req.downloadHandler?.text}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Stage42] stage/start 파싱 실패: {e.Message}\nRaw={req.downloadHandler.text}");
            }
        }
#endif
    }

    private IEnumerator CompleteStageSession()
    {
        yield return CompleteStageSessionWithSession();
        yield break;
#if LEGACY_STAGE42_FALLBACK
        if (string.IsNullOrWhiteSpace(stageSessionId)) yield break;
        string url = ComposeUrl($"/api/train/stage/complete?stageSessionId={UnityWebRequest.EscapeURL(stageSessionId)}");
        if (logVerbose) Debug.Log($"[Stage42] POST {url}");
        using (var req = UnityWebRequest.PostWwwForm(url, ""))
        {
            ApplyCommonHeaders(req);
            yield return req.SendWebRequest();
            if (logVerbose) Debug.Log($"[Stage42] stage/complete resp: code={req.responseCode} body={req.downloadHandler?.text}");
        }
#endif
    }

    // ===== Phoneme normalization (초/중/종 비교를 안정적으로) =====
    private static string NormalizePhoneme(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim();
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
            sb.Append(NormalizePhonemeChar(ch));
        return MergeCompoundJamo(sb.ToString());
    }

    private static char NormalizePhonemeChar(char ch)
    {
        switch (ch)
        {
            // choseong -> compatibility
            case '\u1100': return '\u3131'; case '\u1101': return '\u3132'; case '\u1102': return '\u3134';
            case '\u1103': return '\u3137'; case '\u1104': return '\u3138'; case '\u1105': return '\u3139';
            case '\u1106': return '\u3141'; case '\u1107': return '\u3142'; case '\u1108': return '\u3143';
            case '\u1109': return '\u3145'; case '\u110A': return '\u3146'; case '\u110B': return '\u3147';
            case '\u110C': return '\u3148'; case '\u110D': return '\u3149'; case '\u110E': return '\u314A';
            case '\u110F': return '\u314B'; case '\u1110': return '\u314C'; case '\u1111': return '\u314D'; case '\u1112': return '\u314E';
            // jungseong -> compatibility
            case '\u1161': return '\u314F'; case '\u1162': return '\u3150'; case '\u1163': return '\u3151'; case '\u1164': return '\u3152';
            case '\u1165': return '\u3153'; case '\u1166': return '\u3154'; case '\u1167': return '\u3155'; case '\u1168': return '\u3156';
            case '\u1169': return '\u3157'; case '\u116A': return '\u3158'; case '\u116B': return '\u3159'; case '\u116C': return '\u315A';
            case '\u116D': return '\u315B'; case '\u116E': return '\u315C'; case '\u116F': return '\u315D'; case '\u1170': return '\u315E';
            case '\u1171': return '\u315F'; case '\u1172': return '\u3160'; case '\u1173': return '\u3161'; case '\u1174': return '\u3162'; case '\u1175': return '\u3163';
            // jongseong -> compatibility
            case '\u11A8': return '\u3131'; case '\u11A9': return '\u3132'; case '\u11AA': return '\u3133';
            case '\u11AB': return '\u3134'; case '\u11AC': return '\u3135'; case '\u11AD': return '\u3136';
            case '\u11AE': return '\u3137'; case '\u11AF': return '\u3139'; case '\u11B0': return '\u313A'; case '\u11B1': return '\u313B';
            case '\u11B2': return '\u313C'; case '\u11B3': return '\u313D'; case '\u11B4': return '\u313E'; case '\u11B5': return '\u313F'; case '\u11B6': return '\u3140';
            case '\u11B7': return '\u3141'; case '\u11B8': return '\u3142'; case '\u11B9': return '\u3144'; case '\u11BA': return '\u3145'; case '\u11BB': return '\u3146';
            case '\u11BC': return '\u3147'; case '\u11BD': return '\u3148'; case '\u11BE': return '\u314A'; case '\u11BF': return '\u314B'; case '\u11C0': return '\u314C';
            case '\u11C1': return '\u314D'; case '\u11C2': return '\u314E';
        }
        return ch;
    }

    private static string MergeCompoundJamo(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        // vowel compounds
        s = s.Replace("ㅗㅏ", "ㅘ");
        s = s.Replace("ㅗㅐ", "ㅙ");
        s = s.Replace("ㅗㅣ", "ㅚ");
        s = s.Replace("ㅜㅓ", "ㅝ");
        s = s.Replace("ㅜㅔ", "ㅞ");
        s = s.Replace("ㅜㅣ", "ㅟ");
        s = s.Replace("ㅡㅣ", "ㅢ");
        // final consonant compounds
        s = s.Replace("ㄱㅅ", "ㄳ");
        s = s.Replace("ㄴㅈ", "ㄵ");
        s = s.Replace("ㄴㅎ", "ㄶ");
        s = s.Replace("ㄹㄱ", "ㄺ");
        s = s.Replace("ㄹㅁ", "ㄻ");
        s = s.Replace("ㄹㅂ", "ㄼ");
        s = s.Replace("ㄹㅅ", "ㄽ");
        s = s.Replace("ㄹㅌ", "ㄾ");
        s = s.Replace("ㄹㅍ", "ㄿ");
        s = s.Replace("ㄹㅎ", "ㅀ");
        s = s.Replace("ㅂㅅ", "ㅄ");
        return s;
    }
}




