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

/// <summary>
/// Represents a fuel station area. When a vehicle with RCC_CarControllerV4 enters this trigger,
/// it gradually refills the vehicle's fuel tank at the specified rate.
/// </summary>
public class RCC_FuelStation : RCC_Core {

    /// <summary>
    /// Reference to the vehicle currently within the fuel station trigger.
    /// </summary>
    private RCC_CarControllerV4 targetVehicle;

    /// <summary>
    /// The rate (units per second) at which the vehicle's fuel tank is replenished.
    /// </summary>
    public float refillSpeed = 1f;

    /// <summary>
    /// An optional GameObject (e.g., UI text) indicating the fuel station, which 
    /// will be oriented toward the main camera each frame for readability.
    /// </summary>
    public GameObject text;

    private Camera mCamera;

    void Awake() {
        // 1) grab whatever camera is Main right now
        mCamera = RCC_MainCameraProvider.MainCamera;

        // 2) listen for any camera‐tag‐swaps later on
        RCC_MainCameraProvider.OnMainCameraChanged += HandleMainCameraChanged;
    }

    void OnDestroy() {
        // clean up your subscription
        RCC_MainCameraProvider.OnMainCameraChanged -= HandleMainCameraChanged;
    }

    private void HandleMainCameraChanged(Camera cam) {
        // update your local reference
        mCamera = cam;
    }

    /// <summary>
    /// Detects and stores a reference to any vehicle entering the fuel station trigger.
    /// </summary>
    /// <param name="col">The collider that enters the trigger.</param>
    private void OnTriggerStay(Collider col) {

        targetVehicle = col.gameObject.GetComponentInParent<RCC_CarControllerV4>();

    }

    private void Update() {

        // Orients the optional text object to face the camera if present.
        if (text && mCamera)
            text.transform.rotation = mCamera.transform.rotation;

        // If there's no vehicle inside the trigger, do nothing.
        if (!targetVehicle)
            return;

        // Gradually refill the vehicle's fuel tank at the specified rate.
        targetVehicle.fuelTank += refillSpeed * Time.deltaTime;

    }

    /// <summary>
    /// Clears the reference to the vehicle when it leaves the fuel station trigger.
    /// </summary>
    /// <param name="col">The collider that exits the trigger.</param>
    private void OnTriggerExit(Collider col) {

        if (col.gameObject.GetComponentInParent<RCC_CarControllerV4>())
            targetVehicle = null;

    }

}
