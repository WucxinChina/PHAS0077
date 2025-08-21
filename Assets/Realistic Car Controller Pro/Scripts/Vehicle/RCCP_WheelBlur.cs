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
/// Implements a fake spinning effect for wheels by placing a blurred wheel texture at each wheel's position. 
/// This helps give the illusion of rapid wheel rotation without animating an actual wheel model at high RPM.
/// 
/// Usage:
/// - Creates blurred wheel mesh renderers in the editor or at runtime.
/// - Rotates a texture proportionally to the wheel's RPM, and varies the blur intensity in the shader.
/// - Must be used with a shader that supports the "_BlurIntensity" property (e.g., "RCCP_Wheel_Blur").
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Wheel Blur")]
public class RCCP_WheelBlur : RCCP_Component {

    /// <summary>
    /// Container class referencing a single wheel's blur mesh renderer and its associated RCCP_WheelCollider.
    /// </summary>
    private Wheel[] wheelBlurRenderers;

    [System.Serializable]
    public class Wheel {

        /// <summary>
        /// The MeshRenderer that displays the blurred wheel texture.
        /// </summary>
        public MeshRenderer blurredWheel;

        /// <summary>
        /// The wheel collider associated with this blurred wheel.
        /// </summary>
        public RCCP_WheelCollider wheelCollider;

        /// <summary>
        /// We maintain a manual "wheelRPM" for rotating the blur texture over time. 
        /// This is an approximate measure, not necessarily the same as the real wheelCollider rpm.
        /// </summary>
        public float wheelRPM;

    }

    /// <summary>
    /// Material used by the blurred wheels. Must have a property named "_BlurIntensity".
    /// </summary>
    public Material targetMaterial;

    /// <summary>
    /// Offset applied to each blurred wheel's localPosition to better align with actual wheel geometry.
    /// For example, you can shift wheels on the right side slightly outward or inward if needed.
    /// </summary>
    public Vector3 offset = new Vector3(.1f, 0f, 0f);

    /// <summary>
    /// Uniform scale for the blurred wheel object. 
    /// Typically a small value since the blur mesh may be scaled to match the real wheel radius visually.
    /// </summary>
    [Range(0f, .2f)] public float scale = .06f;

    /// <summary>
    /// Factor controlling how fast the blur texture spins compared to the actual wheelRPM. 
    /// A higher number results in the blur texture spinning more quickly.
    /// </summary>
    [Range(0f, 5f)] public float rotationSpeed = .25f;

    /// <summary>
    /// Internal array that stores the current blur intensity for each wheel. 
    /// This value is smoothed in Update().
    /// </summary>
    private float[] blurIntensity;

    /// <summary>
    /// Smoothing factor for changes in blur intensity (0..∞). 
    /// Higher values mean changes in blur intensity respond more slowly.
    /// </summary>
    public float smoothness = 20f;

    public override void Start() {

        base.Start();

        // Create blurred wheel renderers for each wheel collider on this vehicle.
        CreateRenderers();

    }

    /// <summary>
    /// Removes any previously created blurred wheel objects from this manager’s transform.
    /// </summary>
    public void DestroyRenderers() {

        foreach (Transform item in transform) {

            if (item != transform)
                Destroy(item.gameObject);

        }

    }

    /// <summary>
    /// Like DestroyRenderers(), but uses DestroyImmediate for editor usage (avoiding left-over objects in edit mode).
    /// </summary>
    public void DestroyRenderersEditor() {

        foreach (Transform item in transform) {

            if (item != transform)
                DestroyImmediate(item.gameObject);

        }

    }

    /// <summary>
    /// Creates a blur mesh renderer for each wheel on the vehicle. 
    /// Each blurred wheel is positioned at the same location as the real wheel model.
    /// </summary>
    public void CreateRenderers() {

        // Remove any existing blurred wheel children from previous setups.
        DestroyRenderers();

        // Get all wheel colliders on the vehicle.
        RCCP_WheelCollider[] wheelColliders = CarController.GetComponentsInChildren<RCCP_WheelCollider>();

        if (wheelColliders == null)
            return;

        if (wheelColliders.Length < 1)
            return;

        // Prepare an array for wheel blur data.
        wheelBlurRenderers = new Wheel[wheelColliders.Length];
        blurIntensity = new float[wheelBlurRenderers.Length];

        for (int i = 0; i < wheelColliders.Length; i++) {

            wheelBlurRenderers[i] = new Wheel();

            // Instantiate a blurred wheel prefab, typically assigned in RCCP_Settings (a wheel mesh that uses wheel blur shader).
            GameObject instantiatedRenderer = Instantiate(RCCPSettings.wheelBlur, transform, false);

            // Place it in the local origin for now; we’ll adjust its position in Update().
            instantiatedRenderer.transform.localPosition = Vector3.zero;
            instantiatedRenderer.transform.localRotation = Quaternion.identity;

            // Save references to mesh renderer and wheel collider.
            wheelBlurRenderers[i].blurredWheel = instantiatedRenderer.GetComponentInChildren<MeshRenderer>();
            wheelBlurRenderers[i].wheelCollider = wheelColliders[i];

            // If a custom material is provided, assign it to this blurred wheel’s mesh.
            if (targetMaterial)
                wheelBlurRenderers[i].blurredWheel.material = targetMaterial;

        }

    }

