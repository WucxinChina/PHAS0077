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
using System.Linq;

/// <summary>
/// Main power generator of the vehicle. Produces and transmits the generated power to the clutch.
/// Enhanced version with improved calculations for more realistic engine behavior.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Engine")]
public class RCCP_Engine : RCCP_Component {

    /// <summary>
    /// If true, overrides the engine RPM with an externally provided value. All internal calculations will be skipped.
    /// </summary>
    public bool overrideEngineRPM = false;

    /// <summary>
    /// Indicates whether the engine is currently running.
    /// </summary>
    public bool engineRunning = true;

    /// <summary>
    /// Indicates whether the engine is in the process of starting (a brief delay).
    /// </summary>
    public bool engineStarting = false;

    /// <summary>
    /// Current engine RPM (revolutions per minute).
    /// </summary>
    [Min(0f)] public float engineRPM = 0f;

    /// <summary>
    /// Minimum engine RPM (typical idle speed).
    /// </summary>
    [Min(0f)] public float minEngineRPM = 750f;

    /// <summary>
    /// Maximum engine RPM (redline).
    /// </summary>
    [Min(0f)] public float maxEngineRPM = 7000f;

    /// <summary>
    /// Rate at which the engine freely accelerates when the drivetrain is disengaged (e.g., clutch in, neutral).
    /// </summary>
    public float engineAccelerationRate = .75f;

    /// <summary>
    /// If true, enables dynamic adjustment of acceleration rate based on engine load.
    /// </summary>
    public bool enableDynamicAcceleration = false;

    /// <summary>
    /// Base acceleration rate stored for dynamic calculations when enableDynamicAcceleration is true.
    /// </summary>
    private float baseEngineAccelerationRate = .75f;

    /// <summary>
    /// How strongly the engine couples to the wheels when the clutch is engaged.
    /// </summary>
    public float engineCouplingToWheelsRate = 1.5f;

    /// <summary>
    /// Rate at which the engine RPM drops due to friction or no throttle input.
    /// </summary>
    public float engineDecelerationRate = .35f;

    /// <summary>
    /// Raw target RPM used internally for smoothing.
    /// </summary>
    [Min(0f)] internal float wantedEngineRPMRaw = 0f;

    /// <summary>
    /// Internal velocity for SmoothDamp usage on engine RPM.
    /// </summary>
    private float engineVelocity;

    /// <summary>
    /// Torque curve (normalized 0-1) for the engine. X axis is RPM, Y axis is normalized torque (0-1).
    /// </summary>
    public AnimationCurve NMCurve = new AnimationCurve(new Keyframe(750f, .6f), new Keyframe(4000f, 1f), new Keyframe(7000f, .85f));

    /// <summary>
    /// If true, automatically generates the NMCurve based on minEngineRPM, maxTorqueAtRPM, and maxEngineRPM when the script is reset.
    /// </summary>
    public bool autoCreateNMCurve = true;

    /// <summary>
    /// Desired RPM at which the engine produces peak torque (used if autoCreateNMCurve is true).
    /// </summary>
    [Min(0f)] public float maximumTorqueAsNM = 200f;

    [Min(0f)] public float peakRPM = 4000f;

    /// <summary>
    /// If true, cuts fuel input once RPM approaches maxEngineRPM to act as a rev limiter.
    /// </summary>
    public bool engineRevLimiter = true;

    /// <summary>
    /// Becomes true when the rev limiter is actively cutting fuel.
    /// </summary>
    public bool cutFuel = false;

    /// <summary>
    /// Frequency of rev limiter cuts per second when active.
    /// </summary>
    [Range(5f, 30f)] public float revLimiterCutFrequency = 15f;

    /// <summary>
    /// Internal timer for rev limiter cut cycles.
    /// </summary>
    private float revLimiterTimer = 0f;

    /// <summary>
    /// Enables forced induction simulation (turbo). If true, turboChargePsi is calculated each frame.
    /// </summary>
    public bool turboCharged = false;

    /// <summary>
    /// Current turbo pressure in PSI.
    /// </summary>
    [Min(0f)] public float turboChargePsi = 0f;

    /// <summary>
    /// Last frame's PSI, used for blow-off detection.
    /// </summary>
    [Min(0f)] internal float turboChargePsi_Old = 0f;

    /// <summary>
    /// Max turbo boost (PSI) that can be reached at full throttle and high RPM.
    /// </summary>
    [Min(0f)] public float maxTurboChargePsi = 12f;

    /// <summary>
    /// Maximum torque multiplier from the turbo at max boost.
    /// </summary>
    [Min(0f)] public float turboChargerCoEfficient = 1.25f;

    /// <summary>
    /// True if the turbo is venting/blowing off due to sudden throttle closure.
    /// </summary>
    [HideInInspector] public bool turboBlowOut = false;

