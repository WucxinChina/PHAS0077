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
/// Multiplies the received power from the engine --> clutch by x ratio, and transmits it to the differential. 
/// Higher ratios = faster accelerations, lower top speeds. Lower ratios = slower accelerations, higher top speeds.
/// Supports manual, automatic, and a semi-automatic 'DNRP' system.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Gearbox")]
public class RCCP_Gearbox : RCCP_Component {

    /// <summary>
    /// If true, an external script overrides the gear logic, ignoring internal shift calculations.
    /// </summary>
    public bool overrideGear = false;

    /// <summary>
    /// Gear ratios for each forward gear. Indices correspond to gear numbers (0 = 1st gear, 1 = 2nd gear, etc.).
    /// </summary>
    [Min(0.1f)] public float[] gearRatios = new float[] { 4.35f, 2.5f, 1.66f, 1.23f, 1.0f, .85f };

    /// <summary>
    /// Lazy array storing the engine RPM at which each gear would top out. Computed from gearRatios and the engine's max RPM.
    /// </summary>
    public float[] GearRPMs {

        get {

            gearRPMs = new float[gearRatios.Length];

            for (int i = 0; i < gearRPMs.Length; i++) {

                if (GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Engine>(true))
                    gearRPMs[i] = GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Engine>(true).maxEngineRPM / gearRatios[i];

            }

            return gearRPMs;

        }

    }

    /// <summary>
    /// Backing array for gear RPMs. Access via GearRPMs property for automatic calculation.
    /// </summary>
    public float[] gearRPMs;

    /// <summary>
    /// Index of the current forward gear in use. (0 to gearRatios.Length - 1)
    /// </summary>
    [Min(0)] public int currentGear = 0;

    /// <summary>
    /// Indicates whether the gearbox is effectively driving the wheels (1) or in neutral/park (0).
    /// </summary>
    [Min(0f)] public float gearInput = 0f;

    /// <summary>
    /// Forces a neutral-like state if true, regardless of gear logic.
    /// </summary>
    public bool forceToNGear = false;

    /// <summary>
    /// Forces a reverse-like state if true, regardless of gear logic.
    /// </summary>
    public bool forceToRGear = false;

    [System.Serializable]
    public class CurrentGearState {

        /// <summary>
        /// Defines the gearbox's high-level state (Park, Reverse, Neutral, Forward).
        /// </summary>
        public enum GearState { Park, InReverseGear, Neutral, InForwardGear }
        public GearState gearState = GearState.InForwardGear;

    }

    /// <summary>
    /// Tracks which high-level gear state is currently active (e.g., Park, Reverse, Neutral, or Forward).
    /// </summary>
    public CurrentGearState currentGearState;
    public CurrentGearState.GearState defaultGearState = CurrentGearState.GearState.InForwardGear;

    /// <summary>
    /// Computed target speeds for each gear, used in automatic gear shifting logic.
    /// </summary>
    public float[] targetSpeeds;
    public float[] TargetSpeeds {

        get {

            targetSpeeds = FindTargetSpeed();
            return targetSpeeds;

        }

    }

    /// <summary>
    /// Time (in seconds) the gearbox waits to complete a gear shift.
    /// </summary>
    public float shiftingTime = .2f;

    /// <summary>
    /// True while the gearbox is in the middle of a shift delay.
    /// </summary>
    public bool shiftingNow = false;

    /// <summary>
    /// If true, prevents shifting logic from applying a shift too frequently; uses lastTimeShifted as cooldown.
    /// </summary>
    public bool dontShiftTimer = true;

    /// <summary>
    /// Cooldown timer used to prevent back-to-back shifts.
    /// </summary>
    [Min(0f)] public float lastTimeShifted = 0f;

    /// <summary>
    /// Old boolean for automatic or manual, replaced by transmissionType. Kept for backward compatibility.
    /// </summary>
    [System.Obsolete("automaticTransmission in RCCP_Gearbox is obsolete, please use transmissionType instead.")] public bool automaticTransmission = true;

    /// <summary>
    /// Possible transmission modes: fully manual, basic automatic, or automatic with DNRP states.
    /// </summary>
    public enum TransmissionType { Manual, Automatic, Automatic_DNRP }
    public TransmissionType transmissionType = TransmissionType.Automatic;

    /// <summary>
    /// Semi-automatic gear selection: Drive, Neutral, Reverse, Park.
    /// </summary>
    public enum SemiAutomaticDNRPGear { D, N, R, P }
    public SemiAutomaticDNRPGear automaticGearSelector = SemiAutomaticDNRPGear.D;

    /// <summary>
    /// Higher shiftThreshold means the gearbox shifts up at higher RPMs (sportier feel).
    /// </summary>
    [Range(.1f, .9f)] public float shiftThreshold = .8f;

