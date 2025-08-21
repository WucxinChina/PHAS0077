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
/// Tracks the player's vehicle and maintains a cinematic camera angle.
/// This camera smoothly follows the player and rotates to maintain a dynamic perspective.
/// A pivot GameObject named "Animation Pivot" is used, which contains three animation variations.
/// </summary>
public class RCC_CinematicCamera : RCC_Singleton<RCC_CinematicCamera> {

    /// <summary>
    /// The pivot object used for animated camera movement.
    /// </summary>
    public GameObject pivot;

    /// <summary>
    /// The target position the camera should move towards.
    /// </summary>
    private Vector3 targetPosition;

    /// <summary>
    /// The target field of view for the cinematic camera.
    /// </summary>
    public float targetFOV = 60f;

    private void Start() {

        // If the pivot is not assigned in the Inspector, create one dynamically.
        if (!pivot) {

            pivot = new GameObject("Pivot");
            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localRotation = Quaternion.identity;

        }

    }

    private void Update() {

        // Ensure the player camera is available before proceeding.
        if (!RCCSceneManager.activePlayerCamera)
            return;

        // Ensure the player camera has a valid target.
        if (RCCSceneManager.activePlayerCamera.cameraTarget == null)
            return;

        // Ensure the player vehicle exists.
        if (RCCSceneManager.activePlayerCamera.cameraTarget.playerVehicle == null)
            return;

        // Get the player's vehicle transform.
        Transform target = RCCSceneManager.activePlayerCamera.cameraTarget.playerVehicle.transform;

        // Smoothly rotate the camera to align with the player's vehicle.
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(transform.eulerAngles.x, target.eulerAngles.y + 180f, transform.eulerAngles.z),
            Time.deltaTime * 3f
        );

        // Calculate the target position behind the player's vehicle.
        targetPosition = target.position - transform.rotation * Vector3.forward * 10f;

        // Assign the calculated target position to the camera.
        transform.position = targetPosition;

    }

}
