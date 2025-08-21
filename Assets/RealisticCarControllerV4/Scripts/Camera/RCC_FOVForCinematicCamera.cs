//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Attaches to the "Animation Pivot" of the Cinematic Camera and continuously applies
/// a Field of View (FOV) value to the RCC_CinematicCamera. Typically driven by an animation
/// that updates the FOV field over time, creating smooth cinematic transitions.
/// </summary>
public class RCC_FOVForCinematicCamera : RCC_Core {

    /// <summary>
    /// Reference to the RCC_CinematicCamera component located in the parent hierarchy.
    /// </summary>
    private RCC_CinematicCamera cinematicCamera;

    /// <summary>
    /// The target field of view to be assigned to the cinematic camera.
    /// This value is often animated for cinematic effects.
    /// </summary>
    public float FOV = 30f;

    private void Awake() {

        // Finds the RCC_CinematicCamera component in the parent objects. 
        // This script will control the camera's FOV during runtime.
        cinematicCamera = GetComponentInParent<RCC_CinematicCamera>(true);

    }

    private void Update() {

        // Continuously updates the cinematic camera's FOV based on the current value of FOV.
        // If this value is animated, the camera's FOV changes smoothly over time.
        cinematicCamera.targetFOV = FOV;

    }

}