    /// <summary>
    /// RPM threshold to shift up in automatic modes.
    /// </summary>
    [Min(0f)] public float shiftUpRPM = 5500f;

    /// <summary>
    /// RPM threshold to shift down in automatic modes.
    /// </summary>
    [Min(0f)] public float shiftDownRPM = 2750f;

    /// <summary>
    /// Torque received from the clutch (or prior component) in Nm.
    /// </summary>
    public float receivedTorqueAsNM = 0f;

    /// <summary>
    /// Torque produced after multiplying by the current gear ratio, sent to the differential.
    /// </summary>
    public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Output event for sending torque downstream in the drivetrain.
    /// </summary>
    public RCCP_Event_Output outputEvent = new RCCP_Event_Output();
    private RCCP_Output output = new RCCP_Output();

    private void Update() {

        // Countdown timer preventing immediate re-shift.
        if (lastTimeShifted > 0)
            lastTimeShifted -= Time.deltaTime;

        lastTimeShifted = Mathf.Clamp(lastTimeShifted, 0f, 10f);

    }

    private void FixedUpdate() {

        // Skip internal gear logic if overrideGear is true.
        if (!overrideGear) {

            // Basic automatic logic.
            if (transmissionType == TransmissionType.Automatic)
                AutomaticTransmission();

            // Automatic with DNRP states.
            if (transmissionType == TransmissionType.Automatic_DNRP)
                AutomaticTransmissionDNRP();

            // Force states to neutral or reverse if requested.
            if (forceToNGear)
                currentGearState.gearState = CurrentGearState.GearState.Neutral;

            if (forceToRGear)
                currentGearState.gearState = CurrentGearState.GearState.InReverseGear;

            // If shifting is active, treat state as neutral.
            if (shiftingNow)
                currentGearState.gearState = CurrentGearState.GearState.Neutral;

            // If the gear state is neutral or park, effectively no gear input is applied. Otherwise, gear input = 1.
            if (currentGearState.gearState == CurrentGearState.GearState.Park || currentGearState.gearState == CurrentGearState.GearState.Neutral)
                gearInput = 0f;
            else
                gearInput = 1f;

        }

        // Produce final torque output each physics frame.
        Output();

    }

    /// <summary>
    /// Standard automatic gear shifting. Evaluates RPM and speed, then shifts up/down accordingly.
    /// </summary>
    private void AutomaticTransmission() {

        if (overrideGear)
            return;

        float engineRPM = CarController.engineRPM;

        float lowLimit, highLimit;

        // Low limit is the target speed of the gear below the current gear.
        if (currentGear > 0)
            lowLimit = TargetSpeeds[currentGear - 1];

        // High limit is target speed of current gear times shiftThreshold.
        highLimit = TargetSpeeds[currentGear] * shiftThreshold;

        bool canShiftUpNow = false;

        // Conditions for shifting up: not in reverse, engine above shiftUpRPM, speed above highLimit, not already shifting.
        if (currentGear < gearRatios.Length && currentGearState.gearState != CurrentGearState.GearState.InReverseGear && engineRPM >= shiftUpRPM && CarController.wheelRPM2Speed >= highLimit && CarController.speed >= highLimit)
            canShiftUpNow = true;

        if (shiftingNow)
            canShiftUpNow = false;

        bool canShiftDownNow = false;

        // Conditions for shifting down: not first gear, not in reverse, engine below shiftDownRPM, speed below lowLimit, not already shifting.
        if (currentGear > 0 && currentGearState.gearState != CurrentGearState.GearState.InReverseGear && engineRPM <= shiftDownRPM) {

            if (FindEligibleGear() != currentGear)
                canShiftDownNow = true;
            else
                canShiftDownNow = false;

        }

        if (shiftingNow)
            canShiftDownNow = false;

        // If not using shift-timer logic, reset the shift timer whenever we detect a shift need.
        if (!dontShiftTimer)
            lastTimeShifted = 0f;

        // Only shift if we are not shifting currently and cooldown timer is <= ~0.
        if (!shiftingNow && lastTimeShifted <= .02f) {

            if (canShiftDownNow)
                ShiftToGear(FindEligibleGear());

            if (canShiftUpNow)
                ShiftUp();

        }

    }

