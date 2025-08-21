#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEngine;

namespace Gley.TrafficSystem
{
    // Overtake a slowly moving player.
    public class OvertakePlayer : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        private const float _minOvertakeSpeed = 2;

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

#if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            // is speed is close to 0, do a complete stop and switch back the state into follow. Do not overtake stationary cars.
            var followSpeed = knownWaypointsList.GetFollowSpeed();
            if (followSpeed < _minOvertakeSpeed)
            {
                followSpeed = 0;
                Stop();
                API.StartVehicleBehaviour<FollowPlayer>(VehicleIndex);
            }

            // if the vehicle is to close from the front one, brake harder and reduce the speed to avoid a crash
            if (stopTargetReached)
            {
                if (requiredBrakePower < 1)
                {
                    requiredBrakePower = 1;
                }
            }

            // Perform regular driving
            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), followSpeed, knownWaypointsList.ClosestObstaclePoint, requiredBrakePower, 0.5f, VehicleComponent.distanceToStop);

            // if waypoint changed perform a check to see if the other lane waypoint is free
            if (_checkWaypoint)
            {
                _checkWaypoint = false;
                var waypoint = TrafficWaypointsData.GetWaypointFromIndex(knownWaypointsList.GetWaypointIndex(0));
                if (waypoint.OtherLanes.Length > 0)
                {
                    int index = GetOvertakeIndex(waypoint.OtherLanes, VehicleComponent.VehicleType);
                    if (index != TrafficSystemConstants.INVALID_VEHICLE_INDEX)
                    {
                        Blink(index);
                        // if waypoint is free change to other lane and return back to follow
                        if (API.AllPreviousWaypointsAreFree(index, VehicleIndex))
                        {
                            API.AddWaypointAndClear(index, VehicleIndex);
                            Stop();
                            API.StartVehicleBehaviour<FollowPlayer>(VehicleIndex);
                        }
                    }
                }
            }

            return BehaviourResult;
        }
#endif

        private void Blink(int index)
        {
            float angle = Vector3.SignedAngle(VehicleComponent.GetForwardVector(), TrafficWaypointsData.GetWaypointFromIndex(index).Position - VehicleComponent.transform.position, Vector3.up);
            if (angle > 5)
            {
                VehicleComponent.SetBlinker(UrbanSystem.Internal.BlinkType.Right);
            }
            if (angle < -5)
            {
                VehicleComponent.SetBlinker(UrbanSystem.Internal.BlinkType.Left);
            }
        }


        private int GetOvertakeIndex(int[] otherLanes, VehicleTypes vehicleType)
        {
            float maxSpeed = 0;
            int result = TrafficSystemConstants.INVALID_WAYPOINT_INDEX;

            for (int i = 0; i < otherLanes.Length; i++)
            {
                var waypoint = TrafficWaypointsData.GetWaypointFromIndex(otherLanes[i]);

                bool isAllowed = false;
                foreach (var allowedType in waypoint.AllowedVehicles)
                {
                    if (allowedType == vehicleType)
                    {
                        isAllowed = true;
                        break;
                    }
                }

                if (!isAllowed)
                {
                    continue;
                }

                if (waypoint.MaxSpeed > maxSpeed)
                {
                    maxSpeed = waypoint.MaxSpeed;
                    result = otherLanes[i];
                }
            }
            return result;
        }


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