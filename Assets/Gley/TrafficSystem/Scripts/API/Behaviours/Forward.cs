#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // The default driving behaviour, always active
    public class Forward : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), knownWaypointsList.GetFirstWaypointSpeed(), TrafficSystemConstants.DEFAULT_POSITION, 0, float.PositiveInfinity, 0);

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}
