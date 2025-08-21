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
/// Represents all essential input values for controlling a vehicle and its camera in RCC.
/// This class is typically filled by RCC_InputManager and consumed by RCC_CarControllerV4, RCC_Camera, etc.
/// </summary>
[System.Serializable]
public class RCC_Inputs {

    /// <summary>
    /// Accelerator input, ranging from 0 (no throttle) to 1 (full throttle).
    /// </summary>
    [Range(0f, 1f)]
    public float throttleInput = 0f;

    /// <summary>
    /// Brake input, ranging from 0 (no brake) to 1 (full brake).
    /// </summary>
    [Range(0f, 1f)]
    public float brakeInput = 0f;

    /// <summary>
    /// Steering input, ranging from -1 (full left) to 1 (full right).
    /// </summary>
    [Range(-1f, 1f)]
    public float steerInput = 0f;

    /// <summary>
    /// Clutch input, ranging from 0 (clutch fully engaged) to 1 (clutch fully disengaged).
    /// </summary>
    [Range(0f, 1f)]
    public float clutchInput = 0f;

    /// <summary>
    /// Handbrake input, ranging from 0 (handbrake off) to 1 (handbrake fully engaged).
    /// </summary>
    [Range(0f, 1f)]
    public float handbrakeInput = 0f;

    /// <summary>
    /// Boost or NOS input, ranging from 0 (no boost) to 1 (full boost).
    /// </summary>
    [Range(0f, 1f)]
    public float boostInput = 0f;

    /// <summary>
    /// Gear index for shifting. Examples: 
    /// -1 = reverse, 0-5 = specific forward gears, -2 = neutral (varies depending on controller logic).
    /// </summary>
    public int gearInput = 0;

    /// <summary>
    /// Horizontal axis value used for orbiting the camera around the vehicle (e.g., right-stick or mouse X).
    /// </summary>
    public float orbitX = 0f;

    /// <summary>
    /// Vertical axis value used for orbiting the camera around the vehicle (e.g., right-stick or mouse Y).
    /// </summary>
    public float orbitY = 0f;

    /// <summary>
    /// Vector2 used for zooming or scrolling camera FOV (e.g., mouse wheel, pinch).
    /// </summary>
    public Vector2 scroll = Vector2.zero;

}