    /// <summary>
    /// Additional multiplier applied to the engine torque (e.g., from nitrous).
    /// </summary>
    private float multiplier = 1f;

    /// <summary>
    /// Engine friction factor. Higher values cause RPM to drop faster when throttle is released.
    /// </summary>
    [Range(0f, 1f)] public float engineFriction = .2f;

    /// <summary>
    /// Engine inertia factor. Lower values let the engine rev up/down more quickly.
    /// </summary>
    [Range(.01f, .5f)] public float engineInertia = .2f;

    /// <summary>
    /// Dynamically calculated engine inertia based on RPM and engine load.
    /// </summary>
    private float sensitiveEngineInertia = 1f;

    /// <summary>
    /// Current engine load factor (0-1). Calculated based on throttle input and resistance.
    /// </summary>
    [Range(0f, 1f)] public float engineLoad = 0f;

    /// <summary>
    /// Current engine torque output (Newton-meters).
    /// </summary>
    [Min(0f)] public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Current fuel input to the engine (0-1). Combines throttle input with idle adjustment.
    /// </summary>
    [Range(0f, 1f)] public float fuelInput = 0f;

    /// <summary>
    /// Idle compensation input to prevent engine stalling at low RPM.
    /// </summary>
    [Range(0f, 1f)] public float idleInput = 0f;

    /// <summary>
    /// If true, simulates realistic engine temperature effects on performance.
    /// </summary>
    public bool simulateEngineTemperature = false;

    /// <summary>
    /// Current engine operating temperature in Celsius.
    /// </summary>
    [Range(20f, 150f)] public float engineTemperature = 85f;

    /// <summary>
    /// Optimal engine operating temperature for peak performance.
    /// </summary>
    [Range(70f, 100f)] public float optimalTemperature = 85f;

    /// <summary>
    /// Ambient temperature affecting engine cooling rate.
    /// </summary>
    [Range(-20f, 50f)] public float ambientTemperature = 20f;

    /// <summary>
    /// If true, enables Variable Valve Timing simulation for enhanced torque at specific RPM ranges.
    /// </summary>
    public bool enableVVT = false;

    /// <summary>
    /// RPM range where VVT provides optimal performance boost.
    /// </summary>
    public Vector2 vvtOptimalRange = new Vector2(3000f, 6000f);

    /// <summary>
    /// Torque multiplier applied when engine is in VVT optimal range.
    /// </summary>
    [Range(1f, 1.3f)] public float vvtTorqueMultiplier = 1.1f;

    /// <summary>
    /// If true, enables knock detection and protection.
    /// </summary>
    public bool enableKnockDetection = false;

    /// <summary>
    /// Current knock factor (0-1). Higher values reduce engine performance.
    /// </summary>
    [Range(0f, 1f)] public float knockFactor = 0f;

    public float maximumSpeed = 240f;
    public float maximumSpeed_Old = 240f;

    /// <summary>
    /// Events for torque output, using a custom class.
    /// </summary>
    public RCCP_Event_Output outputEvent = new RCCP_Event_Output();
    public RCCP_Output output = new RCCP_Output();

    public override void Awake() {

        base.Awake();

        maximumSpeed_Old = maximumSpeed;
        UpdateMaximumSpeed();

    }

    public override void Start() {

        base.Start();

        // Store base acceleration rate for dynamic calculations only if enabled
        if (enableDynamicAcceleration)
            baseEngineAccelerationRate = engineAccelerationRate;

        if (autoCreateNMCurve)
            CheckAndCreateNMCurve();

    }

    private void Update() {

        Inputs();

        if (maximumSpeed != maximumSpeed_Old)
            UpdateMaximumSpeed();

        maximumSpeed_Old = maximumSpeed;

    }

    private void FixedUpdate() {

        RPM();
        TurboCharger();
        EngineTemperature();
        GenerateKW();
        NegativeFeedback();
        Output();

    }

    /// <summary>
    /// Starts the engine if it's currently off. Plays a delay, then sets engineRunning to true.
    /// </summary>
    public void StartEngine() {

        if (engineRunning || engineStarting)
            return;

        StartCoroutine(StartEngineDelayed());

    }

    /// <summary>
    /// Immediately stops the engine, setting engineRunning to false.
    /// </summary>
    public void StopEngine() {

        engineRunning = false;

    }

    /// <summary>
    /// Coroutine for engine start delay, simulating a brief ignition sequence.
    /// </summary>
    private IEnumerator StartEngineDelayed() {

        engineRunning = false;
        engineStarting = true;
        yield return new WaitForSeconds(1);
        engineStarting = false;
        engineRunning = true;

    }

