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
/// Transmits the received power from the differential and distributes it to the wheels (if the differential is connected to this axle). 
/// Manages steering, braking, traction, and wheel-related processes for two connected wheels.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Axle")]
public class RCCP_Axle : RCCP_Component {

#if UNITY_EDITOR
    public bool autoAlignWheelColliders = true;
#endif

    /// <summary>
    /// Left wheel model for visual representation.
    /// </summary>
    public Transform leftWheelModel;

    /// <summary>
    /// Right wheel model for visual representation.
    /// </summary>
    public Transform rightWheelModel;

    /// <summary>
    /// WheelCollider for the left wheel.
    /// </summary>
    public RCCP_WheelCollider leftWheelCollider;

    /// <summary>
    /// WheelCollider for the right wheel.
    /// </summary>
    public RCCP_WheelCollider rightWheelCollider;

    /// <summary>
    /// Anti-roll bar force used to reduce body roll.
    /// </summary>
    [Min(0f)] public float antirollForce = 500f;

    /// <summary>
    /// Whether this axle is driven (receives power).
    /// </summary>
    public bool isPower = false;

    /// <summary>
    /// Whether this axle is steered.
    /// </summary>
    public bool isSteer = false;

    /// <summary>
    /// Whether this axle applies braking force.
    /// </summary>
    public bool isBrake = false;

    /// <summary>
    /// Whether this axle applies handbrake force.
    /// </summary>
    public bool isHandbrake = false;

    /// <summary>
    /// Adjusts how much motor torque is applied, from -1 to 1. 
    /// A negative multiplier can invert the direction of the torque.
    /// </summary>
    [Range(-1f, 1f)] public float powerMultiplier = 1f;

    /// <summary>
    /// Adjusts how much steering angle is applied, from -1 to 1.
    /// A negative multiplier can invert the steering direction.
    /// </summary>
    [Range(-1f, 1f)] public float steerMultiplier = 1f;

    /// <summary>
    /// Adjusts how much brake torque is applied, from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float brakeMultiplier = 1f;

    /// <summary>
    /// Adjusts how much handbrake torque is applied, from 0 to 1.
    /// </summary>
    [Range(0f, 1f)] public float handbrakeMultiplier = 1f;

    /// <summary>
    /// Throttle input value from the CarController.
    /// </summary>
    [Min(0f)] public float throttleInput = 0f;

    /// <summary>
    /// Brake input value from the CarController.
    /// </summary>
    [Min(0f)] public float brakeInput = 0f;

    /// <summary>
    /// Steering input value from the CarController, ranging from -1 to 1.
    /// </summary>
    [Range(-1f, 1f)] public float steerInput = 0f;

    /// <summary>
    /// Handbrake input value from the CarController.
    /// </summary>
    [Min(0f)] public float handbrakeInput = 0f;

    /// <summary>
    /// The current steering angle (degrees).
    /// </summary>
    public float steerAngle = 0f;

    /// <summary>
    /// How quickly the steering angle approaches the target steerInput.
    /// </summary>
    [Range(.01f, 5f)] public float steerSpeed = 1f;

    /// <summary>
    /// Whether either of the connected wheels is grounded.
    /// </summary>
    public bool isGrounded = false;

    /// <summary>
    /// Maximum brake torque (in Nm).
    /// </summary>
    [Min(0f)] public float maxBrakeTorque = 5000f;

    /// <summary>
    /// Maximum steer angle (degrees).
    /// </summary>
    [Min(0f)] public float maxSteerAngle = 40f;

    /// <summary>
    /// Traction helper received from the RCCP_Stability component to avoid spins.
    /// </summary>
    public float tractionHelpedSidewaysStiffness = 1f;

    /// <summary>
    /// Produced motor torque (in Nm) for the left wheel.
    /// </summary>
    public float producedMotorTorqueNM_Left = 0f;

    /// <summary>
    /// Produced motor torque (in Nm) for the right wheel.
    /// </summary>
    public float producedMotorTorqueNM_Right = 0f;

    /// <summary>
    /// Produced brake torque (in Nm).
    /// </summary>
    public float producedBrakeTorqueNM = 0f;

