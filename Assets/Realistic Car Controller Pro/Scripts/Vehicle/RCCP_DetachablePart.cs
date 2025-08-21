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
using UnityEngine.Events;
using UnityEngine.Animations;

/// <summary>
/// Detachable part of the vehicle. Uses a Configurable Joint to attach the part to the vehicle.
/// Once its strength falls below certain thresholds, it can become loose or fully detach.
/// Now includes additional "Potential Enhancements."
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Detachable Part")]
public class RCCP_DetachablePart : RCCP_Component {

    /// <summary>
    /// Defines what part of the car this is (e.g., hood, trunk, door, etc.).
    /// </summary>
    public enum DetachablePartType { Hood, Trunk, Door, Bumper_F, Bumper_R, Other }

    #region Internal References

    private ConfigurableJoint joint;
    private ConfigurableJoint Joint {
        get {
            if (joint == null)
                joint = GetComponent<ConfigurableJoint>();
            return joint;
        }
        set {
            joint = value;
        }
    }

    private Rigidbody rigid;
    private Rigidbody Rigid {
        get {
            if (rigid == null)
                rigid = GetComponent<Rigidbody>();
            return rigid;
        }
    }

    /// <summary>
    /// Holds original joint settings for restoring if the part is repaired.
    /// </summary>
    private RCCP_Joint jointProperties = new RCCP_Joint();

    #endregion

    #region Inspector Fields

    /// <summary>
    /// Optional center of mass reference. If assigned, the rigidbody's center of mass is moved to this point at Awake().
    /// </summary>
    public Transform COM;

    /// <summary>
    /// Colliders belonging to this part.
    /// </summary>
    [HideInInspector] public Collider[] partColliders;

    /// <summary>
    /// The part type for identifying its role on the vehicle.
    /// </summary>
    public DetachablePartType partType = DetachablePartType.Hood;

    /// <summary>
    /// If true, the Configurable Joint motions will be locked at Start.
    /// </summary>
    public bool lockAtStart = true;

    /// <summary>
    /// Overall strength of the part. Decreases with collisions until it detaches or becomes loose.
    /// </summary>
    [Min(0f)] public float strength = 100f;

    /// <summary>
    /// Original (maximum) strength of the part. Used for restoring strength on repairs.
    /// </summary>
    [Min(0f)] internal float orgStrength = 100f;

    /// <summary>
    /// If true, the part can fully detach once the strength goes below a certain threshold.
    /// </summary>
    public bool isDetachable = true;

    /// <summary>
    /// Strength threshold at which the part becomes "loose" but not fully detached.
    /// </summary>
    [Min(0)] public int loosePoint = 50;

    /// <summary>
    /// Strength threshold at which the part fully detaches from the vehicle.
    /// </summary>
    [Min(0)] public int detachPoint = 0;

    /// <summary>
    /// After full detachment, the part is disabled after this many seconds.
    /// </summary>
    [Min(0f)] public float deactiveAfterSeconds = 5f;

    /// <summary>
    /// Torque added to the part in local space once its strength falls below loosePoint.
    /// Useful for visualizing the part flapping in the wind.
    /// </summary>
    public Vector3 addTorqueAfterLoose = Vector3.zero;

    #endregion

    #region Enhanced Fields

    [Header("=== Enhancements ===")]

    [Tooltip("Additional damage multiplier by part type (example values). " +
             "You can also expand this logic in the OnCollision method.")]
    public bool useDamageWeighting = true;

    [Tooltip("Optional UnityEvent invoked when the part receives damage. " +
             "Passes the 'damageAmount' that was applied.")]
    public UnityEvent<float> onDamaged;

    #endregion

    #region Private Variables

    private bool broken = false;

    /// <summary>
    /// Example dictionary for part-type-based damage multipliers.
    /// You can customize these values or read them from a ScriptableObject.
    /// </summary>
    private static readonly Dictionary<DetachablePartType, float> damageMultipliers
        = new Dictionary<DetachablePartType, float>() {
            { DetachablePartType.Hood,      1.0f },
            { DetachablePartType.Trunk,     1.2f },
            { DetachablePartType.Door,      0.8f },
            { DetachablePartType.Bumper_F,  1.5f },
            { DetachablePartType.Bumper_R,  1.5f },
            { DetachablePartType.Other,     1.0f }
        };

