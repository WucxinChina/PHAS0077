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
/// Manager for upgradable wheels.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Wheel Manager")]
public class RCCP_VehicleUpgrade_WheelManager : RCCP_UpgradeComponent, IRCCP_UpgradeComponent {

    /// <summary>
    /// Current wheel index.
    /// </summary>
    [Min(-1)] public int wheelIndex = -1;

    /// <summary>
    /// Default wheel index for restoration.
    /// </summary>
    private int defaultWheelIndex = -1;

    /// <summary>
    /// List to keep track of instantiated wheel models for proper cleanup.
    /// </summary>
    private List<GameObject> instantiatedWheels = new List<GameObject>();

    /// <summary>
    /// Default wheel model reference.
    /// </summary>
    private GameObject DefaultWheelObj {

        get {

            // Getting default wheel model
            if (defaultWheelObj == null) {

                RCCP_WheelCollider foundWheel = CarController.GetComponentInChildren<RCCP_WheelCollider>();
                GameObject defaultWheelRef = null;

                if (foundWheel != null && foundWheel.wheelModel != null)
                    defaultWheelRef = foundWheel.wheelModel.gameObject;

                if (defaultWheelRef != null) {

                    defaultWheelObj = Instantiate(defaultWheelRef, transform);
                    defaultWheelObj.transform.localPosition = Vector3.zero;
                    defaultWheelObj.transform.localRotation = Quaternion.identity;
                    defaultWheelObj.transform.localScale = Vector3.one;
                    defaultWheelObj.SetActive(false);

                }

            }

            return defaultWheelObj;

        }

    }

    private GameObject defaultWheelObj;

    /// <summary>
    /// Initializing the wheel manager.
    /// </summary>
    public void Initialize() {

        // Store the default wheel reference
        GameObject defaultWheel = DefaultWheelObj;

        if (defaultWheel != null)
            defaultWheel.SetActive(false);

        // Store default wheel index as -1 (indicating default wheels)
        defaultWheelIndex = -1;

        // If last selected wheel found, change the wheel
        wheelIndex = Loadout.wheel;

        if (wheelIndex != -1) {

            ChangeWheels(RCCPChangableWheels.wheels[wheelIndex].wheel, true);

        } else {

            Restore();

        }

    }

