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
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the vehicle selection system in the RCC demo scene.
/// This script handles spawning vehicles, switching between them, 
/// registering the selected vehicle as the player's car, and transitioning to the next scene.
/// </summary>
public class RCC_CarSelectionExample : RCC_Core {

    /// <summary>
    /// List of spawned vehicles. Vehicles are instantiated only once and stored here 
    /// to prevent redundant instantiation.
    /// </summary>
    private List<RCC_CarControllerV4> _spawnedVehicles = new List<RCC_CarControllerV4>();

    /// <summary>
    /// Transform specifying the vehicle spawn position and rotation.
    /// </summary>
    public Transform spawnPosition;

    /// <summary>
    /// Index of the currently selected vehicle. 
    /// It is modified using the next and previous selection buttons.
    /// </summary>
    public int selectedIndex = 0;

    /// <summary>
    /// Reference to the UI Canvas for RCC.
    /// </summary>
    public GameObject RCCCanvas;

    /// <summary>
    /// Reference to the RCC camera used for vehicle selection.
    /// </summary>
    public RCC_Camera RCCCamera;

    /// <summary>
    /// Name of the next scene to load after selecting a vehicle.
    /// </summary>
    public string nextScene;

    private void Start() {

        // Assign the active RCC camera if not manually assigned.
        if (!RCCCamera)
            RCCCamera = RCCSceneManager.activePlayerCamera;

        // Retrieve the last selected vehicle index from PlayerPrefs.
        selectedIndex = PlayerPrefs.GetInt("SelectedRCCVehicle", 0);

        // Instantiate all vehicles at once and store them in the list.
        CreateVehicles();

    }

    /// <summary>
    /// Instantiates all available vehicles and stores them in the spawned vehicle list.
    /// </summary>
    private void CreateVehicles() {

        for (int i = 0; i < RCC_DemoVehicles.Instance.vehicles.Length; i++) {

            // Spawn vehicle without control, engine off, and not registered as a player vehicle.
            RCC_CarControllerV4 spawnedVehicle = RCC.SpawnRCC(RCC_DemoVehicles.Instance.vehicles[i], spawnPosition.position, spawnPosition.rotation, false, false, false);

            // Disable the spawned vehicle initially.
            spawnedVehicle.gameObject.SetActive(false);

            // Store the vehicle in the list.
            _spawnedVehicles.Add(spawnedVehicle);

        }

        // Activate the initially selected vehicle.
        SpawnVehicle();

        // Enable orbiting camera for vehicle selection if RCCCamera is assigned.
        if (RCCCamera && RCCCamera.GetComponent<RCC_CameraCarSelection>())
            RCCCamera.GetComponent<RCC_CameraCarSelection>().enabled = true;

        // Disable the UI canvas until a vehicle is selected.
        if (RCCCanvas)
            RCCCanvas.SetActive(false);

    }

    /// <summary>
    /// Selects the next vehicle in the list.
    /// </summary>
    public void NextVehicle() {

        selectedIndex++;

        // Loop back to the first vehicle if the index exceeds the maximum.
        if (selectedIndex > _spawnedVehicles.Count - 1)
            selectedIndex = 0;

        SpawnVehicle();

    }

    /// <summary>
    /// Selects the previous vehicle in the list.
    /// </summary>
    public void PreviousVehicle() {

        selectedIndex--;

        // Loop back to the last vehicle if the index is below 0.
        if (selectedIndex < 0)
            selectedIndex = _spawnedVehicles.Count - 1;

        SpawnVehicle();

    }

    /// <summary>
    /// Activates the currently selected vehicle while disabling all others.
    /// </summary>
    public void SpawnVehicle() {

        // Disable all vehicles.
        for (int i = 0; i < _spawnedVehicles.Count; i++)
            _spawnedVehicles[i].gameObject.SetActive(false);

        // Enable only the selected vehicle.
        _spawnedVehicles[selectedIndex].gameObject.SetActive(true);

        // Set the selected vehicle as the active player vehicle.
        RCCSceneManager.activePlayerVehicle = _spawnedVehicles[selectedIndex];

    }

    /// <summary>
    /// Registers the selected vehicle as the player's car and enables controls.
    /// </summary>
    public void SelectVehicle() {

        // Register the selected vehicle as the player vehicle.
        RCC.RegisterPlayerVehicle(_spawnedVehicles[selectedIndex]);

        // Start the engine and enable control for the selected vehicle.
        _spawnedVehicles[selectedIndex].StartEngine();
        _spawnedVehicles[selectedIndex].SetCanControl(true);

        // Save the selected vehicle index for use in the next scene.
        PlayerPrefs.SetInt("SelectedRCCVehicle", selectedIndex);

        // Disable orbiting camera and switch to third-person camera mode.
        if (RCCCamera) {

            if (RCCCamera.GetComponent<RCC_CameraCarSelection>())
                RCCCamera.GetComponent<RCC_CameraCarSelection>().enabled = false;

            RCCCamera.ChangeCamera(RCC_Camera.CameraMode.TPS);

        }

        // Enable UI elements for the game.
        if (RCCCanvas)
            RCCCanvas.SetActive(true);

        // Load the next scene if specified.
        if (!string.IsNullOrEmpty(nextScene))
            OpenScene();

    }

    /// <summary>
    /// Deselects the currently selected vehicle and returns to the selection menu.
    /// </summary>
    public void DeSelectVehicle() {

        // De-register the vehicle as the player vehicle.
        RCC.DeRegisterPlayerVehicle();

        // Reset the vehicle's position and rotation to the spawn position.
        _spawnedVehicles[selectedIndex].transform.position = spawnPosition.position;
        _spawnedVehicles[selectedIndex].transform.rotation = spawnPosition.rotation;

        // Stop the engine and disable vehicle controls.
        _spawnedVehicles[selectedIndex].KillEngine();
        _spawnedVehicles[selectedIndex].SetCanControl(false);

        // Reset vehicle velocity and inertia.
        Rigidbody vehicleRigidbody = _spawnedVehicles[selectedIndex].GetComponent<Rigidbody>();
        vehicleRigidbody.ResetInertiaTensor();
        vehicleRigidbody.linearVelocity = Vector3.zero;
        vehicleRigidbody.angularVelocity = Vector3.zero;

        // Enable orbiting camera for vehicle selection.
        if (RCCCamera) {

            if (RCCCamera.GetComponent<RCC_CameraCarSelection>())
                RCCCamera.GetComponent<RCC_CameraCarSelection>().enabled = true;

            RCCCamera.ChangeCamera(RCC_Camera.CameraMode.TPS);

        }

        // Disable UI elements while in selection mode.
        if (RCCCanvas)
            RCCCanvas.SetActive(false);

    }

    /// <summary>
    /// Loads the next scene specified in the inspector.
    /// </summary>
    public void OpenScene() {

        // Load the specified scene.
        SceneManager.LoadScene(nextScene);

    }

}
