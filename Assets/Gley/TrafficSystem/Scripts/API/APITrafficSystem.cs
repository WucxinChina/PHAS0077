using System.Collections.Generic;
using System.Linq;
using Gley.TrafficSystem.Internal;
using UnityEngine;
using UnityEngine.Events;
#if GLEY_TRAFFIC_SYSTEM
using TrafficManager = Gley.TrafficSystem.Internal.TrafficManager;
#endif

namespace Gley.TrafficSystem
{
    public static class API
    {

        private static float globalSafeDistance = TrafficSettings.MinSpawnDistance;

        /// <summary>
        /// Adds a new neighbor to the waypoint at the specified index.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to which the neighbor will be added.</param>
        /// <param name="waypointIndexToAdd">The index of the waypoint to be added as a neighbor.</param>
        public static void AddNeighbor(int waypointIndex, int waypointIndexToAdd)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.TrafficWaypointsData.AddNeighbor(waypointIndex, waypointIndexToAdd);
#endif
        }


        /// <summary>
        /// Links the specified waypoint to another waypoint in a different lane.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to which the other lane waypoint will be added.</param>
        /// <param name="waypointIndexToAdd">The index of the waypoint from the other lane to be linked.</param>
        public static void AddOtherLane(int waypointIndex, int waypointIndexToAdd)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.TrafficWaypointsData.AddOtherLane(waypointIndex, waypointIndexToAdd);
#endif
        }


        /// <summary>
        /// Adds a previous waypoint reference to the specified waypoint.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to which the previous waypoint will be added.</param>
        /// <param name="waypointIndexToAdd">The index of the waypoint to be added as a previous waypoint.</param>
        public static void AddPrevs(int waypointIndex, int waypointIndexToAdd)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.TrafficWaypointsData.AddPrevs(waypointIndex, waypointIndexToAdd);
#endif
        }


        /// <summary>
        /// Sets a new target waypoint for the specified vehicle, removing any previously assigned waypoints.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to set as the new target.</param>
        /// <param name="vehicleIndex">The index of the vehicle whose target waypoint will be updated.</param>
        public static void AddWaypointAndClear(int waypointIndex, int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.VehicleAI.AddWaypointAndClear(waypointIndex, vehicleIndex);
#endif
        }


        /// <summary>
        /// Add from code an event on a specific waypoint.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint on which to add the event.</param>
        /// <param name="data">The event data is utilized to recognize and respond to the event.</param>
        public static void AddWaypointEvent(int waypointIndex, string data)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.TrafficWaypointsData)
            {
                TrafficManager.Instance.TrafficWaypointsData.SetEventData(waypointIndex, data);
            }
#endif
        }


        /// <summary>
        /// Checks if the specified waypoint and the required number of previous waypoints are free, allowing the vehicle to move into that waypoint without risk of collision. 
        /// No other vehicle should have these waypoints as a target.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to check.</param>
        /// <param name="vehicleIndex">The index of the vehicle performing the check.</param>
        /// <returns>True if all previous waypoints are free; otherwise, false.</returns>
        public static bool AllPreviousWaypointsAreFree(int waypointIndex, int vehicleIndex, bool ignoreTime = false)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.VehicleAI != null)
            {
                return TrafficManager.Instance.VehicleAI.AllPreviousWaypointsAreFree(waypointIndex, vehicleIndex, ignoreTime);
            }
#endif
            return false;
        }


        /// <summary>
        /// Checks if all required waypoints associated with a Complex Give Way waypoint are free before allowing the vehicle to proceed.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to check.</param>
        /// <param name="vehicleIndex">The index of the vehicle performing the check.</param>
        /// <returns>True if all required waypoints are free; otherwise, false.</returns>
        public static bool AllRequiredWaypointsAreFree(int waypointIndex, int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.VehicleAI != null)
            {
                return TrafficManager.Instance.VehicleAI.AllRequiredWaypointsAreFree(waypointIndex, vehicleIndex);
            }
#endif
            return false;
        }


        /// <summary>
        /// Removes all the vehicles from a given area.
        /// </summary>
        /// <param name="center">The center of the circle to remove vehicles from.</param>
        /// <param name="radius">The radius in meters of the circle to remove vehicles from.</param>
        public static void ClearTrafficOnArea(Vector3 center, float radius, System.Func<VehicleComponent, bool> condition = null)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager?.ClearTrafficOnArea(center, radius, condition);