    /// <summary>
    /// Calculates idleInput, fuelInput, and applies rev-limiter logic.
    /// </summary>
    private void Inputs() {

        if (overrideEngineRPM)
            return;

        // Raise idleInput if RPM is below (minEngineRPM + ~30% buffer).
        if (engineRPM <= minEngineRPM + (minEngineRPM / 10f))
            idleInput = 1f - Mathf.InverseLerp(minEngineRPM - (minEngineRPM / 10f), minEngineRPM + (minEngineRPM / 10f), wantedEngineRPMRaw);
        else
            idleInput = 0f;

        // Combine throttle with idle compensation.
        fuelInput = CarController.throttleInput_P + idleInput;
        fuelInput = Mathf.Clamp01(fuelInput);

        // Enhanced rev limiter with realistic hard cuts
        RevLimiter();

        // If the engine is turned off, no fuel and no idle input.
        if (!engineRunning) {

            fuelInput = 0f;
            idleInput = 0f;

        }

    }

    /// <summary>
    /// Enhanced rev limiter with realistic hard cut behavior for more noticeable effect.
    /// </summary>
    private void RevLimiter() {

        if (!engineRevLimiter) {

            cutFuel = false;
            revLimiterTimer = 0f;
            return;

        }

        float cutThreshold = maxEngineRPM - 100f;

        if (engineRPM >= cutThreshold) {

            // Update rev limiter timer
            revLimiterTimer += Time.deltaTime;

            // Calculate cut cycle based on frequency
            float cutCycleDuration = 1f / revLimiterCutFrequency;
            float cutOnDuration = cutCycleDuration * 0.3f; // 30% of cycle is cut
            float cutOffDuration = cutCycleDuration * 0.7f; // 70% of cycle is normal

            // Determine if we should cut fuel based on timer position in cycle
            float cyclePosition = revLimiterTimer % cutCycleDuration;

            if (cyclePosition < cutOnDuration) {

                if (fuelInput >= .5f)
                    wantedEngineRPMRaw -= 20000f * Time.deltaTime;

                cutFuel = true;
                // Complete fuel cut for more noticeable effect
                fuelInput = 0f;

            } else {

                cutFuel = false;

            }

        } else {

            cutFuel = false;
            revLimiterTimer = 0f;

        }

    }

    /// <summary>
    /// Calculates dynamic engine inertia based on RPM and load for more realistic behavior.
    /// </summary>
    private void CalculateDynamicInertia() {

        // Lower inertia at higher RPMs for more responsive behavior
        float rpmFactor = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM);
        float loadFactor = Mathf.Clamp01(engineLoad);

