//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Receives UI-based (mobile) inputs and applies them to the active RCC vehicle. 
/// This class supports various control methods such as buttons, joystick, steering wheel, and gyro. 
/// Different UI elements are enabled or disabled depending on the user’s selected mobile controller type.
/// </summary>
public class RCC_MobileButtons : RCC_Core {

    #region UI Buttons

    [Header("Controller Buttons")]
    /// <summary>
    /// Tap-based gas pedal button.
    /// </summary>
    public RCC_UI_Controller gasButton;

    /// <summary>
    /// Gradual throttle (slider-based) gas pedal.
    /// </summary>
    public RCC_UI_Controller gradualGasButton;

    /// <summary>
    /// Brake button.
    /// </summary>
    public RCC_UI_Controller brakeButton;

    /// <summary>
    /// Left steering button.
    /// </summary>
    public RCC_UI_Controller leftButton;

    /// <summary>
    /// Right steering button.
    /// </summary>
    public RCC_UI_Controller rightButton;

    /// <summary>
    /// Handbrake button.
    /// </summary>
    public RCC_UI_Controller handbrakeButton;

    /// <summary>
    /// NOS (Nitrous) button for touch-screen controls.
    /// </summary>
    public RCC_UI_Controller NOSButton;

    /// <summary>
    /// NOS button for steering wheel layout.
    /// </summary>
    public RCC_UI_Controller NOSButtonSteeringWheel;

    // public GameObject gearButton; // If desired, an extra button for gear changes.

    #endregion

    #region Configuration Fields

    /// <summary>
    /// If true, use the gradual (slider-based) throttle instead of a simple gas button.
    /// </summary>
    public bool useGradualThrottle = false;
    private bool lastUseGradualThrottle = false;

    [Header("Steering Wheel")]
    /// <summary>
    /// Steering wheel UI controller.
    /// </summary>
    public RCC_UI_SteeringWheelController steeringWheel;

    [Header("Joystick")]
    /// <summary>
    /// Joystick UI controller (for steering).
    /// </summary>
    public RCC_UI_Joystick joystick;

    /// <summary>
    /// Static RCC_Inputs struct used to hold processed mobile inputs.
    /// Other RCC scripts read from this to control the vehicle.
    /// </summary>
    public static RCC_Inputs mobileInputs = new RCC_Inputs();

    #endregion

    #region Internal Input Variables

    private float throttleInput = 0f;
    private float brakeInput = 0f;
    private float leftInput = 0f;
    private float rightInput = 0f;
    private float steeringWheelInput = 0f;
    private float handbrakeInput = 0f;

    /// <summary>
    /// Summed boost input from any NOS buttons (mobile/steering wheel).
    /// </summary>
    private float boostInput = 1f;
    private float gyroInput = 0f;
    private float joystickInput = 0f;

    /// <summary>
    /// If true, NOS (nitrous) UI button should be displayed.
    /// </summary>
    private bool canUseNos = false;

    /// <summary>
    /// Used to store the brake button's original position (especially for gyro layout).
    /// </summary>
    private Vector3 orgBrakeButtonPos = Vector3.zero;

    #endregion

    private void Start() {

        // Cache the original brake button position if assigned.
        if (brakeButton)
            orgBrakeButtonPos = brakeButton.transform.position;

        CheckMobileButtons();

    }

    private void OnEnable() {

        // Subscribe to the event that notifies when the active vehicle changes.
        RCC_SceneManager.OnVehicleChanged += CheckMobileButtons;

    }

    /// <summary>
    /// Checks whether mobile controls are enabled in RCC Settings and updates button visibility accordingly.
    /// </summary>
    private void CheckMobileButtons() {

        if (Settings.mobileControllerEnabled)
            EnableButtons();
        else
            DisableButtons();

    }