#endif
        }


        /// <summary>
        /// Disable all waypoints on the specified area to stop vehicles to go in a certain area for a limited amount of time
        /// </summary>
        /// <param name="center">The center of the circle to disable waypoints from.</param>
        /// <param name="radius">The radius in meters of the circle to disable waypoints from.</param>
        public static void DisableAreaWaypoints(Vector3 center, float radius)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DisabledWaypointsManager?.DisableAreaWaypoints(new Area(center, radius));
#endif
        }


        /// <summary>
        /// The marked vehicle will never be automatically removed once instantiated by the system—it will persist until it is manually removed.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle to persist.</param>
        /// <param name="dontRemove">If true the vehicle will never be removed by the system.</param>
        public static void DontRemoveVehicle(int vehicleIndex, bool dontRemove)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData.AllVehicles[vehicleIndex].MovementInfo.DontRemoveVehicle(dontRemove);
#endif

        }

        /// <summary>
        /// Enable all disabled area waypoints
        /// </summary>
        public static void EnableAllWaypoints()
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DisabledWaypointsManager?.EnableAllWaypoints();
#endif
        }


        /// <summary>
        /// Completely remove a vehicle from the Traffic System. Scripts will no longer interact with the vehicle sent as parameter
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle.</param>
        public static void ExcludeVehicleFromSystem(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.ExcludeVehicleFromSystem(vehicleIndex);
#endif
        }


        /// <summary>
        /// Get a list of all vehicles used by the Traffic System.
        /// </summary>
        /// <returns>An array with all vehicle components.</returns>
        public static VehicleComponent[] GetAllVehicles()
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.AllVehiclesData?.AllVehicles;
#else
            return null;
#endif
        }


        /// <summary>
        /// Get the closest waypoint to a position.
        /// </summary>
        /// <param name="position">A Vector3 representing the world-space coordinates from which the nearest traffic waypoint will be determined.</param>
        /// <returns>The closest waypoint component.</returns>
        public static TrafficWaypoint GetClosestWaypoint(Vector3 position)
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.WaypointSelector?.GetClosestWaypoint(position);
#else
            return null;
#endif
        }


        /// <summary>
        /// Get the closest waypoint to a position. Uses a direction vector to select the correct lane.
        /// </summary>
        /// <param name="position">A Vector3 representing the world-space coordinates from which the nearest traffic waypoint will be determined.</param>
        /// <param name="direction">A Vector3 representing the desired orientation. The closest waypoint should be aligned in this direction to ensure proper flow in the intended path.</param>
        /// <returns>The closest waypoint component.</returns>
        public static TrafficWaypoint GetClosestWaypointInDirection(Vector3 position, Vector3 direction)
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.WaypointSelector?.GetClosestWaypointInDirection(position, direction);
#else
            return null;
#endif
        }


        /// <summary>
        /// Converts the excluded vehicle GameObject into its corresponding vehicle index.
        /// </summary>
        /// <param name="vehicle">The root GameObject of an excluded vehicle.</param>
        /// <returns>The vehicle index (-1 = error)</returns>
        public static int GetIgnoredVehicleIndex(GameObject vehicle)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.AllVehiclesData != null)
            {
                return TrafficManager.Instance.AllVehiclesData.GetIgnoredVehicleIndex(vehicle);
            }
#endif
            return TrafficSystemConstants.INVALID_WAYPOINT_INDEX;
        }


        /// <summary>
        /// Returns a list of all excluded vehicles. 
        /// </summary>
        /// <returns>A list of all VehicleComponents that are currently excluded</returns>
        public static List<VehicleComponent> GetIgnoredVehicleList()
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.AllVehiclesData?.GetIgnoredVehicleList();
#else
            return null;
