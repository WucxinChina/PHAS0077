//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
/// <summary>
/// The main vehicle controller script for RCC, providing comprehensive functionality:
/// - Wheels & Suspensions
/// - Steering & Drivetrain
/// - Engine & Gear System
/// - Braking & Stability Assistances
/// - Visual Effects & Audio
/// - Damage & Collision Handling
/// </summary>
public class RCC_CarControllerV4 : RCC_Core {

    /// <summary>
    /// Determines if this vehicle can receive standard inputs from RCC_InputManager.
    /// When false, throttle, brake, and steering inputs from the player are ignored.
    /// </summary>
    public bool canControl = true;

    /// <summary>
    /// Indicates whether the vehicle is fully grounded (at least one wheel on the ground).
    /// </summary>
    public bool isGrounded = false;

    /// <summary>
    /// If true, the vehicle will not rely on the selected behavior in RCC Settings, 
    /// instead maintaining its own configuration for handling and assistance.
    /// </summary>
    public bool overrideBehavior = false;

    /// <summary>
    /// If set to true, this vehicle does not take inputs from RCC_InputManager, 
    /// instead using inputs provided by OverrideInputs().
    /// </summary>
    public bool overrideInputs = false;

    #region Wheels
    //--------------------------------------------------------------------------------
    //                                   WHEELS
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Front left wheel's mesh transform.
    /// </summary>
    public Transform FrontLeftWheelTransform;

    /// <summary>
    /// Front right wheel's mesh transform.
    /// </summary>
    public Transform FrontRightWheelTransform;

    /// <summary>
    /// Rear left wheel's mesh transform.
    /// </summary>
    public Transform RearLeftWheelTransform;

    /// <summary>
    /// Rear right wheel's mesh transform.
    /// </summary>
    public Transform RearRightWheelTransform;

    /// <summary>
    /// Extra wheel transforms (if the vehicle has more than 4 wheels).
    /// </summary>
    public Transform[] ExtraRearWheelsTransform;

    /// <summary>
    /// Front left wheel collider and relevant data.
    /// </summary>
    public RCC_WheelCollider FrontLeftWheelCollider;

    /// <summary>
    /// Front right wheel collider and relevant data.
    /// </summary>
    public RCC_WheelCollider FrontRightWheelCollider;

    /// <summary>
    /// Rear left wheel collider and relevant data.
    /// </summary>
    public RCC_WheelCollider RearLeftWheelCollider;

    /// <summary>
    /// Rear right wheel collider and relevant data.
    /// </summary>
    public RCC_WheelCollider RearRightWheelCollider;

    /// <summary>
    /// Extra wheel colliders (if the vehicle has more than 4 wheels).
    /// </summary>
    public RCC_WheelCollider[] ExtraRearWheelsCollider;

    /// <summary>
    /// Quick access to all wheel colliders on this vehicle. If uninitialized, it finds them.
    /// </summary>
    public RCC_WheelCollider[] AllWheelColliders {

        get {

            if (_allWheelColliders == null || _allWheelColliders.Length <= 0)
                _allWheelColliders = GetComponentsInChildren<RCC_WheelCollider>(true);

            return _allWheelColliders;

        }

    }
    private RCC_WheelCollider[] _allWheelColliders;

    /// <summary>
    /// Quick access to all RCC_Light components in children (head, brake, indicator, etc.).
    /// </summary>
    public RCC_Light[] AllLights {

        get {

            if (_allLights == null || _allLights.Length <= 0)
                _allLights = GetComponentsInChildren<RCC_Light>(true);

            return _allLights;

        }

    }
    private RCC_Light[] _allLights;

    /// <summary>
    /// Indicates if this vehicle physically has extra wheels beyond the standard 4.
    /// </summary>
    public bool hasExtraWheels = false;

    /// <summary>
    /// If true, overrides each wheel's individual settings (steer, power, brake, etc.) with the vehicle-wide setting.
    /// </summary>
    public bool overrideAllWheels = false;

    /// <summary>
    /// Counts how many wheels are actually powered. Used to divide total torque among them.
    /// </summary>
    public int poweredWheels = 0;
    #endregion

    #region SteeringWheel
    //--------------------------------------------------------------------------------
    //                             STEERING WHEEL MODEL
    //--------------------------------------------------------------------------------

    /// <summary>
    /// The 3D steering wheel mesh transform within the interior.
    /// </summary>
    public Transform SteeringWheel;

    /// <summary>
    /// Stores the steering wheel model's default (original) local rotation.
    /// </summary>
    private Quaternion orgSteeringWheelRot = Quaternion.identity;

    /// <summary>
    /// Specifies the axis around which the steering wheel model will rotate.
    /// </summary>
    public SteeringWheelRotateAround steeringWheelRotateAround = SteeringWheelRotateAround.ZAxis;

    /// <summary>
    /// Determines which axis the steering wheel will pivot around.
    /// </summary>
    public enum SteeringWheelRotateAround { XAxis, YAxis, ZAxis }

    /// <summary>
    /// A multiplier controlling how much the steering wheel meshes rotate relative to actual steering angle.
    /// </summary>
    public float steeringWheelAngleMultiplier = 11f;
    #endregion

    #region Drivetrain Type
    //--------------------------------------------------------------------------------
    //                            DRIVETRAIN SETTINGS
    //--------------------------------------------------------------------------------

    /// <summary>
    /// The drivetrain type of the vehicle (FWD, RWD, AWD, or BIASED).
    /// </summary>
    public WheelType wheelTypeChoise = WheelType.RWD;

    /// <summary>
    /// Determines how torque is distributed among the wheels.
    /// </summary>
    public enum WheelType { FWD, RWD, AWD, BIASED }
    #endregion

    #region AI
    //--------------------------------------------------------------------------------
    //                            AI / EXTERNAL CONTROLLER
    //--------------------------------------------------------------------------------

    /// <summary>
    /// If true, no standard input is applied—vehicle's throttle/brake/steer may be set externally (e.g., by an AI script).
    /// </summary>
    public bool externalController = false;
    #endregion

    #region Steering
    //--------------------------------------------------------------------------------
    //                          STEERING CONFIGURATION
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Determines how the vehicle's steering angle is calculated (Curve-based, Simple, or Constant).
    /// </summary>
    public enum SteeringType { Curve, Simple, Constant }

    /// <summary>
    /// Specifies which method is used to limit/adjust steering angle.
    /// </summary>
    public SteeringType steeringType = SteeringType.Curve;

    /// <summary>
    /// When steeringType is Curve, the actual steering angle is looked up from this curve as speed changes.
    /// </summary>
    public AnimationCurve steerAngleCurve = new AnimationCurve(new Keyframe(0f, 40f, 0f, -.3f), new Keyframe(120f, 10f, -.115f, -.1f), new Keyframe(200f, 7f));

    /// <summary>
    /// Maximum steer angle used when speed is below the specified threshold or if using constant steering.
    /// </summary>
    public float steerAngle = 40f;

    /// <summary>
    /// Steering angle used at higher speeds, limiting turn radius to avoid unrealistic oversteer.
    /// </summary>
    public float highspeedsteerAngle = 5f;

    /// <summary>
    /// The speed (km/h) at which steer angle is fully clamped to highspeedsteerAngle.
    /// </summary>
    public float highspeedsteerAngleAtspeed = 120f;

    /// <summary>
    /// Anti-roll horizontal force applied on the front wheels for stability.
    /// </summary>
    public float antiRollFrontHorizontal = 1000f;

    /// <summary>
    /// Anti-roll horizontal force applied on the rear wheels for stability.
    /// </summary>
    public float antiRollRearHorizontal = 1000f;

    /// <summary>
    /// An additional vertical anti-roll factor. Helps reduce flipping in high-COM vehicles (e.g. monster trucks).
    /// </summary>
    public float antiRollVertical = 0f;
    #endregion

    #region Configurations
    //--------------------------------------------------------------------------------
    //                      VEHICLE CONFIGURATION & RIGIDBODY
    //--------------------------------------------------------------------------------

    /// <summary>
    /// A direct reference to the rigidbody component.
    /// </summary>
    public Rigidbody Rigid {
        get {
            if (!_rigid)
                _rigid = GetComponent<Rigidbody>();
            return _rigid;
        }
    }
    private Rigidbody _rigid;

    /// <summary>
    /// A transform used to define the vehicle's center of mass (COM).
    /// </summary>
    public Transform COM;

    /// <summary>
    /// Adjusts the center of mass at runtime based on how the vehicle angles horizontally. 
    /// Options: Off, Slight, Medium, Opposite.
    /// </summary>
    public enum COMAssisterTypes { Off, Slight, Medium, Opposite }
    public COMAssisterTypes COMAssister = COMAssisterTypes.Off;

    /// <summary>
    /// Maximum brake torque for each wheel. The total braking force is distributed among braked wheels.
    /// </summary>
    public float brakeTorque = 2000f;

    /// <summary>
    /// A downforce coefficient multiplied by speed to keep the vehicle stable at high velocities.
    /// </summary>
    public float downForce = 25f;

    /// <summary>
    /// The current speed of the vehicle, updated in FixedUpdate (km/h or mp/h).
    /// </summary>
    public float speed = 0f;

    /// <summary>
    /// The top speed of the vehicle calculated by engine rpm --> gear ratio --> final drive ratio.
    /// </summary>
    public float maxspeed = 240f;

    /// <summary>
    /// Limits the maximum speed of the vehicle.
    /// </summary>
    public bool limitMaxSpeed = false;

    /// <summary>
    /// Limits the maximum speed of the vehicle at this speed.
    /// </summary>
    public float limitMaxSpeedAt = 400f;

    /// <summary>
    /// Tracks how long the vehicle is nearly stationary while upside down, used to auto-reset.
    /// </summary>
    private float resetTime = 0f;
    #endregion

    #region Engine
    //--------------------------------------------------------------------------------
    //                            ENGINE & RPM
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Defines the engine torque output as a function of RPM.
    /// </summary>
    public AnimationCurve engineTorqueCurve = new AnimationCurve();

    /// <summary>
    /// If true, an engine torque curve is automatically regenerated each time certain engine values change (e.g., RPM range).
    /// </summary>
    public bool autoGenerateEngineRPMCurve = true;

    /// <summary>
    /// Peak engine torque (Nm) at the best RPM (maxEngineTorqueAtRPM).
    /// </summary>
    public float maxEngineTorque = 300f;

    /// <summary>
    /// The RPM value at which the engine reaches maxEngineTorque.
    /// </summary>
    public float maxEngineTorqueAtRPM = 5500f;

    /// <summary>
    /// The minimum idle RPM of the engine.
    /// </summary>
    public float minEngineRPM = 800f;

    /// <summary>
    /// The maximum allowable RPM for the engine (including rev limit).
    /// </summary>
    public float maxEngineRPM = 7000f;

    /// <summary>
    /// The smoothed, final engine RPM used in calculations and audio.
    /// </summary>
    public float engineRPM = 0f;

    /// <summary>
    /// A raw, interim RPM value used before smoothing.
    /// </summary>
    public float engineRPMRaw = 0f;

    /// <summary>
    /// The engine's inertia factor. Lower values cause quicker RPM changes.
    /// </summary>
    [Range(.02f, .4f)] public float engineInertia = .15f;

    /// <summary>
    /// If true, cutting throttle at maxEngineRPM to prevent exceeding the limit.
    /// </summary>
    public bool useRevLimiter = true;

    /// <summary>
    /// If true, the vehicle's exhaust can briefly flame when the throttle is released at certain high RPM.
    /// </summary>
    public bool useExhaustFlame = true;

    /// <summary>
    /// Determines if the engine should be automatically started at Awake. 
    /// If externalController is active, this is ignored.
    /// </summary>
    public bool RunEngineAtAwake = true;

    /// <summary>
    /// Tracks whether the engine is running. Stopped engines won't produce torque or consume fuel.
    /// </summary>
    public bool engineRunning = false;

    // Cached old engine values to detect changes that require curve recreation.
    private float oldEngineTorque = -1f;
    private float oldMaxTorqueAtRPM = -1f;
    private float oldMinEngineRPM = -1f;
    private float oldMaxEngineRPM = -1f;
    #endregion

    #region Steering Assistance
    //--------------------------------------------------------------------------------
    //                      STEERING ASSISTANCE FEATURES
    //--------------------------------------------------------------------------------

    /// <summary>
    /// If true, the max steer angle is reduced when the vehicle slides. 
    /// This helps maintain control at high slip angles.
    /// </summary>
    public bool useSteeringLimiter = true;

    /// <summary>
    /// If true, automatically applies a measure of counter-steering when drifting.
    /// </summary>
    public bool useCounterSteering = true;

    /// <summary>
    /// If true, steering changes are smoothed over time (steering sensitivity).
    /// </summary>
    public bool useSteeringSensitivity = true;

    /// <summary>
    /// A multiplier adjusting how aggressively the vehicle counter-steers during drifts.
    /// </summary>
    [Range(0f, 1f)] public float counterSteeringFactor = .5f;

