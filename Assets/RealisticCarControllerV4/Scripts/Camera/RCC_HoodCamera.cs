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
/// A special transform designed for RCC's hood camera mode. The RCC camera will be parented here 
/// when the camera mode is set to "Hood Camera". This script also manages a ConfigurableJoint for 
/// stabilization and addresses potential camera shake issues.
/// </summary>
public class RCC_HoodCamera : RCC_Core {

    private void Awake() {

        CheckJoint();

    }

    /// <summary>
    /// Initiates a process to fix the camera shake bug by temporarily adjusting 
    /// the attached Rigidbody's interpolation mode.
    /// </summary>
    public void FixShake() {

        StartCoroutine(FixShakeDelayed());

    }

    /// <summary>
    /// A coroutine that modifies the Rigidbody's interpolation mode to fix camera shaking.
    /// </summary>
    private IEnumerator FixShakeDelayed() {

        // If there's no Rigidbody on this object, exit immediately.
        if (!GetComponent<Rigidbody>())
            yield break;

        // Wait for the next physics update to ensure changes are made at the correct time.
        yield return new WaitForFixedUpdate();

        // Temporarily disable interpolation and then re-enable it after another physics update.
        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
        yield return new WaitForFixedUpdate();
        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;

    }

    /// <summary>
    /// Verifies that the hood camera's ConfigurableJoint is set up properly (connected to the vehicle).
    /// If missing a connected body, it will search for a RCC_CarControllerV4 in the parent hierarchy.
    /// </summary>
    private void CheckJoint() {

        // Attempt to retrieve the ConfigurableJoint on this object.
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        // If no joint is found, there's nothing to fix here.
        if (!joint)
            return;

        // If the joint isn't connected to the car, attempt to find a valid RCC_CarControllerV4.
        if (joint.connectedBody == null) {

            RCC_CarControllerV4 carController = GetComponentInParent<RCC_CarControllerV4>(true);

            if (carController) {
                // Connect the joint to the car's Rigidbody.
                joint.connectedBody = carController.GetComponent<Rigidbody>();
            } else {
                // If no vehicle can be found, disable the joint and the Rigidbody to avoid errors.
                Debug.LogError("Hood camera of the " + transform.root.name + " has a ConfigurableJoint but no connected body! Disabling the rigidbody and joint on this camera.");
                Destroy(joint);

                Rigidbody rigid = GetComponent<Rigidbody>();
                if (rigid)
                    Destroy(rigid);
            }

        }

    }

    /// <summary>
    /// Unity's reset method, called when this script is added or reset in the Inspector. 
    /// Ensures the hood camera's ConfigurableJoint is connected to the vehicle's Rigidbody.
    /// </summary>
    private void Reset() {

        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        if (!joint)
            return;

        RCC_CarControllerV4 carController = GetComponentInParent<RCC_CarControllerV4>(true);
        if (!carController)
            return;

        // Connect to the car's Rigidbody and set default mass scale settings.
        joint.connectedBody = carController.GetComponent<Rigidbody>();
        joint.connectedMassScale = 0f;

    }

}
