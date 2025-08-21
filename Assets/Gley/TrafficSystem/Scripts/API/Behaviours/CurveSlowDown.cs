#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Slow down if the next waypoint has a big angle
    public class CurveSlowDown : VehicleBehaviour
    {
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            MovementInfo.OnKnownListUpdated += KnownListUpdatedHandler;
        }


        protected override void OnBecameInactive()
        {
            base.OnBecameInactive();
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
            VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(1);
        }

#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            return new BehaviourResult();
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
                        VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(1 - angle / 25f);
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