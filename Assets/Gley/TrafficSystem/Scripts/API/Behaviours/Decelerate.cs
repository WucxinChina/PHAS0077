#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Timely slow down where a waypoint with max speed smaller than the current speed in encountered
    public class Decelerate : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), knownWaypointsList.GetFirstSlowDownSpeed(), knownWaypointsList.GetFirstSlowDownPosition(), requiredBrakePower, 0.5f, 0);

            // if slow down waypoint was passed, stop this behaviour
            if (knownWaypointsList.GetFirstSlowDownSpeed() == TrafficSystemConstants.DEFAULT_SPEED)
            {
                Stop();
            }

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}