    /// <summary>
    /// Disables all mobile UI elements.
    /// </summary>
    private void DisableButtons() {

        if (gasButton && gasButton.gameObject.activeSelf)
            gasButton.gameObject.SetActive(false);

        if (gradualGasButton && gradualGasButton.gameObject.activeSelf)
            gradualGasButton.gameObject.SetActive(false);

        if (leftButton && leftButton.gameObject.activeSelf)
            leftButton.gameObject.SetActive(false);

        if (rightButton && rightButton.gameObject.activeSelf)
            rightButton.gameObject.SetActive(false);

        if (brakeButton && brakeButton.gameObject.activeSelf)
            brakeButton.gameObject.SetActive(false);

        if (steeringWheel && steeringWheel.gameObject.activeSelf)
            steeringWheel.gameObject.SetActive(false);

        if (handbrakeButton && handbrakeButton.gameObject.activeSelf)
            handbrakeButton.gameObject.SetActive(false);

        if (NOSButton && NOSButton.gameObject.activeSelf)
            NOSButton.gameObject.SetActive(false);

        if (NOSButtonSteeringWheel && NOSButtonSteeringWheel.gameObject.activeSelf)
            NOSButtonSteeringWheel.gameObject.SetActive(false);

        // if (gearButton && gearButton.gameObject.activeSelf)
        //     gearButton.gameObject.SetActive(false);

        if (joystick && joystick.gameObject.activeSelf)
            joystick.gameObject.SetActive(false);

    }

    /// <summary>
    /// Enables the relevant UI elements based on the user’s chosen mobile control layout.
    /// </summary>
    private void EnableButtons() {

        // Toggle between simple gas button and a gradual throttle slider.
        if (!useGradualThrottle) {
            if (gasButton && !gasButton.gameObject.activeSelf)
                gasButton.gameObject.SetActive(true);

            if (gradualGasButton && gradualGasButton.gameObject.activeSelf)
                gradualGasButton.gameObject.SetActive(false);

        } else {
            if (gradualGasButton && !gradualGasButton.gameObject.activeSelf)
                gradualGasButton.gameObject.SetActive(true);

            if (gasButton && gasButton.gameObject.activeSelf)
                gasButton.gameObject.SetActive(false);
        }

        // Enable left/right steering, brake, steering wheel, handbrake, NOS, etc.
        if (leftButton && !leftButton.gameObject.activeSelf)
            leftButton.gameObject.SetActive(true);

        if (rightButton && !rightButton.gameObject.activeSelf)
            rightButton.gameObject.SetActive(true);

        if (brakeButton && !brakeButton.gameObject.activeSelf)
            brakeButton.gameObject.SetActive(true);

        if (steeringWheel && !steeringWheel.gameObject.activeSelf)
            steeringWheel.gameObject.SetActive(true);

        if (handbrakeButton && !handbrakeButton.gameObject.activeSelf)
            handbrakeButton.gameObject.SetActive(true);

        if (NOSButton && !NOSButton.gameObject.activeSelf)
            NOSButton.gameObject.SetActive(true);

        if (NOSButtonSteeringWheel && !NOSButtonSteeringWheel.gameObject.activeSelf)
            NOSButtonSteeringWheel.gameObject.SetActive(true);

        // if (gearButton && !gearButton.gameObject.activeSelf)
        //     gearButton.gameObject.SetActive(true);

        if (joystick && !joystick.gameObject.activeSelf)
            joystick.gameObject.SetActive(true);

    }

