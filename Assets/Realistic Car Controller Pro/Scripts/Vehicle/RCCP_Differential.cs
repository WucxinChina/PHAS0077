//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Transmits power from Engine → Clutch → Gearbox to the axle based on differential settings.
/// Supports Open, Limited-slip, FullLocked and Direct modes.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Differential")]
public class RCCP_Differential : RCCP_Component {

    /// <summary>
    /// If true, skip internal differential logic and use externally overridden outputs.
    /// </summary>
    public bool overrideDifferential = false;

    /// <summary>
    /// Differential operating mode.
    /// </summary>
    public enum DifferentialType {
        Open,
        Limited,
        FullLocked,
        Direct
    }

    /// <summary>
    /// Selected differential mode.
    /// </summary>
    public DifferentialType differentialType = DifferentialType.Limited;

    /// <summary>
    /// Limited-slip percentage (0 = fully open, 100 = fully locked).
    /// </summary>
    [Range(0f, 100f)] public float limitedSlipRatio = 80f;

    /// <summary>
    /// Final drive ratio multiplier for torque.
    /// </summary>
    [Min(0.01f)] public float finalDriveRatio = 3.73f;

    /// <summary>
    /// Torque input from upstream component (gearbox), in Nm.
    /// </summary>
    [Min(0f)] public float receivedTorqueAsNM = 0f;

    /// <summary>
    /// Total torque output after applying finalDriveRatio, in Nm.
    /// </summary>
    public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Absolute RPM of the left wheel.
    /// </summary>
    public float leftWheelRPM = 0f;

    /// <summary>
    /// Absolute RPM of the right wheel.
    /// </summary>
    public float rightWheelRPM = 0f;

    /// <summary>
    /// Normalized slip ratio between wheels (0 = no slip, 1 = maximum slip).
    /// </summary>
    [Range(0f, 1f)] public float wheelSlipRatio = 0f;

    /// <summary>
    /// Slip ratio applied to left wheel (–1 to 1).
    /// </summary>
    public float leftWheelSlipRatio = 0f;

    /// <summary>
    /// Slip ratio applied to right wheel (–1 to 1).
    /// </summary>
    public float rightWheelSlipRatio = 0f;

    /// <summary>
    /// Final torque sent to left wheel, in Nm.
    /// </summary>
    public float outputLeft = 0f;

    /// <summary>
    /// Final torque sent to right wheel, in Nm.
    /// </summary>
    public float outputRight = 0f;

    /// <summary>
    /// Axle component this differential drives. Must be assigned.
    /// </summary>
    public RCCP_Axle connectedAxle;

    private void FixedUpdate() {

        if (connectedAxle == null)
            return;

        if (!overrideDifferential) {


            CalculateSlip();

        }

        DistributeTorque();

    }

    /// <summary>
    /// Reads wheel RPMs and computes slip ratios based on the selected differentialType.
    /// </summary>
    private void CalculateSlip() {

        // read absolute wheel RPMs
        if (connectedAxle.leftWheelCollider != null && connectedAxle.leftWheelCollider.isActiveAndEnabled)
            leftWheelRPM = Mathf.Abs(connectedAxle.leftWheelCollider.WheelCollider.rpm);
        else
            leftWheelRPM = 0f;

        if (connectedAxle.rightWheelCollider != null && connectedAxle.rightWheelCollider.isActiveAndEnabled)
            rightWheelRPM = Mathf.Abs(connectedAxle.rightWheelCollider.WheelCollider.rpm);
        else
            rightWheelRPM = 0f;

        float sumRPM = leftWheelRPM + rightWheelRPM;
        float diffRPM = leftWheelRPM - rightWheelRPM;

        // normalized slip ratio (0–1)
        wheelSlipRatio = (sumRPM > 0f)
            ? Mathf.Clamp01(Mathf.Abs(diffRPM) / sumRPM)
            : 0f;

        // per-wheel slip based on mode
        switch (differentialType) {

            case DifferentialType.Open:
                ApplyOpenSlip(diffRPM);
                break;

            case DifferentialType.Limited:
                ApplyLimitedSlip(diffRPM);
                break;

            case DifferentialType.FullLocked:
            case DifferentialType.Direct:
                leftWheelSlipRatio = 0f;
                rightWheelSlipRatio = 0f;
                break;
        }

    }

