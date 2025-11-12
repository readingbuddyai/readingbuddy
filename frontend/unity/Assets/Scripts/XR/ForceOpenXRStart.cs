using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
#if UNITY_OPENXR_PRESENT
using UnityEngine.XR.OpenXR;
#endif

/// <summary>
/// Ensures the OpenXR loader is initialised before gameplay begins so that XR subsystems are ready.
/// Attach this to a bootstrap GameObject in a scene that loads first (e.g. _Persistent).
/// </summary>
public class ForceOpenXRStart : MonoBehaviour
{
    private XRManagerSettings _xrManager;
    private bool _subsystemsStarted;

    private void Awake()
    {
        var generalSettings = XRGeneralSettings.Instance;
        if (generalSettings == null)
        {
            Debug.LogWarning("[XR] XRGeneralSettings.Instance is null. OpenXR cannot be initialised.");
            return;
        }

        _xrManager = generalSettings.Manager;
        if (_xrManager == null)
        {
            Debug.LogWarning("[XR] XRManagerSettings is null. OpenXR cannot be initialised.");
            return;
        }

        StartCoroutine(EnsureOpenXRStarted());
    }

    private IEnumerator EnsureOpenXRStarted()
    {
        if (_xrManager == null)
            yield break;

        if (!_xrManager.isInitializationComplete)
        {
            yield return _xrManager.InitializeLoader();

            if (_xrManager.activeLoader == null)
            {
                Debug.LogError("[XR] Failed to initialise XR loader. No active loader present.");
                yield break;
            }
        }

        if (!_subsystemsStarted)
        {
            _xrManager.StartSubsystems();
            _subsystemsStarted = true;
        }

        LogRuntimeInfo();
    }

    private void LogRuntimeInfo()
    {
        var loader = _xrManager != null ? _xrManager.activeLoader : null;
        string loaderName = loader != null ? loader.GetType().Name : "(null)";

        string runtimeName = "(unknown)";
#if UNITY_OPENXR_PRESENT
        runtimeName = OpenXRRuntime.name;
#endif

        XRDisplaySubsystem displaySubsystem = null;
        if (loader != null)
        {
            displaySubsystem = loader.GetLoadedSubsystem<XRDisplaySubsystem>();
        }

        if (displaySubsystem == null)
        {
            // Fallback: try global query (handles cases where loader returns null).
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);
            if (displays.Count > 0)
                displaySubsystem = displays[0];
        }

        string displayInfo = displaySubsystem != null
            ? $"{displaySubsystem.SubsystemDescriptor?.id ?? displaySubsystem.GetType().Name}, running={displaySubsystem.running}"
            : "(none)";

        Debug.Log($"[XR] ActiveLoader: {loaderName}\n[XR] OpenXR Runtime Name: {runtimeName}\n[XR] Display Subsystem: {displayInfo}");
    }

    private void OnDestroy()
    {
        if (_xrManager == null)
            return;

        if (_subsystemsStarted)
        {
            _xrManager.StopSubsystems();
            _subsystemsStarted = false;
        }

        if (_xrManager.isInitializationComplete)
        {
            _xrManager.DeinitializeLoader();
        }
    }
}

