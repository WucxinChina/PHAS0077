//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Wraps Unity's WheelCollider with additional properties such as drift, deflation, dynamic friction adjustments,
/// and skid particle/audio management. Also aligns wheel models with colliders at runtime.
/// </summary>
[RequireComponent(typeof(WheelCollider))]
public class RCC_WheelCollider : RCC_Core {

    #region References

    /// <summary>
    /// The underlying Unity WheelCollider used by RCC.
    /// </summary>
    public WheelCollider WheelCollider {

        get {

            if (_wheelCollider == null)
                _wheelCollider = GetComponent<WheelCollider>();

            return _wheelCollider;

        }

    }
    private WheelCollider _wheelCollider;

    /// <summary>
    /// Cached reference to the parent vehicle's Rigidbody, obtained from RCC_CarControllerV4.
    /// </summary>
    private Rigidbody Rigid {

        get {

            if (_rigid == null)
                _rigid = CarController.Rigid;

            return _rigid;

        }

    }
    private Rigidbody _rigid;

    /// <summary>
    /// The wheel model mesh transform, used for visual alignment.
    /// </summary>
    public Transform wheelModel;

    #endregion

    #region Wheel State & Geometry

    public WheelHit wheelHit;
    public bool isGrounded = false;
    public int groundIndex = 0;

    /// <summary>
    /// If true, the script aligns <c>wheelModel</c> every frame to match the WheelCollider.
    /// </summary>
    public bool alignWheel = true;

    /// <summary>
    /// If true, draws skidmarks when slip is high enough.
    /// </summary>
    public bool drawSkid = true;

    [HideInInspector] public Vector3 wheelPosition = Vector3.zero;
    [HideInInspector] public Quaternion wheelRotation = Quaternion.identity;

    // Basic wheel logic flags
    public bool canPower = false;
    [Range(-1f, 1f)] public float powerMultiplier = 1f;
    public bool canSteer = false;
    [Range(-1f, 1f)] public float steeringMultiplier = 1f;
    public bool canBrake = false;
    [Range(0f, 1f)] public float brakingMultiplier = 1f;
    public bool canHandbrake = false;
    [Range(0f, 1f)] public float handbrakeMultiplier = 1f;

    public float wheelWidth = 0.3f;
    public float wheelOffset = 0f;

    private float wheelRPM2Speed = 0f;

    /// <summary>
    /// Visual Camber, Caster, Toe angles in degrees.
    /// </summary>
    [Range(-5f, 5f)] public float camber = 0f;
    [Range(-5f, 5f)] public float caster = 0f;
    [Range(-5f, 5f)] public float toe = 0f;

    #endregion

    #region Skid & Slip

    private int lastSkidmark = -1;
    [HideInInspector] public float wheelSlipAmountForward = 0f;
    [HideInInspector] public float wheelSlipAmountSideways = 0f;
    [HideInInspector] public float totalSlip = 0f;

    #endregion

    #region Friction Curves

    private WheelFrictionCurve forwardFrictionCurve;
    private WheelFrictionCurve sidewaysFrictionCurve;
    private WheelFrictionCurve forwardFrictionCurve_Org;
    private WheelFrictionCurve sidewaysFrictionCurve_Org;

    #endregion

    #region Audio & Particles

    private AudioSource audioSource;
    private AudioClip audioClip;
    private float audioVolume = 1f;

    [HideInInspector] public List<ParticleSystem> allWheelParticles = new List<ParticleSystem>();
    private ParticleSystem.EmissionModule emission;

    #endregion

    #region Traction Helpers

    [HideInInspector] public float tractionHelpedSidewaysStiffness = 1f;

    /// <summary>
    /// A factor (0-1) to quickly reduce forward friction if needed (e.g. certain driving modes).
    /// </summary>
    [Range(0f, 1f)] public float forwardGrip = 1f;

    /// <summary>
    /// A factor (0-1) to quickly reduce sideways friction if needed (e.g. drift mode).
    /// </summary>
    [Range(0f, 1f)] public float sidewaysGrip = 1f;

    [HideInInspector] public float bumpForce, oldForce, RotationValue = 0f;

    #endregion

    #region Deflation

    private bool deflated = false;
    [Space] public float deflateRadiusMultiplier = 0.8f;
    public float deflatedStiffnessMultiplier = 0.5f;
    private float defRadius = -1f;

    public AudioClip DeflateAudio {
        get { return Settings.wheelDeflateClip; }
    }
    public AudioClip InflateAudio {
        get { return Settings.wheelInflateClip; }
    }

