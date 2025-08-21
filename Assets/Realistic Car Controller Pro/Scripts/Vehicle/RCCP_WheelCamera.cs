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
/// When the camera is in "Wheel Camera" mode, it will be parented to this GameObject, offering a close-up view of the vehicle’s wheels.
/// Provides an optional fix for camera shake when attached to a rigidbody.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Wheel Camera")]
public class RCCP_WheelCamera : RCCP_Component {

    /// <summary>
    /// Attempts to fix camera shaking issues caused by the rigidbody’s interpolation settings.
    /// </summary>
    public void FixShake() {

        StartCoroutine(FixShakeDelayed());

    }

    private IEnumerator FixShakeDelayed() {

        if (!GetComponent<Rigidbody>())
            yield break;

        // Temporarily disable interpolation, then re-enable it after a fixed update to reduce jitter.
        yield return new WaitForFixedUpdate();
        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
        yield return new WaitForFixedUpdate();
        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;

    }

}