    /// <summary>
    /// A factor controlling how sensitively the vehicle responds to steering input changes.
    /// Higher values = more immediate steering changes.
    /// </summary>
    [Range(.05f, 1f)] public float steeringSensitivityFactor = 1f;

    /// <summary>
    /// The base (original) steering angle. Used as a reference for dynamic steering angle changes.
    /// </summary>
    private float orgSteerAngle = 0f;

    /// <summary>
    /// Stores the previous frame's steering input for damping or limiter logic.
    /// </summary>
    public float oldSteeringInput = 0f;

    /// <summary>
    /// The difference between the current steering input and oldSteeringInput (for sensitivity or limiter logic).
    /// </summary>
    public float steeringDifference = 0f;
    #endregion

    #region Fuel
    //--------------------------------------------------------------------------------
    //                               FUEL SYSTEM
    //--------------------------------------------------------------------------------

    /// <summary>
    /// If true, fuel consumption is tracked and the vehicle can run out of fuel.
    /// </summary>
    public bool useFuelConsumption = false;

    /// <summary>
    /// The maximum capacity of the fuel tank (liters).
    /// </summary>
    public float fuelTankCapacity = 62f;

    /// <summary>
    /// The current amount of fuel. Reduces as you drive if useFuelConsumption = true.
    /// </summary>
    public float fuelTank = 62f;

    /// <summary>
    /// Rate at which the vehicle consumes fuel over time. 
    /// Typically multiplied by engine load or RPM in FixedUpdate.
    /// </summary>
    public float fuelConsumptionRate = .1f;
    #endregion

    #region Heat
    //--------------------------------------------------------------------------------
    //                            ENGINE HEAT
    //--------------------------------------------------------------------------------

    /// <summary>
    /// If true, the engine accumulates heat as it runs. The engine can partially cool at certain thresholds.
    /// </summary>
    public bool useEngineHeat = false;

    /// <summary>
    /// Current temperature of the engine (°C).
    /// </summary>
    public float engineHeat = 15f;

    /// <summary>
    /// Temperature threshold at which cooling water starts cooling the engine.
    /// </summary>
    public float engineCoolingWaterThreshold = 90f;

    /// <summary>
    /// Speed at which engine heat increases under load.
    /// </summary>
    public float engineHeatRate = 1f;

    /// <summary>
    /// Speed at which the engine cools when above engineCoolingWaterThreshold.
    /// </summary>
    public float engineCoolRate = 1f;
    #endregion

    #region Gears
    //--------------------------------------------------------------------------------
    //                            GEARBOX
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Represents a single gear's ratio and speed thresholds.
    /// </summary>
    [System.Serializable]
    public class Gear {

        public float maxRatio;
        public int maxSpeed;
        public int targetSpeedForNextGear;

        public void SetGear(float ratio, int speed, int targetSpeed) {

            maxRatio = ratio;
            maxSpeed = speed;
            targetSpeedForNextGear = targetSpeed;

        }

    }

    /// <summary>
    /// An array of gears describing the final gear ratios and shift points.
    /// </summary>
    public Gear[] gears = null;

    /// <summary>
    /// How many forward gears the vehicle has (not counting reverse).
    /// </summary>
    public int totalGears = 6;

    /// <summary>
    /// The currently active gear index. 0-based for forward gears.
    /// </summary>
    public int currentGear = 0;

    /// <summary>
    /// If true, the vehicle is in neutral gear (no power to wheels).
    /// </summary>
    public bool NGear = false;

    /// <summary>
    /// The final drive ratio that multiplies with the gear ratio to get total ratio.
    /// </summary>
    public float finalRatio = 3.23f;

    /// <summary>
    /// Delay (seconds) to fully engage a new gear. 
    /// During shifting, throttle is cut briefly to simulate realistic gear changes.
    /// </summary>
    [Range(0f, .5f)] public float gearShiftingDelay = .35f;

    /// <summary>
    /// The portion of max RPM at which automatic gear shifts up. 
    /// 1 = shift at max RPM, 0.75 = shift earlier, etc.
    /// </summary>
    [Range(.25f, 1)] public float gearShiftingThreshold = .75f;

    /// <summary>
    /// The speed at which clutch engages/disengages. Lower = faster clutch, Higher = smoother.
    /// </summary>
    [Range(.1f, .9f)] public float clutchInertia = .25f;

    /// <summary>
    /// The RPM above which the vehicle shifts up automatically in auto or semi-auto mode.
    /// </summary>
    public float gearShiftUpRPM = 6500f;

    /// <summary>
    /// The RPM below which the vehicle shifts down automatically in auto or semi-auto mode.
    /// </summary>
    public float gearShiftDownRPM = 3500f;

    /// <summary>
    /// True if the gearbox is currently in the process of shifting (clutch in, cut gas, etc.).
    /// </summary>
    public bool changingGear = false;

    /// <summary>
    /// The direction multiplier. 1 = forward, -1 = reverse, 0 = neutral.
    /// </summary>
    public int direction = 1;

    /// <summary>
    /// If true, the player is allowed to shift into reverse gear automatically when speed is low enough.
    /// </summary>
    internal bool canGoReverseNow = false;

    /// <summary>
    /// A measure used to see if the vehicle has started from a standstill (for calculating clutch logic).
    /// </summary>
    public float launched = 0f;

    /// <summary>
    /// If true, pressing brake input at low speeds automatically engages reverse gear.
    /// </summary>
    public bool autoReverse = true;

    /// <summary>
    /// If true, the gearbox automatically shifts up/down when RPM thresholds are reached.
    /// </summary>
    public bool automaticGear = true;

    /// <summary>
    /// If true, the vehicle is in a "semi-automatic" gear mode, requiring the user to shift up/down 
    /// but still applying some auto clutch.
    /// </summary>
    internal bool semiAutomaticGear = false;

    /// <summary>
    /// If true, an automatic or semi-auto gear will also engage an automatic clutch. 
    /// Otherwise, the user must manually handle the clutch input.
    /// </summary>
    public bool automaticClutch = true;
    #endregion

    #region Audio
    //--------------------------------------------------------------------------------
    //                              AUDIO SYSTEM
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Selects how many engine sound layers to use. 
    /// - Off, OneSource, TwoSource, or ThreeSource.
    /// </summary>
    public AudioType audioType;
    public enum AudioType { OneSource, TwoSource, ThreeSource, Off }

    /// <summary>
    /// If true, artificially generates "off" sound layers (low pass versions) if no off clips are assigned.
    /// </summary>
    public bool autoCreateEngineOffSounds = true;

    // Engine start clip used for starting the engine.
    public AudioClip engineStartClip;

    // Engine audio references.

    private AudioSource engineSoundHigh;
    public AudioClip engineClipHigh;
    private AudioSource engineSoundMed;
    public AudioClip engineClipMed;
    private AudioSource engineSoundLow;
    public AudioClip engineClipLow;
    private AudioSource engineSoundIdle;
    public AudioClip engineClipIdle;
    private AudioSource gearShiftingSound;

    private AudioSource engineSoundHighOff;
    public AudioClip engineClipHighOff;
    private AudioSource engineSoundMedOff;
    public AudioClip engineClipMedOff;
    private AudioSource engineSoundLowOff;
    public AudioClip engineClipLowOff;

    // Additional audio references using shared settings in RCC_Settings.
    private AudioClip[] GearShiftingClips { get { return Settings.gearShiftingClips; } }
    private AudioSource crashSound;
    private AudioClip[] CrashClips { get { return Settings.crashClips; } }
    private AudioSource reversingSound;
    private AudioClip ReversingClip { get { return Settings.reversingClip; } }
    private AudioSource windSound;
    private AudioClip WindClip { get { return Settings.windClip; } }
    private AudioSource brakeSound;
    private AudioClip BrakeClip { get { return Settings.brakeClip; } }
    private AudioSource NOSSound;
    private AudioClip NOSClip { get { return Settings.NOSClip; } }
    private AudioSource turboSound;
    private AudioClip TurboClip { get { return Settings.turboClip; } }
    private AudioSource blowSound;
    private AudioClip[] BlowClip { get { return Settings.blowoutClip; } }

    // Pitch/volume ranges for engine sound sources.
    [Range(0f, 1f)] public float minEngineSoundPitch = .75f;
    [Range(1f, 2f)] public float maxEngineSoundPitch = 1.75f;
    [Range(0f, 1f)] public float minEngineSoundVolume = .05f;
    [Range(0f, 1f)] public float maxEngineSoundVolume = .85f;
    [Range(0f, 1f)] public float idleEngineSoundVolume = .85f;

    // Positions of various audio sources around the vehicle.
    public Vector3 engineSoundPosition = new Vector3(0f, 0f, 1.5f);
    public Vector3 gearSoundPosition = new Vector3(0f, -.5f, .5f);
    public Vector3 turboSoundPosition = new Vector3(0f, 0f, 1.5f);
    public Vector3 exhaustSoundPosition = new Vector3(0f, -.5f, -2f);
    public Vector3 windSoundPosition = new Vector3(0f, 0f, 2f);
    #endregion

    #region Inputs
    //--------------------------------------------------------------------------------
    //                               VEHICLE INPUTS
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Holds the base set of input values for throttle, brake, steer, etc.
    /// Typically updated from RCC_InputManager, but can be overridden manually.
    /// </summary>
    public RCC_Inputs inputs = new RCC_Inputs();

    /// <summary>
    /// Final processed throttle input (0-1).
    /// </summary>
    [HideInInspector] public float throttleInput = 0f;

    /// <summary>
    /// Final processed brake input (0-1).
    /// </summary>
    [HideInInspector] public float brakeInput = 0f;

    /// <summary>
    /// Final processed steering input (-1 to 1).
    /// </summary>
    [HideInInspector] public float steerInput = 0f;

    /// <summary>
    /// An internally computed counter-steer input if useCounterSteering is true.
    /// </summary>
    [HideInInspector] public float counterSteerInput = 0f;

    /// <summary>
    /// Clutch input (0-1). If using manual or semi-auto transmissions and not using UseAutomaticClutch.
    /// </summary>
    [HideInInspector] public float clutchInput = 0f;

    /// <summary>
    /// Handbrake input (0-1). If > 0.1f, wheels that canHandbrake are locked.
    /// </summary>
    [HideInInspector] public float handbrakeInput = 0f;

    /// <summary>
    /// Boost input (0-1), representing how much NOS/turbo user is applying if supported.
    /// </summary>
    [HideInInspector] public float boostInput = 0f;

    /// <summary>
    /// Fuel input (0-1), representing if there is enough fuel to run the engine. 
    /// Set to 0 if the tank is empty.
    /// </summary>
    [HideInInspector] public float fuelInput = 0f;

    /// <summary>
    /// A quick variable to forcibly cut the throttle (e.g., when hitting rev limiter or shifting).
    /// </summary>
    [HideInInspector] public bool cutGas = false;

    /// <summary>
    /// Forces full throttle for certain uses (like burnout or smoke preview). Rarely used.
    /// </summary>
    [HideInInspector] public bool permanentGas = false;

    /// <summary>
    /// Overrides the current engine rpm.
    /// </summary>
    [HideInInspector] public bool overridingEngineRPM = false;
    #endregion

    #region Head Lights
    //--------------------------------------------------------------------------------
    //                              VEHICLE LIGHTS
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Toggles the low-beam headlights.
    /// </summary>
    public bool lowBeamHeadLightsOn = false;

    /// <summary>
    /// Toggles the high-beam headlights.
    /// </summary>
    public bool highBeamHeadLightsOn = false;

    /// <summary>
    /// Toggles the interior/dash lights.
    /// </summary>
    public bool interiorLightsOn = false;
    #endregion

    #region Indicator Lights
    //--------------------------------------------------------------------------------
    //                             INDICATOR SYSTEM
    //--------------------------------------------------------------------------------

    /// <summary>
    /// The current indicator mode (Off, Right, Left, or Hazard/All).
    /// </summary>
    public IndicatorsOn indicatorsOn = IndicatorsOn.Off;

    /// <summary>
    /// Indicator states for referencing the left/right/hazard logic.
    /// </summary>
    public enum IndicatorsOn { Off, Right, Left, All }

    /// <summary>
    /// Timer used for blinking indicators. 
    /// The script toggles indicator lights on/off every 0.5s, cycling each second.
    /// </summary>
    public float indicatorTimer = 0f;
    #endregion

    #region Damage
    //--------------------------------------------------------------------------------
    //                             DAMAGE SYSTEM
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Stores damage settings and logic for collisions (mesh deformation, etc.).
    /// </summary>
    public RCC_Damage damage = new RCC_Damage();

    /// <summary>
    /// Enable/disable collision-based mesh deformation and damage.
    /// </summary>
    public bool useDamage = true;

    /// <summary>
    /// Whether to spawn collision-based particle effects (sparks, etc.) on impact.
    /// </summary>
    public bool useCollisionParticles = true;

