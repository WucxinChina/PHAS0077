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
/// Represents a detachable part of the vehicle, such as a hood, trunk, door, or bumper. 
/// This part can break off under sufficient damage, and includes functionality for locking, unlocking, and restoring.
/// </summary>
public class RCC_DetachablePart : RCC_Core {

    /// <summary>
    /// The ConfigurableJoint used to attach this part to the main vehicle. 
    /// If null, it will be searched in the parent hierarchy.
    /// </summary>
    public ConfigurableJoint Joint {

        get {

            if (_joint == null)
                _joint = GetComponentInParent<ConfigurableJoint>(true);

            return _joint;

        }

        set {

            _joint = value;

        }

    }
    private ConfigurableJoint _joint;

    /// <summary>
    /// The Rigidbody associated with this part. 
    /// If null, it will be searched in the parent hierarchy.
    /// </summary>
    public Rigidbody Rigid {

        get {

            if (_rigid == null)
                _rigid = GetComponentInParent<Rigidbody>(true);

            return _rigid;
        }

        set {

            _rigid = value;

        }

    }
    private Rigidbody _rigid;

    /// <summary>
    /// Stores the original properties of the ConfigurableJoint so they can be restored after repair.
    /// </summary>
    private RCC_Joint jointProperties;

    /// <summary>
    /// An optional Transform to set the center of mass of this part.
    /// </summary>
    public Transform COM;

    /// <summary>
    /// The primary Collider for this part. Used for collision detection when detached or broken.
    /// </summary>
    public Collider partCollider;

    /// <summary>
    /// The type of detachable part (hood, trunk, door, etc.).
    /// </summary>
    public enum DetachablePartType { Hood, Trunk, Door, Bumper_F, Bumper_R }
    public DetachablePartType partType = DetachablePartType.Hood;

    /// <summary>
    /// If true, all ConfigurableJoint motions (X, Y, Z, and angular) will be locked at startup.
    /// </summary>
    public bool lockAtStart = true;

    /// <summary>
    /// The current strength of this part. When reduced by collisions, it can lead to loosening or detachment.
    /// </summary>
    public float strength = 100f;

    /// <summary>
    /// The original strength of this part. Used when restoring it during repairs.
    /// </summary>
    internal float orgStrength = 100f;

    /// <summary>
    /// Determines if this part can break off at a certain damage threshold.
    /// </summary>
    public bool isBreakable = true;

    /// <summary>
    /// Indicates whether the part has already broken off. 
    /// If true, further collision calculations will be ignored.
    /// </summary>
    public bool broken = false;

    /// <summary>
    /// The strength threshold for the part to become loose (but not fully detached). 
    /// Below this, it may wobble or move on hinges.
    /// </summary>
    public int loosePoint = 65;

    /// <summary>
    /// The strength threshold for the part to detach completely from the vehicle.
    /// </summary>
    public int detachPoint = 0;

    /// <summary>
    /// Once detached, this part will be deactivated after the specified number of seconds.
    /// </summary>
    public float deactiveAfterSeconds = 5f;

    /// <summary>
    /// Applied torque when the part's strength goes below the loose point. 
    /// This can simulate flapping or movement due to vehicle speed.
    /// </summary>
    public Vector3 addTorqueAfterLoose = Vector3.zero;

    private void Start() {

        // Save original strength for restoration.
        orgStrength = strength;

        // Attempt to reference the primary collider if not assigned in the Inspector.
        if (!partCollider)
            partCollider = GetComponentInChildren<Collider>();

        // Disable the partCollider by default.
        if (partCollider)
            partCollider.enabled = false;

        // Set the center of mass if a custom COM is assigned.
        if (COM)
            Rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);

        // If no ConfigurableJoint is found, disable this script.
        if (!Joint) {

            Debug.LogWarning("Configurable Joint not found for " + gameObject.name + "!");
            enabled = false;
            return;

        }

        Joint.connectedMassScale = 0f;

        // Cache original joint properties for restoring later.
        GetJointProperties();

