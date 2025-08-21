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
/// Represents a brake zone used for slowing down AI vehicles. 
/// If a sharp turn exists in the scene, placing a Brake Zone will 
/// help AI vehicles adjust their speed accordingly. The AI will 
/// adapt to the target speed when inside this zone.
/// </summary>
public class RCC_AIBrakeZone : RCC_Core {

    /// <summary>
    /// The target maximum speed (km/h) that AI vehicles should adhere to while in this Brake Zone.
    /// </summary>
    public float targetSpeed = 50;

    /// <summary>
    /// The effective range of this Brake Zone. AI vehicles will start slowing down within this distance (in meters).
    /// </summary>
    public float distance = 100f;

}