    private void Update() {

        // If mobile controllers aren’t enabled, ignore input processing.
        if (!Settings.mobileControllerEnabled)
            return;

        // Switch logic based on the selected mobile controller type.
        switch (Settings.mobileController) {

            case RCC_Settings.MobileController.TouchScreen:
                SetTouchScreenLayout();
                break;

            case RCC_Settings.MobileController.Gyro:
                SetGyroLayout();
                break;

            case RCC_Settings.MobileController.SteeringWheel:
                SetSteeringWheelLayout();
                break;

            case RCC_Settings.MobileController.Joystick:
                SetJoystickLayout();
                break;

        }

        // Retrieve raw inputs from UI buttons.
        if (!useGradualThrottle)
            throttleInput = GetInput(gasButton);
        else
            throttleInput = GetInput(gradualGasButton);

        brakeInput = GetInput(brakeButton);
        leftInput = GetInput(leftButton);
        rightInput = GetInput(rightButton);
        handbrakeInput = GetInput(handbrakeButton);
        boostInput = Mathf.Clamp(GetInput(NOSButton) + GetInput(NOSButtonSteeringWheel), 0f, 1f);

        // Merge boost input with throttle input so it’s effectively “full throttle + boost”.
        throttleInput += boostInput;
        throttleInput = Mathf.Clamp01(throttleInput);

        // Check the steering wheel input if active.
        if (steeringWheel && steeringWheel.gameObject.activeSelf)
            steeringWheelInput = steeringWheel.input;
        else
            steeringWheelInput = 0f;

        // Check the joystick input if active.
        if (joystick && joystick.gameObject.activeSelf)
            joystickInput = joystick.InputHorizontal;
        else
            joystickInput = 0f;

        // Toggle throttle UI objects if the “gradual throttle” setting changed at runtime.
        if (useGradualThrottle != lastUseGradualThrottle) {

            if (gasButton)
                gasButton.gameObject.SetActive(!useGradualThrottle);
            if (gradualGasButton)
                gradualGasButton.gameObject.SetActive(useGradualThrottle);

        }

        lastUseGradualThrottle = useGradualThrottle;

        // Push these processed inputs to the static RCC_Inputs struct.
        SetMobileInputs();

    }

    #region Mobile Layout Setups

    /// <summary>
    /// Standard button controls for steering left/right, used with an on-screen gas/brake or slider.
    /// </summary>
    private void SetTouchScreenLayout() {

        // Disable gyro if it was previously enabled.
        if (RCC_InputManager.Instance.gyroUsed) {

            RCC_InputManager.Instance.gyroUsed = false;

            if (SystemInfo.supportsGyroscope && Accelerometer.current != null)
                InputSystem.DisableDevice(Accelerometer.current);

        }

        gyroInput = 0f;

        // Ensure steering wheel is off for this layout.
        if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
            steeringWheel.gameObject.SetActive(false);

        // Enable or disable NOS button if the vehicle supports NOS.
        if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
            NOSButton.gameObject.SetActive(canUseNos);

        // Joystick not used in TouchScreen mode.
        if (joystick && joystick.gameObject.activeInHierarchy)
            joystick.gameObject.SetActive(false);

        // Restore left/right steering buttons if hidden.
        if (!leftButton.gameObject.activeInHierarchy) {

            if (orgBrakeButtonPos != Vector3.zero)
                brakeButton.transform.position = orgBrakeButtonPos;

            leftButton.gameObject.SetActive(true);

        }

        if (!rightButton.gameObject.activeInHierarchy)
            rightButton.gameObject.SetActive(true);

    }

    /// <summary>
    /// Gyro-based steering using the device accelerometer. 
    /// </summary>
    private void SetGyroLayout() {

        // Enable gyro if not already active.
        if (!RCC_InputManager.Instance.gyroUsed) {

            RCC_InputManager.Instance.gyroUsed = true;

            if (SystemInfo.supportsGyroscope && Accelerometer.current != null)
                InputSystem.EnableDevice(Accelerometer.current);

        }

        // Read accelerometer for horizontal tilt.
        if (SystemInfo.supportsGyroscope && Accelerometer.current != null)
            gyroInput = Mathf.Lerp(gyroInput, Accelerometer.current.acceleration.ReadValue().x * Settings.gyroSensitivity, Time.deltaTime * 5f);
        else
            gyroInput = 0f;

        // Place brake button where left button typically is.
        brakeButton.transform.position = leftButton.transform.position;

        // Steering wheel is not used in Gyro layout.
        if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
            steeringWheel.gameObject.SetActive(false);

        // NOS is toggled via the standard button, enable only if canUseNos is true.
        if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
            NOSButton.gameObject.SetActive(canUseNos);

        if (joystick && joystick.gameObject.activeInHierarchy)
            joystick.gameObject.SetActive(false);

        // No separate left/right UI buttons in Gyro mode.
        if (leftButton.gameObject.activeInHierarchy)
            leftButton.gameObject.SetActive(false);

        if (rightButton.gameObject.activeInHierarchy)
            rightButton.gameObject.SetActive(false);

    }

