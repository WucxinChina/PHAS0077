using Gley.UrbanSystem.Internal;
using UnityEngine;

namespace Gley.TrafficSystem
{
    public class DefaultBehaviourImplementation : IBehaviourImplementation, IDestroyable
    {
        private VehicleComponent[] _allVehicles;
        private int _knownWaypoints;

        public IBehaviourImplementation Initialize(params object[] parameters)
        {
            Assign();
            _allVehicles = (VehicleComponent[])parameters[0];
            MovementInfo.OnKnownListUpdated += KnownListUpdatedHandler;
            MovementInfo.OnStopWaypointsUpdated += StopWaypointsUpdatedHandler;
            Events.OnObstaclesUpdated += OnObstaclesUpdatedHandler;
            Events.OnObstacleRemoved += ObstacleRemovedHandler;
            MovementInfo.OnGiveWayWaypointsUpdated += GiveWayWaypointsUpdatedHandler;
            MovementInfo.OnSlowDownWaypointsUpdated += SlowDownWaypointsUpdatedHandler;
            Events.OnVehicleActivated += VehicleAddedHandler;
            Events.OnVehicleCrashed += VehicleCrashedHandler;
            _knownWaypoints = (int)parameters[1];
            return this;
        }


        private void KnownListUpdatedHandler(int vehicleIndex)
        {
            //if there are no waypoints in list -> start no waypoint behaviour
            //Debug.Log(_knownWaypointsList[vehicleIndex].RemainingPathLength + " " + _knownWaypoints);
            if (_allVehicles[vehicleIndex].MovementInfo.RemainingPathLength < _knownWaypoints)
            {
                if (_allVehicles[vehicleIndex].MovementInfo.NoWaypoints == false)
                {
                    _allVehicles[vehicleIndex].MovementInfo.NoWaypoints = true;
                    API.StartVehicleBehaviour<NoWaypoints>(vehicleIndex);
                }
            }
            else
            {
                if (_allVehicles[vehicleIndex].MovementInfo.NoWaypoints == true)
                {
                    _allVehicles[vehicleIndex].MovementInfo.NoWaypoints = false;
                    API.StopVehicleBehaviour<NoWaypoints>(vehicleIndex);
                }
            }
        }


        private void StopWaypointsUpdatedHandler(int vehicleIndex)
        {
            if (_allVehicles[vehicleIndex].MovementInfo.HasStopWaypoints())
            {
                API.StartVehicleBehaviour<StopInPoint>(vehicleIndex);
            }
            else
            {
                API.StopVehicleBehaviour<StopInPoint>(vehicleIndex);
            }
        }


        private void OnObstaclesUpdatedHandler(int vehicleIndex)
        {
            var obstacleType = _allVehicles[vehicleIndex].MovementInfo.ClosestObstacle.ObstacleType;
            switch (obstacleType)
            {
                case ObstacleTypes.TrafficVehicle:                  
                    if (ShouldFollow(vehicleIndex, _allVehicles[vehicleIndex].MovementInfo.ClosestObstacle))
                    {
                        API.StartVehicleBehaviour<FollowVehicle>(vehicleIndex);
                    }
                    break;
                case ObstacleTypes.Player:
                    PlayerSeen(vehicleIndex, _allVehicles[vehicleIndex].MovementInfo.ClosestObstacle);
                    break;
                case ObstacleTypes.DynamicObject:
                    API.StartVehicleBehaviour<StopInDistance>(vehicleIndex);
                    break;
                case ObstacleTypes.StaticObject:
                    API.StartVehicleBehaviour<AvoidReverse>(vehicleIndex);
                    break;
            }
        }


        private void PlayerSeen(int vehicleIndex, Obstacle closestObstacle)
        {
            ITrafficParticipant otherVehicle = closestObstacle.Collider.attachedRigidbody.GetComponent<ITrafficParticipant>();
            if (otherVehicle == null)
            {
                Debug.LogWarning($"{closestObstacle.Collider.name} is a vehicle but does not implement the ITrafficParticipant interface. If this is your player, attach the PlayerComponent or your own implementation of the interface.");
                return;
            }
            API.StartVehicleBehaviour<FollowPlayer>(vehicleIndex);
        }


        private bool IsSameOrientation(Vector3 heading1, Vector3 heading2)
        {
            float dotResult = Vector3.Dot(heading1.normalized, heading2.normalized);
            if (dotResult > 0)
            {
                return true;
            }
            return false;
        }


