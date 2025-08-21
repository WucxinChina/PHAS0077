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
using System.Linq;

/// <summary>
/// Truck trailer has additional wheelcolliders, and now also a lighting system.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ConfigurableJoint))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Trailer Controller")]
public class RCCP_TrailerController : RCCP_GenericComponent {

    /// <summary>
    /// The car controller that will tow this trailer once attached.
    /// This reference is set when the trailer is attached to the towing vehicle.
    /// </summary>
    public RCCP_CarController carController;

    /// <summary>
    /// The Rigidbody component of this trailer.
    /// Used for applying physics and adjusting center of mass.
    /// </summary>
    private Rigidbody Rigid {
        get {
            if (_Rigid == null)
                _Rigid = GetComponent<Rigidbody>();
            return _Rigid;
        }
    }
    private Rigidbody _Rigid;

    /// <summary>
    /// The ConfigurableJoint used to connect the trailer to a towing vehicle.
    /// </summary>
    public ConfigurableJoint Joint {
        get {
            if (_Joint == null)
                _Joint = GetComponent<ConfigurableJoint>();
            return _Joint;
        }
    }
    private ConfigurableJoint _Joint;

    [System.Serializable]
    public class TrailerWheel {

        /// <summary>
        /// The WheelCollider associated with this wheel.
        /// </summary>
        public WheelCollider wheelCollider;

        /// <summary>
        /// The visual model (mesh) for this wheel.
        /// </summary>
        public Transform wheelModel;

        /// <summary>
        /// Determines if this wheel should steer when the trailer moves or turns.
        /// </summary>
        public bool steering = false;

        /// <summary>
        /// The maximum steer angle for this wheel if steering is enabled.
        /// </summary>
        public float maxSteerAngle = 40f;

        /// <summary>
        /// Caches the wheel's local position for reference when applying steer offsets.
        /// </summary>
        [HideInInspector] public Vector3 localPosition = Vector3.zero;

        /// <summary>
        /// Applies motor torque to this wheel if it has a valid WheelCollider.
        /// </summary>
        public void Torque(float torque) {
            if (wheelCollider)
                wheelCollider.motorTorque = torque;
        }

        /// <summary>
        /// Applies brake torque to this wheel if it has a valid WheelCollider.
        /// </summary>
        public void Brake(float brakeTorque) {
            if (wheelCollider)
                wheelCollider.brakeTorque = brakeTorque;
        }

    }

    /// <summary>
    /// An array of TrailerWheel objects representing each wheel on the trailer.
    /// </summary>
    public TrailerWheel[] trailerWheels;

    // ------------------------------------------
    //  CENTER OF MASS
    // ------------------------------------------

    /// <summary>
    /// Transform used to define the trailer's center of mass offset.
    /// </summary>
    public Transform COM;

    // ------------------------------------------
    //  TRAILER ATTACH / DETACH
    // ------------------------------------------

    /// <summary>
    /// Optional legs GameObject that can be enabled or disabled when the trailer is attached or detached.
    /// </summary>
    public GameObject legs;

    /// <summary>
    /// Internal flag to determine if the trailer is effectively sleeping,
    /// based on low velocity and angular velocity.
    /// </summary>
    private bool isSleeping = false;

    /// <summary>
    /// A timer used to prevent rapid attach/detach actions, clamped between 0 and 1.
    /// </summary>
    [Range(0f, 1f)] private float timer = 0f;

    /// <summary>
    /// Reference to the trailer attacher component, used to attach/detach the trailer from vehicles.
    /// </summary>
    public RCCP_TrailerAttacher attacher;

    /// <summary>
    /// Indicates if the trailer is attached to a towing vehicle (joint.connectedBody != null).
    /// </summary>
    public bool attached = false;

    /// <summary>
    /// If true, the camera distance and height are set manually with TPSDistance and TPSHeight.
    /// </summary>
    [Space()]
    public bool manualSetCameraDistanceAndHeight = false;

    /// <summary>
    /// Third-Person-Style camera distance when manually setting camera distance and height.
    /// </summary>
    public float TPSDistance = 20f;

