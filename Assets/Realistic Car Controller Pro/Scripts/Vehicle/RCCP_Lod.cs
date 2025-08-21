//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A "Level of Detail" system for the RCCP vehicle controller that enables or disables more CPU-intensive components based on distance to the main camera.
/// This *does not* handle visual LOD meshes, but rather toggles expensive logic (e.g., WheelColliders alignment, skid drawing, audio, etc.).
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP LOD")]
[DefaultExecutionOrder(10)]
public class RCCP_Lod : RCCP_Component {

    /// <summary>
    /// References to the most performance-heavy vehicle components, which can be toggled off at higher LOD levels.
    /// </summary>
    private RCCP_WheelCollider[] wheelColliders;
    private RCCP_Lights lights;
    private RCCP_Audio audios;
    private RCCP_AeroDynamics aeroDynamics;
    private RCCP_Stability stability;
    private RCCP_Damage damage;
    private RCCP_Particles particles;
    private RCCP_WheelBlur wheelBlur;

    /// <summary>
    /// If true, forces the vehicle to use the lowest LOD level (0), regardless of distance.
    /// </summary>
    public bool forceToFirstLevel = false;

    /// <summary>
    /// If true, forces the vehicle to use LOD level 2 (a more aggressive reduction), regardless of distance.
    /// </summary>
    public bool forceToLatestLevel = false;

    /// <summary>
    /// Current LOD level [0..3]. Level 0 = full detail, 1..3 progressively disable more components.
    /// </summary>
    [Range(0, 3)] public int currentLODLevel = 0;

    /// <summary>
    /// Cached old LOD level to detect transitions in LOD.
    /// </summary>
    private int currentLODLevel_Old = 0;

    /// <summary>
    /// User-facing LOD factor, controlling how quickly LOD distances scale.
    /// </summary>
    [Range(.1f, 1f)] public float lodFactor = .8f;

    /// <summary>
    /// Internal property computing a factor used to scale distance thresholds. 
    /// Creates a range from ~0.5 to ~1.5 as 'lodFactor' goes from 0.1 to 1.
    /// </summary>
    private float LODFactor {

        get {

            return .5f + Mathf.InverseLerp(.1f, 1f, lodFactor);

        }

    }

    public override void Awake() {

        base.Awake();

        // Initialize LOD levels to 0 on awake.
        currentLODLevel = 0;
        currentLODLevel_Old = 0;

        // Gather references to important components that can be toggled off for performance at distance.
        wheelColliders = CarController.AllWheelColliders.ToArray();
        lights = CarController.Lights;
        audios = CarController.Audio;
        aeroDynamics = CarController.AeroDynamics;
        stability = CarController.Stability;
        damage = CarController.Damage;
        particles = CarController.Particles;

        if (CarController.OtherAddonsManager)
            wheelBlur = CarController.OtherAddonsManager.WheelBlur;

    }

    public override void OnEnable() {

        base.OnEnable();

        // Immediately apply current LOD logic on enable.
        LODLevelChanged();

    }

    private int GetLODLevel() {

        // If there's an active main camera in the scene, compute LOD based on distance from camera to this vehicle.
        if (RCCPSceneManager.activeMainCamera) {

            float distanceToCamera = Vector3.Distance(transform.position, RCCPSceneManager.activeMainCamera.transform.position);

            // We define each LOD threshold scaled by LODFactor. 
            // Increase these if you want LOD to switch less frequently, or decrease to switch more aggressively.
            if (distanceToCamera < (50f * LODFactor))
                return 0;
            else if (distanceToCamera < (60f * LODFactor))
                return 1;
            else if (distanceToCamera < (75f * LODFactor))
                return 2;
            else if (distanceToCamera < (100f * LODFactor))
                return 3;

            return 3;

        }

        // If no main camera is present, default to LOD 0 (highest detail).
        return 0;

    }

    private void Update() {

        // If forced LOD levels are requested, override normal distance-based logic.
        if (forceToFirstLevel || forceToLatestLevel) {

            if (forceToFirstLevel)
                currentLODLevel = 0;
            else if (forceToLatestLevel)
                currentLODLevel = 2;

            LODLevelChanged();

        } else {

            // Otherwise, decide LOD level based on distance.
            currentLODLevel = GetLODLevel();

        }

        // Check if we have changed the LOD level since last frame, apply changes if so.
        if (currentLODLevel != currentLODLevel_Old)
            LODLevelChanged();

        currentLODLevel_Old = currentLODLevel;

    }

    /// <summary>
    /// Applies enabling/disabling logic for various components based on the new LOD level.
    /// Levels:
    /// 0 = full detail (everything enabled),
    /// 1 = partial detail,
    /// 2 = more simplified, 
    /// 3 = minimal processing.
    /// </summary>
    private void LODLevelChanged() {

        switch (currentLODLevel) {

            case 0:

                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = true;
                        wheelColliders[i].drawSkid = true;

                    }

                }

                if (lights)
                    lights.enabled = true;

                if (audios)
                    audios.enabled = true;

                if (aeroDynamics)
                    aeroDynamics.enabled = true;

                if (stability)
                    stability.enabled = true;

                if (damage)
                    damage.enabled = true;

                if (particles)
                    particles.enabled = true;

                if (wheelBlur)
                    wheelBlur.enabled = true;

                break;

            case 1:

                // Medium detail, disabling some components (e.g. lights, aero, stability) for performance gains.
                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = false;
                        wheelColliders[i].drawSkid = false;

                    }

                }

                if (lights)
                    lights.enabled = false;

                if (audios)
                    audios.enabled = true;

                if (aeroDynamics)
                    aeroDynamics.enabled = false;

                if (stability)
                    stability.enabled = false;

                if (damage)
                    damage.enabled = false;

                if (particles)
                    particles.enabled = false;

                if (wheelBlur)
                    wheelBlur.enabled = false;

                break;

            case 2:

                // Lower detail, disabling audio and more systems.
                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = false;
                        wheelColliders[i].drawSkid = false;

                    }

                }

                if (lights)
                    lights.enabled = false;

                if (audios)
                    audios.enabled = false;

                if (aeroDynamics)
                    aeroDynamics.enabled = false;

                if (stability)
                    stability.enabled = false;

                if (damage)
                    damage.enabled = false;

                if (particles)
                    particles.enabled = false;

                if (wheelBlur)
                    wheelBlur.enabled = false;

                break;

            case 3:

                // Minimal detail, everything heavy is disabled.
                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = false;
                        wheelColliders[i].drawSkid = false;

                    }

                }

                if (lights)
                    lights.enabled = false;

                if (audios)
                    audios.enabled = false;

                if (aeroDynamics)
                    aeroDynamics.enabled = false;

                if (stability)
                    stability.enabled = false;

                if (damage)
                    damage.enabled = false;

                if (particles)
                    particles.enabled = false;

                if (wheelBlur)
                    wheelBlur.enabled = false;

                break;

        }

    }

    /// <summary>
    /// Resets LOD to level 0 and re-applies relevant changes. Typically called when re-initializing the vehicle.
    /// </summary>
    public void Reload() {

        currentLODLevel = 0;
        currentLODLevel_Old = 0;

        LODLevelChanged();

    }

}
