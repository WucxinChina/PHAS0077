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
/// Connector between engine and the gearbox. Transmits the received power from the engine to the gearbox based on clutch input.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Clutch")]
public class RCCP_Clutch : RCCP_Component {

    /// <summary>
    /// If true, overrides all calculations and uses a custom clutch input set externally.
    /// </summary>
    public bool overrideClutch = false;

    /// <summary>
    /// Current clutch input, clamped between 0 and 1. 
    /// - 0 means fully engaged clutch (no slip), 
    /// - 1 means clutch is fully pressed (full slip).
    /// </summary>
    [Range(0f, 1f)] public float clutchInput = 1f;

    /// <summary>
    /// Raw input used to gradually reach the actual clutch input. 
    /// Useful for smoothing transitions when using an automatic clutch.
    /// </summary>
    [Range(0f, 1f)] private float clutchInputRaw = 1f;

    /// <summary>
    /// How quickly the clutch input changes (higher inertia = slower changes). Only used when automaticClutch is true.
    /// </summary>
    [Range(0f, 1f)] public float clutchInertia = .5f;

    /// <summary>
    /// If true, clutch input is automatically calculated. If false, clutchInput_P from the player is used directly.
    /// </summary>
    public bool automaticClutch = true;

    /// <summary>
    /// If true, forces the clutch input to 1 (fully pressed).
    /// </summary>
    public bool forceToPressClutch = false;

    /// <summary>
    /// If true, clutch input is forced to 1 (fully pressed) while shifting gears.
    /// </summary>
    public bool pressClutchWhileShiftingGears = true;

    /// <summary>
    /// If true, clutch input is forced to 1 (fully pressed) while the handbrake is applied.
    /// </summary>
    public bool pressClutchWhileHandbraking = true;

    /// <summary>
    /// If engine RPM falls below this value, the clutch input will be increased to avoid stalling (when automaticClutch is true).
    /// </summary>
    [Min(0f)] public float engageRPM = 1600f;

    /// <summary>
    /// Torque received from the previous component (usually the engine).
    /// </summary>
    public float receivedTorqueAsNM = 0f;

    /// <summary>
    /// Torque delivered to the next component (usually the gearbox).
    /// </summary>
    public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Output event with a custom output class, holding the torque.
    /// </summary>
    public RCCP_Event_Output outputEvent = new RCCP_Event_Output();
    public RCCP_Output output = new RCCP_Output();

    private void FixedUpdate() {

        // Calculating clutch input based on engine RPM, speed, etc. (or player input if automaticClutch is false).
        Input();

        // Delivering torque to the gearbox or other connected component.
        Output();

    }

    private void Input() {

        if (overrideClutch)
            return;

        if (automaticClutch) {

            float throttleInput = CarController.throttleInput_P;
            float currentRPM = CarController.engineRPM;
            bool isShifting = CarController.shiftingNow;
            bool isHandbraking = CarController.handbrakeInput_V >= .75f;
            float currentSpeed = CarController.absoluteSpeed;
            //bool isInNeutral = CarController.NGearNow;

            float targetClutch = 1f - Mathf.Clamp01(throttleInput * 10f);

            if (currentSpeed >= 10f)
                targetClutch = 0f;

            //if (!isInNeutral && currentSpeed < 10f)
            //targetClutch += Mathf.Clamp01(1f - (currentSpeed / 10f));

            targetClutch = Mathf.Clamp01(targetClutch);

            if (throttleInput > .1f && currentRPM < engageRPM)
                targetClutch += Mathf.Lerp(0f, .25f, Mathf.InverseLerp(0f, 100f, engageRPM - currentRPM));

            // stay in [0,1]
            targetClutch = Mathf.Clamp01(targetClutch);

            // 1) full slip while shifting
            if (pressClutchWhileShiftingGears && isShifting)
                targetClutch = 1f;

            // 2) full slip while handbraking
            if (pressClutchWhileHandbraking && isHandbraking)
                targetClutch = 1f;

            // smooth toward that target (0 inertia = instant, 1 = locked)
            float maxDelta = 1f - clutchInertia;
            clutchInputRaw = Mathf.MoveTowards(clutchInputRaw, targetClutch, maxDelta * Time.fixedDeltaTime * 2f);

            // apply and snap at the ends
            clutchInput = clutchInputRaw;

            if (clutchInputRaw > .98f && clutchInput > .95f)
                clutchInput = 1f;

            if (clutchInputRaw < .02f && clutchInput < .05f)
                clutchInput = 0f;

        }

        // forced full press?
        if (forceToPressClutch)
            clutchInput = 1f;

        // stay in [0,1]
        clutchInput = Mathf.Clamp01(clutchInput);

    }

    /// <summary>
    /// Overrides the internally calculated clutch input with a specific value.
    /// </summary>
    /// <param name="targetInput">Value between 0 and 1 (0 = fully engaged, 1 = fully pressed)</param>
    public void OverrideInput(float targetInput) {

        overrideClutch = true;
        clutchInput = targetInput;
        clutchInputRaw = targetInput;

    }

    public void DisableOverride() {

        overrideClutch = false;

    }

    /// <summary>
    /// Called by the previous component in the drivetrain to deliver torque into this clutch.
    /// </summary>
    /// <param name="output"></param>
    public void ReceiveOutput(RCCP_Output output) {

        receivedTorqueAsNM = output.NM;

    }

    /// <summary>
    /// Outputs the torque after applying the clutch slip factor.
    /// </summary>
    private void Output() {

        if (output == null)
            output = new RCCP_Output();

        // If clutch is fully pressed (1), torque is near 0. If not pressed (0), full torque is passed through.
        producedTorqueAsNM = receivedTorqueAsNM * (1f - clutchInput);

        output.NM = producedTorqueAsNM;
        outputEvent.Invoke(output);

    }

    /// <summary>
    /// Resets essential clutch variables to their default states.
    /// </summary>
    public void Reload() {

        clutchInput = 1f;
        clutchInputRaw = 1f;
        receivedTorqueAsNM = 0f;
        producedTorqueAsNM = 0f;

    }

}