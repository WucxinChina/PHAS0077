using Gley.TrafficSystem.Internal;
#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Change the current driving lane if the road has more than one
    public class ChangeLane : VehicleBehaviour
    {

        private RoadSide _roadSide;
#if GLEY_TRAFFIC_SYSTEM
        private bool _checkWaypoint;
#endif
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            MovementInfo.OnKnownListUpdated += KnownListUpdatedHandler;
        }


        protected override void OnBecameInactive()
        {
            base.OnBecameInactive();
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
        }


        public override void SetParams(object[] parameters)
        {
            _roadSide = (RoadSide)parameters[0];
        }

#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            // if waypoint changed perform a check to see if the other lane waypoint is free
            if (_checkWaypoint)
            {
                _checkWaypoint = false;
                int index = TrafficWaypointsData.GetOtherLaneWaypointIndex(knownWaypointsList.GetWaypointIndex(0), VehicleComponent.VehicleType, _roadSide, VehicleComponent.GetForwardVector());
                if (index != TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
                {
                    // if waypoint is free change to other lane and return back to follow
                    if (API.AllPreviousWaypointsAreFree(index, VehicleIndex))
                    {
                        API.AddWaypointAndClear(index, VehicleIndex);
                        Stop();
                    }
                }
            }

            return new BehaviourResult();
        }
#endif


        // Handler for the waypoint changed event
        private void KnownListUpdatedHandler(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (vehicleIndex == VehicleIndex)
            {
                _checkWaypoint = true;
            }
#endif
        }


        public override void OnDestroy()
        {
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
        }
    }
}