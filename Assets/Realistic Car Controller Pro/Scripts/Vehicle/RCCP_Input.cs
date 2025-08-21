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
/// Receives player input from the RCCP_InputManager and processes it before applying to the CarController.
/// Allows optional overriding of player inputs and external control logic.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Input")]
public class RCCP_Input : RCCP_Component {

    /// <summary>
    /// Cached reference to the main RCCP_InputManager instance.
    /// </summary>
    public RCCP_InputManager RCCPInputManager {

        get {

            if (_RCCPInputManager == null)
                _RCCPInputManager = RCCP_InputManager.Instance;

            return _RCCPInputManager;

        }

    }
    private RCCP_InputManager _RCCPInputManager;

    /// <summary>
    /// If true, bypasses the standard RCCP_InputManager inputs and uses your own input values (OverrideInputs).
    /// </summary>
    public bool overridePlayerInputs = false;

    /// <summary>
    /// If true, bypasses certain external logic, such as steering limiter or counter steering.
    /// </summary>
    public bool overrideExternalInputs = false;

    /// <summary>
    /// (Obsolete) Use 'overridePlayerInputs' instead.
    /// </summary>
    [System.Obsolete("Use 'overridePlayerInputs' instead of this.")]
    public bool overrideInternalInputs {

        get {

            return overridePlayerInputs;

        }
        set {

            overridePlayerInputs = value;

        }

    }

    /// <summary>
    /// RCCP_Inputs is a helper struct containing throttle, brake, steer, etc. 
    /// The active values are updated by the PlayerInputs() method or manually overridden.
    /// </summary>
    public RCCP_Inputs inputs = new RCCP_Inputs();

    /// <summary>
    /// Throttle input, ranging from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float throttleInput = 0f;

    /// <summary>
    /// Steering input, ranging from -1 to 1.
    /// </summary>
    [Range(-1f, 1f)] public float steerInput = 0f;

    /// <summary>
    /// Brake input, ranging from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float brakeInput = 0f;

    /// <summary>
    /// Handbrake input, ranging from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float handbrakeInput = 0f;

    /// <summary>
    /// Clutch input, ranging from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float clutchInput = 0f;

    /// <summary>
    /// NOS (Nitrous) input, ranging from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float nosInput = 0f;

