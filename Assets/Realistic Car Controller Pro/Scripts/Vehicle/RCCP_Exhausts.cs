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
/// Exhaust manager. All RCCP_Exhaust components in the vehicle should be connected to this manager.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Exhausts")]
public class RCCP_Exhausts : RCCP_Component {

    /// <summary>
    /// Internal array of RCCP_Exhaust components found in the vehicle.
    /// </summary>
    private RCCP_Exhaust[] _exhausts;

    /// <summary>
    /// Returns an array of all RCCP_Exhaust components attached to the parent RCCP_CarController.
    /// </summary>
    public RCCP_Exhaust[] Exhaust => _exhausts;

    public override void Awake() {

        base.Awake();

        GetAllExhausts();

    }

    /// <summary>
    /// Refreshes the internal array of exhaust components attached to this manager.
    /// </summary>
    public void GetAllExhausts() {

        _exhausts = GetComponentsInChildren<RCCP_Exhaust>(true);

    }

}