    /// <summary>
    /// Third-Person-Style camera height when manually setting camera distance and height.
    /// </summary>
    public float TPSHeight = 6f;

    /// <summary>
    /// Internal class for storing and restoring the joint motions of the ConfigurableJoint.
    /// </summary>
    private class JointRestrictions {

        /// <summary>
        /// x-motion state of the ConfigurableJoint.
        /// </summary>
        public ConfigurableJointMotion motionX = ConfigurableJointMotion.Free;

        /// <summary>
        /// y-motion state of the ConfigurableJoint.
        /// </summary>
        public ConfigurableJointMotion motionY = ConfigurableJointMotion.Free;

        /// <summary>
        /// z-motion state of the ConfigurableJoint.
        /// </summary>
        public ConfigurableJointMotion motionZ = ConfigurableJointMotion.Free;

        /// <summary>
        /// Angular X-motion state of the ConfigurableJoint.
        /// </summary>
        public ConfigurableJointMotion angularMotionX = ConfigurableJointMotion.Limited;

        /// <summary>
        /// Angular Y-motion state of the ConfigurableJoint.
        /// </summary>
        public ConfigurableJointMotion angularMotionY = ConfigurableJointMotion.Free;

        /// <summary>
        /// Angular Z-motion state of the ConfigurableJoint.
        /// </summary>
        public ConfigurableJointMotion angularMotionZ = ConfigurableJointMotion.Limited;

        /// <summary>
        /// Retrieves the current motion values from the specified ConfigurableJoint.
        /// </summary>
        public void Get(ConfigurableJoint configurableJoint) {
            motionX = configurableJoint.xMotion;
            motionY = configurableJoint.yMotion;
            motionZ = configurableJoint.zMotion;
            angularMotionX = configurableJoint.angularXMotion;
            angularMotionY = configurableJoint.angularYMotion;
            angularMotionZ = configurableJoint.angularZMotion;
        }

        /// <summary>
        /// Sets the stored motion values onto the specified ConfigurableJoint.
        /// </summary>
        public void Set(ConfigurableJoint configurableJoint) {
            configurableJoint.xMotion = motionX;
            configurableJoint.yMotion = motionY;
            configurableJoint.zMotion = motionZ;
            configurableJoint.angularXMotion = angularMotionX;
            configurableJoint.angularYMotion = angularMotionY;
            configurableJoint.angularZMotion = angularMotionZ;
        }

        /// <summary>
        /// Resets all motion values to Free on the specified ConfigurableJoint.
        /// </summary>
        public void Reset(ConfigurableJoint configurableJoint) {
            configurableJoint.xMotion = ConfigurableJointMotion.Free;
            configurableJoint.yMotion = ConfigurableJointMotion.Free;
            configurableJoint.zMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularZMotion = ConfigurableJointMotion.Free;
        }

    }

    /// <summary>
    /// Maximum allowed rotation in degrees between the trailer and the towing vehicle.
    /// </summary>
    [Space()]
    public float maxAngle = 90f; // Maximum allowed rotation in degrees

    /// <summary>
    /// Holds the stored joint restrictions for the trailer’s ConfigurableJoint.
    /// </summary>
    private JointRestrictions jointRestrictions = new JointRestrictions();

    /// <summary>
    /// Caches a transform representing the center position of the trailer for bound calculations.
    /// </summary>
    private Transform _centerPosition = null;

    /// <summary>
    /// Retrieves the position of the trailer's internal "_CenterPosition" object,
    /// which is set to the approximate bounds center of the trailer.
    /// </summary>
    public Vector3 CenterPosition {
        get {
            if (_centerPosition == null) {
                _centerPosition = new GameObject("_CenterPosition").transform;
                _centerPosition.transform.SetParent(transform);
                _centerPosition.position = RCCP_GetBounds.GetBoundsCenter(transform);
                _centerPosition.rotation = transform.rotation;
            }
            return _centerPosition.position;
        }
    }

