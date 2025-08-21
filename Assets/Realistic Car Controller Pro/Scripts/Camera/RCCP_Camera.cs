//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main RCCP Camera controller. Includes 6 different camera modes with many customizable settings. 
/// It doesn't use different cameras on your scene like *other* assets. Simply it parents the camera to their positions, that's all. 
/// No need to be Einstein. Also supports collision detection.
/// Summary: This class manages various camera modes (TPS, FPS, Wheel, Fixed, Cinematic, Top, TruckTrailer),
/// camera transitions, occlusion checks, orbit controls, auto-focus, collisions, and field of view adjustments.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Camera")]
public class RCCP_Camera : RCCP_GenericComponent {

    /// <summary>
    /// Camera target with custom class.
    /// Summary: Holds references to the car controller and child cameras (hood, wheel).
    /// </summary>
    [System.Serializable]
    public class CameraTarget {

        /// <summary>
        /// Summary: The active vehicle for this camera target.
        /// </summary>
        public RCCP_CarController playerVehicle;

        /// <summary>
        /// Summary: Cached reference to the hood camera found on the vehicle.
        /// </summary>
        private RCCP_HoodCamera _hoodCamera;
        public RCCP_HoodCamera HoodCamera {

            get {

                if (!playerVehicle)
                    return null;

                if (!_hoodCamera)
                    _hoodCamera = playerVehicle.GetComponentInChildren<RCCP_HoodCamera>();

                return _hoodCamera;

            }

        }

        /// <summary>
        /// Summary: Cached reference to the wheel camera found on the vehicle.
        /// </summary>
        private RCCP_WheelCamera _wheelCamera;
        public RCCP_WheelCamera WheelCamera {

            get {

                if (!playerVehicle)
                    return null;

                if (!_wheelCamera)
                    _wheelCamera = playerVehicle.GetComponentInChildren<RCCP_WheelCamera>();

                return _wheelCamera;

            }

        }

    }

    /// <summary>
    /// Summary: Returns the position of the player vehicle, optionally using CenterPosition if calculateCenterPosition is true.
    /// </summary>
    public Vector3 GetPlayerPosition {
        get {

            if (cameraTarget == null)
                return Vector3.zero;

            if (cameraTarget.playerVehicle == null)
                return Vector3.zero;

            if (cameraTarget == null)
                return Vector3.zero;

            if (!calculateCenterPosition)
                return cameraTarget.playerVehicle.transform.position;

            return cameraTarget.playerVehicle.CenterPosition;

        }

    }

    /// <summary>
    /// Summary: Returns the rotation of the player vehicle.
    /// </summary>
    public Quaternion GetPlayerRotation {
        get {

            if (cameraTarget == null)
                return Quaternion.identity;

            if (cameraTarget.playerVehicle == null)
                return Quaternion.identity;

            if (cameraTarget == null)
                return Quaternion.identity;

            return cameraTarget.playerVehicle.transform.rotation;

        }

    }

    /// <summary>
    /// Actual camera target with custom class. Holds references to the car controller, hood, and wheel cameras.
    /// </summary>
    public CameraTarget cameraTarget = new CameraTarget();

    /// <summary>
    /// Summary: Indicates if the camera is actively rendering. If false, the camera won't be shown.
    /// </summary>
    public bool isRendering = true;

    /// <summary>
    /// Summary: Reference to the actual Camera component. It's a child of this GameObject's pivot.
    /// </summary>
    public Camera actualCamera;

    /// <summary>
    /// Summary: Pivot center of the camera. Used for making offsets and collision movements.
    /// </summary>
    public GameObject pivot;

    /// <summary>
    /// Summary: Received inputs from the player (mouse, scroll, etc.).
    /// </summary>
    public RCCP_Inputs inputs;

    /// <summary>
    /// Summary: If true, uses the center of the vehicle for positioning instead of transform.position.
    /// </summary>
    public bool calculateCenterPosition = true;

    /// <summary>
    /// Summary: Enumeration of all camera modes available in RCCP_Camera.
    /// </summary>
    public enum CameraMode { TPS, FPS, WHEEL, FIXED, CINEMATIC, TOP, TRUCKTRAILER }

    /// <summary>
    /// Summary: Currently selected camera mode.
    /// </summary>
    public CameraMode cameraMode = CameraMode.TPS;

    /// <summary>
    /// Last camera mode before this frame. Used to check when the camera mode has changed.
    /// </summary>
    private CameraMode lastCameraMode = CameraMode.TPS;

    /// <summary>
    /// Summary: Enumeration for two types of TPS modes (TPS1, TPS2).
    /// TPS1 is the original; TPS2 is a more stable approach.
    /// </summary>
    public enum TPSMode { TPS1, TPS2 }

    /// <summary>
    /// Summary: Chooses which TPS mode to use.
    /// </summary>
    public TPSMode tPSMode = TPSMode.TPS2;

    /// <summary>
    /// Summary: Singleton reference to the RCCP_FixedCamera instance.
    /// </summary>
    private RCCP_FixedCamera FixedCamera { get { return RCCP_FixedCamera.Instance; } }

    /// <summary>
    /// Summary: Singleton reference to the RCCP_CinematicCamera instance.
    /// </summary>
    private RCCP_CinematicCamera CinematicCamera { get { return RCCP_CinematicCamera.Instance; } }

    /// <summary>
    /// Summary: If true, locks the X rotation of the camera to the vehicle's X.
    /// </summary>
    public bool TPSLockX = true;

    /// <summary>
    /// Summary: If true, locks the Y rotation of the camera to the vehicle's Y.
    /// </summary>
    public bool TPSLockY = true;

    /// <summary>
    /// Summary: If true, locks the Z rotation of the camera to the vehicle's Z.
    /// </summary>
    public bool TPSLockZ = false;

    /// <summary>
    /// Summary: Camera rotation won't track the vehicle if the vehicle is not grounded (in free fall).
    /// </summary>
    public bool TPSFreeFall = true;

    /// <summary>
    /// Summary: If true, adjusts distance, height, and pitch angle relative to vehicle velocity for dynamic camera behavior.
    /// </summary>
    public bool TPSDynamic = false;

    /// <summary>
    /// Summary: Enables or disables top camera mode usage when cycling cameras.
    /// </summary>
    public bool useTopCameraMode = false;

    /// <summary>
    /// Summary: Enables or disables hood camera mode usage when cycling cameras.
    /// </summary>
    public bool useHoodCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables orbit control in TPS camera mode.
    /// </summary>
    public bool useOrbitInTPSCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables orbit control in hood camera mode.
    /// </summary>
    public bool useOrbitInHoodCameraMode = true;

    /// <summary>
    /// Summary: If true, orbit control only works while mouse button (or UI) is held.
    /// </summary>
    public bool useOrbitOnlyHolding = true;

    /// <summary>
    /// Summary: Indicates whether player is currently holding the orbit button (mouse/touch).
    /// </summary>
    public bool orbitHolding = false;

