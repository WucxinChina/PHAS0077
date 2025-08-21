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
using UnityEngine.EventSystems;

/// <summary>
/// UI input (float) receiver from UI Button or Slider, implementing IPointerDownHandler and IPointerUpHandler.
/// This component updates its 'input' value based on user interactions and can be used for controlling the vehicle in RCC.
/// </summary>
public class RCC_UI_Controller : RCC_Core, IPointerDownHandler, IPointerUpHandler {

    /// <summary>
    /// UI button attached to this GameObject (if any).
    /// </summary>
    //  UI button.
    private Button button;

    /// <summary>
    /// UI slider attached to this GameObject (if any).
    /// </summary>
    //  UI slider.
    private Slider slider;

    /// <summary>
    /// Current input value ranging from 0f to 1f.
    /// </summary>
    //  Input as float.
    public float input = 0f;

    /// <summary>
    /// The rate at which 'input' increases while the button is pressed.
    /// </summary>
    //  Sensitivity.
    private float Sensitivity { get { return Settings.UIButtonSensitivity; } }

    /// <summary>
    /// The rate at which 'input' decreases while the button is not pressed.
    /// </summary>
    //  Gravity.
    private float Gravity { get { return Settings.UIButtonGravity; } }

    /// <summary>
    /// Indicates whether the UI element is currently pressed.
    /// </summary>
    //  Is pressing now?
    public bool pressing = false;

    /// <summary>
    /// Called when the script instance is being loaded. Retrieves references to the Button or Slider components if they exist.
    /// </summary>
    private void Awake() {

        //  Getting components.
        button = GetComponent<Button>();
        slider = GetComponent<Slider>();

    }

    /// <summary>
    /// Called each time the GameObject is enabled. Resets 'input' and 'pressing' states, and if a slider is present, sets its value to 0.
    /// </summary>
    private void OnEnable() {

        //  Resetting on enable.
        input = 0f;
        pressing = false;

        if (slider)
            slider.value = 0f;

    }

    /// <summary>
    /// When the UI element is pressed down.
    /// </summary>
    /// <param name="eventData">Pointer data associated with the press event.</param>
    public void OnPointerDown(PointerEventData eventData) {

        pressing = true;

    }

    /// <summary>
    /// When the UI element is released.
    /// </summary>
    /// <param name="eventData">Pointer data associated with the release event.</param>
    public void OnPointerUp(PointerEventData eventData) {

        pressing = false;

    }

    /// <summary>
    /// Called once per frame, after Update has finished. Updates 'input' based on the state of the button or slider.
    /// If using a button, 'input' increases based on Sensitivity while pressed and decreases based on Gravity while not pressed.
    /// If using a slider, 'input' is directly set to the slider's value when pressed.
    /// Clamps 'input' between 0f and 1f.
    /// </summary>
    private void LateUpdate() {

        //  If button is not interactable, return with 0.
        if (button && !button.interactable) {

            pressing = false;
            input = 0f;
            return;

        }

        //  If slider is not interactable, return with 0.
        if (slider && !slider.interactable) {

            pressing = false;
            input = 0f;
            slider.value = 0f;
            return;

        }

        //  If slider selected, receive input. Otherwise, it's a button.
        if (slider) {

            if (pressing)
                input = slider.value;
            else
                input = 0f;

            slider.value = input;

        } else {

            if (pressing)
                input += Time.deltaTime * Sensitivity;
            else
                input -= Time.deltaTime * Gravity;

        }

        //  Clamping input.
        if (input < 0f)
            input = 0f;

        if (input > 1f)
            input = 1f;

    }

    /// <summary>
    /// Called each time the GameObject is disabled. Resets 'input' and 'pressing', and if a slider is present, sets its value to 0.
    /// </summary>
    private void OnDisable() {

        //  Resetting on disable.
        input = 0f;
        pressing = false;

        if (slider)
            slider.value = 0f;

    }

}
