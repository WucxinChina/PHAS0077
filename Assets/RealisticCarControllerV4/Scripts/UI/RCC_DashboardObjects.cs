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

/// <summary>
/// Receives inputs from the active vehicle in your scene and updates visual (3D) dashboard needles (not UI images).
/// This script can manage multiple dials such as RPM, speedometer, fuel, and engine heat, 
/// and can also handle interior lights based on the vehicle's status.
/// </summary>
public class RCC_DashboardObjects : RCC_Core {

    /// <summary>
    /// Represents the dial for RPM values, rotating around a specified axis with a given multiplier.
    /// </summary>
    [System.Serializable]
    public class RPMDial {

        /// <summary>
        /// The RPM dial GameObject that will rotate based on engine RPM.
        /// </summary>
        public GameObject dial;

        /// <summary>
        /// A multiplier controlling how far the dial rotates relative to the RPM.
        /// </summary>
        public float multiplier = .05f;

        /// <summary>
        /// Specifies which axis the dial should rotate around.
        /// </summary>
        public RotateAround rotateAround = RotateAround.Z;

        /// <summary>
        /// Stores the dial's original local rotation for reference.
        /// </summary>
        private Quaternion dialOrgRotation = Quaternion.identity;

        /// <summary>
        /// Optional UI Text component to display the current RPM value as text.
        /// </summary>
        public Text text;