#endif
        }


        /// <summary>
        /// Returns a waypoint path between a start position and an end position for a specific vehicle type.
        /// </summary>
        /// <param name="startPosition">A Vector3 for the initial position.</param>
        /// <param name="endPosition">A Vector3 for the final position.</param>
        /// <param name="vehicleType">The vehicle type for which this path is intended.</param>
        /// <returns>The waypoint indexes of the path between startPosition and endPosition.</returns>
        public static List<int> GetPath(Vector3 startPosition, Vector3 endPosition, VehicleTypes vehicleType)
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.PathFindingManager?.GetPath(startPosition, endPosition, vehicleType);
#else
            return null;
#endif
        }


        /// <summary>
        /// Get the state stop state of a crossing
        /// </summary>
        /// <param name="crossingName">The name of the street crossing</param>
        /// <returns>true -> cars will stop</returns>
        public static bool GetPriorityCrossingStopState(string crossingName)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.IntersectionManager != null)
            {
                return TrafficManager.Instance.IntersectionManager.IsPriorityCrossingRed(crossingName);
            }
#endif
            return false;
        }


        /// <summary>
        /// Get the steering angle of a vehicle.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle.</param>
        /// <returns>The value of the current steering angle in degrees.</returns>
        public static float GetSteeringAngle(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.GetSteeringAngle(vehicleIndex);
#else
            return 0;
#endif           
        }

        /// <summary>
        /// Get the color of the vehicle traffic light
        /// </summary>
        /// <param name="crossingName">The name of the crossing to check</param>
        /// <returns>The color of the traffic light</returns>
        public static TrafficLightsColor GetTrafficLightsCrossingState(string crossingName)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.IntersectionManager != null)
            {
                return TrafficManager.Instance.IntersectionManager.GetTrafficLightsCrossingState(crossingName);
            }
#endif
            return TrafficLightsColor.Red;
        }


        /// <summary>
        /// Get a specific vehicle behaviour. Useful to call additional methods or set parameters on that behaviour.
        /// </summary>
        /// <typeparam name="T">The index of the vehicle.</typeparam>
        /// <param name="vehicleIndex">An implementation of VehicleBehaviour abstract class.</param>
        /// <returns></returns>
        public static VehicleBehaviour GetVehicleBehaviourOfType<T>(int vehicleIndex) where T : VehicleBehaviour
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.BehaviourManager?.GetBehaviour(vehicleIndex, typeof(T).Name);
#else
            return null;
#endif
        }


        /// <summary>
        /// Gets the Vehicle Component from the vehicle with the index passed as a parameter.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle to get the component from.</param>
        /// <returns>the component from the vehicle with the index passed as a parameter.</returns>
        public static VehicleComponent GetVehicleComponent(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.AllVehiclesData != null)
            {
                return TrafficManager.Instance.AllVehiclesData.GetVehicle(vehicleIndex);
            }
#endif
            return null;
        }


        /// <summary>
        /// Gets the index of a vehicle GameObject.
        /// </summary>
        /// <param name="vehicle">The root GameObject of a traffic vehicle.</param>
        /// <returns>The list index of the vehicle (-1 = error)</returns>
        public static int GetVehicleIndex(GameObject vehicle)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.AllVehiclesData != null)
            {
                return TrafficManager.Instance.AllVehiclesData.GetVehicleIndex(vehicle);
            }
