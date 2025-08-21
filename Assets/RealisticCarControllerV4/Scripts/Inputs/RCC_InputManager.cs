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
using UnityEngine.InputSystem;

/// <summary>
/// The primary input management system for RCC, handling vehicle, camera, and optional inputs. 
/// This class translates raw input actions (from Unity's InputSystem) into RCC-friendly variables 
/// and events that other systems (e.g., RCC_CarControllerV4, RCC_Camera, RCC_MobileButtons) can access.
/// </summary>
public class RCC_InputManager : RCC_Singleton<RCC_InputManager> {

    /// <summary>
    /// An instance of RCC_Inputs, which aggregates all relevant input values (throttle, brake, etc.).
    /// </summary>
    public RCC_Inputs inputs = new RCC_Inputs();

    /// <summary>
    /// The InputActions asset for RCC. It's assigned and enabled once in Awake().
    /// Note: Not static to prevent issues with multiple scenes and proper cleanup.
    /// </summary>
    private RCC_InputActions inputActions;

    /// <summary>
    /// Indicates whether gyroscopic (mobile) steering is in use.
    /// </summary>
    public bool gyroUsed = false;

    /// <summary>
    /// Flag to track if events have been subscribed to prevent double subscription.
    /// </summary>
    private bool eventsSubscribed = false;

    /// <summary>
    /// Lock object for thread-safe operations.
    /// </summary>
    private readonly object inputLock = new object();

    #region Events and Delegates

    public delegate void onStartStopEngine();
    /// <summary>
    /// Invoked when the engine toggle (start/stop) is performed.
    /// </summary>
    public static event onStartStopEngine OnStartStopEngine;

    public delegate void onLowBeamHeadlights();
    /// <summary>
    /// Invoked when toggling low-beam headlights.
    /// </summary>
    public static event onLowBeamHeadlights OnLowBeamHeadlights;

    public delegate void onHighBeamHeadlights();
    /// <summary>
    /// Invoked when toggling high-beam headlights.
    /// </summary>
    public static event onHighBeamHeadlights OnHighBeamHeadlights;

    public delegate void onChangeCamera();
    /// <summary>
    /// Invoked when requesting a camera mode change.
    /// </summary>
    public static event onChangeCamera OnChangeCamera;

    public delegate void onIndicatorLeft();
    /// <summary>
    /// Invoked when toggling the left indicator.
    /// </summary>
    public static event onIndicatorLeft OnIndicatorLeft;

    public delegate void onIndicatorRight();
    /// <summary>
    /// Invoked when toggling the right indicator.
    /// </summary>
    public static event onIndicatorRight OnIndicatorRight;

    public delegate void onIndicatorHazard();
    /// <summary>
    /// Invoked when toggling the hazard lights.
    /// </summary>
    public static event onIndicatorHazard OnIndicatorHazard;

    public delegate void onInteriorlights();
    /// <summary>
    /// Invoked when toggling the interior lights.
    /// </summary>
    public static event onInteriorlights OnInteriorlights;

    public delegate void onGearShiftUp();
    /// <summary>
    /// Invoked when shifting up a gear.
    /// </summary>
    public static event onGearShiftUp OnGearShiftUp;

    public delegate void onGearShiftDown();
    /// <summary>
    /// Invoked when shifting down a gear.
    /// </summary>
    public static event onGearShiftDown OnGearShiftDown;

    public delegate void onNGear(bool state);
    /// <summary>
    /// Invoked when toggling neutral gear, passing a boolean that indicates the gear state.
    /// </summary>
    public static event onNGear OnNGear;

    public delegate void onSlowMotion(bool state);
    /// <summary>
    /// Invoked when toggling slow-motion gameplay, passing a boolean to indicate activation/deactivation.
    /// </summary>
    public static event onSlowMotion OnSlowMotion;

    public delegate void onRecord();
    /// <summary>
    /// Invoked when starting a recording session.
    /// </summary>
    public static event onRecord OnRecord;

    public delegate void onReplay();
    /// <summary>
    /// Invoked when replaying a previous recording.
    /// </summary>
    public static event onReplay OnReplay;

    public delegate void onLookBack(bool state);
    /// <summary>
    /// Invoked when toggling the look-back camera, passing a boolean for its on/off state.
    /// </summary>
    public static event onLookBack OnLookBack;

    public delegate void onTrailerDetach();
    /// <summary>
    /// Invoked when detaching a trailer from the vehicle.
    /// </summary>
    public static event onTrailerDetach OnTrailerDetach;

    #endregion

    private void Awake() {

        // Check if another instance exists and handle it properly.
        if (Instance != this) {

            Destroy(gameObject);
            return;

        }

        // Hide this GameObject from the scene hierarchy for cleanliness.
        gameObject.hideFlags = HideFlags.HideInHierarchy;

        // Instantiate the inputs container once.
        inputs = new RCC_Inputs();

    }

    private void OnEnable() {

        // Initialize InputActions once during Awake.
        InitializeInputActions();

    }