    /// <summary>
    /// Splits the received torque (based on finalDriveRatio and slip ratios) across the left and right wheels,
    /// handling both forward and reverse by distributing on magnitude then reapplying the sign.
    /// </summary>
    private void DistributeTorque() {

        // 1) compute raw total (may be negative in reverse)
        float rawTotal = receivedTorqueAsNM * finalDriveRatio;

        // 2) extract sign (+1 forward, -1 reverse) and absolute magnitude
        float driveSign = Mathf.Sign(rawTotal);
        float absProduced = Mathf.Abs(rawTotal);

        // 3) even split on magnitude
        float half = absProduced * 0.5f;

        // 4) adjust each side by its slip ratio (slip can be ±1)
        //    positive slip ratio => that wheel loses torque (brake)
        float leftMag = half - (absProduced * leftWheelSlipRatio);
        float rightMag = half - (absProduced * rightWheelSlipRatio);

        // 5) reapply drive sign so “brake” torque stays opposite the drive direction
        outputLeft = leftMag * driveSign;
        outputRight = rightMag * driveSign;

        // 6) store signed total back for diagnostics
        producedTorqueAsNM = outputLeft + outputRight;

        // 7) send to axle
        connectedAxle.isPower = true;
        connectedAxle.ReceiveOutput(outputLeft, outputRight);

    }


    /// <summary>
    /// Open diff: torque shifts entirely based on slip ratio.
    /// </summary>
    private void ApplyOpenSlip(float diffRPM) {

        if (Mathf.Approximately(diffRPM, 0f)) {
            leftWheelSlipRatio = 0f;
            rightWheelSlipRatio = 0f;
        } else if (diffRPM > 0f) {
            leftWheelSlipRatio = wheelSlipRatio;
            rightWheelSlipRatio = -wheelSlipRatio;
        } else {
            leftWheelSlipRatio = -wheelSlipRatio;
            rightWheelSlipRatio = wheelSlipRatio;
        }

    }

    /// <summary>
    /// Limited-slip: residual slip = wheelSlipRatio * (1 – limitedSlipRatio/100).
    /// </summary>
    private void ApplyLimitedSlip(float diffRPM) {

        float factor = 1f - (limitedSlipRatio / 100f);
        float scaled = wheelSlipRatio * factor;

        if (Mathf.Approximately(diffRPM, 0f)) {
            leftWheelSlipRatio = 0f;
            rightWheelSlipRatio = 0f;
        } else if (diffRPM > 0f) {
            leftWheelSlipRatio = scaled;
            rightWheelSlipRatio = -scaled;
        } else {
            leftWheelSlipRatio = -scaled;
            rightWheelSlipRatio = scaled;
        }

    }

    /// <summary>
    /// External override: immediately set outputs and bypass internal logic.
    /// </summary>
    public void OverrideDifferential(float targetOutputLeft, float targetOutputRight) {

        overrideDifferential = true;
        outputLeft = targetOutputLeft;
        outputRight = targetOutputRight;
        producedTorqueAsNM = outputLeft + outputRight;

        connectedAxle.isPower = true;
        connectedAxle.ReceiveOutput(outputLeft, outputRight);

    }

    public void DisableOverride() {

        overrideDifferential = false;

    }

    /// <summary>
    /// Receive torque input from upstream component (e.g. gearbox).
    /// </summary>
    public void ReceiveOutput(RCCP_Output output) {

        receivedTorqueAsNM = output.NM;

    }

    /// <summary>
    /// Reset all runtime state back to defaults.
    /// </summary>
    public void Reload() {

        leftWheelRPM = 0f;
        rightWheelRPM = 0f;
        wheelSlipRatio = 0f;
        leftWheelSlipRatio = 0f;
        rightWheelSlipRatio = 0f;
        outputLeft = 0f;
        outputRight = 0f;
        receivedTorqueAsNM = 0f;
        producedTorqueAsNM = 0f;

    }

}