    /// <summary>
    /// Steering curve that reduces maximum steer angle as vehicle speed increases.
    /// </summary>
    public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(100f, .2f), new Keyframe(200f, .15f));

    /// <summary>
    /// If true, reduces steering angle if the vehicle is skidding sideways.
    /// </summary>
    public bool steeringLimiter = true;

    /// <summary>
    /// If true, automatically applies slight counter steering based on wheel slip.
    /// </summary>
    public bool counterSteering = true;

    /// <summary>
    /// Strength of the automatic counter steering, from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float counterSteerFactor = .5f;

    /// <summary>
    /// If true, vehicle automatically shifts into reverse gear if brake is held at low speeds.
    /// </summary>
    public bool autoReverse = true;

    /// <summary>
    /// If true, swaps throttle and brake inputs when the vehicle is in reverse gear, for certain driving styles.
    /// </summary>
    public bool inverseThrottleBrakeOnReverse = true;

    /// <summary>
    /// If true, throttle is cut to zero when the gearbox is mid-shift to reduce jerking during gear changes.
    /// </summary>
    public bool cutThrottleWhenShifting = true;

    /// <summary>
    /// If true, brake input is automatically applied (1.0) when the vehicle is not controllable.
    /// </summary>
    public bool applyBrakeOnDisable = false;

    /// <summary>
    /// If true, handbrake input is automatically applied (1.0) when the vehicle is not controllable.
    /// </summary>
    public bool applyHandBrakeOnDisable = true;

    /// <summary>
    /// Deadzone for analog inputs to prevent drift
    /// </summary>
    [Range(0f, 0.2f)] public float steeringDeadzone = 0.05f;
    [Range(0f, 0.2f)] public float throttleDeadzone = 0.05f;
    [Range(0f, 0.2f)] public float brakeDeadzone = 0.05f;
    [Range(0f, 0.2f)] public float handbrakeDeadzone = 0.05f;
    [Range(0f, 0.2f)] public float nosDeadzone = 0.05f;
    [Range(0f, 0.2f)] public float clutchDeadzone = 0.05f;

    private bool oldCanControl, oldExternalControl;

    public override void OnEnable() {

        base.OnEnable();

        oldCanControl = CarController.canControl;
        oldExternalControl = CarController.externalControl;

        // Subscribe to RCCP_InputManager events for toggling lights, gear shifting, etc.
        RCCP_InputManager.OnStartEngine += RCCP_InputManager_OnStartEngine;
        RCCP_InputManager.OnStopEngine += RCCP_InputManager_OnStopEngine;
        RCCP_InputManager.OnSteeringHelper += RCCP_InputManager_OnSteeringHelper;
        RCCP_InputManager.OnTractionHelper += RCCP_InputManager_OnTractionHelper;
        RCCP_InputManager.OnAngularDragHelper += RCCP_InputManager_OnAngularDragHelper;
        RCCP_InputManager.OnABS += RCCP_InputManager_OnABS;
        RCCP_InputManager.OnESP += RCCP_InputManager_OnESP;
        RCCP_InputManager.OnTCS += RCCP_InputManager_OnTCS;
        RCCP_InputManager.OnGearShiftedUp += RCCP_InputManager_OnGearShiftedUp;
        RCCP_InputManager.OnGearShiftedDown += RCCP_InputManager_OnGearShiftedDown;
        RCCP_InputManager.OnGearShiftedTo += RCCP_InputManager_OnGearShiftedTo;
        RCCP_InputManager.OnGearShiftedToN += RCCP_InputManager_OnGearShiftedToN;
        RCCP_InputManager.OnGearToggle += RCCP_InputManager_OnGearToggle;
        RCCP_InputManager.OnAutomaticGear += RCCP_InputManager_OnAutomaticGearChanged;
        RCCP_InputManager.OnPressedLowBeamLights += RCCP_InputManager_OnPressedLowBeamLights;
        RCCP_InputManager.OnPressedHighBeamLights += RCCP_InputManager_OnPressedHighBeamLights;
        RCCP_InputManager.OnPressedLeftIndicatorLights += RCCP_InputManager_OnPressedLeftIndicatorLights;
        RCCP_InputManager.OnPressedRightIndicatorLights += RCCP_InputManager_OnPressedRightIndicatorLights;
        RCCP_InputManager.OnPressedIndicatorLights += RCCP_InputManager_OnPressedIndicatorLights;
        RCCP_InputManager.OnTrailerDetach += RCCP_InputManager_OnTrailerDetach;
        RCCP_InputManager.OnRecord += RCC_InputManager_OnRecord;
        RCCP_InputManager.OnReplay += RCC_InputManager_OnReplay;

    }

    private void Update() {

        // 1. Reset all inputs if canControl or externalControl state changed.
        bool canControlChanged = (CarController.canControl != oldCanControl);
        bool externalControlChanged = (CarController.externalControl != oldExternalControl);
        if (canControlChanged || externalControlChanged) {
            inputs = new RCCP_Inputs();
        }

        // Update old states.
        oldCanControl = CarController.canControl;
        oldExternalControl = CarController.externalControl;

        // 2. Fetch standard inputs from RCCP_InputManager if not overriding them.
        if (!overridePlayerInputs) {
            PlayerInputs();
        }

        // 3. Apply the new inputs to local fields and clamp.
        if (inputs != null) {
            throttleInput = ApplyDeadzone(inputs.throttleInput, throttleDeadzone);
            brakeInput = ApplyDeadzone(inputs.brakeInput, brakeDeadzone);
            steerInput = ApplyDeadzone(inputs.steerInput, steeringDeadzone);
            clutchInput = ApplyDeadzone(inputs.clutchInput, clutchDeadzone);
            handbrakeInput = ApplyDeadzone(inputs.handbrakeInput, handbrakeDeadzone);
            nosInput = ApplyDeadzone(inputs.nosInput, nosDeadzone);
        }

        // 4. Post-process inputs (steering limiter, auto-reverse, etc.)
        //    unless external inputs are fully overridden.
        if (!overrideExternalInputs) {
            VehicleControlledInputs();
        }

    }

    /// <summary>
    /// Overrides the input values with those from the provided struct, then prevents standard input fetching.
    /// </summary>
    /// <param name="overridedInputs">The input struct to apply in place of normal player inputs.</param>
    public void OverrideInputs(RCCP_Inputs overridedInputs) {

        overridePlayerInputs = true;
        inputs = overridedInputs;

    }

    /// <summary>
    /// Restores normal input fetching from RCCP_InputManager instead of an overridden inputs struct.
    /// </summary>
    public void DisableOverrideInputs() {

        overridePlayerInputs = false;

    }

    /// <summary>
    /// Grabs the user's raw input from RCCP_InputManager, if the player can control and no external override is active.
    /// </summary>
    private void PlayerInputs() {

        if (CarController.canControl && !CarController.externalControl)
            inputs = RCCPInputManager.GetInputs();

    }

    /// <summary>
    /// Processes higher-level logic such as auto-reverse gear changes, throttle cut while shifting,
    /// counter steering, steering limiting, and speed-based steering curve.
    /// </summary>
    private void VehicleControlledInputs() {

        // ---------------------------------------------------------------------
        // 1. AUTO-REVERSE GEAR LOGIC (if automatic transmission & autoReverse on)
        // ---------------------------------------------------------------------
        var gearbox = CarController.Gearbox;
        if (gearbox && gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic && autoReverse) {

            // If speed is ~3 or less and brake is heavy, shift into reverse if not already reversing
            if (CarController.speed <= 3f && inputs.brakeInput >= 0.75f && !CarController.shiftingNow) {
                if (!CarController.reversingNow)
                    gearbox.ShiftReverse();
            }
            // If speed is ~3 or more in forward direction while reversing, shift to first gear
            else if (CarController.speed >= -3f && CarController.reversingNow && !CarController.shiftingNow) {
                gearbox.ShiftToGear(0);
            }

        }

        // ---------------------------------------------------------------------
        // 2. CUT THROTTLE WHILE SHIFTING (if enabled)
        // ---------------------------------------------------------------------
        if (cutThrottleWhenShifting && CarController.shiftingNow) {
            throttleInput = 0f;
        }

        // ---------------------------------------------------------------------
        // 3. INVERSE THROTTLE / BRAKE IF REVERSING (only for auto transmissions)
        // ---------------------------------------------------------------------
        bool canInverseInputs = (inverseThrottleBrakeOnReverse && CarController.reversingNow);

        if (gearbox && gearbox.transmissionType != RCCP_Gearbox.TransmissionType.Automatic)
            canInverseInputs = false;

        if (canInverseInputs) {
            float originalThrottle = throttleInput;
            float originalBrake = brakeInput;

            throttleInput = inputs.brakeInput;   // Flip
            brakeInput = inputs.throttleInput;   // Flip
        }

        // ---------------------------------------------------------------------
        // 4. COUNTER STEERING
        // ---------------------------------------------------------------------
        if (counterSteering) {

            float sidewaysSlip = 0f;

            // Average front axle sideways slip
            if (CarController.FrontAxle) {
                sidewaysSlip = (CarController.FrontAxle.leftWheelCollider.SidewaysSlip
                               + CarController.FrontAxle.rightWheelCollider.SidewaysSlip) / 2f;
            }

            // Calculate a factor to apply to the current steer
            float steerInputCounter = sidewaysSlip * counterSteerFactor;
            steerInputCounter = Mathf.Clamp(steerInputCounter, -1f, 1f);

            // Add the counter steer factor to the current steer, scaled by (1 - abs(steerInput))
            steerInput += steerInputCounter * (1f - Mathf.Abs(steerInput));

            if (Mathf.Abs(steerInput) < .02f)
                steerInput = 0f;

        }

        // ---------------------------------------------------------------------
        // 5. STEERING LIMITER (reduce steer if vehicle is skidding significantly)
        // ---------------------------------------------------------------------
        if (steeringLimiter) {

            if (CarController.absoluteSpeed >= 15f) {

                // Gather total sideways slip across all wheel colliders
                float sidewaysSlip = 0f;
                int counter = 0;

                foreach (RCCP_WheelCollider w in CarController.AllWheelColliders) {

                    if (Mathf.Abs(w.WheelCollider.steerAngle) >= .05f) {

                        sidewaysSlip += w.SidewaysSlip;
                        counter++;

                    }

                }

                if (counter > 0)
                    sidewaysSlip /= counter;

                float absSlip = Mathf.Abs(sidewaysSlip);
                float clampValue = 1f - Mathf.Clamp01(absSlip);

                // If we're skidding to the right (sidewaysSlip > 0),
                // limit steering to the right, allow more steering to the left.
                if (sidewaysSlip < 0f) {
                    // For instance, clamp right steer to clampValue, but allow full left steer
                    steerInput = Mathf.Clamp(steerInput, -1f, clampValue);
                }
                // If we're skidding to the left (sidewaysSlip < 0),
                // do the opposite
                else if (sidewaysSlip > 0f) {
                    steerInput = Mathf.Clamp(steerInput, -clampValue, 1f);
                }

            }

        }

        // ---------------------------------------------------------------------
        // 6. APPLY SPEED-BASED STEERING CURVE
        // ---------------------------------------------------------------------
        if (steeringCurve != null) {
            float absSpeed = CarController.absoluteSpeed;
            steerInput *= steeringCurve.Evaluate(absSpeed);
        }
    }

    /// <summary>
    /// Apply deadzone and sensitivity curve to input
    /// </summary>
    private float ApplyDeadzone(float input, float deadzone) {

        float absInput = Mathf.Abs(input);

        // Apply deadzone
        if (absInput < deadzone)
            return 0f;

        // Remap input to remove deadzone gap
        float remappedInput = (absInput - deadzone) / (1f - deadzone);

        // Restore sign
        return remappedInput * Mathf.Sign(input);

    }

    /// <summary>
    /// Resets all internal input values to zero. Called internally when canControl changes or externally if needed.
    /// </summary>
    public void ResetInputs() {

        inputs = new RCCP_Inputs();

        throttleInput = 0f;
        steerInput = 0f;
        brakeInput = 0f;
        handbrakeInput = 0f;
        clutchInput = 0f;
        nosInput = 0f;

    }

    #region RCCP InputManager Event Listeners

    private void RCCP_InputManager_OnPressedIndicatorLights() {

        if (!CarController.Lights)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Lights.indicatorsAll = !CarController.Lights.indicatorsAll;
        CarController.Lights.indicatorsLeft = false;
        CarController.Lights.indicatorsRight = false;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched All Indicators To " + CarController.Lights.indicatorsAll);

    }

    private void RCCP_InputManager_OnPressedRightIndicatorLights() {

        if (!CarController.Lights)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Lights.indicatorsRight = !CarController.Lights.indicatorsRight;
        CarController.Lights.indicatorsLeft = false;
        CarController.Lights.indicatorsAll = false;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Right Indicators To " + CarController.Lights.indicatorsRight);

    }

    private void RCCP_InputManager_OnPressedLeftIndicatorLights() {

        if (!CarController.Lights)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Lights.indicatorsLeft = !CarController.Lights.indicatorsLeft;
        CarController.Lights.indicatorsRight = false;
        CarController.Lights.indicatorsAll = false;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Left Indicators To " + CarController.Lights.indicatorsLeft);

    }

    private void RCCP_InputManager_OnPressedHighBeamLights() {

        if (!CarController.Lights)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Lights.highBeamHeadlights = !CarController.Lights.highBeamHeadlights;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched High Beam Lights To " + CarController.Lights.highBeamHeadlights);

    }

    private void RCCP_InputManager_OnPressedLowBeamLights() {

        if (!CarController.Lights)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Lights.lowBeamHeadlights = !CarController.Lights.lowBeamHeadlights;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Low Beam Lights To " + CarController.Lights.lowBeamHeadlights);

    }

    private void RCCP_InputManager_OnSteeringHelper() {

        if (!CarController.Stability)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Stability.steeringHelper = !CarController.Stability.steeringHelper;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Steering Helper To " + CarController.Stability.steeringHelper);

    }

    private void RCCP_InputManager_OnTractionHelper() {

        if (!CarController.Stability)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Stability.tractionHelper = !CarController.Stability.tractionHelper;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Traction Helper To " + CarController.Stability.tractionHelper);

    }

    private void RCCP_InputManager_OnAngularDragHelper() {

        if (!CarController.Stability)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Stability.angularDragHelper = !CarController.Stability.angularDragHelper;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Angular Drag Helper To " + CarController.Stability.angularDragHelper);

    }

    private void RCCP_InputManager_OnABS() {

        if (!CarController.Stability)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Stability.ABS = !CarController.Stability.ABS;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched ABS To " + CarController.Stability.ABS);

    }

    private void RCCP_InputManager_OnESP() {

        if (!CarController.Stability)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Stability.ESP = !CarController.Stability.ESP;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched ESP To " + CarController.Stability.ESP);

    }

    private void RCCP_InputManager_OnTCS() {

        if (!CarController.Stability)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Stability.TCS = !CarController.Stability.TCS;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched TCS To " + CarController.Stability.TCS);

    }

    private void RCCP_InputManager_OnStopEngine() {

        if (!CarController.Engine)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Engine.StopEngine();

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Stopped Engine");

    }

    private void RCCP_InputManager_OnStartEngine() {

        if (!CarController.Engine)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer(!CarController.Engine.engineRunning ? "Starting Engine" : "Killing Engine");

        if (!CarController.Engine.engineRunning)
            CarController.Engine.StartEngine();
        else
            CarController.Engine.StopEngine();

    }

    private void RCCP_InputManager_OnGearShiftedDown() {

        if (!CarController.Gearbox)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        if (!CarController.Gearbox.shiftingNow)
            CarController.Gearbox.ShiftDown();

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Shifted Down");

    }

    private void RCCP_InputManager_OnGearShiftedTo(int gear) {

        if (!CarController.Gearbox)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        if (!CarController.Gearbox.shiftingNow)
            CarController.Gearbox.ShiftToGear(gear);

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Shifted To: " + gear.ToString());

    }

    private void RCCP_InputManager_OnGearShiftedUp() {

        if (!CarController.Gearbox)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        if (!CarController.Gearbox.shiftingNow)
            CarController.Gearbox.ShiftUp();

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Shifted Up");

    }

    private void RCCP_InputManager_OnGearShiftedToN() {

        if (!CarController.Gearbox)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        if (!CarController.Gearbox.shiftingNow)
            CarController.Gearbox.ShiftToN();

        if (RCCPSettings.useInputDebugger && CarController.Gearbox.currentGearState.gearState == RCCP_Gearbox.CurrentGearState.GearState.Neutral)
            RCCP_Events.Event_OnRCCPUIInformer("Shifted To N");
        else
            RCCP_Events.Event_OnRCCPUIInformer("Shifted From N");

    }

    private void RCCP_InputManager_OnGearToggle(RCCP_Gearbox.TransmissionType transmissionType) {

        if (!CarController.Gearbox)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Gearbox.transmissionType = transmissionType;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Automatic Gearbox = " + CarController.Gearbox.transmissionType.ToString());

    }

    private void RCCP_InputManager_OnAutomaticGearChanged(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear) {

        if (!CarController.Gearbox)
            return;

        if (!CarController.IsControllableByPlayer())
            return;

        CarController.Gearbox.transmissionType = RCCP_Gearbox.TransmissionType.Automatic_DNRP;
        CarController.Gearbox.automaticGearSelector = semiAutomaticDNRPGear;

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Automatic Gearbox = " + CarController.Gearbox.automaticGearSelector.ToString());

    }

    private void RCCP_InputManager_OnTrailerDetach() {

        if (!CarController.IsControllableByPlayer())
            return;

        if (!CarController.OtherAddonsManager)
            return;

        if (!CarController.OtherAddonsManager.TrailAttacher)
            return;

        if (!CarController.OtherAddonsManager.TrailAttacher.attachedTrailer)
            return;

        CarController.OtherAddonsManager.TrailAttacher.attachedTrailer.DetachTrailer();

        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Trailer Detached");

    }

    private void RCC_InputManager_OnRecord() {

        if (!CarController.IsControllableByPlayer())
            return;

        if (!CarController.OtherAddonsManager)
            return;

        if (!CarController.OtherAddonsManager.Recorder)
            return;

        CarController.OtherAddonsManager.Recorder.Record();

        if (RCCPSettings.useInputDebugger) {

            if (CarController.OtherAddonsManager.Recorder.mode == RCCP_Recorder.RecorderMode.Record)
                RCCP_Events.Event_OnRCCPUIInformer("Recording Started");
            else
                RCCP_Events.Event_OnRCCPUIInformer("Recording Stopped");

        }

    }

    private void RCC_InputManager_OnReplay() {

        if (!CarController.IsControllableByPlayer())
            return;

        if (!CarController.OtherAddonsManager)
            return;

        if (!CarController.OtherAddonsManager.Recorder)
            return;

        CarController.OtherAddonsManager.Recorder.Play();

        if (RCCPSettings.useInputDebugger) {

            if (CarController.OtherAddonsManager.Recorder.mode == RCCP_Recorder.RecorderMode.Play)
                RCCP_Events.Event_OnRCCPUIInformer("Replaying Started");
            else
                RCCP_Events.Event_OnRCCPUIInformer("Replaying Stopped");

        }

    }

    #endregion

    public override void OnDisable() {

        base.OnDisable();

        RCCP_InputManager.OnStartEngine -= RCCP_InputManager_OnStartEngine;
        RCCP_InputManager.OnStopEngine -= RCCP_InputManager_OnStopEngine;
        RCCP_InputManager.OnSteeringHelper -= RCCP_InputManager_OnSteeringHelper;
        RCCP_InputManager.OnTractionHelper -= RCCP_InputManager_OnTractionHelper;
        RCCP_InputManager.OnAngularDragHelper -= RCCP_InputManager_OnAngularDragHelper;
        RCCP_InputManager.OnABS -= RCCP_InputManager_OnABS;
        RCCP_InputManager.OnESP -= RCCP_InputManager_OnESP;
        RCCP_InputManager.OnTCS -= RCCP_InputManager_OnTCS;
        RCCP_InputManager.OnGearShiftedUp -= RCCP_InputManager_OnGearShiftedUp;
        RCCP_InputManager.OnGearShiftedDown -= RCCP_InputManager_OnGearShiftedDown;
        RCCP_InputManager.OnGearShiftedTo -= RCCP_InputManager_OnGearShiftedTo;
        RCCP_InputManager.OnGearShiftedToN -= RCCP_InputManager_OnGearShiftedToN;
        RCCP_InputManager.OnGearToggle -= RCCP_InputManager_OnGearToggle;
        RCCP_InputManager.OnAutomaticGear -= RCCP_InputManager_OnAutomaticGearChanged;
        RCCP_InputManager.OnPressedLowBeamLights -= RCCP_InputManager_OnPressedLowBeamLights;
        RCCP_InputManager.OnPressedHighBeamLights -= RCCP_InputManager_OnPressedHighBeamLights;
        RCCP_InputManager.OnPressedLeftIndicatorLights -= RCCP_InputManager_OnPressedLeftIndicatorLights;
        RCCP_InputManager.OnPressedRightIndicatorLights -= RCCP_InputManager_OnPressedRightIndicatorLights;
        RCCP_InputManager.OnPressedIndicatorLights -= RCCP_InputManager_OnPressedIndicatorLights;
        RCCP_InputManager.OnTrailerDetach -= RCCP_InputManager_OnTrailerDetach;
        RCCP_InputManager.OnRecord -= RCC_InputManager_OnRecord;
        RCCP_InputManager.OnReplay -= RCC_InputManager_OnReplay;

    }

    private void Reset() {

        Keyframe[] ks = new Keyframe[3];

        ks[0] = new Keyframe(0f, 1f);
        ks[0].outTangent = -.0135f;

        ks[1] = new Keyframe(100f, .2f);
        ks[1].inTangent = -.0015f;
        ks[1].outTangent = -.001f;

        ks[2] = new Keyframe(200f, .15f);

        steeringCurve = new AnimationCurve(ks);

    }

}
