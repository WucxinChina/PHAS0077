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
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Main input manager of the RCCP. Receives inputs from the corresponding device and let the other components use them.
/// Handles keyboard, gamepad, and mobile inputs with support for Unity's new Input System.
/// </summary>
public class RCCP_InputManager : RCCP_Singleton<RCCP_InputManager> {

    /// <summary>
    /// Flag to track if events have been subscribed to prevent double subscription.
    /// </summary>
    private bool eventsSubscribed = false;

    // Action Map Names - These should match your Input Actions asset
    private const string DRIVING_MAP_NAME = "Vehicle";
    private const string CAMERA_MAP_NAME = "Camera";
    private const string REPLAY_MAP_NAME = "Optional";

    // Action Names for Driving Map
    private const string ACTION_THROTTLE = "Throttle";
    private const string ACTION_BRAKE = "Brake";
    private const string ACTION_STEER = "Steering";
    private const string ACTION_HANDBRAKE = "Handbrake";
    private const string ACTION_NOS = "NOS";
    private const string ACTION_CLUTCH = "Clutch";
    private const string ACTION_START_ENGINE = "Start/Stop Engine";
    private const string ACTION_LOW_BEAM = "Low Beam Lights";
    private const string ACTION_HIGH_BEAM = "High Beam Lights";
    private const string ACTION_INDICATOR_RIGHT = "Indicator Right";
    private const string ACTION_INDICATOR_LEFT = "Indicator Left";
    private const string ACTION_INDICATORS = "Indicator Hazard";
    private const string ACTION_GEAR_SHIFT_UP = "Gear Shift Up";
    private const string ACTION_GEAR_SHIFT_DOWN = "Gear Shift Down";
    private const string ACTION_TRAIL_DETACH = "Trail Detach";
    private const string ACTION_N_GEAR = "Gear_N";
    private const string ACTION_1ST_GEAR = "Gear_1";
    private const string ACTION_2ND_GEAR = "Gear_2";
    private const string ACTION_3RD_GEAR = "Gear_3";
    private const string ACTION_4TH_GEAR = "Gear_4";
    private const string ACTION_5TH_GEAR = "Gear_5";
    private const string ACTION_6TH_GEAR = "Gear_6";
    private const string ACTION_R_GEAR = "Gear_R";

    // Action Names for Camera Map
    private const string ACTION_MOUSE_INPUT = "MouseInput";
    private const string ACTION_CHANGE_CAMERA = "Change Camera";
    private const string ACTION_LOOK_BACK = "Look Back";
    private const string ACTION_ORBIT_CAMERA_HOLD = "Hold";

    // Action Names for Replay Map
    private const string ACTION_RECORD = "Record";
    private const string ACTION_REPLAY = "Replay";

    /// <summary>
    /// Current input values received from the active input device
    /// </summary>
    public RCCP_Inputs inputs = new RCCP_Inputs();

    /// <summary>
    /// Reference to the Input Actions asset instance
    /// </summary>
    public InputActionAsset inputActionsInstance = null;

    /// <summary>
    /// When true, inputs are overridden by external source and device inputs are ignored
    /// </summary>
    public bool overrideInputs = false;

    // Input action map references cached for performance
    private InputActionMap drivingMap;
    private InputActionMap cameraMap;
    private InputActionMap replayMap;

    // Events - Gear related
    /// <summary>
    /// Event triggered when gear is shifted up
    /// </summary>
    public delegate void onGearShiftedUp();
    public static event onGearShiftedUp OnGearShiftedUp;

    /// <summary>
    /// Event triggered when gear is shifted down
    /// </summary>
    public delegate void onGearShiftedDown();
    public static event onGearShiftedDown OnGearShiftedDown;

    /// <summary>
    /// Event triggered when gear is shifted to specific index
    /// </summary>
    public delegate void onGearShiftedTo(int gearIndex);
    public static event onGearShiftedTo OnGearShiftedTo;

    /// <summary>
    /// Event triggered when gear is shifted to neutral
    /// </summary>
    public delegate void onGearShiftedToN();
    public static event onGearShiftedToN OnGearShiftedToN;

    /// <summary>
    /// Event triggered when transmission type is toggled
    /// </summary>
    public delegate void onGearToggle(RCCP_Gearbox.TransmissionType transmissionType);
    public static event onGearToggle OnGearToggle;

