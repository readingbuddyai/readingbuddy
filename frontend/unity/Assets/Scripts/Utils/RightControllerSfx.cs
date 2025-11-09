using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
using UnityEngine.XR;
using InputDeviceXR = UnityEngine.XR.InputDevice;
using CommonUsagesXR = UnityEngine.XR.CommonUsages;

namespace Utils
{
    /// <summary>
    /// XR 컨트롤러 입력(트리거/조이스틱)을 감지해 효과음을 재생하고 진동을 보내는 유틸 컴포넌트.
    /// 인스펙터에서 Left/Right를 선택할 수 있습니다.
    /// </summary>
    public class RightControllerSfx : MonoBehaviour
    {
        public enum HandSide { Left, Right }

        [Header("General")]
        [Tooltip("효과음을 적용할 XR 컨트롤러 (Left/Right)")]
        public HandSide handSide = HandSide.Right;

        [Tooltip("true면 오른손 컨트롤러 입력에 맞춰 효과음을 재생합니다.")]
        public bool enableSfx = true;

        [Tooltip("효과음을 재생할 AudioSource. 비워두면 이 컴포넌트가 붙은 오브젝트에서 AudioSource를 찾습니다.")]
        public AudioSource audioSource;

        [Header("Audio Clips")]
        [Tooltip("트리거가 처음 눌렸을 때 재생할 효과음")]
        public AudioClip triggerClip;

        [Tooltip("조이스틱이 처음 기울어졌을 때 재생할 효과음")]
        public AudioClip thumbstickClip;

        [Header("Trigger Haptics")]
        [Tooltip("트리거 입력 시 XR 컨트롤러에 진동을 보낼지 여부")]
        public bool enableTriggerHaptics = true;
        [Tooltip("진동 세기 (0~1)")]
        [Range(0f, 1f)]
        public float triggerHapticsAmplitude = 0.6f;
        [Tooltip("진동 지속 시간(초)")]
        [Range(0f, 3f)]
        public float triggerHapticsDuration = 0.15f;

        [Header("Thumbstick Detection")]
        [Tooltip("조이스틱 입력이 이 값 이상일 때 활성화된 것으로 판단합니다.")]
        [Range(0f, 1f)]
        public float thumbstickActivationThreshold = 0.35f;

        [Header("Editor/Test Override")]
        [Tooltip("XR 입력이 없는 환경(에디터)에서 테스트용으로 사용할 키")]
        public KeyCode triggerFallbackKey = KeyCode.Space;

#if ENABLE_INPUT_SYSTEM
        private AxisControl _triggerAxis;
        private ButtonControl _triggerButton;
        private StickControl _thumbstick;
#endif

        private static readonly List<InputDeviceXR> XrDevices = new List<InputDeviceXR>();

        private bool _triggerWasPressed;
        private bool _thumbstickWasActive;

        private void Awake()
        {
            if (!audioSource)
                audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (!enableSfx)
                return;

            bool triggerPressed = CheckTriggerPressed();
            bool triggerJustPressed = triggerPressed && !_triggerWasPressed;
            bool triggerFallbackPressed = triggerFallbackKey != KeyCode.None && Input.GetKeyDown(triggerFallbackKey);

            if (triggerJustPressed || triggerFallbackPressed)
            {
                PlayOneShot(triggerClip);
                if (triggerJustPressed)
                    SendTriggerHaptics();
            }

            _triggerWasPressed = triggerPressed;

            if (triggerFallbackPressed)
                _triggerWasPressed = true;

            bool thumbstickActive = CheckThumbstickEngaged();
            if (thumbstickActive && !_thumbstickWasActive)
                PlayOneShot(thumbstickClip);

            _thumbstickWasActive = thumbstickActive;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (!clip || !audioSource)
                return;

            audioSource.PlayOneShot(clip);
        }

        private bool CheckTriggerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            ResolveInputSystemControls();
            if (_triggerButton != null && _triggerButton.isPressed)
                return true;
            if (_triggerAxis != null && _triggerAxis.ReadValue() >= 0.99f)
                return true;
#endif

            XrDevices.Clear();
            InputDevices.GetDevicesAtXRNode(ToXRNode(handSide), XrDevices);
            for (int i = 0; i < XrDevices.Count; i++)
            {
                var device = XrDevices[i];
                if (!device.isValid) continue;
                if (device.TryGetFeatureValue(CommonUsagesXR.triggerButton, out bool triggerButton) && triggerButton)
                    return true;
                if (device.TryGetFeatureValue(CommonUsagesXR.trigger, out float triggerValue) && triggerValue >= 0.99f)
                    return true;
            }

            return false;
        }

        private bool CheckThumbstickEngaged()
        {
            float threshold = Mathf.Clamp01(thumbstickActivationThreshold);
            float sqrThreshold = threshold * threshold;

#if ENABLE_INPUT_SYSTEM
            ResolveInputSystemControls();
            if (_thumbstick != null)
            {
                Vector2 value = _thumbstick.ReadValue();
                if (value.sqrMagnitude >= sqrThreshold)
                    return true;
            }
#endif

            XrDevices.Clear();
            InputDevices.GetDevicesAtXRNode(ToXRNode(handSide), XrDevices);
            for (int i = 0; i < XrDevices.Count; i++)
            {
                var device = XrDevices[i];
                if (!device.isValid) continue;
                if (device.TryGetFeatureValue(CommonUsagesXR.primary2DAxis, out Vector2 axisValue) && axisValue.sqrMagnitude >= sqrThreshold)
                    return true;
            }

            return false;
        }

#if ENABLE_INPUT_SYSTEM
        private void ResolveInputSystemControls()
        {
            string handTag = handSide == HandSide.Right ? "RightHand" : "LeftHand";
            string triggerPath = $"<XRController>{{{handTag}}}/trigger";
            string triggerPressedPath = $"<XRController>{{{handTag}}}/triggerPressed";
            string thumbstickPath = $"<XRController>{{{handTag}}}/thumbstick";

            if (_triggerAxis == null || _triggerAxis.device == null || !_triggerAxis.device.added)
                _triggerAxis = InputSystem.FindControl(triggerPath) as AxisControl;
            if (_triggerButton == null || _triggerButton.device == null || !_triggerButton.device.added)
                _triggerButton = InputSystem.FindControl(triggerPressedPath) as ButtonControl;
            if (_thumbstick == null || _thumbstick.device == null || !_thumbstick.device.added)
                _thumbstick = InputSystem.FindControl(thumbstickPath) as StickControl;
        }
#endif

        private void SendTriggerHaptics()
        {
            if (!enableTriggerHaptics)
                return;

            float amplitude = Mathf.Clamp01(triggerHapticsAmplitude);
            float duration = Mathf.Clamp(triggerHapticsDuration, 0f, 5f);
            if (amplitude <= 0f || duration <= 0f)
                return;

            XrDevices.Clear();
            InputDevices.GetDevicesAtXRNode(ToXRNode(handSide), XrDevices);
            for (int i = 0; i < XrDevices.Count; i++)
            {
                var device = XrDevices[i];
                if (!device.isValid) continue;
                if (!device.TryGetHapticCapabilities(out HapticCapabilities capabilities) || !capabilities.supportsImpulse)
                    continue;

                device.SendHapticImpulse(0u, amplitude, duration);
                break;
            }
        }

        private static XRNode ToXRNode(HandSide side)
        {
            return side == HandSide.Right ? XRNode.RightHand : XRNode.LeftHand;
        }
    }
}
