#if GLEY_TRAFFIC_SYSTEM
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Handles driving part of the vehicle
    /// </summary>
    [BurstCompile]
    public struct DriveJob : IJobParallelFor
    {
        public NativeArray<float3> ForwardForce;
        public NativeArray<float3> LateralForce;
        public NativeArray<float3> TrailerForce;
        public NativeArray<float> WheelRotation;
        public NativeArray<float> TurnAngle;
        public NativeArray<float> VehicleRotationAngle;
        public NativeArray<int> CurrentGear;
        public NativeArray<int> TargetGear;
        public NativeArray<bool> ReadyToRemove;
        public NativeArray<bool> NeedsWaypoint;
        public NativeArray<bool> IsBraking;
        public NativeArray<float> RequiredBrakePower;
        public NativeArray<bool> TargetReached;

        [ReadOnly] public NativeArray<float3> TargetWaypointPosition;
        [ReadOnly] public NativeArray<float3> NextWaypointPosition;
        [ReadOnly] public NativeArray<float3> StopPosition;
        [ReadOnly] public NativeArray<float3> AllBotsPosition;
        [ReadOnly] public NativeArray<float3> GroundDirection;
        [ReadOnly] public NativeArray<float3> ForwardDirection;
        [ReadOnly] public NativeArray<float3> RightDirection;
        [ReadOnly] public NativeArray<float3> TrailerRightDirection;
        [ReadOnly] public NativeArray<float3> CarVelocity;
        [ReadOnly] public NativeArray<float3> TrailerVelocity;
        [ReadOnly] public NativeArray<float3> CameraPositions;
        [ReadOnly] public NativeArray<float> WheelCircumferences;
        [ReadOnly] public NativeArray<float> MaxSteer;
        [ReadOnly] public NativeArray<float> PowerStep;
        [ReadOnly] public NativeArray<float> BrakeStep;
        [ReadOnly] public NativeArray<float> Drag;
        [ReadOnly] public NativeArray<float> TrailerDrag;
        [ReadOnly] public NativeArray<float> MaximumAllowedSpeed;
        [ReadOnly] public NativeArray<float> WheelDistance;
        [ReadOnly] public NativeArray<float> SteeringStep;
        [ReadOnly] public NativeArray<float> MassDifference;
        [ReadOnly] public NativeArray<float> DistanceToStop;
        [ReadOnly] public NativeArray<float> AccelerationPercent;
        [ReadOnly] public NativeArray<float> BrakePercent;
        [ReadOnly] public NativeArray<float> SteerPercent;
        [ReadOnly] public NativeArray<float> TargetSpeed;
        [ReadOnly] public NativeArray<int> NrOfWheels;
        [ReadOnly] public NativeArray<int> TrailerNrOfWheels;
        [ReadOnly] public NativeArray<int> GroundedWheels;
        [ReadOnly] public NativeArray<bool> ExcludedVehicles;
        [ReadOnly] public float3 WorldUp;
        [ReadOnly] public float DistanceToRemove;
        [ReadOnly] public float FixedDeltaTime;

        public void Execute(int index)
        {
            if (ExcludedVehicles[index] == true)
            {
                return;
            }

            if (GroundDirection[index].Equals(float3.zero))
            {
                return;
            }

            IsBraking[index] = false;

            //Calculate current vehicle forward speed
            float3 groundNormal = math.normalize(GroundDirection[index] / GroundedWheels[index]);
            float3 groundForwardDirection = ForwardDirection[index] - groundNormal * math.dot(ForwardDirection[index], groundNormal);
            float3 forwardVelocity = math.dot(CarVelocity[index], groundForwardDirection) * groundForwardDirection;

            float3 groundRightDirection = RightDirection[index] - groundNormal * math.dot(RightDirection[index], groundNormal);
            float3 lateralVelocity = math.dot(CarVelocity[index], groundRightDirection) * groundRightDirection;

            float speedSign = math.sign(Vector3.Dot(forwardVelocity, ForwardDirection[index]));
            float currenrUnsignedSpeed = math.length(forwardVelocity);
            float currentSignedSpeed = currenrUnsignedSpeed * speedSign;

            float changeWaypointDistance = CalculateMinToChangeWaypoint(currenrUnsignedSpeed);
            float waypointDistance = math.distance(TargetWaypointPosition[index], AllBotsPosition[index]);
            float3 waypointDirection = TargetWaypointPosition[index] - AllBotsPosition[index];
            float dotProduct = math.dot(waypointDirection, ForwardDirection[index]);// used to check if vehicle passed the current waypoint

            //frames to reach target
            //compute per frame velocity
            float velocityPerFrame = currenrUnsignedSpeed * FixedDeltaTime;

            if (!StopPosition[index].Equals(TrafficSystemConstants.DEFAULT_POSITION))
            {
                //check number of frames required to reach destination
                float targetDistance = math.distance(StopPosition[index], AllBotsPosition[index]) - DistanceToStop[index];

                float3 stopDirection = StopPosition[index] - AllBotsPosition[index];
                float stopDotProduct = math.dot(stopDirection, ForwardDirection[index]);

                if (TargetSpeed[index] < currenrUnsignedSpeed)
                {
                    float requiredDeceleration = (currenrUnsignedSpeed * currenrUnsignedSpeed - TargetSpeed[index] * TargetSpeed[index]) / (2 * targetDistance) * FixedDeltaTime;

                    if (requiredDeceleration < 0)
                    {
                        RequiredBrakePower[index] = 1;
                    }
                    else
                    {
                        RequiredBrakePower[index] = requiredDeceleration / BrakeStep[index] * (1 + 0);
                    }
                }
                else
                {
                    RequiredBrakePower[index] = 0;
                }

                if ((stopDotProduct < 0 && TargetGear[index] != -1) || targetDistance < 0)
                {

                    TargetReached[index] = true;
                }
                else
                {
                    TargetReached[index] = false;
                }
            }
            else
            {
                TargetReached[index] = false;
                RequiredBrakePower[index] = 0;
            }

            //check if a new waypoint is required
            if (waypointDistance < changeWaypointDistance || (dotProduct < 0 && waypointDistance < changeWaypointDistance * 5))
            {
                NeedsWaypoint[index] = true;
            }

            float targetSpeed;
            float maxSpeed;
            // gearbox
            if (CurrentGear[index] != TargetGear[index])
            {
                maxSpeed = 0;
            }
            else
            {
                if (CurrentGear[index] == -1)
                {
                    maxSpeed = MaximumAllowedSpeed[index] / 5;
                }
                else
                {
                    maxSpeed = MaximumAllowedSpeed[index];
                }
            }


            //steering
            float angleToNextWaypoint;
            float3 currentPosition = AllBotsPosition[index];
            float3 currentWaypoint = TargetWaypointPosition[index];
            float3 nextWaypoint = NextWaypointPosition[index];

            float3 predictedWaypoint = GetQuadraticBezierPoint(0.1f, currentPosition, currentWaypoint, nextWaypoint);
            float3 predictedWaypointDirection = predictedWaypoint - AllBotsPosition[index];
            if (SteerPercent[index] == TrafficSystemConstants.AUTO_STEER)
            {
                angleToNextWaypoint = SignedAngle(ForwardDirection[index], waypointDirection, WorldUp);
            }
            else
            {
                angleToNextWaypoint = SteerPercent[index] * MaxSteer[index];
            }


            bool canMakeTurn = CanTurn(index, waypointDistance, currenrUnsignedSpeed, angleToNextWaypoint);
            //canMakeTurn = true;

            //Calculate the next frame speed
            targetSpeed = GetTargetSpeed(index, currenrUnsignedSpeed, maxSpeed, canMakeTurn);



            if (targetSpeed == 0)
            {
                if (currenrUnsignedSpeed < 0.0001)
                {
                    CurrentGear[index] = TargetGear[index];
                }
            }

            float3 lateralCancelForce = -lateralVelocity / FixedDeltaTime / NrOfWheels[index];

            //Add required forces to achieved the target speed in the next frame
            float dv = GetVelocity(index, currentSignedSpeed, targetSpeed * CurrentGear[index]);
            ForwardForce[index] = groundForwardDirection * dv;
            LateralForce[index] = lateralCancelForce;
            if (TrailerNrOfWheels[index] > 0)
            {
                TrailerForce[index] = -TrailerRightDirection[index] * Vector3.Dot(TrailerVelocity[index], TrailerRightDirection[index]) / TrailerNrOfWheels[index];
            }

            //Check if vehicle is far enough from the player and it can be removed
            RemoveVehicle(index);

            //Calculate the wheel turn amount
            WheelRotation[index] += (360 * (currentSignedSpeed / WheelCircumferences[index]) * FixedDeltaTime);

            //Calculate steering angle
            ComputeSteerAngle(index, MaxSteer[index], targetSpeed, angleToNextWaypoint);
        }

        private float CalculateMinToChangeWaypoint(float currentSpeed)
        {
            if (currentSpeed.ToKMH() < 50)
            {
                return 4f; // at low speeds change waypoints at 1.5 meters
            }
            // at add 1m to every 50km/h
            return 4f + (currentSpeed.ToKMH() - 50f) * 0.02f;
        }


        private bool CanTurn(int index, float waypointDistance, float targetVelocity, float angle)
        {
            //check if the car can turn at current speed         
            float framesToReach = waypointDistance / (targetVelocity * FixedDeltaTime);

            //if it is close to the waypoint turn at normal speed 
            if (framesToReach > 5)
            {
                //calculate the number of frames required to rotate to the target amount
                float framesToRotate = math.abs(angle - TurnAngle[index]) / SteeringStep[index];
                if (framesToRotate > framesToReach)
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Compute the required steering angle
        /// </summary>
        /// <param name = "index" ></ param >
        /// < param name="maxSteer"></param>
        /// <param name = "targetVelocity" ></ param >
        void ComputeSteerAngle(int index, float maxSteer, float targetVelocity, float angleToNextWaypoint)
        {
            float currentTurnAngle = TurnAngle[index];
            float currentStep = SteeringStep[index];

            //apply turning speed
            currentTurnAngle = math.clamp(currentTurnAngle + math.clamp(angleToNextWaypoint - currentTurnAngle, -currentStep, currentStep), -maxSteer, maxSteer);

            if (math.abs(angleToNextWaypoint) < math.abs(currentTurnAngle))
            {
                currentTurnAngle = angleToNextWaypoint;
            }

            //compute the body turn angle based on wheel turn amount
            float turnRadius = WheelDistance[index] / math.tan(math.radians(currentTurnAngle));
            VehicleRotationAngle[index] = (180 * targetVelocity * FixedDeltaTime) / (math.PI * turnRadius) * CurrentGear[index];
            TurnAngle[index] = currentTurnAngle;
        }


        private Vector3 GetQuadraticBezierPoint(float t, Vector3 P0, Vector3 P1, Vector3 P2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            // Calculate the position at t on the curve
            return (uu * P0) + (2 * u * t * P1) + (tt * P2);
        }


        private float GetVelocity(int index, float currentSpeed, float targetSpeed)
        {
            float dSpeed = targetSpeed - currentSpeed;
            float velocity = dSpeed + GetDrag(targetSpeed, Drag[index], FixedDeltaTime);
            if (TrailerNrOfWheels[index] > 0)
            {
                if (targetSpeed != 0)
                {
                    velocity += dSpeed * MassDifference[index] + GetDrag(targetSpeed, TrailerDrag[index], FixedDeltaTime);
                }
            }

            return velocity / FixedDeltaTime / NrOfWheels[index];
        }


        private float GetTargetSpeed(int index, float currentSpeed, float maxSpeed, bool canTurn)
        {

            // break if cannot turn in time
            if (canTurn == false)
            {
                maxSpeed = 0;
            }

            float targetSpeed = currentSpeed;
            float speedDifference = math.abs(targetSpeed - maxSpeed);

            if (speedDifference < PowerStep[index] * AccelerationPercent[index])
            {
                //Debug.Log(1);
                return maxSpeed;
            }

            //brake if the vehicle runs faster than the max allowed speed
            if (targetSpeed > maxSpeed)
            {
                //for the brake lights to be active only when hard brakes are needed, to avoid short blinking
                if (speedDifference > 1)
                {
                    //turn on braking lights
                    IsBraking[index] = true;
                }

                if (BrakePercent[index] == 0)
                {
                    targetSpeed = math.max(maxSpeed, targetSpeed - BrakeStep[index]);
                }
                else
                {
                    targetSpeed = math.max(maxSpeed, targetSpeed - BrakeStep[index] * BrakePercent[index]);
                }//Debug.Log(2);
            }
            else
            {
                targetSpeed += PowerStep[index] * AccelerationPercent[index];
                //Debug.Log(3);
            }

            return targetSpeed;
        }


        /// <summary>
        /// Checks if the vehicle can be removed from scene
        /// </summary>
        /// <param name="index">the list index of the vehicle</param>
        void RemoveVehicle(int index)
        {
            bool remove = true;
            for (int i = 0; i < CameraPositions.Length; i++)
            {
                if (math.distancesq(AllBotsPosition[index], CameraPositions[i]) < DistanceToRemove)
                {
                    remove = false;
                    break;
                }
            }
            ReadyToRemove[index] = remove;
        }


        /// <summary>
        /// Compute sign angle between 2 directions
        /// </summary>
        /// <param name="dir1"></param>
        /// <param name="dir2"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        float SignedAngle(float3 dir1, float3 dir2, float3 normal)
        {
            if (dir1.Equals(float3.zero))
            {
                return 0;
            }
            dir1 = math.normalize(dir1);
            return math.degrees(math.atan2(math.dot(math.cross(dir1, dir2), normal), math.dot(dir1, dir2)));
        }


        /// <summary>
        /// Compensate the drag from the physics engine
        /// </summary>
        /// <param name="index"></param>
        /// <param name="targetSpeed"></param>
        /// <returns></returns>
        float GetDrag(float targetSpeed, float drag, float fixedDeltaTime)
        {
            float result = targetSpeed / (1 - drag * fixedDeltaTime) - targetSpeed;
            return result;
        }
    }
}
#endif