//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// A simple manager script for RCC demo scenes. 
/// It can spawn new player vehicles, set behavior modes, restart scenes, and quit the application.
/// Also supports Photon networking for spawning vehicles in multiplayer scenarios.
/// </summary>
public class RCC_Demo : RCC_Core {

    /// <summary>
    /// Index for selecting which vehicle prefab to spawn.
    /// </summary>
    [HideInInspector] public int selectedVehicleIndex = 0;

    /// <summary>
    /// Index for selecting which behavior mode to apply.
    /// </summary>
    [HideInInspector] public int selectedBehaviorIndex = 0;

    /// <summary>
    /// Sets the selected vehicle index. 
    /// Used for spawning a new vehicle from the RCC_DemoVehicles list.
    /// </summary>
    /// <param name="index">The index of the vehicle in the RCC_DemoVehicles array.</param>
    public void SelectVehicle(int index) {
        selectedVehicleIndex = index;
    }

#if PHOTON_UNITY_NETWORKING && RCC_PHOTON
    /// <summary>
    /// Sets the selected photon vehicle index. 
    /// Used for spawning a new networked vehicle from the RCC_DemoVehicles list.
    /// </summary>
    /// <param name="index">The index of the Photon vehicle in the RCC_DemoVehicles array.</param>
    public void SelectPhotonVehicle(int index) {
        selectedVehicleIndex = index;
    }
#endif

    /// <summary>
    /// Spawns a player vehicle at the last known position/rotation, or at the camera's position/rotation if none exist.
    /// If a vehicle is already in the scene, it will be removed before spawning the new one.
    /// </summary>
    public void Spawn() {

        // Store the last known position and rotation of the active player vehicle, if any.
        Vector3 lastKnownPos = Vector3.zero;
        Quaternion lastKnownRot = Quaternion.identity;

        Vector3 lastVelocity = Vector3.zero;
        Vector3 lastAngularVelocity = Vector3.zero;

        // Reference to the previously active player vehicle, if any.
        RCC_CarControllerV4 lastVehicle = RCCSceneManager.activePlayerVehicle;

        // Retrieve the current player vehicle (if one exists) from the scene.
        if (lastVehicle) {

            lastKnownPos = lastVehicle.transform.position;
            lastKnownRot = lastVehicle.transform.rotation;
            lastVelocity = lastVehicle.Rigid.linearVelocity;
            lastAngularVelocity = lastVehicle.Rigid.angularVelocity;

        }

        // If no player vehicle position/rotation is found, use the camera's position/rotation.
        if (lastKnownPos == Vector3.zero) {

            if (RCCSceneManager.activePlayerCamera) {

                lastKnownPos = RCCSceneManager.activePlayerCamera.transform.position;
                lastKnownRot = RCCSceneManager.activePlayerCamera.transform.rotation;

            }

        }

        lastKnownPos.y += .5f;

        // Keep only the Y angle from the last known rotation (no pitch/roll).
        lastKnownRot.x = 0f;
        lastKnownRot.z = 0f;

        lastVelocity.y = 0f;
        lastAngularVelocity.x = 0f;
        lastAngularVelocity.z = 0f;

#if BCG_ENTEREXIT
        BCG_EnterExitVehicle lastEnterExitVehicle;
        bool enterExitVehicleFound = false;

        // If the last vehicle has a driver, make the driver exit before destroying the vehicle.
        if (lastVehicle) {

            lastEnterExitVehicle = lastVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

            if (lastEnterExitVehicle && lastEnterExitVehicle.driver) {

                enterExitVehicleFound = true;
                lastEnterExitVehicle.driver.GetOut();

            }

        }
#endif

        // If a player vehicle is present, destroy it to spawn a new one.
        if (lastVehicle) {

            RCC.DeRegisterPlayerVehicle();
            Destroy(lastVehicle.gameObject);

        }

        // Spawn a new vehicle using the selected index from RCC_DemoVehicles.
        RCC_CarControllerV4 spawnedVehicle = RCC.SpawnRCC(
            RCC_DemoVehicles.Instance.vehicles[selectedVehicleIndex],
            lastKnownPos,
            lastKnownRot,
            registerAsPlayerVehicle: true,
            isControllable: true,
            isEngineRunning: true
        );

        spawnedVehicle.Rigid.linearVelocity = lastVelocity;
        spawnedVehicle.Rigid.angularVelocity = lastAngularVelocity;
        Physics.SyncTransforms();

#if BCG_ENTEREXIT
        // If the previous vehicle had a driver who exited, reassign the driver to the new vehicle if possible.
        if (enterExitVehicleFound) {

            lastEnterExitVehicle = null;
            lastEnterExitVehicle = RCC_SceneManager.Instance.activePlayerVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

            if (!lastEnterExitVehicle)
                lastEnterExitVehicle = RCC_SceneManager.Instance.activePlayerVehicle.gameObject.AddComponent<BCG_EnterExitVehicle>();

            if (BCG_EnterExitManager.Instance.activePlayer && lastEnterExitVehicle && lastEnterExitVehicle.driver == null) {

                BCG_EnterExitManager.Instance.activePlayer.GetIn(lastEnterExitVehicle);

            }
        }
#endif

    }

#if PHOTON_UNITY_NETWORKING && RCC_PHOTON
    /// <summary>
    /// Spawns a Photon-enabled player vehicle at the last known position/rotation, or at the camera's if none exist.
    /// If a vehicle is already in the scene, it will be removed before spawning the new Photon vehicle.
    /// </summary>
    public void SpawnPhoton() {

        // Store the last known position and rotation of the active player vehicle, if any.
        Vector3 lastKnownPos = Vector3.zero;
        Quaternion lastKnownRot = Quaternion.identity;

        // Reference to the previously active player vehicle, if any.
        RCC_CarControllerV4 lastVehicle = RCCSceneManager.activePlayerVehicle;

        // Retrieve the current player vehicle (if one exists) from the scene.
        if (lastVehicle) {

            lastKnownPos = lastVehicle.transform.position;
            lastKnownRot = lastVehicle.transform.rotation;

        }

        // If no player vehicle position/rotation is found, use the camera's position/rotation.
        if (lastKnownPos == Vector3.zero) {

            if (RCCSceneManager.activePlayerCamera) {

                lastKnownPos = RCCSceneManager.activePlayerCamera.transform.position;
                lastKnownRot = RCCSceneManager.activePlayerCamera.transform.rotation;

            }

        }

        // Keep only the Y angle from the last known rotation (no pitch/roll).
        lastKnownRot.x = 0f;
        lastKnownRot.z = 0f;

        // Destroy the last vehicle over the network if it exists.
        if (lastVehicle) {

            RCC.DeRegisterPlayerVehicle();
            PhotonNetwork.Destroy(lastVehicle.gameObject);

        }

        // Instantiate a new Photon vehicle from the "Photon Vehicles" folder using the selected index.
        RCC_CarControllerV4 spawnedPhotonVehicle = PhotonNetwork
            .Instantiate(
                "Photon Vehicles/" + RCC_DemoVehicles.Instance.vehicles[selectedVehicleIndex].gameObject.name,
                lastKnownPos,
                lastKnownRot
            )
            .GetComponent<RCC_CarControllerV4>();

        // Register the spawned photon vehicle as the player's vehicle, making it controllable with engine running.
        RCC.RegisterPlayerVehicle(spawnedPhotonVehicle, isControllable: true, engineState: true);

    }
#endif

