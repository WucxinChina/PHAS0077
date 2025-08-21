
using UnityEngine;

namespace Gley.TrafficSystem
{
    public class Events
    {
        /// <summary>
        /// Event triggered when a vehicle starts executing a specific behavior.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that initiated the behavior.</param>
        /// <param name="behaviour">The type of behavior the vehicle has started.</param>
        public delegate void BehaviourStarted(int vehicleIndex, VehicleBehaviour behaviour);
        public static event BehaviourStarted OnBehaviourStarted;
        public static void TriggerBehaviourStartedEvent(int vehicleIndex, VehicleBehaviour behaviour)
        {
            OnBehaviourStarted?.Invoke(vehicleIndex, behaviour);
        }


        /// <summary>
        /// Event triggered when a vehicle stops executing a specific behavior.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that stopped the behavior.</param>
        /// <param name="behaviour">The type of behavior that was stopped.</param>
        public delegate void BehaviourStopped(int vehicleIndex, VehicleBehaviour behaviour);
        public static event BehaviourStopped OnBehaviourStopped;
        public static void TriggerBehaviourStoppedEvent(int vehicleIndex, VehicleBehaviour behaviour)
        {
            OnBehaviourStopped?.Invoke(vehicleIndex, behaviour);
        }


        /// <summary>
        /// Event triggered whenever a vehicle reaches a new waypoint and updates its destination.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that reached the waypoint.</param>
        /// <param name="newPosition">The new target position for the vehicle.</param>
        public delegate void ChangeDestination(int vehicleIndex, Vector3 newPosition);
        public static ChangeDestination OnChangeDestination;
        public static void TriggerChangeDestinationEvent(int vehicleIndex, Vector3 newPosition)
        {
            OnChangeDestination?.Invoke(vehicleIndex, newPosition);
        }


        /// <summary>
        /// Triggered every time a vehicle reaches the last point of its path.
        /// </summary>
        /// <param name="vehicleIndex">index of the vehicle</param>
        public delegate void DestinationReached(int vehicleIndex);
        public static DestinationReached OnDestinationReached;
        public static void TriggerDestinationReachedEvent(int vehicleIndex)
        {
            OnDestinationReached?.Invoke(vehicleIndex);
        }


        /// <summary>
        /// Event triggered when an obstacle is removed from the vehicle's obstacle list.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle affected by the obstacle removal.</param>
        /// <param name="obstacleType">The type of obstacle that was removed.</param>
        public delegate void ObstacleRemoved(int vehicleIndex, ObstacleTypes obstacleType);
        public static event ObstacleRemoved OnObstacleRemoved;
        public static void TriggerObstacleRemovedEvent(int vehicleIndex, ObstacleTypes obstacleType)
        {
            OnObstacleRemoved?.Invoke(vehicleIndex, obstacleType);
        }


        /// <summary>
        /// Event triggered whenever a new obstacle is detected and added to the vehicle's obstacle list.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle whose obstacle list has been updated.</param>
        public delegate void ObstaclesUpdated(int vehicleIndex);
        public static event ObstaclesUpdated OnObstaclesUpdated;
        public static void TriggerObstaclesUpdatedEvent(int vehicleIndex)
        {
            OnObstaclesUpdated?.Invoke(vehicleIndex);
        }


        /// <summary>
        /// Triggered every time a vehicle is activated inside the scene.
        /// </summary>
        /// <param name="vehicleIndex">index of the vehicle</param>
        public delegate void VehicleActivated(int vehicleIndex, int waypointIndex);
        public static VehicleActivated OnVehicleActivated;
        public static void TriggerVehicleActivatedEvent(int vehicleIndex, int waypointIndex)
        {
            OnVehicleActivated?.Invoke(vehicleIndex, waypointIndex);
        }


        /// <summary>
        /// Triggered when a vehicle crashes into another object. 
        /// </summary>
        public delegate void VehicleCrash(int vehicleIndex, ObstacleTypes obstacleType, Collider other);
        public static event VehicleCrash OnVehicleCrashed;
        public static void TriggerVehicleCrashEvent(int vehicleIndex, ObstacleTypes obstacleType, Collider other)
        {
            OnVehicleCrashed?.Invoke(vehicleIndex, obstacleType, other);
        }


        /// <summary>
        /// Triggered every time a vehicle is deactivated inside the scene.
        /// </summary>
        /// <param name="vehicleIndex">index of the vehicle</param>
        public delegate void VehicleDisabled(int vehicleIndex);
        public static VehicleDisabled OnVehicleDisabled;
        public static void TriggerVehicleDisabledEvent(int vehicleIndex)
        {
            OnVehicleDisabled?.Invoke(vehicleIndex);
        }


        /// <summary>
        /// Triggered every time a waypoint that has the Trigger Event option enabled is reached by a vehicle.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that reached the waypoint.</param>
        /// <param name="waypointIndex">The waypoint index that triggered the event.</param>
        /// <param name="data">The data set on that waypoint by Trigger Event option.</param>
        public delegate void WaypointReached(int vehicleIndex, int waypointIndex, string data);
        public static WaypointReached OnWaypointReached;
        public static void TriggerWaypointReachedEvent(int vehicleIndex, int waypointIndex, string data)
        {
            OnWaypointReached?.Invoke(vehicleIndex, waypointIndex, data);
        }
    }
}