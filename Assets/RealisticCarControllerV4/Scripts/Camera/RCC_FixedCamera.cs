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
/// Implements a fixed camera system for the RCC camera. This camera remains stationary relative 
/// to the scene but tracks the player's vehicle by adjusting its field of view (FOV) and orientation.
/// </summary>
public class RCC_FixedCamera : RCC_Singleton<RCC_FixedCamera> {

    /// <summary>
    /// The position the camera is currently aiming toward.
    /// </summary>
    private Vector3 targetPosition;

    /// <summary>
    /// Maximum allowed distance between the camera and the target vehicle.
    /// </summary>
    public float maxDistance = 50f;

    /// <summary>
    /// The actual distance between the camera and the target, updated each frame.
    /// </summary>
    private float distance;

    /// <summary>
    /// Minimum field of view (FOV) the camera can have.
    /// </summary>
    public float minimumFOV = 20f;

    /// <summary>
    /// Maximum field of view (FOV) the camera can have.
    /// </summary>
    public float maximumFOV = 60f;

    /// <summary>
    /// If false, the camera will not track the vehicle's movement or adjust its transform.
    /// </summary>
    public bool canTrackNow = false;

    private void LateUpdate() {

        // If tracking is disabled, do nothing.
        if (!canTrackNow)
            return;

        // Check for valid references in the RCC scene manager and the active player camera.
        if (!RCCSceneManager.activePlayerCamera ||
            RCCSceneManager.activePlayerCamera.cameraTarget == null ||
            RCCSceneManager.activePlayerCamera.cameraTarget.playerVehicle == null)
            return;

        // Retrieve the target (player vehicle) transform.
        Transform target = RCCSceneManager.activePlayerCamera.cameraTarget.playerVehicle.transform;

        // Calculate the current distance between the camera and the target.
        distance = Vector3.Distance(transform.position, target.position);

        // Adjust camera FOV based on distance; if distance is large, increase FOV toward maximumFOV.
        float speed = RCCSceneManager.activePlayerCamera.cameraTarget.Speed;
        RCCSceneManager.activePlayerCamera.targetFieldOfView =
            Mathf.Lerp(distance > maxDistance / 10f ? maximumFOV : 70f,
                       minimumFOV,
                       (distance * 1.5f) / maxDistance);

        // Calculate a target position just ahead of the vehicle's forward direction (accounting for speed).
        targetPosition = target.position;
        targetPosition += target.rotation * Vector3.forward * (speed * .05f);

        // Slightly translate the camera backward based on vehicle speed, simulating a trailing effect.
        transform.Translate((-target.forward * speed) / 50f * Time.deltaTime);

        // Ensure the camera looks at the calculated target position.
        transform.LookAt(targetPosition);

        // If distance to the vehicle exceeds maxDistance, reposition the camera.
        if (distance > maxDistance)
            ChangePosition();

    }

    /// <summary>
    /// Repositions the camera when the distance to the vehicle exceeds maxDistance. 
    /// Uses a raycast to find a suitable point around the vehicle and orients the camera to look at the target.
    /// </summary>
    public void ChangePosition() {

        // If tracking is disabled, do nothing.
        if (!canTrackNow)
            return;

        // Check for valid references in the RCC scene manager and the active player camera.
        if (!RCCSceneManager.activePlayerCamera ||
            RCCSceneManager.activePlayerCamera.cameraTarget == null ||
            RCCSceneManager.activePlayerCamera.cameraTarget.playerVehicle == null)
            return;

        // Retrieve the target (player vehicle) transform.
        Transform target = RCCSceneManager.activePlayerCamera.cameraTarget.playerVehicle.transform;

        // Determine a random angle for positioning the camera around the vehicle.
        float randomizedAngle = Random.Range(-15f, 15f);

        // Perform a raycast to find an unobstructed position near the target.
        RaycastHit hit;
        if (Physics.Raycast(target.position + Vector3.up * 3f,
                            Quaternion.AngleAxis(randomizedAngle, target.up) * target.forward,
                            out hit,
                            maxDistance)
            && !hit.transform.IsChildOf(target)
            && !hit.collider.isTrigger) {

            // If the raycast hits valid geometry, place the camera near that point, then look at the target.
            transform.position = hit.point + Vector3.up * Random.Range(.5f, 2.5f);
            transform.LookAt(target.position);
            transform.position += transform.forward * 5f;

        } else {

            // Otherwise, position the camera behind the target at a random vertical offset.
            transform.position = target.position + Vector3.up * Random.Range(.5f, 2.5f);
            transform.rotation = target.rotation * Quaternion.AngleAxis(randomizedAngle, Vector3.up);
            transform.position += transform.forward * (maxDistance * .9f);
            transform.LookAt(target.position);
            transform.position += transform.forward * 5f;

        }

    }

}
