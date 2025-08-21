#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEngine;

namespace Gley.TrafficSystem
{
    // Adapt the speed to the player speed
    public class FollowPlayer : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        private const float _minFollowSpeed = 2;
        private const float _overtakeTime = 2;

        private float _followTime;

        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            _followTime = 0;
        }


        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();
            var targetSpeed = knownWaypointsList.GetFollowSpeed();

            // is speed is close to 0, do a complete stop
            if (targetSpeed < _minFollowSpeed)
            {
                targetSpeed = 0;
                _followTime = 0;
                if (stopTargetReached)
                {
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

            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), targetSpeed, knownWaypointsList.ClosestObstaclePoint, requiredBrakePower, 0.5f, VehicleComponent.distanceToStop);

            // If the vehicle was followed for long enough, switch to overtake
            if (_followTime > _overtakeTime)
            {
                //Debug.Log("Overtake");
                Stop();
                API.StartVehicleBehaviour<OvertakePlayer>(VehicleIndex);
            }

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}