#endif
            return -1;
        }


        /// <summary>
        /// Returns the Waypoint object for a given waypoint index.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint.</param>
        /// <returns>The Waypoint object at the index position inside the waypoint list</returns>
        public static TrafficWaypoint GetWaypointFromIndex(int waypointIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            var trafficWaypointsData = TrafficManager.Instance.TrafficWaypointsData;
            if (trafficWaypointsData == null)
            {
                return null;
            }
            return trafficWaypointsData.GetWaypointFromIndex(waypointIndex);
#else
            return null;
#endif
        }


        /// <summary>
        /// After the vehicle is disabled, it will not be instantiated anymore by the Traffic System
        /// </summary>
        /// <param name="vehicleIndex">Index of the vehicle to be excluded</param>
        public static void IgnoreVehicle(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager?.IgnoreVehicle(vehicleIndex);
#endif
        }


        /// <summary>
        /// Initialize the traffic system
        /// </summary>
        /// <param name="activeCamera">Camera that follows the player or the player itself.</param>
        /// <param name="nrOfVehicles">Maximum number of traffic vehicles active at the same time.</param>
        /// <param name="vehiclePool">Available vehicles asset.</param>
        public static void Initialize(Transform activeCamera, int nrOfVehicles, VehiclePool vehiclePool)
        {
            Initialize(activeCamera, nrOfVehicles, vehiclePool, new TrafficOptions());
        }


        /// <summary>
        /// Initialize the traffic system
        /// </summary>
        /// <param name="activeCamera">Camera that follows the player or the player itself.</param>
        /// <param name="nrOfVehicles">Maximum number of traffic vehicles active at the same time.</param>
        /// <param name="vehiclePool">Available vehicles asset.</param>
        /// <param name="trafficOptions">An object used to store the initialization parameters.</param>
        public static void Initialize(Transform activeCamera, int nrOfVehicles, VehiclePool vehiclePool, TrafficOptions trafficOptions)
        {
            Initialize(new Transform[] { activeCamera }, nrOfVehicles, vehiclePool, trafficOptions);
        }


        /// <summary>
        /// Initialize the traffic system
        /// </summary>
        /// <param name="activeCameras">Camera that follows the player or the player itself.</param>
        /// <param name="nrOfVehicles">Maximum number of traffic vehicles active at the same time.</param>
        /// <param name="vehiclePool">Available vehicles asset.</param>
        /// <param name="trafficOptions">An object used to store the initialization parameters.</param>
        public static void Initialize(Transform[] activeCameras, int nrOfVehicles, VehiclePool vehiclePool, TrafficOptions trafficOptions)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.Initialize(activeCameras, nrOfVehicles, vehiclePool, trafficOptions);
#endif
        }


        /// <summary>
        /// Initializes the custom class responsible for controlling vehicle behaviors.
        /// </summary>
        /// <param name="parameters">A variable-length array of generic parameters used for initialization.</param>
        public static void InitializeBehaviourImplementation(params object[] parameters)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.InitializeBehaviourImplementation(parameters);
#endif
        }


        /// <summary>
        /// This will instantiate an excluded vehicle, the vehicle will work normally, but when it is removed it will not be instantiated again
        /// If the index sent as parameter is not an excluded vehicle, it will be ignored
        /// Call AddExcludedVehicleToSystem to make it behave normally
        /// </summary>
        /// <param name="vehicleIndex">Index of the excluded vehicle</param>
        /// <param name="position">It will be instantiated at the closest waypoint from the position sent as parameter</param>
        public static void InstantiateIgnoredVehicle(int vehicleIndex, Vector3 position)
        {
            InstantiateIgnoredVehicle(vehicleIndex, position, null);
        }


        /// <summary>
        /// This will instantiate an excluded vehicle, the vehicle will work normally, but when it is removed it will not be instantiated again
        /// If the index sent as parameter is not an excluded vehicle, it will be ignored
        /// Call AddExcludedVehicleToSystem to make it behave normally
        /// </summary>
        /// <param name="vehicleIndex">Index of the excluded vehicle</param>
        /// <param name="position">It will be instantiated at the closest waypoint from the position sent as parameter</param>
        /// <param name="completeMethod">Callback triggered after instantiation. It returns the VehicleComponent and the waypoint index where the vehicle was instantiated.</param>
        public static void InstantiateIgnoredVehicle(int vehicleIndex, Vector3 position, UnityAction<VehicleComponent, int> completeMethod)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager?.RequestIgnoredVehicle(vehicleIndex, position, completeMethod);