    private AudioSource flatSource;
    public AudioClip FlatAudio {
        get { return Settings.wheelFlatClip; }
    }

    private ParticleSystem _wheelDeflateParticles;
    public ParticleSystem WheelDeflateParticles {

        get {

            // Returns the ParticleSystem from the prefab assigned in RCC_Settings
            return Settings.wheelDeflateParticles.GetComponent<ParticleSystem>();

        }

    }

    #endregion

    #region Ackerman Steering Parameters

    [Header("Ackerman Tuning")]
    [Tooltip("Distance between front and rear axles (approx).")]
    public float ackermanWheelBase = 2.55f;

    [Tooltip("Just a reference length used in the tangent calculation.")]
    public float ackermanSteerReference = 6f;

    [Tooltip("Approx track width (distance between left and right wheels).")]
    public float ackermanTrackWidth = 1.5f;

    #endregion

    private void Awake() {

        // If using fixed WheelColliders in the RCC settings, scale wheel mass.
        if (Settings.useFixedWheelColliders)
            WheelCollider.mass = Rigid.mass / 25f;

        CreatePivotOfTheWheel();
        UpdateWheelFrictions();
        OverrideWheelSettings();
        CreateAudio();
        CreateParticles();

        // Store original
        forwardFrictionCurve_Org = WheelCollider.forwardFriction;
        sidewaysFrictionCurve_Org = WheelCollider.sidewaysFriction;

    }

    #region Initialization

    /// <summary>
    /// Creates a pivot transform around the wheel’s mesh center to ensure correct rotation alignment.
    /// </summary>
    private void CreatePivotOfTheWheel() {

        GameObject newPivot = new GameObject("Pivot_" + wheelModel.transform.name);
        newPivot.transform.position = RCC_GetBounds.GetBoundsCenter(wheelModel.transform);
        newPivot.transform.rotation = transform.rotation;
        newPivot.transform.SetParent(wheelModel.transform.parent, true);

        wheelModel.SetParent(newPivot.transform, true);
        wheelModel = newPivot.transform;

    }

    /// <summary>
    /// Creates the skid/smoke particle systems for different ground materials, storing them in <c>allWheelParticles</c>.
    /// </summary>
    private void CreateParticles() {

        if (Settings.dontUseAnyParticleEffects)
            return;

        for (int i = 0; i < GroundMaterials.frictions.Length; i++) {

            GameObject ps = Instantiate(GroundMaterials.frictions[i].groundParticles, transform.position, transform.rotation);
            emission = ps.GetComponent<ParticleSystem>().emission;
            emission.enabled = false;
            ps.transform.SetParent(transform, false);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;
            allWheelParticles.Add(ps.GetComponent<ParticleSystem>());

        }

    }

    /// <summary>
    /// Creates the AudioSource used for skid SFX.
    /// </summary>
    private void CreateAudio() {

        audioSource = NewAudioSource(Settings.audioMixer, CarController.gameObject,
            "Skid Sound AudioSource", 5f, 50f, 0f, audioClip, true, true, false);

        audioSource.transform.position = transform.position;

    }

    /// <summary>
    /// If <c>CarController.overrideAllWheels</c> is false, sets default power/steer/brake/handbrake on front/rear wheels.
    /// </summary>
    private void OverrideWheelSettings() {

        if (!CarController.overrideAllWheels) {

            if (this == CarController.FrontLeftWheelCollider || this == CarController.FrontRightWheelCollider) {

                canSteer = true;
                canBrake = true;
                brakingMultiplier = 1f;

            }

            if (this == CarController.RearLeftWheelCollider || this == CarController.RearRightWheelCollider) {

                canHandbrake = true;
                canBrake = true;
                brakingMultiplier = 0.75f;

            }

        }

    }

    private void OnEnable() {

        RCC_SceneManager.OnBehaviorChanged += UpdateWheelFrictions;

        if (wheelModel && !wheelModel.gameObject.activeSelf)
            wheelModel.gameObject.SetActive(true);

        wheelSlipAmountForward = 0f;
        wheelSlipAmountSideways = 0f;
        totalSlip = 0f;
        bumpForce = 0f;
        oldForce = 0f;

        if (audioSource) {

            audioSource.volume = 0f;
            audioSource.Stop();

        }

        WheelCollider.motorTorque = 0f;
        WheelCollider.brakeTorque = 0f;
        WheelCollider.steerAngle = 0f;

    }