    /// <summary>
    /// Properly cleanup when the InputManager is destroyed.
    /// </summary>
    private void OnDisable() {

        // Only clean up if this is the actual instance being destroyed.
        if (Instance != this)
            return;

        // Unsubscribe from all events.
        UnsubscribeFromEvents();

        if (inputActions != null) {

            inputActions.Disable();
            inputActions.Vehicle.Disable();
            inputActions.Camera.Disable();
            inputActions.Optional.Disable();

        }

    }

    private void OnDestroy() {

        // Dispose of the input action asset to clean up unmanaged resources
        if (inputActions != null) {

            inputActions.Dispose();
            inputActions = null;

        }

    }

    private void Update() {

        // Collect current frame inputs from the InputSystem (or mobile, if enabled).
        GetInputs();

    }

    /// <summary>
    /// Initializes the InputActions and subscribes to all events once.
    /// Thread-safe implementation to prevent race conditions.
    /// </summary>
    private void InitializeInputActions() {

        lock (inputLock) {

            // Create and enable InputActions if not already done.
            if (inputActions == null) {

                try {

                    inputActions = new RCC_InputActions();
                    inputActions.Enable();
                    inputActions.Vehicle.Enable();
                    inputActions.Camera.Enable();
                    inputActions.Optional.Enable();

                } catch (System.Exception e) {

                    Debug.LogError("RCC: Failed to initialize InputActions: " + e.Message);
                    return;

                }

            }

            // Subscribe to events only once.
            if (!eventsSubscribed)
                SubscribeToEvents();

        }

    }

    /// <summary>
    /// Subscribes to all input action events.
    /// </summary>
    private void SubscribeToEvents() {

        if (inputActions == null || eventsSubscribed)
            return;

        // General vehicle events
        inputActions.Vehicle.StartStopEngine.performed += StartStopEngine_performed;
        inputActions.Vehicle.LowBeamLights.performed += LowBeamLights_performed;
        inputActions.Vehicle.HighBeamLights.performed += HighBeamLights_performed;
        inputActions.Camera.ChangeCamera.performed += ChangeCamera_performed;
        inputActions.Vehicle.IndicatorLeft.performed += IndicatorLeft_performed;
        inputActions.Vehicle.IndicatorRight.performed += IndicatorRight_performed;
        inputActions.Vehicle.IndicatorHazard.performed += IndicatorHazard_performed;
        inputActions.Vehicle.InteriorLights.performed += InteriorLights_performed;
        inputActions.Vehicle.GearShiftUp.performed += GearShiftUp_performed;
        inputActions.Vehicle.GearShiftDown.performed += GearShiftDown_performed;
        inputActions.Vehicle.NGear.performed += NGear_performed;
        inputActions.Vehicle.NGear.canceled += NGear_canceled;
        inputActions.Optional.SlowMotion.performed += SlowMotion_performed;
        inputActions.Optional.SlowMotion.canceled += SlowMotion_canceled;
        inputActions.Optional.Record.performed += Record_performed;
        inputActions.Optional.Replay.performed += Replay_performed;
        inputActions.Camera.LookBack.performed += LookBack_performed;
        inputActions.Camera.LookBack.canceled += LookBack_canceled;
        inputActions.Vehicle.TrailerDetach.performed += TrailerDetach_performed;

        eventsSubscribed = true;

    }

    /// <summary>
    /// Unsubscribes from all input action events.
    /// </summary>
    private void UnsubscribeFromEvents() {

        if (inputActions == null || !eventsSubscribed)
            return;

        // General vehicle events
        inputActions.Vehicle.StartStopEngine.performed -= StartStopEngine_performed;
        inputActions.Vehicle.LowBeamLights.performed -= LowBeamLights_performed;
        inputActions.Vehicle.HighBeamLights.performed -= HighBeamLights_performed;
        inputActions.Camera.ChangeCamera.performed -= ChangeCamera_performed;
        inputActions.Vehicle.IndicatorLeft.performed -= IndicatorLeft_performed;
        inputActions.Vehicle.IndicatorRight.performed -= IndicatorRight_performed;
        inputActions.Vehicle.IndicatorHazard.performed -= IndicatorHazard_performed;
        inputActions.Vehicle.InteriorLights.performed -= InteriorLights_performed;
        inputActions.Vehicle.GearShiftUp.performed -= GearShiftUp_performed;
        inputActions.Vehicle.GearShiftDown.performed -= GearShiftDown_performed;
        inputActions.Vehicle.NGear.performed -= NGear_performed;
        inputActions.Vehicle.NGear.canceled -= NGear_canceled;
        inputActions.Optional.SlowMotion.performed -= SlowMotion_performed;
        inputActions.Optional.SlowMotion.canceled -= SlowMotion_canceled;
        inputActions.Optional.Record.performed -= Record_performed;
        inputActions.Optional.Replay.performed -= Replay_performed;
        inputActions.Camera.LookBack.performed -= LookBack_performed;
        inputActions.Camera.LookBack.canceled -= LookBack_canceled;
        inputActions.Vehicle.TrailerDetach.performed -= TrailerDetach_performed;

        eventsSubscribed = false;

    }

