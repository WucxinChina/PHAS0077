#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Progressively decelerate
    public class SlowDownAndStop : VehicleBehaviour
    {
        private float _accelerationPercent;

        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            _accelerationPercent = 1;
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
            if (vehicleIndex == VehicleIndex)
            {
                _accelerationPercent *= 0.9f;
                if (_accelerationPercent < 0.1f)
                {
                    _accelerationPercent = 0.1f;
                }
                VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(_accelerationPercent);
            }
        }


        public override void OnDestroy()
        {
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
        }
    }
}