        /// <summary>
        /// Initializes the dial by capturing its default local rotation.
        /// </summary>
        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        /// <summary>
        /// Updates the dial's rotation based on the provided RPM value.
        /// </summary>
        /// <param name="value">The current RPM value from the vehicle.</param>
        public void Update(float value) {

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

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    /// <summary>
    /// Represents the dial for speedometer values, rotating around a specified axis with a given multiplier.
    /// </summary>
    [System.Serializable]
    public class SpeedoMeterDial {

        /// <summary>
        /// The speed dial GameObject that will rotate based on vehicle speed.
        /// </summary>
        public GameObject dial;

        /// <summary>
        /// A multiplier controlling how far the dial rotates relative to the speed.
        /// </summary>
        public float multiplier = 1f;

        /// <summary>
        /// Specifies which axis the dial should rotate around.
        /// </summary>
        public RotateAround rotateAround = RotateAround.Z;

        /// <summary>
        /// Stores the dial's original local rotation for reference.
        /// </summary>
        private Quaternion dialOrgRotation = Quaternion.identity;

        /// <summary>
        /// Optional UI Text component to display the current speed as text.
        /// </summary>
        public Text text;

        /// <summary>
        /// Initializes the dial by capturing its default local rotation.
        /// </summary>
        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        /// <summary>
        /// Updates the dial's rotation based on the provided speed value.
        /// </summary>
        /// <param name="value">The current speed value from the vehicle (KMH or MPH).</param>
        public void Update(float value) {

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

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    /// <summary>
    /// Represents the dial for fuel values, rotating around a specified axis with a given multiplier.
    /// </summary>
    [System.Serializable]
    public class FuelDial {

        /// <summary>
        /// The fuel dial GameObject that will rotate based on the vehicle's current fuel level.
        /// </summary>
        public GameObject dial;

        /// <summary>
        /// A multiplier controlling how far the dial rotates relative to the fuel amount.
        /// </summary>
        public float multiplier = .1f;

        /// <summary>
        /// Specifies which axis the dial should rotate around.
        /// </summary>
        public RotateAround rotateAround = RotateAround.Z;

        /// <summary>
        /// Stores the dial's original local rotation for reference.
        /// </summary>
        private Quaternion dialOrgRotation = Quaternion.identity;

        /// <summary>
        /// Optional UI Text component to display the current fuel level as text.
        /// </summary>
        public Text text;

        /// <summary>
        /// Initializes the dial by capturing its default local rotation.
        /// </summary>
        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        /// <summary>
        /// Updates the dial's rotation based on the provided fuel value.
        /// </summary>
        /// <param name="value">The current fuel amount in the vehicle.</param>
        public void Update(float value) {

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

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    /// <summary>
    /// Represents the dial for engine heat values, rotating around a specified axis with a given multiplier.
    /// </summary>
    [System.Serializable]
    public class HeatDial {

        /// <summary>
        /// The heat dial GameObject that will rotate based on the vehicle's engine heat level.
        /// </summary>
        public GameObject dial;

        /// <summary>
        /// A multiplier controlling how far the dial rotates relative to the heat level.
        /// </summary>
        public float multiplier = .1f;

        /// <summary>
        /// Specifies which axis the dial should rotate around.
        /// </summary>
        public RotateAround rotateAround = RotateAround.Z;

        /// <summary>
        /// Stores the dial's original local rotation for reference.
        /// </summary>
        private Quaternion dialOrgRotation = Quaternion.identity;

        /// <summary>
        /// Optional UI Text component to display the current heat level as text.
        /// </summary>
        public Text text;

        /// <summary>
        /// Initializes the dial by capturing its default local rotation.
        /// </summary>
        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        /// <summary>
        /// Updates the dial's rotation based on the provided heat value.
        /// </summary>
        /// <param name="value">The current engine heat of the vehicle.</param>
        public void Update(float value) {

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

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    /// <summary>
    /// Manages the interior lighting of the dashboard, enabling and disabling it based on vehicle states.
    /// </summary>
    [System.Serializable]
    public class InteriorLight {

        /// <summary>
        /// The Light component to be controlled.
        /// </summary>
        public Light light;

        /// <summary>
        /// The maximum intensity of the interior light when active.
        /// </summary>
        public float intensity = 1f;

        /// <summary>
        /// The render mode of the light (Auto, Important, or NotImportant).
        /// </summary>
        public LightRenderMode renderMode = LightRenderMode.Auto;

        /// <summary>
        /// Updates the interior light's intensity based on the specified state.
        /// </summary>
        /// <param name="state">If true, sets the light to full intensity; otherwise, sets intensity to zero.</param>
        public void Update(bool state) {

            if (!light.enabled)
                light.enabled = true;

            light.renderMode = renderMode;
            light.intensity = state ? intensity : 0f;

        }

    }

    /// <summary>
    /// Dial object handling RPM needle behavior and optional readout.
    /// </summary>
    [Space()]
    public RPMDial rPMDial = new RPMDial();

    /// <summary>
    /// Dial object handling speedometer needle behavior and optional readout.
    /// </summary>
    [Space()]
    public SpeedoMeterDial speedDial = new SpeedoMeterDial();

    /// <summary>
    /// Dial object handling fuel level needle behavior and optional readout.
    /// </summary>
    [Space()]
    public FuelDial fuelDial = new FuelDial();

    /// <summary>
    /// Dial object handling engine heat needle behavior and optional readout.
    /// </summary>
    [Space()]
    public HeatDial heatDial = new HeatDial();

    /// <summary>
    /// Array of interior lights that can be toggled based on vehicle headlight status.
    /// </summary>
    [Space()]
    public InteriorLight[] interiorLights = new InteriorLight[0];

    /// <summary>
    /// Defines which axis a dial should rotate around.
    /// </summary>
    public enum RotateAround { X, Y, Z }

    /// <summary>
    /// Captures default dial rotations on Awake.
    /// </summary>
    private void Awake() {

        //  Initializing dials.
        rPMDial.Init();
        speedDial.Init();
        fuelDial.Init();
        heatDial.Init();

    }

    /// <summary>
    /// Updates dashboard dials and interior lights each frame.
    /// </summary>
    private void Update() {

        //  If no vehicle is assigned, skip updating dials/lights.
        if (!CarController)
            return;

        Dials();
        Lights();

    }

    /// <summary>
    /// Updates dial rotations based on the active vehicle's RPM, speed, fuel, and engine heat.
    /// </summary>
    private void Dials() {

        if (rPMDial.dial != null)
            rPMDial.Update(CarController.engineRPM);

        if (speedDial.dial != null)
            speedDial.Update(CarController.speed);

        if (fuelDial.dial != null)
            fuelDial.Update(CarController.fuelTank);

        if (heatDial.dial != null)
            heatDial.Update(CarController.engineHeat);

    }

    /// <summary>
    /// Updates the interior lights according to whether the vehicle's headlights are on.
    /// </summary>
    private void Lights() {

        for (int i = 0; i < interiorLights.Length; i++)
            interiorLights[i].Update(CarController.lowBeamHeadLightsOn);

    }

}