    /// <summary>
    /// Sets the selected behavior index. This index can be used to switch driving styles/handling in RCC.
    /// </summary>
    /// <param name="index">The index of the behavior mode in RCC_Settings.</param>
    public void SetBehavior(int index) {

        selectedBehaviorIndex = index;

    }

    /// <summary>
    /// Applies the selected behavior to the current vehicle, such as different handling or driving styles.
    /// </summary>
    public void InitBehavior() {

        RCC.SetBehavior(selectedBehaviorIndex);

    }

    /// <summary>
    /// Sets the mobile controller type (Touch, Gyro, Steering Wheel, or Joystick).
    /// </summary>
    /// <param name="index">
    /// 0 = TouchScreen,
    /// 1 = Gyro,
    /// 2 = SteeringWheel,
    /// 3 = Joystick
    /// </param>
    public void SetMobileController(int index) {

        switch (index) {

            case 0:
                RCC.SetMobileController(RCC_Settings.MobileController.TouchScreen);
                break;
            case 1:
                RCC.SetMobileController(RCC_Settings.MobileController.Gyro);
                break;
            case 2:
                RCC.SetMobileController(RCC_Settings.MobileController.SteeringWheel);
                break;
            case 3:
                RCC.SetMobileController(RCC_Settings.MobileController.Joystick);
                break;

        }

    }

    /// <summary>
    /// Sets the quality level of the current scene, matching Unity's predefined quality settings.
    /// </summary>
    /// <param name="index">An index corresponding to a quality level in Unity's Quality Settings.</param>
    public void SetQuality(int index) {

        QualitySettings.SetQualityLevel(index);

    }

    /// <summary>
    /// Restarts the current scene by reloading it from the build settings.
    /// </summary>
    public void RestartScene() {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    /// <summary>
    /// Quits the application. This will not work in the Unity Editor.
    /// </summary>
    public void Quit() {

        Application.Quit();

    }

}
