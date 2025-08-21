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
/// Limits the maximum speed of the vehicle on a per-gear basis,
/// and applies a direct counter-force to slow the vehicle downhill if it exceeds the limit.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Limiter")]
public class RCCP_Limiter : RCCP_Component {

    /// <summary>
    /// An array specifying the speed limit for each gear.
    /// The index must match how your gearbox indexes gears (including reverse or neutral if needed).
    /// </summary>
    [Min(-1f)] public float[] limitSpeedAtGear = new float[0];

    /// <summary>
    /// Auto sets the target speeds by using target gear's maximum speed taken from RCCP_Gearbox.
    /// </summary>
    public bool autoSet = false;

    /// <summary>
    /// If true, the limiter is actively preventing further acceleration (engine cut).
    /// </summary>
    public bool limitingNow = false;

    /// <summary>
    /// If true, the limiter applies a counter-force directly to the rigidbody to maintain speed downhill.
    /// </summary>
    public bool applyDownhillForce = true;

    /// <summary>
    /// The strength of the force applied (in Newtons per kg, effectively an acceleration)
    /// if the vehicle is over the limit. 
    /// Tweak to handle heavier vehicles or steeper slopes.
    /// </summary>
    [Range(0f, 100f)] public float downhillForceStrength = 20f;

    // Use FixedUpdate for physics-based limiting
    private void FixedUpdate() {

        // Ensure the gearbox is valid
        if (!CarController.Gearbox)
            return;

        if (autoSet)
            limitSpeedAtGear = CarController.Gearbox.TargetSpeeds;

        // Ensure the array is valid
        if (limitSpeedAtGear.Length == 0)
            return;

        int currentGear = CarController.Gearbox.currentGear;

        // Make sure we don't go out of bounds for limitSpeedAtGear
        if (currentGear < 0 || currentGear >= limitSpeedAtGear.Length) {

            // If gear index is invalid, don't limit speed or cut fuel
            limitingNow = false;
            CarController.Engine.cutFuel = false;
            return;

        }

        float gearSpeedLimit = limitSpeedAtGear[currentGear];

        if (gearSpeedLimit <= 0)
            return;

        float currentSpeed = CarController.absoluteSpeed;  // km/h

        // If current speed is beyond the gear limit, cut fuel to stop accelerating
        // (only needed if you want the engine not to push past the limit).
        if (currentSpeed > gearSpeedLimit) {

            limitingNow = true;
            CarController.Engine.cutFuel = true;

            // Optionally apply extra force if speed is above limit (e.g., going downhill).
            if (applyDownhillForce)
                ApplyDownhillForce(gearSpeedLimit);

        } else {

            // If we're below or at the limit, no limiting needed
            limitingNow = false;
            CarController.Engine.cutFuel = false;

        }

    }

    /// <summary>
    /// Applies a braking force opposite to the vehicle's velocity if it's above the limit.
    /// </summary>
    /// <param name="limit">Speed limit in km/h.</param>
    private void ApplyDownhillForce(float limit) {

        // Convert speed from km/h to m/s
        // 1 km/h = 0.277778 m/s
        float limitMS = limit * 0.277778f;

        // Get the current velocity in world space
        Vector3 velocity = CarController.Rigid.linearVelocity;
        float currentSpeedMS = velocity.magnitude;

        // If we're above the limit in m/s, apply a force to reduce the velocity.
        if (currentSpeedMS > limitMS) {

            // Direction opposite of the velocity
            Vector3 brakeDirection = -velocity.normalized;

            // The idea is to apply an acceleration enough to bring the vehicle closer to the limit.
            // This is a simplistic approach: the stronger the downhillForceStrength, 
            // the more quickly you'll force the speed down.
            float force = downhillForceStrength * CarController.Rigid.mass;

            // Add the force in the opposite direction of travel
            CarController.Rigid.AddForce(brakeDirection * force, ForceMode.Force);

        }

    }

    /// <summary>
    /// Resets the limiting status, typically called when reloading or resetting the vehicle.
    /// </summary>
    public void Reload() {

        limitingNow = false;

    }

    private void Reset() {

        // If a gearbox cannot be found, disable this component as it cannot function without gear data.
        var gearbox = GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Gearbox>(true);

        if (!gearbox) {

            Debug.LogError("No RCCP_Gearbox found on this vehicle. RCCP_Limiter requires a gearbox to limit speed by gear.");
            enabled = false;
            return;

        }

        // Initialize the limitSpeedAtGear array based on the gearbox�s number of gears, defaulting each to 999 km/h.
        limitSpeedAtGear = new float[gearbox.gearRatios.Length];

        for (int i = 0; i < limitSpeedAtGear.Length; i++)
            limitSpeedAtGear[i] = -1;

    }

}
