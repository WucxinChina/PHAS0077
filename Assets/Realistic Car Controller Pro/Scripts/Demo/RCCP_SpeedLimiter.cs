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
/// Used to slow down the vehicle by increasing drag.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Speed Limiter")]
public class RCCP_SpeedLimiter : RCCP_GenericComponent {

    /// <summary>
    /// Default drag value of the rigidbody.
    /// </summary>
    private float defaultDrag = -1f;

    private void OnTriggerStay(Collider other) {

        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        if (!carController)
            return;

        if (defaultDrag == -1)
            defaultDrag = carController.Rigid.linearDamping;

        carController.Rigid.linearDamping = .02f * carController.absoluteSpeed;

    }

    private void OnTriggerExit(Collider other) {

        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        if (!carController)
            return;

        carController.Rigid.linearDamping = defaultDrag;

    }

}