    public Transform TrailerConnectionPoint {
        get {
            if (trailerConnectionPoint == null) {
                trailerConnectionPoint = new GameObject("RCCP_TrailerConnectionPoint").transform;
                trailerConnectionPoint.SetParent(transform);
                trailerConnectionPoint.localPosition = Vector3.zero;
                trailerConnectionPoint.localRotation = Quaternion.identity;
            }
            if (trailerConnectionPoint.localPosition == Vector3.zero && attacher)
                trailerConnectionPoint.position = attacher.transform.position;
            return trailerConnectionPoint;
        }
        set {
            trailerConnectionPoint = value;
        }
    }
    public Transform trailerConnectionPoint;

    // ------------------------------------------
    //  TRAILER LIGHTS
    // ------------------------------------------

    /// <summary>
    /// All RCCP_Light components found on the trailer, cached at startup.
    /// </summary>
    [HideInInspector] public List<RCCP_Light> allLights = new List<RCCP_Light>();

    /// <summary>
    /// Unity's Start method, runs once when the script is first enabled.
    /// Initializes references and sets up the ConfigurableJoint if already connected.
    /// </summary>
    private void Start() {

        jointRestrictions.Get(Joint);

        Joint.configuredInWorldSpace = true;
        Joint.autoConfigureConnectedAnchor = false;
        Joint.connectedAnchor = Vector3.zero;
        Joint.connectedBody = null;

        // If a connectedBody is assigned by default, attach the trailer.
        if (Joint.connectedBody)
            AttachTrailer(Joint.connectedBody.gameObject.GetComponent<RCCP_CarController>());
        else
            DetachTrailer();

        GetAllLights();

    }

    /// <summary>
    /// Unity's FixedUpdate, used for applying physics-based logic, including motor and brake torques.
    /// </summary>
    private void FixedUpdate() {

        attached = (Joint.connectedBody != null);

        if (attached && Rigid.centerOfMass != transform.InverseTransformPoint(COM.transform.position))
            Rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);

        // If trailer is not connected, skip torque logic.
        if (!carController)
            return;

        if (trailerWheels != null && trailerWheels.Length > 0) {
            // Apply motor/ brake torque
            for (int i = 0; i < trailerWheels.Length; i++) {
                trailerWheels[i].Torque(carController.throttleInput_V * (attached ? 1f : 0f));
                trailerWheels[i].Brake(attached ? 0f : 5000f);
            }
        }