    /// <summary>
    /// Automatic gear logic but also respects the D/N/R/P states if transmissionType is Automatic_DNRP.
    /// </summary>
    private void AutomaticTransmissionDNRP() {

        if (overrideGear)
            return;

        switch (automaticGearSelector) {

            case SemiAutomaticDNRPGear.D:
                currentGearState.gearState = CurrentGearState.GearState.InForwardGear;
                break;

            case SemiAutomaticDNRPGear.N:
                currentGearState.gearState = CurrentGearState.GearState.Neutral;
                break;

            case SemiAutomaticDNRPGear.R:
                currentGearState.gearState = CurrentGearState.GearState.InReverseGear;
                break;

            case SemiAutomaticDNRPGear.P:
                currentGearState.gearState = CurrentGearState.GearState.Park;
                break;

        }

        float engineRPM = CarController.engineRPM;

        float lowLimit, highLimit;

        if (currentGear > 0)
            lowLimit = TargetSpeeds[currentGear - 1];

        highLimit = TargetSpeeds[currentGear] * shiftThreshold;

        bool canShiftUpNow = false;

        if (currentGear < gearRatios.Length && currentGearState.gearState != CurrentGearState.GearState.InReverseGear && engineRPM >= shiftUpRPM && CarController.wheelRPM2Speed >= highLimit && CarController.speed >= highLimit)
            canShiftUpNow = true;

        if (shiftingNow)
            canShiftUpNow = false;

        bool canShiftDownNow = false;

        if (currentGear > 0 && currentGearState.gearState != CurrentGearState.GearState.InReverseGear && engineRPM <= shiftDownRPM) {

            if (FindEligibleGear() != currentGear)
                canShiftDownNow = true;
            else
                canShiftDownNow = false;

        }

        if (shiftingNow)
            canShiftDownNow = false;

        if (!dontShiftTimer)
            lastTimeShifted = 0f;

        if (!shiftingNow && lastTimeShifted <= .02f) {

            if (canShiftDownNow)
                ShiftToGear(FindEligibleGear());

            if (canShiftUpNow)
                ShiftUp();

        }

    }

    /// <summary>
    /// Accepts torque from the upstream component (e.g., clutch), storing it for the next output calculation.
    /// </summary>
    /// <param name="output">The RCCP_Output containing the torque in Nm.</param>
    public void ReceiveOutput(RCCP_Output output) {

        receivedTorqueAsNM = output.NM;

    }

    /// <summary>
    /// Finds a target speed array, used to decide if the current speed is too high or low for a gear, 
    /// factoring in maximumSpeed from CarController and gearRatios.
    /// </summary>
    /// <returns>Float array of speeds for each gear ratio.</returns>
    private float[] FindTargetSpeed() {

        float[] targetSpeeds = new float[gearRatios.Length];

        for (int i = targetSpeeds.Length - 1; i >= 0; i--) {

            targetSpeeds[i] = (CarController.maximumSpeed) / gearRatios[i];
            targetSpeeds[i] *= gearRatios[^1];

        }

        return targetSpeeds;

    }

    /// <summary>
    /// Returns which gear index is appropriate for the vehicle�s current speed (usually for shifting down).
    /// </summary>
    /// <returns>Index of an eligible gear based on the current speed.</returns>
    private int FindEligibleGear() {

        int eligibleGear = 0;

        for (int i = 0; i < TargetSpeeds.Length; i++) {

            if (CarController.speed < TargetSpeeds[i]) {

                eligibleGear = i;
                break;

            }

        }

        return eligibleGear;

    }

    /// <summary>
    /// Attempts to upshift by one gear or from reverse gear to first gear, if conditions allow.
    /// </summary>
    public void ShiftUp() {

        if (shiftingNow)
            return;

        if (transmissionType == TransmissionType.Automatic_DNRP && automaticGearSelector != SemiAutomaticDNRPGear.D)
            return;

        // If currently in reverse, shifting up puts it into first gear (index 0).
        if (currentGearState.gearState == CurrentGearState.GearState.InReverseGear) {

            StartCoroutine(ShiftTo(0));

        } else {

            if (currentGear < gearRatios.Length - 1)
                StartCoroutine(ShiftTo(currentGear + 1));

        }

    }

    /// <summary>
    /// Attempts to downshift by one gear, or into reverse if already at the first gear.
    /// </summary>
    public void ShiftDown() {

        if (shiftingNow)
            return;

        if (transmissionType == TransmissionType.Automatic_DNRP && automaticGearSelector != SemiAutomaticDNRPGear.D)
            return;

        if (currentGear > 0)
            StartCoroutine(ShiftTo(currentGear - 1));
        else
            ShiftReverse();

    }

    /// <summary>
    /// Initiates a shift into reverse gear, if allowed by speed, transmissions settings, and shift cooldown.
    /// </summary>
    public void ShiftReverse() {

        if (shiftingNow)
            return;

        if (transmissionType == TransmissionType.Automatic_DNRP && automaticGearSelector != SemiAutomaticDNRPGear.R)
            return;

        // Prevent shifting into reverse if the vehicle is moving too fast forward.
        if (CarController.speed > 20f)
            return;

        StartCoroutine(ShiftTo(-1));

    }

    /// <summary>
    /// Immediately attempts to shift to the specified forward gear index, if not already shifting.
    /// </summary>
    /// <param name="gear">Gear index, or -1 for reverse.</param>
    public void ShiftToGear(int gear) {

        if (shiftingNow)
            return;

        StartCoroutine(ShiftTo(gear));

    }