    /// <summary>
    /// Event triggered when automatic gear is changed
    /// </summary>
    public delegate void onAutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear);
    public static event onAutomaticGear OnAutomaticGear;

    // Events - Camera related
    /// <summary>
    /// Event triggered when camera is changed
    /// </summary>
    public delegate void onChangedCamera();
    public static event onChangedCamera OnChangedCamera;

    /// <summary>
    /// Event triggered when look back camera state changes
    /// </summary>
    public delegate void onLookBackCamera(bool state);
    public static event onLookBackCamera OnLookBackCamera;

    /// <summary>
    /// Event triggered when orbit camera hold state changes
    /// </summary>
    public delegate void onHoldOrbitCamera(bool state);
    public static event onHoldOrbitCamera OnHoldOrbitCamera;

    // Events - Lights related
    /// <summary>
    /// Event triggered when low beam lights are toggled
    /// </summary>
    public delegate void onPressedLowBeamLights();
    public static event onPressedLowBeamLights OnPressedLowBeamLights;

    /// <summary>
    /// Event triggered when high beam lights are toggled
    /// </summary>
    public delegate void onPressedHighBeamLights();
    public static event onPressedHighBeamLights OnPressedHighBeamLights;

    /// <summary>
    /// Event triggered when left indicator lights are toggled
    /// </summary>
    public delegate void onPressedLeftIndicatorLights();
    public static event onPressedLeftIndicatorLights OnPressedLeftIndicatorLights;

    /// <summary>
    /// Event triggered when right indicator lights are toggled
    /// </summary>
    public delegate void onPressedRightIndicatorLights();
    public static event onPressedRightIndicatorLights OnPressedRightIndicatorLights;

    /// <summary>
    /// Event triggered when hazard lights are toggled
    /// </summary>
    public delegate void onPressedIndicatorLights();
    public static event onPressedIndicatorLights OnPressedIndicatorLights;

    // Events - Engine related
    /// <summary>
    /// Event triggered when engine start is requested
    /// </summary>
    public delegate void onStartEngine();
    public static event onStartEngine OnStartEngine;

    /// <summary>
    /// Event triggered when engine stop is requested
    /// </summary>
    public delegate void onStopEngine();
    public static event onStopEngine OnStopEngine;

    // Events - Helpers related
    /// <summary>
    /// Event triggered when steering helper is toggled
    /// </summary>
    public delegate void onSteeringHelper();
    public static event onSteeringHelper OnSteeringHelper;

    /// <summary>
    /// Event triggered when traction helper is toggled
    /// </summary>
    public delegate void onTractionHelper();
    public static event onTractionHelper OnTractionHelper;

    /// <summary>
    /// Event triggered when angular drag helper is toggled
    /// </summary>
    public delegate void onAngularDragHelper();
    public static event onAngularDragHelper OnAngularDragHelper;

    /// <summary>
    /// Event triggered when ABS is toggled
    /// </summary>
    public delegate void onABS();
    public static event onABS OnABS;

    /// <summary>
    /// Event triggered when ESP is toggled
    /// </summary>
    public delegate void onESP();
    public static event onESP OnESP;

    /// <summary>
    /// Event triggered when TCS is toggled
    /// </summary>
    public delegate void onTCS();
    public static event onTCS OnTCS;

    // Events - Replay related
    /// <summary>
    /// Event triggered when recording is toggled
    /// </summary>
    public delegate void onRecord();
    public static event onRecord OnRecord;

    /// <summary>
    /// Event triggered when replay is started
    /// </summary>
    public delegate void onReplay();
    public static event onReplay OnReplay;

    // Events - Misc
    /// <summary>
    /// Event triggered when trailer detach is requested
    /// </summary>
    public delegate void onTrailerDetach();
    public static event onTrailerDetach OnTrailerDetach;

    /// <summary>
    /// Event triggered when options menu is requested
    /// </summary>
    public delegate void onOptions();
    public static event onOptions OnOptions;

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake() {

        // Let the base singleton class handle the instance management
        // Only initialize if this is the valid instance
        if (Instance == this) {

            // Initialize inputs
            inputs = new RCCP_Inputs();

            // Make this object persistent across scene loads
            DontDestroyOnLoad(gameObject);

        }

    }

    /// <summary>
    /// Called when the object becomes enabled and active
    /// </summary>
    private void OnEnable() {

        // Initialize input system
        InitializeInputSystem();

    }

    /// <summary>
    /// Called when the object becomes disabled
    /// </summary>
    private void OnDisable() {

        // Clean up input system
        CleanupInputSystem();

    }

    /// <summary>
    /// Initializes the input system and subscribes to input events
    /// </summary>
    private void InitializeInputSystem() {

        try {

            // Check if RCCP_InputActions instance exists
            if (RCCP_InputActions.Instance == null) {

                Debug.LogWarning("RCCP_InputActions.Instance is null. Input system will not be initialized.");
                return;

            }

            // Get the Input Actions from RCCP_InputActions
            inputActionsInstance = RCCP_InputActions.Instance.inputActions;

            if (inputActionsInstance == null) {

                Debug.LogWarning("InputActions asset is null in RCCP_InputActions.Instance");
                return;

            }

            // Cache action maps
            CacheActionMaps();

            // Enable the entire asset
            inputActionsInstance.Enable();

            // Subscribe to events only once.
            if (!eventsSubscribed)
                SubscribeToAllEvents();

        } catch (Exception e) {

            Debug.LogError($"Failed to initialize input system: {e.Message}");

        }

    }

    /// <summary>
    /// Caches references to action maps for performance
    /// </summary>
    private void CacheActionMaps() {

        try {

            // Find action maps by name instead of using indices
            drivingMap = inputActionsInstance.FindActionMap(DRIVING_MAP_NAME);
            cameraMap = inputActionsInstance.FindActionMap(CAMERA_MAP_NAME);
            replayMap = inputActionsInstance.FindActionMap(REPLAY_MAP_NAME);

            // Validate that all maps were found
            if (drivingMap == null)
                Debug.LogError($"Could not find action map: {DRIVING_MAP_NAME}");

            if (cameraMap == null)
                Debug.LogError($"Could not find action map: {CAMERA_MAP_NAME}");

            if (replayMap == null)
                Debug.LogError($"Could not find action map: {REPLAY_MAP_NAME}");

        } catch (Exception e) {

            Debug.LogError($"Failed to cache action maps: {e.Message}");

        }

    }

    /// <summary>
    /// Cleans up the input system and unsubscribes from events
    /// </summary>
    private void CleanupInputSystem() {

        // Safely unsubscribe from all events
        if (inputActionsInstance != null) {

            UnsubscribeFromAllEvents();

            // Disable the input actions
            inputActionsInstance.Disable();

        }

        // Clear cached references
        drivingMap = null;
        cameraMap = null;
        replayMap = null;

    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    private void Update() {

        // Validate and recreate inputs if null
        if (inputs == null)
            inputs = new RCCP_Inputs();

        // Process inputs based on current input mode
        if (!overrideInputs) {

            // Check if RCCPSettings exists before accessing it
            bool useMobileController = false;

            if (RCCPSettings != null)
                useMobileController = RCCPSettings.mobileControllerEnabled;

            // Get inputs from appropriate source
            if (!useMobileController)
                inputs = GetKeyboardInputs();
            else
                inputs = GetMobileInputs();

        }

    }

    /// <summary>
    /// Gets keyboard and gamepad inputs from the new input system
    /// </summary>
    /// <returns>RCCP_Inputs structure with current input values</returns>
    private RCCP_Inputs GetKeyboardInputs() {

        // Return current inputs if action system is not ready
        if (inputActionsInstance == null || drivingMap == null || cameraMap == null)
            return inputs;

        try {

            // Read driving inputs
            var throttleAction = drivingMap.FindAction(ACTION_THROTTLE);
            if (throttleAction != null && throttleAction.enabled)
                inputs.throttleInput = throttleAction.ReadValue<float>();

            var brakeAction = drivingMap.FindAction(ACTION_BRAKE);
            if (brakeAction != null && brakeAction.enabled)
                inputs.brakeInput = brakeAction.ReadValue<float>();

            var steerAction = drivingMap.FindAction(ACTION_STEER);
            if (steerAction != null && steerAction.enabled)
                inputs.steerInput = steerAction.ReadValue<float>();

            var handbrakeAction = drivingMap.FindAction(ACTION_HANDBRAKE);
            if (handbrakeAction != null && handbrakeAction.enabled)
                inputs.handbrakeInput = handbrakeAction.ReadValue<float>();

            var nosAction = drivingMap.FindAction(ACTION_NOS);
            if (nosAction != null && nosAction.enabled)
                inputs.nosInput = nosAction.ReadValue<float>();

            var clutchAction = drivingMap.FindAction(ACTION_CLUTCH);
            if (clutchAction != null && clutchAction.enabled)
                inputs.clutchInput = clutchAction.ReadValue<float>();

            // Read camera inputs
            var mouseAction = cameraMap.FindAction(ACTION_MOUSE_INPUT);
            if (mouseAction != null && mouseAction.enabled)
                inputs.mouseInput = mouseAction.ReadValue<Vector2>();

        } catch (Exception e) {

            Debug.LogError($"Error reading keyboard inputs: {e.Message}");

        }

        return inputs;

    }

    /// <summary>
    /// Gets mobile inputs from RCCP_MobileInputs component
    /// </summary>
    /// <returns>RCCP_Inputs structure with current mobile input values</returns>
    private RCCP_Inputs GetMobileInputs() {

        // Get mobile inputs instance
        RCCP_MobileInputs mobileInputs = RCCP_MobileInputs.Instance;

        if (mobileInputs != null) {

            // Copy values from mobile inputs
            inputs.throttleInput = mobileInputs.throttleInput;
            inputs.brakeInput = mobileInputs.brakeInput;
            inputs.steerInput = mobileInputs.steerInput;
            inputs.handbrakeInput = mobileInputs.ebrakeInput;
            inputs.nosInput = mobileInputs.nosInput;

        }

        return inputs;

    }

    // Input Override Methods

    /// <summary>
    /// Overrides the current inputs with custom values
    /// </summary>
    /// <param name="overriddenInputs">Custom input values to use</param>
    public void OverrideInputs(RCCP_Inputs overriddenInputs) {

        if (overriddenInputs == null) {

            Debug.LogWarning("Cannot override inputs with null value");
            return;

        }

        overrideInputs = true;
        inputs = overriddenInputs;

    }

    /// <summary>
    /// Disables input override and returns to normal input processing
    /// </summary>
    public void DisableOverrideInputs() {

        overrideInputs = false;

    }

    /// <summary>
    /// Gets the current input values
    /// </summary>
    /// <returns>Current RCCP_Inputs structure</returns>
    public RCCP_Inputs GetInputs() {

        return inputs;

    }

    // Public Methods for Event Triggering

    /// <summary>
    /// Triggers gear shift up event
    /// </summary>
    public void GearShiftUp() {

        OnGearShiftedUp?.Invoke();

    }

    /// <summary>
    /// Triggers gear shift down event
    /// </summary>
    public void GearShiftDown() {

        OnGearShiftedDown?.Invoke();

    }

    /// <summary>
    /// Triggers gear shift to neutral event
    /// </summary>
    public void GearShiftToN() {

        OnGearShiftedToN?.Invoke();

    }

    /// <summary>
    /// Triggers transmission type toggle event
    /// </summary>
    /// <param name="transmissionType">New transmission type</param>
    public void ToggleGear(RCCP_Gearbox.TransmissionType transmissionType) {

        OnGearToggle?.Invoke(transmissionType);

    }

    /// <summary>
    /// Triggers automatic gear change event
    /// </summary>
    /// <param name="semiAutomaticDNRPGear">New gear position</param>
    public void AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear) {

        OnAutomaticGear?.Invoke(semiAutomaticDNRPGear);

    }

    /// <summary>
    /// Triggers camera change event
    /// </summary>
    public void ChangeCamera() {

        OnChangedCamera?.Invoke();

    }

    /// <summary>
    /// Triggers low beam headlights toggle event
    /// </summary>
    public void LowBeamHeadlights() {

        OnPressedLowBeamLights?.Invoke();

    }

    /// <summary>
    /// Triggers high beam headlights toggle event
    /// </summary>
    public void HighBeamHeadlights() {

        OnPressedHighBeamLights?.Invoke();

    }

    /// <summary>
    /// Triggers left indicator lights toggle event
    /// </summary>
    public void IndicatorLeftlights() {

        OnPressedLeftIndicatorLights?.Invoke();

    }

    /// <summary>
    /// Triggers right indicator lights toggle event
    /// </summary>
    public void IndicatorRightlights() {

        OnPressedRightIndicatorLights?.Invoke();

    }

    /// <summary>
    /// Triggers hazard lights toggle event
    /// </summary>
    public void Indicatorlights() {

        OnPressedIndicatorLights?.Invoke();

    }

    /// <summary>
    /// Triggers look back camera state change event
    /// </summary>
    /// <param name="state">True when looking back, false otherwise</param>
    public void LookBackCamera(bool state) {

        OnLookBackCamera?.Invoke(state);

    }

    /// <summary>
    /// Triggers orbit camera hold state change event
    /// </summary>
    /// <param name="state">True when holding orbit, false otherwise</param>
    public void HoldOrbitCamera(bool state) {

        OnHoldOrbitCamera?.Invoke(state);

    }

    /// <summary>
    /// Triggers engine start event
    /// </summary>
    public void StartEngine() {

        OnStartEngine?.Invoke();

    }

    /// <summary>
    /// Triggers engine stop event
    /// </summary>
    public void StopEngine() {

        OnStopEngine?.Invoke();

    }

    /// <summary>
    /// Triggers steering helper toggle event
    /// </summary>
    public void SteeringHelper() {

        OnSteeringHelper?.Invoke();

    }

    /// <summary>
    /// Triggers traction helper toggle event
    /// </summary>
    public void TractionHelper() {

        OnTractionHelper?.Invoke();

    }

    /// <summary>
    /// Triggers angular drag helper toggle event
    /// </summary>
    public void AngularDragHelper() {

        OnAngularDragHelper?.Invoke();

    }

    /// <summary>
    /// Triggers ABS toggle event
    /// </summary>
    public void ABS() {

        OnABS?.Invoke();

    }

    /// <summary>
    /// Triggers ESP toggle event
    /// </summary>
    public void ESP() {

        OnESP?.Invoke();

    }

    /// <summary>
    /// Triggers TCS toggle event
    /// </summary>
    public void TCS() {

        OnTCS?.Invoke();

    }

    /// <summary>
    /// Triggers recording toggle event
    /// </summary>
    public void Record() {

        OnRecord?.Invoke();

    }

    /// <summary>
    /// Triggers replay start event
    /// </summary>
    public void Replay() {

        OnReplay?.Invoke();

    }

    /// <summary>
    /// Triggers trailer detach event
    /// </summary>
    public void TrailDetach() {

        OnTrailerDetach?.Invoke();

    }

    /// <summary>
    /// Triggers options menu event
    /// </summary>
    public void Options() {

        OnOptions?.Invoke();

    }

    // Input System Callbacks

    /// <summary>
    /// Callback for gear shift up input
    /// </summary>
    private void GearShiftUp_performed(InputAction.CallbackContext ctx) {

        GearShiftUp();

    }

    /// <summary>
    /// Callback for gear shift down input
    /// </summary>
    private void GearShiftDown_performed(InputAction.CallbackContext ctx) {

        GearShiftDown();

    }

    /// <summary>
    /// Callback for neutral gear input
    /// </summary>
    private void NGear_performed(InputAction.CallbackContext ctx) {

        GearShiftToN();

    }

    /// <summary>
    /// Callback for 1st gear input
    /// </summary>
    private void _1stGear_performed(InputAction.CallbackContext ctx) {

        OnGearShiftedTo?.Invoke(0);

    }

    /// <summary>
    /// Callback for 2nd gear input
    /// </summary>
    private void _2ndGear_performed(InputAction.CallbackContext ctx) {

        OnGearShiftedTo?.Invoke(1);

    }

    /// <summary>
    /// Callback for 3rd gear input
    /// </summary>
    private void _3rdGear_performed(InputAction.CallbackContext ctx) {

        OnGearShiftedTo?.Invoke(2);

    }

    /// <summary>
    /// Callback for 4th gear input
    /// </summary>
    private void _4thGear_performed(InputAction.CallbackContext ctx) {

        OnGearShiftedTo?.Invoke(3);

    }

    /// <summary>
    /// Callback for 5th gear input
    /// </summary>
    private void _5thGear_performed(InputAction.CallbackContext ctx) {

        OnGearShiftedTo?.Invoke(4);

    }

    /// <summary>
    /// Callback for 6th gear input
    /// </summary>
    private void _6thGear_performed(InputAction.CallbackContext ctx) {

        OnGearShiftedTo?.Invoke(5);

    }

    /// <summary>
    /// Callback for reverse gear input
    /// </summary>
    private void _RGear_performed(InputAction.CallbackContext ctx) {

        OnGearShiftedTo?.Invoke(-1);

    }

    /// <summary>
    /// Callback for trailer detach input
    /// </summary>
    private void TrailDetach_performed(InputAction.CallbackContext ctx) {

        TrailDetach();

    }

    /// <summary>
    /// Callback for camera change input
    /// </summary>
    private void ChangeCamera_performed(InputAction.CallbackContext ctx) {

        ChangeCamera();

    }

    /// <summary>
    /// Callback for look back camera pressed
    /// </summary>
    private void LookBackCamera_performed(InputAction.CallbackContext ctx) {

        LookBackCamera(true);

    }

    /// <summary>
    /// Callback for look back camera released
    /// </summary>
    private void LookBackCamera_canceled(InputAction.CallbackContext ctx) {

        LookBackCamera(false);

    }

    /// <summary>
    /// Callback for orbit camera hold pressed
    /// </summary>
    private void HoldOrbitCamera_performed(InputAction.CallbackContext ctx) {

        HoldOrbitCamera(true);

    }

    /// <summary>
    /// Callback for orbit camera hold released
    /// </summary>
    private void HoldOrbitCamera_canceled(InputAction.CallbackContext ctx) {

        HoldOrbitCamera(false);

    }

    /// <summary>
    /// Callback for engine start input
    /// </summary>
    private void StartEngine_performed(InputAction.CallbackContext ctx) {

        StartEngine();

    }

    /// <summary>
    /// Callback for low beam lights input
    /// </summary>
    private void LowBeamHeadlights_performed(InputAction.CallbackContext ctx) {

        LowBeamHeadlights();

    }

    /// <summary>
    /// Callback for high beam lights input
    /// </summary>
    private void HighBeamHeadlights_performed(InputAction.CallbackContext ctx) {

        HighBeamHeadlights();

    }

    /// <summary>
    /// Callback for left indicator lights input
    /// </summary>
    private void IndicatorLeftlights_performed(InputAction.CallbackContext ctx) {

        IndicatorLeftlights();

    }

    /// <summary>
    /// Callback for right indicator lights input
    /// </summary>
    private void IndicatorRightlights_performed(InputAction.CallbackContext ctx) {

        IndicatorRightlights();

    }

    /// <summary>
    /// Callback for hazard lights input
    /// </summary>
    private void Indicatorlights_performed(InputAction.CallbackContext ctx) {

        Indicatorlights();

    }

    /// <summary>
    /// Callback for record input
    /// </summary>
    private void Record_performed(InputAction.CallbackContext ctx) {

        Record();

    }

    /// <summary>
    /// Callback for replay input
    /// </summary>
    private void Replay_performed(InputAction.CallbackContext ctx) {

        Replay();

    }

    // Event Subscription Methods

    /// <summary>
    /// Subscribes to all input events
    /// </summary>
    private void SubscribeToAllEvents() {

        SubscribeDrivingMapEvents();
        SubscribeCameraMapEvents();
        SubscribeReplayMapEvents();

        eventsSubscribed = true;

    }

    /// <summary>
    /// Unsubscribes from all input events
    /// </summary>
    private void UnsubscribeFromAllEvents() {

        UnsubscribeDrivingMapEvents();
        UnsubscribeCameraMapEvents();
        UnsubscribeReplayMapEvents();

        eventsSubscribed = false;

    }

    /// <summary>
    /// Subscribes to driving map input events
    /// </summary>
    private void SubscribeDrivingMapEvents() {

        if (drivingMap == null)
            return;

        if (eventsSubscribed)
            return;

        try {

            // Subscribe to each action by name
            var startEngineAction = drivingMap.FindAction(ACTION_START_ENGINE);
            if (startEngineAction != null)
                startEngineAction.performed += StartEngine_performed;

            var lowBeamAction = drivingMap.FindAction(ACTION_LOW_BEAM);
            if (lowBeamAction != null)
                lowBeamAction.performed += LowBeamHeadlights_performed;

            var highBeamAction = drivingMap.FindAction(ACTION_HIGH_BEAM);
            if (highBeamAction != null)
                highBeamAction.performed += HighBeamHeadlights_performed;

            var indicatorRightAction = drivingMap.FindAction(ACTION_INDICATOR_RIGHT);
            if (indicatorRightAction != null)
                indicatorRightAction.performed += IndicatorRightlights_performed;

            var indicatorLeftAction = drivingMap.FindAction(ACTION_INDICATOR_LEFT);
            if (indicatorLeftAction != null)
                indicatorLeftAction.performed += IndicatorLeftlights_performed;

            var indicatorsAction = drivingMap.FindAction(ACTION_INDICATORS);
            if (indicatorsAction != null)
                indicatorsAction.performed += Indicatorlights_performed;

            var gearShiftUpAction = drivingMap.FindAction(ACTION_GEAR_SHIFT_UP);
            if (gearShiftUpAction != null)
                gearShiftUpAction.performed += GearShiftUp_performed;

            var gearShiftDownAction = drivingMap.FindAction(ACTION_GEAR_SHIFT_DOWN);
            if (gearShiftDownAction != null)
                gearShiftDownAction.performed += GearShiftDown_performed;

            var trailDetachAction = drivingMap.FindAction(ACTION_TRAIL_DETACH);
            if (trailDetachAction != null)
                trailDetachAction.performed += TrailDetach_performed;

            var nGearAction = drivingMap.FindAction(ACTION_N_GEAR);
            if (nGearAction != null)
                nGearAction.performed += NGear_performed;

            var gear1Action = drivingMap.FindAction(ACTION_1ST_GEAR);
            if (gear1Action != null)
                gear1Action.performed += _1stGear_performed;

            var gear2Action = drivingMap.FindAction(ACTION_2ND_GEAR);
            if (gear2Action != null)
                gear2Action.performed += _2ndGear_performed;

            var gear3Action = drivingMap.FindAction(ACTION_3RD_GEAR);
            if (gear3Action != null)
                gear3Action.performed += _3rdGear_performed;

            var gear4Action = drivingMap.FindAction(ACTION_4TH_GEAR);
            if (gear4Action != null)
                gear4Action.performed += _4thGear_performed;

            var gear5Action = drivingMap.FindAction(ACTION_5TH_GEAR);
            if (gear5Action != null)
                gear5Action.performed += _5thGear_performed;

            var gear6Action = drivingMap.FindAction(ACTION_6TH_GEAR);
            if (gear6Action != null)
                gear6Action.performed += _6thGear_performed;

            var gearRAction = drivingMap.FindAction(ACTION_R_GEAR);
            if (gearRAction != null)
                gearRAction.performed += _RGear_performed;

        } catch (Exception e) {

            Debug.LogError($"Failed to subscribe to driving map events: {e.Message}");

        }

    }

    /// <summary>
    /// Unsubscribes from driving map input events
    /// </summary>
    private void UnsubscribeDrivingMapEvents() {

        if (drivingMap == null)
            return;

        if (!eventsSubscribed)
            return;

        try {

            // Unsubscribe from each action by name
            var startEngineAction = drivingMap.FindAction(ACTION_START_ENGINE);
            if (startEngineAction != null)
                startEngineAction.performed -= StartEngine_performed;

            var lowBeamAction = drivingMap.FindAction(ACTION_LOW_BEAM);
            if (lowBeamAction != null)
                lowBeamAction.performed -= LowBeamHeadlights_performed;

            var highBeamAction = drivingMap.FindAction(ACTION_HIGH_BEAM);
            if (highBeamAction != null)
                highBeamAction.performed -= HighBeamHeadlights_performed;

            var indicatorRightAction = drivingMap.FindAction(ACTION_INDICATOR_RIGHT);
            if (indicatorRightAction != null)
                indicatorRightAction.performed -= IndicatorRightlights_performed;

            var indicatorLeftAction = drivingMap.FindAction(ACTION_INDICATOR_LEFT);
            if (indicatorLeftAction != null)
                indicatorLeftAction.performed -= IndicatorLeftlights_performed;

            var indicatorsAction = drivingMap.FindAction(ACTION_INDICATORS);
            if (indicatorsAction != null)
                indicatorsAction.performed -= Indicatorlights_performed;

            var gearShiftUpAction = drivingMap.FindAction(ACTION_GEAR_SHIFT_UP);
            if (gearShiftUpAction != null)
                gearShiftUpAction.performed -= GearShiftUp_performed;

            var gearShiftDownAction = drivingMap.FindAction(ACTION_GEAR_SHIFT_DOWN);
            if (gearShiftDownAction != null)
                gearShiftDownAction.performed -= GearShiftDown_performed;

            var trailDetachAction = drivingMap.FindAction(ACTION_TRAIL_DETACH);
            if (trailDetachAction != null)
                trailDetachAction.performed -= TrailDetach_performed;

            var nGearAction = drivingMap.FindAction(ACTION_N_GEAR);
            if (nGearAction != null)
                nGearAction.performed -= NGear_performed;

            var gear1Action = drivingMap.FindAction(ACTION_1ST_GEAR);
            if (gear1Action != null)
                gear1Action.performed -= _1stGear_performed;

            var gear2Action = drivingMap.FindAction(ACTION_2ND_GEAR);
            if (gear2Action != null)
                gear2Action.performed -= _2ndGear_performed;

            var gear3Action = drivingMap.FindAction(ACTION_3RD_GEAR);
            if (gear3Action != null)
                gear3Action.performed -= _3rdGear_performed;

            var gear4Action = drivingMap.FindAction(ACTION_4TH_GEAR);
            if (gear4Action != null)
                gear4Action.performed -= _4thGear_performed;

            var gear5Action = drivingMap.FindAction(ACTION_5TH_GEAR);
            if (gear5Action != null)
                gear5Action.performed -= _5thGear_performed;

            var gear6Action = drivingMap.FindAction(ACTION_6TH_GEAR);
            if (gear6Action != null)
                gear6Action.performed -= _6thGear_performed;

            var gearRAction = drivingMap.FindAction(ACTION_R_GEAR);
            if (gearRAction != null)
                gearRAction.performed -= _RGear_performed;

        } catch (Exception e) {

            Debug.LogError($"Failed to unsubscribe from driving map events: {e.Message}");

        }

    }

    /// <summary>
    /// Subscribes to camera map input events
    /// </summary>
    private void SubscribeCameraMapEvents() {

        if (cameraMap == null)
            return;

        if (eventsSubscribed)
            return;

        try {

            var changeCameraAction = cameraMap.FindAction(ACTION_CHANGE_CAMERA);
            if (changeCameraAction != null)
                changeCameraAction.performed += ChangeCamera_performed;

            var lookBackAction = cameraMap.FindAction(ACTION_LOOK_BACK);
            if (lookBackAction != null) {

                lookBackAction.performed += LookBackCamera_performed;
                lookBackAction.canceled += LookBackCamera_canceled;

            }

            var orbitCameraAction = cameraMap.FindAction(ACTION_ORBIT_CAMERA_HOLD);
            if (orbitCameraAction != null) {

                orbitCameraAction.performed += HoldOrbitCamera_performed;
                orbitCameraAction.canceled += HoldOrbitCamera_canceled;

            }

        } catch (Exception e) {

            Debug.LogError($"Failed to subscribe to camera map events: {e.Message}");

        }

    }

    /// <summary>
    /// Unsubscribes from camera map input events
    /// </summary>
    private void UnsubscribeCameraMapEvents() {

        if (cameraMap == null)
            return;

        if (!eventsSubscribed)
            return;

        try {

            var changeCameraAction = cameraMap.FindAction(ACTION_CHANGE_CAMERA);
            if (changeCameraAction != null)
                changeCameraAction.performed -= ChangeCamera_performed;

            var lookBackAction = cameraMap.FindAction(ACTION_LOOK_BACK);
            if (lookBackAction != null) {

                lookBackAction.performed -= LookBackCamera_performed;
                lookBackAction.canceled -= LookBackCamera_canceled;

            }

            var orbitCameraAction = cameraMap.FindAction(ACTION_ORBIT_CAMERA_HOLD);
            if (orbitCameraAction != null) {

                orbitCameraAction.performed -= HoldOrbitCamera_performed;
                orbitCameraAction.canceled -= HoldOrbitCamera_canceled;

            }

        } catch (Exception e) {

            Debug.LogError($"Failed to unsubscribe from camera map events: {e.Message}");

        }

    }

    /// <summary>
    /// Subscribes to replay map input events
    /// </summary>
    private void SubscribeReplayMapEvents() {

        if (replayMap == null)
            return;

        if (eventsSubscribed)
            return;

        try {

            var recordAction = replayMap.FindAction(ACTION_RECORD);
            if (recordAction != null)
                recordAction.performed += Record_performed;

            var replayAction = replayMap.FindAction(ACTION_REPLAY);
            if (replayAction != null)
                replayAction.performed += Replay_performed;

        } catch (Exception e) {

            Debug.LogError($"Failed to subscribe to replay map events: {e.Message}");

        }

    }

    /// <summary>
    /// Unsubscribes from replay map input events
    /// </summary>
    private void UnsubscribeReplayMapEvents() {

        if (replayMap == null)
            return;

        if (!eventsSubscribed)
            return;

        try {

            var recordAction = replayMap.FindAction(ACTION_RECORD);
            if (recordAction != null)
                recordAction.performed -= Record_performed;

            var replayAction = replayMap.FindAction(ACTION_REPLAY);
            if (replayAction != null)
                replayAction.performed -= Replay_performed;

        } catch (Exception e) {

            Debug.LogError($"Failed to unsubscribe from replay map events: {e.Message}");

        }

    }

    /// <summary>
    /// Handles application pause state changes (primarily for mobile platforms).
    /// Disables input when paused and re-enables when resumed.
    /// </summary>
    /// <param name="pauseStatus">True when application is paused, false when resumed</param>
    private void OnApplicationPause(bool pauseStatus) {

        // Only handle if we have valid input actions and not overriding inputs
        if (inputActionsInstance == null || overrideInputs)
            return;

        try {

            if (pauseStatus) {

                // Application is being paused - disable inputs to prevent stuck inputs
                if (inputActionsInstance.enabled) {

                    inputActionsInstance.Disable();

                    // Reset current input values to prevent stuck inputs
                    ResetInputValues();

                }

            } else {

                // Application is resuming - re-enable inputs
                if (!inputActionsInstance.enabled) {

                    inputActionsInstance.Enable();

                }

            }

        } catch (Exception e) {

            Debug.LogError($"RCCP_InputManager: Error handling application pause: {e.Message}");

        }

    }

    /// <summary>
    /// Handles application focus changes (primarily for desktop platforms).
    /// Disables input when focus is lost and re-enables when focus is regained.
    /// </summary>
    /// <param name="hasFocus">True when application has focus, false when focus is lost</param>
    private void OnApplicationFocus(bool hasFocus) {

        // Only handle if we have valid input actions and not overriding inputs
        if (inputActionsInstance == null || overrideInputs)
            return;

        // Skip on mobile platforms as OnApplicationPause handles it
#if UNITY_ANDROID || UNITY_IOS
        return;
#endif

        try {

            if (!hasFocus) {

                // Application lost focus - disable inputs to prevent stuck inputs
                if (inputActionsInstance.enabled) {

                    inputActionsInstance.Disable();

                    // Reset current input values to prevent stuck inputs
                    ResetInputValues();

                }

            } else {

                // Application regained focus - re-enable inputs
                if (!inputActionsInstance.enabled) {

                    inputActionsInstance.Enable();

                }

            }

        } catch (Exception e) {

            Debug.LogError($"RCCP_InputManager: Error handling application focus: {e.Message}");

        }

    }

    /// <summary>
    /// Resets all input values to their default state.
    /// Called when application loses focus or is paused to prevent stuck inputs.
    /// </summary>
    private void ResetInputValues() {

        if (inputs != null) {

            inputs.throttleInput = 0f;
            inputs.brakeInput = 0f;
            inputs.steerInput = 0f;
            inputs.handbrakeInput = 0f;
            inputs.nosInput = 0f;
            inputs.clutchInput = 0f;
            inputs.mouseInput = Vector2.zero;

        }

    }

}