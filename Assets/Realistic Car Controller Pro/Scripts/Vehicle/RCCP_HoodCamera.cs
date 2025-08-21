//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// When the camera is in "Hood Camera" mode, it will be parented to this GameObject. 
/// A ConfigurableJoint can be used to attach this hood camera to the vehicle's Rigidbody, 
/// helping to reduce camera shake when the vehicle moves.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Hood Camera")]
public class RCCP_HoodCamera : RCCP_Component {

    public override void Start() {

        base.Start();
        CheckJoint();

    }

    /// <summary>
    /// Called to fix a shaking bug by resetting the Rigidbody interpolation, if present.
    /// </summary>
    public void FixShake() {

        StartCoroutine(FixShakeDelayed());

    }

    /// <summary>
    /// Waits for a fixed update, then sets Rigidbody interpolation to None to reduce shake.
    /// </summary>
    private IEnumerator FixShakeDelayed() {

        Rigidbody rb = GetComponent<Rigidbody>();

        if (!rb)
            yield break;

        yield return new WaitForFixedUpdate();
        rb.interpolation = RigidbodyInterpolation.None;

    }

    /// <summary>
    /// Checks for a ConfigurableJoint. If it's missing a connected body, tries to attach it to the vehicle's rigidbody.
    /// Otherwise, removes the joint and rigidbody if no suitable connection is found.
    /// </summary>
    private void CheckJoint() {

        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        if (!joint)
            return;

        // If there's no connectedBody assigned, attempt to connect it to the vehicle's Rigidbody.
        if (!joint.connectedBody) {

            if (CarController) {

                joint.connectedBody = CarController.Rigid;

            } else {

                Debug.LogError("Hood camera on " + transform.root.name + " has a ConfigurableJoint with no connected body. Removing joint and rigidbody.");
                Destroy(joint);

                Rigidbody rb = GetComponent<Rigidbody>();

                if (rb)
                    Destroy(rb);

            }

        }

    }

    /// <summary>
    /// Resets the connected body of the ConfigurableJoint to the car's Rigidbody if available.
    /// </summary>
    public void Reset() {

        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        if (!joint)
            return;

        RCCP_CarController parentCarController = GetComponentInParent<RCCP_CarController>(true);

        if (!parentCarController)
            return;

        joint.connectedBody = parentCarController.GetComponent<Rigidbody>();
        joint.connectedMassScale = 0f;

    }

}
