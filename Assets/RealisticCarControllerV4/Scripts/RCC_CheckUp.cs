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
/// Performs various checks on the vehicle configuration, including colliders, rigidbodies, 
/// wheel colliders, and engine parameters to ensure proper setup.
/// </summary>
public class RCC_CheckUp {

    /// <summary>
    /// Retrieves all MeshColliders that have their isTrigger property enabled.
    /// </summary>
    /// <param name="vehicle">The target vehicle GameObject.</param>
    /// <returns>An array of MeshColliders set as triggers.</returns>
    public static MeshCollider[] GetTriggerMeshColliders(GameObject vehicle) {

        MeshCollider[] meshcolliders = vehicle.GetComponentsInChildren<MeshCollider>(true);
        List<MeshCollider> triggers = new List<MeshCollider>();

        for (int i = 0; i < meshcolliders.Length; i++) {

            if (meshcolliders[i].isTrigger)
                triggers.Add(meshcolliders[i]);

        }

        return triggers.ToArray();

    }

    /// <summary>
    /// Retrieves all colliders within the vehicle.
    /// </summary>
    /// <param name="vehicle">The target vehicle GameObject.</param>
    /// <returns>An array of all colliders.</returns>
    public static Collider[] GetColliders(GameObject vehicle) {

        return vehicle.GetComponentsInChildren<Collider>(true);

    }

    /// <summary>
    /// Determines whether the vehicle has at least one enabled non-WheelCollider.
    /// </summary>
    /// <param name="vehicle">The target vehicle GameObject.</param>
    /// <returns>True if the vehicle has a valid collider, otherwise false.</returns>
    public static bool HaveCollider(GameObject vehicle) {

        Collider[] colliders = vehicle.GetComponentsInChildren<Collider>(true);

        foreach (var collider in colliders) {

            if (collider.enabled && collider.GetType() != typeof(WheelCollider))
                return true;

        }

        return false;
    }

    /// <summary>
    /// Retrieves all Rigidbody components in the vehicle, excluding the main Rigidbody and specific components.
    /// </summary>
    /// <param name="vehicle">The target vehicle GameObject.</param>
    /// <returns>An array of additional Rigidbody components.</returns>
    public static Rigidbody[] GetRigids(GameObject vehicle) {

        Rigidbody[] rigidbodies = vehicle.GetComponentsInChildren<Rigidbody>(true);
        List<Rigidbody> rigids = new List<Rigidbody>();

        foreach (var rb in rigidbodies) {

            if (rb != vehicle.GetComponent<Rigidbody>() &&
                rb.GetComponent<RCC_HoodCamera>() == null &&
                rb.GetComponent<RCC_WheelCamera>() == null &&
                rb != vehicle.GetComponent<RCC_DetachablePart>())
                rigids.Add(rb);

        }

        return rigids.ToArray();

    }

    /// <summary>
    /// Retrieves all WheelColliders in the vehicle that may have incorrect parameters.
    /// </summary>
    /// <param name="vehicle">The target vehicle GameObject.</param>
    /// <returns>An array of WheelColliders with potential configuration issues.</returns>
    public static WheelCollider[] GetWheelColliders(GameObject vehicle) {

        WheelCollider[] wheelColliders = vehicle.GetComponentsInChildren<WheelCollider>(true);
        List<WheelCollider> wheels = new List<WheelCollider>();

        foreach (var wheel in wheelColliders) {

            if (wheel.radius == 0 || wheel.suspensionDistance <= 0.01f)
                wheels.Add(wheel);

        }

        return wheels.ToArray();
    }

    /// <summary>
    /// Retrieves all enabled SphereColliders in the vehicle.
    /// </summary>
    /// <param name="vehicle">The target vehicle GameObject.</param>
    /// <returns>An array of enabled SphereColliders.</returns>
    public static SphereCollider[] GetSphereColliders(GameObject vehicle) {

        SphereCollider[] sphereColliders = vehicle.GetComponentsInChildren<SphereCollider>(true);
        List<SphereCollider> spheres = new List<SphereCollider>();

        foreach (var sphere in sphereColliders) {

            if (sphere.enabled)
                spheres.Add(sphere);

        }

        return spheres.ToArray();
    }

    /// <summary>
    /// Checks for incorrect engine and gear configurations.
    /// </summary>
    /// <param name="vehicle">The target vehicle's RCC_CarControllerV4 component.</param>
    /// <returns>An array of error messages related to incorrect configurations.</returns>
    public static string[] IncorrectConfiguration(RCC_CarControllerV4 vehicle) {

        float minEngineRPM = vehicle.minEngineRPM;
        float maxEngineRPM = vehicle.maxEngineRPM;
        float gearDownRPM = vehicle.gearShiftDownRPM;
        float gearUpRPM = vehicle.gearShiftUpRPM;

        List<string> errorMessages = new List<string>();

        if (minEngineRPM >= maxEngineRPM)
            errorMessages.Add("Min engine RPM must be lower than max engine RPM!");

        if (gearDownRPM >= gearUpRPM)
            errorMessages.Add("Gear shift down RPM must be lower than gear shift up RPM!");

        if (gearDownRPM <= minEngineRPM)
            errorMessages.Add("Gear shift down RPM must be higher than min engine RPM!");

        if (gearUpRPM >= maxEngineRPM)
            errorMessages.Add("Gear shift up RPM must be lower than max engine RPM!");

        if ((Mathf.Abs(maxEngineRPM) - Mathf.Abs(minEngineRPM)) < 3000f)
            errorMessages.Add("Max and min engine RPMs are too close to each other!");

        if ((Mathf.Abs(gearUpRPM) - Mathf.Abs(gearDownRPM)) < 1000f)
            errorMessages.Add("Gear shift up and down RPMs are too close to each other!");

        return errorMessages.ToArray();

    }

    /// <summary>
    /// Checks if any mesh in the vehicle has an incorrect axis alignment.
    /// </summary>
    /// <param name="vehicle">The target vehicle GameObject.</param>
    /// <param name="meshes">An array of MeshFilter components to check.</param>
    /// <returns>True if at least one mesh has an incorrect axis, otherwise false.</returns>
    public static bool HaveWrongAxis(GameObject vehicle, MeshFilter[] meshes) {

        foreach (var mesh in meshes) {

            if (mesh && 1 - Mathf.Abs(Quaternion.Dot(mesh.transform.rotation, vehicle.transform.rotation)) > 0.01f)
                return true;

        }

        return false;

    }

    /// <summary>
    /// Checks if the vehicle has a valid configuration for power, steering, braking, and handbrake.
    /// </summary>
    /// <param name="vehicle">The target RCC_CarControllerV4 vehicle.</param>
    /// <returns>A boolean array indicating if each configuration type is found: power, steer, brake, handbrake.</returns>
    public static bool[] HaveWrongOverride(RCC_CarControllerV4 vehicle) {

        RCC_WheelCollider[] allWheels = vehicle.GetComponentsInChildren<RCC_WheelCollider>(true);

        bool powerFound = false;
        bool steerFound = false;
        bool brakeFound = false;
        bool ebrakeFound = false;

        foreach (var wheel in allWheels) {

            if (wheel.canPower)
                powerFound = true;
            if (wheel.canSteer)
                steerFound = true;
            if (wheel.canBrake)
                brakeFound = true;
            if (wheel.canHandbrake)
                ebrakeFound = true;

        }

        return new bool[] { powerFound, steerFound, brakeFound, ebrakeFound };

    }

}