    /// <summary>
    /// Produced handbrake torque (in Nm).
    /// </summary>
    public float producedHandbrakeTorqueNM = 0f;

    private void Update() {

        Inputs();
        CheckGrounded();

    }

    /// <summary>
    /// Receives input values from the CarController each frame.
    /// </summary>
    private void Inputs() {

        throttleInput = CarController.throttleInput_P;
        steerInput = CarController.steerInput_P;
        brakeInput = CarController.brakeInput_P;
        handbrakeInput = CarController.handbrakeInput_P;

    }

    /// <summary>
    /// Determines whether either wheel on this axle is currently grounded.
    /// </summary>
    private void CheckGrounded() {

        if (!leftWheelCollider || !rightWheelCollider)
            return;

        if ((leftWheelCollider.WheelCollider.enabled && leftWheelCollider.WheelCollider.isGrounded) ||
            (rightWheelCollider.WheelCollider.enabled && rightWheelCollider.WheelCollider.isGrounded))
            isGrounded = true;
        else
            isGrounded = false;

    }

    private void FixedUpdate() {

        // Ensures both wheel colliders know which axle they belong to.
        if (leftWheelCollider)
            leftWheelCollider.connectedAxle = this;
        if (rightWheelCollider)
            rightWheelCollider.connectedAxle = this;

        // Apply steer input smoothly over time.
        steerAngle = Mathf.MoveTowards(steerAngle, steerInput * maxSteerAngle, Time.fixedDeltaTime * steerSpeed * 150f);

        // Calculate brake and handbrake torques based on inputs.
        producedBrakeTorqueNM = brakeInput * maxBrakeTorque;
        producedHandbrakeTorqueNM = handbrakeInput * maxBrakeTorque;

        AntiRollBars();
        Output();

    }

    /// <summary>
    /// Applies anti-roll force to counteract body roll between left and right wheels on this axle.
    /// </summary>
    private void AntiRollBars() {

        if (!leftWheelCollider || !rightWheelCollider)
            return;

        if (!leftWheelCollider.WheelCollider.enabled || !rightWheelCollider.WheelCollider.enabled)
            return;

        if (!leftWheelCollider.wheelHit.collider || !rightWheelCollider.wheelHit.collider)
            return;

        if (leftWheelCollider.wheelHit.point == Vector3.zero || rightWheelCollider.wheelHit.point == Vector3.zero)
            return;

        float travel_L = 1f;
        float travel_R = 1f;

        bool grounded_L = leftWheelCollider.isGrounded;
        if (grounded_L)
            travel_L = (-leftWheelCollider.transform.InverseTransformPoint(leftWheelCollider.wheelHit.point).y - leftWheelCollider.WheelCollider.radius) / leftWheelCollider.WheelCollider.suspensionDistance;

        bool grounded_R = rightWheelCollider.isGrounded;
        if (grounded_R)
            travel_R = (-rightWheelCollider.transform.InverseTransformPoint(rightWheelCollider.wheelHit.point).y - rightWheelCollider.WheelCollider.radius) / rightWheelCollider.WheelCollider.suspensionDistance;

        float calculatedForce = (travel_L - travel_R) * antirollForce;

        // Apply forces at each wheel's position to reduce body roll.
        if (leftWheelCollider.WheelCollider.enabled && rightWheelCollider.WheelCollider.enabled) {

            if (grounded_L)
                CarController.Rigid.AddForceAtPosition(leftWheelCollider.transform.up * -calculatedForce, leftWheelCollider.transform.position);
            if (grounded_R)
                CarController.Rigid.AddForceAtPosition(rightWheelCollider.transform.up * calculatedForce, rightWheelCollider.transform.position);

        }

    }

    /// <summary>
    /// Receives motor torque output from the differential and stores for this axle.
    /// </summary>
    /// <param name="left">Left wheel motor torque (Nm)</param>
    /// <param name="right">Right wheel motor torque (Nm)</param>
    public void ReceiveOutput(float left, float right) {

        producedMotorTorqueNM_Left = left;
        producedMotorTorqueNM_Right = right;

    }

