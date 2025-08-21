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
/// Manages a wheel's physics, alignment, slip calculations, friction, and special states (deflation, drift, etc.) 
/// using Unity WheelCollider. This component is designed to work in conjunction with the RCCP vehicle system.
/// </summary>
[RequireComponent(typeof(WheelCollider))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP WheelCollider")]
public class RCCP_WheelCollider : RCCP_Component {

    #region Constants

    // Magic numbers converted to constants for better maintainability
    private const float WHEEL_TEMPERATURE_INCREASE_RATE = 10f;
    private const float WHEEL_TEMPERATURE_DECREASE_RATE = 1.5f;
    private const float MIN_WHEEL_TEMPERATURE = 20f;
    private const float MAX_WHEEL_TEMPERATURE = 125f;
    private const float WHEEL_MASS_DIVIDER = 25f;
    private const float WHEEL_FORCE_APP_POINT_DISTANCE = 0.1f;
    private const float WHEEL_SUSPENSION_DISTANCE = 0.2f;
    private const float WHEEL_SPRING_VALUE = 50000f;
    private const float WHEEL_DAMPER_VALUE = 3500f;
    private const float FORWARD_FRICTION_EXTREMUM_SLIP = 0.4f;
    private const float SIDEWAYS_FRICTION_EXTREMUM_SLIP = 0.35f;
    private const float MIN_TORQUE_THRESHOLD = 5f;
    private const float SPEED_THRESHOLD_CHECK = 0.01f;
    private const float CIRCUMFERENCE_MULTIPLIER = 60f / 1000f;
    private const float RPM_TO_DEGREES_MULTIPLIER = 360f / 60f;
    private const float FRICTION_COMPARISON_EPSILON = 0.0001f;
    private const float HANDBRAKE_DIVISOR = 5f;
    private const float GEAR_SPEED_TOLERANCE = 1.02f;
    private const float AUDIO_SOURCE_CREATION_DELAY = 0.02f;
    private const float SKID_VOLUME_THRESHOLD = 0.02f;
    private const float MIN_AUDIO_VOLUME_TO_STOP = 0.05f;
    private const float BUMP_FORCE_THRESHOLD = 5000f;

    #endregion

    #region Core Fields

    /// <summary>
    /// Backing field for WheelCollider component reference.
    /// </summary>
    private WheelCollider _wheelCollider;

    /// <summary>
    /// Actual wheelcollider component. Lazy-loaded on first access.
    /// </summary>
    public WheelCollider WheelCollider {

        get {

            if (_wheelCollider == null)
                _wheelCollider = GetComponent<WheelCollider>();

            return _wheelCollider;

        }

    }

    /// <summary>
    /// This wheel is connected to this axle. Defines axle grouping (front/rear or other).
    /// </summary>
    public RCCP_Axle connectedAxle;

    /// <summary>
    /// Information about what this wheel is currently hitting (if anything).
    /// </summary>
    public WheelHit wheelHit;

    /// <summary>
    /// If true, wheel models are aligned to WheelCollider orientation and position each frame.
    /// </summary>
    public bool alignWheels = true;

    #endregion

    #region Status Fields

    /// <summary>
    /// Indicates whether this wheel is in contact with a surface.
    /// </summary>
    [Space()]
    public bool isGrounded = false;

    /// <summary>
    /// Indicates whether this wheel is currently slipping above a threshold (skidding).
    /// </summary>
    public bool isSkidding = false;

    /// <summary>
    /// Index of the ground material this wheel is on, used for slip thresholds, audio, etc.
    /// </summary>
    [Min(0)]
    public int groundIndex = 0;

    #endregion

    #region Input Fields

    /// <summary>
    /// Motor torque applied to this wheel in Nm.
    /// </summary>
    [Space()]
    public float motorTorque = 0f;

    /// <summary>
    /// Brake torque applied to this wheel in Nm.
    /// </summary>
    public float brakeTorque = 0f;

    /// <summary>
    /// Steer input angle in degrees (before Ackermann corrections, if any).
    /// </summary>
    public float steerInput = 0f;

    /// <summary>
    /// Handbrake input in range [0..1].
    /// </summary>
    [Min(0f)]
    public float handbrakeInput = 0f;

    public float negativeFeedbackIntensity = 0f;

    #endregion

    #region Visual Fields

    /// <summary>
    /// Transform reference for the visual representation (wheel model).
    /// </summary>
    [Space()]
    public Transform wheelModel;

    /// <summary>
    /// Approximate speed of the wheel derived from RPM, in km/h.
    /// </summary>
    public float wheelRPM2Speed = 0f;

    public float WheelRPM {

        get {

            if (WheelCollider == null)
                return 0f;

            float rawRpm = WheelCollider.rpm;

            // Only update when grounded
            if (WheelCollider.isGrounded) {
                // simple low-pass filter: smoothFactor ~ 0.1�0.2
                _filteredRpm = Mathf.Lerp(_filteredRpm, rawRpm, 0.15f);
            }

            return _filteredRpm;

        }

    }
    private float _filteredRpm = 0f;

    /// <summary>
    /// Width of the wheel used for skidmarks.
    /// </summary>
    [Space()]
    [Min(.1f)]
    public float width = .25f;

    /// <summary>
    /// Total rotation of the wheel (for spinning animation).
    /// </summary>
    private float wheelRotation = 0f;

    /// <summary>
    /// Camber angle, caster angle, and X offset to adjust wheel tilt and position.
    /// </summary>
    public float camber, caster, offset = 0f;

    #endregion

    #region Physics Fields

    /// <summary>
    /// Represents the 'temperature' or usage stress of this wheel. Increases with slip.
    /// </summary>
    [Space()]
    public float totalWheelTemp = MIN_WHEEL_TEMPERATURE;

    /// <summary>
    /// Combined magnitude of forward and sideways slip, used for skids and audio.
    /// </summary>
    [System.Obsolete("Use TotalSlip instead totalSlip.")]
    public float totalSlip {

        get {

            return TotalSlip;

        }

    }

    [System.Obsolete("Use SidewaysSlip instead wheelSlipAmountSideways.")]
    public float wheelSlipAmountSideways {

        get {

            return SidewaysSlip;

        }

    }

    [System.Obsolete("Use ForwardSlip instead wheelSlipAmountForward.")]
    public float wheelSlipAmountForward {

        get {

            return ForwardSlip;

        }

    }

    public float ForwardSlip {

        get {

            if (WheelCollider == null)
                return 0f;

            float rawSlip = wheelHit.forwardSlip;

            if (!isGrounded)
                rawSlip = 0f;

            // simple low-pass filter: smoothFactor ~ 0.1�0.2
            _filteredForwardSlip = Mathf.Lerp(_filteredForwardSlip, rawSlip, 0.15f);

            return _filteredForwardSlip;

        }

    }
    private float _filteredForwardSlip = 0f;

    public float SidewaysSlip {

        get {

            if (WheelCollider == null)
                return 0f;

            float rawSlip = wheelHit.sidewaysSlip;

            if (!isGrounded)
                rawSlip = 0f;

            // simple low-pass filter: smoothFactor ~ 0.1�0.2
            _filteredSidewaysSlip = Mathf.Lerp(_filteredSidewaysSlip, rawSlip, 0.15f);

            return _filteredSidewaysSlip;

        }

    }
    private float _filteredSidewaysSlip = 0f;

    public float TotalSlip {

        get {

            if (WheelCollider == null)
                return 0f;

            return Mathf.Abs(ForwardSlip) + Mathf.Abs(SidewaysSlip);

        }

    }

    /// <summary>
    /// Current bump force used in collision/bump sound calculations.
    /// </summary>
    [HideInInspector]
    public float bumpForce, oldForce = 0f;

    #endregion

    #region Skidmarks Fields

    /// <summary>
    /// Whether this wheel can generate skidmarks or not.
    /// </summary>
    [Space()]
    public bool drawSkid = true;

    /// <summary>
    /// Index of the last skidmark created by this wheel in the global SkidmarksManager.
    /// </summary>
    private int lastSkidmark = -1;

    #endregion

    #region Friction Fields

    /// <summary>
    /// Forward friction curve used by this wheel.
    /// </summary>
    private WheelFrictionCurve forwardFrictionCurve;

    /// <summary>
    /// Sideways friction curve used by this wheel.
    /// </summary>
    private WheelFrictionCurve sidewaysFrictionCurve;

    /// <summary>
    /// Default forward friction curve (backup for resetting).
    /// </summary>
    private WheelFrictionCurve forwardFrictionCurve_Def;

    /// <summary>
    /// Default sideways friction curve (backup for resetting).
    /// </summary>
    private WheelFrictionCurve sidewaysFrictionCurve_Def;

    #endregion

    #region Traction Control Fields

    /// <summary>
    /// ESP traction cut factor applied to reduce motor torque during slip.
    /// </summary>
    [Space()]
    public float cutTractionESP = 0f;

    /// <summary>
    /// TCS traction cut factor applied to reduce motor torque during forward slip.
    /// </summary>
    public float cutTractionTCS = 0f;

    /// <summary>
    /// ABS brake cut factor applied to reduce brake torque during wheel lock.
    /// </summary>
    public float cutBrakeABS = 0f;

    #endregion

    #region Audio Fields

    /// <summary>
    /// AudioSource component for skid sound effects.
    /// </summary>
    private AudioSource skidAudioSource;

    /// <summary>
    /// Audio clip for skid sounds, determined by ground material.
    /// </summary>
    private AudioClip skidClip;

    /// <summary>
    /// Volume level for skid audio, determined by ground material and slip amount.
    /// </summary>
    private float skidVolume = 0f;

    #endregion

    #region Special State Fields

    /// <summary>
    /// Whether this wheel is currently deflated (flat tire).
    /// </summary>
    [Space()]
    public bool deflated = false;

    /// <summary>
    /// Scaling factor to reduce wheel radius when deflated.
    /// </summary>
    [Min(0f)]
    public float deflatedRadiusMultiplier = .8f;

    /// <summary>
    /// Stiffness multiplier applied when wheel is deflated.
    /// </summary>
    [Min(0f)]
    public float deflatedStiffnessMultiplier = .25f;

    /// <summary>
    /// Cached un-deflated radius. Used to restore radius on inflation.
    /// </summary>
    [Min(0f)]
    private float defRadius = -1f;

    /// <summary>
    /// Whether drift mode is active for this wheel.
    /// </summary>
    [Space()]
    public bool driftMode = false;

    /// <summary>
    /// Whether to use stable friction curves that adjust based on vehicle speed.
    /// </summary>
    public bool stableFrictionCurves = false;

    /// <summary>
    /// Minimum forward stiffness for drift mode.
    /// </summary>
    private readonly float minForwardStiffnessForDrift = .75f;

    /// <summary>
    /// Maximum forward stiffness for drift mode.
    /// </summary>
    private readonly float maxForwardStiffnessForDrift = 1.25f;

    /// <summary>
    /// Minimum sideways stiffness for drift mode.
    /// </summary>
    private readonly float minSidewaysStiffnessForDrift = .45f;

    /// <summary>
    /// Maximum sideways stiffness for drift mode.
    /// </summary>
    private readonly float maxSidewaysStiffnessForDrift = 1f;

    #endregion

    #region Ackermann Steering Fields

    /// <summary>
    /// Distance between the front and rear axles, used in steering calculations (Ackermann).
    /// </summary>
    [Space()]
    public float wheelbase = 2.55f;

    /// <summary>
    /// Distance between the left and right wheels on an axle.
    /// </summary>
    public float trackWidth = 1.5f;

    /// <summary>
    /// Holds a smoothed or partially processed velocity-based value used in drift calculations.
    /// </summary>
    private float sqrVel;

    #endregion

    #region Performance Cache Fields

    // Performance optimization: cache frequently used references
    private RCCP_GroundMaterials cachedGroundMaterials;
    private RCCP_SkidmarksManager cachedSkidmarksManager;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Unity Awake method. Ensures the wheel model is assigned. Disables if missing.
    /// </summary>
    public override void Awake() {

        base.Awake();

        if (wheelModel == null) {

            Debug.LogError("Wheel model is not selected for " + transform.name + ". Disabling this wheelcollider.");
            enabled = false;
            return;

        }

        gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

        // Cache references for performance
        CacheReferences();

    }

    /// <summary>
    /// Unity Start method. Configures wheel mass, obtains friction curves, creates audio source, and sets up a pivot transform for the wheel model.
    /// </summary>
    public override void Start() {

        base.Start();

        // Increasing mass of the wheel for more stable handling. 
        // In RCCPSettings, if useFixedWheelColliders is true, it sets mass based on the vehicle mass.
        if (RCCPSettings.useFixedWheelColliders) {

            WheelCollider.mass = CarController.Rigid.mass / WHEEL_MASS_DIVIDER;
            WheelCollider.ConfigureVehicleSubsteps(1000f, 10, 8);

        }

        // Getting friction curves from the wheels.
        forwardFrictionCurve = WheelCollider.forwardFriction;
        sidewaysFrictionCurve = WheelCollider.sidewaysFriction;

        // Getting default friction curves from the wheels for later resets.
        forwardFrictionCurve_Def = forwardFrictionCurve;
        sidewaysFrictionCurve_Def = sidewaysFrictionCurve;

        // Creating a pivot at the correct position and rotation for the wheel model.
        GameObject newPivot = new GameObject("Pivot_" + wheelModel.transform.name);

        newPivot.transform.SetPositionAndRotation(RCCP_GetBounds.GetBoundsCenter(wheelModel.transform), transform.rotation);
        newPivot.transform.SetParent(wheelModel.transform.parent, true);

        // Assigning the actual wheel model to the new pivot.
        wheelModel.SetParent(newPivot.transform, true);
        wheelModel = newPivot.transform;

        Invoke(nameof(CreateAudioSource), AUDIO_SOURCE_CREATION_DELAY);

    }

    /// <summary>
    /// Unity OnEnable method. Re-applies mass to the wheel if useFixedWheelColliders is active.
    /// </summary>
    public override void OnEnable() {

        base.OnEnable();

        //// Increasing mass of the wheel for more stable handling.
        //if (RCCPSettings.useFixedWheelColliders) {

        //    WheelCollider.mass = CarController.Rigid.mass / WHEEL_MASS_DIVIDER;

        //    // Suggested Values Based on Use Case
        //    // Vehicle Type    Speed Threshold Steps Below Steps Above
        //    // Realistic Cars  5 - 10 m / s(18 - 36 km / h)   5 - 10    1 - 3
        //    // Arcade Cars 5 - 15 m / s(18 - 54 km / h)   3 - 5 1 - 2
        //    // Off - Road / Bumpy Terrain    3 - 8 m / s(11 - 28 km / h)    10 - 15   5 - 7
        //    // High - Speed Racing Cars  15 - 25 m / s(54 - 90 km / h)  3 - 6 1 - 2
        //    WheelCollider.ConfigureVehicleSubsteps(5f, 9, 6);

        //}

    }

    /// <summary>
    /// Unity Update method. Optionally aligns the visual wheel model.
    /// </summary>
    private void Update() {

        if (alignWheels)
            WheelAlign();

    }

    /// <summary>
    /// Unity FixedUpdate method. Calculates speed from RPM, applies motor/brake torque, handles friction, skidmarks, etc.
    /// </summary>
    private void FixedUpdate() {

        // If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        // Convert RPM to approximate speed (km/h).
        CalculateWheelSpeed();

        // Execute all wheel physics calculations
        MotorTorque();
        BrakeTorque();
        GroundMaterial();
        Frictions();
        SkidMarks();
        WheelTemp();
        Audio();

        negativeFeedbackIntensity = 0f;

    }

    #endregion

    #region Performance Optimization Methods

    /// <summary>
    /// Caches frequently used component references for performance.
    /// </summary>
    private void CacheReferences() {

        cachedGroundMaterials = RCCP_GroundMaterials.Instance;
        cachedSkidmarksManager = RCCP_SkidmarksManager.Instance;

    }

    /// <summary>
    /// Calculates wheel speed from RPM with null checking and optimization.
    /// </summary>
    private void CalculateWheelSpeed() {

        if (WheelCollider == null)
            return;

        float circumference = 2.0f * Mathf.PI * WheelCollider.radius;

        if (Mathf.Abs(WheelRPM) > SPEED_THRESHOLD_CHECK)
            wheelRPM2Speed = (circumference * WheelRPM) * CIRCUMFERENCE_MULTIPLIER;
        else
            wheelRPM2Speed = 0f;

    }

    #endregion

    #region Visual Alignment Methods

    /// <summary>
    /// Aligning wheel model position and rotation to match the WheelCollider, accounting for camber/caster.
    /// </summary>
    private void WheelAlign() {

        // Return if no wheel model selected.
        if (wheelModel == null)
            return;

        // If wheelcollider is not enabled, hide or disable model. Otherwise show it.
        wheelModel.gameObject.SetActive(WheelCollider.enabled);

        // If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        // Positions and rotations of the wheel.
        Vector3 wheelPosition;
        Quaternion wheelRotation;

        // Getting position and rotation from WheelCollider.
        WheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

        // Increase the rotation value based on RPM.
        this.wheelRotation += WheelRPM * RPM_TO_DEGREES_MULTIPLIER * Time.deltaTime;

        // Assigning position and rotation to the wheel model.
        wheelModel.transform.SetPositionAndRotation(wheelPosition, transform.rotation * Quaternion.Euler(this.wheelRotation, WheelCollider.steerAngle, 0f));

        // Adjust offset by X axis to simulate different rim offsets.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.position += (transform.right * offset);
        else
            wheelModel.transform.position -= (transform.right * offset);

        // Adjusting camber angle by Z axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, -camber);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, camber);

        // Adjusting caster angle by X axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, -caster);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, caster);

    }

    #endregion

    #region Torque Application Methods

    /// <summary>
    /// Applies the accumulated motorTorque to the WheelCollider's motorTorque, factoring in traction cuts (ESP/TCS) and overtorque checks.
    /// FIXED: Now properly handles negative torque for engine braking and over-rev protection.
    /// </summary>
    private void MotorTorque() {

        float torque = motorTorque;
        bool positiveTorque = torque >= 0f;

        // IMPORTANT: Only apply traction control cuts to positive (driving) torque
        // Negative torque (engine braking) should NOT be limited by ESP/TCS systems

        if (positiveTorque) {

            // Cut traction for ESP only on positive torque.
            if (cutTractionESP != 0f) {

                torque -= Mathf.Clamp(torque * (Mathf.Abs(SidewaysSlip) * cutTractionESP), 0f, Mathf.Infinity);
                torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

            }

            // Cut traction for TCS if there's forward slip, only on positive torque.
            if (cutTractionTCS != 0f && Mathf.Abs(ForwardSlip) > .05f) {

                if (Mathf.Sign(WheelRPM) >= 0) {

                    torque -= Mathf.Clamp(torque * (Mathf.Abs(ForwardSlip) * cutTractionTCS), 0f, Mathf.Infinity);
                    torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

                }

            }

        } else {

            // For negative torque (engine braking), only apply TCS if wheel is spinning backwards
            if (cutTractionTCS != 0f && Mathf.Abs(ForwardSlip) > .05f) {

                if (Mathf.Sign(WheelRPM) < 0) {

                    torque += Mathf.Clamp(-torque * (Mathf.Abs(ForwardSlip) * cutTractionTCS), 0f, Mathf.Infinity);
                    torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

                }

            }

        }

        float bTorque = torque;

        // FIXED: Reduce minimum torque threshold to allow smaller negative values to pass through
        // Original was 5f which was too high for engine braking effects
        if (Mathf.Abs(torque) < 1f)
            torque = 0f;

        // IMPORTANT FIX: Only zero out positive torque when overtorque conditions are met
        // Engine braking (negative torque) should still work even when engine is off or speed limit reached
        if (CheckOvertorque())
            torque = 0f;

        torque = Mathf.Lerp(torque, -.5f * bTorque, negativeFeedbackIntensity);

        if (positiveTorque && Mathf.Abs(wheelRPM2Speed) > 50f && wheelRPM2Speed < 50f)
            torque = 0f;

        if (!positiveTorque && Mathf.Abs(wheelRPM2Speed) > 50f && wheelRPM2Speed > -50f)
            torque = 0f;

        // Apply final torque to wheel collider
        WheelCollider.motorTorque = torque;

        // Reset values for next frame
        cutTractionESP = 0f;
        cutTractionTCS = 0f;
        motorTorque = 0f;

    }

    /// <summary>
    /// Applies the accumulated brakeTorque to the WheelCollider's brakeTorque, factoring in ABS cuts.
    /// </summary>
    private void BrakeTorque() {

        float torque = brakeTorque;

        // ABS brake cut.
        if (cutBrakeABS != 0f) {

            torque -= Mathf.Clamp(torque * cutBrakeABS, 0f, Mathf.Infinity);
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        }

        torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        if (torque < MIN_TORQUE_THRESHOLD)
            torque = 0f;

        WheelCollider.brakeTorque = torque;

        cutBrakeABS = 0f;
        brakeTorque = 0f;

    }

    #endregion

    #region Physics Calculation Methods

    /// <summary>
    /// Manages friction curves and updates slip values. Also applies drift mode changes if enabled.
    /// </summary>
    private void Frictions() {

        // Null check for cached ground materials
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null)
            return;

        // Null check before accessing ground materials array
        if (groundIndex >= 0 && groundIndex < cachedGroundMaterials.frictions.Length) {

            if (TotalSlip >= cachedGroundMaterials.frictions[groundIndex].slip)
                isSkidding = true;
            else
                isSkidding = false;

            // Setting stiffness of the forward and sideways friction curves.
            forwardFrictionCurve.stiffness = cachedGroundMaterials.frictions[groundIndex].forwardStiffness;
            sidewaysFrictionCurve.stiffness = ((cachedGroundMaterials.frictions[groundIndex].sidewaysStiffness * (1f - (handbrakeInput / HANDBRAKE_DIVISOR))) * connectedAxle.tractionHelpedSidewaysStiffness);

        }

        handbrakeInput = 0f;

        // If wheel is deflated, multiply the stiffness by the deflatedStiffnessMultiplier.
        if (deflated) {

            forwardFrictionCurve.stiffness *= deflatedStiffnessMultiplier;
            sidewaysFrictionCurve.stiffness *= deflatedStiffnessMultiplier;

        }

        // If drift mode is active, apply drift adjustments to friction curves.
        if (driftMode)
            Drift();

        if (stableFrictionCurves)
            TuneFrictionCurves();

        // Setting new friction curves to wheels if they have changed significantly.
        if (!ApproximatelyEqualFriction(forwardFrictionCurve, WheelCollider.forwardFriction))
            WheelCollider.forwardFriction = forwardFrictionCurve;

        if (!ApproximatelyEqualFriction(sidewaysFrictionCurve, WheelCollider.sidewaysFriction))
            WheelCollider.sidewaysFriction = sidewaysFrictionCurve;

        // Also control wheel damping based on motor torque.
        if (groundIndex >= 0 && groundIndex < cachedGroundMaterials.frictions.Length) {

            if (Mathf.Abs(WheelCollider.motorTorque) < 100f)
                WheelCollider.wheelDampingRate = cachedGroundMaterials.frictions[groundIndex].damp;
            else
                WheelCollider.wheelDampingRate = 0f;

        }

    }

    /// <summary>
    /// Updates wheel temperature based on slip and cools it over time.
    /// </summary>
    private void WheelTemp() {

        if (isSkidding)
            totalWheelTemp += Time.fixedDeltaTime * WHEEL_TEMPERATURE_INCREASE_RATE * TotalSlip;

        totalWheelTemp -= Time.fixedDeltaTime * WHEEL_TEMPERATURE_DECREASE_RATE;
        totalWheelTemp = Mathf.Clamp(totalWheelTemp, MIN_WHEEL_TEMPERATURE, MAX_WHEEL_TEMPERATURE);

    }

    #endregion

    #region Ground Material Detection Methods

    /// <summary>
    /// Determines the appropriate ground material index by checking contact's PhysicMaterial or terrain texture.
    /// </summary>
    private void GroundMaterial() {

        isGrounded = WheelCollider.GetGroundHit(out wheelHit);

        // If there are no contact points, set default index to 0.
        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null) {

            groundIndex = 0;
            return;

        }

        // Null check for cached ground materials
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null) {

            groundIndex = 0;
            return;

        }

        // Contacted any physic material in Configurable Ground Materials yet?
        bool contactedWithAnyMaterialYet = false;

        // Checking the material of the contact point in the RCCP_GroundMaterials ground frictions.
        for (int i = 0; i < cachedGroundMaterials.frictions.Length; i++) {

            // If there is one, assign the index of the material. 
            if (wheelHit.collider.sharedMaterial == cachedGroundMaterials.frictions[i].groundMaterial) {

                contactedWithAnyMaterialYet = true;
                groundIndex = i;
                break; // Performance optimization: break when found

            }

        }

        // If ground PhysicMaterial is not found among configured ground materials, check if we are on a terrain collider.
        if (!contactedWithAnyMaterialYet) {

            // If terrains are not initialized yet, return.
            if (!RCCPSceneManager.terrainsInitialized) {

                groundIndex = 0;
                return;

            }

            // Null check for terrain ground materials
            if (cachedGroundMaterials.terrainFrictions == null) {

                groundIndex = 0;
                return;

            }

            // Checking the material of the contact point in the RCCP_GroundMaterials terrain frictions.
            for (int i = 0; i < cachedGroundMaterials.terrainFrictions.Length; i++) {

                if (wheelHit.collider.sharedMaterial == cachedGroundMaterials.terrainFrictions[i].groundMaterial) {

                    RCCP_SceneManager.Terrains currentTerrain = null;

                    // Null check for terrains array
                    if (RCCPSceneManager.terrains != null) {

                        for (int l = 0; l < RCCPSceneManager.terrains.Length; l++) {

                            if (RCCPSceneManager.terrains[l] != null && RCCPSceneManager.terrains[l].terrainCollider == cachedGroundMaterials.terrainFrictions[i].groundMaterial) {
                                currentTerrain = RCCPSceneManager.terrains[l];
                                break;
                            }

                        }

                    }

                    // Once we have that terrain, get exact position in the terrain map coordinate.
                    if (currentTerrain != null && currentTerrain.terrain != null) {

                        Vector3 playerPos = transform.position;
                        Vector3 TerrainCord = ConvertToSplatMapCoordinate(currentTerrain.terrain, playerPos);
                        float comp = 0f;

                        // Null check for splatmap data
                        if (currentTerrain.mSplatmapData != null && TerrainCord.x >= 0 && TerrainCord.z >= 0 &&
                            TerrainCord.x < currentTerrain.alphamapWidth && TerrainCord.z < currentTerrain.alphamapHeight) {

                            // Finding the right terrain texture around the hit position.
                            for (int k = 0; k < currentTerrain.mNumTextures; k++) {

                                if (comp < currentTerrain.mSplatmapData[(int)TerrainCord.z, (int)TerrainCord.x, k])
                                    groundIndex = k;

                            }

                            // Null check for splatmap indexes before assignment
                            if (cachedGroundMaterials.terrainFrictions[i].splatmapIndexes != null &&
                                groundIndex >= 0 && groundIndex < cachedGroundMaterials.terrainFrictions[i].splatmapIndexes.Length) {

                                // Assign the index of the material based on splatmap indexes.
                                groundIndex = cachedGroundMaterials.terrainFrictions[i].splatmapIndexes[groundIndex].index;

                            }

                        }

                    }

                    break; // Performance optimization: break when terrain found

                }

            }

        }

    }

    #endregion

    #region Skidmarks Methods

    /// <summary>
    /// Handles skidmark generation based on slip threshold and wheel contact.
    /// </summary>
    private void SkidMarks() {

        // If drawing skids are not enabled, return.
        if (!drawSkid)
            return;

        // Null checks for safety
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null)
            return;

        if (groundIndex < 0 || groundIndex >= cachedGroundMaterials.frictions.Length)
            return;

        // If slip is above the ground friction slip threshold...
        if (TotalSlip > cachedGroundMaterials.frictions[groundIndex].slip) {

            Vector3 skidPoint = wheelHit.point + (CarController.Rigid.linearVelocity * Time.deltaTime);

            // If velocity is nonzero and the wheel is grounded, record a new skidmark.
            if (CarController.Rigid.linearVelocity.magnitude > .1f && isGrounded && wheelHit.normal != Vector3.zero && wheelHit.point != Vector3.zero && skidPoint != Vector3.zero && Mathf.Abs(skidPoint.magnitude) >= .1f) {

                if (cachedSkidmarksManager != null)
                    lastSkidmark = cachedSkidmarksManager.AddSkidMark(skidPoint, wheelHit.normal, TotalSlip - cachedGroundMaterials.frictions[groundIndex].slip, width, lastSkidmark, groundIndex);

            } else {

                lastSkidmark = -1;

            }

        } else {

            // Slip is not above threshold, reset last skidmark index.
            lastSkidmark = -1;

        }

    }

    #endregion

    #region Audio Methods

    /// <summary>
    /// Creating audiosource for skid SFX.
    /// </summary>
    private void CreateAudioSource() {

        if (skidAudioSource != null)
            return;

        // Null check for CarController and Audio components
        if (CarController == null)
            return;

        if (CarController.Audio != null && CarController.Audio.audioMixer != null)
            skidAudioSource = RCCP_AudioSource.NewAudioSource(CarController.Audio.audioMixer, CarController.gameObject, "Skid Sound AudioSource", 3f, 50f, 0f, null, true, true, false);
        else
            skidAudioSource = RCCP_AudioSource.NewAudioSource(CarController.gameObject, "Skid Sound AudioSource", 3f, 50f, 0f, null, true, true, false);

        if (CarController.Audio != null && skidAudioSource != null) {

            if (CarController.Audio.transform.childCount > 0)
                skidAudioSource.transform.SetParent(CarController.Audio.transform.GetChild(0), true);
            else
                skidAudioSource.transform.SetParent(CarController.Audio.transform, true);

        }

    }

    /// <summary>
    /// Manages the skid audio playback by monitoring total slip and applying volumes/pitches.
    /// Also calculates a bump force when the wheel hits large forces.
    /// </summary>
    private void Audio() {

        // Null checks for safety
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null)
            return;

        if (groundIndex < 0 || groundIndex >= cachedGroundMaterials.frictions.Length)
            return;

        if (skidAudioSource != null) {

            // If total slip is high enough, play skid SFX.
            if (TotalSlip > cachedGroundMaterials.frictions[groundIndex].slip) {

                skidClip = cachedGroundMaterials.frictions[groundIndex].groundSound;
                skidVolume = cachedGroundMaterials.frictions[groundIndex].volume;

                if (skidAudioSource.clip != skidClip)
                    skidAudioSource.clip = skidClip;

                if (!skidAudioSource.isPlaying)
                    skidAudioSource.Play();

                if (CarController.Rigid.linearVelocity.magnitude > .1f) {

                    skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, Mathf.Lerp(0f, skidVolume, TotalSlip - cachedGroundMaterials.frictions[groundIndex].slip), Time.fixedDeltaTime * 10f);
                    skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, Mathf.Lerp(.7f, 1f, TotalSlip - cachedGroundMaterials.frictions[groundIndex].slip), Time.fixedDeltaTime * 10f);

                } else {

                    skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, 0f, Time.fixedDeltaTime * 10f);
                    skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, 1f, Time.fixedDeltaTime * 10f);

                }

            } else {

                skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, 0f, Time.fixedDeltaTime * 10f);
                skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, 1f, Time.fixedDeltaTime * 10f);

                if (skidAudioSource.volume <= MIN_AUDIO_VOLUME_TO_STOP && skidAudioSource.isPlaying)
                    skidAudioSource.Stop();

            }

            if (skidAudioSource.volume < SKID_VOLUME_THRESHOLD)
                skidAudioSource.volume = 0f;

        }

        // Calculate bump force based on difference in hit force.
        bumpForce = wheelHit.force - oldForce;

        // If bump force is high enough, you could play a bump SFX here.
        if ((bumpForce) >= BUMP_FORCE_THRESHOLD) {
            // Example: Trigger bump sounds, apply random pitch, etc.
        }

        oldForce = wheelHit.force;

    }

    #endregion

    #region Input Methods

    /// <summary>
    /// Applies Ackermann steering geometry to this wheel based on the given steering angle.
    /// </summary>
    /// <param name="steeringAngle">Input steering angle in degrees</param>
    public void ApplySteering(float steeringAngle) {

        if (!WheelCollider.enabled)
            return;

        float avgAngleDeg = steeringAngle;
        float avgAngleRad = avgAngleDeg * Mathf.Deg2Rad;

        float radiusInside = wheelbase / Mathf.Tan(Mathf.Abs(avgAngleRad));
        float finalAngleDeg;

        bool turningRight = steeringAngle > 0f;
        bool turningLeft = steeringAngle < 0f;
        bool thisIsLeftWheel = transform.localPosition.x < 0f;

        if (turningRight) {

            if (thisIsLeftWheel) {

                // Outside wheel (left) during right turn - larger turning radius
                float outsideAngleRad = Mathf.Atan(wheelbase / (radiusInside + trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * outsideAngleRad;

            } else {

                // Inside wheel (right) during right turn - smaller turning radius
                float insideAngleRad = Mathf.Atan(wheelbase / (radiusInside - trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * insideAngleRad;

            }

        } else if (turningLeft) {

            if (thisIsLeftWheel) {

                // Inside wheel (left) during left turn - smaller turning radius
                float insideAngleRad = Mathf.Atan(wheelbase / (radiusInside - trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * insideAngleRad;

            } else {

                // Outside wheel (right) during left turn - larger turning radius
                float outsideAngleRad = Mathf.Atan(wheelbase / (radiusInside + trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * outsideAngleRad;

            }

            finalAngleDeg *= -1f;

        } else {

            finalAngleDeg = 0f;

        }

        WheelCollider.steerAngle = finalAngleDeg;

    }

    /// <summary>
    /// Adds motor torque (Nm) to be applied in the next FixedUpdate. Positive for forward, negative for reverse.
    /// </summary>
    /// <param name="torque">Motor torque in Newton-meters</param>
    public void AddMotorTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        motorTorque += torque;

    }

    /// <summary>
    /// Adds brake torque (Nm) to be applied in the next FixedUpdate.
    /// </summary>
    /// <param name="torque">Brake torque in Newton-meters</param>
    public void AddBrakeTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;

    }

    /// <summary>
    /// Adds handbrake torque (Nm) to be applied in the next FixedUpdate, also sets the handbrake input factor.
    /// </summary>
    /// <param name="torque">Handbrake torque in Newton-meters</param>
    public void AddHandbrakeTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;
        handbrakeInput += Mathf.Clamp01(torque / 1000f);

    }

    public void AddNegativeFeedback(float intensity) {

        if (!WheelCollider.enabled)
            return;

        negativeFeedbackIntensity += intensity;
        negativeFeedbackIntensity = Mathf.Clamp01(negativeFeedbackIntensity);

    }

    /// <summary>
    /// Cuts traction torque (ESP) to control slip. Larger values reduce more motor torque.
    /// </summary>
    /// <param name="_cutTraction">ESP traction cut factor (0-1)</param>
    public void CutTractionESP(float _cutTraction) {

        if (!WheelCollider.enabled)
            return;

        cutTractionESP = _cutTraction;

    }

    /// <summary>
    /// Cuts traction torque (TCS) for forward slip. Larger values reduce more motor torque.
    /// </summary>
    /// <param name="_cutTraction">TCS traction cut factor (0-1)</param>
    public void CutTractionTCS(float _cutTraction) {

        if (!WheelCollider.enabled)
            return;

        cutTractionTCS = _cutTraction;

    }

    /// <summary>
    /// Cuts brake torque (ABS) to prevent wheel lock. Larger values reduce more brake torque.
    /// </summary>
    /// <param name="_cutBrake">ABS brake cut factor (0-1)</param>
    public void CutBrakeABS(float _cutBrake) {

        if (!WheelCollider.enabled)
            return;

        cutBrakeABS = _cutBrake;

    }

    #endregion

    #region Special State Methods

    /// <summary>
    /// Deflates the wheel, reducing radius and friction stiffness. Triggers events in CarController.
    /// </summary>
    public void Deflate() {

        if (!WheelCollider.enabled)
            return;

        if (deflated)
            return;

        deflated = true;

        if (defRadius == -1)
            defRadius = WheelCollider.radius;

        WheelCollider.radius = defRadius * deflatedRadiusMultiplier;

        if (CarController != null && CarController.Rigid != null) {
            CarController.Rigid.AddForceAtPosition(transform.right * UnityEngine.Random.Range(-1f, 1f) * 25f, transform.position, ForceMode.Acceleration);
            CarController.OnWheelDeflated();
        }

    }

    /// <summary>
    /// Inflates the wheel, restoring normal friction stiffness and handling.
    /// </summary>
    public void Inflate() {

        if (!WheelCollider.enabled)
            return;

        if (!deflated)
            return;

        deflated = false;

        if (defRadius != -1)
            WheelCollider.radius = defRadius;

        if (CarController != null)
            CarController.OnWheelInflated();

    }

    /// <summary>
    /// Applies drift mode friction adjustments based on vehicle velocity and wheel position.
    /// Uses more sophisticated calculation than the original simple version.
    /// </summary>
    private void Drift() {

        // Null check for CarController
        if (CarController == null || CarController.Rigid == null)
            return;

        // 1. Calculate squared velocity based on lateral movement for drift calculations
        Vector3 relativeVelocity = transform.InverseTransformDirection(CarController.Rigid.linearVelocity);
        sqrVel = Mathf.Lerp(sqrVel, (relativeVelocity.x * relativeVelocity.x) / 50f, Time.fixedDeltaTime * 100f);

        // 2. Incorporate forward slip if any.
        if (Mathf.Abs(wheelHit.forwardSlip) > 0f) {
            sqrVel += (Mathf.Abs(wheelHit.forwardSlip) * 0.5f);
        }

        sqrVel = Mathf.Max(sqrVel, 0f);

        // 3. Adjust forward friction differently for rear wheels (z < 0) vs front wheels (z >= 0).
        if (transform.localPosition.z < 0) {

            // Rear wheels - more aggressive drift behavior
            forwardFrictionCurve.extremumValue = Mathf.Clamp(
                forwardFrictionCurve_Def.extremumValue - (sqrVel / 1f),
                minForwardStiffnessForDrift,
                maxForwardStiffnessForDrift
            );
            forwardFrictionCurve.asymptoteValue = Mathf.Clamp(
                forwardFrictionCurve_Def.asymptoteValue + (sqrVel / 1f),
                minForwardStiffnessForDrift,
                maxForwardStiffnessForDrift
            );

        } else {

            // Front wheels - less aggressive drift behavior
            forwardFrictionCurve.extremumValue = Mathf.Clamp(
                forwardFrictionCurve_Def.extremumValue - (sqrVel / 0.5f),
                minForwardStiffnessForDrift / 2f,
                maxForwardStiffnessForDrift
            );
            forwardFrictionCurve.asymptoteValue = Mathf.Clamp(
                forwardFrictionCurve_Def.asymptoteValue - (sqrVel / 0.5f),
                minForwardStiffnessForDrift / 2f,
                maxForwardStiffnessForDrift
            );

        }

        // 4. Adjust sideways friction for drifting.
        sidewaysFrictionCurve.extremumValue = Mathf.Clamp(
            sidewaysFrictionCurve_Def.extremumValue - (sqrVel / 1f),
            minSidewaysStiffnessForDrift,
            maxSidewaysStiffnessForDrift
        );
        sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(
            sidewaysFrictionCurve_Def.asymptoteValue - (sqrVel / 1f),
            minSidewaysStiffnessForDrift,
            maxSidewaysStiffnessForDrift
        );

    }

    /// <summary>
    /// Applies speed-based friction curve tuning for more stable handling at different speeds.
    /// </summary>
    private void TuneFrictionCurves() {

        // Null check for CarController
        if (CarController == null)
            return;

        float speedFactor = Mathf.InverseLerp(0f, 360f, CarController.absoluteSpeed);

        // Forward friction adjustments based on speed
        forwardFrictionCurve.extremumSlip = Mathf.Lerp(forwardFrictionCurve_Def.extremumSlip, forwardFrictionCurve_Def.extremumSlip * .95f, speedFactor);
        forwardFrictionCurve.extremumValue = Mathf.Lerp(forwardFrictionCurve_Def.extremumValue, forwardFrictionCurve_Def.extremumValue * 1.05f, speedFactor);
        forwardFrictionCurve.asymptoteSlip = Mathf.Lerp(forwardFrictionCurve_Def.asymptoteSlip, forwardFrictionCurve_Def.asymptoteSlip * .95f, speedFactor);
        forwardFrictionCurve.asymptoteValue = Mathf.Lerp(forwardFrictionCurve_Def.asymptoteValue, forwardFrictionCurve_Def.asymptoteValue * 1.05f, speedFactor);

        // Sideways friction adjustments based on speed
        sidewaysFrictionCurve.extremumSlip = Mathf.Lerp(sidewaysFrictionCurve_Def.extremumSlip, sidewaysFrictionCurve_Def.extremumSlip * .95f, speedFactor);
        sidewaysFrictionCurve.extremumValue = Mathf.Lerp(sidewaysFrictionCurve_Def.extremumValue, sidewaysFrictionCurve_Def.extremumValue * 1.05f, speedFactor);
        sidewaysFrictionCurve.asymptoteSlip = Mathf.Lerp(sidewaysFrictionCurve_Def.asymptoteSlip, sidewaysFrictionCurve_Def.asymptoteSlip * .95f, speedFactor);
        sidewaysFrictionCurve.asymptoteValue = Mathf.Lerp(sidewaysFrictionCurve_Def.asymptoteValue, sidewaysFrictionCurve_Def.asymptoteValue * 1.05f, speedFactor);

    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if two WheelFrictionCurves are effectively the same (within a small epsilon).
    /// </summary>
    /// <param name="a">First friction curve</param>
    /// <param name="b">Second friction curve</param>
    /// <param name="epsilon">Comparison tolerance</param>
    /// <returns>True if curves are approximately equal</returns>
    private bool ApproximatelyEqualFriction(WheelFrictionCurve a, WheelFrictionCurve b, float epsilon = FRICTION_COMPARISON_EPSILON) {

        return
            Mathf.Abs(a.extremumSlip - b.extremumSlip) < epsilon &&
            Mathf.Abs(a.extremumValue - b.extremumValue) < epsilon &&
            Mathf.Abs(a.asymptoteSlip - b.asymptoteSlip) < epsilon &&
            Mathf.Abs(a.asymptoteValue - b.asymptoteValue) < epsilon &&
            Mathf.Abs(a.stiffness - b.stiffness) < epsilon;

    }

    /// <summary>
    /// Checks if the wheel should stop receiving motor torque due to engine state or speed limits.
    /// </summary>
    /// <returns>True if motor torque should be cut</returns>
    private bool CheckOvertorque() {

        // Null checks for safety
        if (CarController == null)
            return true;

        if (!CarController.engineRunning)
            return true;

        if (CarController.absoluteSpeed > CarController.maximumSpeed)
            return true;

        // Null check for Gearbox before accessing
        if (CarController.Gearbox != null && CarController.Gearbox.TargetSpeeds != null && CarController.Gearbox.currentGear >= 0 && CarController.Gearbox.currentGear < CarController.Gearbox.TargetSpeeds.Length) {

            if (Mathf.Abs(wheelRPM2Speed) > (CarController.Gearbox.TargetSpeeds[CarController.Gearbox.currentGear] * GEAR_SPEED_TOLERANCE))
                return true;

        }

        return false;

    }

    #endregion

    #region Friction Configuration Methods

    /// <summary>
    /// Sets the forward friction curves of the wheel. Allows customizing slip and grip levels.
    /// </summary>
    /// <param name="extremumSlip">Slip value at peak grip</param>
    /// <param name="extremumValue">Peak grip value</param>
    /// <param name="asymptoteSlip">Slip value at sliding friction</param>
    /// <param name="asymptoteValue">Sliding friction value</param>
    public void SetFrictionCurvesForward(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        forwardFrictionCurve = newCurve;

        forwardFrictionCurve_Def = forwardFrictionCurve;

    }

    /// <summary>
    /// Sets the sideways friction curves of the wheel. Allows customizing slip and grip levels.
    /// </summary>
    /// <param name="extremumSlip">Slip value at peak grip</param>
    /// <param name="extremumValue">Peak grip value</param>
    /// <param name="asymptoteSlip">Slip value at sliding friction</param>
    /// <param name="asymptoteValue">Sliding friction value</param>
    public void SetFrictionCurvesSideways(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        sidewaysFrictionCurve = newCurve;

        sidewaysFrictionCurve_Def = sidewaysFrictionCurve;

    }

    #endregion

    #region Terrain Support Methods

    /// <summary>
    /// Converts world position to terrain splat map coordinates for checking terrain texture indexes.
    /// </summary>
    /// <param name="terrain">Target terrain</param>
    /// <param name="playerPos">World position to convert</param>
    /// <returns>Terrain coordinates in splat map space</returns>
    private Vector3 ConvertToSplatMapCoordinate(Terrain terrain, Vector3 playerPos) {

        if (terrain == null || terrain.terrainData == null)
            return Vector3.zero;

        Vector3 vecRet = new Vector3();
        Vector3 terPosition = terrain.transform.position;
        vecRet.x = ((playerPos.x - terPosition.x) / terrain.terrainData.size.x) * terrain.terrainData.alphamapWidth;
        vecRet.z = ((playerPos.z - terPosition.z) / terrain.terrainData.size.z) * terrain.terrainData.alphamapHeight;
        return vecRet;

    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Aligns wheel model with wheel collider in the Editor. Adjusts WheelCollider radius and position based on the model mesh bounds.
    /// </summary>
    public void AlignWheel() {

        if (!WheelCollider.enabled)
            return;

        if (wheelModel == null)
            return;

        transform.position = RCCP_GetBounds.GetBoundsCenter(wheelModel);
        transform.position += transform.up * (WheelCollider.suspensionDistance * (transform.root.localScale.y * (1f - WheelCollider.suspensionSpring.targetPosition)));
        WheelCollider.radius = RCCP_GetBounds.MaxBoundsExtent(wheelModel) / transform.root.localScale.y;

    }

    /// <summary>
    /// Detaches the wheel by creating a physics-enabled copy and disabling the original.
    /// </summary>
    public void DetachWheel() {

        if (!WheelCollider.enabled)
            return;

        if (wheelModel != null && !wheelModel.gameObject.activeSelf)
            return;

        // Create detached wheel copy
        GameObject clonedWheel = Instantiate(wheelModel.gameObject, wheelModel.transform.position, wheelModel.transform.rotation, null);
        clonedWheel.SetActive(true);
        clonedWheel.AddComponent<Rigidbody>();

        // Add mesh collider for physics
        GameObject clonedMeshCollider = new GameObject("MeshCollider");
        clonedMeshCollider.transform.SetParent(clonedWheel.transform, false);
        clonedMeshCollider.transform.position = RCCP_GetBounds.GetBoundsCenter(clonedWheel.transform);
        MeshCollider mc = clonedMeshCollider.AddComponent<MeshCollider>();
        MeshFilter biggestMesh = RCCP_GetBounds.GetBiggestMesh(clonedWheel.transform);

        if (biggestMesh != null && biggestMesh.mesh != null) {
            mc.sharedMesh = biggestMesh.mesh;
            mc.convex = true;
        }

        // Set appropriate layer
        clonedMeshCollider.layer = LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer);

        foreach (Transform item in clonedMeshCollider.GetComponentsInChildren<Transform>(true)) {
            item.gameObject.layer = LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer);
        }

        // Disable original wheel
        WheelCollider.enabled = false;

    }

    /// <summary>
    /// Repairs the wheel by re-enabling it and restoring visual model.
    /// </summary>
    public void OnRepair() {

        if (WheelCollider.enabled)
            return;

        WheelCollider.enabled = true;

        if (wheelModel != null)
            wheelModel.gameObject.SetActive(true);

        Inflate();

    }

    /// <summary>
    /// Resets all runtime fields (torques, slip, audio) for this wheel. Useful when toggling the wheel on/off.
    /// </summary>
    public void Reload() {

        negativeFeedbackIntensity = 0f;
        motorTorque = 0f;
        brakeTorque = 0f;
        steerInput = 0f;
        handbrakeInput = 0f;
        wheelRotation = 0f; // Fixed: removed duplicate assignment
        cutTractionESP = 0f;
        cutTractionTCS = 0f;
        cutBrakeABS = 0f;
        bumpForce = 0f;
        oldForce = 0f;
        lastSkidmark = -1;
        skidVolume = 0f;
        defRadius = -1f; // Reset cached radius for deflation system

        if (skidAudioSource != null) {

            skidAudioSource.volume = 0f;
            skidAudioSource.pitch = 1f;

        }

    }

    #endregion

    #region Unity Editor Methods

    /// <summary>
    /// Unity Reset method. Called when script is first added or component is reset in the Editor. 
    /// Initializes some default values for WheelCollider.
    /// </summary>
    private void Reset() {

        WheelCollider wc = GetComponent<WheelCollider>();

        if (wc == null)
            return;

        // Increasing mass of the wheel for more stable handling.
        if (RCCP_Settings.Instance.useFixedWheelColliders) {

            RCCP_CarController carController = GetComponentInParent<RCCP_CarController>(true);
            if (carController != null && carController.Rigid != null)
                wc.mass = carController.Rigid.mass / WHEEL_MASS_DIVIDER;

        }

        wc.forceAppPointDistance = WHEEL_FORCE_APP_POINT_DISTANCE;
        wc.suspensionDistance = WHEEL_SUSPENSION_DISTANCE;

        JointSpring js = wc.suspensionSpring;
        js.spring = WHEEL_SPRING_VALUE;
        js.damper = WHEEL_DAMPER_VALUE;
        wc.suspensionSpring = js;

        WheelFrictionCurve frictionCurveFwd = wc.forwardFriction;
        frictionCurveFwd.extremumSlip = FORWARD_FRICTION_EXTREMUM_SLIP;
        wc.forwardFriction = frictionCurveFwd;

        WheelFrictionCurve frictionCurveSide = wc.sidewaysFriction;
        frictionCurveSide.extremumSlip = SIDEWAYS_FRICTION_EXTREMUM_SLIP;
        wc.sidewaysFriction = frictionCurveSide;

    }

    #endregion

}