    /// <summary>
    /// Summary: Enables or disables wheel camera mode usage when cycling cameras.
    /// </summary>
    public bool useWheelCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables fixed camera mode usage when cycling cameras.
    /// </summary>
    public bool useFixedCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables cinematic camera mode usage when cycling cameras.
    /// </summary>
    public bool useCinematicCameraMode = true;

    /// <summary>
    /// Summary: If true, uses an orthographic camera for the top camera mode.
    /// </summary>
    public bool useOrthoForTopCamera = false;

    /// <summary>
    /// Summary: If true, checks for occlusion between camera and vehicle to avoid clipping or obstacles.
    /// </summary>
    public bool useOcclusion = true;

    /// <summary>
    /// Summary: Camera will be occluded by these layers (SphereCast).
    /// </summary>
    public LayerMask occlusionLayerMask = -1;

    /// <summary>
    /// Summary: Stores the occluded position if an obstacle is detected.
    /// </summary>
    private Vector3 occluded = Vector3.zero;

    /// <summary>
    /// Summary: If true, camera mode will change automatically (like every 10 seconds) for cinematic effect.
    /// </summary>
    public bool useAutoChangeCamera = false;

    /// <summary>
    /// Summary: A timer used for auto-switching camera modes if useAutoChangeCamera is true.
    /// </summary>
    public float autoChangeCameraTimer = 0f;

    /// <summary>
    /// Summary: Predefined angle for top camera mode.
    /// </summary>
    public Vector3 topCameraAngle = new Vector3(45f, 45f, 0f);

    /// <summary>
    /// Summary: Distance from the target in top camera mode.
    /// </summary>
    public float topCameraDistance = 100f;

    /// <summary>
    /// Summary: Maximum forward offset in top camera mode, based on vehicle speed.
    /// </summary>
    [Min(0f)] public float maximumZDistanceOffset = 10f;

    /// <summary>
    /// Summary: Internally used offset for top camera mode, recalculated based on current speed.
    /// </summary>
    [Min(0f)] private float topCameraDistanceOffset = 0f;

    /// <summary>
    /// Summary: Internally used target position (used in top camera and others).
    /// </summary>
    private Vector3 targetPosition = Vector3.zero;

    /// <summary>
    /// Summary: Used for detecting if vehicle direction changes (reverse/forward) to reset orbit.
    /// </summary>
    [Min(-1)] private int direction = 1;

    /// <summary>
    /// Summary: Main TPS camera distance from the player.
    /// </summary>
    [Range(0f, 20f)] public float TPSDistance = 6.5f;

    /// <summary>
    /// Summary: Main TPS camera height above the player.
    /// </summary>
    [Range(0f, 10f)] public float TPSHeight = 1.5f;

    /// <summary>
    /// Summary: Damping factor for smoothing rotation in TPS mode.
    /// </summary>
    [Range(0f, 1f)] public float TPSRotationDamping = .5f;

    /// <summary>
    /// Summary: Maximum tilt angle for the camera in TPS mode, based on lateral velocity.
    /// </summary>
    [Range(0f, 25f)] public float TPSTiltMaximum = 15f;

    /// <summary>
    /// Summary: Tilt multiplier used to fine-tune tilt effect in TPS mode.
    /// </summary>
    [Range(0f, 1.5f)] public float TPSTiltMultiplier = 1f;

    /// <summary>
    /// Summary: Additional Yaw offset in TPS mode.
    /// </summary>
    [Range(-45f, 45f)] public float TPSYaw = 0f;

    /// <summary>
    /// Summary: Additional Pitch offset in TPS mode.
    /// </summary>
    [Range(-45f, 45f)] public float TPSPitch = 7.5f;

    /// <summary>
    /// Summary: If true, the camera automatically adjusts TPSDistance/TPSHeight based on the vehicle's bounds.
    /// </summary>
    public bool TPSAutoFocus = true;

    /// <summary>
    /// Summary: If true, camera automatically rotates for reverse gear.
    /// </summary>
    public bool TPSAutoReverse = true;

    /// <summary>
    /// Summary: If true, camera will shake slightly in TPS mode, based on vehicle speed.
    /// </summary>
    public bool TPSShake = true;

    /// <summary>
    /// Summary: TPS position offset local to the vehicle.
    /// </summary>
    public Vector3 TPSOffset = new Vector3(0f, 0f, .2f);

    /// <summary>
    /// Summary: Internally used temporary offset for TPS, allowing smooth transitions.
    /// </summary>
    private Vector3 _TPSOffset = Vector3.zero;

    /// <summary>
    /// Summary: Initial rotation for the camera when the game starts (TPS).
    /// </summary>
    public Vector3 TPSStartRotation = new Vector3(0f, 0f, 0f);

    /// <summary>
    /// Summary: Stores the last rotation of the camera in TPS mode to smooth transitions.
    /// </summary>
    private Quaternion TPSLastRotation = Quaternion.identity;

    /// <summary>
    /// Summary: The current tilt angle used when tilting the camera in TPS mode.
    /// </summary>
    private float TPSTiltAngle = 0f;

    /// <summary>
    /// Summary: Main field of view target used by all camera modes. The actual camera FOV will lerp to this.
    /// </summary>
    [Min(0f)] public float targetFieldOfView = 60f;

    /// <summary>
    /// Summary: The minimum FOV in TPS mode, based on speed.
    /// </summary>
    [Range(10f, 90f)] public float TPSMinimumFOV = 40f;

    /// <summary>
    /// Summary: The maximum FOV in TPS mode, based on speed.
    /// </summary>
    [Range(10f, 160f)] public float TPSMaximumFOV = 60f;

    /// <summary>
    /// Summary: Default hood camera FOV.
    /// </summary>
    [Range(10f, 160f)] public float hoodCameraFOV = 60f;

    /// <summary>
    /// Summary: Default wheel camera FOV.
    /// </summary>
    [Range(10f, 160f)] public float wheelCameraFOV = 60f;

    /// <summary>
    /// Summary: Minimum orthographic size when in top camera mode, based on speed.
    /// </summary>
    [Min(0f)] public float minimumOrtSize = 10f;

    /// <summary>
    /// Summary: Maximum orthographic size when in top camera mode, based on speed.
    /// </summary>
    [Min(0f)] public float maximumOrtSize = 20f;

    /// <summary>
    /// Summary: Keeps track of how many times the camera has switched modes, used in the switch/case cycle.
    /// </summary>
    [Min(0)] internal int cameraSwitchCount = 0;

    /// <summary>
    /// Summary: Velocity values for smoothing the camera's position (used by MoveTowards).
    /// </summary>
    private float xVelocity, yVelocity, zVelocity = 0f;
    private Vector3 accelerationVelocity = Vector3.zero;

    /// <summary>
    /// Summary: Current acceleration vector of the vehicle, used for dynamic camera behavior.
    /// </summary>
    public Vector3 acceleration = Vector3.zero;

    /// <summary>
    /// Summary: Stores the last frame's velocity for calculating acceleration.
    /// </summary>
    public Vector3 lastVelocity = Vector3.zero;

