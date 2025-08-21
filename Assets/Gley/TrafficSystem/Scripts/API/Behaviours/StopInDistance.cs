#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEngine;


namespace Gley.TrafficSystem
{
    // Stop and wait for the dynamic obstacle to pass
    public class StopInDistance : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            // if stop position is different from the obstacle position, do not use those values -> are for other obstacle
            if (math.lengthsq(stopPosition-knownWaypointsList.ClosestObstaclePoint)>1f)
            {
                requiredBrakePower = 0;
                stopTargetReached = false;
            }

            var BehaviourResult = new BehaviourResult();

            // if the vehicle is beyond the obstacle position, completely stop the vehicle
            if (stopTargetReached)
            {
                //BehaviourResult.CompleteStop = true;
                requiredBrakePower = 10;
            }

            // Perform regular driving
            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), 0, knownWaypointsList.ClosestObstaclePoint, requiredBrakePower, 0.95f, VehicleComponent.distanceToStop);

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}