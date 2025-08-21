#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Make room for special vehicles like Ambulances.
    public class ClearPath : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        private RoadSide _roadSide;
        private float _accelerationPercent;
        private bool _checkWaypoint;


        protected override void OnBecomeActive()
        {
            _accelerationPercent = 1;
            base.OnBecomeActive();
            MovementInfo.OnKnownListUpdated += KnownListUpdatedHandler;
            if (_roadSide == RoadSide.Left)
            {
                VehicleComponent.MovementInfo.SetOffset(-1);
            }
            else
            {
                VehicleComponent.MovementInfo.SetOffset(1);
            }
            ((FollowVehicle)API.GetVehicleBehaviourOfType<FollowVehicle>(VehicleIndex)).DisableOvertake(true);
        }


        protected override void OnBecameInactive()
        {
            base.OnBecameInactive();
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
            VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(1);
            VehicleComponent.MovementInfo.ResetOffset();
            ((FollowVehicle)API.GetVehicleBehaviourOfType<FollowVehicle>(VehicleIndex)).DisableOvertake(false);
        }


        public override void SetParams(object[] parameters)
        {
            _roadSide = (RoadSide)parameters[0];
        }


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
                    }
                }
                else
                {
                    if (TrafficWaypointsData.GetWaypointFromIndex(knownWaypointsList.GetWaypointIndex(0)).Name.Contains(UrbanSystem.Internal.UrbanSystemConstants.Connect))
                    {
                        _accelerationPercent = 1;
                    }

                    _accelerationPercent *= 0.9f;
                    if (_accelerationPercent < 0.1f)
                    {
                        _accelerationPercent = 0.1f;
                    }
                    VehicleComponent.MovementInfo.SetMaxSpeedCorrectionPercent(_accelerationPercent);

                }
            }

            return new BehaviourResult();
        }


        private void KnownListUpdatedHandler(int vehicleIndex)
        {
            if (vehicleIndex == VehicleIndex)
            {
                _checkWaypoint = true;
            }
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}