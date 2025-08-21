//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// UI text highlighter when mouse hovers.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Button Text Highlighter")]
public class RCCP_UI_ButtonTextHighlighter : RCCP_UIComponent, IPointerEnterHandler, IPointerExitHandler {

    /// <summary>
    /// Reference to the TextMeshPro component.
    /// </summary>
    private TextMeshProUGUI text;

    /// <summary>
    /// Reference to the Animator component.
    /// </summary>
    private Animator animator;

    /// <summary>
    /// Color for the default text state.
    /// </summary>
    public Color defaultTextColor = Color.white;

    /// <summary>
    /// Color for the target (highlighted) text state.
    /// </summary>
    public Color targetTextColor = Color.black;

    /// <summary>
    /// Boolean to track whether the mouse is hovering over the UI element.
    /// </summary>
    private bool hovering = false;

    /// <summary>
    /// Speed of color transition.
    /// </summary>
    public float speed = 10f;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake() {

        // Get the TextMeshPro component in children
        text = GetComponentInChildren<TextMeshProUGUI>();

        // Get the Animator component in children
        animator = GetComponentInChildren<Animator>();

        // Set the default text color to the current color of the text
        defaultTextColor = text.color;

    }

    /// <summary>
    /// OnEnable is called when the object becomes enabled and active.
    /// </summary>
    private void OnEnable() {

        // Reset hovering state
        hovering = false;

        // Reset text color to default
        text.color = defaultTextColor;

    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update() {

        // If hovering, lerp the text color to target color, otherwise lerp to default color
        if (hovering)
            text.color = Color.Lerp(text.color, targetTextColor, Time.deltaTime * speed);
        else
            text.color = Color.Lerp(text.color, defaultTextColor, Time.deltaTime * speed);

    }

    /// <summary>
    /// Called when the pointer enters the UI element.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerEnter(PointerEventData eventData) {

        // Set hovering to true
        hovering = true;

        // Play animation if animator exists
        if (animator)
            animator.Play(0);

    }

    /// <summary>
    /// Called when the pointer exits the UI element.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerExit(PointerEventData eventData) {

        // Set hovering to false
        hovering = false;

    }

    /// <summary>
    /// OnDisable is called when the object becomes disabled or inactive.
    /// </summary>
    private void OnDisable() {

        // Reset hovering state
        hovering = false;
        // Reset text color to default
        text.color = defaultTextColor;

    }

}