    /// <summary>
    /// Whether to play crash audio clips on collisions.
    /// </summary>
    public bool useCollisionAudio = true;

    /// <summary>
    /// Particle system prefab for contact collisions, assigned from RCC_Settings.
    /// </summary>
    public GameObject contactSparkle { get { return Settings.contactParticles; } }

    /// <summary>
    /// Particle system prefab for scraping collisions, assigned from RCC_Settings.
    /// </summary>
    public GameObject scratchSparkle { get { return Settings.scratchParticles; } }

    /// <summary>
    /// A local pool of contact spark particle systems for re-use, avoiding repeated instantiations.
    /// </summary>
    private List<ParticleSystem> contactSparkeList = new List<ParticleSystem>();

    /// <summary>
    /// A local pool of scratch spark particle systems for re-use, avoiding repeated instantiations.
    /// </summary>
    private List<ParticleSystem> scratchSparkeList = new List<ParticleSystem>();

    /// <summary>
    /// The maximum number of contact particle systems to create and pool.
    /// </summary>
    public int maximumContactSparkle = 5;

    /// <summary>
    /// The main parent GameObject to keep all collision particles in a neat hierarchy.
    /// </summary>
    private GameObject allContactParticles;
    #endregion

    #region Helpers
    //--------------------------------------------------------------------------------
    //                       HELPER COMPONENTS
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Tracks the vehicle's rotation from the previous frame, used for steering assist logic.
    /// </summary>
    private float oldRotation = 0f;

    /// <summary>
    /// A debug transform for visualizing velocity direction. 
    /// The script uses it for steer helper calculations.
    /// </summary>
    private Transform velocityDirection;

    /// <summary>
    /// A debug transform for visualizing steering direction. 
    /// The script uses it for steer helper calculations.
    /// </summary>
    private Transform steeringDirection;

    /// <summary>
    /// Holds the angle between velocity direction and forward direction. 
    /// Used in advanced steering calculations.
    /// </summary>
    private float velocityAngle = 0f;

    /// <summary>
    /// A local angle measure used in traction or ESP computations.
    /// </summary>
    private float angle = 0f;

    /// <summary>
    /// Tracks the vehicle's angular velocity about Y for drift/traction logic.
    /// </summary>
    private float angularVelo = 0f;
    #endregion

    #region Driving Assistances
    //--------------------------------------------------------------------------------
    //                        DRIVER ASSISTANCE FEATURES
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Anti-lock brake system (ABS). If true, the script modulates braking to prevent wheel lock.
    /// </summary>
    public bool ABS = true;

    /// <summary>
    /// Traction Control System (TCS). If true, throttle is cut or torque is adjusted to prevent wheel spin.
    /// </summary>
    public bool TCS = true;

    /// <summary>
    /// Electronic Stability Program (ESP). If true, modulates brake/torque for stability under steering/cornering.
    /// </summary>
    public bool ESP = true;

    /// <summary>
    /// If true, the script uses a "steer helper" approach to correct small divergences in steering.
    /// </summary>
    public bool steeringHelper = true;

    /// <summary>
    /// If true, uses traction helper logic to mitigate slip at moderate angles.
    /// </summary>
    public bool tractionHelper = true;

    /// <summary>
    /// If true, uses a dynamic angular drag approach to reduce rotational spin at high speeds.
    /// </summary>
    public bool angularDragHelper = false;

    // Thresholds for ABS / TCS / ESP logic.
    [Range(.05f, .5f)] public float ABSThreshold = .35f;
    [Range(.05f, 1f)] public float TCSStrength = .5f;
    [Range(.05f, .5f)] public float ESPThreshold = .5f;
    [Range(.05f, 1f)] public float ESPStrength = .25f;
    [Range(0f, 1f)] public float steerHelperLinearVelStrength = .1f;
    [Range(0f, 1f)] public float steerHelperAngularVelStrength = .1f;
    [Range(0f, 1f)] public float tractionHelperStrength = .1f;
    [Range(0f, 1f)] public float angularDragHelperStrength = .1f;

    /// <summary>
    /// ABS is active this frame if wheels exceed slip threshold while braking.
    /// </summary>
    public bool ABSAct = false;

    /// <summary>
    /// TCS is active this frame if wheels exceed slip threshold while accelerating.
    /// </summary>
    public bool TCSAct = false;

    /// <summary>
    /// ESP is active this frame if understeer/oversteer is detected and correction is applied.
    /// </summary>
    public bool ESPAct = false;

    /// <summary>
    /// If true, the ESP system is malfunctioning or deliberately disabled for advanced scenarios.
    /// </summary>
    public bool ESPBroken = false;

    /// <summary>
    /// Sum of front wheels' sideways slip. Helps identify understeer.
    /// </summary>
    public float frontSlip = 0f;

    /// <summary>
    /// Sum of rear wheels' sideways slip. Helps identify oversteer.
    /// </summary>
    public float rearSlip = 0f;

    /// <summary>
    /// True if the front slip indicates understeering beyond ESPThreshold.
    /// </summary>
    public bool underSteering = false;

    /// <summary>
    /// True if the rear slip indicates oversteering beyond ESPThreshold.
    /// </summary>
    public bool overSteering = false;
    #endregion

    #region Drift
    //--------------------------------------------------------------------------------
    //                             DRIFT LOGIC
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Drift mode.
    /// </summary>
    public bool driftMode = false;

    /// <summary>
    /// True if the vehicle is currently drifting, determined by rear slip angle.
    /// </summary>
    internal bool driftingNow = false;

    /// <summary>
    /// The magnitude of the drift angle. A positive or negative slip.
    /// </summary>
    internal float driftAngle = 0f;
    #endregion

    #region Turbo / NOS / Boost
    //--------------------------------------------------------------------------------
    //                            TURBO & NOS
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Turbo spool factor (0-30). Higher = more turbo pressure. 
    /// The script adjusts it based on throttle and engine load for audio or boost logic.
    /// </summary>
    public float turboBoost = 0f;

    /// <summary>
    /// The current level of NOS (0-100). Consumed while used; can regenerate over time.
    /// </summary>
    public float NoS = 100f;

    private float NoSConsumption = 25f;
    private float NoSRegenerateTime = 10f;

    /// <summary>
    /// If true, the vehicle can use a nitrous oxide system for short bursts of power.
    /// </summary>
    public bool useNOS = false;

    /// <summary>
    /// If true, the vehicle uses a turbo system, affecting audio pitch and spool.
    /// </summary>
    public bool useTurbo = false;
    #endregion

    #region Customizer
    //--------------------------------------------------------------------------------
    //                        VEHICLE CUSTOMIZER
    //--------------------------------------------------------------------------------

    private RCC_Customizer customizer;
    /// <summary>
    /// Optional reference to a <c>RCC_Customizer</c> component for paint, body modifications, etc.
    /// </summary>
    public RCC_Customizer Customizer {

        get {

            if (!customizer)
                customizer = GetComponent<RCC_Customizer>();

            return customizer;

        }

    }
    #endregion

    #region Events
    //--------------------------------------------------------------------------------
    //                          VEHICLE EVENTS
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Fired when this player-controlled vehicle spawns or becomes active.
    /// </summary>
    public delegate void onRCCPlayerSpawned(RCC_CarControllerV4 RCC);
    public static event onRCCPlayerSpawned OnRCCPlayerSpawned;

    /// <summary>
    /// Fired when this vehicle is destroyed or disabled.
    /// </summary>
    public delegate void onRCCPlayerDestroyed(RCC_CarControllerV4 RCC);
    public static event onRCCPlayerDestroyed OnRCCPlayerDestroyed;

    /// <summary>
    /// Fired upon collisions involving this vehicle (only if it's the active player vehicle).
    /// </summary>
    public delegate void onRCCPlayerCollision(RCC_CarControllerV4 RCC, Collision collision);
    public static event onRCCPlayerCollision OnRCCPlayerCollision;
    #endregion

    /// <summary>
    /// If this vehicle is a truck with a trailer, stores reference to the attached trailer (if any).
    /// </summary>
    public RCC_TruckTrailer attachedTrailer;

    private void Awake() {

        // Limit the max angular velocity for performance & stability.
        Rigid.maxAngularVelocity = Settings.maxAngularVelocity;

        // Ensure gear shift threshold, engine inertia are within specified range.
        gearShiftingThreshold = Mathf.Clamp(gearShiftingThreshold, .25f, 1f);

        if (engineInertia > .4f)
            engineInertia = .15f;

        engineInertia = Mathf.Clamp(engineInertia, .02f, .4f);

        // Cache old engine parameters to detect changes.
        oldEngineTorque = maxEngineTorque;
        oldMaxTorqueAtRPM = maxEngineTorqueAtRPM;
        oldMinEngineRPM = minEngineRPM;
        oldMaxEngineRPM = maxEngineRPM;

        // Assign each wheel's visual transform to the matching wheel collider.
        FrontLeftWheelCollider.wheelModel = FrontLeftWheelTransform;
        FrontRightWheelCollider.wheelModel = FrontRightWheelTransform;
        RearLeftWheelCollider.wheelModel = RearLeftWheelTransform;
        RearRightWheelCollider.wheelModel = RearRightWheelTransform;

        if (ExtraRearWheelsCollider != null) {

            for (int i = 0; i < ExtraRearWheelsCollider.Length; i++)
                ExtraRearWheelsCollider[i].wheelModel = ExtraRearWheelsTransform[i];

        }

        // Store the original steering angle for dynamic calculations (e.g., highspeed steer limit).
        orgSteerAngle = steerAngle;

        // Set up a parent for contact/scratch particles to keep the hierarchy tidy.
        allContactParticles = new GameObject("All Contact Particles");
        allContactParticles.transform.SetParent(transform, false);

        // Create and configure audio sources for engine, brakes, etc.
        CreateAudios();

        // If overrideBehavior is false, apply the currently selected RCC behavior from settings (if any).
        if (!overrideBehavior)
            CheckBehavior();

        // If configured, start the engine upon Awake or if external controller is used.
        if (RunEngineAtAwake || externalController) {

            engineRunning = true;
            fuelInput = 1f;

        }

        // Ensure steerAngleCurve is properly defined or create a default one.
        if (steerAngleCurve == null)
            steerAngleCurve = new AnimationCurve(new Keyframe(0f, 40f, 0f, -.3f), new Keyframe(120f, 10f, -.115f, -.1f), new Keyframe(200f, 7f));
        else if (steerAngleCurve.length < 1)
            steerAngleCurve = new AnimationCurve(new Keyframe(0f, 40f, 0f, -.3f), new Keyframe(120f, 10f, -.115f, -.1f), new Keyframe(200f, 7f));

#if BCG_URP
        if (IsURP())
            CheckLensflares_URP();
#endif

    }

    private void OnEnable() {

        // Reset gear, drift, and assistance flags.
        changingGear = false;
        currentGear = 0;
        driftingNow = false;
        driftAngle = 0f;
        ABSAct = false;
        TCSAct = false;
        ESPAct = false;
        frontSlip = 0f;
        rearSlip = 0f;
        underSteering = false;
        overSteering = false;
        oldRotation = 0f;
        velocityAngle = 0f;
        angle = 0f;
        angularVelo = 0f;
        throttleInput = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        counterSteerInput = 0f;
        clutchInput = 0f;
        handbrakeInput = 0f;
        boostInput = 0f;
        cutGas = false;
        permanentGas = false;
        NGear = false;
        direction = 1;
        launched = 0f;
        resetTime = 0f;

        // If the engine is running, set initial RPM to minEngineRPM.
        if (engineRunning) {

            engineRPMRaw = minEngineRPM;
            engineRPM = engineRPMRaw;

        }

        // Fire the OnRCCPlayerSpawned event unless externally controlled (e.g., an AI).
        StartCoroutine(RCCPlayerSpawned());

        // Listen for changes in global RCC behavior, update this vehicle accordingly.
        RCC_SceneManager.OnBehaviorChanged += CheckBehavior;

        // Listen for input events that might affect this vehicle.
        RCC_InputManager.OnStartStopEngine += RCC_InputManager_OnStartStopEngine;
        RCC_InputManager.OnLowBeamHeadlights += RCC_InputManager_OnLowBeamHeadlights;
        RCC_InputManager.OnHighBeamHeadlights += RCC_InputManager_OnHighBeamHeadlights;
        RCC_InputManager.OnIndicatorLeft += RCC_InputManager_OnIndicatorLeft;
        RCC_InputManager.OnIndicatorRight += RCC_InputManager_OnIndicatorRight;
        RCC_InputManager.OnIndicatorHazard += RCC_InputManager_OnIndicatorHazard;
        RCC_InputManager.OnInteriorlights += RCC_InputManager_OnInteriorLights;
        RCC_InputManager.OnGearShiftUp += RCC_InputManager_OnGearShiftUp;
        RCC_InputManager.OnGearShiftDown += RCC_InputManager_OnGearShiftDown;
        RCC_InputManager.OnNGear += RCC_InputManager_OnNGear;
        RCC_InputManager.OnTrailerDetach += RCC_InputManager_OnTrailerDetach;

    }

