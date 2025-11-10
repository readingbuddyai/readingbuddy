using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace Utils
{
    /// <summary>
    /// 트리거(Activate Action)를 누르는 동안에만 지정한 파티클 빔을 재생하는 컴포넌트.
    /// 파티클은 Looping = true, Play On Awake = false 상태로 세팅해 주세요.
    /// </summary>
    [RequireComponent(typeof(ActionBasedController))]
    public class BeamTriggerActivator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("트리거 입력 동안 재생할 파티클 시스템 목록 (비워두면 자식에서 자동 검색)")]
        public ParticleSystem[] beamParticles;

        [Tooltip("beamParticles가 비어 있으면 자식 파티클을 자동으로 채웁니다.")]
        public bool autoPopulateChildren = true;

        [Tooltip("입력을 읽어올 XR 컨트롤러. 비워두면 같은 오브젝트에서 찾습니다.")]
        public ActionBasedController xrController;

        private InputAction _activateAction;

        private void Awake()
        {
            if (!xrController)
                xrController = GetComponent<ActionBasedController>();

            if ((beamParticles == null || beamParticles.Length == 0) && autoPopulateChildren)
                beamParticles = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void OnEnable()
        {
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();

            StopBeam();
        }

        private void SubscribeInput()
        {
            if (!xrController || xrController.activateAction == null)
                return;

            _activateAction = xrController.activateAction.action;
            if (_activateAction == null)
                return;

            _activateAction.performed += OnTriggerPressed;
            _activateAction.canceled += OnTriggerReleased;
        }

        private void UnsubscribeInput()
        {
            if (_activateAction == null)
                return;

            _activateAction.performed -= OnTriggerPressed;
            _activateAction.canceled -= OnTriggerReleased;
            _activateAction = null;
        }

        private void OnTriggerPressed(InputAction.CallbackContext context)
        {
            PlayBeam();
        }

        private void OnTriggerReleased(InputAction.CallbackContext context)
        {
            StopBeam();
        }

        private void PlayBeam()
        {
            if (beamParticles == null)
                return;

            for (int i = 0; i < beamParticles.Length; i++)
            {
                var ps = beamParticles[i];
                if (!ps) continue;
                if (!ps.isPlaying)
                    ps.Play();
            }
        }

        private void StopBeam()
        {
            if (beamParticles == null)
                return;

            for (int i = 0; i < beamParticles.Length; i++)
            {
                var ps = beamParticles[i];
                if (!ps) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}

