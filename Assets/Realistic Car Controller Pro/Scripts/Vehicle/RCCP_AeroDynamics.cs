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
#if UNITY_EDITOR        // keep compilation clean in builds
using UnityEditor;
#endif

/// <summary>
/// Manages the dynamics of the vehicle.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Dynamics")]
public class RCCP_AeroDynamics : RCCP_Component {

    /// <summary>
    /// Center of Mass for the vehicle. If not found, automatically creates a "COM" child object.
    /// </summary>
    public Transform COM {

        get {

            if (com == null) {

                if (transform.Find("COM")) {

                    com = transform.Find("COM");
                    return com;

                }

                GameObject newCom = new GameObject("COM");
                newCom.transform.SetParent(transform, false);
                com = newCom.transform;
                RecalculateCOM();

                return com;

            }

            return com;

        }
        set {

            com = value;

        }

    }

    private Transform com;

    [Min(0f)] public float downForce = 10f;

    /// <summary>
    /// Air resistance applied to the vehicle based on speed. Higher values cause more aerodynamic drag.
    /// </summary>
    [Range(0f, 100f)] public float airResistance = 10f;

    /// <summary>
    /// Deceleration applied to the vehicle based on speed. Higher values cause the vehicle to slow down more quickly.
    /// </summary>
    [Range(0f, 100f)] public float wheelResistance = 10f;

    /// <summary>
    /// Ignores the rigidbody drag force while accelerating. Used to achieve the maximum speed easily.
    /// </summary>
    public bool ignoreRigidbodyDragOnAccelerate = false;

    /// <summary>
    /// If true, the COM (Center of Mass) will be dynamically updated each physics frame.
    /// </summary>
    public bool dynamicCOM = false;

    /// <summary>
    /// If enabled, the vehicle will automatically reset if it flips upside down.
    /// </summary>
    public bool autoReset = true;

    /// <summary>
    /// Time (in seconds) to wait before resetting the vehicle if it's flipped.
    /// </summary>
    [Min(0f)] public float autoResetTime = 3f;
    private float autoResetTimer = 0f;

    private float defaultDrag = -1f;

    public override void Start() {

        base.Start();

        // Assigning center of mass position once at the start.
        CarController.Rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

    }