    private void LateUpdate() {

        if (wheelBlurRenderers == null)
            return;

        if (wheelBlurRenderers.Length < 1)
            return;

        // For each wheel blur, rotate the texture and adjust blur intensity based on the actual wheel rpm.
        for (int i = 0; i < wheelBlurRenderers.Length; i++) {

            if (wheelBlurRenderers[i] != null && wheelBlurRenderers[i].blurredWheel != null && wheelBlurRenderers[i].wheelCollider != null) {

                // We accumulate “wheelRPM” manually: 
                //   The real wheel speed in degrees per second ( (rpm * 360° / 60) ), 
                //   scaled by 1/5 for a more stable increment, 
                //   multiplied by deltaTime, 
                //   then added cumulatively so the texture keeps spinning over time.
                wheelBlurRenderers[i].wheelRPM += (wheelBlurRenderers[i].wheelCollider.WheelCollider.rpm * (360f / 60f)) / 5f * Time.deltaTime;

                // Gradually adjust the blur intensity. Higher wheel rpm => greater blur.
                blurIntensity[i] = Mathf.Lerp(blurIntensity[i], Mathf.Abs(wheelBlurRenderers[i].wheelCollider.WheelCollider.rpm / 2000f), Time.deltaTime * smoothness);

                // Update the material’s blur property.
                wheelBlurRenderers[i].blurredWheel.material.SetFloat("_BlurIntensity", blurIntensity[i]);

                // Match the blur wheel’s parent transform to the wheel’s position.
                wheelBlurRenderers[i].blurredWheel.transform.parent.position = wheelBlurRenderers[i].wheelCollider.wheelModel.position;

                // The parent transform’s rotation includes the main vehicle’s rotation, plus the wheel’s incremental rotation.
                wheelBlurRenderers[i].blurredWheel.transform.parent.rotation = CarController.transform.rotation * Quaternion.Euler(wheelBlurRenderers[i].wheelRPM * rotationSpeed, wheelBlurRenderers[i].wheelCollider.WheelCollider.steerAngle, 0f);

                // If the wheel is on the right side (x > 0), offset is added. If on the left, offset is subtracted.
                if (wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition.x > 0) {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition += offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);

                } else {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition -= offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, -wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                }

                // Scale the entire blurred wheel object to an appropriate size.
                wheelBlurRenderers[i].blurredWheel.transform.localScale = new Vector3(scale, scale, scale);

            }

        }

    }

    /// <summary>
    /// Resets all blur wheels to default positions and zero blur intensity (e.g., on vehicle enable or reset).
    /// </summary>
    public void Reload() {

        if (wheelBlurRenderers == null)
            return;

        if (wheelBlurRenderers.Length < 1)
            return;

        for (int i = 0; i < wheelBlurRenderers.Length; i++) {

            if (wheelBlurRenderers[i] != null && wheelBlurRenderers[i].blurredWheel != null && wheelBlurRenderers[i].wheelCollider != null) {

                // Reset wheel rpm to 0.0
                wheelBlurRenderers[i].wheelRPM = 0f;

                // Reset the material’s blur property.
                wheelBlurRenderers[i].blurredWheel.material.SetFloat("_BlurIntensity", 0f);

                // Reassign position/rotation/scale to defaults in case the vehicle was moved.
                wheelBlurRenderers[i].blurredWheel.transform.parent.position = wheelBlurRenderers[i].wheelCollider.wheelModel.position;
                wheelBlurRenderers[i].blurredWheel.transform.parent.rotation = CarController.transform.rotation * Quaternion.Euler(wheelBlurRenderers[i].wheelRPM * rotationSpeed, wheelBlurRenderers[i].wheelCollider.WheelCollider.steerAngle, 0f);

                if (wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition.x > 0) {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition += offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);

                } else {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition -= offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, -wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                }

                wheelBlurRenderers[i].blurredWheel.transform.localScale = new Vector3(scale, scale, scale);

            }

        }

    }

    /// <summary>
    /// Toggles the blurred wheels in the editor for previewing or debugging, e.g., to see if they match real wheel positions.
    /// </summary>
    public void Toggle() {

        if (wheelBlurRenderers == null)
            return;

        if (wheelBlurRenderers.Length < 1)
            return;

        for (int i = 0; i < wheelBlurRenderers.Length; i++) {

            if (wheelBlurRenderers[i] != null && wheelBlurRenderers[i].blurredWheel != null && wheelBlurRenderers[i].wheelCollider != null) {

                wheelBlurRenderers[i].blurredWheel.transform.parent.position = RCCP_GetBounds.GetBoundsCenter(wheelBlurRenderers[i].wheelCollider.wheelModel);
                wheelBlurRenderers[i].blurredWheel.transform.parent.rotation = CarController.transform.rotation * Quaternion.Euler(wheelBlurRenderers[i].wheelRPM, wheelBlurRenderers[i].wheelCollider.WheelCollider.steerAngle, 0f);

                if (wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition.x > 0) {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition += offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);

                } else {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition -= offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, -wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                }

                wheelBlurRenderers[i].blurredWheel.transform.localScale = new Vector3(scale, scale, scale);

            }

        }

    }

}
