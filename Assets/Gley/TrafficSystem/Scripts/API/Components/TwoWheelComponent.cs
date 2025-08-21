using UnityEngine;

namespace Gley.TrafficSystem
{
    /// <summary>
    /// Handles the balancing and leaning behavior of a 2-wheeled vehicle (like a bike or motorcycle).
    /// </summary>
    public class TwoWheelComponent : VehicleComponent
    {
        [Header("2 Wheel Vehicle Properties")]
        [Tooltip("The maximum allowed tilt (lean) angle in degrees during turns.")]
        [SerializeField] private float _maxTilt = 15;

        [Tooltip("How strongly the vehicle resists falling sideways. Higher = more upright stability.")]
        [SerializeField] private float _stabilityFactor = 1500f;

        [Tooltip("How much angular velocity is damped on the Z axis to prevent oscillation.")]
        [SerializeField] private float _damping = 0.1f;

        private float _targetAngle;
        private float _lerpSpeed;
        private float _targetZRotation;
        private Vector3 _localAngularVelocity;

        public override void ApplyAdditionalForces(float wheelTurnAngle)
        {
            // Convert angular velocity to local space so we can isolate Z axis
            _localAngularVelocity = rb.transform.InverseTransformDirection(rb.angularVelocity);

            // Dampen Z rotation (side wobble)
            _localAngularVelocity.z *= (1f - _damping);

            // Apply damped rotation back to Rigidbody
            rb.angularVelocity = rb.transform.TransformDirection(_localAngularVelocity);

            // Compute torque to upright the bike
            Vector3 fullTorque = Vector3.Cross(rb.transform.up, Vector3.up) * _stabilityFactor;

            // Project that torque onto the bike's forward axis (Z axis)
            Vector3 projectedTorque = Vector3.Project(fullTorque, rb.transform.forward);

            // Apply torque only around the forward axis
            rb.AddTorque(projectedTorque, ForceMode.Acceleration);


            // Determine how fast to change target lean — slower when returning to center
            _lerpSpeed = Mathf.Abs(wheelTurnAngle) > Mathf.Abs(_targetAngle) ? 1f : 3f;

            // Smoothly interpolate to target steering angle
            _targetAngle = Mathf.Lerp(_targetAngle, wheelTurnAngle, _lerpSpeed * Time.fixedDeltaTime);

            // Compute turn radius: r = wheelDistance / tan(steering angle)
            float turnRadius = wheelDistance / Mathf.Tan(_targetAngle * Mathf.Deg2Rad);

            // Compute lean angle using physics: lean = atan(v² / (r * g))
            float leanAngleRad = Mathf.Atan((GetCurrentSpeedMS() * GetCurrentSpeedMS()) / (turnRadius * Mathf.Abs(Physics.gravity.y)));

            // Convert to degrees and clamp to max lean
            _targetZRotation = -Mathf.Clamp(leanAngleRad * Mathf.Rad2Deg, -_maxTilt, _maxTilt);

            // Visually apply lean to vehicle body (not physics-based tilt)
            carHolder.localEulerAngles = new Vector3(0f, 0f, _targetZRotation);
        }
    }
}