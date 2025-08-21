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
/// Represents a station that repairs any RCC vehicle entering its trigger zone.
/// When a vehicle collider enters, the script locates the car controller and calls Repair().
/// </summary>
public class RCC_RepairStation : RCC_Core {

    /// <summary>
    /// Reference to the vehicle currently inside the repair zone.
    /// </summary>
    private RCC_CarControllerV4 targetVehicle;

    /// <summary>
    /// An optional text object (UI element) that can be used to label or signal the repair station.
    /// This text is oriented to face the main camera each frame.
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
    /// When a collider enters this trigger, check if it's a vehicle and repair it immediately.
    /// </summary>
    /// <param name="col">The collider entering the zone.</param>
    private void OnTriggerEnter(Collider col) {

        // Ignore trigger-only colliders.
        if (col.isTrigger)
            return;

        // Attempt to find an RCC_CarControllerV4 in the entering object.
        if (targetVehicle == null)
            targetVehicle = col.gameObject.GetComponentInParent<RCC_CarControllerV4>();

        // If found, repair the vehicle.
        if (targetVehicle)
            targetVehicle.Repair();

    }

    private void Update() {

        // If a text object is assigned, orient it toward the main camera for visibility.
        if (text && mCamera)
            text.transform.rotation = mCamera.transform.rotation;

    }

    /// <summary>
    /// Clears the reference to the vehicle upon exiting the trigger.
    /// </summary>
    /// <param name="col">The collider exiting the zone.</param>
    private void OnTriggerExit(Collider col) {

        // If no vehicle is stored, nothing to clear.
        if (!targetVehicle)
            return;

        // If the exiting collider belongs to the stored vehicle, unset targetVehicle.
        if (col.gameObject.GetComponentInParent<RCC_CarControllerV4>())
            targetVehicle = null;

    }

}
