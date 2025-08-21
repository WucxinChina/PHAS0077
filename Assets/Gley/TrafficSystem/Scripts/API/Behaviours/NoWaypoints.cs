#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEngine;

namespace Gley.TrafficSystem
{
    // There are no more free waypoints to allow the passage of the current vehicle
    public class NoWaypoints : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            // if the last waypoint was reached, stop
            if (stopTargetReached)
            {
                requiredBrakePower = 10;
            }

            // Perform regular driving
            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), 0, knownWaypointsList.GetLastWaypointPosition(), requiredBrakePower, 1f, 0);

            return BehaviourResult;
        }
#endif


        public override void OnDestroy()
        {

        }
    }
}