    /// <summary>
    /// Summary: Smoothed version of the acceleration to avoid abrupt camera movements.
    /// </summary>
    public Vector3 acceleration_Smoothed = Vector3.zero;

    /// <summary>
    /// Summary: Direction of recent collision, used for collision-based camera shakes or offsets.
    /// </summary>
    private Vector3 collisionDirection = Vector3.zero;

    /// <summary>
    /// Summary: Position offset for collision effects.
    /// </summary>
    private Vector3 collisionPos = Vector3.zero;

    /// <summary>
    /// Summary: Rotation offset for collision effects.
    /// </summary>
    private Quaternion collisionRot = Quaternion.identity;

    /// <summary>
    /// Summary: Multiplier for zooming in/out with scroll input in TPS mode.
    /// </summary>
    [Range(.5f, 10f)] public float zoomScrollMultiplier = 5f;

    /// <summary>
    /// Summary: Internally tracked scroll input for zoom in TPS mode.
    /// </summary>
    private float zoomScroll = 0;

    /// <summary>
    /// Summary: Minimum and maximum allowable scroll values in TPS mode.
    /// </summary>
    [Min(0f)] public float minimumScroll = 0f;
    [Min(0f)] public float maximumScroll = 5f;

    /// <summary>
    /// Summary: Raw orbit X and Y input from the mouse / UI.
    /// </summary>
    private float orbitX, orbitY = 0f;

    /// <summary>
    /// Summary: Smoothed orbit X and Y values.
    /// </summary>
    private float orbitX_Smoothed, orbitY_Smoothed = 0f;

    /// <summary>
    /// Summary: Minimum Orbit Y angle.
    /// </summary>
    public float minOrbitY = -15f;

    /// <summary>
    /// Summary: Maximum Orbit Y angle.
    /// </summary>
    public float maxOrbitY = 70f;

    /// <summary>
    /// Summary: Orbit X speed (mouse drag speed).
    /// </summary>
    [Min(0f)] public float orbitXSpeed = 100f;

    /// <summary>
    /// Summary: Orbit Y speed (mouse drag speed).
    /// </summary>
    [Min(0f)] public float orbitYSpeed = 100f;

    /// <summary>
    /// Summary: Smooth factor for orbit transitions.
    /// </summary>
    [Min(0f)] public float orbitSmooth = 25f;

    /// <summary>
    /// Summary: If true, orbit only when left mouse button is pressed.
    /// </summary>
    public bool orbitWhileHolding = false;

    /// <summary>
    /// Summary: If true, orbits reset automatically after some time if the vehicle is moving.
    /// </summary>
    public bool orbitReset = true;
    private float orbitResetTimer = 0f;
    private float oldOrbitX, oldOrbitY = 0f;

    /// <summary>
    /// Summary: If true, camera looks behind the vehicle in TPS mode (e.g., reversing).
    /// </summary>
    public bool lookBackNow = false;

    /// <summary>
    /// Summary: A gameobject used for dynamic camera calculations when TPSDynamic is on.
    /// </summary>
    private Transform TPSAccelerationPoint;

    private void Awake() {

        // Getting Camera.
        actualCamera = GetComponentInChildren<Camera>();

        // Creating pivot position of the camera.
        if (!pivot) {

            pivot = transform.Find("Pivot").gameObject;

            if (!pivot)
                pivot = new GameObject("Pivot");

            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localRotation = Quaternion.identity;
            actualCamera.transform.SetParent(pivot.transform, true);

        }

    }

    private void OnEnable() {

        // Calling this event when BCG Camera spawned.
        RCCP_Events.Event_OnRCCPCameraSpawned(this);

        // Listening player vehicle collisions for crashing effects.
        RCCP_InputManager.OnChangedCamera += RCCP_InputManager_OnChangedCamera;
        RCCP_InputManager.OnLookBackCamera += RCCP_InputManager_OnLookBackCamera;
        RCCP_InputManager.OnHoldOrbitCamera += RCCP_InputManager_OnHoldOrbitCamera;

    }

    /// <summary>
    /// Look back or not on TPS mode.
    /// </summary>
    /// <param name="state"></param>
    private void RCCP_InputManager_OnLookBackCamera(bool state) {

        lookBackNow = state;

    }

    /// <summary>
    /// Orbit camera if holding.
    /// </summary>
    /// <param name="state"></param>
    private void RCCP_InputManager_OnHoldOrbitCamera(bool state) {

        orbitHolding = state;

    }

    /// <summary>
    /// When player presses the change camera button, cycles to the next camera mode.
    /// </summary>
    private void RCCP_InputManager_OnChangedCamera() {

        ChangeCamera();

    }

    /// <summary>
    /// Sets target vehicle of the camera, resets camera modes, and possibly auto-focuses if enabled.
    /// Summary: This is how you initialize which car the camera should follow.
    /// </summary>
    /// <param name="player"></param>
    public void SetTarget(RCCP_CarController player) {

        // Setting target vehicle.
        cameraTarget = new CameraTarget {
            playerVehicle = player
        };

        // If auto focus is enabled, adjust distance and height of the camera automatically.
        if (TPSAutoFocus)
            StartCoroutine(AutoFocus());

        // And reset the camera modes.
        ResetCamera();

        TPSLastRotation = player.transform.rotation;

    }

    /// <summary>
    /// Clears the current camera target.
    /// </summary>
    public void RemoveTarget() {

        transform.SetParent(null);
        cameraTarget.playerVehicle = null;

    }

    private void Update() {

        // If it's inactive, disable the camera's GameObject.
        if (!isRendering) {

            if (actualCamera.gameObject.activeSelf)
                actualCamera.gameObject.SetActive(false);

        } else {

            if (!actualCamera.gameObject.activeSelf)
                actualCamera.gameObject.SetActive(true);

        }

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Receive inputs.
        Inputs();

        // Lerp current field of view to target field of view.
        actualCamera.fieldOfView = Mathf.Lerp(actualCamera.fieldOfView, targetFieldOfView, Time.deltaTime * 5f);

        // If TPSDynamic is enabled, we need a reference point for the camera to smooth toward.
        if (TPSDynamic) {

            if (TPSAccelerationPoint == null) {

                GameObject TPSAccelerationPointGO = new GameObject("TPSAccelerationPoint");
                TPSAccelerationPointGO.hideFlags = HideFlags.HideInHierarchy;
                TPSAccelerationPoint = TPSAccelerationPointGO.transform;

            }

            acceleration_Smoothed = Vector3.SmoothDamp(acceleration_Smoothed, acceleration, ref accelerationVelocity, .3f);

            if (cameraTarget.playerVehicle) {

                TPSAccelerationPoint.position = GetPlayerPosition;
                TPSAccelerationPoint.rotation = GetPlayerRotation;
                TPSAccelerationPoint.position -= TPSAccelerationPoint.rotation * (acceleration_Smoothed * 2f);

            }

        } else {

            acceleration_Smoothed = Vector3.zero;

        }

    }