    #endregion

    #region Unity Methods

    public override void Awake() {

        base.Awake();

        CreatePivot();

        // Remember original strength
        orgStrength = strength;

        // Grab all colliders
        partColliders = GetComponentsInChildren<Collider>(true);

#if UNITY_2022_2_OR_NEWER
        LayerMask curLayerMask = -1;
        foreach (Collider collider in partColliders) {
            curLayerMask = collider.excludeLayers;
            curLayerMask |= (1 << LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer));
            collider.excludeLayers = curLayerMask;
        }
#endif

        // Set custom COM if assigned
        if (COM)
            Rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);

        // Check for joint
        if (!Joint) {

            Debug.LogWarning("Configurable Joint not found for " + gameObject.name + "!");
            enabled = false;
            return;

        }

        // Capture original joint properties
        GetJointProperties();

        // Lock motions if requested
        if (lockAtStart)
            RCCP_Joint.LockPart(Joint);

        foreach (Transform item in GetComponentsInChildren<Transform>(true))
            item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

    }

    private void CreatePivot() {

        GameObject container = new GameObject("TempContainerForRepivot");

        foreach (Transform item in transform.GetComponentsInChildren<Transform>()) {

            if (!Equals(item, transform) && item.parent == transform)
                item.SetParent(container.transform, true);

        }

        Vector3 jointAnchorVector = transform.TransformPoint(Joint.anchor);
        transform.position = jointAnchorVector;

        foreach (Transform item in container.GetComponentsInChildren<Transform>()) {

            if (!Equals(item, container.transform) && item.parent == container.transform)
                item.SetParent(transform, true);

        }

        Destroy(container);

        Joint.anchor = Vector3.zero;
        Joint.autoConfigureConnectedAnchor = false;

        ParentConstraint parentConstraint = gameObject.AddComponent<ParentConstraint>();

        ConstraintSource constraintSource = new ConstraintSource();
        constraintSource.sourceTransform = CarController.OtherAddonsManager.BodyTilt._tiltRoot;
        constraintSource.weight = 1f;
        parentConstraint.AddSource(constraintSource);
        parentConstraint.SetTranslationOffset(0, transform.localPosition);
        parentConstraint.constraintActive = true;

        parentConstraint.translationAxis = Axis.X | Axis.Y | Axis.Z;
        parentConstraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;

    }

    private void Update() {

        if (broken)
            return;

        // Add torque if part is "loose"
        if (addTorqueAfterLoose != Vector3.zero && strength <= loosePoint) {

            float speed = transform.InverseTransformDirection(Rigid.linearVelocity).z;
            Rigid.AddRelativeTorque(addTorqueAfterLoose * speed);

        }

    }

    #endregion

    #region Damage Handling

    /// <summary>
    /// Called by external code (e.g., damage system) to reduce the part's strength based on collision impulse.
    /// Enhancements: 
    ///  - Use damage weighting based on partType
    ///  - Invoke UnityEvent for visual/audio feedback
    /// </summary>
    /// <param name="impulse">Collision impulse magnitude.</param>
    public void OnCollision(float impulse) {

        if (!enabled || broken)
            return;

        float dmgMultiplier = 1f;

        if (useDamageWeighting && damageMultipliers.ContainsKey(partType))
            dmgMultiplier = damageMultipliers[partType];

        // Example weighted damage
        float damageToApply = impulse * 5f * dmgMultiplier;
        strength -= damageToApply;
        strength = Mathf.Clamp(strength, 0f, Mathf.Infinity);

        CheckJoint();

    }

    /// <summary>
    /// Checks if part needs to become loose or detach based on its current strength.
    /// Optionally supports smooth transition from locked -> free motion.
    /// </summary>
    private void CheckJoint() {

        if (broken)
            return;

        // If fully detachable and strength <= detachPoint, break off completely
        if (isDetachable && strength <= detachPoint) {

            broken = true;
            RCCP_Joint.LoosePart(Joint);
            transform.SetParent(null);
            StartCoroutine(DisablePart(deactiveAfterSeconds));

            if (TryGetComponent(out ParentConstraint parentConstraint))
                parentConstraint.constraintActive = false;

        }

        // If strength <= loosePoint, attempt to unlock
        else if (strength <= loosePoint) {

            if (Joint) {

                // Instantly unlock motions
                Joint.angularXMotion = jointProperties.jointMotionAngularX;
                Joint.angularYMotion = jointProperties.jointMotionAngularY;
                Joint.angularZMotion = jointProperties.jointMotionAngularZ;

                Joint.xMotion = jointProperties.jointMotionX;
                Joint.yMotion = jointProperties.jointMotionY;
                Joint.zMotion = jointProperties.jointMotionZ;

            }

            if (TryGetComponent(out ParentConstraint parentConstraint)) {

                parentConstraint.translationAxis = Axis.Y;
                parentConstraint.rotationAxis = Axis.None;
                parentConstraint.constraintActive = true;

            }

        }

    }

    #endregion

    #region Repair Logic

    /// <summary>
    /// Repairs and reattaches this part, restoring its ConfigurableJoint and strength.
    /// </summary>
    public void OnRepair() {

        strength = orgStrength;
        broken = false;

        // If the ConfigurableJoint was removed or destroyed, re-add it
        if (Joint == null)
            Joint = gameObject.AddComponent<ConfigurableJoint>();

        // Restore original joint properties, then lock them as at startup.
        jointProperties.SetProperties(Joint);
        RCCP_Joint.LockPart(Joint);

        // Re-enable this part if it was disabled.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (TryGetComponent(out ParentConstraint parentConstraint)) {

            parentConstraint.translationAxis = Axis.X | Axis.Y | Axis.Z;
            parentConstraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;
            parentConstraint.constraintActive = true;

        }

        StartCoroutine(nameof(FixInterpolation));

    }

    /// <summary>
    /// Waits a set delay before disabling (or pooling) the part, triggered after detachment.
    /// </summary>
    /// <param name="delay">Seconds to wait.</param>
    private IEnumerator DisablePart(float delay) {

        yield return new WaitForSeconds(delay);

        if (broken) {

            // Simply disable if not using pooling
            gameObject.SetActive(false);

        }

    }

    #endregion

    #region Misc

    /// <summary>
    /// Helper for re-fixing interpolation after re-attaching or restoring the part.
    /// </summary>
    private IEnumerator FixInterpolation() {

        yield return new WaitForFixedUpdate();
        Rigid.interpolation = RigidbodyInterpolation.Interpolate;
        yield return new WaitForFixedUpdate();
        Rigid.interpolation = RigidbodyInterpolation.None;

    }

    /// <summary>
    /// Saves original joint properties so they can be restored when repairing the part.
    /// </summary>
    private void GetJointProperties() {

        jointProperties = new RCCP_Joint();
        jointProperties.GetProperties(Joint);

    }

    /// <summary>
    /// Called when adding this component to a GameObject. Creates a default COM, ConfigurableJoint, and sets up colliders.
    /// </summary>
    private void Reset() {

        if (!COM) {

            COM = new GameObject("COM").transform;
            COM.SetParent(transform, false);
            COM.localPosition = Vector3.zero;
            COM.localRotation = Quaternion.identity;

        }

        ConfigurableJoint cJoint = GetComponent<ConfigurableJoint>();

        if (!cJoint)
            cJoint = gameObject.AddComponent<ConfigurableJoint>();

        cJoint.connectedBody = GetComponentInParent<Rigidbody>(true);
        cJoint.massScale = 1f;
        cJoint.connectedMassScale = 0f;

        Rigidbody cJointRigid = cJoint.GetComponent<Rigidbody>();
        if (cJointRigid) {

            cJointRigid.mass = 10f;
            cJointRigid.linearDamping = 0f;
            cJointRigid.angularDamping = .05f;
            cJointRigid.interpolation = RigidbodyInterpolation.None;
            cJointRigid.collisionDetectionMode = CollisionDetectionMode.Discrete;

        }

        if (partColliders == null || (partColliders != null && partColliders.Length < 1))
            partColliders = GetComponentsInChildren<Collider>(true);

    }

    #endregion

}