        if (carController) {
            // Get the trailer's forward in the truck's local space
            Vector3 localForward = carController.transform.InverseTransformDirection(transform.forward);

            // Yaw angle is basically the angle in XZ-plane of that local space
            float angle = Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;

            // If the angle exceeds the limit, apply a corrective force
            if (Mathf.Abs(angle) > maxAngle) {
                float correction = Mathf.Sign(angle) * (Mathf.Abs(angle) - maxAngle);

                // Apply the torque around local up
                Rigid.AddRelativeTorque(Vector3.up * -correction * 5f, ForceMode.Acceleration);

                // Slightly reduce angular velocity for stability
                Rigid.angularVelocity *= 0.95f;
            }

        }

    }

    /// <summary>
    /// Unity's Update, used for checking trailer sleep state, handling attach/detach timers,
    /// aligning wheels, and updating trailer lights.
    /// </summary>
    private void Update() {

        // Detect if trailer is effectively sleeping
        if (Rigid.linearVelocity.sqrMagnitude < 0.01f && Mathf.Abs(Rigid.angularVelocity.sqrMagnitude) < 0.01f)
            isSleeping = true;
        else
            isSleeping = false;

        // Decrement attach/detach timer
        if (timer > 0f)
            timer -= Time.deltaTime;

        timer = Mathf.Clamp01(timer);

        // Wheel alignment
        WheelAlign();

        // If we have a reference to the carController, bind that to the lights
        if (carController) {
            foreach (RCCP_Light item in allLights)
                item.CarController = carController;
        } else {
            foreach (RCCP_Light item in allLights)
                item.CarController = null;
        }

    }

    /// <summary>
    /// Aligning wheel model position and rotation, and applying slip-based steer offsets.
    /// </summary>
    private void WheelAlign() {

        if (isSleeping)
            return;

        if (trailerWheels == null || trailerWheels.Length == 0)
            return;

        float averageSidewaysSlip = 0f;
        int steeringWheelAmount = 0;

        for (int i = 0; i < trailerWheels.Length; i++) {

            if (!trailerWheels[i].wheelModel)
                continue;

            Vector3 wheelPosition;
            Quaternion wheelRotation;
            trailerWheels[i].wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);
            trailerWheels[i].wheelModel.SetPositionAndRotation(wheelPosition, wheelRotation);

            if (trailerWheels[i].steering) {
                WheelHit hit;
                trailerWheels[i].wheelCollider.GetGroundHit(out hit);
                int direction = transform.InverseTransformDirection(Rigid.linearVelocity).z > 0 ? 1 : -1;

                averageSidewaysSlip += hit.sidewaysSlip * direction;
                steeringWheelAmount++;
            }
        }

        // Average the slip across all steering wheels (including negative slip)
        if (steeringWheelAmount > 0)
            averageSidewaysSlip /= steeringWheelAmount;

        // Apply steering angle offset based on the slip
        for (int i = 0; i < trailerWheels.Length; i++) {
            if (trailerWheels[i].steering) {
                if (trailerWheels[i].localPosition == Vector3.zero)
                    trailerWheels[i].localPosition = trailerWheels[i].wheelCollider.transform.localPosition;

                trailerWheels[i].wheelCollider.steerAngle += averageSidewaysSlip;
                trailerWheels[i].wheelCollider.steerAngle = Mathf.Clamp(
                    trailerWheels[i].wheelCollider.steerAngle,
                    -trailerWheels[i].maxSteerAngle,
                    trailerWheels[i].maxSteerAngle
                );

                int side = (trailerWheels[i].localPosition.x < 0) ? -1 : 1;

                Vector3 targetPos = new Vector3(
                    trailerWheels[i].localPosition.x - trailerWheels[i].wheelCollider.steerAngle * 0.01f,
                    trailerWheels[i].localPosition.y,
                    trailerWheels[i].localPosition.z - trailerWheels[i].wheelCollider.steerAngle * 0.015f * side
                );
                trailerWheels[i].wheelCollider.transform.localPosition = targetPos;
            }
        }
    }

    // ------------------------------------------
    //  ATTACH / DETACH
    // ------------------------------------------

    /// <summary>
    /// Detaches the trailer from the current towing vehicle, disabling any references
    /// on the towing vehicle’s side and restoring the trailer’s joint motion to free.
    /// </summary>
    public void DetachTrailer() {

        if (carController && carController.OtherAddonsManager && carController.OtherAddonsManager.TrailAttacher)
            carController.OtherAddonsManager.TrailAttacher.attachedTrailer = null;

        carController = null;
        timer = 1f;

        Joint.connectedBody = null;
        Joint.autoConfigureConnectedAnchor = false;
        Joint.connectedAnchor = Vector3.zero;

        jointRestrictions.Reset(Joint);

        if (legs)
            legs.SetActive(true);

    }

    /// <summary>
    /// Attaches the trailer to the specified vehicle, setting up the ConfigurableJoint and
    /// referencing the car controller for torque/lights logic.
    /// </summary>
    public void AttachTrailer(RCCP_CarController vehicle) {

        if (attached)
            return;

        if (timer > 0f)
            return;

        timer = 1f;
        carController = vehicle;

        if (carController && carController.OtherAddonsManager && carController.OtherAddonsManager.TrailAttacher)
            carController.OtherAddonsManager.TrailAttacher.attachedTrailer = this;

        Joint.connectedBody = carController.Rigid;
        Joint.autoConfigureConnectedAnchor = false;

        Vector3 connectionPoint = carController.transform.InverseTransformPoint(TrailerConnectionPoint.position);
        connectionPoint.x = 0f;
        connectionPoint.y = 0f;

        Joint.connectedAnchor = connectionPoint;
        jointRestrictions.Set(Joint);

        if (legs)
            legs.SetActive(false);

        Rigid.isKinematic = false;

    }

    /// <summary>
    /// Finds all RCCP_Light components under this vehicle and updates the manager’s lights list.
    /// </summary>
    public void GetAllLights() {

        if (allLights == null)
            allLights = new List<RCCP_Light>();

        allLights.Clear();
        allLights = GetComponentsInChildren<RCCP_Light>(true).ToList();

    }

    /// <summary>
    /// Unity's Reset method, called in the Editor when adding this component.
    /// Sets up default child objects (COM, Wheel Models, Wheel Colliders) if they are not found.
    /// Also initializes mass and other default values.
    /// </summary>
    private void Reset() {

        if (Joint) {

            Joint.axis = Vector3.right;
            Joint.secondaryAxis = Vector3.zero;

            Joint.xMotion = ConfigurableJointMotion.Locked;
            Joint.yMotion = ConfigurableJointMotion.Locked;
            Joint.zMotion = ConfigurableJointMotion.Locked;
            Joint.angularXMotion = ConfigurableJointMotion.Limited;
            Joint.angularYMotion = ConfigurableJointMotion.Free;
            Joint.angularZMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit sj = Joint.lowAngularXLimit;
            sj.limit = -20f;
            Joint.lowAngularXLimit = sj;
            sj = Joint.highAngularXLimit;
            sj.limit = 20f;
            Joint.highAngularXLimit = sj;
            sj = Joint.angularZLimit;
            sj.limit = 10f;
            Joint.angularZLimit = sj;

            Joint.autoConfigureConnectedAnchor = false;
            Joint.configuredInWorldSpace = true;

        }

        // COM
        if (COM == null) {

            GameObject com = new GameObject("COM");
            com.transform.SetParent(transform, false);
            com.transform.localPosition = Vector3.zero;
            com.transform.localRotation = Quaternion.identity;
            com.transform.localScale = Vector3.one;
            COM = com.transform;

        }

        // Trailer Attacher
        if (attacher == null) {

            attacher = new GameObject("RCCP_TrailerAttacher").AddComponent<RCCP_TrailerAttacher>();
            attacher.transform.localScale = new Vector3(1f, .5f, .1f);
            attacher.transform.SetParent(transform, false);
            attacher.transform.localPosition = new Vector3(0f, 0f, 2.5f);
            attacher.transform.localRotation = Quaternion.identity;

        }

        // trailerConnectionPoint
        TrailerConnectionPoint = new GameObject("RCCP_TrailerConnectionPoint").transform;
        TrailerConnectionPoint.transform.SetParent(transform);
        TrailerConnectionPoint.transform.localPosition = Vector3.zero;
        TrailerConnectionPoint.transform.localRotation = Quaternion.identity;
        TrailerConnectionPoint.transform.localScale = Vector3.one;
        TrailerConnectionPoint.position = attacher.transform.position;

        // Wheel Models
        if (transform.Find("Wheel Models") == null) {

            GameObject go = new GameObject("Wheel Models");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

        }

        // Wheel Colliders
        if (transform.Find("Wheel Colliders") == null) {

            GameObject go = new GameObject("Wheel Colliders");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

        }

        Rigidbody rigidb = GetComponent<Rigidbody>();

        if (!rigidb)
            rigidb = gameObject.AddComponent<Rigidbody>();

        rigidb.mass = 1000f;
        rigidb.linearDamping = .025f;
        rigidb.angularDamping = .5f;
        rigidb.interpolation = RigidbodyInterpolation.Interpolate;
        rigidb.collisionDetectionMode = CollisionDetectionMode.Discrete;

    }

    // ---------------------------------------------
    // GIZMO DRAWING
    // ---------------------------------------------
    private void OnDrawGizmos() {

        // Set a color for the gizmo (e.g., green).
        Gizmos.color = Color.green;

        // Draw a wireframe cube matching the BoxCollider center & size.
        Gizmos.DrawSphere(TrailerConnectionPoint.position, .2f);

    }

}
