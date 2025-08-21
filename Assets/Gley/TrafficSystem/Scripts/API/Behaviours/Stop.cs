#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Complete stop if active
    public class Stop : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            PerformForwardMovement(ref BehaviourResult, 0, 0, TrafficSystemConstants.DEFAULT_POSITION, 1, 1, 0);

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}