    /// <summary>
    /// Updates the friction curves for this wheel based on current RCC behavior settings.
    /// </summary>
    private void UpdateWheelFrictions() {

        forwardFrictionCurve = WheelCollider.forwardFriction;
        sidewaysFrictionCurve = WheelCollider.sidewaysFriction;

        RCC_Settings.BehaviorType behavior = Settings.selectedBehaviorType;

        // If no override, load behavior friction settings
        if (!CarController.overrideBehavior && behavior != null) {

            forwardFrictionCurve = SetFrictionCurves(
                forwardFrictionCurve,
                behavior.forwardExtremumSlip,
                behavior.forwardExtremumValue,
                behavior.forwardAsymptoteSlip,
                behavior.forwardAsymptoteValue
            );

            sidewaysFrictionCurve = SetFrictionCurves(
                sidewaysFrictionCurve,
                behavior.sidewaysExtremumSlip,
                behavior.sidewaysExtremumValue,
                behavior.sidewaysAsymptoteSlip,
                behavior.sidewaysAsymptoteValue
            );

        }

        WheelCollider.forwardFriction = forwardFrictionCurve;
        WheelCollider.sidewaysFriction = sidewaysFrictionCurve;

    }

    #endregion

    private void Update() {

        if (!CarController.enabled)
            return;

        // Align wheel visuals in Update() for smooth rendering
        if (alignWheel)
            WheelAlign();

    }

    private void FixedUpdate() {

        if (!CarController.enabled)
            return;

        // Convert RPM to approximate speed (km/h)
        float circumference = 2.0f * Mathf.PI * WheelCollider.radius;

        if (Mathf.Abs(WheelCollider.rpm) > .01f)
            wheelRPM2Speed = (circumference * WheelCollider.rpm) * 60f / 1000f;
        else
            wheelRPM2Speed = 0f;

        // Automatic power distribution if overrideAllWheels = false
        if (!CarController.overrideAllWheels) {

            switch (CarController.wheelTypeChoise) {
                case RCC_CarControllerV4.WheelType.AWD:
                    canPower = true;
                    break;
                case RCC_CarControllerV4.WheelType.BIASED:
                    canPower = true;
                    break;
                case RCC_CarControllerV4.WheelType.FWD:
                    if (this == CarController.FrontLeftWheelCollider || this == CarController.FrontRightWheelCollider)
                        canPower = true;
                    else
                        canPower = false;
                    break;
                case RCC_CarControllerV4.WheelType.RWD:
                    if (this == CarController.RearLeftWheelCollider || this == CarController.RearRightWheelCollider)
                        canPower = true;
                    else
                        canPower = false;
                    break;

            }

        }

        GroundMaterial();
        Frictions();
        TotalSlip();
        SkidMarks();
        Particles();
        Audio();
        CheckDeflate();
        ESP();

    }

    #region ESP & Skid Logic