        // If lockAtStart is true, lock all joint motions on Start.
        if (lockAtStart)
            StartCoroutine(LockJoint());

    }

    /// <summary>
    /// Caches the current ConfigurableJoint's settings in the jointProperties variable for later restoration.
    /// </summary>
    private void GetJointProperties() {

        jointProperties = new RCC_Joint();
        jointProperties.GetProperties(Joint);

    }

    /// <summary>
    /// Coroutine that locks all motions of the ConfigurableJoint after FixedUpdate, 
    /// preventing any movement at startup.
    /// </summary>
    private IEnumerator LockJoint() {

        yield return new WaitForFixedUpdate();
        RCC_Joint.LockPart(Joint);

    }

    private void Update() {

        // If the part has already broken off, skip further processing.
        if (broken)
            return;

        // If the part is loosened below the loose point, apply extra torque based on vehicle speed.
        if (addTorqueAfterLoose != Vector3.zero && strength <= loosePoint) {

            float speed = transform.InverseTransformDirection(Rigid.linearVelocity).z;
            Rigid.AddRelativeTorque(new Vector3(
                addTorqueAfterLoose.x * speed,
                addTorqueAfterLoose.y * speed,
                addTorqueAfterLoose.z * speed
            ));

        }

    }

    /// <summary>
    /// Called from the vehicle's damage system, passing the collision impulse. 
    /// Reduces the part's strength and checks if it needs to detach.
    /// </summary>
    /// <param name="impulse">The magnitude of the collision impulse.</param>
    public void OnCollision(float impulse) {

        // If the part is already broken, ignore collisions.
        if (broken)
            return;

        // Reduce strength by an amount proportional to the collision impulse.
        strength -= impulse * 10f;
        strength = Mathf.Clamp(strength, 0f, Mathf.Infinity);

        // Evaluate if the part should loosen or detach.
        CheckJoint();

    }

    /// <summary>
    /// Checks if the part should remain attached, loosen, or detach based on the current strength. 
    /// Also enables the part's collider upon detachment.
    /// </summary>
    private void CheckJoint() {

        // If the part is already broken, do nothing.
        if (broken)
            return;

        // If strength <= detachPoint, break off entirely (if breakable).
        if (isBreakable && strength <= detachPoint) {

            if (Joint) {

                if (partCollider)
                    partCollider.enabled = true;

                broken = true;
                Destroy(Joint);
                transform.SetParent(null);
                StartCoroutine(DisablePart(deactiveAfterSeconds));

            }

            // If strength <= loosePoint, unlock the joint motions so the part can move freely.
        } else if (strength <= loosePoint) {

            if (partCollider)
                partCollider.enabled = false;

            if (Joint) {

                Joint.angularXMotion = jointProperties.jointMotionAngularX;
                Joint.angularYMotion = jointProperties.jointMotionAngularY;
                Joint.angularZMotion = jointProperties.jointMotionAngularZ;

                Joint.xMotion = jointProperties.jointMotionX;
                Joint.yMotion = jointProperties.jointMotionY;
                Joint.zMotion = jointProperties.jointMotionZ;

            }

        }

    }

    /// <summary>
    /// Restores the part to its initial state. Re-creates the joint if necessary 
    /// and resets the part's strength to its original value.
    /// </summary>
    public void OnRepair() {

        // If the GameObject is inactive, reactivate it.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Disable the collider again because the part is re-attached.
        if (partCollider)
            partCollider.enabled = false;

        // If the joint was destroyed, create a new one and set it up with original properties.
        if (Joint == null) {

            Joint = gameObject.AddComponent<ConfigurableJoint>();
            jointProperties.SetProperties(Joint);

        } else {

            // Restore the part's strength and mark it as not broken.
            strength = orgStrength;
            broken = false;

            // Lock all joint motions to hold the part in place.
            Joint.angularXMotion = ConfigurableJointMotion.Locked;
            Joint.angularYMotion = ConfigurableJointMotion.Locked;
            Joint.angularZMotion = ConfigurableJointMotion.Locked;

            Joint.xMotion = ConfigurableJointMotion.Locked;
            Joint.yMotion = ConfigurableJointMotion.Locked;
            Joint.zMotion = ConfigurableJointMotion.Locked;

        }

    }

    /// <summary>
    /// Disables the part after the specified delay, simulating complete detachment once it has fallen away.
    /// </summary>
    /// <param name="delay">The time in seconds to wait before disabling.</param>
    /// <returns>An IEnumerator for the coroutine.</returns>
    private IEnumerator DisablePart(float delay) {

        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);

    }

    /// <summary>
    /// Automatically sets up default settings for the detachable part 
    /// if needed: layer, COM, ConfigurableJoint, Rigidbody, and Collider.
    /// </summary>
    private void Reset() {

        if (!string.IsNullOrEmpty(Settings.DetachablePartLayer))
            gameObject.layer = LayerMask.NameToLayer(Settings.DetachablePartLayer);

        if (!COM) {

            COM = new GameObject("COM").transform;
            COM.SetParent(transform);
            COM.localPosition = Vector3.zero;
            COM.localRotation = Quaternion.identity;

        }

        if (!Joint)
            Joint = gameObject.AddComponent<ConfigurableJoint>();

        Joint.connectedBody = GetComponentInParent<RCC_CarControllerV4>(true).gameObject.GetComponent<Rigidbody>();

        if (!Rigid)
            Rigid = gameObject.AddComponent<Rigidbody>();

        Rigid.mass = 35f;
        Rigid.interpolation = RigidbodyInterpolation.Interpolate;
        Rigid.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (!partCollider)
            partCollider = GetComponentInChildren<Collider>();

    }

}
