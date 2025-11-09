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
    /// 오른손 XR 컨트롤러 입력(트리거/조이스틱)을 감지해 효과음을 재생하는 유틸 컴포넌트.
    /// Stage11Controller 등 다양한 스테이지에서 재사용할 수 있도록 별도 분리.
    /// </summary>
    public class RightControllerSfx : MonoBehaviour
    {
        [Header("General")]
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
        private AxisControl _rightTriggerAxis;
        private ButtonControl _rightTriggerButton;
        private StickControl _rightThumbstick;
#endif

        private static readonly List<InputDeviceXR> RightHandDevices = new List<InputDeviceXR>();

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

            bool triggerPressed = CheckRightTriggerPressed();
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

            bool thumbstickActive = CheckRightThumbstickEngaged();
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

        private bool CheckRightTriggerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            ResolveInputSystemControls();
            if (_rightTriggerButton != null && _rightTriggerButton.isPressed)
                return true;
            if (_rightTriggerAxis != null && _rightTriggerAxis.ReadValue() >= 0.99f)
                return true;
#endif

            RightHandDevices.Clear();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, RightHandDevices);
            for (int i = 0; i < RightHandDevices.Count; i++)
            {
                var device = RightHandDevices[i];
                if (!device.isValid) continue;
                if (device.TryGetFeatureValue(CommonUsagesXR.triggerButton, out bool triggerButton) && triggerButton)
                    return true;
                if (device.TryGetFeatureValue(CommonUsagesXR.trigger, out float triggerValue) && triggerValue >= 0.99f)
                    return true;
            }

            return false;
        }

        private bool CheckRightThumbstickEngaged()
        {
            float threshold = Mathf.Clamp01(thumbstickActivationThreshold);
            float sqrThreshold = threshold * threshold;

#if ENABLE_INPUT_SYSTEM
            ResolveInputSystemControls();
            if (_rightThumbstick != null)
            {
                Vector2 value = _rightThumbstick.ReadValue();
                if (value.sqrMagnitude >= sqrThreshold)
                    return true;
            }
#endif

            RightHandDevices.Clear();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, RightHandDevices);
            for (int i = 0; i < RightHandDevices.Count; i++)
            {
                var device = RightHandDevices[i];
                if (!device.isValid) continue;
                if (device.TryGetFeatureValue(CommonUsagesXR.primary2DAxis, out Vector2 axisValue) && axisValue.sqrMagnitude >= sqrThreshold)
                    return true;
            }

            return false;
        }

#if ENABLE_INPUT_SYSTEM
        private void ResolveInputSystemControls()
        {
            if (_rightTriggerAxis == null || _rightTriggerAxis.device == null || !_rightTriggerAxis.device.added)
                _rightTriggerAxis = InputSystem.FindControl("<XRController>{RightHand}/trigger") as AxisControl;
            if (_rightTriggerButton == null || _rightTriggerButton.device == null || !_rightTriggerButton.device.added)
                _rightTriggerButton = InputSystem.FindControl("<XRController>{RightHand}/triggerPressed") as ButtonControl;
            if (_rightThumbstick == null || _rightThumbstick.device == null || !_rightThumbstick.device.added)
                _rightThumbstick = InputSystem.FindControl("<XRController>{RightHand}/thumbstick") as StickControl;
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

            RightHandDevices.Clear();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, RightHandDevices);
            for (int i = 0; i < RightHandDevices.Count; i++)
            {
                var device = RightHandDevices[i];
                if (!device.isValid) continue;
                if (!device.TryGetHapticCapabilities(out HapticCapabilities capabilities) || !capabilities.supportsImpulse)
                    continue;

                device.SendHapticImpulse(0u, amplitude, duration);
                break;
            }
        }
    }
}
