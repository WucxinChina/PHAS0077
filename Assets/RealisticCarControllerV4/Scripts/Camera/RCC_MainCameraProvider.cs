//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEngine.Rendering;
#endif

/// <summary>
/// Provides cached access to the main camera to avoid the performance overhead of Camera.main.
/// Automatically tracks camera changes and supports Built-in RP, URP, HDRP; handles domain reloads and application quit cleanup.
/// </summary>
public static class RCC_MainCameraProvider {
    // The cached reference to the main camera
    private static Camera cachedCamera;

    /// <summary>
    /// Fired when the main camera changes (a different camera becomes tagged as "MainCamera").
    /// </summary>
    public static event Action<Camera> OnMainCameraChanged;

    /// <summary>
    /// Gets the main camera. Uses cached reference when possible.
    /// Falls back to Camera.main if cache is null or camera is inactive.
    /// </summary>
    public static Camera MainCamera {
        get {
            if (cachedCamera == null || !cachedCamera.gameObject.activeInHierarchy) {
                cachedCamera = Camera.main;
            }
            return cachedCamera;
        }
    }

    /// <summary>
    /// Resets static fields and unsubscribes callbacks on domain reload.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() {
        Camera.onPreCull -= OnCameraPreCull;
#if UNITY_2018_1_OR_NEWER
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#endif
        cachedCamera = null;
        OnMainCameraChanged = null;
    }

    /// <summary>
    /// Subscribes to render callbacks and application quit on load.
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    static void Initialize() {
        Camera.onPreCull += OnCameraPreCull;
#if UNITY_2018_1_OR_NEWER
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#endif
        Application.quitting += Cleanup;
    }

    /// <summary>
    /// Cleans up event subscriptions and clears cache on application quit.
    /// </summary>
    public static void Cleanup() {
        Camera.onPreCull -= OnCameraPreCull;
#if UNITY_2018_1_OR_NEWER
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#endif
        Application.quitting -= Cleanup;
        cachedCamera = null;
    }

    /// <summary>
    /// Called for every camera each frame in Built-in Render Pipeline.
    /// </summary>
    static void OnCameraPreCull(Camera cam) {
        TryUpdate(cam);
    }

#if UNITY_2018_1_OR_NEWER
    /// <summary>
    /// Called for every camera each frame in Scriptable Render Pipeline (URP/HDRP).
    /// </summary>
    static void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam) {
        TryUpdate(cam);
    }
#endif

    /// <summary>
    /// Checks if the provided camera should become the new cached main camera.
    /// Updates cache and fires event if a new main camera is detected.
    /// </summary>
    static void TryUpdate(Camera cam) {
        if (cam == cachedCamera || cam == null || !cam.CompareTag("MainCamera"))
            return;

        cachedCamera = cam;
        OnMainCameraChanged?.Invoke(cam);
    }

    /// <summary>
    /// Forces a refresh of the cached camera reference.
    /// Useful when you know the main camera has changed but automatic detection hasn't caught it.
    /// </summary>
    public static void ForceRefresh() {
        var previous = cachedCamera;
        cachedCamera = Camera.main;
        if (previous != cachedCamera && cachedCamera != null) {
            OnMainCameraChanged?.Invoke(cachedCamera);
        }
    }
}
