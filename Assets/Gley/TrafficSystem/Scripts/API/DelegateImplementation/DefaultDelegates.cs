using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.TrafficSystem
{
    public class DefaultDelegates
    {
        #region SpawnWaypoints


        /// <summary>
        /// The default behavior, a random square is chosen from the available ones 
        /// </summary>
        /// <param name="neighbors"></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static int GetRandomSpawnWaypoint(List<Vector2Int> neighbors, Vector3 position, Vector3 direction, VehicleTypes vehicleType, bool useWaypointPriority)
        {
#if GLEY_TRAFFIC_SYSTEM
            Vector2Int selectedNeighbor = neighbors[Random.Range(0, neighbors.Count)];

            return GetPossibleWaypoint(selectedNeighbor, vehicleType, useWaypointPriority);
#else
            return -1;
#endif
        }


        /// <summary>
        /// The square in front of the player is chosen
        /// </summary>
        /// <param name="neighbors"></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static int GetForwardSpawnWaypoint(List<Vector2Int> neighbors, Vector3 position, Vector3 direction, VehicleTypes vehicleType, bool useWaypointPriority)
        {
#if GLEY_TRAFFIC_SYSTEM
            Vector2Int selectedNeighbor = Vector2Int.zero;
            float angle = 180;
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3 cellDirection = API.GetGridData().GetCellPosition(neighbors[i]) - position;
                float newAngle = Vector3.Angle(cellDirection, direction);
                if (newAngle < angle)
                {
                    selectedNeighbor = neighbors[i];
                    angle = newAngle;
                }
            }

            return GetPossibleWaypoint(selectedNeighbor, vehicleType, useWaypointPriority);
#else
            return -1;
#endif
        }

        private static int GetPossibleWaypoint(Vector2Int selectedNeighbor, VehicleTypes vehicleType, bool usePriority)
        {
#if GLEY_TRAFFIC_SYSTEM
            ////get a random waypoint that supports the current vehicle
            List<SpawnWaypoint> possibleWaypoints = API.GetGridData().GetTrafficSpawnWaypointsForCell(selectedNeighbor, (int)vehicleType);
            if (possibleWaypoints.Count > 0)
            {
                if (usePriority)
                {
                    int totalPriority = 0;
                    foreach (SpawnWaypoint waypoint in possibleWaypoints)
                    {
                        totalPriority += waypoint.Priority;
                    }
                    int randomPriority = Random.Range(1, totalPriority);
                    totalPriority = 0;
                    for (int i = 0; i < possibleWaypoints.Count; i++)
                    {
                        totalPriority += possibleWaypoints[i].Priority;
                        if (totalPriority >= randomPriority)
                        {
                            return possibleWaypoints[i].WaypointIndex;
                        }
                    }
                }
                else
                {
                    return possibleWaypoints[Random.Range(0, possibleWaypoints.Count)].WaypointIndex;
                }
            }
#endif
            return -1;
        }
        #endregion


        #region TrafficLights
        public static void TrafficLightBehaviour(TrafficLightsColor currentRoadColor, GameObject[] redLightObjects, GameObject[] yellowLightObjects, GameObject[] greenLightObjects, string name)
        {
            switch (currentRoadColor)
            {
                case TrafficLightsColor.Red:
                    SetLight(true, redLightObjects, name);
                    SetLight(false, yellowLightObjects, name);
                    SetLight(false, greenLightObjects, name);
                    break;
                case TrafficLightsColor.YellowRed:
                case TrafficLightsColor.YellowGreen:
                    SetLight(false, redLightObjects, name);
                    SetLight(true, yellowLightObjects, name);
                    SetLight(false, greenLightObjects, name);
                    break;
                case TrafficLightsColor.Green:
                    SetLight(false, redLightObjects, name);
                    SetLight(false, yellowLightObjects, name);
                    SetLight(true, greenLightObjects, name);
                    break;
            }
        }

        /// <summary>
        /// Set traffic lights color
        /// </summary>
        private static void SetLight(bool active, GameObject[] lightObjects, string name)
        {
            for (int j = 0; j < lightObjects.Length; j++)
            {
                if (lightObjects[j] != null)
                {
                    if (lightObjects[j].activeSelf != active)
                    {
                        lightObjects[j].SetActive(active);
                    }
                }
                else
                {
                    Debug.LogWarning("Intersection " + name + " has null red light objects");
                }
            }
        }
        #endregion


        #region TriggerModifier
        public static void TriggerSizeModifier(float currentSpeed, BoxCollider frontCollider, float maxSpeed, float minTriggerLength, float maxTriggerLength)
        {
            float minSpeed = 20;
            if (currentSpeed < minSpeed)
            {
                frontCollider.size = new Vector3(frontCollider.size.x, frontCollider.size.y, minTriggerLength);
                frontCollider.center = new Vector3(frontCollider.center.x, frontCollider.center.y, minTriggerLength / 2);
            }
            else
            {
                if (currentSpeed >= maxSpeed)
                {
                    frontCollider.size = new Vector3(frontCollider.size.x, frontCollider.size.y, maxTriggerLength);
                    frontCollider.center = new Vector3(frontCollider.center.x, frontCollider.center.y, maxTriggerLength / 2);
                }
                else
                {
                    float newsize = minTriggerLength + (currentSpeed - minSpeed) * ((maxTriggerLength - minTriggerLength) / (maxSpeed - minSpeed));
                    frontCollider.size = new Vector3(frontCollider.size.x, frontCollider.size.y, newsize);
                    frontCollider.center = new Vector3(frontCollider.center.x, frontCollider.center.y, newsize / 2);
                }
            }
        }
        #endregion
    }
}