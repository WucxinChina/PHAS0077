using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Get path to a destination waypoint.
    /// </summary>
    public class PathFindingManager 
    {
        private readonly GridData _gridData;
        private readonly PathFindingData _trafficPathFindingData;
        private readonly AStar _aStar;


        public PathFindingManager (GridData gridData, PathFindingData trafficPathFindingData)
        {
            _gridData = gridData;
            _trafficPathFindingData = trafficPathFindingData;
            _aStar = new AStar ();
        }


        public List<int> GetPathToDestination(int vehicleIndex, int currentWaypointIndex, Vector3 position, VehicleTypes vehicleType)
        {
            if (currentWaypointIndex < 0)
            {
                Debug.LogWarning($"Cannot find route to destination. Vehicle at index {vehicleIndex} is disabled or has an invalid target waypoint");
                return null;
            }

            int closestWaypointIndex = GetClosestPathFindingWaypoint(position, (int)vehicleType);
            if (closestWaypointIndex < 0)
            {
                Debug.LogWarning("No waypoint found closer to destination");
                return null;
            }

            List<int> path = _aStar.FindPath(currentWaypointIndex, closestWaypointIndex, (int)vehicleType, _trafficPathFindingData.AllPathFindingWaypoints);

            if (path != null)
            {
                return path;
            }

            Debug.LogWarning($"No path found for vehicle {vehicleIndex} to {position}");
            return null;
        }


        public List<int> GetPath(Vector3 startPosition, Vector3 endPosition, VehicleTypes vehicleType)
        {
            var startIndex = GetClosestPathFindingWaypoint(startPosition, (int)vehicleType);
            if(startIndex== TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                Debug.LogWarning($"No traffic waypoint found close to {startPosition}");
                return null;
            }

            var endIndex = GetClosestPathFindingWaypoint(endPosition, (int)vehicleType);
            if(endIndex == TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                Debug.LogWarning($"No traffic waypoint found closed to {endPosition}");
                return null;
            }

            var path = _aStar.FindPath(startIndex, endIndex, (int)vehicleType, _trafficPathFindingData.AllPathFindingWaypoints);
            if (path == null)
            {
                Debug.LogWarning($"No path found from {startPosition} to {endPosition}");
            }
            return path;
        }


        private int GetClosestPathFindingWaypoint(Vector3 position, int type)
        {
            List<int> possibleWaypoints = _gridData.GetTrafficWaypointsAroundPosition(position);
            if (possibleWaypoints.Count == 0)
            {
                return TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
            }


            float distance = float.MaxValue;
            int resultWaypointIndex = TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
            foreach (int waypointIndex in possibleWaypoints)
            {
                if (_trafficPathFindingData.GetAllowedAgents(waypointIndex).Contains(type))
                {
                    float newDistance = Vector3.SqrMagnitude(_trafficPathFindingData.AllPathFindingWaypoints[waypointIndex].WorldPosition - position);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        resultWaypointIndex = waypointIndex;
                    }
                }
            }
            return resultWaypointIndex;
        }
    }
}