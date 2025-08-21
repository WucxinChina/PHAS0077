using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    public class VehicleAI : IDestroyable
    {
        private readonly AllVehiclesData _allVehiclesData;
        private readonly TrafficWaypointsData _trafficWaypointsData;
        private readonly SoundManager _soundManager;
        private readonly TimeManager _timeManager;
        private readonly PlayerWaypointsManager _playerWaypointsManager;
        private readonly int _knownWaypoints;

        private bool _added;


        public delegate void NewWaypointRequested(int vehicleIndex);
        public static event NewWaypointRequested OnNewWaypointRequested;
        public static void TriggerNewWaypointRequestedEvent(int vehicleIndex)
        {
            OnNewWaypointRequested?.Invoke(vehicleIndex);
        }


        public VehicleAI(AllVehiclesData allVehiclesData, TrafficWaypointsData trafficWaypointsData, SoundManager soundManager, TimeManager timeManager, int knownWaypoints, PlayerWaypointsManager playerWaypointsManager)
        {
            _allVehiclesData = allVehiclesData;
            _trafficWaypointsData = trafficWaypointsData;
            _soundManager = soundManager;
            _timeManager = timeManager;
            _knownWaypoints = knownWaypoints;
            _playerWaypointsManager = playerWaypointsManager;
            Events.OnVehicleActivated += VehicleAddedHandler;
            Events.OnVehicleDisabled += VehicleRemovedHandler;
            WaypointEvents.OnStopStateChanged += StopStateChangedHandler;
            WaypointEvents.OnGiveWayStateChanged += GiveWayStateChangedHandler;
            GiveWay.OnPassageGranted += PassageGrantedHandler;
            VehicleEvents.OnObstacleInTriggerAdded += ObstacleInTriggerAddedHandler;
            VehicleEvents.OnObstacleInTriggerRemoved += ObstacleInTriggerRemovedHandler;
            Assign();
        }


        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        public void Drive(int vehicleIndex, int currentGear, bool waypointRequested, VehicleTypes vehicleType)
        {
            _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.SetClosestObstacleAndSpeed(_allVehiclesData.AllVehicles[vehicleIndex].FrontTrigger.position);

            if (waypointRequested)
            {
                TriggerNewWaypointRequestedEvent(vehicleIndex);
                if (!_added)
                {
                    UpdateKnownWaypointsList(vehicleIndex, vehicleType);
                    _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.IncreaseActivePosition();
                    RemoveOldWaypoint(vehicleIndex);
                    _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.UpdateCoveredWaypoints(_allVehiclesData.AllVehicles[vehicleIndex].BackPosition.position, _allVehiclesData.AllVehicles[vehicleIndex].BackPosition.forward);
                    Events.TriggerChangeDestinationEvent(vehicleIndex, _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.GetFirstPosition());
                }
                _added = false;
            }
            _allVehiclesData.AllVehicles[vehicleIndex].UpdateVehicleScripts(_soundManager.MasterVolume, _timeManager.RealTimeSinceStartup, currentGear < 0);
        }


        public void AddWaypointAndClear(int waypointIndex, int vehicleIndex)
        {
            _added = true;
            var nextWaypoint = _trafficWaypointsData.GetWaypointFromIndex(waypointIndex);
            _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.AddOtherLaneWaypoint(waypointIndex, _trafficWaypointsData.AllTrafficWaypoints[waypointIndex].Position, nextWaypoint.MaxSpeed, nextWaypoint.Stop, nextWaypoint.GetGiveWayState(), 0);
            Events.TriggerChangeDestinationEvent(vehicleIndex, _trafficWaypointsData.AllTrafficWaypoints[waypointIndex].Position);
            UpdateKnownWaypointsList(vehicleIndex, _allVehiclesData.AllVehicles[vehicleIndex].VehicleType);
        }


        public bool AllPreviousWaypointsAreFree(int waypointIndex, int vehicleIndexThatMadeTheRequest, bool ignoreTime)
        {
            // has no waypoints to advance
            if (waypointIndex == TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                return false;
            }

            if (IsThisWaypointAPlayerDestination(waypointIndex))
            {
                return false;
            }

            for (int i = 0; i < _allVehiclesData.AllVehicles.Length; i++)
            {
                if (_allVehiclesData.AllVehicles[i].MovementInfo.IsVehicleOnThisWaypoint(waypointIndex))
                {
                    return false;
                }
            }

            if(ignoreTime)
            {
                return true;
            }

            var vehicle = _allVehiclesData.AllVehicles[vehicleIndexThatMadeTheRequest];
            var waypoint = _trafficWaypointsData.AllTrafficWaypoints[waypointIndex];

            float distance = Vector3.SqrMagnitude(vehicle.FrontPosition.position - waypoint.Position);
            float timeToReachDestination = vehicle.GetTimeToCoverDistance(Mathf.Sqrt(distance));


            // all vehicles that have the current waypoint as a target
            for (int i = 0; i < _allVehiclesData.AllVehicles.Length; i++)
            {
                if (i == vehicleIndexThatMadeTheRequest)
                {
                    continue;
                }
                if (_allVehiclesData.AllVehicles[i].MovementInfo.GetWaypointIndex(0) == waypointIndex)
                {
                    float otherDistance = Vector3.SqrMagnitude(_allVehiclesData.AllVehicles[i].FrontPosition.position - waypoint.Position);
                    float otherTimeToReachDestination = _allVehiclesData.AllVehicles[i].GetTimeToCoverDistance(Mathf.Sqrt(otherDistance));
                    //Debug.Log($"Other Vehicle {i} time: {otherTimeToReachDestination} current vehicle {vehicleIndexThatMadeTheRequest} time {timeToReachDestination}");
                    if (otherTimeToReachDestination < timeToReachDestination)
                    {

                        if (vehicle.AllColliders.Contains(_allVehiclesData.AllVehicles[i].MovementInfo.ClosestObstacle.Collider))
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }

            //verify if prevs are occupied 
            float distanceToCheck = vehicle.MovementInfo.GetFirstWaypointSpeed() * timeToReachDestination;

            if (ArePrevsWaypointsATarget(waypoint, distanceToCheck, vehicleIndexThatMadeTheRequest, out var closestVehicleIndex))
            {
                // a vehicle is already on that waypoint
                if (closestVehicleIndex == TrafficSystemConstants.INVALID_VEHICLE_INDEX)
                {
                    return false;
                }

                var otherVehicle = _allVehiclesData.AllVehicles[closestVehicleIndex];
                float otherDistance = Vector3.SqrMagnitude(otherVehicle.FrontPosition.position - waypoint.Position);
                float otherTimeToReachDestination = otherVehicle.GetTimeToCoverDistance(Mathf.Sqrt(otherDistance));

                //Debug.Log($"Other Vehicle {closestVehicleIndex} time: {otherTimeToReachDestination} current vehicle {vehicleIndexThatMadeTheRequest} time {timeToReachDestination}");
                // if the coming vehicle will arrive later than the give way vehicle, allow th give way vehicle to pass
                // 2 is an additional 2 seconds time to avoid crashes if they both will arrive close to one another. 
                if (otherTimeToReachDestination < timeToReachDestination)
                {
                    if (vehicle.AllColliders.Contains(otherVehicle.MovementInfo.ClosestObstacle.Collider))
                    {
                        return true;
                    }
                    else
                    {

                    }
                    return false;
                }
                else
                {
                    //if time to reach is less, check if the other vehicle is not in front of current vehicle
                    Vector3 relativePosition = otherVehicle.FrontPosition.position - vehicle.FrontPosition.position;
                    float dot = Vector3.Dot(relativePosition, otherVehicle.transform.forward);
                    if (dot > 0) // Other car is in front
                    {
                        return false;
                    }
                    else
                    {
                        // if other vehicle is behind but has a greater speed, let it pass (useful for overtaking)
                        if (otherVehicle.GetCurrentSpeedMS() > 5 && otherTimeToReachDestination - timeToReachDestination < 2 && otherVehicle.GetCurrentSpeedMS() > vehicle.GetCurrentSpeedMS())
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }


        public bool AllRequiredWaypointsAreFree(int waypointIndex, int vehicleIndex)
        {
            var waypointsToCheck = _trafficWaypointsData.AllTrafficWaypoints[waypointIndex].GiveWayList;
            foreach (var waypoint in waypointsToCheck)
            {
                if (_playerWaypointsManager.IsThisWaypointIndexATarget(waypointIndex))
                {
                    return false;
                }

                foreach (var vehicle in _allVehiclesData.AllVehicles)
                {
                    if (vehicle.ListIndex != vehicleIndex && vehicle.MovementInfo.GetCurrentWaypointIndex() == waypoint)
                    {
                        return false;
                    }
                    if (vehicle.ListIndex != vehicleIndex && vehicle.MovementInfo.IsVehicleOnThisWaypoint(waypoint))
                    {
                        return false;
                    }
                }

                if (_trafficWaypointsData.AllTrafficWaypoints[waypoint].Stop)
                {
                    return true;
                }
            }
            return true;
        }


        public bool IsThisWaypointADestination(int waypointIndex, int vehicleIndexThatMadeTheRequest)
        {
            if (_playerWaypointsManager.IsThisWaypointIndexATarget(waypointIndex))
            {
                return true;
            }

            if (_trafficWaypointsData.AllTrafficWaypoints[waypointIndex].Stop)
            {
                return false;
            }

            foreach (var vehicle in _allVehiclesData.AllVehicles)
            {
                if (vehicle.ListIndex != vehicleIndexThatMadeTheRequest && vehicle.MovementInfo.GetCurrentWaypointIndex() == waypointIndex)
                {
                    //check if it is not behind me
                    if (vehicle.GetCurrentSpeedMS() < 2)
                    {
                        if (_allVehiclesData.AllVehicles[vehicleIndexThatMadeTheRequest].AllColliders.Contains(vehicle.MovementInfo.ClosestObstacle.Collider))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                if (vehicle.ListIndex != vehicleIndexThatMadeTheRequest && vehicle.MovementInfo.IsVehicleOnThisWaypoint(waypointIndex))
                {
                    return true;
                }
            }
            return false;
        }


        private bool ArePrevsWaypointsATarget(Waypoint startWaypoint, float maxDistance, int vehicleIndexThatMadeTheRequest, out int closestVehicleIndex)
        {
            Queue<Waypoint> queue = new Queue<Waypoint>();
            HashSet<int> visited = new HashSet<int>();

            // Start with the initial waypoint
            queue.Enqueue(startWaypoint);

            while (queue.Count > 0)
            {
                Waypoint currentWaypoint = queue.Dequeue();

                // Mark as visited
                if (!visited.Add(currentWaypoint.ListIndex))
                    continue;


                //// Check if the current waypoint is temporarily disabled
                if (IsThisWaypointADestination(currentWaypoint.ListIndex, vehicleIndexThatMadeTheRequest, out closestVehicleIndex))
                {
                    return true;
                }

                // Add current waypoint's prev waypoints to the stack for further exploration
                int[] prevIndices = currentWaypoint.Prevs;
                foreach (int prevIndex in currentWaypoint.Prevs)
                {
                    var newWaypoint = _trafficWaypointsData.AllTrafficWaypoints[prevIndex];
                    float sqrDistance = Vector3.SqrMagnitude(startWaypoint.Position - newWaypoint.Position);

                    if (sqrDistance < maxDistance * maxDistance)
                    {
                        queue.Enqueue(newWaypoint);
                    }
                }
            }
            closestVehicleIndex = TrafficSystemConstants.INVALID_VEHICLE_INDEX;
            return false;
        }


        private bool IsThisWaypointADestination(int waypointIndex, int vehicleIndexThatMadeTheRequest, out int closestVehicleIndex)
        {
            var distance = float.MaxValue;
            bool isDestination = false;
            closestVehicleIndex = TrafficSystemConstants.INVALID_VEHICLE_INDEX;

            for (int i = 0; i < _allVehiclesData.AllVehicles.Length; i++)
            {
                if (i == vehicleIndexThatMadeTheRequest)
                {
                    continue;
                }
                if (_allVehiclesData.AllVehicles[i].MovementInfo.WaypointCanBeReached(waypointIndex))
                {
                    var magnitude = Vector3.SqrMagnitude(_allVehiclesData.AllVehicles[i].FrontPosition.position - _trafficWaypointsData.AllTrafficWaypoints[waypointIndex].Position);

                    if (magnitude < distance)
                    {
                        distance = magnitude;
                        closestVehicleIndex = i;
                    }
                    isDestination = true;
                }
            }
            return isDestination;
        }

        private void VehicleAddedHandler(int vehicleIndex, int waypointIndex)
        {
            _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.ClearTarget(_allVehiclesData.AllVehicles[vehicleIndex].MaxSpeed);
            AddWaypointToPath(vehicleIndex, waypointIndex, 0);
            UpdateKnownWaypointsList(vehicleIndex, _allVehiclesData.AllVehicles[vehicleIndex].VehicleType);
        }


        private void VehicleRemovedHandler(int vehicleIndex)
        {
            //Debug.Log($"Vehicle {vehicleIndex} removed");
        }


        private void StopStateChangedHandler(int waypointIndex, bool stopState)
        {
            for (int i = 0; i < _allVehiclesData.AllVehicles.Length; i++)
            {
                if (_allVehiclesData.AllVehicles[i].MovementInfo.TrySetStopWaypoint(waypointIndex, stopState))
                {
                    RemoveOldWaypoint(i);
                }
            }
        }


        private void GiveWayStateChangedHandler(int waypointIndex, GiveWayType giveWayType)
        {
            for (int i = 0; i < _allVehiclesData.AllVehicles.Length; i++)
            {
                if (_allVehiclesData.AllVehicles[i].MovementInfo.TrySetGiveWayWaypoint(waypointIndex, giveWayType))
                {
                    RemoveOldWaypoint(i);
                }
            }
        }


        private void PassageGrantedHandler(int vehicleIndex, int waypointIndex)
        {
            if (_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.TrySetGiveWayWaypoint(waypointIndex, GiveWayType.None))
            {
                RemoveOldWaypoint(vehicleIndex);
            }
            else
            {
                Debug.Log("Something is not good " + vehicleIndex);
            }
        }


        private void ObstacleInTriggerRemovedHandler(int vehicleIndex, Obstacle obstacleToRemove)
        {
            _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.SetObstacles(_allVehiclesData.AllVehicles[vehicleIndex].Obstacles, _allVehiclesData.AllVehicles[vehicleIndex].FrontTrigger.position);
            _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.RemoveObstacle(obstacleToRemove.ObstacleType);
        }


        private void ObstacleInTriggerAddedHandler(int vehicleIndex, Obstacle newObstacle)
        {
            _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.SetObstacles(_allVehiclesData.AllVehicles[vehicleIndex].Obstacles, _allVehiclesData.AllVehicles[vehicleIndex].FrontTrigger.position);
        }


        private void RemoveOldWaypoint(int vehicleIndex)
        {
            while (_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.IsAllowedToChange())
            {
                _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.TargetPassed();
                var oldWaypointIndex = _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.OldWaypointIndex;
                if (oldWaypointIndex != TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
                {
                    var oldWaypoint = _trafficWaypointsData.GetWaypointFromIndex(oldWaypointIndex);

                    _allVehiclesData.AllVehicles[vehicleIndex].BlinkersController.UpdateBlinkers(oldWaypoint.BlinkType);

                    if (oldWaypoint.Enter)
                    {
                        var intersections = _trafficWaypointsData.AllTrafficWaypoints[oldWaypointIndex].AssociatedIntersections;
                        foreach (var intersection in intersections)
                        {
                            intersection.VehicleEnter(vehicleIndex);
                        }
                    }

                    if (oldWaypoint.Exit)
                    {
                        var intersections = _trafficWaypointsData.AllTrafficWaypoints[oldWaypointIndex].AssociatedIntersections;
                        foreach (var intersection in intersections)
                        {
                            intersection.VehicleLeft(vehicleIndex);
                        }
                    }

                    if (oldWaypoint.TriggerEvent)
                    {
                        Events.TriggerWaypointReachedEvent(vehicleIndex, oldWaypointIndex, _trafficWaypointsData.AllTrafficWaypoints[oldWaypointIndex].EventData);
                    }
                }
            }
        }


        private void AddWaypointToPath(int vehicleIndex, int waypointIndex, int angle)
        {
            var waypoint = _trafficWaypointsData.AllTrafficWaypoints[waypointIndex];
            _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.AddWaypointAsTarget(waypointIndex, waypoint.Position, waypoint.MaxSpeed, waypoint.Stop, waypoint.GetGiveWayState(), angle);
        }


        private bool IsThisWaypointAPlayerDestination(int waypointIndex)
        {
            return _playerWaypointsManager.IsThisWaypointIndexATarget(waypointIndex);
        }


        private void UpdateKnownWaypointsList(int vehicleIndex, VehicleTypes vehicleType)
        {
            bool found = true;
            while (_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.PathLength <= _knownWaypoints && found == true)
            {
                if (_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.HasPath)
                {
                    found = AddCustomPathWaypoint(vehicleIndex);
                }
                else
                {
                    found = AddRandomWaypoint(vehicleIndex, vehicleType);
                }
            }
        }


        private bool AddCustomPathWaypoint(int vehicleIndex)
        {
            if (_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.CustomPath != null && _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.CustomPath.Count > 0)
            {
                var nextWaypoint = _trafficWaypointsData.GetWaypointFromIndex(_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.CustomPath.Dequeue());
                AddWaypointToPath(vehicleIndex, nextWaypoint.ListIndex, 0);
                return true;
            }
            else
            {
                //Debug.Log(_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.PathLength);
                if (_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.PathLength == 1 && _allVehiclesData.AllVehicles[vehicleIndex].GetCurrentSpeedMS() < 0.1f)
                {
                    Events.TriggerDestinationReachedEvent(vehicleIndex);
                }
            }

            return false;
        }


        public bool AddRandomWaypoint(int vehicleIndex, VehicleTypes vehicleType)
        {
            var found = false;
            var oldWaypointIndex = _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.GetLastWaypointIndex();
            if (oldWaypointIndex != TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
            {
                if (_trafficWaypointsData.AllTrafficWaypoints[oldWaypointIndex].HasNeighbors)
                {
                    List<NeighborStruct> possibleWaypoints = _trafficWaypointsData.GetNeighborsWithConditions(oldWaypointIndex, vehicleType);
                    if (possibleWaypoints.Count > 0)
                    {
                        found = true;
                        var neighbor = possibleWaypoints[Random.Range(0, possibleWaypoints.Count)];
                        AddWaypointToPath(vehicleIndex, neighbor.WaypointIndex, neighbor.Angle);
                    }
                }
            }
            return found;
        }


        public void OnDestroy()
        {
            Events.OnVehicleActivated -= VehicleAddedHandler;
            Events.OnVehicleDisabled -= VehicleRemovedHandler;
            WaypointEvents.OnStopStateChanged -= StopStateChangedHandler;
            WaypointEvents.OnGiveWayStateChanged -= GiveWayStateChangedHandler;
            GiveWay.OnPassageGranted -= PassageGrantedHandler;
            VehicleEvents.OnObstacleInTriggerAdded -= ObstacleInTriggerAddedHandler;
            VehicleEvents.OnObstacleInTriggerRemoved -= ObstacleInTriggerRemovedHandler;
        }
    }
}