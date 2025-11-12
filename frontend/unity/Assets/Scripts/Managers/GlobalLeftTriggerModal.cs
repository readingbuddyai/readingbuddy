using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Utils;

/// <summary>
/// 왼손 트리거로 토글하는 글로벌 모달 오버레이
/// - Lives in _Persistent and DontDestroyOnLoad
/// 다시 누르면 닫힙니다.
/// </summary>
public class GlobalLeftTriggerModal : MonoBehaviour
{
    [Header("Overlay Style")]
    [Tooltip("Background color of the modal overlay (alpha recommended 0.5~0.8)")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.6f);

    [Tooltip("Optional hint text shown at the bottom")]
    public string hintText = "왼손 트리거를 다시 눌러 닫기";

    [Tooltip("TMP가 없으면 기본 text 컴포넌트를 사용합니다.")] 
    public Font uiFont;

    [Header("Display Space")]
    [Tooltip("If true, spawns a World Space canvas locked in front of the active camera (recommended for VR).")]
    public bool useWorldSpaceForXR = true;
    [Tooltip("Distance in meters in front of the camera for the world-space modal")]
    public float worldSpaceDistance = 1.25f;
    [Tooltip("Panel size in meters for the world-space modal (scaled from 1000px = 1m)")]
    public Vector2 worldSpaceSizeMeters = new Vector2(1.6f, 0.9f);
    [Tooltip("If true, parent the modal to the camera so it follows head movement")]
    public bool parentToCamera = true;

    [Header("Detection")]
    [Tooltip("Analog trigger threshold considered as 'pressed'")]
    [Range(0.1f, 1f)]
    public float triggerPressThreshold = 0.6f;

    [Header("Editor/Test Override")]
    [Tooltip("Editor fallback key to toggle (for non-XR testing)")]
    public KeyCode toggleFallbackKey = KeyCode.M;

    private GameObject _overlayRoot;
    private bool _visible;
    private bool _leftTriggerWasPressed;
    private Camera _cachedCamera;
    private Button _homeButton;
    private Button _audioButton;
    private System.Action<string> _setAudioLabel;
    [Header("Actions")]
    [Tooltip("Scene name to load when pressing Home button")]
    public string homeSceneName = "Home";

    [Header("Audio Visuals")]
    [Tooltip("Sprite when audio is MUTED")]
    public Sprite audioMutedSprite;
    [Tooltip("Sprite when audio is UNMUTED")]
    public Sprite audioUnmutedSprite;
    [Tooltip("Optional target Image for audio icon. If not set, uses the Button's own Image.")]
    public Image audioIconTarget;

    [Header("Home Visuals")]
    [Tooltip("Sprite for the Home button icon (optional)")]
    public Sprite homeSprite;
    [Tooltip("Target Image to show the Home icon. If not set, uses the Home Button's Image.")]
    public Image homeIconTarget;

    [Header("UI Hook (Optional)")]
    [Tooltip("If assigned, this prefab is instantiated as the overlay root instead of auto-built UI.")]
    public RectTransform overlayPrefab;
    [Tooltip("If using a custom overlay, assign the Home button here.")]
    public Button homeButtonRef;
    [Tooltip("If using a custom overlay, assign the Audio toggle button here.")]
    public Button audioButtonRef;
    [Tooltip("If false, do not auto-build default UI (expect prefab/refs).")]
    public bool autoBuildUI = true;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        UpdateHomeButtonState();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Update()
    {
        bool pressed = IsLeftTriggerPressed();
        bool justPressed = pressed && !_leftTriggerWasPressed;

        if (justPressed || (toggleFallbackKey != KeyCode.None && Input.GetKeyDown(toggleFallbackKey)))
        {
            Toggle();
        }

        _leftTriggerWasPressed = pressed;
    }

    public void Toggle()
    {
        SetVisible(!_visible);
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
        EnsureOverlay();
        if (_overlayRoot != null)
            _overlayRoot.SetActive(_visible);
        UpdateHomeButtonState();
    }

