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
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reads vehicle parameters (RPM, speed, steering angle, lights) and reflects them on a dashboard model.
/// Can rotate a steering wheel transform, dial needle transforms, and toggle interior lights.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Visual Dashboard")]
public class RCCP_Visual_Dashboard : RCCP_Component {

    /// <summary>
    /// Optional steering wheel transform inside the vehicle’s interior. 
    /// This wheel is rotated based on the actual wheel steering angle.
    /// </summary>
    public Transform steeringWheel;

    /// <summary>
    /// Initial local rotation of the steering wheel, captured at runtime to offset further rotations.
    /// </summary>
    private Quaternion orgSteeringWheelRot;

    /// <summary>
    /// Determines which local axis the steering wheel is rotated around when turning.
    /// </summary>
    public enum SteeringWheelRotateAround { XAxis, YAxis, ZAxis }
    public SteeringWheelRotateAround steeringWheelRotateAround = SteeringWheelRotateAround.ZAxis;

    /// <summary>
    /// Multiply the vehicle's steer angle by this factor to get the steering wheel rotation.
    /// </summary>
    public float steeringAngleMultiplier = 3f;

    /// <summary>
    /// Smoothing factor for steering wheel rotation, to prevent abrupt changes.
    /// </summary>
    [Min(0f)] public float steeringAngleSmoother = 5f;

    /// <summary>
    /// Represents a physical dial/needle (e.g., for RPM or speed).
    /// </summary>
    [System.Serializable]
    public class RPMDial {

        /// <summary>
        /// Dial GameObject in the scene. Usually has a needle that rotates around some axis.
        /// </summary>
        public GameObject dial;

        /// <summary>
        /// Factor to multiply the source value (RPM or speed) before applying rotation.
        /// </summary>
        [Min(0f)] public float multiplier = .05f;

        /// <summary>
        /// Which axis the dial needle rotates around (X, Y, or Z).
        /// </summary>
        public RotateAround rotateAround = RotateAround.Z;

        /// <summary>
        /// The dial's initial local rotation, used as a baseline.
        /// </summary>
        private Quaternion dialOrgRotation = Quaternion.identity;

        /// <summary>
        /// Optional TextMeshPro display, for showing numeric values (e.g., "RPM" or "speed").
        /// </summary>
        public TextMeshPro text;

        /// <summary>
        /// Initializes dialOrgRotation if the dial is assigned.
        /// </summary>
        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        /// <summary>
        /// Rotates the dial needle based on a given value (e.g., rpm, speed).
        /// If a text field is present, updates it to show the value.
        /// </summary>
        /// <param name="value">Numeric value to display and rotate the needle.</param>
        public void Update(float value) {

            // Determine the axis to rotate around
            Vector3 targetAxis = Vector3.forward;

            switch (rotateAround) {

                case RotateAround.X:
                    targetAxis = Vector3.right;
                    break;
                case RotateAround.Y:
                    targetAxis = Vector3.up;
                    break;
                case RotateAround.Z:
                    targetAxis = Vector3.forward;
                    break;

            }

            // Apply a negative rotation to rotate the needle "down" as the value increases
            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    /// <summary>
    /// Encapsulates an interior light (e.g., a dash or cockpit light) that can be toggled or adjusted.
    /// </summary>
    [System.Serializable]
    public class InteriorLight {

        public Light lightSource;
        [Range(0f, 10f)] public float intensity = 1f;
        public LightRenderMode renderMode = LightRenderMode.Auto;

        /// <summary>
        /// Called once at startup to configure the light’s render mode, if set.
        /// </summary>
        public void Init() {

            if (lightSource)
                lightSource.renderMode = renderMode;

        }

        /// <summary>
        /// Updates the light intensity based on whether it should be on or off.
        /// </summary>
        /// <param name="state">True to enable, false to disable.</param>
        public void Update(bool state) {

            if (!lightSource)
                return;

            if (!lightSource.enabled)
                lightSource.enabled = true;

            lightSource.intensity = state ? intensity : 0f;

        }

    }

    /// <summary>
    /// Dial for displaying RPM, e.g., a needle that rotates proportionally to engineRPM.
    /// </summary>
    [Space()] public RPMDial rPMDial = new RPMDial();

    /// <summary>
    /// Dial for displaying speed, e.g., a needle that rotates proportionally to km/h.
    /// </summary>
    [Space()] public RPMDial speedDial = new RPMDial();

    /// <summary>
    /// Array of interior lights (e.g., dash lights) that can turn on when low beams are enabled.
    /// </summary>
    [Space()] public InteriorLight[] interiorLights = new InteriorLight[0];

    /// <summary>
    /// Axis options for rotating a dial or needle. Similar to SteeringWheelRotateAround but used for dials.
    /// </summary>
    public enum RotateAround { X, Y, Z }

    public override void Start() {

        base.Start();

        // Initialize references for the dials
        rPMDial.Init();
        speedDial.Init();

        // Initialize references for any interior lights
        for (int i = 0; i < interiorLights.Length; i++)
            interiorLights[i].Init();

    }

    private void Update() {

        SteeringWheel();
        Dials();
        Lights();

    }

    /// <summary>
    /// Rotates the steering wheel model based on the vehicle’s current steer angle.
    /// </summary>
    private void SteeringWheel() {

        if (steeringWheel) {

            // Store the original localRotation if not yet stored
            if (orgSteeringWheelRot.eulerAngles == Vector3.zero)
                orgSteeringWheelRot = steeringWheel.transform.localRotation;

            Quaternion targetRotation = Quaternion.identity;

            switch (steeringWheelRotateAround) {

                case SteeringWheelRotateAround.XAxis:
                    targetRotation = orgSteeringWheelRot * Quaternion.AngleAxis(CarController.steerAngle * steeringAngleMultiplier, -Vector3.right);
                    break;
                case SteeringWheelRotateAround.YAxis:
                    targetRotation = orgSteeringWheelRot * Quaternion.AngleAxis(CarController.steerAngle * steeringAngleMultiplier, -Vector3.up);
                    break;
                case SteeringWheelRotateAround.ZAxis:
                    targetRotation = orgSteeringWheelRot * Quaternion.AngleAxis(CarController.steerAngle * steeringAngleMultiplier, -Vector3.forward);
                    break;

            }

            steeringWheel.transform.localRotation = Quaternion.Slerp(
                steeringWheel.transform.localRotation,
                targetRotation,
                Time.deltaTime * steeringAngleSmoother * 3f
            );

        }

    }

    /// <summary>
    /// Updates the RPM and speed dials by rotating them to match the engine RPM and vehicle speed.
    /// </summary>
    private void Dials() {

        if (rPMDial.dial != null)
            rPMDial.Update(CarController.engineRPM);

        if (speedDial.dial != null)
            speedDial.Update(CarController.absoluteSpeed);

    }

    /// <summary>
    /// Toggles interior lights (dashboard glow, etc.) when the vehicle’s low-beam headlights are on.
    /// </summary>
    private void Lights() {

        if (!CarController.Lights)
            return;

        for (int i = 0; i < interiorLights.Length; i++)
            interiorLights[i].Update(CarController.Lights.lowBeamHeadlights);

    }

    public void Reload() {

        // Currently no specialized reload logic needed.

    }

    private void OnValidate() {

        if (interiorLights != null && interiorLights.Length >= 1) {

            for (int i = 0; i < interiorLights.Length; i++) {

                if (interiorLights[i].intensity < .01f)
                    interiorLights[i].intensity = 1f;

            }

        }

    }

}