    /// <summary>
    /// Steering wheel UI for rotation-based steering, typically used in a single UI wheel for steering input.
    /// </summary>
    private void SetSteeringWheelLayout() {

        // Disable gyro if previously active.
        if (RCC_InputManager.Instance.gyroUsed) {

            RCC_InputManager.Instance.gyroUsed = false;

            if (SystemInfo.supportsGyroscope && Accelerometer.current != null)
                InputSystem.DisableDevice(Accelerometer.current);

        }

        gyroInput = 0f;

        // Enable steering wheel UI if not already active.
        if (steeringWheel && !steeringWheel.gameObject.activeInHierarchy) {

            steeringWheel.gameObject.SetActive(true);

            if (orgBrakeButtonPos != Vector3.zero)
                brakeButton.transform.position = orgBrakeButtonPos;

        }

        // Hide standard NOS button, use the steering wheel NOS if available.
        if (NOSButton && NOSButton.gameObject.activeInHierarchy)
            NOSButton.gameObject.SetActive(false);

        if (NOSButtonSteeringWheel && NOSButtonSteeringWheel.gameObject.activeInHierarchy != canUseNos)
            NOSButtonSteeringWheel.gameObject.SetActive(canUseNos);

        if (joystick && joystick.gameObject.activeInHierarchy)
            joystick.gameObject.SetActive(false);

        // Hide the left/right steering buttons.
        if (leftButton.gameObject.activeInHierarchy)
            leftButton.gameObject.SetActive(false);
        if (rightButton.gameObject.activeInHierarchy)
            rightButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Uses a joystick UI element for steering input.
    /// </summary>
    private void SetJoystickLayout() {

        // Disable gyro if previously active.
        if (RCC_InputManager.Instance.gyroUsed) {

            RCC_InputManager.Instance.gyroUsed = false;

            if (SystemInfo.supportsGyroscope && Accelerometer.current != null)
                InputSystem.DisableDevice(Accelerometer.current);

        }

        gyroInput = 0f;

        // Hide the steering wheel UI.
        if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
            steeringWheel.gameObject.SetActive(false);

        // Use the standard NOS button if the vehicle can use NOS.
        if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
            NOSButton.gameObject.SetActive(canUseNos);

        // Enable the joystick UI if not already active.
        if (joystick && !joystick.gameObject.activeInHierarchy) {

            joystick.gameObject.SetActive(true);
            brakeButton.transform.position = orgBrakeButtonPos;

        }

        // Hide left/right steering buttons.
        if (leftButton.gameObject.activeInHierarchy)
            leftButton.gameObject.SetActive(false);

        if (rightButton.gameObject.activeInHierarchy)
            rightButton.gameObject.SetActive(false);

    }

    #endregion

    /// <summary>
    /// Captures the final UI button states, merges them, and assigns them to the static RCC_Inputs struct for use by RCC.
    /// </summary>
    private void SetMobileInputs() {

        // Check if the active vehicle supports NOS (nitrous).
        if (RCCSceneManager.activePlayerVehicle)
            canUseNos = RCCSceneManager.activePlayerVehicle.useNOS;
        else
            canUseNos = false;

        mobileInputs.throttleInput = throttleInput;
        mobileInputs.brakeInput = brakeInput;
        mobileInputs.steerInput = -leftInput + rightInput + steeringWheelInput + gyroInput + joystickInput;
        mobileInputs.handbrakeInput = handbrakeInput;
        mobileInputs.boostInput = boostInput;

    }

    /// <summary>
    /// Retrieves the current input value from a specified RCC_UI_Controller button (range [0..1]).
    /// </summary>
    /// <param name="button">The UI controller button to read.</param>
    /// <returns>Current button input value, or 0 if not active.</returns>
    private float GetInput(RCC_UI_Controller button) {

        if (button == null)
            return 0f;

        if (!button.gameObject.activeSelf)
            return 0f;

        return button.input;

    }

    private void OnDisable() {

        // Unsubscribe from the vehicle change event to prevent memory leaks.
        RCC_SceneManager.OnVehicleChanged -= CheckMobileButtons;

    }

}