    private void EnsureOverlay()
    {
        // Replace/reconfigure if needed when camera changes across scenes
        var activeCam = FindActiveCamera();
        bool needRebuild = _overlayRoot == null;

        if (!needRebuild)
        {
            var existingCanvas = _overlayRoot.GetComponent<Canvas>();
            if (useWorldSpaceForXR && (existingCanvas == null || existingCanvas.renderMode != RenderMode.WorldSpace))
                needRebuild = true;
            if (!useWorldSpaceForXR && (existingCanvas == null || existingCanvas.renderMode != RenderMode.ScreenSpaceOverlay))
                needRebuild = true;
            if (useWorldSpaceForXR && existingCanvas != null && existingCanvas.worldCamera != activeCam)
                needRebuild = true;
        }

        if (!needRebuild)
        {
            // Keep following camera if world-space and parented
            if (useWorldSpaceForXR && parentToCamera && _overlayRoot.transform.parent != (activeCam ? activeCam.transform : null))
            {
                _overlayRoot.transform.SetParent(activeCam ? activeCam.transform : null, worldPositionStays: false);
                PositionWorldSpaceModal(_overlayRoot.transform as RectTransform, activeCam);
            }
            return;
        }

        if (_overlayRoot != null)
        {
            Destroy(_overlayRoot);
            _overlayRoot = null;
        }

        // Root object (custom prefab or programmatic build)
        Canvas canvas = null;
        if (overlayPrefab != null)
        {
            var inst = Instantiate(overlayPrefab);
            _overlayRoot = inst.gameObject;
            _overlayRoot.name = "GlobalModalOverlay";
            _overlayRoot.layer = LayerMask.NameToLayer("UI");
            DontDestroyOnLoad(_overlayRoot);
            canvas = _overlayRoot.GetComponent<Canvas>();
            if (canvas == null) canvas = _overlayRoot.AddComponent<Canvas>();
            if (_overlayRoot.GetComponent<GraphicRaycaster>() == null)
                _overlayRoot.AddComponent<GraphicRaycaster>();
            if (_overlayRoot.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                _overlayRoot.AddComponent<TrackedDeviceGraphicRaycaster>();
            if (_overlayRoot.GetComponent<CanvasGroup>() == null)
            {
                var cg = _overlayRoot.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }
        else
        {
            _overlayRoot = new GameObject("GlobalModalOverlay", typeof(RectTransform));
            _overlayRoot.layer = LayerMask.NameToLayer("UI");
            DontDestroyOnLoad(_overlayRoot);
            canvas = _overlayRoot.AddComponent<Canvas>();
            _overlayRoot.AddComponent<GraphicRaycaster>();
            _overlayRoot.AddComponent<TrackedDeviceGraphicRaycaster>();
            var canvasGroup = _overlayRoot.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (useWorldSpaceForXR)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = activeCam;
            canvas.sortingOrder = short.MaxValue;

            var rect = (RectTransform)_overlayRoot.transform;
            if (parentToCamera && activeCam != null)
                rect.SetParent(activeCam.transform, false);
            PositionWorldSpaceModal(rect, activeCam);

            if (autoBuildUI && overlayPrefab == null)
            {
                // Background panel with fixed pixel size (1m ~= 1000px)
                var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(_overlayRoot.transform, false);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0.5f, 0.5f);
                bgRect.anchorMax = new Vector2(0.5f, 0.5f);
                bgRect.pivot = new Vector2(0.5f, 0.5f);
                bgRect.sizeDelta = worldSpaceSizeMeters * 1000f; // pixels while scale=0.001
                var bgImg = bg.GetComponent<Image>();
                bgImg.color = overlayColor;

                BuildDefaultWidgets(bg.transform);
            }
        }
        else
        {
            // Screen-space overlay (non-VR UI)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; // top-most

            if (autoBuildUI && overlayPrefab == null)
            {
                // Background panel
                var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(_overlayRoot.transform, false);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                var bgImg = bg.GetComponent<Image>();
                bgImg.color = overlayColor;

                BuildDefaultWidgets(bg.transform);
            }
        }

        // If user supplied custom references, wire them
        if (_homeButton == null && homeButtonRef != null) _homeButton = homeButtonRef;
        if (_audioButton == null && audioButtonRef != null) _audioButton = audioButtonRef;
        // If not wired, leave to user-specified references
        if (_homeButton != null)
        {
            _homeButton.onClick.RemoveListener(OnClickHome);
            _homeButton.onClick.AddListener(OnClickHome);
        }
        if (_audioButton != null)
        {
            _audioButton.onClick.RemoveListener(OnClickToggleAudio);
            _audioButton.onClick.AddListener(OnClickToggleAudio);
        }

        // Sync UI state (label/icon) with audio state
        UpdateAudioVisual();
        UpdateHomeVisual();
        UpdateHomeButtonState();

        _overlayRoot.SetActive(false);
    }

    private void PositionWorldSpaceModal(RectTransform rect, Camera cam)
    {
        if (rect == null || cam == null) return;
        rect.localScale = Vector3.one * 0.001f; // 1000px ~= 1m
        if (parentToCamera)
        {
            rect.localPosition = new Vector3(0f, 0f, Mathf.Max(0.1f, worldSpaceDistance));
            rect.localRotation = Quaternion.identity;
        }
        else
        {
            rect.position = cam.transform.position + cam.transform.forward * Mathf.Max(0.1f, worldSpaceDistance);
            rect.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }
    }

    private void BuildDefaultWidgets(Transform parent)
    {
        // Hint text
        var hint = new GameObject("HintText", typeof(RectTransform));
        hint.transform.SetParent(parent, false);
        var hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0f, 80f);
        hintRect.sizeDelta = new Vector2(1600f, 160f);
#if TMP_PRESENT
        var tmp = hint.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = hintText;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 48f;
        tmp.color = new Color(1f, 1f, 1f, 0.9f);
#else
        var txt = hint.AddComponent<Text>();
        txt.text = hintText;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 42;
        txt.color = new Color(1f, 1f, 1f, 0.9f);
        if (uiFont != null) txt.font = uiFont;
#endif

        // Buttons (middle center)
        Vector2 btnSize = new Vector2(300f, 300f);
        float spacing = 40f;
        float xOffset = (btnSize.x * 0.5f) + (spacing * 0.5f);

        System.Action<string> _unused;
        var homeBtn = CreateButton("HomeButton", parent, btnSize, new Vector2(-xOffset, 0f), "Home으로 이동", out _unused);
        _homeButton = homeBtn;

        System.Action<string> setLabel;
        var audioBtn = CreateButton("AudioButton", parent, btnSize, new Vector2(+xOffset, 0f), GetAudioLabel(), out setLabel);
        _audioButton = audioBtn;
        _setAudioLabel = setLabel;

        _homeButton.onClick.RemoveAllListeners();
        _homeButton.onClick.AddListener(OnClickHome);
        _audioButton.onClick.RemoveAllListeners();
        _audioButton.onClick.AddListener(OnClickToggleAudio);

        // Apply initial visuals for default build
        UpdateHomeVisual();
    }

