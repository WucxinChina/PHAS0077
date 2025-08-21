#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Stop and check if the path is free for the current vehicle to continue
    public class GiveWay : VehicleBehaviour
    {
        public delegate void PassageGranted(int vehicleIndex, int waypointIndex);
        public static PassageGranted OnPassageGranted;
        public static void TriggerPassageGrantedEvent(int vehicleIndex, int waypointIndex)
        {
            OnPassageGranted?.Invoke(vehicleIndex, waypointIndex);
        }

        #if GLEY_TRAFFIC_SYSTEM
        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            // if stop position is different from the give way position, do not use those values -> are for other obstacle
            if(!stopPosition.Equals(knownWaypointsList.GetFirstGiveWayPosition()))
            {
                requiredBrakePower = 0;
                stopTargetReached = false;
            }

            var BehaviourResult = new BehaviourResult();

            // if the vehicle is beyond the give way position, completely stop the vehicle
            if (stopTargetReached)
            {
                //BehaviourResult.CompleteStop = true;
                requiredBrakePower = 1;
            }

            // regular driving
            PerformForwardMovement(ref BehaviourResult, knownWaypointsList.GetFirstWaypointSpeed(), 0, knownWaypointsList.GetFirstGiveWayPosition(), requiredBrakePower, 0.80f, 0);


            // check if the passage is free
            if (knownWaypointsList.IsFirstWaypointGiveWay())
            {
                VehicleComponent.BlinkersController.UpdateBlinkers(UrbanSystem.Internal.BlinkType.Start);
                var giveWayState = knownWaypointsList.GetGiveWayType(0);

                // do some checks to determine is the current give way waypoint is zipper or not.
                if (giveWayState == GiveWayType.Standard)
                {
                    var nextState = knownWaypointsList.GetGiveWayType(1);
                    if (nextState == GiveWayType.Zipper)
                    {
                        giveWayState = GiveWayType.Zipper;
                    }
                }
                else
                {
                    if (giveWayState == GiveWayType.Zipper)
                    {
                        giveWayState = GiveWayType.None;
                    }
                }

                // check if the passage is free for the current vehicle
                switch (giveWayState)
                {
                    case GiveWayType.Intersection:
                        if (CanEnterIntersection(knownWaypointsList.GetWaypointIndex(0)))
                        {
                            TriggerPassageGrantedEvent(VehicleIndex, knownWaypointsList.GetWaypointIndex(0));
                        }
                        break;

                    case GiveWayType.Complex:
                        if (API.AllRequiredWaypointsAreFree(knownWaypointsList.GetWaypointIndex(0), VehicleIndex))
                        {
                            TriggerPassageGrantedEvent(VehicleIndex, knownWaypointsList.GetWaypointIndex(0));
                        }
                        break;

                    case GiveWayType.Zipper:
                        if (!API.IsThisWaypointADestination(knownWaypointsList.GetWaypointIndex(1), VehicleIndex))
                        {
                            TriggerPassageGrantedEvent(VehicleIndex, knownWaypointsList.GetWaypointIndex(0));
                            TriggerPassageGrantedEvent(VehicleIndex, knownWaypointsList.GetWaypointIndex(0));
                        }
                        break;

                    case GiveWayType.Standard:
                        if (API.AllPreviousWaypointsAreFree(knownWaypointsList.GetWaypointIndex(1), VehicleIndex))
                        {
                            TriggerPassageGrantedEvent(VehicleIndex, knownWaypointsList.GetWaypointIndex(0));
                        }
                        break;

                    case GiveWayType.None:
                        TriggerPassageGrantedEvent(VehicleIndex, knownWaypointsList.GetWaypointIndex(0));
                        break;
                }
            }

            return BehaviourResult;
        }
#endif

        /// <summary>
        /// Check if can switch to target waypoint
        /// </summary>
        /// <param name="waypointIndex"></param>
        /// <returns></returns>
        private bool CanEnterIntersection(int waypointIndex)
        {
            var intersections = TrafficWaypointsData.AllTrafficWaypoints[waypointIndex].AssociatedIntersections;
            foreach (var intersection in intersections)
            {
                if (intersection.IsPathFree(waypointIndex) == false)
                {
                    return false;
                }
            }
            return true;
        }


        public override void OnDestroy()
        {

        }


    }
}
