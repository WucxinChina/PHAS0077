using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Stores all available waypoints for current scene.
    /// </summary>
    public class TrafficWaypointsData : MonoBehaviour
    {
        [SerializeField] private TrafficWaypoint[] _allTrafficWaypoints;


        public TrafficWaypoint[] AllTrafficWaypoints
        {
            get
            {
                return _allTrafficWaypoints;
            }
        }


        public TrafficWaypointsData Initialize()
        {
            WaypointEvents.OnTrafficLightChanged += TrafficLightChangedHandler;
            return this;
        }


        public void SetTrafficWaypoints(TrafficWaypoint[] waypoints)
        {
            _allTrafficWaypoints = waypoints;
        }


        public void AssignZipperGiveWay()
        {
            for (int i = 0; i < _allTrafficWaypoints.Length; i++)
            {
                if (_allTrafficWaypoints[i].ZipperGiveWay)
                {
                    var prevs = _allTrafficWaypoints[i].Prevs;
                    for (int j = 0; j < prevs.Length; j++)
                    {
                        _allTrafficWaypoints[prevs[j]].SetGiveWayValue(true);
                    }
                }
            }
        }


        public bool IsValid(out string error)
        {
            error = string.Empty;
            if (_allTrafficWaypoints == null)
            {
                error = TrafficSystemErrors.NullWaypointData;
                return false;
            }

            if (_allTrafficWaypoints.Length <= 0)
            {
                error = TrafficSystemErrors.NoWaypointsFound;
                return false;
            }

            return true;
        }


        public TrafficWaypoint GetWaypointFromIndex(int waypointIndex)
        {
            if (IsWaypointIndexValid(waypointIndex))
            {
                return _allTrafficWaypoints[waypointIndex];
            }
            return null;
        }


        public List<NeighborStruct> GetNeighborsWithConditions(int waypointIndex, VehicleTypes vehicleType)
        {
            var result = new List<NeighborStruct>();
            var allNeighbors = _allTrafficWaypoints[waypointIndex].Neighbors;
            var allAngles = _allTrafficWaypoints[waypointIndex].Angle;

            for (int i = 0; i < allNeighbors.Length; i++)
            {
                if (_allTrafficWaypoints[allNeighbors[i]].AllowedVehicles.Contains(vehicleType) && !_allTrafficWaypoints[allNeighbors[i]].TemporaryDisabled)
                {
                    result.Add(new NeighborStruct(allNeighbors[i], allAngles[i]));
                }
            }
            return result;
        }


        public List<int> GetOtherLanesWithConditions(int waypointIndex, VehicleTypes vehicleType)
        {
            List<int> result = new List<int>();
            var allNeighbors = _allTrafficWaypoints[waypointIndex].OtherLanes;

            for (int i = 0; i < allNeighbors.Length; i++)
            {
                if (_allTrafficWaypoints[allNeighbors[i]].AllowedVehicles.Contains(vehicleType) && !_allTrafficWaypoints[allNeighbors[i]].TemporaryDisabled)
                {
                    result.Add(allNeighbors[i]);
                }
            }
            return result;
        }


        public int GetOtherLaneWaypointIndex(int currentWaypointIndex, VehicleTypes vehicleType, RoadSide side, Vector3 forwardVector)
        {
            if (_allTrafficWaypoints[currentWaypointIndex].HasOtherLanes)
            {
                List<int> possibleWaypoints = GetOtherLanesWithConditions(currentWaypointIndex, vehicleType);
                if (possibleWaypoints.Count > 0)
                {
                    return GetSideWaypoint(possibleWaypoints, currentWaypointIndex, side, forwardVector);
                }
            }
            return TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        public void SetEventData(int waypointIndex, string data)
        {
            if (IsWaypointIndexValid(waypointIndex))
            {
                if (data != null)
                {
                    _allTrafficWaypoints[waypointIndex].TriggerEvent = true;
                }
                else
                {
                    _allTrafficWaypoints[waypointIndex].TriggerEvent = false;
                }
                _allTrafficWaypoints[waypointIndex].EventData = data;
            }
        }


        public Quaternion GetNextOrientation(TrafficWaypoint waypoint)
        {
            if (!waypoint.HasNeighbors)
            {
                return Quaternion.identity;
            }
            return Quaternion.LookRotation(_allTrafficWaypoints[waypoint.Neighbors[0]].Position - waypoint.Position);
        }

        public Quaternion GetPrevOrientation(TrafficWaypoint waypoint)
        {
            if (!waypoint.HasPrevs)
            {

                return Quaternion.identity;
            }
            return Quaternion.LookRotation(waypoint.Position - _allTrafficWaypoints[waypoint.Prevs[0]].Position);
        }


        private int GetSideWaypoint(List<int> waypointIndexes, int currentWaypointIndex, RoadSide side, Vector3 forwardVector)
        {
            switch (side)
            {
                case RoadSide.Any:
                    return waypointIndexes[Random.Range(0, waypointIndexes.Count)];
                case RoadSide.Left:
                    for (int i = 0; i < waypointIndexes.Count; i++)
                    {
                        if (Vector3.SignedAngle(_allTrafficWaypoints[waypointIndexes[i]].Position - _allTrafficWaypoints[currentWaypointIndex].Position, forwardVector, Vector3.up) > 5)
                        {
                            return waypointIndexes[i];
                        }
                    }
                    break;
                case RoadSide.Right:
                    for (int i = 0; i < waypointIndexes.Count; i++)
                    {
                        if (Vector3.SignedAngle(_allTrafficWaypoints[waypointIndexes[i]].Position - _allTrafficWaypoints[currentWaypointIndex].Position, _allTrafficWaypoints[_allTrafficWaypoints[currentWaypointIndex].Neighbors[0]].Position - _allTrafficWaypoints[currentWaypointIndex].Position, Vector3.up) < -5)
                        {
                            return waypointIndexes[i];
                        }
                    }
                    break;
            }

            return TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        public void AddNeighbor(int waypointIndex, int waypointIndexToAdd)
        {
            if (IsWaypointIndexValid(waypointIndex) && IsWaypointIndexValid(waypointIndexToAdd))
            {
                AllTrafficWaypoints[waypointIndex].AddNeighbor(waypointIndexToAdd);
            }
        }


        public void AddPrevs(int waypointIndex, int waypointIndexToAdd)
        {
            if (IsWaypointIndexValid(waypointIndex) && IsWaypointIndexValid(waypointIndexToAdd))
            {
                AllTrafficWaypoints[waypointIndex].AddPrev(waypointIndexToAdd);
            }
        }


        public void AddOtherLane(int waypointIndex, int waypointIndexToAdd)
        {
            if (IsWaypointIndexValid(waypointIndex) && IsWaypointIndexValid(waypointIndexToAdd))
            {
                AllTrafficWaypoints[waypointIndex].AddOtherLane(waypointIndexToAdd);
            }
        }


        private bool IsWaypointIndexValid(int waypointIndex)
        {
            if (waypointIndex < 0)
            {
                Debug.LogError($"Waypoint index {waypointIndex} should be >= 0");
                return false;
            }

            if (waypointIndex >= _allTrafficWaypoints.Length)
            {
                Debug.LogError($"Waypoint index {waypointIndex} should be < {_allTrafficWaypoints.Length}");
                return false;
            }

            if (_allTrafficWaypoints[waypointIndex] == null)
            {
                Debug.LogError($"Waypoint at {waypointIndex} is null, Verify the setup");
                return false;
            }

            return true;
        }


        private void TrafficLightChangedHandler(int waypointIndex, bool newValue)
        {
            _allTrafficWaypoints[waypointIndex].SetStopValue(newValue);
        }


        private void OnDestroy()
        {
            WaypointEvents.OnTrafficLightChanged -= TrafficLightChangedHandler;
        }
    }
}