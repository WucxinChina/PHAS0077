using System.Collections.Generic;

using UnityEngine;
#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    public class MovementInfo
    {
        private Queue<int> _customPath;
        private List<int> _waypointIndexes;
        private List<Vector3> _positions;
        private List<float> _maxSpeed;
        private List<int> _angle;
        private List<bool> _stop;
        private List<GiveWayType> _giveWay;
        private List<bool> _slowDown;
        private List<Obstacle> _obstacles;
        private Queue<int> _coveredWaypoints;
        private int _activePosition;
        private int _associatedVehicle;
        private float _maxVehicleSpeed;
        private Vector3 _closestObstaclePoint;
        private Obstacle _closestObstacle;
        private float _followSpeed;
        private float _associatedVehicleWidth;
        private float _offset;
        private float _originalOffset;
        private float _correctionPercent;
        private bool _hasPath;
        private bool _ignoreRuls;
        private bool _dontRemove = true;

        public Vector3 OldPosition { get; private set; }
        public int OldWaypointIndex { get; private set; }
        public bool NoWaypoints { get; set; }
        public bool HasPath => _hasPath;
        public bool DontRemove => _dontRemove;
        public Queue<int> CustomPath => _customPath;
        public List<int> WaypointIndexes => _waypointIndexes;
        public Queue<int> CoveredWaypoints => _coveredWaypoints;

        public int PathLength
        {
            get
            {
                return _positions.Count;
            }
        }

        public int RemainingPathLength
        {
            get
            {
                return _positions.Count - _activePosition;
            }
        }
#if GLEY_TRAFFIC_SYSTEM
        public float3 ClosestObstaclePoint => _closestObstaclePoint;
#endif
        public Obstacle ClosestObstacle => _closestObstacle;
        public float FollowSpeed => _followSpeed;

        public delegate void KnownListUpdated(int vehicleIndex);
        public static event KnownListUpdated OnKnownListUpdated;
        public static void TriggerKnownListUpdatedEvent(int vehicleIndex)
        {
            OnKnownListUpdated?.Invoke(vehicleIndex);
        }


        public delegate void StopWaypointsUpdated(int vehicleIndex);
        public static event StopWaypointsUpdated OnStopWaypointsUpdated;
        public static void TriggerStopWaypointsUpdatedEvent(int vehicleIndex)
        {
            OnStopWaypointsUpdated?.Invoke(vehicleIndex);
        }


        public delegate void SlowDownWaypointsUpdated(int vehicleIndex);
        public static event SlowDownWaypointsUpdated OnSlowDownWaypointsUpdated;
        public static void TriggerSlowDownWaypointsUpdatedEvent(int vehicleIndex)
        {
            OnSlowDownWaypointsUpdated?.Invoke(vehicleIndex);
        }


        public delegate void GiveWayWaypointsUpdated(int vehicleIndex);
        public static event GiveWayWaypointsUpdated OnGiveWayWaypointsUpdated;
        public static void TriggerGiveWayWaypointsUpdatedEvent(int vehicleIndex)
        {
            OnGiveWayWaypointsUpdated?.Invoke(vehicleIndex);
        }

        public MovementInfo(int associatedVehicle, float associatedVehicleWidth, float originalOffset)
        {
            _waypointIndexes = new List<int>();
            _coveredWaypoints = new Queue<int>();
            _positions = new List<Vector3>();
            _maxSpeed = new List<float>();
            _angle = new List<int>();
            _slowDown = new List<bool>();
            _stop = new List<bool>();
            _giveWay = new List<GiveWayType>();
            _obstacles = new List<Obstacle>();
            _activePosition = 0;
            _associatedVehicle = associatedVehicle;
            _closestObstaclePoint = TrafficSystemConstants.DEFAULT_POSITION;
            _followSpeed = TrafficSystemConstants.DEFAULT_SPEED;
            _associatedVehicleWidth = associatedVehicleWidth / 2;
            _originalOffset = originalOffset;
            SetOffset(originalOffset);
            SetMaxSpeedCorrectionPercent(1);
            //SetOffset(1);
        }


        public int GetWaypointIndex(int pathPosition)
        {
            if (IsPathPositionValid(pathPosition))
            {
                return _waypointIndexes[pathPosition];
            }
            return TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        public int GetCurrentWaypointIndex()
        {
            return GetWaypointIndex(0);
        }

        public bool IsVehicleOnThisWaypoint(int waypointIndex)
        {
            if (_coveredWaypoints.Contains(waypointIndex))
            {
                return true;
            }
            return false;
        }


        public int GetAngle(int pathPosition)
        {
            if (IsPathPositionValid(pathPosition))
            {
                return _angle[pathPosition];
            }
            return 0;
        }


        public Vector3 GetFirstPosition()
        {
            return GetPosition(_activePosition);
        }

        public void SetIgnoreRules(bool ignoreRules)
        {
            _ignoreRuls = ignoreRules;
        }

        public void DontRemoveVehicle(bool dontRemove)
        {
            _dontRemove = dontRemove;
        }


        public void SetOffset(float offsetPercent)
        {
            _offset = Mathf.Clamp(offsetPercent, -1, 1);
        }


        public void ResetOffset()
        {
            _offset = _originalOffset;
        }


        public void SetMaxSpeedCorrectionPercent(float correctionPercent)
        {
            _correctionPercent = correctionPercent;
        }


        private Vector3 GetPositionOffset(int waypointIndex)
        {
            var waypoint = API.GetWaypointFromIndex(waypointIndex);

            return _offset * (waypoint.LaneWidth / 2 - _associatedVehicleWidth) * waypoint.LeftDirection;
        }


        public Vector3 GetPosition(int pathPosition)
        {
            if (IsPathPositionValid(pathPosition))
            {
                if (_offset == 0)
                {
                    return _positions[pathPosition];
                }
                else
                {
                    return _positions[pathPosition] + GetPositionOffset(GetWaypointIndex(pathPosition));
                }
            }
            return TrafficSystemConstants.DEFAULT_POSITION;
        }


        public float GetFirstWaypointSpeed()
        {
            return GetWaypointSpeed(_activePosition);
        }


        public float GetFollowSpeed()
        {
            return _followSpeed;
        }


        public float GetWaypointSpeed(int pathPosition)
        {
            if (IsPathPositionValid(pathPosition))
            {
                return _maxSpeed[pathPosition] * _correctionPercent;
            }
            return TrafficSystemConstants.DEFAULT_SPEED;
        }


        public bool IsStopWaypoint(int pathPosition)
        {
            if (IsPathPositionValid(pathPosition))
            {
                return _stop[pathPosition];
            }
            return false;
        }


        public bool IsGiveWayWaypoint(int pathPosition)
        {
            if (IsPathPositionValid(pathPosition))
            {
                return _giveWay[pathPosition] != GiveWayType.None;
            }
            return false;
        }


        public GiveWayType GetGiveWayType(int pathPosition)
        {
            return _giveWay[pathPosition];
        }


        public bool HasStopWaypoints()
        {
            for (int i = 0; i < _stop.Count; i++)
            {
                if (_stop[i] == true)
                {
                    return true;
                }
            }
            return false;
        }


        public bool HasGiveWayWaypoints()
        {
            for (int i = 0; i < _giveWay.Count; i++)
            {
                if (_giveWay[i] != GiveWayType.None)
                {
                    return true;
                }
            }
            return false;
        }


        public bool HasObstacles()
        {
            return _obstacles.Count != 0;
        }


        public bool HasSlowDownWaypoints()
        {
            for (int i = 0; i < _slowDown.Count; i++)
            {
                if (_slowDown[i] == true)
                {
                    return true;
                }
            }
            return false;
        }


        public void SetObstacles(List<Obstacle> obstacles, Vector3 targetPosition)
        {
            _obstacles = obstacles;
            SetClosestObstacleAndSpeed(targetPosition);
            Events.TriggerObstaclesUpdatedEvent(_associatedVehicle);
        }


        public void RemoveObstacle(ObstacleTypes typeToRemove)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                if (_obstacles[i].ObstacleType == typeToRemove)
                {
                    return;
                }
            }
            Events.TriggerObstacleRemovedEvent(_associatedVehicle, typeToRemove);
        }


        public void SetStopState(int pathPosition, bool stopState)
        {
            if (IsPathPositionValid(pathPosition))
            {
                if (_stop[pathPosition] != stopState)
                {
                    _stop[pathPosition] = stopState;
                    TriggerStopWaypointsUpdatedEvent(_associatedVehicle);
                }
            }
        }


        public void SetGiveWayState(int pathPosition, GiveWayType giveWayType)
        {
            if (IsPathPositionValid(pathPosition))
            {
                if (_giveWay[pathPosition] != giveWayType)
                {
                    _giveWay[pathPosition] = giveWayType;
                    TriggerGiveWayWaypointsUpdatedEvent(_associatedVehicle);
                }
            }
        }


        public bool TrySetStopWaypoint(int waypointIndex, bool stopState)
        {
            for (int i = 0; i < _waypointIndexes.Count; i++)
            {
                if (waypointIndex == _waypointIndexes[i])
                {
                    SetStopState(i, stopState);
                    return true;
                }
            }
            return false;
        }


        public bool TrySetGiveWayWaypoint(int waypointIndex, GiveWayType giveWayType)
        {
            for (int i = 0; i < _waypointIndexes.Count; i++)
            {
                if (waypointIndex == _waypointIndexes[i])
                {
                    SetGiveWayState(i, giveWayType);
                    return true;
                }
            }
            return false;
        }


        public void AddOtherLaneWaypoint(int waypointIndex, Vector3 waypointPosition, float waypointSpeed, bool stop, GiveWayType giveWayType, int angle)
        {
            for (int i = _waypointIndexes.Count - 1; i >= 0; i--)
            {
                RemoveWaypointFromTarget(_waypointIndexes[i]);
            }
            _activePosition = 0;
            TriggerGiveWayWaypointsUpdatedEvent(_associatedVehicle);
            TriggerStopWaypointsUpdatedEvent(_associatedVehicle);
            AddWaypointAsTarget(waypointIndex, waypointPosition, waypointSpeed, stop, giveWayType, angle);
        }


        public void SetPath(List<int> pathWaypoints)
        {
            _hasPath = true;
            for (int i = _waypointIndexes.Count - 1; i >= 0; i--)
            {
                RemoveWaypointFromTarget(_waypointIndexes[i]);
            }
            _customPath = new Queue<int>(pathWaypoints);
            _activePosition = 0;
        }


        public void RemovePath()
        {
            _hasPath = false;
            if (_customPath != null)
            {
                _customPath.Clear();
            }
        }


        public void AddWaypointAsTarget(int waypointIndex, Vector3 waypointPosition, float waypointSpeed, bool stop, GiveWayType giveWayType, int angle)
        {
            if (waypointIndex < 0)
            {
                Debug.LogError($"Cannot add {waypointIndex} as target");
                return;
            }
            _waypointIndexes.Add(waypointIndex);
            _positions.Add(waypointPosition);

            waypointSpeed = Mathf.Min(waypointSpeed, _maxVehicleSpeed);
            if (_maxSpeed.Count == 0 || _maxSpeed[_maxSpeed.Count - 1] <= waypointSpeed)
            {
                _slowDown.Add(false);
            }
            else
            {
                _slowDown.Add(true);
                TriggerSlowDownWaypointsUpdatedEvent(_associatedVehicle);
            }

            _angle.Add(angle);
            _maxSpeed.Add(waypointSpeed.KMHToMS());
            _stop.Add(stop);
            _giveWay.Add(giveWayType);

            TriggerKnownListUpdatedEvent(_associatedVehicle);
            if (stop == true)
            {
                MovementInfo.TriggerStopWaypointsUpdatedEvent(_associatedVehicle);
            }
            if (giveWayType != GiveWayType.None)
            {
                MovementInfo.TriggerGiveWayWaypointsUpdatedEvent(_associatedVehicle);
            }
        }


        public void RemoveWaypointFromTarget(int waypointIndex)
        {
            int index = _waypointIndexes.IndexOf(waypointIndex);
            if (index >= 0)
            {
                _waypointIndexes.RemoveAt(index);
                _positions.RemoveAt(index);
                _maxSpeed.RemoveAt(index);
                _slowDown.RemoveAt(index);
                _stop.RemoveAt(index);
                _giveWay.RemoveAt(index);
                _angle.RemoveAt(index);
            }
        }


        public int GetLastWaypointIndex()
        {
            if (_waypointIndexes.Count == 0)
            {
                return TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
            }
            return _waypointIndexes[_waypointIndexes.Count - 1];
        }


        public Vector3 GetLastWaypointPosition()
        {
            if (_positions.Count == 0)
            {
                return TrafficSystemConstants.DEFAULT_POSITION;
            }
            return GetPosition(_positions.Count - 1);
        }


        public bool IsWaypointATarget(int waypointIndex)
        {
            return _waypointIndexes.Contains(waypointIndex);
        }


        public bool WaypointCanBeReached(int targetWaypointIndex)
        {
            var targetFound = false;
            for (int i = 0; i < _waypointIndexes.Count; i++)
            {
                if (_waypointIndexes[i] == targetWaypointIndex)
                {
                    return true;
                }
            }
            return targetFound;
        }


        public void TargetPassed()
        {
            if (_waypointIndexes.Count <= 0)
            {
                return;
            }

            //is just a position in the list
            if (_waypointIndexes[0] >= 0)
            {
                OldWaypointIndex = _waypointIndexes[0];
                _coveredWaypoints.Enqueue(OldWaypointIndex);
            }
            OldPosition = _positions[0];
            _waypointIndexes.RemoveAt(0);
            _positions.RemoveAt(0);
            _maxSpeed.RemoveAt(0);
            _slowDown.RemoveAt(0);
            _stop.RemoveAt(0);
            _giveWay.RemoveAt(0);
            _angle.RemoveAt(0);
            _activePosition--;
            TriggerKnownListUpdatedEvent(_associatedVehicle);
        }


        public void IncreaseActivePosition()
        {
            if (PathLength - 1 > _activePosition)
            {
                _activePosition++;
            }

            TriggerKnownListUpdatedEvent(_associatedVehicle);
        }


        public void ClearTarget(float maxVehicleSpeed)
        {
            _waypointIndexes.Clear();
            _positions.Clear();
            _maxSpeed.Clear();
            _slowDown.Clear();
            _stop.Clear();
            _giveWay.Clear();
            _angle.Clear();
            OldWaypointIndex = TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
            OldPosition = default;
            _maxVehicleSpeed = maxVehicleSpeed.ToKMH();
            _obstacles.Clear();
            _activePosition = 0;
            _coveredWaypoints.Clear();
            RemovePath();
            //TriggerKnownListUpdatedEvent(_associatedVehicle);
        }


        private bool IsPathPositionValid(int pathPosition)
        {
            if (pathPosition < 0)
            {
                //Debug.LogError($"Path position {pathPosition} should be >= 0");
                return false;
            }
            if (pathPosition >= _waypointIndexes.Count)
            {
                //Debug.LogError($"Path position {pathPosition} should be < {_waypointIndices.Count}");
                return false;
            }
            return true;
        }


        public void PrintWaypoints()
        {
            string text = string.Empty;
            for (int i = 0; i < _waypointIndexes.Count; i++)
            {
                text += $"{_waypointIndexes[i]} {_positions[i]} \n";
            }
            Debug.Log(text);

        }


        public Vector3 GetFirstStopPosition()
        {
            for (int i = 0; i < _stop.Count; i++)
            {
                if (_stop[i] == true)
                {
                    return GetPosition(i);
                }
            }
            return TrafficSystemConstants.DEFAULT_POSITION;
        }


        public Vector3 GetFirstGiveWayPosition()
        {
            for (int i = 0; i < _giveWay.Count; i++)
            {
                if (_giveWay[i] != GiveWayType.None)
                {
                    return GetPosition(i);
                }
            }
            return TrafficSystemConstants.DEFAULT_POSITION;
        }


        public void SetClosestObstacleAndSpeed(Vector3 targetPosition)
        {
            if (HasObstacles())
            {
                var index = 0;
                var position = TrafficSystemConstants.DEFAULT_POSITION;
                var minDistance = float.MaxValue;
                for (int i = 0; i < _obstacles.Count; i++)
                {
                    Vector3 closestPoint;
                    if (_obstacles[i].IsConvex)
                    {
                        closestPoint = _obstacles[i].Collider.ClosestPoint(targetPosition);
                    }
                    else
                    {
                        closestPoint = _obstacles[i].Collider.bounds.ClosestPoint(targetPosition);
                    }
                    float distance = Vector3.SqrMagnitude(closestPoint - targetPosition);
                    if (Vector3.SqrMagnitude(closestPoint - targetPosition) < minDistance)
                    {
                        position = closestPoint;
                        minDistance = distance;
                        index = i;
                    }
                }

                _closestObstaclePoint = position;
                _closestObstacle = _obstacles[index];
                if (_closestObstacle.ObstacleType == ObstacleTypes.Player || _closestObstacle.ObstacleType == ObstacleTypes.TrafficVehicle)
                {
                    if (_closestObstacle.VehicleScript != null)
                    {
                        _followSpeed = _closestObstacle.VehicleScript.GetCurrentSpeedMS();
                    }
                    else
                    {
                        Debug.LogWarning($"{_closestObstacle.Collider.name} is a vehicle but does not implement the ITrafficParticipant interface. If this is your player, attach the PlayerComponent or your own implementation of the interface.", _closestObstacle.Collider);
                        _followSpeed = TrafficSystemConstants.DEFAULT_SPEED;
                    }
                }
                else
                {
                    _followSpeed = TrafficSystemConstants.DEFAULT_SPEED;
                }
                return;
            }

            _closestObstacle = default;
            _closestObstaclePoint = TrafficSystemConstants.DEFAULT_POSITION;
            _followSpeed = TrafficSystemConstants.DEFAULT_SPEED;
        }


        public Vector3 GetFirstSlowDownPosition()
        {
            for (int i = 0; i < _slowDown.Count; i++)
            {
                if (_slowDown[i] == true)
                {
                    return GetPosition(i);
                }
            }
            return TrafficSystemConstants.DEFAULT_POSITION;
        }


        public bool IsFirstWaypointGiveWay()
        {
            return _activePosition > 0 && _giveWay[0] != GiveWayType.None;
        }


        public float GetFirstSlowDownSpeed()
        {
            for (int i = 0; i < _slowDown.Count; i++)
            {
                if (_slowDown[i] == true)
                {
                    return GetWaypointSpeed(i);
                }
            }
            return TrafficSystemConstants.DEFAULT_SPEED;
        }


        public bool IsAllowedToChange()
        {
            if (_activePosition <= 0)
            {
                return false;
            }

            if(_ignoreRuls == true)
            {
                return true;
            }

            if (_stop[0] == true)
            {
                return false;
            }

            if (_giveWay[0] != GiveWayType.None)
            {
                return false;
            }

            return true;
        }

        public void UpdateCoveredWaypoints(Vector3 position, Vector3 forward)
        {
            if (_coveredWaypoints.Count > 0)
            {
#if GLEY_TRAFFIC_SYSTEM
                var waypoint = _coveredWaypoints.Peek();
                float waypointDistance = Vector3.Distance(API.GetWaypointFromIndex(waypoint).Position, position);
                float3 waypointDirection = API.GetWaypointFromIndex(waypoint).Position - position;
                float dotProduct = Vector3.Dot(waypointDirection, forward);

                if (dotProduct < 0)
                {
                    _coveredWaypoints.Dequeue();
                    UpdateCoveredWaypoints(position, forward);
                }
#endif
            }
        }
    }
}