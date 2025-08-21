using UnityEngine;

namespace Gley.TrafficSystem
{
    [RequireComponent(typeof(Rigidbody))]
    [HelpURL("https://gley.gitbook.io/mobile-traffic-system-v3/setup-guide/truck-+-trailer-implementation")]
    public class TrailerComponent : MonoBehaviour, ITrafficParticipant
    {
        [Header("Object References")]
        [Tooltip("RigidBody of the vehicle")]
        public Rigidbody rb;
        [Tooltip("Empty GameObject used to rotate the vehicle from the correct point")]
        public Transform trailerHolder;
        [Tooltip("The point where the trailer attaches to the truck")]
        public Transform truckConnectionPoint;
        [Tooltip("The joint that will connect to the truck")]
        public ConfigurableJoint joint;
        [Tooltip("All trailer wheels and their properties")]
        public Internal.Wheel[] allWheels;
        [Tooltip("If suspension is set to 0, the value of suspension will be half of the wheel radius")]
        public float maxSuspension = 0f;
        [Tooltip("How rigid the suspension will be. Higher the value -> more rigid the suspension")]
        public float springStiffness = 5;


        [HideInInspector]
        public float width;
        [HideInInspector]
        public float height;
        [HideInInspector]
        public float length;


        private VehicleComponent _associatedVehicle;
        private float _springForce;

        public Vector3 Velocity
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return rb.linearVelocity;
#else
                return rb.velocity;
#endif
            }
        }

        public void Initialize(VehicleComponent associatedVehicle)
        {
            _associatedVehicle = associatedVehicle;
            _springForce = ((rb.mass * -Physics.gravity.y) / allWheels.Length);
            Vector3 centerOfMass = Vector3.zero;
            for (int i = 0; i < allWheels.Length; i++)
            {
                allWheels[i].wheelTransform.Translate(Vector3.up * (allWheels[i].maxSuspension / 2 + allWheels[i].wheelRadius));
                centerOfMass += allWheels[i].wheelTransform.position;
            }
            rb.centerOfMass = centerOfMass / allWheels.Length;
            DeactivateVehicle();
        }


        public float GetCurrentSpeedMS()
        {
            return _associatedVehicle.GetCurrentSpeedMS();
        }


        public void DeactivateVehicle()
        {
            rb.transform.localPosition = Vector3.zero;
            rb.transform.localRotation = Quaternion.identity;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }


        public int GetNrOfWheels()
        {
            return allWheels.Length;
        }


        public float GetSpringForce()
        {
            return _springForce;
        }


        public float GetSpringStiffness()
        {
            return springStiffness;
        }


        public Vector3 GetHeading()
        {
            return transform.forward;
        }

        public bool AlreadyCollidingWith(Collider[] allColliders)
        {
            return false;
        }
    }
}