        // Combine RPM and load factors
        float dynamicFactor = (rpmFactor * 0.7f) + (loadFactor * 0.3f);
        sensitiveEngineInertia = Mathf.Lerp(engineInertia, engineInertia * 0.75f, dynamicFactor);

    }

    /// <summary>
    /// Calculates realistic engine friction that varies with RPM and temperature.
    /// </summary>
    private float CalculateEngineFriction() {

        float baseFriction = engineFriction;

        // Higher friction at higher RPMs
        float rpmFriction = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM) * 0.3f;

        // Temperature affects friction
        float tempFactor = 1f;
        if (simulateEngineTemperature) {

            if (engineTemperature < optimalTemperature) {

                // Cold engine has more friction
                tempFactor = Mathf.Lerp(1.5f, 1f, Mathf.InverseLerp(ambientTemperature, optimalTemperature, engineTemperature));

            } else if (engineTemperature > optimalTemperature + 20f) {

                // Overheated engine has more friction
                tempFactor = Mathf.Lerp(1f, 1.3f, Mathf.InverseLerp(optimalTemperature + 20f, 150f, engineTemperature));

            }

        }

        return (baseFriction + rpmFriction) * tempFactor;

    }

    /// <summary>
    /// Calculates current engine load based on throttle input and resistance from drivetrain.
    /// </summary>
    private void CalculateEngineLoad() {

        // Base load from throttle input
        float throttleLoad = CarController ? CarController.throttleInput_P : 0f;

        // Additional load from drivetrain resistance
        float drivetrainLoad = 0f;

        if (CarController.Clutch) {

            float clutchInput = CarController.Clutch.clutchInput;

            // More load when clutch is engaged and resisting
            if (clutchInput < 0.5f) {

                drivetrainLoad = (1f - clutchInput) * 0.3f;

            }

        }

        engineLoad = Mathf.Clamp01(throttleLoad + drivetrainLoad);

        // Adjust acceleration rate based on load if dynamic acceleration is enabled
        if (enableDynamicAcceleration) {

            float loadMultiplier = Mathf.Lerp(1.2f, 0.7f, engineLoad);
            engineAccelerationRate = baseEngineAccelerationRate * loadMultiplier;

        }

    }

    /// <summary>
    /// Simulates realistic engine temperature changes based on load and ambient conditions.
    /// </summary>
    private void EngineTemperature() {

        if (!simulateEngineTemperature)
            return;

        // Target temperature based on engine load and ambient temperature
        float baseTargetTemp = ambientTemperature + 65f; // Base operating temp
        float loadTempIncrease = engineLoad * 30f; // Load increases temperature
        float targetTemp = baseTargetTemp + loadTempIncrease;

        // Temperature change rate varies based on conditions
        float tempChangeRate = engineRunning ? 2f : 5f; // Cool down faster when engine is off

        if (engineRunning) {

            engineTemperature = Mathf.MoveTowards(engineTemperature, targetTemp, Time.fixedDeltaTime * tempChangeRate);

        } else {

            engineTemperature = Mathf.MoveTowards(engineTemperature, ambientTemperature, Time.fixedDeltaTime * tempChangeRate);

        }

        engineTemperature = Mathf.Clamp(engineTemperature, ambientTemperature, 150f);

    }

    /// <summary>
    /// Calculates knock factor based on engine load and RPM conditions.
    /// </summary>
    private void CalculateKnockDetection() {

        if (!enableKnockDetection) {

            knockFactor = 0f;
            return;

        }

        // High load at low RPM increases knock risk
        float rpmFactor = Mathf.InverseLerp(maxEngineRPM * 0.3f, maxEngineRPM * 0.6f, engineRPM);
        rpmFactor = 1f - rpmFactor; // Invert so low RPM = high risk

        float loadFactor = engineLoad;

        // Combine factors
        knockFactor = Mathf.Clamp01(rpmFactor * loadFactor * 0.8f);

        // Temperature also affects knock
        if (simulateEngineTemperature && engineTemperature > optimalTemperature + 10f) {

            float tempKnockFactor = (engineTemperature - optimalTemperature - 10f) / 40f;
            knockFactor = Mathf.Clamp01(knockFactor + tempKnockFactor);

        }

    }

    /// <summary>
    /// Handles all RPM-related calculations including dynamic inertia and load.
    /// </summary>
    private void RPM() {

        if (overrideEngineRPM)
            return;

        // Calculate dynamic factors
        CalculateDynamicInertia();
        CalculateEngineLoad();
        CalculateKnockDetection();

        // Get dynamic friction
        float dynamicFriction = CalculateEngineFriction();

        // Check if clutch is disengaged for proper free-revving calculations
        float clutchInput = 0f;
        bool clutchDisengaged = false;

        if (CarController.Clutch) {

            clutchInput = CarController.Clutch.clutchInput;
            clutchDisengaged = clutchInput >= 0.9f; // Consider clutch disengaged when 90% or more pressed

        }

        if (!engineRunning) {

            wantedEngineRPMRaw -= 5000f * Time.fixedDeltaTime;

        } else {

            // When clutch is disengaged, engine behaves as free-revving
            if (clutchDisengaged) {

                // Free-revving calculations with enhanced acceleration and deceleration
                float freeRevAcceleration = engineAccelerationRate * 8.5f; // Faster acceleration when free-revving
                float freeRevDeceleration = engineDecelerationRate * 7.2f; // Slightly faster deceleration when free-revving

                wantedEngineRPMRaw += fuelInput * freeRevAcceleration * 1000f * Time.fixedDeltaTime;
                wantedEngineRPMRaw -= dynamicFriction * freeRevDeceleration * 1000f * Time.fixedDeltaTime;

                // Additional engine braking when no throttle input
                if (fuelInput <= 0.1f) {

                    float engineBraking = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM) * 200f;
                    wantedEngineRPMRaw -= engineBraking * Time.fixedDeltaTime;

                }

            } else {

                // Normal calculations when clutch is engaged
                wantedEngineRPMRaw += fuelInput * engineAccelerationRate * 1000f * Time.fixedDeltaTime;
                wantedEngineRPMRaw -= dynamicFriction * engineDecelerationRate * 1000f * Time.fixedDeltaTime;

                // Factor in wheel coupling when clutch is engaged
                CheckEngineRPMForWheelFeedback();

            }

        }

        // Handle negative feedback if needed.
        CheckEngineRPMForNegativeFeedback();

        // Clamp final raw target RPM.
        wantedEngineRPMRaw = Mathf.Clamp(wantedEngineRPMRaw, 0f, maxEngineRPM + 100f);

        // SmoothDamp final engine RPM for stability.
        engineRPM = Mathf.SmoothDamp(engineRPM, wantedEngineRPMRaw, ref engineVelocity, sensitiveEngineInertia * .35f);

    }

    /// <summary>
    /// Minimum turbo PSI required to trigger a blow-off when throttle is released.
    /// </summary>
    private float turboBlowOffMinPsi = 5f;

    /// <summary>
    /// Threshold under which we consider the throttle "released."
    /// </summary>
    private float throttleLiftThreshold = 0.1f;

    /// <summary>
    /// Tracks throttle input from the previous FixedUpdate.
    /// </summary>
    private float lastFuelInput;

    /// <summary>
    /// Enhanced turbocharging with improved lag and spool-up behavior.
    /// </summary>
    private void TurboCharger() {

        // If engine or turbo is off, gradually reduce PSI.
        if (!engineRunning || !turboCharged) {

            turboChargePsi = Mathf.MoveTowards(turboChargePsi, 0f, Time.fixedDeltaTime * 10f);
            turboBlowOut = false;
            lastFuelInput = fuelInput;
            return;

        }

        // Calculate spool-up curve
        float rpmFactor = Mathf.InverseLerp(minEngineRPM * 1.5f, maxEngineRPM * 0.8f, engineRPM);
        rpmFactor = Mathf.Pow(rpmFactor, 1.5f);

        float targetPsi = maxTurboChargePsi * fuelInput * rpmFactor;
        float spoolRate = Mathf.Lerp(10f, 20f, fuelInput);
        turboChargePsi = Mathf.MoveTowards(turboChargePsi, targetPsi, Time.fixedDeltaTime * spoolRate);

        // Blow-off event detection: when throttle goes from "pressed" to "released"
        bool justLifted = lastFuelInput >= throttleLiftThreshold && fuelInput < throttleLiftThreshold;
        if (justLifted && turboChargePsi > turboBlowOffMinPsi) {

            turboBlowOut = true;

        } else {

            turboBlowOut = false;

        }

        // Store state for next update
        lastFuelInput = fuelInput;

    }

    /// <summary>
    /// Overrides the engine RPM to a specified value, bypassing internal calculations.
    /// </summary>
    /// <param name="targetRPM">Engine RPM to set.</param>
    public void OverrideRPM(float targetRPM) {

        overrideEngineRPM = true;
        engineRPM = targetRPM;
        wantedEngineRPMRaw = targetRPM;

    }

    public void DisableOverride() {

        overrideEngineRPM = false;

    }

    /// <summary>
    /// Enhanced torque generation with VVT, temperature, and knock compensation.
    /// </summary>
    private void GenerateKW() {

        if (!engineRunning) {

            producedTorqueAsNM = 0f;
            return;

        }

        // Base torque from curve
        float torqueCurveMultiplier = NMCurve.Evaluate(engineRPM);
        float baseTorque = maximumTorqueAsNM * torqueCurveMultiplier * fuelInput;

        // Temperature compensation
        float temperatureMultiplier = 1f;
        if (simulateEngineTemperature) {

            if (engineTemperature < optimalTemperature) {

                // Cold engine produces less power
                temperatureMultiplier = Mathf.Lerp(0.85f, 1f, Mathf.InverseLerp(ambientTemperature, optimalTemperature, engineTemperature));

            } else if (engineTemperature > optimalTemperature + 15f) {

                // Overheated engine loses power
                temperatureMultiplier = Mathf.Lerp(1f, 0.7f, Mathf.InverseLerp(optimalTemperature + 15f, 150f, engineTemperature));

            }

        }

        // VVT bonus
        float vvtMultiplier = 1f;
        if (enableVVT && engineRPM >= vvtOptimalRange.x && engineRPM <= vvtOptimalRange.y) {

            vvtMultiplier = vvtTorqueMultiplier;

        }

        // Knock penalty
        float knockMultiplier = 1f - (knockFactor * 0.3f);

        // Turbo boost
        float turboMultiplier = 1f;
        if (turboCharged && turboChargePsi > 0f) {

            float boostRatio = turboChargePsi / maxTurboChargePsi;
            turboMultiplier = 1f + (boostRatio * (turboChargerCoEfficient - 1f));

        }

        // Apply all multipliers
        producedTorqueAsNM = baseTorque * temperatureMultiplier * vvtMultiplier * knockMultiplier * turboMultiplier * multiplier;

        // Clamp torque so we never exceed our realistic limit
        producedTorqueAsNM = Mathf.Clamp(producedTorqueAsNM, -maximumTorqueAsNM * 1.8f, maximumTorqueAsNM * 1.8f);

        // Reset frame multiplier
        multiplier = 1f;

    }

    /// <summary>
    /// Applies negative torque feedback based on wheel rotation relative to gear direction.
    /// Fixed to properly handle reverse gear scenarios.
    /// </summary>
    private void CheckEngineRPMForNegativeFeedback() {

        if (CarController.PoweredAxles == null ||
            CarController.Gearbox == null ||
            CarController.Differentials == null)
            return;

        // Get current gear ratio (can be negative for reverse)
        float currentGearRatio = CarController.Gearbox.gearRatios[CarController.Gearbox.currentGear];

        // Skip if in neutral
        if (Mathf.Approximately(currentGearRatio, 0f))
            return;

        float finalDrive = CarController.GetFinalDriveRatio();

        // Compute average wheel RPM (with sign preserved for direction)
        float wheelRPMSum = 0f;
        int wheelCount = 0;

        foreach (var axle in CarController.PoweredAxles) {

            if (axle.leftWheelCollider?.WheelCollider != null) {

                wheelRPMSum += axle.leftWheelCollider.WheelCollider.rpm;
                wheelCount++;

            }

            if (axle.rightWheelCollider?.WheelCollider != null) {

                wheelRPMSum += axle.rightWheelCollider.WheelCollider.rpm;
                wheelCount++;

            }

        }

        if (wheelCount == 0)
            return;

        float avgWheelRPM = wheelRPMSum / wheelCount;

        // Calculate expected wheel rotation direction based on gear
        bool isReverseGear = currentGearRatio < 0f;
        float expectedWheelDirection = isReverseGear ? -1f : 1f;

        // Check if wheels are spinning in the wrong direction relative to gear
        bool wheelsSpinningWrongDirection = (avgWheelRPM * expectedWheelDirection) < -50f;

        // Apply negative feedback if wheels spinning wrong direction
        if (wheelsSpinningWrongDirection && producedTorqueAsNM > 0f) {

            float wrongDirectionSpeed = Mathf.Abs(avgWheelRPM);
            float brakeFactor = Mathf.Clamp01(wrongDirectionSpeed / 500f);
            producedTorqueAsNM *= (1f - brakeFactor);

        }

        // Calculate equivalent engine RPM from wheel speed
        float equivalentEngineRPM = Mathf.Abs(avgWheelRPM * currentGearRatio * finalDrive);

        // Apply over-rev protection
        if (engineRPM > equivalentEngineRPM * 1.2f && equivalentEngineRPM > 100f) {

            float rpmDifference = engineRPM - equivalentEngineRPM;
            float overRevFactor = Mathf.Clamp01(rpmDifference / 2000f);
            producedTorqueAsNM *= (1f - overRevFactor * 0.5f);

        }

    }

    /// <summary>
    /// Handles the coupling between engine RPM and wheel RPM for realistic behavior.
    /// Fixed to properly handle reverse gear calculations.
    /// </summary>
    private void CheckEngineRPMForWheelFeedback() {

        float clutchInput = 0f;

        if (CarController.Clutch)
            clutchInput = CarController.Clutch.clutchInput;

        // If clutch is fully pressed, no wheel coupling.
        if (clutchInput >= .95f)
            return;

        float wheelRPM = 0f;
        int wheelCount = 0;

        if (CarController.PoweredAxles != null && CarController.PoweredAxles.Count >= 1) {

            for (int i = 0; i < CarController.PoweredAxles.Count; i++) {

                RCCP_Axle axle = CarController.PoweredAxles[i];

                if (axle.leftWheelCollider && axle.leftWheelCollider.WheelCollider) {

                    wheelRPM += axle.leftWheelCollider.WheelCollider.rpm;
                    wheelCount++;

                }

                if (axle.rightWheelCollider && axle.rightWheelCollider.WheelCollider) {

                    wheelRPM += axle.rightWheelCollider.WheelCollider.rpm;
                    wheelCount++;

                }

            }

            if (wheelCount > 0)
                wheelRPM /= wheelCount;

        }

        float currentGearRatio = 1f;
        float finalDriveRatio = 1f;

        if (CarController.Gearbox && CarController.Gearbox.gearRatios != null && CarController.Gearbox.gearRatios.Length > 0)
            currentGearRatio = CarController.Gearbox.gearRatios[CarController.Gearbox.currentGear];

        if (CarController.Differentials != null && CarController.Differentials.Length > 0)
            finalDriveRatio = CarController.GetFinalDriveRatio();

        // Calculate equivalent engine RPM - use absolute value for engine RPM calculation
        float equivalentEngineRPM = Mathf.Abs(wheelRPM * currentGearRatio * finalDriveRatio);

        float totalSlip = CarController.GetAverageWheelSlip(CarController.PoweredAxles) * CarController.throttleInput_V;
        totalSlip = Mathf.Abs(totalSlip);
        totalSlip = 2f * Mathf.Pow(totalSlip, 2f);
        totalSlip = Mathf.Clamp01(totalSlip);

        float slipFactor = Mathf.Lerp(2.7f, 1f, totalSlip);

        if (equivalentEngineRPM > 0f) {

            float couplingStrength = (1f - clutchInput) * engineCouplingToWheelsRate * slipFactor;
            wantedEngineRPMRaw = Mathf.Lerp(wantedEngineRPMRaw, equivalentEngineRPM, couplingStrength * Time.fixedDeltaTime);

        }

    }

    /// <summary>
    /// Sets engine torque multiplier (e.g., for nitrous systems).
    /// </summary>
    /// <param name="targetMultiplier">Torque multiplier to apply this frame.</param>
    public void SetTorqueMultiplier(float targetMultiplier) {

        multiplier = targetMultiplier;

    }

    /// <summary>
    /// Multiplies the current engine torque multiplier by the specified value.
    /// Useful for stacking multiple torque modifications (e.g., NOS + tuning + temperature effects).
    /// </summary>
    /// <param name="targetMultiplier">Multiplier value to apply to the current multiplier.</param>
    public void Multiply(float targetMultiplier) {

        multiplier *= targetMultiplier;

    }

    /// <summary>
    /// Outputs the calculated torque to connected components and invokes events.
    /// </summary>
    private void Output() {

        if (output == null)
            output = new RCCP_Output();

        output.NM = producedTorqueAsNM;
        outputEvent.Invoke(output);

    }

    /// <summary>
    /// Reloads and resets engine parameters when script is enabled or disabled.
    /// </summary>
    public void Reload() {

        engineStarting = false;
        engineRPM = 0f;
        wantedEngineRPMRaw = 0f;

        if (engineRunning) {

            wantedEngineRPMRaw = minEngineRPM;
            engineRPM = wantedEngineRPMRaw;

        }


        engineVelocity = 0f;
        fuelInput = 0f;
        idleInput = 0f;
        producedTorqueAsNM = 0f;
        multiplier = 1f;
        cutFuel = false;
        revLimiterTimer = 0f;

        if (turboCharged) {

            turboChargePsi = 0f;
            turboChargePsi_Old = 0f;
            turboBlowOut = false;

        }

        if (simulateEngineTemperature) {

            engineTemperature = ambientTemperature + 20f;

        }

        engineLoad = 0f;
        knockFactor = 0f;
        sensitiveEngineInertia = engineInertia;

    }

    /// <summary>
    /// Checks if autoCreateNMCurve is enabled and, if so, regenerates the engine torque curve (NMCurve)
    /// using minEngineRPM, peak-torque RPM (maximumTorqueAsNM), and maxEngineRPM.
    /// </summary>
    public void CheckAndCreateNMCurve() {

        // only proceed when auto-creation is requested
        if (!autoCreateNMCurve)
            return;

        // define normalized torque at idle (minEngineRPM) and redline (maxEngineRPM)
        float idleTorqueNormalized = 0.7f;
        float redlineTorqueNormalized = 0.85f;

        // build a new curve with three key points: idle, peak, and redline
        NMCurve = new AnimationCurve(
            new Keyframe(minEngineRPM, idleTorqueNormalized),
            new Keyframe(peakRPM, 1f),
            new Keyframe(maxEngineRPM, redlineTorqueNormalized)
        );

    }

    /// <summary>
    /// Applies negative-feedback braking when a wheel’s RPM overshoots the
    /// target wheel speed for the current gear.
    /// </summary>
    private void NegativeFeedback() {

        // Ensure drivetrain is present
        if (CarController.Gearbox == null ||
             CarController.Differentials == null ||
             CarController.Clutch == null) {
            return;
        }

        // Skip if clutch is nearly fully pressed
        if (CarController.Clutch.clutchInput >= 0.9f) {
            return;
        }

        // Skip if gearbox not engaging drive (neutral or park)
        if (CarController.Gearbox.gearInput <= 0f) {
            return;
        }

        float targetSpeed = CarController.targetWheelSpeedForCurrentGear;

        // Avoid division by zero
        if (Mathf.Approximately(targetSpeed, 0f)) {
            return;
        }

        // Loop each axle and clamp each wheel individually
        foreach (var axle in CarController.PoweredAxles) {

            ApplyNegativeFeedbackToWheel(axle.leftWheelCollider, targetSpeed);
            ApplyNegativeFeedbackToWheel(axle.rightWheelCollider, targetSpeed);

        }

    }

    /// <summary>
    /// Computes slip ratio on a single wheel and applies braking
    /// when slip exceeds threshold.
    /// </summary>
    /// <param name="wheel">The RCCP_WheelCollider to clamp</param>
    /// <param name="targetSpeed">Target wheel speed in RPM</param>
    private void ApplyNegativeFeedbackToWheel(RCCP_WheelCollider wheel, float targetSpeed) {

        if (wheel == null) {
            return;
        }

        float currentSpeed = Mathf.Abs(wheel.wheelRPM2Speed);

        // Only apply if wheel is above target
        if (currentSpeed <= targetSpeed) {
            return;
        }

        // Slip ratio = (actual − target) / target
        float slipRatio = Mathf.Abs(currentSpeed - targetSpeed) / targetSpeed;

        // These could be made public fields for tuning
        float slipThreshold = 0.1f;    // start feedback at 10% overspeed
        float maxSlipRatio = .8f;     // full feedback at 30% overspeed

        if (slipRatio <= slipThreshold) {
            return;
        }

        // Normalize into 0…1
        float normalizedSlip = Mathf.Clamp01((slipRatio - slipThreshold)
                                              / (maxSlipRatio - slipThreshold));

        // Smooth the ramp-up curve
        float feedbackStrength = Mathf.SmoothStep(0f, 1f, normalizedSlip);

        // Apply the negative feedback braking
        if (feedbackStrength >= .05f)
            wheel.AddNegativeFeedback(feedbackStrength);

    }

    /// <summary>
    /// Recomputes and applies per-differential finalDriveRatio
    /// such that the vehicle’s top speed matches maximumSpeed.
    /// </summary>
    public void UpdateMaximumSpeed() {

        // find the car controller
        RCCP_CarController controller = GetComponentInParent<RCCP_CarController>(true);
        if (controller == null)
            return;

        // find the gearbox
        RCCP_Gearbox gearbox = controller.GetComponentInChildren<RCCP_Gearbox>(true);
        if (gearbox == null || gearbox.gearRatios == null || gearbox.gearRatios.Length == 0)
            return;

        // gather all differentials that actually drive wheels
        List<RCCP_Differential> validDiffs = new List<RCCP_Differential>();
        float totalCircumference = 0f;

        foreach (RCCP_Differential diff in controller.GetComponentsInChildren<RCCP_Differential>(true)) {

            if (!diff.gameObject.activeSelf)
                continue;

            RCCP_Axle axle = diff.connectedAxle;
            if (axle == null)
                continue;

            float radius = controller.GetAverageWheelRadius(axle);
            totalCircumference += 2f * Mathf.PI * radius;
            validDiffs.Add(diff);
        }

        if (validDiffs.Count == 0)
            return;

        // average wheel circumference in meters
        float averageCircumference = totalCircumference / validDiffs.Count;

        float lastGearRatio = gearbox.gearRatios[^1];
        if (maximumSpeed > 0f && lastGearRatio > 0f) {
            // engineRPM → wheel rpm (at diff ratio=1) → km/h
            float k = maxEngineRPM / lastGearRatio
                      * averageCircumference
                      * 60f    // minutes per hour
                      / 1000f; // meters → kilometers

            // total diff ratio needed
            float requiredTotalRatio = k / maximumSpeed;

            foreach (RCCP_Differential diff in validDiffs) {

                if (!diff.gameObject.activeSelf)
                    continue;

                diff.finalDriveRatio = requiredTotalRatio;
            }
        }

        // propagate the first diff’s ratio into any speed-upgrader UI
        RCCP_VehicleUpgrade_Speed speedUpgrader = controller.GetComponentInChildren<RCCP_VehicleUpgrade_Speed>(true);

        if (speedUpgrader != null && speedUpgrader.defMaxSpeed <= 0f)
            speedUpgrader.defMaxSpeed = maximumSpeed;

    }

    /// <summary>
    /// Reads back the current finalDriveRatios and computes
    /// what the resulting top speed would be, storing in maximumSpeed.
    /// </summary>
    public void RetrieveMaximumSpeed() {

        // find the car controller
        RCCP_CarController controller = GetComponentInParent<RCCP_CarController>(true);
        if (controller == null)
            return;

        // find the gearbox
        RCCP_Gearbox gearbox = controller.GetComponentInChildren<RCCP_Gearbox>(true);
        if (gearbox == null || gearbox.gearRatios == null || gearbox.gearRatios.Length == 0)
            return;

        // gather all differentials that actually drive wheels
        List<RCCP_Differential> validDiffs = new List<RCCP_Differential>();
        float totalCircumference = 0f;

        foreach (RCCP_Differential diff in controller.GetComponentsInChildren<RCCP_Differential>(true)) {

            if (!diff.gameObject.activeSelf)
                continue;

            RCCP_Axle axle = diff.connectedAxle;
            if (axle == null)
                continue;

            float radius = controller.GetAverageWheelRadius(axle);
            totalCircumference += 2f * Mathf.PI * radius;
            validDiffs.Add(diff);
        }

        if (validDiffs.Count == 0)
            return;

        // average wheel circumference in meters
        float averageCircumference = totalCircumference / validDiffs.Count;

        // compute the *product* of all diff ratios
        float totalFinalDriveRatio = 0f;
        foreach (RCCP_Differential diff in validDiffs) {

            if (!diff.gameObject.activeSelf)
                continue;

            totalFinalDriveRatio += diff.finalDriveRatio;
        }

        totalFinalDriveRatio /= (float)validDiffs.Count;

        // speed formula: (engineRPM / lastGearRatio / totalDiffRatio) * circumference * 60 / 1000
        float lastGearRatio = gearbox.gearRatios[^1];
        maximumSpeed = (maxEngineRPM / lastGearRatio / totalFinalDriveRatio)
                       * averageCircumference
                       * 60f   // minutes per hour
                       / 1000f;

        // store for legacy compatibility if needed
        maximumSpeed_Old = maximumSpeed;
    }


    private void Reset() {

        RetrieveMaximumSpeed();

    }

}