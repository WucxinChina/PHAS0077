#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
using UnityEngine;
#endif

namespace Gley.TrafficSystem
{
    public class IgnoreTrafficRules : VehicleBehaviour
    {
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            MovementInfo.OnKnownListUpdated += KnownListUpdatedHandler;
            VehicleComponent.MovementInfo.SetIgnoreRules(true);
        }

        protected override void OnBecameInactive()
        {
            base.OnBecameInactive();
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
            VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(1);
            VehicleComponent.MovementInfo.SetIgnoreRules(false);
        }
#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            if (VehicleComponent.MovementInfo.ClosestObstacle.Collider != null)
            {
                if (VehicleComponent.MovementInfo.ClosestObstacle.ObstacleType == ObstacleTypes.StaticObject)
                {
                    return new BehaviourResult(0);
                }
                if (VehicleComponent.MovementInfo.ClosestObstacle.ObstacleType == ObstacleTypes.TrafficVehicle)
                {
                    var followBehaviour = (FollowVehicle)(API.GetVehicleBehaviourOfType<FollowVehicle>(VehicleIndex));
                    if(followBehaviour!=null)
                    {
                        followBehaviour.SetMinFollowSpeed(0);
                        followBehaviour.SetOvertakeTime(0);
                    }

                    var overtakeBehaviour = (Overtake)(API.GetVehicleBehaviourOfType<Overtake>(VehicleIndex));
                    if (overtakeBehaviour != null)
                    {
                        overtakeBehaviour.SetIgnoreTime(true);
                    }
                    return new BehaviourResult(0);
                }
            }
            var BehaviourResult = new BehaviourResult(100);
            BehaviourResult.MaxAllowedSpeed = VehicleComponent.MaxSpeed;
            PerformForwardMovement(ref BehaviourResult, VehicleComponent.MaxSpeed, VehicleComponent.MaxSpeed, knownWaypointsList.GetFirstStopPosition(), 2, 1f, VehicleComponent.distanceToStop);
            return BehaviourResult;
        }
#endif

        private void KnownListUpdatedHandler(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (vehicleIndex == VehicleIndex)
            {
                var angle = math.clamp(VehicleComponent.MovementInfo.GetAngle(1), 0, 20);
                if (angle > 2)
                {
                    if (VehicleComponent.GetCurrentSpeedMS().ToKMH() / VehicleComponent.maxPossibleSpeed > 0.6f)
                    {
                        VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(1 - angle / 45f);
                        return;
                    }
                }
                VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(1);
            }
#endif
        }

        public override void OnDestroy()
        {
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
        }
    }
}
