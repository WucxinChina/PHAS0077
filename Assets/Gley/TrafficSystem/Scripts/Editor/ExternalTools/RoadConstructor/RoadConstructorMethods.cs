#if GLEY_ROADCONSTRUCTOR_TRAFFIC
#if GLEY_PEDESTRIAN_SYSTEM
using Gley.PedestrianSystem.Internal;
#endif
using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Internal;
using PampelGames.RoadConstructor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem.Editor
{
    public class RoadConstructorMethods : UnityEditor.Editor
    {
        static Dictionary<PampelGames.RoadConstructor.Waypoint, WaypointSettings> _connections;

        private static string RoadConstructorWaypointsHolder
        {
            get
            {
                return $"{TrafficSystemConstants.PACKAGE_NAME}/{UrbanSystemConstants.EDITOR_HOLDER}/RoadConstructorWaypoints";
            }
        }

        private static string RoadConstructorIntersectionHolder
        {
            get
            {
                return $"{TrafficSystemConstants.PACKAGE_NAME}/{UrbanSystemConstants.EDITOR_HOLDER}/RoadConstructorIntersections";
            }
        }

        private static string RoadConstructorConnectionsHolder
        {
            get
            {
                return $"{TrafficSystemConstants.PACKAGE_NAME}/{UrbanSystemConstants.EDITOR_HOLDER}/RoadConstructorConnections";
            }
        }

        public static void ExtractWaypoints(Internal.IntersectionType intersectionType, float greenLightTime, float yellowLightTime, bool linkLanes, int waypointDistance, List<int> vehicleTypes)
        {
            Debug.Log("Extracting waypoints");
            _connections = new Dictionary<PampelGames.RoadConstructor.Waypoint, WaypointSettings>();
            DestroyImmediate(GameObject.Find(RoadConstructorWaypointsHolder));
            DestroyImmediate(GameObject.Find(RoadConstructorIntersectionHolder));
            DestroyImmediate(GameObject.Find(RoadConstructorConnectionsHolder));

            var roadConstructor = FindObjectOfType<RoadConstructor>();



            Transform intersectionHolder = MonoBehaviourUtilities.GetOrCreateGameObject(RoadConstructorIntersectionHolder, true).transform;
            Transform waypointsHolder = MonoBehaviourUtilities.GetOrCreateGameObject(RoadConstructorWaypointsHolder, true).transform;
            Transform connectorsHolder = MonoBehaviourUtilities.GetOrCreateGameObject(RoadConstructorConnectionsHolder, true).transform;

            var allWaypoints = new List<PampelGames.RoadConstructor.Waypoint>();
            //create road waypoints
            var roadObjects = roadConstructor.GetRoads();
            for (var i = 0; i < roadObjects.Count; i++)
            {
                var trafficLanes = roadObjects[i].GetTrafficLanes(TrafficLaneType.Car, TrafficLaneDirection.Forward);
                var roadName = roadObjects[i].name.Replace("-", "");
                Transform road = MonoBehaviourUtilities.CreateGameObject(roadName, waypointsHolder, trafficLanes[0].spline.Knots.First().Position, true).transform;
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    Transform lane = MonoBehaviourUtilities.CreateGameObject($"Lane_{j}-Forward", road, trafficLanes[j].spline.Knots.First().Position, true).transform;
                    var waypoints = trafficLanes[j].GetWaypoints();
                    allWaypoints.AddRange(waypoints);
                    if (waypoints.Count > 0)
                    {
                        CreateTrafficWaypoints(lane, waypoints, (int)trafficLanes[j].maxSpeed, trafficLanes[j].width, $"{roadName}-{lane.name}", false, vehicleTypes);
                    }
                }

                trafficLanes = roadObjects[i].GetTrafficLanes(TrafficLaneType.Car, TrafficLaneDirection.Backwards);
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    Transform lane = MonoBehaviourUtilities.CreateGameObject($"Lane_{j}-Backwards", road, trafficLanes[j].spline.Knots.First().Position, true).transform;
                    var waypoints = trafficLanes[j].GetWaypoints();
                    allWaypoints.AddRange(waypoints);
                    if (waypoints.Count > 0)
                    {
                        CreateTrafficWaypoints(lane, waypoints, (int)trafficLanes[j].maxSpeed, trafficLanes[j].width, $"{roadName}-{lane.name}", false, vehicleTypes);
                    }
                }
            }

            //create intersection waypoints
            var intersectionObjects = roadConstructor.GetIntersections();
            for (var i = 0; i < intersectionObjects.Count; i++)
            {
                var trafficLanes = intersectionObjects[i].GetTrafficLanes(TrafficLaneType.Car, TrafficLaneDirection.Both);
                var roadName = intersectionObjects[i].name.Replace("-", "");
                Transform road = MonoBehaviourUtilities.CreateGameObject(roadName, connectorsHolder, trafficLanes[0].spline.Knots.First().Position, true).transform;
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    Transform lane = MonoBehaviourUtilities.CreateGameObject($"Lane_{j}", road, trafficLanes[j].spline.Knots.First().Position, true).transform;
                    var waypoints = trafficLanes[j].GetWaypoints();
                    allWaypoints.AddRange(waypoints);
                    if (waypoints.Count > 0)
                    {
                        CreateTrafficWaypoints(lane, waypoints, (int)trafficLanes[j].maxSpeed, trafficLanes[j].width, $"{roadName}-{lane.name}", true, vehicleTypes);
                    }
                }
            }


            //link waypoints
            for (var i = 0; i < allWaypoints.Count; i++)
            {
                Link(allWaypoints[i]);
            }

            if (linkLanes)
            {
                LinkOvertakeLanes(waypointsHolder, waypointDistance);
            }

            //add intersections
            CreateIntersections(connectorsHolder, intersectionType, intersectionHolder, greenLightTime, yellowLightTime);

        }

        private static void CreateIntersections(Transform holder, Internal.IntersectionType intersectionType, Transform intersectionHolder, float greenLightTime, float yellowLightTime)
        {
            for (int i = 0; i < holder.childCount; i++)
            {
                var intersection = MonoBehaviourUtilities.CreateGameObject(holder.GetChild(i).name, intersectionHolder, holder.GetChild(i).position, true);
                switch (intersectionType)
                {
                    case Internal.IntersectionType.Priority:
                        AddPriorityIntersection(intersection, holder.GetChild(i));
                        break;
                    case Internal.IntersectionType.TrafficLights:
                        AddTrafficLightsIntersection(intersection, holder.GetChild(i), greenLightTime, yellowLightTime);
                        break;
                    default:
                        Debug.LogWarning($"{intersectionType} not supported");
                        break;
                }
            }
        }

        private static void AddTrafficLightsIntersection(GameObject intersection, Transform intersectionConnections, float greenLightTime, float yellowLightTime)
        {
            var intersectionScript = intersection.AddComponent<TrafficLightsIntersectionSettings>();
            intersectionScript.stopWaypoints = new List<IntersectionStopWaypointsSettings>();
            intersectionScript.exitWaypoints = new List<WaypointSettings>();
            intersectionScript.greenLightTime = greenLightTime;
            intersectionScript.yellowLightTime = yellowLightTime;
            for (int i = 0; i < intersectionConnections.childCount; i++)
            {
                //add waypoints
                WaypointSettings waypointToAdd = intersectionConnections.GetChild(i).GetChild(0).GetComponent<WaypointSettings>();
                if (waypointToAdd.prev.Count > 0)
                {
                    waypointToAdd = (WaypointSettings)waypointToAdd.prev[0];
                    AssignEnterWaypoints(intersectionScript.stopWaypoints, waypointToAdd);
                }
                else
                {
                    Debug.LogWarning(waypointToAdd.name + " has no previous waypoints", waypointToAdd);
                }

                //exit waypoints
                waypointToAdd = intersectionConnections.GetChild(i).GetChild(intersectionConnections.GetChild(i).childCount - 1).GetComponent<WaypointSettings>();
                if (waypointToAdd.neighbors.Count > 0)
                {
                    waypointToAdd = (WaypointSettings)waypointToAdd.neighbors[0];
                    if (!intersectionScript.exitWaypoints.Contains(waypointToAdd))
                    {
                        intersectionScript.exitWaypoints.Add(waypointToAdd);
                    }
                }
                else
                {
                    Debug.LogWarning(waypointToAdd.name + " has no neighbors.", waypointToAdd);
                }
            }

            //set position
            if (intersectionScript.stopWaypoints.Count > 0)
            {
                Vector3 position = new Vector3();
                int nr = 0;
                for (int i = 0; i < intersectionScript.stopWaypoints.Count; i++)
                {
                    for (int j = 0; j < intersectionScript.stopWaypoints[i].roadWaypoints.Count; j++)
                    {
                        position += intersectionScript.stopWaypoints[i].roadWaypoints[j].transform.position;
                        nr++;
                    }
                }
                intersection.transform.position = position / nr;
            }

            //remove not valid intersections
            if (intersectionScript.stopWaypoints.Count <= 2)
            {
                DestroyImmediate(intersection);
                return;
            }
#if GLEY_PEDESTRIAN_SYSTEM
            var pedestrianWaypoints = new List<PedestrianWaypointSettings>();
            var directionWaypoints = new List<PedestrianWaypointSettings>();

            var pedestrianWaypointsHolder = GameObject.Find($"PedestrianSystem/EditorData/RoadConstructorConnections/{intersection.name}");

            if(pedestrianWaypointsHolder==null)
            {
                Debug.LogError("You are using the Pedestrian System. Please extract the pedestrian waypoints before extracting the traffic waypoints");
                return;
            }

            for (int i = 0; i < pedestrianWaypointsHolder.transform.childCount; i++)
            {
                for(int j=0;j<pedestrianWaypointsHolder.transform.GetChild(i).childCount;j++)
                {
                    if(pedestrianWaypointsHolder.transform.GetChild(i).GetChild(j).name.Contains(UrbanSystemConstants.ConnectionEdgeName))
                    {
                        var waypoint = pedestrianWaypointsHolder.transform.GetChild(i).GetChild(j).GetComponent<PedestrianWaypointSettings>();
                        pedestrianWaypoints.Add(waypoint);
                        for (int l = 0; l < waypoint.neighbors.Count; l++)
                        {
                            if (waypoint.neighbors[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                            {
                                directionWaypoints.Add((PedestrianWaypointSettings)waypoint.neighbors[l]);
                            }
                        }
                        for (int l = 0; l < waypoint.prev.Count; l++)
                        {
                            if (waypoint.prev[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                            {
                                directionWaypoints.Add((PedestrianWaypointSettings)waypoint.prev[l]);
                            }
                        }
                    }
                }
            }

            //Add pedestrian waypoints
            intersectionScript.pedestrianWaypoints = pedestrianWaypoints;
            intersectionScript.directionWaypoints = directionWaypoints;
#endif

        }



        static void AddPriorityIntersection(GameObject intersection, Transform intersectionConnections)
        {
            var intersectionScript = intersection.AddComponent<PriorityIntersectionSettings>();
            intersectionScript.enterWaypoints = new List<IntersectionStopWaypointsSettings>();
            intersectionScript.exitWaypoints = new List<WaypointSettings>();
            for (int i = 0; i < intersectionConnections.childCount; i++)
            {
                //add enterWaypoints
                WaypointSettings waypointToAdd = intersectionConnections.GetChild(i).GetChild(0).GetComponent<WaypointSettings>();
                if (waypointToAdd.prev.Count > 0)
                {
                    waypointToAdd = (WaypointSettings)waypointToAdd.prev[0];
                    AssignEnterWaypoints(intersectionScript.enterWaypoints, waypointToAdd);
                }
                else
                {
                    Debug.LogWarning(waypointToAdd.name + " has no previous waypoints", waypointToAdd);
                }

                //exit waypoints
                waypointToAdd = intersectionConnections.GetChild(i).GetChild(intersectionConnections.GetChild(i).childCount - 1).GetComponent<WaypointSettings>();
                if (waypointToAdd.neighbors.Count > 0)
                {
                    waypointToAdd = (WaypointSettings)waypointToAdd.neighbors[0];
                    if (!intersectionScript.exitWaypoints.Contains(waypointToAdd))
                    {
                        intersectionScript.exitWaypoints.Add(waypointToAdd);
                    }
                }
                else
                {
                    Debug.LogWarning(waypointToAdd.name + " has no neighbors.", waypointToAdd);
                }
            }
            //set position
            if (intersectionScript.enterWaypoints.Count > 0)
            {
                Vector3 position = new Vector3();
                int nr = 0;
                for (int i = 0; i < intersectionScript.enterWaypoints.Count; i++)
                {
                    for (int j = 0; j < intersectionScript.enterWaypoints[i].roadWaypoints.Count; j++)
                    {
                        position += intersectionScript.enterWaypoints[i].roadWaypoints[j].transform.position;
                        nr++;
                    }
                }
                intersection.transform.position = position / nr;
            }

            //remove not valid intersections
            if (intersectionScript.enterWaypoints.Count <= 2)
            {
                DestroyImmediate(intersection);
                return;
            }
#if GLEY_PEDESTRIAN_SYSTEM
            var pedestrianWaypoints = new List<PedestrianWaypointSettings>();
            var directionWaypoints = new List<PedestrianWaypointSettings>();

            var pedestrianWaypointsHolder = GameObject.Find($"PedestrianSystem/EditorData/RoadConstructorConnections/{intersection.name}");

            for (int i = 0; i < pedestrianWaypointsHolder.transform.childCount; i++)
            {
                for (int j = 0; j < pedestrianWaypointsHolder.transform.GetChild(i).childCount; j++)
                {
                    if (pedestrianWaypointsHolder.transform.GetChild(i).GetChild(j).name.Contains(UrbanSystemConstants.ConnectionEdgeName))
                    {
                        var waypoint = pedestrianWaypointsHolder.transform.GetChild(i).GetChild(j).GetComponent<PedestrianWaypointSettings>();
                        pedestrianWaypoints.Add(waypoint);
                        for (int l = 0; l < waypoint.neighbors.Count; l++)
                        {
                            if (waypoint.neighbors[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                            {
                                directionWaypoints.Add((PedestrianWaypointSettings)waypoint.neighbors[l]);
                            }
                        }
                        for (int l = 0; l < waypoint.prev.Count; l++)
                        {
                            if (waypoint.prev[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                            {
                                directionWaypoints.Add((PedestrianWaypointSettings)waypoint.prev[l]);
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < pedestrianWaypoints.Count; i++)
            {
                var waypoint = pedestrianWaypoints[i];
                for (int l = 0; l < waypoint.neighbors.Count; l++)
                {
                    if (!waypoint.neighbors[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                    { 
                        var number = waypoint.neighbors[l].name.Split("-")[0].Split("_")[1];
                        for (int j = 0; j < intersectionScript.enterWaypoints.Count; j++)
                        {
                            var roadName = intersectionScript.enterWaypoints[j].roadWaypoints[0].name.Split("-")[0].Split("_")[1];
                            if(number == roadName)
                            {
                                intersectionScript.enterWaypoints[j].pedestrianWaypoints.Add(waypoint);
                            }
                        }
                    }
                }
                for (int l = 0; l < waypoint.prev.Count; l++)
                {
                    if (!waypoint.prev[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                    {
                        var number = waypoint.prev[l].name.Split("-")[0].Split("_")[1];
                        for (int j = 0; j < intersectionScript.enterWaypoints.Count; j++)
                        {
                            var roadName = intersectionScript.enterWaypoints[j].roadWaypoints[0].name.Split("-")[0].Split("_")[1];
                            if (number == roadName)
                            {
                                intersectionScript.enterWaypoints[j].pedestrianWaypoints.Add(waypoint);
                            }
                        }
                    }
                }
            }

            for(int i=0;i< intersectionScript.enterWaypoints.Count;i++)
            {
                for(int j = 0; j < intersectionScript.enterWaypoints[i].pedestrianWaypoints.Count;j++)
                {
                    var waypoint = intersectionScript.enterWaypoints[i].pedestrianWaypoints[j];
                    for (int l = 0; l < waypoint.neighbors.Count; l++)
                    {
                        if (waypoint.neighbors[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                        {
                            intersectionScript.enterWaypoints[i].directionWaypoints.Add((PedestrianWaypointSettings)waypoint.neighbors[l]);
                        }
                    }
                    for (int l = 0; l < waypoint.prev.Count; l++)
                    {
                        if (waypoint.prev[l].name.Contains(UrbanSystemConstants.ConnectionWaypointName))
                        {
                            intersectionScript.enterWaypoints[i].directionWaypoints.Add((PedestrianWaypointSettings)waypoint.prev[l]);
                        }
                    }
                }
            }
            //Add pedestrian waypoints
            //intersectionScript. = pedestrianWaypoints;
            //intersectionScript.directionWaypoints = directionWaypoints;
#endif
        }

        private static void AssignEnterWaypoints(List<IntersectionStopWaypointsSettings> enterWaypoints, WaypointSettings waypointToAdd)
        {
            string roadName = waypointToAdd.name.Split('-')[0];
            int index = -1;

            for (int j = 0; j < enterWaypoints.Count; j++)
            {
                if (enterWaypoints[j].roadWaypoints.Count > 0)
                {
                    if (enterWaypoints[j].roadWaypoints[0].name.Contains(roadName))
                    {
                        index = j;
                    }
                }
            }
            if (index == -1)
            {
                enterWaypoints.Add(new IntersectionStopWaypointsSettings());
                index = enterWaypoints.Count - 1;
                enterWaypoints[index].roadWaypoints = new List<WaypointSettings>();
            }

            if (!enterWaypoints[index].roadWaypoints.Contains(waypointToAdd))
            {
                enterWaypoints[index].roadWaypoints.Add(waypointToAdd);
            }
        }

        private static void LinkOvertakeLanes(Transform holder, int waypointDistance)
        {
            for (int i = 0; i < holder.childCount; i++)
            {
                for (int j = 0; j < holder.GetChild(i).childCount; j++)
                {
                    Transform firstLane = holder.GetChild(i).GetChild(j);
                    int laneToLink = j - 1;
                    if (laneToLink >= 0)
                    {
                        LinkLanes(firstLane, holder.GetChild(i).GetChild(laneToLink), waypointDistance);
                    }
                    laneToLink = j + 1;
                    if (laneToLink < holder.GetChild(i).childCount)
                    {
                        LinkLanes(firstLane, holder.GetChild(i).GetChild(laneToLink), waypointDistance);
                    }
                }
            }
        }

        private static void LinkLanes(Transform firstLane, Transform secondLane, int waypointDistance)
        {
            if (secondLane.name.Split('-')[1] == firstLane.name.Split('-')[1])
            {
                LinkLaneWaypoints(firstLane, secondLane, waypointDistance);
            }
        }

        private static void LinkLaneWaypoints(Transform currentLane, Transform otherLane, int waypointDistance)
        {
            for (int i = 0; i < currentLane.childCount; i++)
            {
                int otherLaneIndex = i + waypointDistance;
                if (otherLaneIndex < otherLane.childCount - 1)
                {
                    WaypointSettings currentLaneWaypoint = currentLane.GetChild(i).GetComponent<WaypointSettings>();
                    WaypointSettings otherLaneWaypoint = otherLane.GetChild(otherLaneIndex).GetComponent<WaypointSettings>();
                    currentLaneWaypoint.otherLanes.Add(otherLaneWaypoint);
                }
            }
        }

        private static void Link(PampelGames.RoadConstructor.Waypoint waypoint)
        {
            if (_connections.TryGetValue(waypoint, out var trafficWaypoint))
            {
                for (int i = 0; i < waypoint.next.Count; i++)
                {
                    if (_connections.TryGetValue(waypoint.next[i], out var neighbor))
                    {
                        trafficWaypoint.neighbors.Add(neighbor);
                    }
                }
                for (int i = 0; i < waypoint.prev.Count; i++)
                {
                    if (_connections.TryGetValue(waypoint.prev[i], out var prev))
                    {
                        trafficWaypoint.prev.Add(prev);
                    }
                }
            }
        }


        private static void CreateTrafficWaypoints(Transform waypointsHolder, List<PampelGames.RoadConstructor.Waypoint> waypoints, int maxSpeed, float laneWidth, string name, bool intersection, List<int> vehicleTypes)
        {
            TrafficWaypointCreator waypointCreator = new TrafficWaypointCreator();
            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypointName = name;
                if (waypoints[i].startPoint)
                {
                    if (intersection)
                    {
                        waypointName += "-" + UrbanSystemConstants.ConnectionEdgeName + i;
                    }
                    else
                    {
                        waypointName += UrbanSystemConstants.InWaypointEnding;
                    }
                }
                else
                {
                    if (waypoints[i].endPoint)
                    {
                        if (intersection)
                        {
                            waypointName += "-" + UrbanSystemConstants.ConnectionEdgeName + i;
                        }
                        else
                        {
                            waypointName += UrbanSystemConstants.OutWaypointEnding;
                        }
                    }
                    else
                    {
                        if (intersection)
                        {
                            waypointName += "-" + UrbanSystemConstants.ConnectionWaypointName + i;
                        }
                        else
                        {
                            waypointName += "-Waypoint_" + i;
                        }
                    }
                }

                var transform = waypointCreator.CreateWaypoint(waypointsHolder, waypoints[i].transform.position, waypointName, vehicleTypes, maxSpeed, laneWidth);
                _connections.Add(waypoints[i], transform.GetComponent<WaypointSettings>());
            }
        }
    }
}
#endif