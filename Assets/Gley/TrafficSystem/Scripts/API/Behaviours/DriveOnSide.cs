#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Drive on the edge of the lane
    public class DriveOnSide : VehicleBehaviour
    {
        protected override void OnBecameInactive()
        {
            base.OnBecameInactive();
            VehicleComponent.MovementInfo.ResetOffset();
        }


        public override void SetParams(object[] parameters)
        {
            base.SetParams(parameters);
            var _roadSide = (RoadSide)parameters[0];
            switch (_roadSide)
            {
                case RoadSide.Left:
                    VehicleComponent.MovementInfo.SetOffset(-1);
                    break;
                case RoadSide.Right:
                    VehicleComponent.MovementInfo.SetOffset(1);
                    break;

            }
        }

#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            return new BehaviourResult();
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}