    private Camera FindActiveCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var cams = GameObject.FindObjectsOfType<Camera>();
            if (cams.Length > 0) cam = cams[0];
        }
        _cachedCamera = cam;
        return cam;
    }

    private Button CreateButton(string name, Transform parent, Vector2 size, Vector2 anchoredPos, string label, out System.Action<string> setLabel)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);

        var btn = go.GetComponent<Button>();

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lrect = labelGo.GetComponent<RectTransform>();
        lrect.anchorMin = Vector2.zero;
        lrect.anchorMax = Vector2.one;
        lrect.offsetMin = Vector2.zero;
        lrect.offsetMax = Vector2.zero;

#if TMP_PRESENT
        var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 42f;
        tmp.color = Color.black;
        setLabel = (s) => tmp.text = s;
#else
        var txt = labelGo.AddComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 36;
        txt.color = Color.black;
        if (uiFont != null) txt.font = uiFont;
        setLabel = (s) => txt.text = s;
#endif
        return btn;
    }

    private static bool _isMutedCached = false;
    private string GetAudioLabel() => _isMutedCached ? "?�리 켜기" : "?�리 ?�기";

    private void OnClickHome()
    {
        if (_homeButton != null) _homeButton.interactable = false;
        StartCoroutine(CoGoHomeRouter());
    }

    private IEnumerator CoGoHomeRouter()
    {
        // Align with existing project pattern (SceneRouter + Additive + ActiveScene ?�환)
        Utils.GlobalSfxManager.Instance?.PlaySceneTransitionSfx();
        yield return SceneRouter.LoadContent(homeSceneName);
        if (_homeButton != null) _homeButton.interactable = true;
        SetVisible(false);
    }

    private void OnClickToggleAudio()
    {
        _isMutedCached = !_isMutedCached;
        ApplyAudioMuteState(_isMutedCached);
        UpdateAudioVisual();
    }

    private static readonly System.Collections.Generic.Dictionary<AudioSource, bool> _prevMuteStates = new System.Collections.Generic.Dictionary<AudioSource, bool>();
    private void ApplyAudioMuteState(bool muted)
    {
        // Primary: pause the listener so future sources also respect the state
        AudioListener.pause = muted;

        // Fallback for sources that ignore listener pause: force-mute while muted, restore after
        var sources = GameObject.FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            var s = sources[i];
            if (muted)
            {
                if (!_prevMuteStates.ContainsKey(s))
                    _prevMuteStates[s] = s.mute;
                if (s.ignoreListenerPause)
                    s.mute = true;
            }
            else
            {
                if (_prevMuteStates.TryGetValue(s, out bool prev))
                    s.mute = prev;
            }
        }
        if (!muted) _prevMuteStates.Clear();

        // BGM manager support
        if (BgmManager.Instance != null && BgmManager.Instance.audioSource != null)
        {
            var bgm = BgmManager.Instance.audioSource;
            if (muted)
            {
                if (!_prevMuteStates.ContainsKey(bgm))
                    _prevMuteStates[bgm] = bgm.mute;
                if (bgm.ignoreListenerPause)
                    bgm.mute = true;
            }
            else
            {
                if (_prevMuteStates.TryGetValue(bgm, out bool prev))
                    bgm.mute = prev;
            }
        }
    }

    private void UpdateAudioVisual()
    {
        // Update label if provided
        _setAudioLabel?.Invoke(GetAudioLabel());

        // Update icon sprite
        Image target = audioIconTarget;
        if (target == null && _audioButton != null)
            target = _audioButton.GetComponent<Image>();
        if (target != null)
        {
            var sprite = _isMutedCached ? audioMutedSprite : audioUnmutedSprite;
            if (sprite != null)
            {
                target.sprite = sprite;
                target.preserveAspect = true;
            }
        }
    }

    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        UpdateHomeButtonState(next.name);
    }

    private void UpdateHomeButtonState(string currentSceneName = null)
    {
        if (_homeButton == null) return;
        if (string.IsNullOrEmpty(currentSceneName))
            currentSceneName = SceneManager.GetActiveScene().name;
        var targetHome = string.IsNullOrEmpty(homeSceneName) ? "Home" : homeSceneName;
        bool isHome = string.Equals(currentSceneName, targetHome, System.StringComparison.OrdinalIgnoreCase);
        _homeButton.interactable = !isHome;
    }

    private static readonly System.Collections.Generic.List<InputDevice> _leftDevices = new System.Collections.Generic.List<InputDevice>();

    private bool IsLeftTriggerPressed()
    {
        _leftDevices.Clear();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, _leftDevices);
        for (int i = 0; i < _leftDevices.Count; i++)
        {
            var device = _leftDevices[i];
            if (!device.isValid) continue;
            // Prefer triggerButton; also check analog pulled over threshold
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool btn) && btn)
                return true;
            if (device.TryGetFeatureValue(CommonUsages.trigger, out float val) && val >= Mathf.Clamp01(triggerPressThreshold))
                return true;
        }
        return false;
    }

    private void UpdateHomeVisual()
    {
        Image target = homeIconTarget;
        if (target == null && _homeButton != null)
            target = _homeButton.GetComponent<Image>();
        if (target != null && homeSprite != null)
        {
            target.sprite = homeSprite;
            target.preserveAspect = true;
            // Ensure fully visible
            var c = target.color; c.a = 1f; target.color = c;
        }
    }
}