#endif
        }


        /// <summary>
        /// Add a traffic vehicle to the closest waypoint from the given position
        /// This method will wait until that vehicle type is available and the closest waypoint will be free to add a new vehicle on it.
        /// The method will run in background until the new vehicle is added.
        /// </summary>
        /// <param name="position">The position where to add a new vehicle</param>
        /// <param name="vehicleType">The type of vehicle to add</param>
        public static void InstantiateVehicle(Vector3 position, VehicleTypes vehicleType)
        {
            InstantiateVehicle(position, vehicleType, null);
        }


        /// <summary>
        /// Add a traffic vehicle to the closest waypoint from the given position
        /// This method will wait until that vehicle type is available and the closest waypoint will be free to add a new vehicle on it.
        /// The method will run in background until the new vehicle is added.
        /// </summary>
        /// <param name="position">The position where to add a new vehicle</param>
        /// <param name="vehicleType">The type of vehicle to add</param>
        /// <param name="completeMethod">Callback triggered after instantiation. It returns the VehicleComponent and the waypoint index where the vehicle was instantiated.</param>
        public static void InstantiateVehicle(Vector3 position, VehicleTypes vehicleType, UnityAction<VehicleComponent, int> completeMethod)
        {
#if GLEY_TRAFFIC_SYSTEM
            Vector3 safePos = FindSafeSpawnPosition(position);
            TrafficManager.Instance.DensityManager?.RequestVehicleAtPosition(safePos, vehicleType, completeMethod, null);
#endif
        }



        /// <summary>
        /// Instantiates a vehicle on the specified position with an initial velocity.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle to be instantiated. Can be active or inactive at the time of instantiation.</param>
        /// <param name="frontWheelsPosition">The vehicle will be instantiated with the front wheels on this position.</param>
        /// <param name="vehicleRotation">The rotation of the instantiated vehicle.</param>
        /// <param name="initialVelocity">The initial linear velocity.</param>
        /// <param name="initialAngularVelocity">The initial angular velocity.</param>
        /// <param name="nextWaypointIndex">The waypoint the vehicle will go towards.</param>
        public static void InstantiateVehicleOnTheSpot(int vehicleIndex, Vector3 frontWheelsPosition, Quaternion vehicleRotation, Vector3 initialVelocity, Vector3 initialAngularVelocity, int nextWaypointIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager.InstantiateTrafficVehicle(vehicleIndex, frontWheelsPosition, vehicleRotation, initialVelocity, initialAngularVelocity, nextWaypointIndex);
#endif
        }


        /// <summary>
        /// Adds a vehicle and sets a predefined path to destination
        /// </summary>
        /// <param name="position">A Vector3 for the initial position. The vehicle will be placed on the closest waypoint from this position.</param>
        /// <param name="vehicleType">The type of the vehicle to be instantiated.</param>
        /// <param name="destination">A Vector3 for the destination position. The closest waypoint from this position will be the destination of the vehicle.</param>
        public static void InstantiateVehicleWithPath(Vector3 position, VehicleTypes vehicleType, Vector3 destination)
        {
            InstantiateVehicleWithPath(position, vehicleType, destination, null);
        }


        /// <summary>
        /// Adds a vehicle and sets a predefined path to destination
        /// </summary>
        /// <param name="position">A Vector3 for the initial position. The vehicle will be placed on the closest waypoint from this position.</param>
        /// <param name="vehicleType">The type of the vehicle to be instantiated.</param>
        /// <param name="destination">A Vector3 for the destination position. The closest waypoint from this position will be the destination of the vehicle.</param>
        /// <param name="completeMethod">Callback triggered after initialization. It returns the VehicleComponent and the waypoint index where the vehicle was instantiated.</param>
        public static void InstantiateVehicleWithPath(Vector3 position, VehicleTypes vehicleType, Vector3 destination, UnityAction<VehicleComponent, int> completeMethod)
        {
#if GLEY_TRAFFIC_SYSTEM
            Vector3 safePos = FindSafeSpawnPosition(position);
            TrafficManager.Instance.InstantiateVehicleWithPath(safePos, vehicleType, destination, completeMethod);
#endif
        }



        /// <summary>
        /// Check if the Traffic System is initialized
        /// </summary>
        /// <returns>true if initialized</returns>
        public static bool IsInitialized()
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Exists)
            {
                return TrafficManager.Instance.Initialized;
            }
#endif
            return false;
        }


        /// <summary>
        /// Determines whether the specified waypoint is currently the destination of any vehicle.
        /// </summary>
        /// <param name="waypointIndex">The index of the waypoint to check.</param>
        /// <param name="vehicleIndex">The index of the vehicle performing the check.</param>
        /// <returns>True if the waypoint is a destination for any vehicle; otherwise, false.</returns>
        public static bool IsThisWaypointADestination(int waypointIndex, int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.VehicleAI != null)
            {
                return TrafficManager.Instance.VehicleAI.IsThisWaypointADestination(waypointIndex, vehicleIndex);
            }
