//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Manages a vehicle's fuel tank. Consumes fuel based on engine RPM and throttle,
/// optionally shutting off the engine when empty.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Fuel Tank")]
public class RCCP_FuelTank : RCCP_Component {

    /// <summary>
    /// Total capacity of the fuel tank, in liters.
    /// </summary>
    [Range(0f, 300f)]
    public float fuelTankCapacity = 60f;

    /// <summary>
    /// Ratio of current fuel level to maximum capacity (0 = empty, 1 = full).
    /// </summary>
    [Range(0f, 1f)]
    public float fuelTankFillAmount = 1f;

    /// <summary>
    /// If true, stops the engine automatically when the tank is empty.
    /// </summary>
    public bool stopEngine = true;

    /// <summary>
    /// Default (full) capacity, used for clamping or refilling to 100%.
    /// </summary>
    private float fuelTankCapacityDefault = 60f;

    // -------------------------------------------------------------------------
    //      More Accurate Consumption Settings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Base fuel consumption in liters/hour at idle speed (minimal throttle).
    /// </summary>
    [Range(0f, 20f)]
    public float baseLitersPerHour = 1f;

    /// <summary>
    /// Maximum fuel consumption in liters/hour at full throttle and near max RPM.
    /// </summary>
    [Range(0f, 200f)]
    public float maxLitersPerHour = 25f;

    /// <summary>
    /// Minimal throttle fraction used if the engine is running (to simulate idle consumption).
    /// Helps ensure some fuel is burned even at low or zero throttle.
    /// </summary>
    [Range(0f, 1f)]
    public float minimalIdleThrottle = 0.05f;

    /// <summary>
    /// Current instantaneous fuel consumption in liters/hour (useful for debugging/UI).
    /// </summary>
    [Range(0f, 200f)]
    public float currentLitersPerHour = 0f;

    public override void Start() {
        base.Start();
        // Cache the default capacity so we know the max tank level for fill ratio.
        fuelTankCapacityDefault = fuelTankCapacity;
    }

    private void Update() {

        // Update fill ratio: 0..1
        // This is basically tankCurrent / tankMax. 
        // The Lerp(0,1, x) pattern is the same as just x in this case.
        fuelTankFillAmount = Mathf.Clamp01(fuelTankCapacity / fuelTankCapacityDefault);

        // If there's no engine or it's not running, we won't consume fuel.
        if (!CarController.Engine || !CarController.Engine.engineRunning)
            return;

        // -----------------------------------------------------------
        // 1) Determine throttle factor. Even at 0 user-throttle, 
        //    we can use minimalIdleThrottle to represent idle load.
        // -----------------------------------------------------------
        float throttleInput = CarController.throttleInput_V;
        if (throttleInput < minimalIdleThrottle && CarController.Engine.engineRPM > 100f) {
            // If engine is running but user throttle is below idle, 
            // assume minimal consumption anyway.
            throttleInput = minimalIdleThrottle;
        }

        // -----------------------------------------------------------
        // 2) Compute an approximate fraction of max RPM.
        //    If you have CarController.Engine.maxRPM, you can use that. 
        //    For a simpler approach, define an assumed "maxRPM" or 
        //    clamp the engineRPM for the fraction.
        // -----------------------------------------------------------
        float assumedMaxRPM = CarController.Engine.maxEngineRPM > 1000f
            ? CarController.maxEngineRPM
            : 6000f; // fallback if maxRPM isn't set
        float rpmFraction = Mathf.Clamp01(CarController.Engine.engineRPM / assumedMaxRPM);

        // -----------------------------------------------------------
        // 3) Blend between base and max liters/hour based on throttle + rpm fraction.
        //    e.g. at idle => base consumption,
        //         at full throttle & high RPM => max consumption.
        // -----------------------------------------------------------
        currentLitersPerHour = Mathf.Lerp(
            baseLitersPerHour,
            maxLitersPerHour,
            rpmFraction * throttleInput
        );

        // -----------------------------------------------------------
        // 4) Convert liters/hour to liters/second for *this frame*.
        // -----------------------------------------------------------
        float litersPerSecond = currentLitersPerHour / 3600f;  // 3600 seconds in an hour
        float consumptionThisFrame = litersPerSecond * Time.deltaTime;

        // -----------------------------------------------------------
        // 5) Subtract from the tank. 
        // -----------------------------------------------------------
        fuelTankCapacity -= consumptionThisFrame;

        // If the tank is now empty, optionally stop the engine.
        if (fuelTankCapacity <= 0f) {
            fuelTankCapacity = 0f;
            if (stopEngine) {
                CarController.Engine.StopEngine();
            }
        }

    }

    /// <summary>
    /// Completely refills the fuel tank to its default capacity.
    /// </summary>
    public void Refill() {
        fuelTankCapacity = fuelTankCapacityDefault;
    }

    /// <summary>
    /// Refills the fuel tank by a specified amount (liters/second), 
    /// accumulated over time each frame. Clamped by the maximum capacity.
    /// </summary>
    /// <param name="amountOfFuel">Units of fuel (liters/second) to add per second.</param>
    public void Refill(float amountOfFuel) {
        // Increase the tank capacity by the given rate over time.
        fuelTankCapacity += amountOfFuel * Time.deltaTime;
        // Clamp to default capacity if overfilled.
        if (fuelTankCapacity > fuelTankCapacityDefault)
            fuelTankCapacity = fuelTankCapacityDefault;
    }

    /// <summary>
    /// Resets or reloads the fuel tank. Currently no extra logic needed.
    /// </summary>
    public void Reload() {
        // You could reset variables here if needed. 
    }

}