    /// <summary>
    /// Invokes OnRCCPlayerSpawned event for this vehicle after one frame.
    /// </summary>
    private IEnumerator RCCPlayerSpawned() {

        yield return new WaitForEndOfFrame();

        // If not an external/AI controller, raise the player spawned event.
        if (!externalController) {

            if (OnRCCPlayerSpawned != null)
                OnRCCPlayerSpawned(this);

        }

    }

    /// <summary>
    /// Creates all wheel colliders in an editor script scenario. Not used at runtime typically.
    /// </summary>
    public void CreateWheelColliders() {

        CreateWheelColliders(this);

    }

    /// <summary>
    /// Creates and configures the multiple audio sources needed for engine, reversing, wind, etc.
    /// </summary>
    private void CreateAudios() {

        switch (audioType) {

            case AudioType.OneSource:
                engineSoundHigh = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High AudioSource", 5, 50, 0, engineClipHigh, true, true, false);

                if (autoCreateEngineOffSounds) {

                    engineSoundHighOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5, 50, 0, engineClipHigh, true, true, false);
                    NewLowPassFilter(engineSoundHighOff, 3000f);

                } else {

                    engineSoundHighOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5, 50, 0, engineClipHighOff, true, true, false);

                }

                break;

            case AudioType.TwoSource:
                engineSoundHigh = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High AudioSource", 5, 50, 0, engineClipHigh, true, true, false);
                engineSoundLow = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low AudioSource", 5, 25, 0, engineClipLow, true, true, false);