#endif
            return false;
        }


        /// <summary>
        /// Remove a specific vehicle from scene
        /// </summary>
        /// <param name="vehicle">Root GameObject of the vehicle to remove</param>
        public static void RemoveVehicle(GameObject vehicle)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager.RemoveVehicle(vehicle);
#endif
        }


        /// <summary>
        /// Remove a specific vehicle from scene
        /// </summary>
        /// <param name="vehicleIndex">Index of the vehicle to remove</param>
        public static void RemoveVehicle(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager.RemoveVehicle(vehicleIndex, true);
#endif
        }


        /// <summary>
        /// Remove a predefined path for a vehicle.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle to remove the path from.</param>
        public static void RemoveVehiclePath(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData?.AllVehicles[vehicleIndex].MovementInfo.RemovePath();
#endif
        }


        /// <summary>
        /// Remove an event from a waypoint.
        /// </summary>
        /// <param name="waypointIndex">The waypoint to remove the event from.</param>
        public static void RemoveWaypointEvent(int waypointIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            if (TrafficManager.Instance.TrafficWaypointsData != null)
            {
                TrafficManager.Instance.TrafficWaypointsData.SetEventData(waypointIndex, null);
            }
#endif
        }


        /// <summary>
        /// Return a previously excluded vehicle back to the Traffic System.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle.</param>
        public static void RestoreExcludedVehicleToSystem(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.RestoreRemovedVehicleToSystem(vehicleIndex);
#endif
        }


        /// <summary>
        /// Add a previously excluded vehicle back to the Traffic System
        /// </summary>
        /// <param name="vehicleIndex">Index of the vehicle to be added back to the system</param>
        public static void RestoreIgnoredVehicle(int vehicleIndex)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager?.RestoreIgnoredVehicle(vehicleIndex);
#endif
        }


        /// <summary>
        /// Set how far away active intersections should be -> default is 1
        /// If set to 2 -> intersections will update on a 2 square distance from the player
        /// </summary>
        /// <param name="level">How many squares away should intersections be updated</param>
        public static void SetActiveSquares(int level)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.SetActiveSquaresLevel(level);
#endif
        }


        /// <summary>
        /// Change the complete behaviour list for all vehicles. The old behaviours list will be overridden.
        /// </summary>
        /// <param name="vehicleBehaviours">An object that implements IBehaviourList</param>
        public static void SetAllVehiclesBehaviours(IBehaviourList vehicleBehaviours)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.BehaviourManager?.SetAllVehiclesBehaviours(vehicleBehaviours);
#endif
        }


        /// <summary>
        /// Update the active camera that is used to remove vehicles when are not in view
        /// </summary>
        /// <param name="activeCamera">Represents the camera or the player prefab</param>
        public static void SetCamera(Transform activeCamera)
        {
            SetCameras(new Transform[] { activeCamera });
        }


        /// <summary>
        /// Update active cameras that are used to remove vehicles when are not in view
        /// this is used in multiplayer/split screen setups
        /// </summary>
        /// <param name="activeCameras">Represents the cameras or the players from your game</param>
        public static void SetCameras(Transform[] activeCameras)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.UpdateCamera(activeCameras);
#endif
        }


        /// <summary>
        /// Calculates a path from the current position of the vehicle to a specified destination.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle.</param>
        /// <param name="position">The destination position.</param>
        public static void SetDestination(int vehicleIndex, Vector3 position)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.SetDestination(vehicleIndex, position);
#endif
        }


        /// <summary>
        /// Control the engine volume from your master volume
        /// </summary>
        /// <param name="volume">Current engine AudioSource volume</param>
        public static void SetEngineVolume(float volume)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.SoundManager?.UpdateMasterVolume(volume);
#endif
        }


        /// <summary>
        /// Set the give way property of a specified waypoint.
        /// </summary>
        /// <param name="waypointIndex">The waypoint index to change the property on.</param>
        /// <param name="giveWayValue">The new value of the property.</param>
        public static void SetGiveWayProperty(int waypointIndex, bool giveWayValue)
        {
#if GLEY_TRAFFIC_SYSTEM
            var trafficWaypointsData = TrafficManager.Instance.TrafficWaypointsData;
            if (trafficWaypointsData == null)
            {
                return;
            }
            var waypoint = trafficWaypointsData.GetWaypointFromIndex(waypointIndex);
            waypoint?.SetGiveWayValue(giveWayValue);
#endif
        }


        /// <summary>
        /// Enable/disable hazard lights for a vehicle
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle</param>
        /// <param name="activate">True - means hazard lights are on</param>
        public static void SetHazardLights(int vehicleIndex, bool activate)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData.AllVehicles[vehicleIndex].BlinkersController.SetHazardLights(activate);