    /// <summary>
    /// Toggles between a neutral state and forward gear state if not shifting, helpful for manual toggles.
    /// </summary>
    public void ShiftToN() {

        if (shiftingNow)
            return;

        if (currentGearState.gearState == CurrentGearState.GearState.Neutral)
            currentGearState.gearState = CurrentGearState.GearState.InForwardGear;
        else
            currentGearState.gearState = CurrentGearState.GearState.Neutral;

    }

    /// <summary>
    /// Coroutine that waits shiftingTime seconds before finalizing a gear change.
    /// </summary>
    /// <param name="gear">Gear index, or -1 for reverse.</param>
    /// <returns></returns>
    private IEnumerator ShiftTo(int gear) {

        shiftingNow = true;

        yield return new WaitForSeconds(shiftingTime);

        lastTimeShifted = 1f;

        if (gear == -1)
            currentGearState.gearState = CurrentGearState.GearState.InReverseGear;
        else
            currentGearState.gearState = CurrentGearState.GearState.InForwardGear;

        gear = Mathf.Clamp(gear, 0, gearRatios.Length - 1);
        currentGear = gear;

        shiftingNow = false;

    }

    /// <summary>
    /// Calculates final torque output based on gear ratio, then invokes outputEvent to pass it downstream (to the differential).
    /// </summary>
    private void Output() {

        if (output == null)
            output = new RCCP_Output();

        producedTorqueAsNM = receivedTorqueAsNM * gearRatios[currentGear] * gearInput;

        if (currentGearState.gearState == CurrentGearState.GearState.InReverseGear)
            producedTorqueAsNM *= -1;

        int activeDifferentials = CarController.ActiveDifferentials;

        // Dividing torque among all listening events if desired. 
        if (activeDifferentials > 0)
            output.NM = producedTorqueAsNM / (float)activeDifferentials;
        else
            output.NM = 0f;

        outputEvent.Invoke(output);

    }

    /// <summary>
    /// Initializes a certain number of forward gears with default ratios.
    /// </summary>
    /// <param name="totalGears">Number of forward gears to create.</param>
    public void InitGears(int totalGears) {

        gearRatios = new float[totalGears];

        float[] gearRatio = new float[gearRatios.Length];
        int[] maxSpeedForGear = new int[gearRatios.Length];
        int[] targetSpeedForGear = new int[gearRatios.Length];

        // Provide default sets of gear ratios for given gear counts.
        if (gearRatios.Length == 1)
            gearRatio = new float[] { 1.0f };

        if (gearRatios.Length == 2)
            gearRatio = new float[] { 2.0f, 1.0f };

        if (gearRatios.Length == 3)
            gearRatio = new float[] { 2.0f, 1.5f, 1.0f };

        if (gearRatios.Length == 4)
            gearRatio = new float[] { 2.86f, 1.62f, 1.0f, .72f };

        if (gearRatios.Length == 5)
            gearRatio = new float[] { 4.23f, 2.52f, 1.66f, 1.22f, 1.0f };

        if (gearRatios.Length == 6)
            gearRatio = new float[] { 4.35f, 2.5f, 1.66f, 1.23f, 1.0f, .85f };

        if (gearRatios.Length == 7)
            gearRatio = new float[] { 4.5f, 2.5f, 1.66f, 1.23f, 1.0f, .9f, .8f };

        if (gearRatios.Length == 8)
            gearRatio = new float[] { 4.6f, 2.5f, 1.86f, 1.43f, 1.23f, 1.05f, .9f, .72f };

        gearRatios = gearRatio;

    }

    /// <summary>
    /// Allows an external script to override the current gear index and gear state (Park, Reverse, Neutral, or Forward).
    /// </summary>
    /// <param name="targetGear">Gear index (0-based) or -1 for reverse.</param>
    /// <param name="gearState">High-level gear state to assign.</param>
    public void OverrideGear(int targetGear, float targetGearInput, CurrentGearState.GearState gearState) {

        overrideGear = true;
        currentGear = targetGear;
        gearInput = targetGearInput;
        currentGearState.gearState = gearState;

    }

    public void DisableOverride() {

        overrideGear = false;

    }

    /// <summary>
    /// Resets all gearbox states (e.g., shiftingNow, lastTimeShifted, currentGear) to defaults. Usually called on vehicle reactivation.
    /// </summary>
    public void Reload() {

        // Stop any active shifting coroutines to prevent conflicts
        StopAllCoroutines();

        // Reset gear states
        shiftingNow = false;
        currentGearState.gearState = defaultGearState;
        lastTimeShifted = 0f;
        currentGear = 0;
        gearInput = 0f;
        receivedTorqueAsNM = 0f;
        producedTorqueAsNM = 0f;

    }

}
