#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Reverse in the opposite direction if a static obstacle is encountered.
    public class AvoidReverse : VehicleBehaviour
    {
        #if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            // Set gear to reverse
            BehaviourResult.TargetGear = -1;

            // If vehicle is reversing set the steer to maximum in opposite direction
            if (currentGear == BehaviourResult.TargetGear)
            {
                if (VehicleComponent.frontTrigger.transform.localEulerAngles.y > 0)
                {
                    Steer(ref BehaviourResult, -1);
                }
                else
                {
                    Steer(ref BehaviourResult, 1);
                }
            }

            // If brake is required, hard brake
            if (requiredBrakePower > 0.1f && requiredBrakePower < 1)
            {
                requiredBrakePower = 1;
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