#endif
        }


        /// <summary>
        /// Force a road from a traffic light intersection to change to green
        /// </summary>
        /// <param name="intersectionName">Name of the intersection to change</param>
        /// <param name="roadIndex">The road index to change</param>
        /// <param name="doNotChangeAgain">If true that road will stay green until this param is set back to false</param>
        public static void SetIntersectionRoadToGreen(string intersectionName, int roadIndex, bool doNotChangeAgain = false)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.IntersectionManager?.SetRoadToGreen(intersectionName, roadIndex, doNotChangeAgain);
#endif
        }


        /// <summary>
        /// Inform the priority pedestrian crossing that pedestrians started to cross 
        /// </summary>
        /// <param name="crossingName">The name of the street crossing</param>
        /// <param name="stop">Stop the cars</param>
        /// <param name="stopUpdate">Currently not used, no automatic update implemented</param>
        public static void SetPriorityCrossingStopState(string crossingName, bool stop, bool stopUpdate = true)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.IntersectionManager.SetPriorityCrossingState(crossingName, stop, stopUpdate);
#endif
        }


        /// <summary>
        /// Sets the lateral offset for a specific vehicle.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle to set the offset for.</param>
        /// <param name="offset">The new lateral offset value to apply to the vehicle's path.</param>
        public static void SetOffset(int vehicleIndex, float offset)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData.AllVehicles[vehicleIndex].MovementInfo.SetOffset(offset);
#endif
        }


        /// <summary>
        /// Change the stop property of a specified waypoint.
        /// </summary>
        /// <param name="waypointIndex">The waypoint index to change the property on.</param>
        /// <param name="stopValue">The new value of the property.</param>
        public static void SetStopProperty(int waypointIndex, bool stopValue)
        {
#if GLEY_TRAFFIC_SYSTEM
            var trafficWaypointsData = TrafficManager.Instance.TrafficWaypointsData;
            if (trafficWaypointsData == null)
            {
                return;
            }

            var waypoint = trafficWaypointsData.GetWaypointFromIndex(waypointIndex);
            waypoint?.SetStopValue(stopValue);
#endif
        }


        /// <summary>
        /// Modify max number of active vehicles
        /// </summary>
        /// <param name="nrOfVehicles">New max number of vehicles, needs to be less than the initialization max number of vehicles</param>
        public static void SetTrafficDensity(int nrOfVehicles)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.DensityManager?.SetTrafficDensity(nrOfVehicles);
#endif
        }


        /// <summary>
        /// Manually set the traffic light crossing state. The crossing will change instantly to the new color.
        /// </summary>
        /// <param name="crossingName">The name of the crossing to change.</param>
        /// <param name="newColor">The new color.</param>
        /// <param name="stopUpdate">Stop crossing from automatically changing colors from now on.</param>
        public static void SetTrafficLightsCrossingState(string crossingName, TrafficLightsColor newColor, bool stopUpdate = false)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.IntersectionManager.SetCrossingState(crossingName, newColor, stopUpdate, TrafficManager.Instance.TimeManager.RealTimeSinceStartup);
#endif
        }


        /// <summary>
        /// Change the complete behaviour list for a vehicle. The old behaviours list will be overridden.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle.</param>
        /// <param name="vehicleBehaviours">An object that implements IBehaviourList</param>
        public static void SetVehicleBehaviours(int vehicleIndex, IBehaviourList vehicleBehaviours)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.BehaviourManager?.SetVehicleBehaviours(vehicleIndex, vehicleBehaviours.GetBehaviours());
#endif
        }


        /// <summary>
        /// A specific predefined path can be assigned to any active vehicle within the Traffic System. 
        /// </summary>
        /// <param name="vehicleIndex"></param>
        /// <param name="pathWaypoints"></param>
        public static void SetVehiclePath(int vehicleIndex, List<int> pathWaypoints)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData?.AllVehicles[vehicleIndex].MovementInfo.SetPath(pathWaypoints);