        private void ObstacleRemovedHandler(int vehicleIndex, ObstacleTypes obstacleType)
        {
            switch (obstacleType)
            {
                case ObstacleTypes.Player:
                    API.StopVehicleBehaviour<FollowPlayer>(vehicleIndex);
                    API.StopVehicleBehaviour<OvertakePlayer>(vehicleIndex);
                    break;
                case ObstacleTypes.StaticObject:
                    API.StopVehicleBehaviour<AvoidReverse>(vehicleIndex);
                    break;
                case ObstacleTypes.Other:
                    break;
                case ObstacleTypes.DynamicObject:
                    API.StopVehicleBehaviour<StopInDistance>(vehicleIndex);
                    break;
                case ObstacleTypes.TrafficVehicle:
                    API.StopVehicleBehaviour<FollowVehicle>(vehicleIndex);
                    API.StopVehicleBehaviour<Overtake>(vehicleIndex);
                    break;
                case ObstacleTypes.Road:
                    break;
                default:
                    Debug.Log($" Obstacle type {obstacleType} not implemented");
                    break;
            }
        }


        private void GiveWayWaypointsUpdatedHandler(int vehicleIndex)
        {
            if (_allVehicles[vehicleIndex].MovementInfo.HasGiveWayWaypoints())
            {
                API.StartVehicleBehaviour<GiveWay>(vehicleIndex);
            }
            else
            {
                API.StopVehicleBehaviour<GiveWay>(vehicleIndex);
            }
        }


        private void SlowDownWaypointsUpdatedHandler(int vehicleIndex)
        {
            if (_allVehicles[vehicleIndex].MovementInfo.HasSlowDownWaypoints())
            {
                API.StartVehicleBehaviour<Decelerate>(vehicleIndex);
            }
            else
            {
                API.StopVehicleBehaviour<Decelerate>(vehicleIndex);
            }
        }


        private void VehicleAddedHandler(int vehicleIndex, int waypointIndex)
        {
            API.StartVehicleBehaviour<Forward>(vehicleIndex);
            API.StartVehicleBehaviour<CurveSlowDown>(vehicleIndex);
        }


        private void VehicleCrashedHandler(int vehicleIndex, ObstacleTypes obstacleType, Collider other)
        {
            switch (obstacleType)
            {
                case ObstacleTypes.Player:
                case ObstacleTypes.StaticObject:
                case ObstacleTypes.DynamicObject:
                    API.StartVehicleBehaviour<TempStop>(vehicleIndex);
                    break;
                case ObstacleTypes.TrafficVehicle:
                    CheckWhoIsInFront(vehicleIndex, other);
                    break;
            }

        }

        private void CheckWhoIsInFront(int vehicleIndex, Collider other)
        {
            if (other.attachedRigidbody != null)
            {
                var otherComponent = other.attachedRigidbody.GetComponent<VehicleComponent>();
                if (otherComponent != null)
                {
                    var currentComponent = _allVehicles[vehicleIndex];
                    Vector3 relativePosition = otherComponent.FrontPosition.position - currentComponent.FrontPosition.position;
                    float dot = Vector3.Dot(relativePosition, currentComponent.transform.forward);

                    if (dot > 0) // Other car is in front
                    {
                        API.StartVehicleBehaviour<TempStop>(vehicleIndex);
                    }
                }
            }
        }

        private bool ShouldFollow(int vehicleIndex, Obstacle closestObstacle)
        {
            if (closestObstacle.Collider == null)
            {
                return false;
            }
            if(closestObstacle.Collider.attachedRigidbody==null)
            {
                return false;
            }

            //Debug.Log(closestObstacle.Collider);
            ITrafficParticipant otherVehicle = closestObstacle.Collider.attachedRigidbody.GetComponent<ITrafficParticipant>();
            if (otherVehicle.AlreadyCollidingWith(_allVehicles[vehicleIndex].AllColliders))
            {
                return false;
            }
            bool sameOrientation = IsSameOrientation(_allVehicles[vehicleIndex].GetHeading(), otherVehicle.GetHeading());

            if (!sameOrientation)
            {
                return false;
            }

            return true;
        }

        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }

        public void OnDestroy()
        {
            MovementInfo.OnKnownListUpdated -= KnownListUpdatedHandler;
            MovementInfo.OnStopWaypointsUpdated -= StopWaypointsUpdatedHandler;
            Events.OnObstaclesUpdated -= OnObstaclesUpdatedHandler;
            Events.OnObstacleRemoved -= ObstacleRemovedHandler;
            MovementInfo.OnGiveWayWaypointsUpdated -= GiveWayWaypointsUpdatedHandler;
            MovementInfo.OnSlowDownWaypointsUpdated -= SlowDownWaypointsUpdatedHandler;
            Events.OnVehicleActivated -= VehicleAddedHandler;
            Events.OnVehicleCrashed -= VehicleCrashedHandler;
        }
    }
}