                if (autoCreateEngineOffSounds) {

                    engineSoundHighOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5, 50, 0, engineClipHigh, true, true, false);
                    engineSoundLowOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5, 25, 0, engineClipLow, true, true, false);

                    NewLowPassFilter(engineSoundHighOff, 3000f);
                    NewLowPassFilter(engineSoundLowOff, 3000f);

                } else {

                    engineSoundHighOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5, 50, 0, engineClipHighOff, true, true, false);
                    engineSoundLowOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5, 25, 0, engineClipLowOff, true, true, false);

                }

                break;

            case AudioType.ThreeSource:
                engineSoundHigh = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High AudioSource", 5, 50, 0, engineClipHigh, true, true, false);
                engineSoundMed = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Medium AudioSource", 5, 50, 0, engineClipMed, true, true, false);
                engineSoundLow = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low AudioSource", 5, 25, 0, engineClipLow, true, true, false);

                if (autoCreateEngineOffSounds) {

                    engineSoundHighOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5, 50, 0, engineClipHigh, true, true, false);
                    engineSoundMedOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Medium Off AudioSource", 5, 50, 0, engineClipMed, true, true, false);
                    engineSoundLowOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5, 25, 0, engineClipLow, true, true, false);

                    if (engineSoundHighOff)
                        NewLowPassFilter(engineSoundHighOff, 3000f);
                    if (engineSoundMedOff)
                        NewLowPassFilter(engineSoundMedOff, 3000f);
                    if (engineSoundLowOff)
                        NewLowPassFilter(engineSoundLowOff, 3000f);

                } else {

                    engineSoundHighOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound High Off AudioSource", 5, 50, 0, engineClipHighOff, true, true, false);
                    engineSoundMedOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Medium Off AudioSource", 5, 50, 0, engineClipMedOff, true, true, false);
                    engineSoundLowOff = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Low Off AudioSource", 5, 25, 0, engineClipLowOff, true, true, false);

                }

                break;
        }

        engineSoundIdle = NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Sound Idle AudioSource", 5, 25, 0, engineClipIdle, true, true, false);
        reversingSound = NewAudioSource(Settings.audioMixer, gameObject, gearSoundPosition, "Reverse Sound AudioSource", 10, 50, 0, ReversingClip, true, false, false);
        windSound = NewAudioSource(Settings.audioMixer, gameObject, windSoundPosition, "Wind Sound AudioSource", 1, 10, 0, WindClip, true, true, false);
        brakeSound = NewAudioSource(Settings.audioMixer, gameObject, "Brake Sound AudioSource", 1, 10, 0, BrakeClip, true, true, false);

        if (useNOS)
            NOSSound = NewAudioSource(Settings.audioMixer, gameObject, exhaustSoundPosition, "NOS Sound AudioSource", 5, 10, .5f, NOSClip, true, false, false);

        if (useNOS || useTurbo)
            blowSound = NewAudioSource(Settings.audioMixer, gameObject, exhaustSoundPosition, "NOS Blow", 1f, 10f, .5f, null, false, false, false);

        if (useTurbo) {

            turboSound = NewAudioSource(Settings.audioMixer, gameObject, turboSoundPosition, "Turbo Sound AudioSource", .1f, .5f, 0f, TurboClip, true, true, false);
            NewHighPassFilter(turboSound, 10000f, 10);

        }

    }

    /// <summary>
    /// Checks if a selected behavior is set in RCC Settings and applies it if <c>overrideBehavior</c> is false.
    /// </summary>
    private void CheckBehavior() {

        if (overrideBehavior)
            return;

        if (Settings.selectedBehaviorType == null)
            return;

        SetBehavior(this);

    }

    /// <summary>
    /// Rebuilds the engineTorqueCurve with three key points: minEngineRPM, maxEngineTorqueAtRPM, and maxEngineRPM.
    /// </summary>
    public void ReCreateEngineTorqueCurve() {

        engineTorqueCurve = new AnimationCurve();
        engineTorqueCurve.AddKey(minEngineRPM, maxEngineTorque / 2f);
        engineTorqueCurve.AddKey(maxEngineTorqueAtRPM, maxEngineTorque);
        engineTorqueCurve.AddKey(maxEngineRPM, maxEngineTorque / 1.5f);

        oldEngineTorque = maxEngineTorque;
        oldMaxTorqueAtRPM = maxEngineTorqueAtRPM;
        oldMinEngineRPM = minEngineRPM;
        oldMaxEngineRPM = maxEngineRPM;

    }

    private void CalculateMaxSpeed() {

        // Calculating average traction wheel radius.
        float averagePowerWheelRadius = 0f;
        int counter = 0;

        for (int i = 0; i < AllWheelColliders.Length; i++) {

            if (AllWheelColliders[i] == null || !AllWheelColliders[i].canPower || !AllWheelColliders[i].enabled)
                continue;

            counter++;
            averagePowerWheelRadius += AllWheelColliders[i].WheelCollider.radius;
        }

        if (counter > 0)
            averagePowerWheelRadius /= counter;
        else
            return;

        // Calculating max speed at last gear as km/h unit.
        maxspeed = maxEngineRPM / (gears[gears.Length - 1].maxRatio * finalRatio);
        maxspeed = (maxspeed * 60f / 1000f) * (2 * Mathf.PI * averagePowerWheelRadius);

        float maxSpeedForGears = maxspeed * gears[gears.Length - 1].maxRatio;

        // Calculating max speed for each gear.
        for (int i = 0; i < gears.Length; i++)
            gears[i].maxSpeed = (int)(maxSpeedForGears / gears[i].maxRatio);

    }

    /// <summary>
    /// Creates default gear ratios and speed thresholds based on totalGears, if none exist.
    /// </summary>
    public void InitGears() {

        gears = new Gear[totalGears];

        float[] gearRatio = new float[gears.Length];
        int[] maxSpeedForGear = new int[gears.Length];
        int[] targetSpeedForGear = new int[gears.Length];

        if (gears.Length == 1)
            gearRatio = new float[] { 1.0f };
        if (gears.Length == 2)
            gearRatio = new float[] { 2.0f, 1.0f };
        if (gears.Length == 3)
            gearRatio = new float[] { 2.0f, 1.5f, 1.0f };
        if (gears.Length == 4)
            gearRatio = new float[] { 2.86f, 1.62f, 1.0f, .72f };
        if (gears.Length == 5)
            gearRatio = new float[] { 4.23f, 2.52f, 1.66f, 1.22f, 1.0f };
        if (gears.Length == 6)
            gearRatio = new float[] { 4.35f, 2.5f, 1.66f, 1.23f, 1.0f, .85f };
        if (gears.Length == 7)
            gearRatio = new float[] { 4.5f, 2.5f, 1.66f, 1.23f, 1.0f, .9f, .8f };
        if (gears.Length == 8)
            gearRatio = new float[] { 4.6f, 2.5f, 1.86f, 1.43f, 1.23f, 1.05f, .9f, .72f };

        for (int i = 0; i < gears.Length; i++) {

            maxSpeedForGear[i] = (int)((maxspeed / gears.Length) * (i + 1));
            targetSpeedForGear[i] = (int)(Mathf.Lerp(0, maxspeed * Mathf.Lerp(0f, 1f, gearShiftingThreshold), ((float)(i + 1) / (float)(gears.Length))));

        }

        for (int i = 0; i < gears.Length; i++) {

            gears[i] = new Gear();
            gears[i].SetGear(gearRatio[i], maxSpeedForGear[i], targetSpeedForGear[i]);

        }

    }

    /// <summary>
    /// Toggles the engine state (kill if running, start if not).
    /// </summary>
    public void KillOrStartEngine() {

        if (engineRunning)
            KillEngine();
        else
            StartEngine();

    }

    /// <summary>
    /// Immediately starts the engine (if not already running).
    /// </summary>
    public void StartEngine() {

        if (!engineRunning)
            StartCoroutine(StartEngineDelayed());

    }

    /// <summary>
    /// Starts the engine, optionally instantly or via a start clip coroutine.
    /// </summary>
    public void StartEngine(bool instantStart) {

        if (instantStart) {

            fuelInput = 1f;
            engineRunning = true;

        } else {

            StartCoroutine(StartEngineDelayed());

        }

    }

    /// <summary>
    /// Plays an engine start sound, then sets engineRunning to true.
    /// </summary>
    public IEnumerator StartEngineDelayed() {

        if (!engineRunning) {

            NewAudioSource(Settings.audioMixer, gameObject, engineSoundPosition, "Engine Start AudioSource", 1, 10, 1, engineStartClip, false, true, true);
            yield return new WaitForSeconds(1f);
            engineRunning = true;
            fuelInput = 1f;

        }

    }

    /// <summary>
    /// Kills the engine by setting engineRunning = false and removing fuel input.
    /// </summary>
    public void KillEngine() {

        fuelInput = 0f;
        engineRunning = false;

    }

    /// <summary>
    /// Handles steering wheel model rotation in the interior. 
    /// Used for visual feedback when the player steers.
    /// </summary>
    private void OtherVisuals() {

        if (SteeringWheel) {

            if (orgSteeringWheelRot.eulerAngles == Vector3.zero)
                orgSteeringWheelRot = SteeringWheel.transform.localRotation;

            switch (steeringWheelRotateAround) {

                case SteeringWheelRotateAround.XAxis:
                    SteeringWheel.transform.localRotation = Quaternion.Lerp(
                        SteeringWheel.transform.localRotation,
                        orgSteeringWheelRot * Quaternion.AngleAxis(((steerInput * steerAngle) * -steeringWheelAngleMultiplier), Vector3.right),
                        Time.deltaTime * 25f
                    );

                    break;

                case SteeringWheelRotateAround.YAxis:
                    SteeringWheel.transform.localRotation = Quaternion.Lerp(
                        SteeringWheel.transform.localRotation,
                        orgSteeringWheelRot * Quaternion.AngleAxis(((steerInput * steerAngle) * -steeringWheelAngleMultiplier), Vector3.up),
                        Time.deltaTime * 25f
                    );
                    break;

                case SteeringWheelRotateAround.ZAxis:
                    SteeringWheel.transform.localRotation = Quaternion.Lerp(
                        SteeringWheel.transform.localRotation,
                        orgSteeringWheelRot * Quaternion.AngleAxis(((steerInput * steerAngle) * -steeringWheelAngleMultiplier), Vector3.forward),
                        Time.deltaTime * 25f
                    );
                    break;
            }
        }
    }

    private void Update() {

        Inputs();
        Audio();
        CheckReset();

        if (useDamage) {

            damage.UpdateRepair();
            damage.UpdateDamage();

        }

        OtherVisuals();

        indicatorTimer += Time.deltaTime;

        if (throttleInput >= .1f)
            launched += throttleInput * Time.deltaTime;
        else
            launched -= Time.deltaTime;

        launched = Mathf.Clamp01(launched);

        float rearSidewaysSlip = RearLeftWheelCollider.wheelSlipAmountSideways + RearRightWheelCollider.wheelSlipAmountSideways;
        rearSidewaysSlip /= 2f;

        driftingNow = Mathf.Abs(rearSidewaysSlip) > .25f;
        driftAngle = rearSidewaysSlip * 1f;

    }

    private void FixedUpdate() {

        CalculateMaxSpeed();

        Vector3 locVel = transform.InverseTransformDirection(Rigid.angularVelocity);

        switch (COMAssister) {
            case COMAssisterTypes.Off:
                locVel *= 0f;
                break;
            case COMAssisterTypes.Slight:
                locVel /= 10f;
                break;
            case COMAssisterTypes.Medium:
                locVel /= 5f;
                break;
            case COMAssisterTypes.Opposite:
                locVel /= -5f;
                break;

        }

        Rigid.centerOfMass = new Vector3(COM.localPosition.x + locVel.y, COM.localPosition.y, COM.localPosition.z);

        speed = Rigid.linearVelocity.magnitude * 3.6f;

        if (oldEngineTorque == -1f)
            oldEngineTorque = maxEngineTorque;

        if (oldMaxTorqueAtRPM == -1f)
            oldMaxTorqueAtRPM = maxEngineTorqueAtRPM;

        if (oldMinEngineRPM == -1f)
            oldMinEngineRPM = minEngineRPM;

        if (oldMaxEngineRPM == -1f)
            oldMaxEngineRPM = maxEngineRPM;

        if (autoGenerateEngineRPMCurve && (oldEngineTorque != maxEngineTorque || oldMaxTorqueAtRPM != maxEngineTorqueAtRPM || minEngineRPM != oldMinEngineRPM || maxEngineRPM != oldMaxEngineRPM))
            ReCreateEngineTorqueCurve();

        if (gears == null || gears.Length == 0) {

            print("Gear can not be 0! Recreating gears...");
            InitGears();

        }

        int currentPoweredWheels = 0;

        for (int i = 0; i < AllWheelColliders.Length; i++) {

            if (AllWheelColliders[i].canPower)
                currentPoweredWheels++;

        }

        poweredWheels = currentPoweredWheels;

        Engine();
        Steering();
        Wheels();

        if (canControl) {

            if (automaticClutch)
                AutomaticClutch();

            if (automaticGear)
                AutomaticGearbox();

        }

        AntiRollBars();
        CheckGrounded();

        if (useRevLimiter)
            RevLimiter();

        if (useTurbo)
            Turbo();

        if (useNOS)
            NOS();

        if (useFuelConsumption)
            Fuel();

        if (useEngineHeat)
            EngineHeat();

        if (steeringHelper)
            SteerHelper();

        if (tractionHelper)
            TractionHelper();

        if (angularDragHelper)
            AngularDragHelper();

        if (ESP) {

            ESPCheck();

        } else {

            frontSlip = 0f;
            rearSlip = 0f;
            underSteering = false;
            overSteering = false;
            ESPAct = false;

        }

        if (!overrideBehavior && Settings.selectedBehaviorType != null && Settings.selectedBehaviorType.applyRelativeTorque) {

            if (isGrounded) {

                // Get local velocity components
                Vector3 relativeVelocity = transform.InverseTransformDirection(Rigid.linearVelocity);

                Rigid.AddRelativeTorque(Vector3.up * (((inputs.steerInput) * Mathf.Sign(relativeVelocity.z))) * Mathf.Lerp(1.5f, 1.5f, speed / 160f), ForceMode.Acceleration);

            }

        }

        Rigid.AddForceAtPosition(-transform.up * (speed * downForce), COM.transform.position, ForceMode.Force);

    }

    /// <summary>
    /// Gathers input from RCC_InputManager or uses external override if set.
    /// </summary>
    private void Inputs() {

        if (canControl) {

            if (!externalController) {

                if (!overrideInputs)
                    inputs = RCC_InputManager.Instance.inputs;

                if (!automaticGear || semiAutomaticGear) {

                    if (!changingGear && !cutGas)
                        throttleInput = inputs.throttleInput;
                    else
                        throttleInput = 0f;

                } else {

                    if (!changingGear && !cutGas)
                        throttleInput = (direction == 1 ? Mathf.Clamp01(inputs.throttleInput) : Mathf.Clamp01(inputs.brakeInput));
                    else
                        throttleInput = 0f;

                }

                if (!automaticGear || semiAutomaticGear) {

                    brakeInput = Mathf.Clamp01(inputs.brakeInput);

                } else {

                    if (!cutGas)
                        brakeInput = (direction == 1 ? Mathf.Clamp01(inputs.brakeInput) : Mathf.Clamp01(inputs.throttleInput));
                    else
                        brakeInput = 0f;

                }

                if (useSteeringSensitivity) {

                    bool oppositeDirection = Mathf.Sign(inputs.steerInput) != Mathf.Sign(steerInput);

                    steerInput = Mathf.MoveTowards(steerInput, inputs.steerInput + counterSteerInput,
                        (Time.deltaTime * steeringSensitivityFactor * Mathf.Lerp(10f, 5f, steerAngle / orgSteerAngle)) * (oppositeDirection ? 1f : 1f));

                } else {

                    steerInput = inputs.steerInput + counterSteerInput;

                }

                SteeringAssistance();

                boostInput = inputs.boostInput;
                handbrakeInput = inputs.handbrakeInput;

                if (!automaticClutch) {

                    if (!NGear)
                        clutchInput = inputs.clutchInput;
                    else
                        clutchInput = 1f;

                }

            }

        } else if (!externalController) {

            throttleInput = 0f;
            brakeInput = 0f;
            steerInput = 0f;
            boostInput = 0f;
            handbrakeInput = 1f;

        }

        if (fuelInput <= 0f) {

            throttleInput = 0f;
            engineRunning = false;

        }

        if (changingGear || cutGas)
            throttleInput = 0f;

        if (!useNOS || NoS < 5 || throttleInput < .75f)
            boostInput = 0f;

        throttleInput = Mathf.Clamp01(throttleInput);
        brakeInput = Mathf.Clamp01(brakeInput);
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
        boostInput = Mathf.Clamp01(boostInput);
        handbrakeInput = Mathf.Clamp01(handbrakeInput);

        if (autoReverse) {

            canGoReverseNow = true;

        } else {

            if (brakeInput < .5f && speed < 5)
                canGoReverseNow = true;
            else if (brakeInput > 0 && transform.InverseTransformDirection(Rigid.linearVelocity).z > 1f)
                canGoReverseNow = false;

        }

        if (automaticGear && !semiAutomaticGear && !changingGear) {

            if (brakeInput > .9f && transform.InverseTransformDirection(Rigid.linearVelocity).z < 1f && canGoReverseNow && direction != -1)
                StartCoroutine(ChangeGear(-1));
            else if (throttleInput < .1f && transform.InverseTransformDirection(Rigid.linearVelocity).z > -1f && direction == -1)
                StartCoroutine(ChangeGear(0));

        }

    }

    /// <summary>
    /// Clamps steering input if the vehicle is sliding, 
    /// and optionally adds a small counter-steer input if drifting.
    /// </summary>
    private void SteeringAssistance() {

        float sidewaysSlip = 0f;

        foreach (RCC_WheelCollider w in AllWheelColliders)
            sidewaysSlip += w.wheelSlipAmountSideways;

        sidewaysSlip /= AllWheelColliders.Length;

        if (useSteeringLimiter) {

            float maxSteerInput = Mathf.Clamp(1f - Mathf.Abs(sidewaysSlip), -1f, 1f);
            float sign = -Mathf.Sign(sidewaysSlip);

            if (maxSteerInput > 0f)
                steerInput = Mathf.Clamp(steerInput, -maxSteerInput, maxSteerInput);
            else
                steerInput = Mathf.Clamp(steerInput, sign * maxSteerInput, sign * maxSteerInput);

        }

        if (useCounterSteering)
            counterSteerInput = counterSteeringFactor * driftAngle;

    }

    /// <summary>
    /// Calculates and updates engine RPM based on current gear, throttle, clutch, and wheel RPM.
    /// </summary>
    private void Engine() {

        if (overridingEngineRPM)
            return;

        float tractionRPM = 0;

        for (int i = 0; i < AllWheelColliders.Length; i++) {

            if (AllWheelColliders[i].canPower)
                tractionRPM += Mathf.Abs(AllWheelColliders[i].WheelCollider.rpm);

        }

        float velocity = 0f;
        float newEngineInertia = engineInertia + (clutchInput / 5f * engineInertia);
        newEngineInertia *= Mathf.Lerp(1f, Mathf.Clamp(clutchInput, .75f, 1f), engineRPM / maxEngineRPM);

        engineRPMRaw = Mathf.SmoothDamp(
            engineRPMRaw,
            (Mathf.Lerp(speed < 15f ? minEngineRPM : 0f, maxEngineRPM, (clutchInput * throttleInput)) +
                ((tractionRPM / Mathf.Clamp(poweredWheels, 1, Mathf.Infinity)) * finalRatio * (gears[currentGear].maxRatio)) * (1f - clutchInput)) * fuelInput,
            ref velocity,
            newEngineInertia * .75f
        );

        engineRPMRaw = Mathf.Clamp(engineRPMRaw, 0f, maxEngineRPM + 500f);
        engineRPM = Mathf.Lerp(engineRPM, engineRPMRaw, Time.fixedDeltaTime * Mathf.Clamp(1f - clutchInput, .25f, 1f) * 25f);

    }

    /// <summary>
    /// Adjusts steerAngle based on SteeringType (Curve, Simple, Constant).
    /// </summary>
    private void Steering() {

        switch (steeringType) {
            case SteeringType.Curve:
                steerAngle = steerAngleCurve.Evaluate(speed);
                break;
            case SteeringType.Simple:
                steerAngle = Mathf.Lerp(orgSteerAngle, highspeedsteerAngle, (speed / highspeedsteerAngleAtspeed));
                break;
            case SteeringType.Constant:
                steerAngle = orgSteerAngle;
                break;

        }

    }

    /// <summary>
    /// Processes engine and driving assistance audio, plus wind and brake sounds.
    /// </summary>
    private void Audio() {

        EngineSounds();

        windSound.volume = Mathf.Lerp(0f, Settings.maxWindSoundVolume, speed / 300f);
        windSound.pitch = UnityEngine.Random.Range(.9f, 1f);

        if (direction == 1)
            brakeSound.volume = Mathf.Lerp(0f, Settings.maxBrakeSoundVolume, Mathf.Clamp01((FrontLeftWheelCollider.WheelCollider.brakeTorque + FrontRightWheelCollider.WheelCollider.brakeTorque) / (brakeTorque * 2f)) * Mathf.Lerp(0f, 1f, FrontLeftWheelCollider.WheelCollider.rpm / 50f));
        else
            brakeSound.volume = 0f;

    }

    /// <summary>
    /// Checks slip angles at front and rear wheels to determine if the vehicle is understeering or oversteering. 
    /// Activates ESP assistance if needed.
    /// </summary>
    private void ESPCheck() {

        if (ESPBroken) {

            frontSlip = 0f;
            rearSlip = 0f;
            underSteering = false;
            overSteering = false;
            ESPAct = false;
            return;

        }

        frontSlip = FrontLeftWheelCollider.wheelSlipAmountSideways + FrontRightWheelCollider.wheelSlipAmountSideways;
        rearSlip = RearLeftWheelCollider.wheelSlipAmountSideways + RearRightWheelCollider.wheelSlipAmountSideways;

        if (Mathf.Abs(frontSlip) >= ESPThreshold)
            underSteering = true;
        else
            underSteering = false;

        if (Mathf.Abs(rearSlip) >= ESPThreshold)
            overSteering = true;
        else
            overSteering = false;

        if (overSteering || underSteering)
            ESPAct = true;
        else
            ESPAct = false;

    }

    /// <summary>
    /// Applies engine sound logic across multiple audio sources depending on AudioType. 
    /// Adjusts volume/pitch with engine RPM and throttle.
    /// </summary>
    private void EngineSounds() {

        float lowRPM, medRPM, highRPM;

        if (engineRPM < ((maxEngineRPM) / 2f))
            lowRPM = Mathf.Lerp(0f, 1f, engineRPM / ((maxEngineRPM) / 2f));
        else
            lowRPM = Mathf.Lerp(1f, .25f, engineRPM / maxEngineRPM);

        if (engineRPM < ((maxEngineRPM) / 2f))
            medRPM = Mathf.Lerp(-.5f, 1f, engineRPM / ((maxEngineRPM) / 2f));
        else
            medRPM = Mathf.Lerp(1f, .5f, engineRPM / maxEngineRPM);

        highRPM = Mathf.Lerp(-1f, 1f, engineRPM / maxEngineRPM);

        lowRPM = Mathf.Clamp01(lowRPM) * maxEngineSoundVolume;
        medRPM = Mathf.Clamp01(medRPM) * maxEngineSoundVolume;
        highRPM = Mathf.Clamp01(highRPM) * maxEngineSoundVolume;

        float volumeLevel = Mathf.Clamp(throttleInput, 0f, 1f);
        float pitchLevel = Mathf.Lerp(minEngineSoundPitch, maxEngineSoundPitch, engineRPM / maxEngineRPM) * (engineRunning ? 1f : 0f);

        switch (audioType) {

            case AudioType.OneSource:
                engineSoundHigh.volume = volumeLevel * maxEngineSoundVolume;
                engineSoundHigh.pitch = pitchLevel;

                engineSoundHighOff.volume = (1f - volumeLevel) * maxEngineSoundVolume;
                engineSoundHighOff.pitch = pitchLevel;

                if (engineSoundIdle) {
                    engineSoundIdle.volume = Mathf.Lerp(engineRunning ? idleEngineSoundVolume : 0f, 0f, engineRPM / maxEngineRPM);
                    engineSoundIdle.pitch = pitchLevel;
                }

                if (!engineSoundHigh.isPlaying)
                    engineSoundHigh.Play();
                if (!engineSoundIdle.isPlaying)
                    engineSoundIdle.Play();

                break;

            case AudioType.TwoSource:
                engineSoundHigh.volume = highRPM * volumeLevel;
                engineSoundHigh.pitch = pitchLevel;
                engineSoundLow.volume = lowRPM * volumeLevel;
                engineSoundLow.pitch = pitchLevel;

                engineSoundHighOff.volume = highRPM * (1f - volumeLevel);
                engineSoundHighOff.pitch = pitchLevel;
                engineSoundLowOff.volume = lowRPM * (1f - volumeLevel);
                engineSoundLowOff.pitch = pitchLevel;

                if (engineSoundIdle) {

                    engineSoundIdle.volume = Mathf.Lerp(engineRunning ? idleEngineSoundVolume : 0f, 0f, engineRPM / maxEngineRPM);
                    engineSoundIdle.pitch = pitchLevel;

                }

                if (!engineSoundLow.isPlaying)
                    engineSoundLow.Play();
                if (!engineSoundHigh.isPlaying)
                    engineSoundHigh.Play();
                if (!engineSoundIdle.isPlaying)
                    engineSoundIdle.Play();

                break;

            case AudioType.ThreeSource:
                engineSoundHigh.volume = highRPM * volumeLevel;
                engineSoundHigh.pitch = pitchLevel;
                engineSoundMed.volume = medRPM * volumeLevel;
                engineSoundMed.pitch = pitchLevel;
                engineSoundLow.volume = lowRPM * volumeLevel;
                engineSoundLow.pitch = pitchLevel;

                engineSoundHighOff.volume = highRPM * (1f - volumeLevel);
                engineSoundHighOff.pitch = pitchLevel;
                engineSoundMedOff.volume = medRPM * (1f - volumeLevel);
                engineSoundMedOff.pitch = pitchLevel;
                engineSoundLowOff.volume = lowRPM * (1f - volumeLevel);
                engineSoundLowOff.pitch = pitchLevel;

                if (engineSoundIdle) {

                    engineSoundIdle.volume = Mathf.Lerp(engineRunning ? idleEngineSoundVolume : 0f, 0f, engineRPM / maxEngineRPM);
                    engineSoundIdle.pitch = pitchLevel;

                }

                if (!engineSoundLow.isPlaying)
                    engineSoundLow.Play();
                if (!engineSoundMed.isPlaying)
                    engineSoundMed.Play();
                if (!engineSoundHigh.isPlaying)
                    engineSoundHigh.Play();
                if (!engineSoundIdle.isPlaying)
                    engineSoundIdle.Play();

                break;

            case AudioType.Off:
                if (engineSoundHigh) {

                    engineSoundHigh.volume = 0f;
                    engineSoundHigh.pitch = 1f;

                }

                if (engineSoundMed) {

                    engineSoundMed.volume = 0f;
                    engineSoundMed.pitch = 1f;

                }

                if (engineSoundLow) {

                    engineSoundLow.volume = 0f;
                    engineSoundLow.pitch = 1f;

                }

                if (engineSoundHighOff) {

                    engineSoundHighOff.volume = 0f;
                    engineSoundHighOff.pitch = 1f;

                }

                if (engineSoundMedOff) {

                    engineSoundMedOff.volume = 0f;
                    engineSoundMedOff.pitch = 1f;

                }

                if (engineSoundLowOff) {

                    engineSoundLowOff.volume = 0f;
                    engineSoundLowOff.pitch = 1f;

                }

                if (engineSoundIdle) {

                    engineSoundIdle.volume = 0f;
                    engineSoundIdle.pitch = 1f;

                }

                if (engineSoundLow && engineSoundLow.isPlaying)
                    engineSoundLow.Stop();
                if (engineSoundMed && engineSoundMed.isPlaying)
                    engineSoundMed.Stop();
                if (engineSoundHigh && engineSoundHigh.isPlaying)
                    engineSoundHigh.Stop();
                if (engineSoundIdle && engineSoundIdle.isPlaying)
                    engineSoundIdle.Stop();

                break;
        }
    }

    /// <summary>
    /// Applies torque, brake, and steering to each wheel collider based on input, gear, etc.
    /// </summary>
    private void Wheels() {

        for (int i = 0; i < AllWheelColliders.Length; i++) {

            if (AllWheelColliders[i].canPower)
                AllWheelColliders[i].ApplyMotorTorque((direction * AllWheelColliders[i].powerMultiplier * (1f - clutchInput) * throttleInput * (1f + boostInput) * (engineTorqueCurve.Evaluate(engineRPM) * gears[currentGear].maxRatio * finalRatio)) / Mathf.Clamp(poweredWheels, 1, Mathf.Infinity));

            if (AllWheelColliders[i].canSteer)
                AllWheelColliders[i].ApplySteering(steerInput * AllWheelColliders[i].steeringMultiplier, steerAngle);

            bool appliedBrake = false;

            if (!appliedBrake && handbrakeInput > .5f) {

                appliedBrake = true;

                if (AllWheelColliders[i].canHandbrake)
                    AllWheelColliders[i].ApplyBrakeTorque((brakeTorque * handbrakeInput) * AllWheelColliders[i].handbrakeMultiplier);

            }

            if (!appliedBrake && brakeInput >= .05f) {

                appliedBrake = true;

                if (AllWheelColliders[i].canBrake)
                    AllWheelColliders[i].ApplyBrakeTorque((brakeInput * brakeTorque) * AllWheelColliders[i].brakingMultiplier);

            }

            if (ESPAct)
                appliedBrake = true;

            if (!appliedBrake)
                AllWheelColliders[i].ApplyBrakeTorque(0f);

            if (!AllWheelColliders[i].canPower)
                AllWheelColliders[i].ApplyMotorTorque(0f);
            if (!AllWheelColliders[i].canBrake)
                AllWheelColliders[i].ApplyBrakeTorque(0f);
            if (!AllWheelColliders[i].canSteer)
                AllWheelColliders[i].ApplySteering(0f, 0f);

        }

    }

    /// <summary>
    /// Computes and applies anti-roll forces on the front and rear axles to reduce body roll.
    /// </summary>
    private void AntiRollBars() {

        #region Horizontal
        float travelFL = 1f;
        float travelFR = 1f;

        bool groundedFL = FrontLeftWheelCollider.isGrounded;
        if (groundedFL)
            travelFL = (-FrontLeftWheelCollider.transform.InverseTransformPoint(FrontLeftWheelCollider.wheelHit.point).y - FrontLeftWheelCollider.WheelCollider.radius) / FrontLeftWheelCollider.WheelCollider.suspensionDistance;

        bool groundedFR = FrontRightWheelCollider.isGrounded;
        if (groundedFR)
            travelFR = (-FrontRightWheelCollider.transform.InverseTransformPoint(FrontRightWheelCollider.wheelHit.point).y - FrontRightWheelCollider.WheelCollider.radius) / FrontRightWheelCollider.WheelCollider.suspensionDistance;

        float antiRollForceFrontHorizontal = (travelFL - travelFR) * antiRollFrontHorizontal;

        if (FrontLeftWheelCollider.isActiveAndEnabled && FrontRightWheelCollider.isActiveAndEnabled) {

            if (groundedFL)
                Rigid.AddForceAtPosition(FrontLeftWheelCollider.transform.up * -antiRollForceFrontHorizontal, FrontLeftWheelCollider.transform.position);
            if (groundedFR)
                Rigid.AddForceAtPosition(FrontRightWheelCollider.transform.up * antiRollForceFrontHorizontal, FrontRightWheelCollider.transform.position);

        }

        float travelRL = 1f;
        float travelRR = 1f;

        bool groundedRL = RearLeftWheelCollider.isGrounded;
        if (groundedRL)
            travelRL = (-RearLeftWheelCollider.transform.InverseTransformPoint(RearLeftWheelCollider.wheelHit.point).y - RearLeftWheelCollider.WheelCollider.radius) / RearLeftWheelCollider.WheelCollider.suspensionDistance;

        bool groundedRR = RearRightWheelCollider.isGrounded;
        if (groundedRR)
            travelRR = (-RearRightWheelCollider.transform.InverseTransformPoint(RearRightWheelCollider.wheelHit.point).y - RearRightWheelCollider.WheelCollider.radius) / RearRightWheelCollider.WheelCollider.suspensionDistance;

        float antiRollForceRearHorizontal = (travelRL - travelRR) * antiRollRearHorizontal;

        if (RearLeftWheelCollider.isActiveAndEnabled && RearRightWheelCollider.isActiveAndEnabled) {

            if (groundedRL)
                Rigid.AddForceAtPosition(RearLeftWheelCollider.transform.up * -antiRollForceRearHorizontal, RearLeftWheelCollider.transform.position);
            if (groundedRR)
                Rigid.AddForceAtPosition(RearRightWheelCollider.transform.up * antiRollForceRearHorizontal, RearRightWheelCollider.transform.position);

        }
        #endregion

        #region Vertical
        float antiRollForceFrontVertical = (travelFL - travelRL) * antiRollVertical;
        if (FrontLeftWheelCollider.isActiveAndEnabled && RearLeftWheelCollider.isActiveAndEnabled) {

            if (groundedFL)
                Rigid.AddForceAtPosition(FrontLeftWheelCollider.transform.up * -antiRollForceFrontVertical, FrontLeftWheelCollider.transform.position);
            if (groundedRL)
                Rigid.AddForceAtPosition(RearLeftWheelCollider.transform.up * antiRollForceFrontVertical, RearLeftWheelCollider.transform.position);

        }

        float antiRollForceRearVertical = (travelFR - travelRR) * antiRollVertical;
        if (FrontRightWheelCollider.isActiveAndEnabled && RearRightWheelCollider.isActiveAndEnabled) {

            if (groundedFR)
                Rigid.AddForceAtPosition(FrontRightWheelCollider.transform.up * -antiRollForceRearVertical, FrontRightWheelCollider.transform.position);
            if (groundedRR)
                Rigid.AddForceAtPosition(RearRightWheelCollider.transform.up * antiRollForceRearVertical, RearRightWheelCollider.transform.position);

        }
        #endregion
    }

    /// <summary>
    /// Checks if at least one wheel is grounded. Sets isGrounded accordingly.
    /// </summary>
    private void CheckGrounded() {

        bool grounded = false;

        for (int i = 0; i < AllWheelColliders.Length; i++) {

            if (AllWheelColliders[i].WheelCollider.isGrounded)
                grounded = true;

        }

        isGrounded = grounded;

    }

    /// <summary>
    /// If engineRPM surpasses maxEngineRPM, forcibly cuts throttle to emulate a rev limiter effect.
    /// </summary>
    private void RevLimiter() {

        if (engineRPM >= maxEngineRPM * .985f)
            cutGas = true;
        else
            cutGas = false;

    }

    /// <summary>
    /// Manages nitrous usage (NoS) if available. Consumes NoS while active and regenerates after waiting.
    /// </summary>
    private void NOS() {

        if (!NOSSound)
            NOSSound = NewAudioSource(Settings.audioMixer, gameObject, exhaustSoundPosition, "NOS Sound AudioSource", 5f, 10f, .5f, NOSClip, true, false, false);
        if (!blowSound)
            blowSound = NewAudioSource(Settings.audioMixer, gameObject, exhaustSoundPosition, "NOS Blow", 1f, 10f, .5f, null, false, false, false);

        if (boostInput >= .8f && throttleInput >= .8f && NoS > 5) {

            NoS -= NoSConsumption * Time.fixedDeltaTime;
            NoSRegenerateTime = 0f;

            if (!NOSSound.isPlaying)
                NOSSound.Play();

        } else {

            if (NoS < 100 && NoSRegenerateTime >= 3)
                NoS += (NoSConsumption / 1.5f) * Time.fixedDeltaTime;

            NoSRegenerateTime += Time.fixedDeltaTime;

            if (NOSSound.isPlaying) {

                NOSSound.Stop();
                blowSound.clip = BlowClip[UnityEngine.Random.Range(0, BlowClip.Length)];
                blowSound.Play();

            }

        }

    }

    /// <summary>
    /// Manages turbo spool logic if assigned, adjusting spool sound volume & pitch with usage.
    /// </summary>
    private void Turbo() {

        if (!turboSound) {

            turboSound = NewAudioSource(Settings.audioMixer, gameObject, turboSoundPosition, "Turbo Sound AudioSource", .1f, .5f, 0, TurboClip, true, true, false);
            NewHighPassFilter(turboSound, 10000f, 10);

        }

        turboBoost = Mathf.Lerp(turboBoost, Mathf.Clamp(Mathf.Pow(throttleInput, 10) * 30f + Mathf.Pow(engineRPM / maxEngineRPM, 10) * 30f, 0f, 30f), Time.fixedDeltaTime * 10f);

        if (turboBoost >= 25f) {

            if (turboBoost < (turboSound.volume * 30f)) {

                if (!blowSound.isPlaying) {

                    blowSound.clip = Settings.blowoutClip[UnityEngine.Random.Range(0, Settings.blowoutClip.Length)];
                    blowSound.Play();

                }

            }

        }

        turboSound.volume = Mathf.Lerp(turboSound.volume, turboBoost / 30f, Time.fixedDeltaTime * 5f);
        turboSound.pitch = Mathf.Lerp(Mathf.Clamp(turboSound.pitch, 2f, 3f), (turboBoost / 30f) * 2f, Time.fixedDeltaTime * 5f);

    }

    /// <summary>
    /// If fuel consumption is enabled, reduce the fuelTank over time as engine runs.
    /// </summary>
    private void Fuel() {

        fuelTank -= ((engineRPM / 10000f) * fuelConsumptionRate) * Time.fixedDeltaTime;
        fuelTank = Mathf.Clamp(fuelTank, 0f, fuelTankCapacity);

        if (fuelTank <= 0f)
            fuelInput = 0f;

    }

    /// <summary>
    /// Accumulates engine heat with RPM, and cools it past a threshold. 
    /// Used for advanced temperature mechanics.
    /// </summary>
    private void EngineHeat() {

        engineHeat += ((engineRPM / 10000f) * engineHeatRate) * Time.fixedDeltaTime;

        if (engineHeat > engineCoolingWaterThreshold)
            engineHeat -= engineCoolRate * Time.fixedDeltaTime;

        engineHeat -= (engineCoolRate / 10f) * Time.fixedDeltaTime;
        engineHeat = Mathf.Clamp(engineHeat, 15f, 120f);

    }

    /// <summary>
    /// If speed is low and vehicle is upside down for a while, automatically resets orientation.
    /// </summary>
    private void CheckReset() {

        if (!Settings.autoReset)
            return;

        if (speed < 5 && !Rigid.isKinematic) {

            if (transform.eulerAngles.z < 300 && transform.eulerAngles.z > 60) {

                resetTime += Time.deltaTime;

                if (resetTime > 3) {

                    transform.SetPositionAndRotation(new Vector3(transform.position.x, transform.position.y + 3, transform.position.z), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
                    resetTime = 0f;

                }

            }

        }

    }

    /// <summary>
    /// Provides a steering limiter or counter-steering logic.
    /// </summary>
    private void SteerHelper() {

        if (!isGrounded)
            return;

        if (!steeringDirection || !velocityDirection) {

            if (!steeringDirection) {
                GameObject steeringDirectionGO = new GameObject("Steering Direction");
                steeringDirectionGO.transform.SetParent(transform, false);
                steeringDirection = steeringDirectionGO.transform;
                steeringDirectionGO.transform.localPosition = new Vector3(1f, 2f, 0f);
                steeringDirectionGO.transform.localScale = new Vector3(.1f, .1f, 3f);

            }

            if (!velocityDirection) {

                GameObject velocityDirectionGO = new GameObject("Velocity Direction");
                velocityDirectionGO.transform.SetParent(transform, false);
                velocityDirection = velocityDirectionGO.transform;
                velocityDirectionGO.transform.localPosition = new Vector3(-1f, 2f, 0f);
                velocityDirectionGO.transform.localScale = new Vector3(.1f, .1f, 3f);

            }

            return;

        }

        for (int i = 0; i < AllWheelColliders.Length; i++) {

            if (AllWheelColliders[i].wheelHit.point == Vector3.zero)
                return;

        }

        Vector3 v = Rigid.angularVelocity;
        velocityAngle = (v.y * Mathf.Clamp(transform.InverseTransformDirection(Rigid.linearVelocity).z, -1f, 1f)) * Mathf.Rad2Deg;
        velocityDirection.localRotation = Quaternion.Lerp(velocityDirection.localRotation, Quaternion.AngleAxis(Mathf.Clamp(velocityAngle / 3f, -45f, 45f), Vector3.up), Time.fixedDeltaTime * 20f);
        steeringDirection.localRotation = Quaternion.Euler(0f, FrontLeftWheelCollider.WheelCollider.steerAngle, 0f);

        int normalizer;

        if (steeringDirection.localRotation.y > velocityDirection.localRotation.y)
            normalizer = 1;
        else
            normalizer = -1;

        float angle2 = Quaternion.Angle(velocityDirection.localRotation, steeringDirection.localRotation) * (normalizer);
        Rigid.AddRelativeTorque(Vector3.up * ((angle2 * (Mathf.Clamp(transform.InverseTransformDirection(Rigid.linearVelocity).z, -10f, 10f) / 1000f)) * steerHelperAngularVelStrength), ForceMode.VelocityChange);

        if (Mathf.Abs(oldRotation - transform.eulerAngles.y) < 10f) {

            float turnadjust = (transform.eulerAngles.y - oldRotation) * (steerHelperLinearVelStrength / 2f);
            Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
            Rigid.linearVelocity = (velRotation * Rigid.linearVelocity);

        }

        oldRotation = transform.eulerAngles.y;

    }

    /// <summary>
    /// Slightly adjusts side slip stiffness on the front wheels based on yaw velocity. 
    /// Helps reduce over-rotation/spin at moderate slip angles.
    /// </summary>
    private void TractionHelper() {

        if (!isGrounded)
            return;

        Vector3 velocity = Rigid.linearVelocity;
        velocity -= transform.up * Vector3.Dot(velocity, transform.up);
        velocity.Normalize();

        angle = -Mathf.Asin(Vector3.Dot(Vector3.Cross(transform.forward, velocity), transform.up));
        angularVelo = Rigid.angularVelocity.y;

        if (angle * FrontLeftWheelCollider.WheelCollider.steerAngle < 0)
            FrontLeftWheelCollider.tractionHelpedSidewaysStiffness = (1f - Mathf.Clamp01(tractionHelperStrength * Mathf.Abs(angularVelo)));
        else
            FrontLeftWheelCollider.tractionHelpedSidewaysStiffness = 1f;

        if (angle * FrontRightWheelCollider.WheelCollider.steerAngle < 0)
            FrontRightWheelCollider.tractionHelpedSidewaysStiffness = (1f - Mathf.Clamp01(tractionHelperStrength * Mathf.Abs(angularVelo)));
        else
            FrontRightWheelCollider.tractionHelpedSidewaysStiffness = 1f;

    }

    /// <summary>
    /// Dynamically adjusts <c>Rigid.angularDrag</c> based on vehicle speed to mitigate spin.
    /// </summary>
    private void AngularDragHelper() {

        Rigid.angularDamping = Mathf.Lerp(0f, 10f, (speed * angularDragHelperStrength) / 1000f);

    }

    /// <summary>
    /// If UseAutomaticClutch is true, smoothly sets clutch input in first gear or while shifting.
    /// </summary>
    private void AutomaticClutch() {

        if (!automaticClutch)
            return;

        float tractionRPM = 0;

        for (int i = 0; i < AllWheelColliders.Length; i++) {

            if (AllWheelColliders[i].canPower)
                tractionRPM += Mathf.Abs(AllWheelColliders[i].WheelCollider.rpm);

        }

        if (currentGear == 0) {

            if (launched >= .25f)
                clutchInput = Mathf.Lerp(clutchInput, (Mathf.Lerp(1f, (Mathf.Lerp(clutchInertia, 0f, (tractionRPM / Mathf.Clamp(poweredWheels, 1, Mathf.Infinity)) / gears[0].targetSpeedForNextGear)), Mathf.Abs(throttleInput))), Time.fixedDeltaTime * 20f);
            else
                clutchInput = Mathf.Lerp(clutchInput, 1f / speed, Time.fixedDeltaTime * 20f);

        } else {

            if (changingGear)
                clutchInput = Mathf.Lerp(clutchInput, 1, Time.fixedDeltaTime * 20f);
            else
                clutchInput = Mathf.Lerp(clutchInput, 0, Time.fixedDeltaTime * 20f);

        }

        if (cutGas || handbrakeInput >= .1f)
            clutchInput = 1f;

        if (NGear)
            clutchInput = 1f;

        clutchInput = Mathf.Clamp01(clutchInput);

    }

    /// <summary>
    /// Automatically manages shifting gears up/down if in AutomaticGear mode. 
    /// Also manages reversing sound if direction is -1.
    /// </summary>
    private void AutomaticGearbox() {

        if (currentGear < gears.Length - 1 && !changingGear) {

            if (direction == 1 && speed >= gears[currentGear].targetSpeedForNextGear && engineRPM >= gearShiftUpRPM) {

                if (!semiAutomaticGear)
                    StartCoroutine(ChangeGear(currentGear + 1));
                else if (semiAutomaticGear && direction != -1)
                    StartCoroutine(ChangeGear(currentGear + 1));

            }

        }

        if (currentGear > 0 && !changingGear) {

            if (direction != -1 && speed < gears[currentGear - 1].targetSpeedForNextGear && engineRPM <= gearShiftDownRPM)
                StartCoroutine(ChangeGear(currentGear - 1));

        }

        if (direction == -1) {

            if (!reversingSound.isPlaying)
                reversingSound.Play();

            reversingSound.volume = Mathf.Lerp(0f, 1f, speed / gears[0].maxSpeed);
            reversingSound.pitch = reversingSound.volume;

        } else {

            if (reversingSound.isPlaying)
                reversingSound.Stop();

            reversingSound.volume = 0f;
            reversingSound.pitch = 0f;

        }

    }

    /// <summary>
    /// Executes the gear change process with a short delay. 
    /// Cuts gas, optionally plays gear shift sound, and sets currentGear to the new value.
    /// </summary>
    public IEnumerator ChangeGear(int gear) {

        changingGear = true;

        if (Settings.useTelemetry)
            print("Shifted to: " + (gear).ToString());

        if (GearShiftingClips.Length > 0) {

            gearShiftingSound = NewAudioSource(Settings.audioMixer, gameObject, gearSoundPosition, "Gear Shifting AudioSource", 1f, 5f, Settings.maxGearShiftingSoundVolume, GearShiftingClips[UnityEngine.Random.Range(0, GearShiftingClips.Length)], false, true, true);

            if (!gearShiftingSound.isPlaying)
                gearShiftingSound.Play();

        }

        yield return new WaitForSeconds(gearShiftingDelay);

        if (gear == -1) {

            currentGear = 0;

            if (!NGear)
                direction = -1;
            else
                direction = 0;

        } else {

            currentGear = gear;
            if (!NGear)
                direction = 1;
            else
                direction = 0;

        }

        changingGear = false;

    }

    /// <summary>
    /// Manually shifts up one gear if possible. 
    /// Called from external input or UI.
    /// </summary>
    public void GearShiftUp() {

        if (currentGear < gears.Length - 1 && !changingGear) {

            if (direction != -1)
                StartCoroutine(ChangeGear(currentGear + 1));
            else
                StartCoroutine(ChangeGear(0));

        }

    }

    /// <summary>
    /// Manually shifts to the specified gear index. 
    /// Negative gear = reverse, -2 = neutral.
    /// </summary>
    public void GearShiftTo(int gear) {

        if (gear < -1 || gear >= gears.Length)
            return;
        if (gear == currentGear)
            return;

        StartCoroutine(ChangeGear(gear));

    }

    /// <summary>
    /// Manually shifts down one gear if possible.
    /// </summary>
    public void GearShiftDown() {

        if (currentGear >= 0)
            StartCoroutine(ChangeGear(currentGear - 1));

    }

    #region Collisions

    private void OnCollisionEnter(Collision collision) {

        if (collision.contactCount < 1)
            return;

        if (collision.relativeVelocity.magnitude < 5)
            return;

        if (OnRCCPlayerCollision != null && this == RCCSceneManager.activePlayerVehicle)
            OnRCCPlayerCollision(this, collision);

        if (((1 << collision.gameObject.layer) & damage.damageFilter) != 0) {

            if (useDamage) {

                if (!damage.carController)
                    damage.Initialize(this);

                damage.OnCollision(collision);

            }

            if (useCollisionAudio) {

                if (CrashClips.Length > 0)
                    crashSound = NewAudioSource(Settings.audioMixer, gameObject, "Crash Sound AudioSource", 5f, 20f, Settings.maxCrashSoundVolume * (collision.impulse.magnitude / 10000f), CrashClips[UnityEngine.Random.Range(0, CrashClips.Length)], false, true, true);

            }

            if (useCollisionParticles) {

                if (contactSparkle && contactSparkeList.Count < 1) {

                    for (int i = 0; i < maximumContactSparkle; i++) {

                        GameObject sparks = Instantiate(contactSparkle, transform.position, Quaternion.identity);
                        sparks.transform.SetParent(allContactParticles.transform);
                        contactSparkeList.Add(sparks.GetComponent<ParticleSystem>());
                        ParticleSystem.EmissionModule em = sparks.GetComponent<ParticleSystem>().emission;
                        em.enabled = false;

                    }

                }

                for (int i = 0; i < contactSparkeList.Count; i++) {

                    if (!contactSparkeList[i].isPlaying) {

                        contactSparkeList[i].transform.position = collision.GetContact(0).point;
                        ParticleSystem.EmissionModule em = contactSparkeList[i].emission;
                        em.rateOverTimeMultiplier = collision.impulse.magnitude / 500f;
                        em.enabled = true;
                        contactSparkeList[i].Play();
                        break;

                    }

                }

            }

        }

    }

    private void OnCollisionStay(Collision collision) {

        if (collision.contactCount < 1 || collision.relativeVelocity.magnitude < 2f) {

            if (scratchSparkeList != null) {

                for (int i = 0; i < scratchSparkeList.Count; i++) {

                    ParticleSystem.EmissionModule em = scratchSparkeList[i].emission;
                    em.enabled = false;

                }

            }

            return;

        }

        if (((1 << collision.gameObject.layer) & damage.damageFilter) != 0) {

            if (useCollisionParticles) {

                if (scratchSparkle && scratchSparkeList.Count < 1) {

                    for (int i = 0; i < maximumContactSparkle; i++) {

                        GameObject sparks = Instantiate(scratchSparkle, transform.position, Quaternion.identity);
                        sparks.transform.SetParent(allContactParticles.transform);
                        scratchSparkeList.Add(sparks.GetComponent<ParticleSystem>());
                        ParticleSystem.EmissionModule em = sparks.GetComponent<ParticleSystem>().emission;
                        em.enabled = false;

                    }

                }

                ContactPoint[] contacts = new ContactPoint[collision.contactCount];
                collision.GetContacts(contacts);

                int ind = -1;
                foreach (ContactPoint cp in contacts) {

                    ind++;

                    if (ind < scratchSparkeList.Count && !scratchSparkeList[ind].isPlaying) {

                        scratchSparkeList[ind].transform.position = cp.point;
                        ParticleSystem.EmissionModule em = scratchSparkeList[ind].emission;
                        em.enabled = true;
                        em.rateOverTimeMultiplier = collision.relativeVelocity.magnitude / 1f;
                        scratchSparkeList[ind].Play();

                    }

                }

            }

        }

    }

    private void OnCollisionExit(Collision collision) {

        for (int i = 0; i < scratchSparkeList.Count; i++) {

            ParticleSystem.EmissionModule em = scratchSparkeList[i].emission;
            em.enabled = false;
            scratchSparkeList[i].Stop();

        }

    }

    #endregion

    /// <summary>
    /// Allows the vehicle to spin the wheels and blow smoke for previewing or burnout show.
    /// If state = true, sets canControl to false, Rigid kinematic, and permanentGas = true.
    /// </summary>
    public void PreviewSmokeParticle(bool state) {

        canControl = state;
        permanentGas = state;
        Rigid.isKinematic = state;

    }

    /// <summary>
    /// Detaches the truck trailer if one is attached to this vehicle.
    /// </summary>
    public void DetachTrailer() {

        if (!attachedTrailer)
            return;

        attachedTrailer.DetachTrailer();

    }

    private void OnDestroy() {

        if (OnRCCPlayerDestroyed != null)
            OnRCCPlayerDestroyed(this);

        damage.Cleanup();

    }

    /// <summary>
    /// Enables or disables player control for this vehicle.
    /// </summary>
    /// <param name="state">If true, can control the vehicle with standard inputs.</param>
    public void SetCanControl(bool state) {

        canControl = state;

    }

    /// <summary>
    /// Toggles external AI control. If true, user input is ignored in favor of script-driven input.
    /// </summary>
    public void SetExternalControl(bool state) {

        externalController = state;

    }

    /// <summary>
    /// Starts or kills the engine based on the provided boolean.
    /// </summary>
    public void SetEngine(bool state) {

        if (state)
            StartEngine();
        else
            KillEngine();

    }

    /// <summary>
    /// Repairs damage on this vehicle immediately, resetting deformation if applicable.
    /// </summary>
    public void Repair() {

        damage.repairNow = true;

    }

    #region InputManager Events

    private void RCC_InputManager_OnTrailerDetach() {

        if (!canControl || externalController)
            return;

        DetachTrailer();

    }

    private void RCC_InputManager_OnGearShiftDown() {

        if (!canControl || externalController)
            return;

        GearShiftDown();

    }

    private void RCC_InputManager_OnGearShiftUp() {

        if (!canControl || externalController)
            return;

        GearShiftUp();

    }

    private void RCC_InputManager_OnNGear(bool state) {

        if (!canControl || externalController)
            return;

        NGear = state;

    }

    private void RCC_InputManager_OnIndicatorHazard() {

        if (!canControl || externalController)
            return;

        if (indicatorsOn != IndicatorsOn.All)
            indicatorsOn = IndicatorsOn.All;
        else
            indicatorsOn = IndicatorsOn.Off;

    }

    private void RCC_InputManager_OnIndicatorRight() {

        if (!canControl || externalController)
            return;

        if (indicatorsOn != IndicatorsOn.Right)
            indicatorsOn = IndicatorsOn.Right;
        else
            indicatorsOn = IndicatorsOn.Off;

    }

    private void RCC_InputManager_OnIndicatorLeft() {

        if (!canControl || externalController)
            return;

        if (indicatorsOn != IndicatorsOn.Left)
            indicatorsOn = IndicatorsOn.Left;
        else
            indicatorsOn = IndicatorsOn.Off;

    }

    private void RCC_InputManager_OnHighBeamHeadlights() {

        if (!canControl || externalController)
            return;

        highBeamHeadLightsOn = !highBeamHeadLightsOn;

    }

    private void RCC_InputManager_OnLowBeamHeadlights() {

        if (!canControl || externalController)
            return;

        lowBeamHeadLightsOn = !lowBeamHeadLightsOn;

    }

    private void RCC_InputManager_OnInteriorLights() {

        if (!canControl || externalController)
            return;

        interiorLightsOn = !interiorLightsOn;

    }

    private void RCC_InputManager_OnStartStopEngine() {

        if (!canControl || externalController)
            return;

        KillOrStartEngine();

    }

    #endregion

    #region Overriding Inputs

    /// <summary>
    /// Directly overrides all vehicle inputs with those provided in newInputs.
    /// The vehicle will ignore normal player input until <c>DisableOverrideInputs</c> is called.
    /// </summary>
    public void OverrideInputs(RCC_Inputs newInputs) {

        overrideInputs = true;
        inputs = newInputs;

    }

    /// <summary>
    /// Overloads <c>OverrideInputs</c> with an additional flag to toggle external controller mode.
    /// </summary>
    public void OverrideInputs(RCC_Inputs newInputs, bool enableExternalController) {

        overrideInputs = true;
        inputs = newInputs;
        externalController = enableExternalController;

    }

    /// <summary>
    /// Disables the manual input overrides, allowing the vehicle to receive normal input again.
    /// </summary>
    public void DisableOverrideInputs() {

        overrideInputs = false;

    }

    /// <summary>
    /// Disables both overrideInputs and optionally the externalController.
    /// </summary>
    public void DisableOverrideInputs(bool disableExternalController) {

        overrideInputs = false;
        externalController = !disableExternalController;

    }

    #endregion

    /// <summary>
    /// Moves the COM object to the approximate center of the vehicle’s bounding box (for quick setup).
    /// </summary>
    public void CheckCOMPosition() {

        if (!COM)
            return;

        Bounds bounds = RCC_GetBounds.GetBounds(transform);
        COM.transform.position = bounds.center;
        COM.transform.position -= transform.up * (bounds.size.y / 5f);
        COM.transform.position += transform.forward * (bounds.size.z / 35f);

    }

#if BCG_URP
    /// <summary>
    /// If using URP lens flares, this replaces standard lens flare components with SRP-based lens flares in any child RCC_Light.
    /// </summary>
    public bool CheckLensflares_URP() {

        RCC_Light[] allLights = GetComponentsInChildren<RCC_Light>(true);
        bool proccessed = false;

        for (int i = 0; i < allLights.Length; i++) {

            if (allLights[i].TryGetComponent(out LensFlare foundFlare)) {
                proccessed = true;
                DestroyImmediate(foundFlare);
                allLights[i].lensFlare = null;

            }

            if (!allLights[i].TryGetComponent(out LensFlareComponentSRP foundURPFlare)) {

                proccessed = true;
                foundURPFlare = allLights[i].gameObject.AddComponent<LensFlareComponentSRP>();
                foundURPFlare.intensity = 0f;
                foundURPFlare.lensFlareData = Settings.lensflareURP as LensFlareDataSRP;
                foundURPFlare.attenuationByLightShape = false;
                foundURPFlare.useOcclusion = false;
                allLights[i].lensFlareURP = foundURPFlare;

            }

        }

        if (proccessed)
            Debug.Log("Old lensflare components have been found in the " + transform.name + ", replacing them with SRP lensflares.");

        return proccessed;

    }
#endif

    private bool IsURP() {

        RenderPipelineAsset activePipeline = GraphicsSettings.currentRenderPipeline;

        if (activePipeline != null && activePipeline.GetType().ToString().Contains("Universal"))
            return true;

        return false;

    }


    private void OnDisable() {

        // Unsubscribe from behavior change events.
        RCC_SceneManager.OnBehaviorChanged -= CheckBehavior;

        // Unsubscribe from input manager events.
        RCC_InputManager.OnStartStopEngine -= RCC_InputManager_OnStartStopEngine;
        RCC_InputManager.OnLowBeamHeadlights -= RCC_InputManager_OnLowBeamHeadlights;
        RCC_InputManager.OnHighBeamHeadlights -= RCC_InputManager_OnHighBeamHeadlights;
        RCC_InputManager.OnIndicatorLeft -= RCC_InputManager_OnIndicatorLeft;
        RCC_InputManager.OnIndicatorRight -= RCC_InputManager_OnIndicatorRight;
        RCC_InputManager.OnIndicatorHazard -= RCC_InputManager_OnIndicatorHazard;
        RCC_InputManager.OnInteriorlights -= RCC_InputManager_OnInteriorLights;
        RCC_InputManager.OnGearShiftUp -= RCC_InputManager_OnGearShiftUp;
        RCC_InputManager.OnGearShiftDown -= RCC_InputManager_OnGearShiftDown;
        RCC_InputManager.OnNGear -= RCC_InputManager_OnNGear;
        RCC_InputManager.OnTrailerDetach -= RCC_InputManager_OnTrailerDetach;

    }

    public void OverrideRPM(float targetRPM) {

        overridingEngineRPM = true;
        engineRPM = targetRPM;
        engineRPMRaw = engineRPM;

    }

    public void DisableOverrideRPM() {

        overridingEngineRPM = false;

    }

    /// <summary>
    /// Resets core settings in the editor, creating a default Rigidbody and COM if missing. 
    /// Also sets typical values from RCC_InitialSettings for quick setup.
    /// </summary>
    private void Reset() {

        Rigidbody rigid = GetComponent<Rigidbody>();

        if (!rigid)
            rigid = gameObject.AddComponent<Rigidbody>();

        rigid.mass = RCC_InitialSettings.Instance.mass;
        rigid.linearDamping = RCC_InitialSettings.Instance.drag;
        rigid.angularDamping = RCC_InitialSettings.Instance.angularDrag;
        rigid.interpolation = RCC_InitialSettings.Instance.interpolation;
        rigid.collisionDetectionMode = RCC_InitialSettings.Instance.collisionDetectionMode;

        if (transform.Find("COM"))
            DestroyImmediate(transform.Find("COM").gameObject);

        if (transform.Find("Wheel Colliders"))
            DestroyImmediate(transform.Find("Wheel Colliders").gameObject);

        GameObject newCOM = new GameObject("COM");
        newCOM.transform.parent = transform;
        newCOM.transform.localPosition = Vector3.zero;
        newCOM.transform.localRotation = Quaternion.identity;
        newCOM.transform.localScale = Vector3.one;
        COM = newCOM.transform;

        ReCreateEngineTorqueCurve();
        steerAngleCurve = new AnimationCurve(new Keyframe(0f, 40f, 0f, -.3f), new Keyframe(120f, 10f, -.115f, -.1f), new Keyframe(200f, 7f));

        if (Settings.RCCLayer != "") {

            foreach (Transform item in GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = LayerMask.NameToLayer(Settings.RCCLayer);

        }

    }

}
