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
/// Tilts specified gameobjects based on the car's velocity and/or angular velocity,
/// but places all tilt targets as children under a single tiltRoot, so they rotate together.
/// Colliders are now placed outside of the tilt root.
/// </summary>
[DefaultExecutionOrder(10)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Body Tilt")]
public class RCCP_BodyTilt : RCCP_Component {

    [Tooltip("The transforms (e.g., body meshes) you want to tilt. They should not have colliders.")]
    public Transform[] tiltTargets;

    [Header("Tilt Settings")]
    [Tooltip("Maximum angle (in degrees) the body can tilt in either direction.")]
    public float maxTiltAngle = 7f;

    [Tooltip("Controls how sensitive the car tilts based on forward/backward velocity.")]
    public float forwardTiltMultiplier = 5f;

    [Tooltip("Controls how sensitive the car tilts based on sideways velocity.")]
    public float sidewaysTiltMultiplier = 5f;

    [Header("Smoothing")]
    [Tooltip("Larger values = quicker response. 0 = no smoothing (instant).")]
    public float tiltSmoothSpeed = 5f;

    // We'll store the tilt angles here
    private Vector3 _currentTiltEuler;

    // Root that we will tilt as a single transform
    public Transform _tiltRoot {

        get {

            if (_tiltRootInstance != null)
                return _tiltRootInstance;

            //  Create a new root for tilting.
            _tiltRootInstance = new GameObject("TiltRoot").transform;
            _tiltRootInstance.SetParent(transform, false);
            _tiltRootInstance.localPosition = Vector3.zero;
            _tiltRootInstance.localRotation = Quaternion.identity;
            _tiltRootInstance.localScale = Vector3.one;

            return _tiltRootInstance;

        }

    }
    private Transform _tiltRootInstance;

    public override void Start() {

        base.Start();

        // 2) Re-parent all tilt targets under this new root.
        foreach (Transform t in tiltTargets) {
            if (t == null) continue;
            t.SetParent(_tiltRoot, true);
        }

        // 3) Transfer any colliders to containers so they don't rotate with the tilt.
        TransferCollidersFromTiltTargets();

        // 4) Setting layers of the new children.
        SetLayers();

    }

    private void Update() {

        // 1. Convert car's angular velocity to local space
        Vector3 localAngularVelocity = transform.InverseTransformDirection(CarController.Rigid.angularVelocity);

        // Dot for forward/reverse check
        float forwardDot = Vector3.Dot(CarController.Rigid.linearVelocity, CarController.transform.forward);
        float directionSign = Mathf.Sign(forwardDot);
        // directionSign is +1 if forwardDot >= 0, -1 if forwardDot < 0

        // 2. Calculate tilt based on linear velocity
        float pitchAngle = 0f; // X-axis tilt
        float rollAngle = 0f; // Z-axis tilt

        // 3. Incorporate angular velocity if desired
        float rollFromAngular = localAngularVelocity.y * sidewaysTiltMultiplier * .75f; // typical turning (yaw)
        float pitchFromAngular = localAngularVelocity.x * forwardTiltMultiplier * 3.5f; // typical turning (pitch)

        rollFromAngular *= directionSign;

        rollAngle += rollFromAngular;
        pitchAngle += pitchFromAngular;

        // 4. Clamp angles
        pitchAngle = Mathf.Clamp(pitchAngle, -maxTiltAngle * .5f, maxTiltAngle * .5f);
        rollAngle = Mathf.Clamp(rollAngle, -maxTiltAngle, maxTiltAngle);

        // 5. Smooth or snap
        if (tiltSmoothSpeed > 0) {
            _currentTiltEuler.x = Mathf.Lerp(_currentTiltEuler.x, pitchAngle, Time.deltaTime * tiltSmoothSpeed);
            _currentTiltEuler.z = Mathf.Lerp(_currentTiltEuler.z, rollAngle, Time.deltaTime * tiltSmoothSpeed);
        } else {
            _currentTiltEuler.x = pitchAngle;
            _currentTiltEuler.z = rollAngle;
        }

        // 6. Apply rotation to the single tilt root
        if (_tiltRoot != null)
            _tiltRoot.localRotation = Quaternion.Euler(_currentTiltEuler.x, 0f, _currentTiltEuler.z);

    }

    /// <summary>
    /// For each tilt target, finds any colliders in it or its children,
    /// and transfers them to a dedicated container object (one per tilt target).
    /// </summary>
    private void TransferCollidersFromTiltTargets() {
        foreach (Transform tiltTarget in tiltTargets) {
            if (!tiltTarget) continue;

            // 1) Create a container for this tiltTarget's colliders.
            //    Instead of tilting with the root, we parent it *outside* the tilt root.
            GameObject container = new GameObject(tiltTarget.name + "_Colliders");

            // This places the container as a sibling of _tiltRoot, ensuring it won't rotate.
            // If _tiltRoot.parent is the Car's main transform, then container is also under that.
            container.transform.SetParent(_tiltRoot.parent, false);

            // 2) Recursively move colliders into this container
            TransferCollidersRecursively(tiltTarget, container.transform);
        }
    }

    /// <summary>
    /// Recursively finds any colliders under 'current' and moves them into 'colliderContainer'.
    /// Each collider gets its own child GameObject so we can preserve position, rotation, scale.
    /// </summary>
    private void TransferCollidersRecursively(Transform current, Transform colliderContainer) {
        // 1. Transfer colliders on the current transform
        var colliders = current.GetComponents<Collider>();
        foreach (var col in colliders) {
            TransferSingleCollider(col, colliderContainer);
        }

        // 2. Recurse for children
        for (int i = 0; i < current.childCount; i++) {
            TransferCollidersRecursively(current.GetChild(i), colliderContainer);
        }
    }

    /// <summary>
    /// Clones the Collider on a brand-new child of 'colliderContainer', preserving the world transform.
    /// </summary>
    private void TransferSingleCollider(Collider sourceCollider, Transform colliderContainer) {
        // Create a new child object under the container to hold this collider
        var newColliderGO = new GameObject(sourceCollider.gameObject.name + "_Collider");
        newColliderGO.transform.SetParent(colliderContainer, true);

        // Copy world transform
        newColliderGO.transform.position = sourceCollider.transform.position;
        newColliderGO.transform.rotation = sourceCollider.transform.rotation;
        newColliderGO.transform.localScale = sourceCollider.transform.lossyScale; // handle scale

        // Duplicate the collider component
        DuplicateCollider(sourceCollider, newColliderGO);

        // Remove the original
        Destroy(sourceCollider);
    }

    /// <summary>
    /// Copies all relevant fields from the source collider to the new collider on destinationObj.
    /// </summary>
    private void DuplicateCollider(Collider source, GameObject destinationObj) {
        if (source is BoxCollider boxSrc) {
            var boxDest = destinationObj.AddComponent<BoxCollider>();
            boxDest.center = boxSrc.center;
            boxDest.size = boxSrc.size;
            boxDest.isTrigger = boxSrc.isTrigger;
            boxDest.sharedMaterial = boxSrc.sharedMaterial;
        } else if (source is SphereCollider sphereSrc) {
            var sphereDest = destinationObj.AddComponent<SphereCollider>();
            sphereDest.center = sphereSrc.center;
            sphereDest.radius = sphereSrc.radius;
            sphereDest.isTrigger = sphereSrc.isTrigger;
            sphereDest.sharedMaterial = sphereSrc.sharedMaterial;
        } else if (source is CapsuleCollider capSrc) {
            var capDest = destinationObj.AddComponent<CapsuleCollider>();
            capDest.center = capSrc.center;
            capDest.radius = capSrc.radius;
            capDest.height = capSrc.height;
            capDest.direction = capSrc.direction;
            capDest.isTrigger = capSrc.isTrigger;
            capDest.sharedMaterial = capSrc.sharedMaterial;
        } else if (source is MeshCollider meshSrc) {
            var meshDest = destinationObj.AddComponent<MeshCollider>();
            meshDest.sharedMesh = meshSrc.sharedMesh;
            meshDest.convex = meshSrc.convex;
            meshDest.isTrigger = meshSrc.isTrigger;
            meshDest.sharedMaterial = meshSrc.sharedMaterial;
        } else {
            Debug.LogWarning($"[CarTilt] Unsupported collider type '{source.GetType().Name}' " +
                             $"on '{source.gameObject.name}'. It will be destroyed.");
        }
    }

    private void SetLayers() {

        gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

        var children = transform.GetComponentsInChildren<Transform>(true);

        if (RCCP_Settings.Instance.RCCPLayer != "") {

            foreach (var child in children)
                child.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

        }

        if (RCCP_Settings.Instance.RCCPWheelColliderLayer != "") {

            foreach (RCCP_WheelCollider item in GetComponentsInChildren<RCCP_WheelCollider>(true))
                item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

        }

        if (RCCP_Settings.Instance.RCCPDetachablePartLayer != "") {

            foreach (RCCP_DetachablePart item in GetComponentsInChildren<RCCP_DetachablePart>(true))
                item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

        }

    }

    public void Reload() {
        // Reset variables if needed
    }
}
