//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the rotation and positioning of the brake caliper based on the associated RCC_WheelCollider.
/// This script ensures that the brake caliper rotates correctly with camber, caster, and steering angles.
/// </summary>
public class RCC_Caliper : RCC_Core {

    /// <summary>
    /// Reference to the associated RCC_WheelCollider.
    /// </summary>
    public RCC_WheelCollider wheelCollider;

    /// <summary>
    /// A pivot GameObject created at runtime to correctly position and rotate the caliper.
    /// </summary>
    private GameObject newPivot;

    /// <summary>
    /// Stores the default local rotation of the caliper for reference.
    /// </summary>
    private Quaternion defLocalRotation;

    private void Awake() {

        // No need to proceed if the WheelCollider is not assigned.
        if (!wheelCollider) {

            Debug.LogError("WheelCollider is not assigned for this caliper: " + transform.name);
            enabled = false;
            return;

        }

        // Creating a new pivot GameObject to correct positioning issues.
        newPivot = new GameObject("Pivot_" + transform.name);
        newPivot.transform.SetParent(wheelCollider.WheelCollider.transform, false);

        // Re-parenting the caliper to the new pivot for accurate movement.
        transform.SetParent(newPivot.transform, true);

        // Storing the default local rotation of the pivot for future use.
        defLocalRotation = newPivot.transform.localRotation;

    }

    private void LateUpdate() {

        // No need to proceed if the WheelCollider or its model is missing.
        if (!wheelCollider.wheelModel || !wheelCollider.WheelCollider)
            return;

        // Determine if the wheel is on the left (-1) or right (+1) side of the vehicle.
        int side = wheelCollider.transform.localPosition.x < 0 ? -1 : 1;

        // Positioning the pivot at the correct height based on suspension movement.
        newPivot.transform.position = wheelCollider.wheelPosition;
        newPivot.transform.localPosition += Vector3.up * wheelCollider.WheelCollider.suspensionDistance / 2f;

        // Applying camber, caster, and steering angles to the caliper's pivot rotation.
        newPivot.transform.localRotation = defLocalRotation *
            Quaternion.Euler(wheelCollider.caster * side, wheelCollider.WheelCollider.steerAngle, wheelCollider.camber * side);

    }

}