    /// <summary>
    /// Applies motor torque, steer angle, brake torque, and handbrake torque to the wheel colliders.
    /// </summary>
    private void Output() {

        // Apply multipliers to the torque/brake values.
        producedMotorTorqueNM_Left *= powerMultiplier;
        producedMotorTorqueNM_Right *= powerMultiplier;
        producedBrakeTorqueNM *= brakeMultiplier;
        producedHandbrakeTorqueNM *= handbrakeMultiplier;

        // Applies motor torque if this axle is powered.
        if (isPower) {

            if (leftWheelCollider)
                leftWheelCollider.AddMotorTorque(producedMotorTorqueNM_Left);

            if (rightWheelCollider)
                rightWheelCollider.AddMotorTorque(producedMotorTorqueNM_Right);

        }

        // Applies steering if this axle can steer.
        if (isSteer) {

            if (leftWheelCollider)
                leftWheelCollider.ApplySteering(steerAngle * steerMultiplier);

            if (rightWheelCollider)
                rightWheelCollider.ApplySteering(steerAngle * steerMultiplier);

        }

        // Applies brake torque if this axle can brake.
        if (isBrake) {

            // Park gear in automatic DNRP sets the brake torque to full.
            if (CarController.Gearbox &&
                CarController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic_DNRP &&
                CarController.Gearbox.automaticGearSelector == RCCP_Gearbox.SemiAutomaticDNRPGear.P)
                producedBrakeTorqueNM = 1f * maxBrakeTorque;

            if (leftWheelCollider)
                leftWheelCollider.AddBrakeTorque(producedBrakeTorqueNM / 2f);

            if (rightWheelCollider)
                rightWheelCollider.AddBrakeTorque(producedBrakeTorqueNM / 2f);

        }

        // Applies handbrake torque if this axle can handbrake and handbrake input is sufficient.
        if (isHandbrake && handbrakeInput >= .2f) {

            // Park gear in automatic DNRP sets the handbrake torque to full.
            if (CarController.Gearbox &&
                CarController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic_DNRP &&
                CarController.Gearbox.automaticGearSelector == RCCP_Gearbox.SemiAutomaticDNRPGear.P)
                producedHandbrakeTorqueNM = 1f * maxBrakeTorque;

            if (leftWheelCollider)
                leftWheelCollider.AddHandbrakeTorque(producedHandbrakeTorqueNM / 2f);

            if (rightWheelCollider)
                rightWheelCollider.AddHandbrakeTorque(producedHandbrakeTorqueNM / 2f);

        }

    }

    /// <summary>
    /// Resets axle-related variables to default.
    /// </summary>
    public void Reload() {

        throttleInput = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        handbrakeInput = 0f;
        steerAngle = 0f;
        tractionHelpedSidewaysStiffness = 1f;
        producedBrakeTorqueNM = 0f;
        producedHandbrakeTorqueNM = 0f;
        producedMotorTorqueNM_Left = 0f;
        producedMotorTorqueNM_Right = 0f;

    }

    private void Reset() {

        // Destroys any existing WheelColliders to recreate fresh ones.
        RCCP_WheelCollider[] oldWheelColliders = gameObject.GetComponentsInChildren<RCCP_WheelCollider>(true);

        for (int i = 0; i < oldWheelColliders.Length; i++)
            DestroyImmediate(oldWheelColliders[i].gameObject);

        GameObject newWheelCollider_L = new GameObject("WheelCollider_L");
        newWheelCollider_L.transform.SetParent(transform, false);
        RCCP_WheelCollider wheelCollider_L = newWheelCollider_L.AddComponent<RCCP_WheelCollider>();

        GameObject newWheelCollider_R = new GameObject("WheelCollider_R");
        newWheelCollider_R.transform.SetParent(transform, false);
        RCCP_WheelCollider wheelCollider_R = newWheelCollider_R.AddComponent<RCCP_WheelCollider>();

        leftWheelCollider = wheelCollider_L;
        rightWheelCollider = wheelCollider_R;

        leftWheelCollider.connectedAxle = this;
        rightWheelCollider.connectedAxle = this;

    }

}
