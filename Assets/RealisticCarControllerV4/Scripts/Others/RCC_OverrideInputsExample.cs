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
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Demonstrates how to override vehicle inputs in Realistic Car Controller. 
/// By assigning values directly to RCC_Inputs and calling OverrideInputs() on a RCC_CarControllerV4, 
/// user-defined control values can temporarily replace normal player inputs.
/// </summary>
public class RCC_OverrideInputsExample : RCC_Core {

    /// <summary>
    /// Reference to the target vehicle whose controls will be overridden. 
    /// If <c>takePlayerVehicle</c> is true, automatically uses the active player vehicle.
    /// </summary>
    public RCC_CarControllerV4 targetVehicle;

    /// <summary>
    /// If true, this script will automatically grab the current active player vehicle 
    /// from the RCC Scene Manager each frame.
    /// </summary>
    public bool takePlayerVehicle = true;

    /// <summary>
    /// The custom RCC_Inputs struct holding the override input values (throttle, brake, steering, etc.).
    /// </summary>
    public RCC_Inputs newInputs = new RCC_Inputs();

    /// <summary>
    /// Tracks whether overriding is currently enabled. If true, <c>targetVehicle</c> will use <c>newInputs</c>.
    /// </summary>
    private bool overrideNow = false;

    // UI references for controlling input values.

    /// <summary>
    /// Slider for throttle input override (0-1).
    /// </summary>
    public Slider throttle;

    /// <summary>
    /// Slider for brake input override (0-1).
    /// </summary>
    public Slider brake;

    /// <summary>
    /// Slider for steering input override (-1 to 1). 
    /// Ensure the slider is configured appropriately (e.g., min=-1, max=1).
    /// </summary>
    public Slider steering;

    /// <summary>
    /// Slider for handbrake input override (0-1).
    /// </summary>
    public Slider handbrake;

    /// <summary>
    /// Slider for nitrous/boost input override (0-1).
    /// </summary>
    public Slider nos;

    /// <summary>
    /// Text element (TMP) for displaying status of override inputs.
    /// </summary>
    public TextMeshProUGUI statusText;

    private void Update() {

        // Update the custom input struct based on UI slider values each frame.
        newInputs.throttleInput = throttle.value;
        newInputs.brakeInput = brake.value;
        newInputs.steerInput = steering.value;
        newInputs.handbrakeInput = handbrake.value;
        newInputs.boostInput = nos.value;

        // If designated, automatically grab the current active player vehicle from RCC Scene Manager.
        if (takePlayerVehicle)
            targetVehicle = RCCSceneManager.activePlayerVehicle;

        // If overriding is enabled and a valid target vehicle is found, apply the new input overrides.
        if (targetVehicle && overrideNow)
            targetVehicle.OverrideInputs(newInputs);

        // Update the status text to indicate whether overriding is enabled or disabled.
        if (statusText && targetVehicle)
            statusText.text = "Status: " + (overrideNow ? "Enabled" : "Disabled");

    }

    /// <summary>
    /// Enables input overriding for the target vehicle. 
    /// Calls OverrideInputs() so that <c>targetVehicle</c> immediately starts using <c>newInputs</c>.
    /// </summary>
    public void EnableOverride() {

        if (!targetVehicle)
            return;

        overrideNow = true;
        targetVehicle.OverrideInputs(newInputs);

    }

    /// <summary>
    /// Disables input overriding for the target vehicle. 
    /// Calls DisableOverrideInputs() to resume normal player input.
    /// </summary>
    public void DisableOverride() {

        if (!targetVehicle)
            return;

        overrideNow = false;
        targetVehicle.DisableOverrideInputs();

    }

}