    /// <summary>
    /// Extra braking if over/understeering (ESP). Helps stabilize the car.
    /// </summary>
    private void ESP() {

        if (CarController.ESP && CarController.brakeInput < .5f) {

            if (CarController.handbrakeInput < .5f) {

                // Understeering
                if (CarController.underSteering) {

                    if (this == CarController.FrontLeftWheelCollider) {

                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) *
                            Mathf.Clamp(-CarController.rearSlip, 0f, Mathf.Infinity));
                    }

                    if (this == CarController.FrontRightWheelCollider) {

                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) *
                            Mathf.Clamp(CarController.rearSlip, 0f, Mathf.Infinity));

                    }

                }

                // Oversteering
                if (CarController.overSteering) {

                    if (this == CarController.RearLeftWheelCollider) {

                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) *
                            Mathf.Clamp(-CarController.frontSlip, 0f, Mathf.Infinity));

                    }

                    if (this == CarController.RearRightWheelCollider) {

                        ApplyBrakeTorque((CarController.brakeTorque * CarController.ESPStrength) *
                            Mathf.Clamp(CarController.frontSlip, 0f, Mathf.Infinity));

                    }

                }

            }

        }

    }

    #endregion

    #region Wheel Alignment

    /// <summary>
    /// Positions & rotates the wheelModel to match the WheelCollider's pose,
    /// plus custom spinning angle and camber/caster.
    /// </summary>
    private void WheelAlign() {

        if (!wheelModel) {

            Debug.LogWarning(transform.name + " wheel of the " + CarController.transform.name + " is missing wheel model.");
            return;

        }

        // Get actual collider pose
        WheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

        // If we want custom spinning separate from the wheel collider's own rotation:
        // (Note: WheelCollider's 'rpm' already spins wheelRotation, but if you want to do your own spin, you can do so.)
        RotationValue += WheelCollider.rpm * (360f / 60f) * Time.deltaTime;

        // Combine the collider's rotation with custom spin on the local X axis
        Quaternion combinedRot = transform.rotation * Quaternion.Euler(RotationValue, WheelCollider.steerAngle, 0f);

        // Set final position & rotation
        wheelModel.SetPositionAndRotation(wheelPosition, combinedRot);

        // Apply camber (tilt around forward axis)
        if (transform.localPosition.x < 0f)  // Left side
            wheelModel.RotateAround(wheelModel.position, transform.forward, -camber);
        else  // Right side
            wheelModel.RotateAround(wheelModel.position, transform.forward, camber);

        // Apply caster (tilt around right axis)
        if (transform.localPosition.x < 0f)
            wheelModel.RotateAround(wheelModel.position, transform.right, -caster);
        else
            wheelModel.RotateAround(wheelModel.position, transform.right, caster);

    }

    #endregion

    #region Skidmarks

    private void SkidMarks() {

        if (!drawSkid)
            return;

        if (!Settings.dontUseSkidmarks) {

            if (totalSlip > GroundMaterials.frictions[groundIndex].slip) {

                Vector3 skidPoint = wheelHit.point + 1f * Time.deltaTime * (Rigid.linearVelocity);

                if (Rigid.linearVelocity.magnitude > 1f && isGrounded && wheelHit.normal != Vector3.zero &&
                    wheelHit.point != Vector3.zero && skidPoint != Vector3.zero &&
                    Mathf.Abs(skidPoint.x) > 1f && Mathf.Abs(skidPoint.z) > 1f) {

                    lastSkidmark = RCC_SkidmarksManager.Instance.AddSkidMark(
                        skidPoint, wheelHit.normal, totalSlip, wheelWidth, lastSkidmark, groundIndex
                    );

                } else {

                    lastSkidmark = -1;

                }

            } else {

                lastSkidmark = -1;

            }

        }

    }

    #endregion

    #region Friction

    /// <summary>
    /// Applies friction based on ground material, handbrake, and deflation. Then calls drift logic if enabled.
    /// </summary>
    private void Frictions() {

        float hbInput = CarController.handbrakeInput;

        if (canHandbrake && hbInput > .75f)
            hbInput = .75f;
        else
            hbInput = 1f;

        forwardFrictionCurve.stiffness = GroundMaterials.frictions[groundIndex].forwardStiffness * forwardGrip;
        sidewaysFrictionCurve.stiffness = (GroundMaterials.frictions[groundIndex].sidewaysStiffness * hbInput * tractionHelpedSidewaysStiffness) * sidewaysGrip;

        // Deflated wheels have reduced friction
        if (deflated) {

            forwardFrictionCurve.stiffness *= deflatedStiffnessMultiplier;
            sidewaysFrictionCurve.stiffness *= deflatedStiffnessMultiplier;

        }

        CarController.driftMode = false;

        // If drifting is allowed by the behavior settings, apply drift logic
        if (!CarController.overrideBehavior && Settings.selectedBehaviorType != null && Settings.selectedBehaviorType.applyExternalWheelFrictions)
            Drift();

        WheelCollider.forwardFriction = forwardFrictionCurve;
        WheelCollider.sidewaysFriction = sidewaysFrictionCurve;

        WheelCollider.wheelDampingRate = GroundMaterials.frictions[groundIndex].damp * (1f - CarController.throttleInput);

    }

    private void TotalSlip() {

        if (isGrounded && wheelHit.point != Vector3.zero) {

            wheelSlipAmountForward = wheelHit.forwardSlip;
            wheelSlipAmountSideways = wheelHit.sidewaysSlip;

        } else {

            wheelSlipAmountForward = 0f;
            wheelSlipAmountSideways = 0f;

        }

        totalSlip = Mathf.Lerp(
            totalSlip,
            (Mathf.Abs(wheelSlipAmountSideways) + Mathf.Abs(wheelSlipAmountForward)) / 2f,
            Time.fixedDeltaTime * 10f
        );

    }

    /// <summary>
    /// Dynamically modifies friction curves if the vehicle is drifting/slipping.
    /// Also simulates a small powerslide effect with RWD.
    /// </summary>
    private void Drift() {

        CarController.driftMode = true;

        // You may further refine your drift logic here.
        Vector3 relativeVelocity = transform.InverseTransformDirection(Rigid.linearVelocity);

        float lateralSlip = Mathf.Abs(wheelHit.sidewaysSlip);
        float forwardSlip = Mathf.Abs(wheelHit.forwardSlip);

        // If RWD, also require throttle
        bool isRearWheel = (WheelCollider == CarController.RearLeftWheelCollider.WheelCollider ||
                            WheelCollider == CarController.RearRightWheelCollider.WheelCollider);

        // Compute drift factor
        float driftFactor = Mathf.Clamp01(lateralSlip + forwardSlip);

        // Reduce grip while drifting
        float sidewaysReduction = Mathf.Lerp(1f, 0.65f, driftFactor);
        float forwardReduction = Mathf.Lerp(1f, 0.75f, driftFactor);

        if (!isRearWheel)
            forwardReduction *= Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(lateralSlip));

        // Lerp to new friction values
        sidewaysFrictionCurve.extremumValue = Mathf.Lerp(
            sidewaysFrictionCurve.extremumValue,
            sidewaysFrictionCurve_Org.extremumValue * sidewaysReduction,
            Time.fixedDeltaTime * 15f
        );
        sidewaysFrictionCurve.asymptoteValue = Mathf.Lerp(
            sidewaysFrictionCurve.asymptoteValue,
            sidewaysFrictionCurve_Org.asymptoteValue * sidewaysReduction,
            Time.fixedDeltaTime * 15f
        );

        forwardFrictionCurve.extremumValue = Mathf.Lerp(
            forwardFrictionCurve.extremumValue,
            forwardFrictionCurve_Org.extremumValue * forwardReduction,
            Time.fixedDeltaTime * 15f
        );
        forwardFrictionCurve.asymptoteValue = Mathf.Lerp(
            forwardFrictionCurve.asymptoteValue,
            forwardFrictionCurve_Org.asymptoteValue * forwardReduction,
            Time.fixedDeltaTime * 15f
        );

        // Powerslide effect for RWD
        if (isRearWheel && CarController.wheelTypeChoise == RCC_CarControllerV4.WheelType.RWD) {

            if (Mathf.Abs(CarController.throttleInput) >= 0.8f && lateralSlip >= 0.3f) {

                float powerslideForce = lateralSlip * 600f;  // Tweak for balance

                // Small sideways push at the wheel
                Rigid.AddForceAtPosition(
                    transform.right * powerslideForce * Mathf.Sign(relativeVelocity.x) * 0.35f,
                    transform.position, ForceMode.Force
                );

                // Additional force at center of mass
                Rigid.AddForceAtPosition(
                    transform.right * powerslideForce * Mathf.Sign(relativeVelocity.x),
                    CarController.COM.position, ForceMode.Force
                );

            }

        }

    }

    #endregion

    #region Particles & Audio

    /// <summary>
    /// Enables or disables skid particle systems based on slip and ground material.
    /// </summary>
    private void Particles() {

        if (Settings.dontUseAnyParticleEffects)
            return;

        for (int i = 0; i < allWheelParticles.Count; i++) {
            ParticleSystem.EmissionModule em = allWheelParticles[i].emission;

            if (totalSlip > GroundMaterials.frictions[groundIndex].slip) {

                if (i == groundIndex) {

                    em.enabled = true;

                } else {

                    em.enabled = false;

                }

            } else {

                em.enabled = false;

            }

            if (isGrounded && wheelHit.point != Vector3.zero)
                allWheelParticles[i].transform.position = wheelHit.point + (0.05f * transform.up);

        }

    }

    /// <summary>
    /// Plays/stops the skid sound, adjusting volume/pitch with slip. Also detects bumps.
    /// </summary>
    private void Audio() {

        audioClip = GroundMaterials.frictions[groundIndex].groundSound;
        audioVolume = GroundMaterials.frictions[groundIndex].volume;

        if (totalSlip > GroundMaterials.frictions[groundIndex].slip) {

            if (audioSource.clip != audioClip)
                audioSource.clip = audioClip;

            if (!audioSource.isPlaying)
                audioSource.Play();

            if (Rigid.linearVelocity.magnitude > 1f) {

                audioSource.volume = Mathf.Lerp(audioSource.volume, Mathf.Lerp(0f, audioVolume, totalSlip), Time.deltaTime * 5f);
                audioSource.pitch = Mathf.Lerp(audioSource.pitch, Mathf.Lerp(.9f, 1.1f, totalSlip), Time.deltaTime * 5f);

            } else {

                audioSource.volume = Mathf.Lerp(audioSource.volume, 0f, Time.deltaTime * 5f);

            }

        } else {

            audioSource.volume = Mathf.Lerp(audioSource.volume, 0f, Time.deltaTime * 5f);

            if (audioSource.volume <= 0.05f && audioSource.isPlaying)
                audioSource.Stop();

        }

        // Bump detection
        bumpForce = wheelHit.force - oldForce;

        if (bumpForce >= 5000f) {

            AudioSource bumpSound = NewAudioSource(
                Settings.audioMixer, CarController.gameObject,
                "Bump Sound AudioSource", 5f, 50f,
                (bumpForce - 5000f) / 3000f,
                Settings.bumpClip,
                false, true, true
            );

            bumpSound.pitch = Random.Range(0.95f, 1.05f);

        }

        oldForce = wheelHit.force;

    }

    /// <summary>
    /// Checks if ANY wheel on this CarController is skidding.
    /// </summary>
    public bool IsSkidding() {

        // Example of "any wheel" skidding logic:
        for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

            var otherWheel = CarController.AllWheelColliders[i];
            int otherGroundIndex = otherWheel.groundIndex;

            if (otherWheel.totalSlip > GroundMaterials.frictions[otherGroundIndex].slip)
                return true;

        }

        return false;

    }

    #endregion

    #region Wheel Input & Logic

    /// <summary>
    /// Applies engine torque to this wheel, factoring in TCS if enabled.
    /// </summary>
    public void ApplyMotorTorque(float torque) {

        if (CarController.TCS) {

            // Basic TCS logic
            if (Mathf.Abs(WheelCollider.rpm) >= 1) {

                if (Mathf.Abs(wheelSlipAmountForward) > GroundMaterials.frictions[groundIndex].slip) {

                    CarController.TCSAct = true;
                    torque -= Mathf.Clamp(
                        torque * (Mathf.Abs(wheelSlipAmountForward)) * CarController.TCSStrength,
                        -Mathf.Infinity, Mathf.Infinity
                    );

                    if (WheelCollider.rpm > 1) {

                        torque -= Mathf.Clamp(
                            torque * (Mathf.Abs(wheelSlipAmountForward)) * CarController.TCSStrength,
                            0f, Mathf.Infinity
                        );
                        torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

                    } else {

                        torque += Mathf.Clamp(
                            -torque * (Mathf.Abs(wheelSlipAmountForward)) * CarController.TCSStrength,
                            0f, Mathf.Infinity
                        );
                        torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

                    }

                } else {

                    CarController.TCSAct = false;

                }

            } else {

                CarController.TCSAct = false;

            }

        }

        if (CheckOvertorque())
            torque = 0;

        if (Mathf.Abs(torque) > 1f)
            WheelCollider.motorTorque = torque;
        else
            WheelCollider.motorTorque = 0f;

    }

    /// <summary>
    /// Applies a steering angle (including a simple Ackerman approach).
    /// </summary>
    public void ApplySteering(float steerInput, float angle) {

        // Basic Ackerman formula with user-exposed parameters
        // steerInput is in [-1, 1], angle is the max angle

        float finalAngle = 0f;

        if (Mathf.Abs(steerInput) > Mathf.Epsilon) {

            // left wheel
            if (transform.localPosition.x < 0f) {

                // Example formula for left wheel
                finalAngle = (Mathf.Deg2Rad * angle * ackermanWheelBase) *
                    (Mathf.Rad2Deg * Mathf.Atan(
                        ackermanWheelBase / (ackermanSteerReference + (ackermanTrackWidth / 2f))
                    ) * steerInput);

            }
            // right wheel
            else {

                finalAngle = (Mathf.Deg2Rad * angle * ackermanWheelBase) *
                    (Mathf.Rad2Deg * Mathf.Atan(
                        ackermanWheelBase / (ackermanSteerReference - (ackermanTrackWidth / 2f))
                    ) * steerInput);

            }

        }

        WheelCollider.steerAngle = finalAngle;

        // Apply toe offset
        if (transform.localPosition.x < 0)
            WheelCollider.steerAngle += toe;
        else
            WheelCollider.steerAngle -= toe;

    }

    /// <summary>
    /// Applies brake torque, factoring in ABS if enabled.
    /// </summary>
    public void ApplyBrakeTorque(float torque) {

        if (CarController.ABS && CarController.handbrakeInput <= .1f) {

            if ((Mathf.Abs(wheelHit.forwardSlip) * Mathf.Clamp01(torque)) >= CarController.ABSThreshold) {

                CarController.ABSAct = true;
                torque = 0;

            } else {

                CarController.ABSAct = false;

            }

        }

        if (Mathf.Abs(torque) > 1f)
            WheelCollider.brakeTorque = torque;
        else
            WheelCollider.brakeTorque = 0f;

    }

    /// <summary>
    /// Checks if torque should be zeroed out (engine off, speed limit, etc.).
    /// </summary>
    private bool CheckOvertorque() {

        if (!CarController.engineRunning)
            return true;

        if (CarController.limitMaxSpeed && CarController.speed >= CarController.limitMaxSpeedAt)
            return true;

        if (CarController.speed >= CarController.maxspeed)
            return true;

        if (Mathf.Abs(wheelRPM2Speed) >= CarController.gears[CarController.currentGear].maxSpeed)
            return true;

        if (CarController.currentGear == 0 && CarController.direction == -1 && Mathf.Abs(wheelRPM2Speed) >= CarController.gears[CarController.currentGear].maxSpeed * .65f)
            return true;

        return false;

    }

    #endregion

    #region Ground Material & Terrain

    /// <summary>
    /// Determines current ground surface index by checking shared PhysicMaterial or terrain splatmap.
    /// </summary>
    private void GroundMaterial() {

        isGrounded = WheelCollider.GetGroundHit(out wheelHit);

        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null) {

            groundIndex = 0;
            return;

        }

        // Check known ground materials
        for (int i = 0; i < GroundMaterials.frictions.Length; i++) {

            if (wheelHit.collider.sharedMaterial == GroundMaterials.frictions[i].groundMaterial) {

                groundIndex = i;
                return;

            }

        }

        // Check terrain if enabled
        if (!RCCSceneManager.terrainsInitialized) {

            groundIndex = 0;
            return;

        }

        for (int i = 0; i < GroundMaterials.terrainFrictions.Length; i++) {

            if (wheelHit.collider.sharedMaterial == GroundMaterials.terrainFrictions[i].groundMaterial) {

                RCC_SceneManager.Terrains currentTerrain = null;

                for (int l = 0; l < RCCSceneManager.terrains.Length; l++) {

                    if (RCCSceneManager.terrains[l].terrainCollider == GroundMaterials.terrainFrictions[i].groundMaterial) {

                        currentTerrain = RCCSceneManager.terrains[l];
                        break;

                    }

                }

                if (currentTerrain != null) {

                    Vector3 playerPos = transform.position;
                    Vector3 TerrainCord = ConvertToSplatMapCoordinate(currentTerrain.terrain, playerPos);
                    float comp = 0f;
                    int bestIndex = 0;

                    // Find strongest texture
                    for (int k = 0; k < currentTerrain.mNumTextures; k++) {

                        float val = currentTerrain.mSplatmapData[(int)TerrainCord.z, (int)TerrainCord.x, k];

                        if (val > comp) {

                            comp = val;
                            bestIndex = k;

                        }

                    }

                    groundIndex = GroundMaterials.terrainFrictions[i].splatmapIndexes[bestIndex].index;
                    return;

                }

            }

        }

        // Default to index 0 if none matched
        groundIndex = 0;

    }

    /// <summary>
    /// Converts position to terrain splatmap coordinate.
    /// </summary>
    private Vector3 ConvertToSplatMapCoordinate(Terrain terrain, Vector3 playerPos) {

        Vector3 terPosition = terrain.transform.position;
        Vector3 coord = new Vector3();
        coord.x = ((playerPos.x - terPosition.x) / terrain.terrainData.size.x) * terrain.terrainData.alphamapWidth;
        coord.z = ((playerPos.z - terPosition.z) / terrain.terrainData.size.z) * terrain.terrainData.alphamapHeight;
        return coord;

    }

    #endregion

    #region Deflation

    /// <summary>
    /// Checks if friction indicates a deflation scenario, handles deflated audio/particles, etc.
    /// </summary>
    private void CheckDeflate() {

        if (deflated) {

            if (!flatSource)
                flatSource = NewAudioSource(gameObject, FlatAudio.name, 1f, 15f, 0.5f, FlatAudio, true, false, false);

            flatSource.volume = Mathf.Clamp01(Mathf.Abs(WheelCollider.rpm * 0.001f));
            flatSource.volume *= isGrounded ? 1f : 0f;

            if (!flatSource.isPlaying)
                flatSource.Play();
        } else {

            if (flatSource && flatSource.isPlaying)
                flatSource.Stop();

        }

        if (_wheelDeflateParticles != null) {

            ParticleSystem.EmissionModule em = _wheelDeflateParticles.emission;
            if (deflated) {

                if (Mathf.Abs(WheelCollider.rpm) > 100f && isGrounded)
                    em.enabled = true;
                else
                    em.enabled = false;

            } else {

                em.enabled = false;

            }

        }

        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null)
            return;

        // Check if ground material wants to deflate
        for (int i = 0; i < GroundMaterials.frictions.Length; i++) {

            if (wheelHit.collider.sharedMaterial == GroundMaterials.frictions[i].groundMaterial) {

                if (GroundMaterials.frictions[i].deflate)
                    Deflate();

            }

        }

    }

    /// <summary>
    /// Deflates the tire, reducing radius and stiffness. Plays deflate audio & spawns deflate particles.
    /// </summary>
    public void Deflate() {

        if (deflated)
            return;

        deflated = true;

        if (defRadius == -1)
            defRadius = WheelCollider.radius;

        WheelCollider.radius = defRadius * deflateRadiusMultiplier;

        if (DeflateAudio)
            NewAudioSource(gameObject, DeflateAudio.name, 5f, 50f, 1f, DeflateAudio, false, true, true);

        if (_wheelDeflateParticles == null && WheelDeflateParticles) {

            GameObject ps = Instantiate(WheelDeflateParticles.gameObject, transform.position, transform.rotation);
            _wheelDeflateParticles = ps.GetComponent<ParticleSystem>();
            _wheelDeflateParticles.transform.SetParent(transform, false);
            _wheelDeflateParticles.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            _wheelDeflateParticles.transform.localRotation = Quaternion.identity;

        }

        // Small random lateral push
        CarController.Rigid.AddForceAtPosition(
            transform.right * Random.Range(-1f, 1f) * 30f,
            transform.position,
            ForceMode.Acceleration
        );

    }

    /// <summary>
    /// Restores tire to original radius & friction if previously deflated.
    /// </summary>
    public void Inflate() {

        if (!deflated)
            return;

        deflated = false;

        if (defRadius != -1)
            WheelCollider.radius = defRadius;

        if (InflateAudio)
            NewAudioSource(gameObject, InflateAudio.name, 5f, 50f, 1f, InflateAudio, false, true, true);

    }

    #endregion

    #region Friction Utility

    /// <summary>
    /// Creates a new friction curve from given slip/value parameters.
    /// </summary>
    public WheelFrictionCurve SetFrictionCurves(
        WheelFrictionCurve curve,
        float extremumSlip,
        float extremumValue,
        float asymptoteSlip,
        float asymptoteValue
    ) {

        WheelFrictionCurve newCurve = curve;
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;

        return newCurve;

    }

    #endregion

    private void OnDisable() {

        RCC_SceneManager.OnBehaviorChanged -= UpdateWheelFrictions;

        if (wheelModel)
            wheelModel.gameObject.SetActive(false);

        wheelSlipAmountForward = 0f;
        wheelSlipAmountSideways = 0f;
        totalSlip = 0f;
        bumpForce = 0f;
        oldForce = 0f;

        if (audioSource) {

            audioSource.volume = 0f;
            audioSource.Stop();

        }

        WheelCollider.motorTorque = 0f;
        WheelCollider.brakeTorque = 0f;
        WheelCollider.steerAngle = 0f;

    }

    /// <summary>
    /// Draw debug info in Editor, visualizing forces and slips.
    /// </summary>
    private void OnDrawGizmos() {

#if UNITY_EDITOR
        if (Application.isPlaying) {

            WheelCollider.GetGroundHit(out WheelHit hit);
            float extension = (
                -WheelCollider.transform.InverseTransformPoint(hit.point).y
                - (WheelCollider.radius * transform.lossyScale.y)
            ) / WheelCollider.suspensionDistance;

            Debug.DrawLine(
                hit.point,
                hit.point + transform.up * (hit.force / Rigid.mass),
                extension <= 0.0f ? Color.magenta : Color.white
            );

            Debug.DrawLine(
                hit.point,
                hit.point - transform.forward * hit.forwardSlip * 2f,
                Color.green
            );
            Debug.DrawLine(
                hit.point,
                hit.point - transform.right * hit.sidewaysSlip * 2f,
                Color.red
            );

        }

#endif
    }

}
