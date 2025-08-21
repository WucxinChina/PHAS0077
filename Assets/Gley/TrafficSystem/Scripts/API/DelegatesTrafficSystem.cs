using System.Collections.Generic;
using UnityEngine;
#if GLEY_TRAFFIC_SYSTEM
using TrafficManager = Gley.TrafficSystem.Internal.TrafficManager;
#endif


namespace Gley.TrafficSystem
{
    /// <summary>
    /// Delegate used to validate the position of a vehicle before instantiation.
    /// </summary>
    /// <param name="vehiclePosition">The position of the vehicle.</param>
    /// <param name="vehicleLength">The length of the vehicle.</param>
    /// <param name="vehicleHeight">The height of the vehicle.</param>
    /// <param name="vehicleWidth">The width of the vehicle.</param>
    /// <param name="vehicleRotation">The rotation of the vehicle.</param>
    /// <returns>Returns true if the position is valid, otherwise false.</returns>
    public delegate bool CustomPositionValidation(Vector3 vehiclePosition, float vehicleLength, float vehicleHeight, float vehicleWidth, Quaternion vehicleRotation);

    public delegate void TrafficLightsBehaviour(TrafficLightsColor currentRoadColor, GameObject[] redLightObjects, GameObject[] yellowLightObjects, GameObject[] greenLightObjects, string name);

    public delegate int SpawnWaypointSelector(List<Vector2Int> neighbors, Vector3 position, Vector3 direction, VehicleTypes vehicleType, bool useWaypointPriority);

    public delegate void ModifyTriggerSize(float currentSpeed, BoxCollider frontCollider, float maxSpeed, float minTriggerLength, float maxTriggerLength);

    public class Delegates
    {
        /// <summary>
        /// Controls the dimension of the front trigger based on the vehicle's speed.
        /// </summary>
        /// <param name="modifyTriggerSizeDelegate">new delegate method</param>
        /// <param name="vehicleIndex">vehicle index to apply (-1 apply to all)</param>
        public static void SetModifyTriggerSize(ModifyTriggerSize modifyTriggerSizeDelegate, int vehicleIndex = -1)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.AllVehiclesData?.ModifyTriggerSize(vehicleIndex, modifyTriggerSizeDelegate);
#endif
        }


        /// <summary>
        /// Controls the selection of a free waypoint to instantiate a new vehicle on.
        /// </summary>
        /// <param name="spawnWaypointSelectorDelegate">new delegate method</param>
        public static void SetSpawnWaypointSelector(SpawnWaypointSelector spawnWaypointSelectorDelegate)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.WaypointSelector?.SetSpawnWaypointSelector(spawnWaypointSelectorDelegate);
#endif
        }


        /// <summary>
        /// Controls the behavior of the lights inside a traffic light intersection.
        /// </summary>
        /// <param name="trafficLightsBehaviourDelegate">new delegate method</param>
        public static void SetTrafficLightsBehaviour(TrafficLightsBehaviour trafficLightsBehaviourDelegate)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.IntersectionManager?.SetTrafficLightsBehaviour(trafficLightsBehaviourDelegate);
#endif
        }


        /// <summary>
        /// Sets a custom position validation method.
        /// This delegate allows you to define custom logic for validating positions
        /// based on parameters such as position, length, height, width, and rotation.
        /// </summary>
        /// <param name="customPositionValidation">The delegate method to use for custom position validation.</param>
        public static void SetCustomPositionValidation(CustomPositionValidation customPositionValidation)
        {
#if GLEY_TRAFFIC_SYSTEM
            TrafficManager.Instance.PositionValidator.SetCustomPositionValidation(customPositionValidation);
#endif
        }
    }
}