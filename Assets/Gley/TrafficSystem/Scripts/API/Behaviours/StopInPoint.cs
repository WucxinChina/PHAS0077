#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Stop at red light or other waypoints with stop property true.
    public class StopInPoint : VehicleBehaviour
    {
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
        }

#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            // if stop position is different from the stop position, do not use those values -> are for other obstacle
            if (!stopPosition.Equals(knownWaypointsList.GetFirstStopPosition()))
            {
                requiredBrakePower = 0;
                stopTargetReached = false;
            }

            var BehaviourResult = new BehaviourResult();

            // if the vehicle is beyond the stop position, completely stop the vehicle
            if (stopTargetReached)
            {
                //BehaviourResult.CompleteStop = true;
                requiredBrakePower = 10;
            }

            // perform regular driving
            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), 0, knownWaypointsList.GetFirstStopPosition(), requiredBrakePower, 0.80f, 0);

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}