    private void LateUpdate() {

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Even if we have the player vehicle and it's disabled, return.
        if (!cameraTarget.playerVehicle.gameObject.activeSelf)
            return;

        if (Time.timeScale <= 0)
            return;

        // Checks occlusion if it's enabled.
        if (!useOcclusion)
            occluded = Vector3.zero;

        // Run the corresponding method with the chosen camera mode.
        switch (cameraMode) {

            case CameraMode.TPS:

                if (useOrbitInTPSCameraMode)
                    ORBIT();

                if (tPSMode == TPSMode.TPS1)
                    TPS();
                else
                    TPS2();

                break;

            case CameraMode.FPS:

                if (useOrbitInHoodCameraMode)
                    ORBIT();

                FPS();

                break;

            case CameraMode.WHEEL:
                WHEEL();
                break;

            case CameraMode.FIXED:
                FIXED();
                break;

            case CameraMode.CINEMATIC:
                CINEMATIC();
                break;

            case CameraMode.TOP:
                TOP();
                break;

            case CameraMode.TRUCKTRAILER:

                if (useOrbitInTPSCameraMode)
                    ORBIT();

                TRUCKTRAILER();
                break;

        }

        // If camera mode changed during this frame, reset the modes.
        if (lastCameraMode != cameraMode)
            ResetCamera();

        lastCameraMode = cameraMode;

        if (useAutoChangeCamera)
            autoChangeCameraTimer += Time.deltaTime;
        else
            autoChangeCameraTimer = 0f;

        // If auto change camera is enabled, change the camera mode each 10 seconds.
        if (useAutoChangeCamera && autoChangeCameraTimer >= 10) {

            autoChangeCameraTimer = 0f;
            ChangeCamera();

        }

    }

    private void FixedUpdate() {

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Even if we have the player vehicle and it's disabled, return.
        if (!cameraTarget.playerVehicle.gameObject.activeSelf)
            return;

        Vector3 currentSpeed = cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity);
        acceleration = currentSpeed - lastVelocity;
        lastVelocity = currentSpeed;

        acceleration = Vector3.ClampMagnitude(acceleration, .1f);