#endif
        }


        /// <summary>
        /// Start executing a specific behaviour.
        /// </summary>
        /// <typeparam name="T">The behaviour to start. An implementation of VehicleBehaviour abstract class.</typeparam>
        /// <param name="vehicleIndex">The index of the vehicle.</param>
        /// <param name="parameters">Additional parameters required by the behaviour.</param>
        public static void StartVehicleBehaviour<T>(int vehicleIndex, params object[] parameters) where T : VehicleBehaviour
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.BehaviourManager?.StartBehaviour(vehicleIndex, typeof(T).Name, parameters);
#endif
        }


        /// <summary>
        /// Stop executing a specific behaviour.
        /// </summary>
        /// <typeparam name="T">The behaviour to stop. An implementation of VehicleBehaviour abstract class.</typeparam>
        /// <param name="vehicleIndex">The index of the vehicle.</param>
        public static void StopVehicleBehaviour<T>(int vehicleIndex) where T : VehicleBehaviour
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.BehaviourManager?.StopBehaviour(vehicleIndex, typeof(T).Name);
#endif
        }


        /// <summary>
        /// When this method is called, the vehicle passed as param is no longer controlled by the traffic system 
        /// until it is out of view and respawned
        /// </summary>
        /// <param name="vehicle">The vehicle to be removed from the Traffic System.</param>
        public static void StopVehicleDriving(GameObject vehicle)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.StopVehicleDriving(vehicle);
#endif
        }


        /// <summary>
        ///If a vehicle detects a collider and that collider is destroyed by another script, 
        ///the OnTriggerExit method is not automatically triggered.
        ///In such cases, this method needs to be manually invoked to remove the obstacle in front of the traffic vehicle.
        /// </summary>
        /// <param name="collider">The removed collider.</param>
        public static void TriggerColliderRemovedEvent(Collider collider)
        {
#if GLEY_TRAFFIC_SYSTEM
            TriggerColliderRemovedEvent(new Collider[] { collider });
#endif
        }


        public static void TriggerColliderRemovedEvent(Collider[] collider)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData?.TriggerColliderRemovedEvent(collider);
#endif
        }


        /// <summary>
        /// Turn all vehicle lights on or off
        /// </summary>
        /// <param name="on">If true, lights are on</param>
        public static void UpdateVehicleLights(bool on)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData?.UpdateVehicleLights(on);
#endif
        }


        /// <summary>
        /// Provides access to the waypoint data stored inside the grid. Useful for extending the plugin.
        /// </summary>
        /// <returns>An instance of GridDataHandler</returns>
        internal static UrbanSystem.Internal.GridData GetGridData()
        {
#if GLEY_TRAFFIC_SYSTEM
            return TrafficManager.Instance.GridData;
#else
            return null;
#endif
        }


        private static Vector3 FindSafeSpawnPosition(Vector3 startPosition)
        {
            var allVehicles = GetAllVehicles();
            var trafficData = TrafficManager.Instance.TrafficWaypointsData;
            TrafficWaypoint spawnWaypoint = GetClosestWaypoint(startPosition);

            if (allVehicles != null && spawnWaypoint != null)
            {
                bool tooClose = true;
                int maxTry = 50; // 避免死循环
                int tries = 0;

                while (tooClose && tries < maxTry)
                {
                    tooClose = false;
                    foreach (var v in allVehicles)
                    {
                        if (v == null) continue;
                        float dist = Vector3.Distance(spawnWaypoint.Position, v.FrontPosition.position);
                        if (dist < globalSafeDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                    {
                        var neighbors = spawnWaypoint.Neighbors;
                        if (neighbors != null && neighbors.Length > 0)
                        {
                            spawnWaypoint = trafficData.GetWaypointFromIndex(neighbors[0]); // 正确地取出下一个 TrafficWaypoint
                        }
                        else
                        {
                            break;
                        }
                    }
                    tries++;
                }
            }

            return spawnWaypoint != null ? spawnWaypoint.Position : startPosition;
        }



    }
}