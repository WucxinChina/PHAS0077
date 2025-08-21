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
/// NOS (Nitrous Oxide System) / Boost feature. Increases engine torque for a limited duration, then regenerates over time.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Nos")]
public class RCCP_Nos : RCCP_Component {

    /// <summary>
    /// True if NOS is currently being used (player is pressing NOS input and conditions are met).
    /// </summary>
    [HideInInspector] public bool nosInUse = false;

    /// <summary>
    /// Engine torque will be multiplied by this factor while NOS is active.
    /// </summary>
    [Min(0f)] public float torqueMultiplier = 2.5f;

    /// <summary>
    /// Current NOS amount [0..1], reflecting how much total duration is left.
    /// </summary>
    [Range(0f, 1f)] public float amount = 1f;

    /// <summary>
    /// Maximum duration (in seconds) that NOS can be used before empty.
    /// </summary>
    [Min(0f)] public float durationTime = 3f;

    /// <summary>
    /// Current countdown timer for the NOS. Clamped between [0..durationTime].
    /// </summary>
    [Min(0f)] public float timer = 3f;

    /// <summary>
    /// Time (in seconds) after NOS is used up that begins regeneration. 
    /// If the player stops using NOS, once this time passes, the NOS tank starts refilling.
    /// </summary>
    [Min(0f)] public float regenerateTime = 2f;

    /// <summary>
    /// Internal timer tracking how long since NOS was last used, for deciding when regeneration starts.
    /// </summary>
    [Min(0f)] private float regenerateTimer = 1f;

    /// <summary>
    /// Regeneration rate for NOS. Higher values refill the NOS tank more quickly once regeneration begins.
    /// </summary>
    [Min(0f)] public float regenerateRate = 1f;

    private void Update() {

        // If there's no engine, we cannot apply NOS.
        if (!CarController.Engine)
            return;

        // True if the player's NOS input is at least 50% pressed.
        nosInUse = CarController.nosInput_P >= .5f;

        // NOS is disabled if the engine isn't running.
        if (!CarController.Engine.engineRunning)
            nosInUse = false;

        // NOS is disabled if the throttle input is below ~50%.
        if (CarController.throttleInput_V < .5f)
            nosInUse = false;

        // If timer is nearly empty, disable NOS to avoid going negative.
        if (timer <= 0f)
            nosInUse = false;

        // If NOS is active, decrease the timer and multiply engine torque.
        if (nosInUse) {

            // Reset the regeneration wait timer.
            regenerateTimer = 0f;

            // Decrease NOS timer.
            timer -= Time.deltaTime;
            timer = Mathf.Clamp(timer, 0f, Mathf.Infinity);

            // Apply torque multiplier for the current frame.
            CarController.Engine.Multiply(torqueMultiplier);

        }

        // Once we're not using NOS, if enough time has passed (>= regenerateTime), begin regeneration.
        if (regenerateTimer < regenerateTime)
            regenerateTimer += Time.deltaTime;

        if (regenerateTimer >= regenerateTime)
            timer += Time.deltaTime * regenerateRate;

        // Clamp timer to maximum capacity and update amount ratio.
        timer = Mathf.Clamp(timer, 0f, durationTime);
        amount = timer / durationTime;

    }

    /// <summary>
    /// Resets NOS usage, typically called when reloading vehicle states.
    /// </summary>
    public void Reload() {

        nosInUse = false;
        timer = durationTime; // Reset to full tank
        regenerateTimer = 0f;
        amount = 1f; // Reset amount to full

    }

}