    /// <summary>
    /// Changes the wheel with the target wheel index.
    /// </summary>
    /// <param name="index">Index of the wheel in RCCPChangableWheels.</param>
    public void UpdateWheel(int index) {

        // Setting wheel index
        wheelIndex = index;

        // Return if wheel index is not set
        if (wheelIndex == -1) {

            Restore();
            return;

        }

        // Validate wheel index
        if (RCCPChangableWheels.wheels == null || wheelIndex >= RCCPChangableWheels.wheels.Length) {

            Debug.LogError("RCCP_ChangableWheels doesn't have wheelIndex " + wheelIndex.ToString());
            return;

        }

        // Check if wheel exists at index
        if (RCCPChangableWheels.wheels[wheelIndex] == null || RCCPChangableWheels.wheels[wheelIndex].wheel == null) {

            Debug.LogError("RCCP_ChangableWheels wheel at index " + wheelIndex.ToString() + " is null");
            return;

        }

        // Changing the wheels
        ChangeWheels(RCCPChangableWheels.wheels[wheelIndex].wheel, true);

        // Refreshing the loadout
        Refresh(this);

        // Saving the loadout
        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Changes the wheel with the target wheel index without saving.
    /// </summary>
    /// <param name="index">Index of the wheel in RCCPChangableWheels.</param>
    public void UpdateWheelWithoutSave(int index) {

        // Setting wheel index
        wheelIndex = index;

        // Return if wheel index is not set
        if (wheelIndex == -1) {

            Restore();
            return;

        }

        // Validate wheel index
        if (RCCPChangableWheels.wheels == null || wheelIndex >= RCCPChangableWheels.wheels.Length) {

            Debug.LogError("RCCP_ChangableWheels doesn't have wheelIndex " + wheelIndex.ToString());
            return;

        }

        // Check if wheel exists at index
        if (RCCPChangableWheels.wheels[wheelIndex] == null || RCCPChangableWheels.wheels[wheelIndex].wheel == null) {

            Debug.LogError("RCCP_ChangableWheels wheel at index " + wheelIndex.ToString() + " is null");
            return;

        }

        // Changing the wheels
        ChangeWheels(RCCPChangableWheels.wheels[wheelIndex].wheel, true);

    }

    /// <summary>
    /// Change wheel models. You can find your wheel models array in Tools --> BCG --> RCCP --> Configure Changable Wheels.
    /// </summary>
    /// <param name="wheel">Wheel prefab to instantiate.</param>
    /// <param name="applyRadius">Apply wheel radius based on wheel bounds.</param>
    public void ChangeWheels(GameObject wheel, bool applyRadius) {

        // Return if no wheel or wheel is deactivated
        if (!wheel || (wheel && !wheel.activeSelf))
            return;

        // Return if no any wheelcolliders found
        if (CarController.AllWheelColliders == null)
            return;

        // Return if no any wheelcolliders found
        if (CarController.AllWheelColliders.Length < 1)
            return;

        // Clean up previously instantiated wheels
        CleanupInstantiatedWheels();

        // Looping all wheelcolliders
        for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

            RCCP_WheelCollider wheelCollider = CarController.AllWheelColliders[i];

            if (wheelCollider != null && wheelCollider.wheelModel != null) {

                // Disabling all child models of the wheel
                Transform[] childTransforms = wheelCollider.wheelModel.GetComponentsInChildren<Transform>();

                foreach (Transform t in childTransforms) {

                    if (t != null && t != wheelCollider.wheelModel)
                        t.gameObject.SetActive(false);

                }

                // Instantiating new wheel model
                GameObject newWheel = Instantiate(wheel, wheelCollider.transform.position, wheelCollider.transform.rotation, wheelCollider.wheelModel);
                newWheel.transform.localPosition = Vector3.zero;
                newWheel.transform.localRotation = Quaternion.identity;
                newWheel.SetActive(true);

                // Add to tracked list for cleanup
                instantiatedWheels.Add(newWheel);

                // If wheel is at right side, multiply scale X by -1 for symmetry
                if (wheelCollider.transform.localPosition.x > 0f)
                    newWheel.transform.localScale = new Vector3(newWheel.transform.localScale.x * -1f, newWheel.transform.localScale.y, newWheel.transform.localScale.z);

                // If apply radius is set to true, calculate the radius
                if (applyRadius)
                    wheelCollider.WheelCollider.radius = RCCP_GetBounds.MaxBoundsExtent(wheel.transform);

            }

        }

    }

    /// <summary>
    /// Cleans up previously instantiated wheel models to prevent memory leaks.
    /// </summary>
    private void CleanupInstantiatedWheels() {

        foreach (GameObject wheelObj in instantiatedWheels) {

            if (wheelObj != null)
                DestroyImmediate(wheelObj);

        }

        instantiatedWheels.Clear();

    }

    /// <summary>
    /// Restores the wheels to default.
    /// </summary>
    public void Restore() {

        // Set wheel index back to default
        wheelIndex = defaultWheelIndex;

        // Clean up any instantiated wheels
        CleanupInstantiatedWheels();

        // Return if no wheel colliders
        if (CarController.AllWheelColliders == null || CarController.AllWheelColliders.Length < 1)
            return;

        // Restore all wheel models to their original state
        for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

            RCCP_WheelCollider wheelCollider = CarController.AllWheelColliders[i];

            if (wheelCollider != null && wheelCollider.wheelModel != null) {

                // Re-enable all original child models
                Transform[] childTransforms = wheelCollider.wheelModel.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in childTransforms) {

                    if (t != null)
                        t.gameObject.SetActive(true);

                }

            }

        }

    }

    /// <summary>
    /// Clean up when component is destroyed.
    /// </summary>
    private void OnDestroy() {

        CleanupInstantiatedWheels();

        if (defaultWheelObj != null)
            DestroyImmediate(defaultWheelObj);

    }

}