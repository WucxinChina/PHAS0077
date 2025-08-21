#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEngine;

namespace Gley.TrafficSystem
{
    // Adapt the speed to the front vehicle.
    public class FollowVehicle : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        private float _minFollowSpeed = 3;
        private float _overtakeTime = 5;
        private float _followTime;
        private bool _disableOvertake;
        private bool _stopped;


        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            _followTime = 0;
            _overtakeTime = 5;
            _minFollowSpeed = 3;
        }


        public void SetOvertakeTime(float time)
        {
            _overtakeTime = time;
        }

        public void SetMinFollowSpeed(float speed)
        {
            _minFollowSpeed = speed;
        }
#endif
        public void DisableOvertake(bool active)
        {
#if GLEY_TRAFFIC_SYSTEM
            _disableOvertake = active;
#endif
        }

#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            var closestObstacle = knownWaypointsList.ClosestObstacle;
            if (closestObstacle.Collider == null || closestObstacle.Collider.attachedRigidbody == null)
            {
                return BehaviourResult; // Exit early if no valid obstacle
            }

            ITrafficParticipant otherVehicle = knownWaypointsList.ClosestObstacle.Collider.attachedRigidbody.GetComponent<ITrafficParticipant>();
            if (otherVehicle == null)
            {
                return BehaviourResult;
            }

            if (otherVehicle.GetCurrentSpeedMS() < _minFollowSpeed)
            {
                _stopped = true;
            }



            float targetSpeed;
            if (_stopped)
            {
                targetSpeed = 0;

                if (requiredBrakePower < 1)
                {
                    requiredBrakePower = 1;
                }
                if (otherVehicle.GetCurrentSpeedMS() > _minFollowSpeed)
                {
                    _stopped = false;
                }
            }
            else
            {
                targetSpeed = knownWaypointsList.GetFollowSpeed();
            }

            // sometimes 2 cars see each other. to avoid this, make one to continue to avoid stopping indefinitely
            if (otherVehicle.AlreadyCollidingWith(AllVehiclesData.AllVehicles[VehicleIndex].AllColliders))
            {
                if (otherVehicle.GetCurrentSpeedMS() < 1)
                {
                    targetSpeed = knownWaypointsList.GetFirstWaypointSpeed();
                }
            }



            if (VehicleComponent.GetCurrentSpeedMS() < _minFollowSpeed)
            {

            }

            // is speed is close to 0, do a complete stop
            if (targetSpeed < _minFollowSpeed)
            {
                targetSpeed = 0;
                _followTime = 0;
                if (stopTargetReached)
                {
                    //BehaviourResult.CompleteStop = true;
                    requiredBrakePower = 10;
                }
            }
            else
            {
                // increase the follow time
                _followTime += Time.deltaTime;
            }

            // if the vehicle is to close from the front one, brake harder and reduce the speed to avoid a crash
            if (stopTargetReached)
            {
                if (requiredBrakePower < 1)
                {
                    requiredBrakePower = 1;
                    targetSpeed *= 0.9f;
                }
            }

            // Perform regular driving
            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), targetSpeed, knownWaypointsList.ClosestObstaclePoint, requiredBrakePower, 0.5f, VehicleComponent.distanceToStop);

            // If the vehicle was followed for long enough, switch to overtake
            if (_followTime > _overtakeTime && _disableOvertake == false)
            {
                Stop();
                API.StartVehicleBehaviour<Overtake>(VehicleIndex);
            }

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}