    private void FixedUpdate() {

        // Dynamically updates COM if enabled.
        if (dynamicCOM)
            CarController.Rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

        if (defaultDrag < 0)
            defaultDrag = CarController.Rigid.linearDamping;

        if (ignoreRigidbodyDragOnAccelerate)
            CarController.Rigid.linearDamping = defaultDrag * (1f - CarController.throttleInput_V);

        // Local forward speed (z-axis).
        float linearSpeed = transform.InverseTransformDirection(CarController.Rigid.linearVelocity).z;
        float speedMagnitude = Mathf.Abs(linearSpeed);

        if (CarController.IsGrounded) {

            // --------------------------------------------------------------------
            // 1. Downforce (velocity-squared version for more "realistic" feel)
            // --------------------------------------------------------------------
            // If you want to keep it linear, use:
            //   float downforceValue = downForce * speedMagnitude;
            float downforceValue = downForce * (speedMagnitude * speedMagnitude);
            downforceValue *= .15f;

            // Apply downforce in local downward direction.
            RCCP_WheelCollider[] wheelColliders = CarController.AllWheelColliders;

            if (wheelColliders != null && wheelColliders.Length > 0) {

                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null)
                        CarController.Rigid.AddForceAtPosition(-transform.up * (downforceValue / (float)wheelColliders.Length), wheelColliders[i].transform.position, ForceMode.Force);

                }

            }

        }

        // --------------------------------------------------------------------
        // 2. Aerodynamic drag (handles negative speeds correctly)
        // --------------------------------------------------------------------
        // Example: drag factor scaled 0..100 => 0..1
        // If speed > 100, this caps at 1.0. Increase "100f" if you have higher top speeds.
        //float dragFactor01 = Mathf.InverseLerp(0f, 100f, speedMagnitude);

        // If you prefer squared drag:
        //   float dragForce = airResistance * speedMagnitude * speedMagnitude * Mathf.Sign(linearSpeed);
        // If you prefer linear:
        float dragForce = airResistance * speedMagnitude;

        // Scale it up
        dragForce *= Mathf.Lerp(.5f, .035f, speedMagnitude / 80f);

        // Apply drag opposite local forward axis. 
        // (If your CarController.direction is needed, keep it. Otherwise, you might drop it 
        // so that the sign from linearSpeed alone is what matters.)
        CarController.Rigid.AddForceAtPosition(-CarController.Rigid.linearVelocity.normalized * dragForce, COM.position, ForceMode.Force);

        // --------------------------------------------------------------------
        // 3. Rolling resistance (uses InverseLerp for 0..100 speed range)
        // --------------------------------------------------------------------
        // If your absoluteSpeed is guaranteed within [0..1], you can keep Mathf.Lerp. 
        // Otherwise, InverseLerp 0..100 to get a 0..1 factor. (Adjust '100f' as needed.)
        float rollingFactor01 = Mathf.InverseLerp(0f, 100f, CarController.absoluteSpeed);
        float correctedWheelForce = wheelResistance * 1f;

        // Scale it up
        correctedWheelForce *= 50f * (1f - CarController.throttleInput_V);

        // Rolling resistance always opposes motion. We'll use linearSpeed sign:
        float rollingSign = Mathf.Sign(linearSpeed);

        // Apply rolling resistance only if grounded.
        if (CarController.IsGrounded) {
            CarController.Rigid.AddRelativeForce(
                -Vector3.forward * correctedWheelForce * rollingSign,
                ForceMode.Force
            );
        }

        // --------------------------------------------------------------------
        // 4. Auto-reset if upside down
        // --------------------------------------------------------------------
        if (autoReset)
            CheckUpsideDown();

    }

    /// <summary>
    /// Checks if the vehicle is upside down and resets it after 'autoResetTime' if speed is low.
    /// </summary>
    private void CheckUpsideDown() {

        // If vehicle speed is under 5, not kinematic, and z rotation is between 60 and 300, reset after the timer.
        if (Mathf.Abs(CarController.absoluteSpeed) < 8f && !CarController.Rigid.isKinematic) {

            if (CarController.transform.eulerAngles.z < 300f && CarController.transform.eulerAngles.z > 60f) {

                autoResetTimer += Time.deltaTime;

                if (autoResetTimer > autoResetTime) {

                    CarController.transform.SetPositionAndRotation(

                        new Vector3(CarController.transform.position.x, CarController.transform.position.y + 3f, CarController.transform.position.z),
                        Quaternion.Euler(0f, CarController.transform.eulerAngles.y, 0f)

                    );

                    autoResetTimer = 0f;

                }

            }

        }

    }

    /// <summary>
    /// Resets the timer used for flipping the vehicle.
    /// </summary>
    public void Reload() {

        autoResetTimer = 0f;

    }

    /// <summary>
    /// Re-calculates the COM position using renderer bounds plus user-defined
    /// racing biases. Call this after changing the vehicle�s meshes.
    /// </summary>
    public void RecalculateCOM() {

        Transform t = COM;

        // --------------------------------------------------------------------
        // 1) Build combined world-space bounds of every visible renderer
        //    (ignores trails / particles so exhaust smoke doesn�t skew the box)
        // --------------------------------------------------------------------
        Renderer[] renderers = GetComponentInParent<RCCP_CarController>(true).GetComponentsInChildren<Renderer>(false);

        Bounds worldBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer r in renderers) {

            if (r is TrailRenderer || r is ParticleSystemRenderer)
                continue;

            if (!hasBounds) {

                worldBounds = new Bounds(r.bounds.center, r.bounds.size);
                hasBounds = true;

            } else {

                worldBounds.Encapsulate(r.bounds);

            }

        }

        // Fallback if no renderers were found
        if (!hasBounds) {

            t.localPosition = new Vector3(0f, -.25f, 0f);
            return;

        }

        // --------------------------------------------------------------------
        // 2) Convert to local space so the COM lives neatly under the root
        // --------------------------------------------------------------------
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        Vector3 ext = worldBounds.extents;             // half-sizes (world)

        // Because we stay in root space (no scaling on car root in RCCP),
        // using world extents for biasing is sufficiently accurate.

        // --------------------------------------------------------------------
        // 3) Apply race-car biases
        // --------------------------------------------------------------------
        // Vertical: bottom + (height * bias)
        float bottomLocalY = localCenter.y - ext.y;
        localCenter.y = bottomLocalY + (worldBounds.size.y * .3f);

        // Longitudinal (Z): centre � fore/aft bias
        localCenter.z = 0f;

        // Lateral (X): centre � left/right bias
        localCenter.x = 0f;

        // --------------------------------------------------------------------
        // 4) Commit
        // --------------------------------------------------------------------
        t.localPosition = localCenter;

    }


    private void Reset() {

        // Runs when the component is first added from the Inspector.
        RecalculateCOM();

    }

}
