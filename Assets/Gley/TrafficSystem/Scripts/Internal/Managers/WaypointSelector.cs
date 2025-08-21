using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    public class WaypointSelector : IDestroyable
    {
        private readonly GridData _gridData;
        private readonly TrafficWaypointsData _trafficWaypointsData;

        private SpawnWaypointSelector _spawnWaypointSelector;

        public WaypointSelector(GridData gridData, SpawnWaypointSelector spawnWaypointSelector, TrafficWaypointsData trafficWaypointsData)
        {
            Assign();
            _gridData = gridData;
            _spawnWaypointSelector = spawnWaypointSelector;
            _trafficWaypointsData = trafficWaypointsData;
        }


        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        /// <summary>
        /// Set the default waypoint generating method
        /// </summary>
        /// <param name="spawnWaypointSelector"></param>
        public void SetSpawnWaypointSelector(SpawnWaypointSelector spawnWaypointSelector)
        {
            _spawnWaypointSelector = spawnWaypointSelector;
        }


        public TrafficWaypoint GetAFreeWaypoint(Vector3 cameraPosition, int depth, VehicleTypes carType, Vector3 playerPosition, Vector3 playerDirection, bool useWaypointPriority)
        {
            var cell = _gridData.GetCell(cameraPosition);
            //get all cell neighbors for the specified depth
            List<Vector2Int> neighbors = _gridData.GetCellNeighbors(cell.CellProperties.Row, cell.CellProperties.Column, depth, false);

            for (int i = neighbors.Count - 1; i >= 0; i--)
            {
                if (!_gridData.HasTrafficSpawnWaypoints(neighbors[i]))
                {
                    neighbors.RemoveAt(i);
                }
            }

            //if neighbors exists
            if (neighbors.Count > 0)
            {
                var waypointIndex = ApplyNeighborSelectorMethod(neighbors, playerPosition, playerDirection, carType, useWaypointPriority);
                if (waypointIndex != TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
                {
                    return _trafficWaypointsData.GetWaypointFromIndex(waypointIndex);
                }
            }

            return null;
        }


        private int ApplyNeighborSelectorMethod(List<Vector2Int> neighbors, Vector3 playerPosition, Vector3 playerDirection, VehicleTypes carType, bool useWaypointPriority)
        {
            try
            {
                return _spawnWaypointSelector(neighbors, playerPosition, playerDirection, carType, useWaypointPriority);
            }
            catch (System.Exception e)
            {
                Debug.LogError(TrafficSystemErrors.NoNeighborSelectorMethod(e.Message));
                return DefaultDelegates.GetRandomSpawnWaypoint(neighbors, playerPosition, playerDirection, carType, useWaypointPriority);
            }
        }


        public List<int> GetAreaWaypoints(Area area)
        {
            var result = new List<int>();
            CellData cell = _gridData.GetCell(area.center);
            List<Vector2Int> neighbors = _gridData.GetCellNeighbors(cell.CellProperties.Row, cell.CellProperties.Column, Mathf.CeilToInt(area.radius * 2 / _gridData.GridCellSize), false);

            for (int i = neighbors.Count - 1; i >= 0; i--)
            {
                cell = _gridData.GetCell(neighbors[i]);
                for (int j = 0; j < cell.TrafficWaypointsData.Waypoints.Count; j++)
                {
                    int waypointIndex = cell.TrafficWaypointsData.Waypoints[j];
                    if (Vector3.SqrMagnitude(area.center - _trafficWaypointsData.AllTrafficWaypoints[waypointIndex].Position) < area.sqrRadius)
                    {
                        result.Add(waypointIndex);
                    }
                }
            }

            return result;
        }


        public TrafficWaypoint GetClosestSpawnWaypoint(Vector3 position, VehicleTypes type)
        {
            List<SpawnWaypoint> possibleWaypoints = _gridData.GetTrafficSpawnWaypoipointsAroundPosition(position, (int)type);

            if (possibleWaypoints.Count == 0)
            {
                return null;
            }

            float distance = float.MaxValue;
            int waypointIndex = -1;
            for (int i = 0; i < possibleWaypoints.Count; i++)
            {
                float newDistance = Vector3.SqrMagnitude(_trafficWaypointsData.AllTrafficWaypoints[possibleWaypoints[i].WaypointIndex].Position - position);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    waypointIndex = possibleWaypoints[i].WaypointIndex;
                }
            }
            if (waypointIndex != TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                return _trafficWaypointsData.AllTrafficWaypoints[waypointIndex];
            }
            return null;
        }


        public TrafficWaypoint GetClosestWaypoint(Vector3 position)
        {
            var cell = _gridData.GetCell(position);
            var cellNeighbors = _gridData.GetCellNeighbors(cell.CellProperties.Row, cell.CellProperties.Column, 1, false);
            var allWaypoints = new List<TrafficWaypoint>();
            for (int i = 0; i < cellNeighbors.Count; i++)
            {
                List<int> cellWaypoints = _gridData.GetAllTrafficWaypointsInCell(cellNeighbors[i]);
                for (int j = 0; j < cellWaypoints.Count; j++)
                {
                    allWaypoints.Add(_trafficWaypointsData.AllTrafficWaypoints[cellWaypoints[j]]);
                }
            }

            TrafficWaypoint waypointIndex = null;
            float oldDistance = Mathf.Infinity;
            for (int i = 0; i < allWaypoints.Count; i++)
            {
                float newDistance = Vector3.SqrMagnitude(position - allWaypoints[i].Position);
                if (newDistance < oldDistance)
                {
                    waypointIndex = allWaypoints[i];
                    oldDistance = newDistance;
                }
            }
            if (waypointIndex == null)
            {
                Debug.LogWarning($"No valid waypoint found for position {position}");
            }

            return waypointIndex;
        }

        public TrafficWaypoint GetClosestWaypointInDirection(Vector3 position, Vector3 direction)
        {
            var cell = _gridData.GetCell(position);
            var cellNeighbors = _gridData.GetCellNeighbors(cell.CellProperties.Row, cell.CellProperties.Column, 1, false);
            var allWaypoints = new List<TrafficWaypoint>();
            for (int i = 0; i < cellNeighbors.Count; i++)
            {
                List<int> cellWaypoints = _gridData.GetAllTrafficWaypointsInCell(cellNeighbors[i]);
                for (int j = 0; j < cellWaypoints.Count; j++)
                {
                    allWaypoints.Add(_trafficWaypointsData.AllTrafficWaypoints[cellWaypoints[j]]);
                }
            }

            TrafficWaypoint waypointIndex = null;
            float oldDistance = Mathf.Infinity;
            for (int i = 0; i < allWaypoints.Count; i++)
            {
                float newDistance = Vector3.SqrMagnitude(position - allWaypoints[i].Position);
                if (newDistance < oldDistance)
                {
                    if (CheckOrientation(allWaypoints[i], direction))
                    {
                        waypointIndex = allWaypoints[i];
                        oldDistance = newDistance;
                    }
                }
            }
            if (waypointIndex == null)
            {
                Debug.LogWarning($"No valid waypoint found for position {position}");
            }

            return waypointIndex;
        }


        private bool CheckOrientation(TrafficWaypoint waypoint, Vector3 direction)
        {
            if (waypoint.Neighbors.Length < 1)
                return false;

            TrafficWaypoint neighbor = _trafficWaypointsData.AllTrafficWaypoints[waypoint.Neighbors[0]];
            float angle = Vector3.SignedAngle(direction, neighbor.Position - waypoint.Position, Vector3.up);
            if (Mathf.Abs(angle) < 90)
            {
                return true;
            }
            return false;
        }

        public void OnDestroy()
        {
            
        }
    }
}