    /// <summary>
    /// Updates all input values for the current frame.
    /// </summary>
    public void GetInputs() {

        // Ensure InputActions are initialized.
        if (inputActions == null || !inputActions.asset.enabled) {

            InitializeInputActions();
            return;

        }

        // If the mobile controller is disabled, read standard input from the InputActions asset.
        if (!Settings.mobileControllerEnabled) {

            try {

                inputs.throttleInput = inputActions.Vehicle.Throttle.ReadValue<float>();
                inputs.brakeInput = inputActions.Vehicle.Brake.ReadValue<float>();
                inputs.steerInput = inputActions.Vehicle.Steering.ReadValue<float>();
                inputs.handbrakeInput = inputActions.Vehicle.Handbrake.ReadValue<float>();
                inputs.boostInput = inputActions.Vehicle.NOS.ReadValue<float>();
                inputs.clutchInput = inputActions.Vehicle.Clutch.ReadValue<float>();

                // Camera orbit/zoom inputs (e.g., right-stick or mouse movement).
                inputs.orbitX = inputActions.Camera.Orbit.ReadValue<Vector2>().x;
                inputs.orbitY = inputActions.Camera.Orbit.ReadValue<Vector2>().y;
                inputs.scroll = inputActions.Camera.Zoom.ReadValue<Vector2>();

            } catch (System.Exception e) {

                Debug.LogError("RCC: Error reading input values: " + e.Message);

            }

        } else {

            // If using mobile controls, read from RCC_MobileButtons.
            if (RCC_MobileButtons.mobileInputs != null) {

                inputs.throttleInput = RCC_MobileButtons.mobileInputs.throttleInput;
                inputs.brakeInput = RCC_MobileButtons.mobileInputs.brakeInput;
                inputs.steerInput = RCC_MobileButtons.mobileInputs.steerInput;
                inputs.handbrakeInput = RCC_MobileButtons.mobileInputs.handbrakeInput;
                inputs.boostInput = RCC_MobileButtons.mobileInputs.boostInput;

            }

        }

    }

    #region Input Action Callbacks

    private void StartStopEngine_performed(InputAction.CallbackContext obj) {

        OnStartStopEngine?.Invoke();

    }

    private void LowBeamLights_performed(InputAction.CallbackContext obj) {

        OnLowBeamHeadlights?.Invoke();

    }

    private void HighBeamLights_performed(InputAction.CallbackContext obj) {

        OnHighBeamHeadlights?.Invoke();

    }

    private void ChangeCamera_performed(InputAction.CallbackContext obj) {

        OnChangeCamera?.Invoke();

    }

    private void IndicatorLeft_performed(InputAction.CallbackContext obj) {

        OnIndicatorLeft?.Invoke();

    }

    private void IndicatorRight_performed(InputAction.CallbackContext obj) {

        OnIndicatorRight?.Invoke();

    }

    private void IndicatorHazard_performed(InputAction.CallbackContext obj) {

        OnIndicatorHazard?.Invoke();

    }

    private void InteriorLights_performed(InputAction.CallbackContext obj) {

        OnInteriorlights?.Invoke();

    }

    private void GearShiftUp_performed(InputAction.CallbackContext obj) {

        OnGearShiftUp?.Invoke();

    }

    private void GearShiftDown_performed(InputAction.CallbackContext obj) {

        OnGearShiftDown?.Invoke();

    }

    private void NGear_performed(InputAction.CallbackContext obj) {

        OnNGear?.Invoke(true);

    }

    private void NGear_canceled(InputAction.CallbackContext obj) {

        OnNGear?.Invoke(false);

    }

    private void SlowMotion_performed(InputAction.CallbackContext obj) {

        OnSlowMotion?.Invoke(true);

    }

    private void SlowMotion_canceled(InputAction.CallbackContext obj) {

        OnSlowMotion?.Invoke(false);

    }

    private void Record_performed(InputAction.CallbackContext obj) {

        OnRecord?.Invoke();

    }

    private void Replay_performed(InputAction.CallbackContext obj) {

        OnReplay?.Invoke();

    }

    private void LookBack_performed(InputAction.CallbackContext obj) {

        OnLookBack?.Invoke(true);

    }

    private void LookBack_canceled(InputAction.CallbackContext obj) {

        OnLookBack?.Invoke(false);

    }

    private void TrailerDetach_performed(InputAction.CallbackContext obj) {

        OnTrailerDetach?.Invoke();

    }

    #endregion

    /// <summary>
    /// Additional cleanup when the application loses focus or is paused.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus) {

        if (pauseStatus && inputActions != null) {

            inputActions.Disable();

        } else if (!pauseStatus && inputActions != null) {

            inputActions.Enable();

        }

    }

    /// <summary>
    /// Additional cleanup when the application loses focus.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus) {

        if (!hasFocus && inputActions != null) {

            inputActions.Disable();

        } else if (hasFocus && inputActions != null) {

            inputActions.Enable();

        }

    }

}