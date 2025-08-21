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
/// Demonstrates how to use the RCC API for spawning, registering, and controlling vehicles.
/// This example script allows spawning a vehicle with configurable settings, setting it as the player vehicle,
/// and toggling its controllability and engine state.
/// </summary>
public class RCC_APIExample : RCC_Core {

    /// <summary>
    /// Vehicle prefab to be spawned.
    /// </summary>
    public RCC_CarControllerV4 spawnVehiclePrefab;

    /// <summary>
    /// The currently spawned vehicle instance.
    /// </summary>
    private RCC_CarControllerV4 currentVehiclePrefab;

    /// <summary>
    /// Transform specifying the spawn position and rotation.
    /// </summary>
    public Transform spawnTransform;

    /// <summary>
    /// Whether the vehicle should be registered as the player vehicle upon spawning.
    /// </summary>
    public bool playerVehicle;

    /// <summary>
    /// Whether the spawned vehicle should be controllable.
    /// </summary>
    public bool controllable;

    /// <summary>
    /// Whether the vehicle's engine should start running upon spawning.
    /// </summary>
    public bool engineRunning;

    /// <summary>
    /// Spawns a new vehicle instance with the specified settings.
    /// </summary>
    public void Spawn() {

        // Spawns the vehicle at the given position and rotation with the specified settings.
        currentVehiclePrefab = RCC.SpawnRCC(spawnVehiclePrefab, spawnTransform.position, spawnTransform.rotation, playerVehicle, controllable, engineRunning);

    }

    /// <summary>
    /// Registers the currently spawned vehicle as the player vehicle.
    /// </summary>
    public void SetPlayer() {

        // Registers the spawned vehicle as the player's vehicle.
        RCC.RegisterPlayerVehicle(currentVehiclePrefab);

    }

    /// <summary>
    /// Sets the controllability state of the spawned vehicle.
    /// </summary>
    /// <param name="control">If true, enables control; otherwise, disables it.</param>
    public void SetControl(bool control) {

        // Enables or disables control for the spawned vehicle.
        RCC.SetControl(currentVehiclePrefab, control);

    }

    /// <summary>
    /// Toggles the engine state of the spawned vehicle.
    /// </summary>
    /// <param name="engine">If true, starts the engine; otherwise, stops it.</param>
    public void SetEngine(bool engine) {

        // Starts or stops the engine of the spawned vehicle.
        RCC.SetEngine(currentVehiclePrefab, engine);

    }

    /// <summary>
    /// Deregisters the current player vehicle, removing its player association.
    /// </summary>
    public void DeRegisterPlayer() {

        // Deregisters the vehicle as the player vehicle.
        RCC.DeRegisterPlayerVehicle();

    }

}
