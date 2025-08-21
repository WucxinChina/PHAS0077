#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Put the vehicle in reverse gear 
    public class Reverse : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            BehaviourResult.TargetGear = -1;

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}