        acceleration.x = currentSpeed.x * .04f * (1f - Mathf.InverseLerp(0f, 200f, cameraTarget.playerVehicle.absoluteSpeed));
        acceleration.y = 0f;
        acceleration.z *= 1.75f * (1f - Mathf.InverseLerp(0f, 200f, cameraTarget.playerVehicle.absoluteSpeed));

    }

    /// <summary>
    /// Summary: Receives player inputs for orbit and scroll in camera modes.
    /// </summary>
    private void Inputs() {

        // Receiving player inputs
        inputs = RCCP_InputManager.Instance.GetInputs();

        if (!useOrbitOnlyHolding) {

            // Setting orbits.
            orbitX += inputs.mouseInput.x;
            orbitY -= inputs.mouseInput.y;

        }

        // Clamping orbit Y.
        orbitY = Mathf.Clamp(orbitY, minOrbitY, maxOrbitY);

        // Smoothing orbits.
        orbitX_Smoothed = Mathf.Lerp(orbitX_Smoothed, orbitX, Time.deltaTime * orbitSmooth);
        orbitY_Smoothed = Mathf.Lerp(orbitY_Smoothed, orbitY, Time.deltaTime * orbitSmooth);

    }

    /// <summary>
    /// Summary: Cycles to the next camera mode, skipping modes not in use or missing references.
    /// </summary>
    public void ChangeCamera() {

        cameraSwitchCount++;

        // Adjust if your total camera modes have changed
        if (cameraSwitchCount > 6)  // now that we have 7 modes, adjust the max as needed
            cameraSwitchCount = 0;

        switch (cameraSwitchCount) {
            case 0:
                cameraMode = CameraMode.TPS;
                break;
            case 1:
                if (useHoodCameraMode && cameraTarget.HoodCamera)
                    cameraMode = CameraMode.FPS;
                else
                    ChangeCamera();
                break;
            case 2:
                if (useWheelCameraMode && cameraTarget.WheelCamera)
                    cameraMode = CameraMode.WHEEL;
                else
                    ChangeCamera();
                break;
            case 3:
                if (useFixedCameraMode && FixedCamera)
                    cameraMode = CameraMode.FIXED;
                else
                    ChangeCamera();
                break;
            case 4:
                if (useCinematicCameraMode && CinematicCamera)
                    cameraMode = CameraMode.CINEMATIC;
                else
                    ChangeCamera();
                break;
            case 5:
                if (useTopCameraMode)
                    cameraMode = CameraMode.TOP;
                else
                    ChangeCamera();
                break;
            case 6: // your new camera mode index
                cameraMode = CameraMode.TRUCKTRAILER;
                break;
        }

    }

    /// <summary>
    /// Summary: Directly switches to the specified camera mode.
    /// </summary>
    /// <param name="mode"></param>
    public void ChangeCamera(CameraMode mode) {

        cameraMode = mode;

    }

    /// <summary>
    /// Summary: First-person camera mode (hood/driver camera). 
    /// If orbit is on for hood camera, uses orbitX and orbitY for rotation.
    /// </summary>
    private void FPS() {

        // Assigning orbit rotation, and transform rotation.
        if (useOrbitInHoodCameraMode)
            transform.rotation = GetPlayerRotation * Quaternion.Euler(orbitY_Smoothed, orbitX_Smoothed, 0f);
        else
            transform.rotation = GetPlayerRotation;

    }

    /// <summary>
    /// Summary: Wheel camera mode, typically used for close-ups of wheels.
    /// If occluded, reverts to TPS mode.
    /// </summary>
    private void WHEEL() {

        if (useOcclusion) {

            occluded = OccludeRay();

            if (occluded != Vector3.zero)
                ChangeCamera(CameraMode.TPS);

        }

    }

    /// <summary>
    /// Summary: Original TPS camera logic with distance, height, tilt, etc.
    /// </summary>
    private void TPS() {

        if (cameraTarget.playerVehicle.ConnectedTrailer && cameraTarget.playerVehicle.ConnectedTrailer.gameObject.activeSelf && cameraMode != CameraMode.TRUCKTRAILER) {

            ChangeCamera(CameraMode.TRUCKTRAILER);
            return;

        }

        // Setting rotation of the camera.
        transform.rotation = TPSLastRotation;

        // If TPS Auto Reverse is enabled and vehicle is moving backwards, reset X and Y orbits when direction changes.
        direction = cameraTarget.playerVehicle.direction;

        int directionAngle = 0;
        float rotDamp = TPSRotationDamping;

        if (TPSAutoReverse)
            directionAngle = (direction == 1 ? 0 : 180);

        // If player is looking back, override directionAngle and set full damping for quick response.
        if (lookBackNow) {

            directionAngle = 180;
            rotDamp = 1f;

        }

        // If TPSFreeFall is true and not grounded, reduce damping drastically.
        if (TPSFreeFall && Time.time >= 1f) {

            if (!cameraTarget.playerVehicle.IsGrounded)
                rotDamp = -10f;

        }

        float xAngle = 0f;

        // If locked, smoothly track vehicle's X angle.
        if (TPSLockX)
            xAngle = Mathf.SmoothDampAngle(transform.eulerAngles.x, cameraTarget.playerVehicle.transform.eulerAngles.x * (directionAngle == 180 ? -1f : 1f), ref xVelocity, 1f - rotDamp);

        if (useOrbitInTPSCameraMode && orbitY != 0)
            xAngle = orbitY_Smoothed;

        float yAngle = 0f;

        // If locked, smoothly track vehicle's Y angle (plus orbit if active).
        if (TPSLockY) {

            if (!useOrbitInTPSCameraMode) {
                yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + directionAngle, ref yVelocity, 1f - rotDamp);
            } else {

                if (orbitX != 0)
                    yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + orbitX_Smoothed, ref yVelocity, .025f);
                else
                    yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + directionAngle, ref yVelocity, 1f - rotDamp);

            }

        } else {

            if (useOrbitInTPSCameraMode && orbitX != 0)
                yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, orbitX_Smoothed, ref yVelocity, .025f);

        }

        float zAngle = 0f;

        // If locked, smoothly track vehicle's Z angle.
        if (TPSLockZ)
            zAngle = Mathf.SmoothDampAngle(transform.eulerAngles.z, cameraTarget.playerVehicle.transform.eulerAngles.z, ref zVelocity, 1f - rotDamp);

        if (float.IsNaN(yAngle) || float.IsNaN(xAngle) || float.IsNaN(zAngle))
            return;

        // Position at the target.
        Vector3 position = GetPlayerPosition;

        // Rotation at the target.
        Quaternion rotation = Quaternion.Euler(xAngle, yAngle, zAngle);

        // Then offset by distance behind.
        position += rotation * (-Vector3.forward * (TPSDistance + zoomScroll));
        position += GetPlayerRotation * TPSOffset;
        position += Vector3.up * TPSHeight;

        float addTPSPitch = 0f;

        // If TPSDynamic is on, reduce distance and height relative to speed.
        if (TPSDynamic && TPSAccelerationPoint != null) {

            transform.position -= (GetPlayerPosition - TPSAccelerationPoint.position) * Time.fixedDeltaTime * 50f;
            addTPSPitch = cameraTarget.playerVehicle.transform.InverseTransformDirection(TPSAccelerationPoint.position - GetPlayerPosition).z * Time.fixedDeltaTime * 500f;

        }

        transform.SetPositionAndRotation(position, rotation);

        // Collision offsets.
        if (Time.deltaTime != 0) {
            collisionPos = Vector3.Lerp(collisionPos, Vector3.zero, Time.deltaTime * 5f);
            collisionRot = Quaternion.Lerp(collisionRot, Quaternion.identity, Time.deltaTime * 5f);
        }

        pivot.transform.localPosition = Vector3.Lerp(pivot.transform.localPosition, collisionPos, Time.deltaTime * 10f);
        pivot.transform.localRotation = Quaternion.Lerp(pivot.transform.localRotation, collisionRot, Time.deltaTime * 10f);

        // Lerp FOV with speed.
        targetFieldOfView = Mathf.Lerp(TPSMinimumFOV, TPSMaximumFOV, Mathf.Abs(cameraTarget.playerVehicle.absoluteSpeed) / 360f);

        float xVel = cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity).x;
        xVel = Mathf.Clamp(xVel, -1f, 1f);

        // Calculate tilt.
        TPSTiltAngle = TPSTiltMaximum * (xVel * Mathf.Abs(xVel) / 250f);
        TPSTiltAngle *= TPSTiltMultiplier;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                transform.position = occluded;
        }

        // Store last rotation.
        TPSLastRotation = transform.rotation;

        // Set pitch, yaw, and tilt.
        transform.rotation *= Quaternion.Euler(TPSPitch + addTPSPitch, 0f, TPSYaw + TPSTiltAngle);

        // Optional camera shake based on speed.
        if (TPSShake) {

            float speed = cameraTarget.playerVehicle.absoluteSpeed;
            float maxSpeed = 260f;
            float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
            float shakeMagnitude = normalizedSpeed * .1f;
            float frequency = 3f;
            float noiseX = (Mathf.PerlinNoise(Time.time * frequency, 0f) - .5f) * 1f;
            float noiseY = (Mathf.PerlinNoise(0f, Time.time * frequency) - .5f) * 1f;
            float shakeX = noiseX * shakeMagnitude;
            float shakeY = noiseY * shakeMagnitude;
            Vector3 localShakeOffset = transform.right * shakeX + transform.up * shakeY;
            transform.position += localShakeOffset;
            float noiseZ = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - .5f) * 2f;
            float rotShakeZ = noiseZ * shakeMagnitude * 2f;
            transform.rotation *= Quaternion.Euler(0f, 0f, rotShakeZ);

        }

    }

    /// <summary>
    /// Summary: A more stable TPS camera logic (TPS2). Includes drift support, orbit, and better rotation handling.
    /// </summary>
    private void TPS2() {

        if (cameraTarget.playerVehicle.ConnectedTrailer && cameraTarget.playerVehicle.ConnectedTrailer.gameObject.activeSelf && cameraMode != CameraMode.TRUCKTRAILER) {

            ChangeCamera(CameraMode.TRUCKTRAILER);
            return;

        }

        transform.rotation = TPSLastRotation;

        int dir = TPSAutoReverse ? cameraTarget.playerVehicle.direction : 1;
        if (lookBackNow)
            dir *= -1;

        Vector3 vehicleForward = cameraTarget.playerVehicle.transform.forward * dir;

        bool driftMode = false;
        for (int i = 0; i < cameraTarget.playerVehicle.AllWheelColliders.Length; i++) {

            if (cameraTarget.playerVehicle.AllWheelColliders[i] == null)
                continue;

            if (cameraTarget.playerVehicle.AllWheelColliders[i].driftMode) {
                driftMode = true;
                break;
            }

        }

        if (driftMode) {
            Vector3 playerVelocityDirection = cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity);
            playerVelocityDirection.y = 0f;
            playerVelocityDirection = cameraTarget.playerVehicle.transform.TransformDirection(playerVelocityDirection);

            vehicleForward = Vector3.Lerp(
                vehicleForward,
                playerVelocityDirection + (cameraTarget.playerVehicle.transform.forward * dir),
                Mathf.InverseLerp(0f, 100f, cameraTarget.playerVehicle.absoluteSpeed)
            );
        }

        Quaternion desiredRotation = Quaternion.LookRotation(vehicleForward, Vector3.up);
        Vector3 desiredEulers = desiredRotation.eulerAngles;

        if (!TPSLockX)
            desiredEulers.x = transform.eulerAngles.x;
        if (!TPSLockY)
            desiredEulers.y = transform.eulerAngles.y;
        if (!TPSLockZ)
            desiredEulers.z = 0f;
        else
            desiredEulers.z = cameraTarget.playerVehicle.transform.eulerAngles.z;

        desiredRotation = Quaternion.Euler(desiredEulers);

        float rotDamp = TPSRotationDamping * .5f;
        if (TPSFreeFall && Time.time >= 1f && !cameraTarget.playerVehicle.IsGrounded)
            rotDamp = 0f;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotDamp * 10f * Time.deltaTime
        );

        Vector3 finalEulers = transform.rotation.eulerAngles;

        if (useOrbitInTPSCameraMode) {
            if (orbitY != 0f)
                finalEulers.x = orbitY_Smoothed;
            if (orbitX != 0f)
                finalEulers.y = orbitX_Smoothed;
            if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
                finalEulers.z = 0f;
        }

        if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
            transform.rotation = Quaternion.Euler(finalEulers);

        if (TPSFreeFall && Time.time >= 1f) {
            if (!cameraTarget.playerVehicle.IsGrounded)
                _TPSOffset = Vector3.Lerp(_TPSOffset, Vector3.zero, Time.deltaTime * 10f);
            else
                _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        } else {
            _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        }

        Vector3 finalPos = GetPlayerPosition
            + GetPlayerRotation * _TPSOffset
            - transform.forward * TPSDistance
            + Vector3.up * TPSHeight;

        float addTPSPitch = 0f;
        if (TPSDynamic && TPSAccelerationPoint != null) {
            finalPos -= (GetPlayerPosition - TPSAccelerationPoint.position)
                        * Time.fixedDeltaTime * 50f;
            addTPSPitch = cameraTarget.playerVehicle.transform
                .InverseTransformDirection(TPSAccelerationPoint.position - GetPlayerPosition)
                .z
                * Time.fixedDeltaTime * 300f;
        }

        transform.position = finalPos;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                transform.position = occluded;
        }

        if (Time.deltaTime > 0f) {
            collisionPos = Vector3.Lerp(collisionPos, Vector3.zero, Time.deltaTime * 5f);
            collisionRot = Quaternion.Lerp(collisionRot, Quaternion.identity, Time.deltaTime * 5f);
        }

        pivot.transform.localPosition = Vector3.Lerp(
            pivot.transform.localPosition,
            collisionPos,
            Time.deltaTime * 10f
        );

        pivot.transform.localRotation = Quaternion.Lerp(
            pivot.transform.localRotation,
            collisionRot,
            Time.deltaTime * 10f
        );

        TPSLastRotation = transform.rotation;

        float localVelocityX = cameraTarget.playerVehicle.transform
            .InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity).x;

        TPSTiltAngle = TPSTiltMaximum
            * (Mathf.Clamp(localVelocityX, -10f, 10f) * 0.04f)
            * TPSTiltMultiplier;

        transform.rotation *= Quaternion.Euler(
            TPSPitch + addTPSPitch,
            0f,
            TPSYaw + TPSTiltAngle
        );

        if (TPSShake) {

            float speed = cameraTarget.playerVehicle.absoluteSpeed;
            float maxSpeed = 260f;
            float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
            float shakeMagnitude = normalizedSpeed * .1f;
            float frequency = 3f;
            float noiseX = (Mathf.PerlinNoise(Time.time * frequency, 0f) - .5f) * 1f;
            float noiseY = (Mathf.PerlinNoise(0f, Time.time * frequency) - .5f) * 1f;
            float shakeX = noiseX * shakeMagnitude;
            float shakeY = noiseY * shakeMagnitude;
            Vector3 localShakeOffset = transform.right * shakeX + transform.up * shakeY;
            transform.position += localShakeOffset;
            float noiseZ = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - .5f) * 2f;
            float rotShakeZ = noiseZ * shakeMagnitude * 2f;
            transform.rotation *= Quaternion.Euler(0f, 0f, rotShakeZ);

        }

        targetFieldOfView = Mathf.Lerp(
            TPSMinimumFOV,
            TPSMaximumFOV,
            Mathf.Abs(cameraTarget.playerVehicle.absoluteSpeed) / 260f
        );

    }

    /// <summary>
    /// Summary: A specialized TPS camera mode for trucks with trailers, focusing on both truck and trailer center.
    /// </summary>
    private void TRUCKTRAILER() {

        if (!cameraTarget.playerVehicle.ConnectedTrailer) {
            SetTarget(cameraTarget.playerVehicle);
            ChangeCamera(CameraMode.TPS);
            return;
        }

        transform.rotation = TPSLastRotation;

        int dir = TPSAutoReverse ? cameraTarget.playerVehicle.direction : 1;
        if (lookBackNow)
            dir *= -1;

        Vector3 vehicleForward = cameraTarget.playerVehicle.transform.forward * dir;
        Quaternion desiredRotation = Quaternion.LookRotation(vehicleForward, Vector3.up);
        Vector3 desiredEulers = desiredRotation.eulerAngles;

        if (!TPSLockX)
            desiredEulers.x = transform.eulerAngles.x;
        if (!TPSLockY)
            desiredEulers.y = transform.eulerAngles.y;
        if (!TPSLockZ)
            desiredEulers.z = 0f;
        else
            desiredEulers.z = cameraTarget.playerVehicle.transform.eulerAngles.z;

        desiredRotation = Quaternion.Euler(desiredEulers);

        float rotDamp = TPSRotationDamping * .5f;
        if (TPSFreeFall && Time.time >= 1f && !cameraTarget.playerVehicle.IsGrounded)
            rotDamp = 0f;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotDamp * 10f * Time.deltaTime
        );

        Vector3 finalEulers = transform.rotation.eulerAngles;

        if (useOrbitInTPSCameraMode) {
            if (orbitY != 0f)
                finalEulers.x = orbitY_Smoothed;
            if (orbitX != 0f)
                finalEulers.y = orbitX_Smoothed;
            if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
                finalEulers.z = 0f;
        }

        if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
            transform.rotation = Quaternion.Euler(finalEulers);

        if (TPSFreeFall && Time.time >= 1f) {
            if (!cameraTarget.playerVehicle.IsGrounded)
                _TPSOffset = Vector3.Lerp(_TPSOffset, Vector3.zero, Time.deltaTime * 10f);
            else
                _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        } else {
            _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        }

        Transform truck = cameraTarget.playerVehicle.transform;
        RCCP_TrailerController trailer = cameraTarget.playerVehicle.ConnectedTrailer;

        Vector3 combinedCenter = (truck.position + trailer.CenterPosition) * 0.5f;

        Vector3 finalPos = combinedCenter
            + GetPlayerRotation * _TPSOffset
            - transform.forward * TPSDistance
            + Vector3.up * TPSHeight;

        float addTPSPitch = 0f;
        if (TPSDynamic && TPSAccelerationPoint != null) {
            finalPos -= (GetPlayerPosition - TPSAccelerationPoint.position)
                        * Time.fixedDeltaTime * 50f;
            addTPSPitch = cameraTarget.playerVehicle.transform
                .InverseTransformDirection(TPSAccelerationPoint.position - GetPlayerPosition).z
                * Time.fixedDeltaTime * 300f;
        }

        transform.position = finalPos;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                transform.position = occluded;
        }

        if (Time.deltaTime > 0f) {
            collisionPos = Vector3.Lerp(collisionPos, Vector3.zero, Time.deltaTime * 10f);
            collisionRot = Quaternion.Lerp(collisionRot, Quaternion.identity, Time.deltaTime * 10f);
        }

        pivot.transform.localPosition = Vector3.Lerp(
            pivot.transform.localPosition,
            collisionPos,
            Time.deltaTime * 10f
        );
        pivot.transform.localRotation = Quaternion.Lerp(
            pivot.transform.localRotation,
            collisionRot,
            Time.deltaTime * 10f
        );

        TPSLastRotation = transform.rotation;

        float localVelocityX = cameraTarget.playerVehicle.transform
            .InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity).x;
        TPSTiltAngle = TPSTiltMaximum
            * (Mathf.Clamp(localVelocityX, -10f, 10f) * 0.04f)
            * TPSTiltMultiplier;

        transform.rotation *= Quaternion.Euler(
            TPSPitch + addTPSPitch,
            0f,
            TPSYaw + TPSTiltAngle
        );

        if (TPSShake) {
            float speed = cameraTarget.playerVehicle.absoluteSpeed;
            float maxSpeed = 260f;
            float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
            float shakeMagnitude = normalizedSpeed * .1f;
            float frequency = 3f;
            float noiseX = (Mathf.PerlinNoise(Time.time * frequency, 0f) - .5f) * 1f;
            float noiseY = (Mathf.PerlinNoise(0f, Time.time * frequency) - .5f) * 1f;
            float shakeX = noiseX * shakeMagnitude;
            float shakeY = noiseY * shakeMagnitude;
            Vector3 localShakeOffset = transform.right * shakeX + transform.up * shakeY;
            transform.position += localShakeOffset;
            float noiseZ = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - .5f) * 2f;
            float rotShakeZ = noiseZ * shakeMagnitude * 2f;
            transform.rotation *= Quaternion.Euler(0f, 0f, rotShakeZ);
        }

        targetFieldOfView = Mathf.Lerp(
            TPSMinimumFOV,
            TPSMaximumFOV,
            Mathf.Abs(cameraTarget.playerVehicle.absoluteSpeed) / 260f
        );

    }

    /// <summary>
    /// Summary: Fixed camera mode, uses RCCP_FixedCamera logic.
    /// If occluded, moves the camera closer.
    /// </summary>
    private void FIXED() {

        if (FixedCamera.transform.parent != null)
            FixedCamera.transform.SetParent(null);

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                FixedCamera.transform.position = occluded;
        }

    }

    /// <summary>
    /// Summary: Top-down camera mode (orthographic or perspective).
    /// Ortho size / FOV adjusts with speed.
    /// </summary>
    private void TOP() {

        actualCamera.orthographic = useOrthoForTopCamera;

        topCameraDistanceOffset = Mathf.Lerp(0f, maximumZDistanceOffset, cameraTarget.playerVehicle.absoluteSpeed / 100f);
        targetFieldOfView = Mathf.Lerp(minimumOrtSize, maximumOrtSize, cameraTarget.playerVehicle.absoluteSpeed / 100f);
        actualCamera.orthographicSize = Mathf.Lerp(actualCamera.orthographicSize, targetFieldOfView, Time.deltaTime * 3f);

        targetPosition = GetPlayerPosition;
        targetPosition += GetPlayerRotation * Vector3.forward * topCameraDistanceOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(topCameraAngle), Time.deltaTime * 15f);

        pivot.transform.localPosition = new Vector3(0f, 0f, -topCameraDistance);

    }

    /// <summary>
    /// Summary: Receives input for orbiting the camera, resetting if the player hasn't changed orbit for a while.
    /// </summary>
    private void ORBIT() {

        if (Mathf.Abs(orbitX - oldOrbitX) > Mathf.Epsilon) {
            if ((cameraMode == CameraMode.TPS || cameraMode == CameraMode.TRUCKTRAILER) && Mathf.Abs(oldOrbitX) < Mathf.Epsilon && Mathf.Abs(orbitX) > Mathf.Epsilon) {
                orbitX = transform.eulerAngles.y;
                orbitX_Smoothed = orbitX;
            }
            oldOrbitX = orbitX;
            orbitResetTimer = 2f;
        }

        if (Mathf.Abs(orbitY - oldOrbitY) > Mathf.Epsilon) {
            oldOrbitY = orbitY;
            orbitResetTimer = 2f;
        }

        if (orbitResetTimer > 0f)
            orbitResetTimer -= Time.deltaTime;

        orbitResetTimer = Mathf.Clamp(orbitResetTimer, 0f, 2f);

        if (orbitReset && cameraTarget.playerVehicle.absoluteSpeed >= 10f && orbitResetTimer <= 0f) {
            orbitX = 0f;
            orbitY = 0f;
        }

    }

    /// <summary>
    /// Used with mobile UI drag panel for orbiting.
    /// </summary>
    /// <param name="pointerData"></param>
    public void OnDrag(PointerEventData pointerData) {

        if (!useOrbitOnlyHolding)
            return;

        orbitX += pointerData.delta.x * orbitXSpeed / 1000f;
        orbitY -= pointerData.delta.y * orbitYSpeed / 1000f;

        orbitY = Mathf.Clamp(orbitY, minOrbitY, maxOrbitY);

        orbitX_Smoothed = Mathf.Lerp(orbitX_Smoothed, orbitX, Time.deltaTime * orbitSmooth);
        orbitY_Smoothed = Mathf.Lerp(orbitY_Smoothed, orbitY, Time.deltaTime * orbitSmooth);

        orbitResetTimer = 2f;

    }

    /// <summary>
    /// Summary: Cinematic mode uses a separate camera system (RCCP_CinematicCamera).
    /// </summary>
    private void CINEMATIC() {

        if (CinematicCamera.transform.parent != null)
            CinematicCamera.transform.SetParent(null);

        targetFieldOfView = CinematicCamera.targetFOV;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                ChangeCamera(CameraMode.TPS);
        }

    }

    /// <summary>
    /// Summary: Resets camera parameters (field of view, offsets, orbits) when switching modes.
    /// </summary>
    public void ResetCamera() {

        if (FixedCamera)
            FixedCamera.canTrackNow = false;

        TPSTiltAngle = 0f;

        collisionPos = Vector3.zero;
        collisionRot = Quaternion.identity;

        actualCamera.transform.localPosition = Vector3.zero;
        actualCamera.transform.localRotation = Quaternion.identity;

        pivot.transform.localPosition = collisionPos;
        pivot.transform.localRotation = collisionRot;

        orbitX = TPSStartRotation.y;
        orbitY = TPSStartRotation.x;

        orbitHolding = false;

        zoomScroll = 0f;

        if (TPSStartRotation != Vector3.zero)
            TPSStartRotation = Vector3.zero;

        actualCamera.orthographic = false;
        occluded = Vector3.zero;

        switch (cameraMode) {

            case CameraMode.TPS:
                transform.SetParent(null);
                targetFieldOfView = TPSMinimumFOV;
                break;

            case CameraMode.FPS:
                transform.SetParent(cameraTarget.HoodCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = hoodCameraFOV;
                cameraTarget.HoodCamera.FixShake();
                break;

            case CameraMode.WHEEL:
                transform.SetParent(cameraTarget.WheelCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = wheelCameraFOV;
                break;

            case CameraMode.FIXED:
                transform.SetParent(FixedCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = 60;
                FixedCamera.canTrackNow = true;
                break;

            case CameraMode.CINEMATIC:
                transform.SetParent(CinematicCamera.pivot.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = 30f;
                break;

            case CameraMode.TOP:
                transform.SetParent(null);
                targetFieldOfView = minimumOrtSize;
                targetPosition = GetPlayerPosition;
                targetPosition += GetPlayerRotation * Vector3.forward * topCameraDistanceOffset;
                transform.position = GetPlayerPosition;
                break;

            case CameraMode.TRUCKTRAILER:
                transform.SetParent(null);
                if (cameraTarget != null && cameraTarget.playerVehicle != null) {
                    Transform truck = cameraTarget.playerVehicle.transform;
                    RCCP_TrailerController trailer = cameraTarget.playerVehicle.ConnectedTrailer;

                    if (trailer) {

                        if (trailer.manualSetCameraDistanceAndHeight) {

                            TPSDistance = trailer.TPSDistance;
                            TPSHeight = trailer.TPSHeight;

                        } else {

                            StartCoroutine(AutoFocus(truck, trailer.transform));

                        }

                    } else {

                        // If auto focus is enabled, adjust distance and height of the camera automatically.
                        if (TPSAutoFocus)
                            StartCoroutine(AutoFocus(truck));

                    }

                }
                targetFieldOfView = TPSMinimumFOV;
                break;

        }

        actualCamera.fieldOfView = targetFieldOfView;

    }

    /// <summary>
    /// Summary: Enables or disables the camera. If disabled, the camera won't render.
    /// </summary>
    /// <param name="state"></param>
    public void ToggleCamera(bool state) {

        isRendering = state;

    }

    /// <summary>
    /// Performs an occlusion check from the target position to the desired camera position.
    /// Summary: Uses SphereCast to detect any obstacles and reposition if needed.
    /// </summary>
    /// <returns>Returns a new position if occluded, otherwise Vector3.zero.</returns>
    public Vector3 OccludeRay() {

        Vector3 targetFollow = GetPlayerPosition;

        Vector3 direction = transform.position - targetFollow;
        float distance = direction.magnitude;
        float cameraRadius = .2f;

        if (distance < 0.01f)
            return Vector3.zero;

        Vector3 directionNorm = direction.normalized;

        LayerMask properLayers = occlusionLayerMask;
        properLayers &= ~(1 << LayerMask.NameToLayer(RCCPSettings.RCCPLayer));
        properLayers &= ~(1 << LayerMask.NameToLayer(RCCPSettings.RCCPPropLayer));
        properLayers &= ~(1 << LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer));

        RaycastHit[] wallHits = Physics.SphereCastAll(
            origin: targetFollow,
            radius: cameraRadius,
            direction: directionNorm,
            maxDistance: distance,
            layerMask: properLayers);

        System.Array.Sort(wallHits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < wallHits.Length; i++) {
            if (!wallHits[i].collider.isTrigger && !wallHits[i].transform.IsChildOf(cameraTarget.playerVehicle.transform)) {
                Vector3 occludedPos = wallHits[i].point + wallHits[i].normal * cameraRadius;
                return occludedPos;
            }
        }

        return Vector3.zero;

    }

    /// <summary>
    /// Summary: AutoFocus adjusts TPSDistance and TPSHeight based on the vehicle's bounds over the given duration.
    /// </summary>
    public IEnumerator AutoFocus(float duration = 2f) {

        float bounds = RCCP_GetBounds.MaxBoundsExtent(cameraTarget.playerVehicle.transform);
        float targetDistance = bounds * 2.75f;
        float targetHeight = bounds * 0.65f;

        float startDistance = TPSDistance;
        float startHeight = TPSHeight;

        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            TPSDistance = Mathf.Lerp(startDistance, targetDistance, t);
            TPSHeight = Mathf.Lerp(startHeight, targetHeight, t);

            yield return null;
        }

        TPSDistance = targetDistance;
        TPSHeight = targetHeight;

    }

    /// <summary>
    /// Summary: AutoFocus variant that focuses on a specific Transform's bounds.
    /// </summary>
    public IEnumerator AutoFocus(Transform target, float duration = 2f) {

        float bounds = RCCP_GetBounds.MaxBoundsExtent(target);
        float targetDistance = bounds * 2.75f;
        float targetHeight = bounds * 0.65f;

        float startDistance = TPSDistance;
        float startHeight = TPSHeight;

        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            TPSDistance = Mathf.Lerp(startDistance, targetDistance, t);
            TPSHeight = Mathf.Lerp(startHeight, targetHeight, t);

            yield return null;
        }

        TPSDistance = targetDistance;
        TPSHeight = targetHeight;

    }

    /// <summary>
    /// Summary: AutoFocus variant that focuses on two transforms, used for truck-trailer combos.
    /// </summary>
    public IEnumerator AutoFocus(Transform t1, Transform t2, float duration = 3f) {

        float bounds1 = RCCP_GetBounds.MaxBoundsExtent(t1);
        float bounds2 = RCCP_GetBounds.MaxBoundsExtent(t2);

        float combinedBounds = bounds1 + bounds2;

        float startDistance = TPSDistance;
        float startHeight = TPSHeight;

        float targetDistance = combinedBounds * 2.75f;
        float targetHeight = combinedBounds * 0.65f;

        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            TPSDistance = Mathf.Lerp(startDistance, targetDistance, t);
            TPSHeight = Mathf.Lerp(startHeight, targetHeight, t);

            yield return null;
        }

        TPSDistance = targetDistance;
        TPSHeight = targetHeight;

    }

    /// <summary>
    /// Summary: Checks if the actual camera is active, not null, and enabled.
    /// </summary>
    /// <returns></returns>
    public bool IsCameraActive() {

        if (!actualCamera)
            return false;

        if (!actualCamera.gameObject.activeSelf)
            return false;

        if (!actualCamera.isActiveAndEnabled)
            return false;

        return true;

    }

    private void OnDisable() {

        RCCP_InputManager.OnChangedCamera -= RCCP_InputManager_OnChangedCamera;
        RCCP_InputManager.OnLookBackCamera -= RCCP_InputManager_OnLookBackCamera;
        RCCP_InputManager.OnHoldOrbitCamera -= RCCP_InputManager_OnHoldOrbitCamera;

    }

}
