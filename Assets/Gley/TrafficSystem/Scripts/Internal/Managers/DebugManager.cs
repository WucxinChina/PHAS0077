using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using Unity.Collections;
#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEditor;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Draw helping gizmos on scene.
    /// </summary>
    public class DebugManager
    {
#if UNITY_EDITOR
#if GLEY_TRAFFIC_SYSTEM
        private readonly DebugSettings _debugSettings;
        private readonly AllVehiclesData _allVehiclesData;
        private readonly DisabledWaypointsManager _disabledWaypointsManager;
        private readonly PathFindingData _pathFindingData;
        private readonly IntersectionManager _intersectionManager;
        private readonly TrafficWaypointsData _trafficWaypointsData;
        private readonly GridData _gridData;
        private readonly BehaviourManager _behaviourManager;
        private readonly PositionValidator _positionValidator;

        public DebugManager(DebugSettings debugSettings, AllVehiclesData allVehiclesData, PathFindingData pathFindingData, IntersectionManager intersectionManager,
            TrafficWaypointsData trafficWaypointsData, GridData gridData, BehaviourManager behaviourManager, DisabledWaypointsManager disabledWaypointsManager, PositionValidator positionValidator)
        {
            _debugSettings = debugSettings;
            _allVehiclesData = allVehiclesData;
            _pathFindingData = pathFindingData;
            _intersectionManager = intersectionManager;
            _trafficWaypointsData = trafficWaypointsData;
            _gridData = gridData;
            _behaviourManager = behaviourManager;
            _disabledWaypointsManager = disabledWaypointsManager;
            _positionValidator = positionValidator;
        }


        public void Update(int nrOfVehicles, int totalWheels, NativeArray<float3> wheelSuspensionPosition, NativeArray<float3> wheelSuspensionForce, NativeArray<int> wheelAssociatedCar)
        {
            if (_debugSettings.drawBodyForces)
            {
                DrawBodyForces(nrOfVehicles, totalWheels, wheelSuspensionPosition, wheelSuspensionForce, wheelAssociatedCar);
            }

            if (_debugSettings.debug)
            {
                DrawObstacleLine(nrOfVehicles);
            }
        }


        public void DrawGizmos()
        {
            if (_debugSettings.debug)
            {
                DebugVehicleActions(_debugSettings.debugSpeed, _debugSettings.debugPathFinding, _debugSettings.debugBehaviours);
            }

            if (_debugSettings.debugIntersections)
            {
                DebugIntersections();
            }

            if (_debugSettings.debugWaypoints)
            {
                DebugWaypoints();
            }

            if (_debugSettings.debugDisabledWaypoints)
            {
                DebugDisabledWaypoints();
            }

            if (_debugSettings.DebugSpawnWaypoints)
            {
                DebugSpawnWaypoints();
            }

            if (_debugSettings.DebugPlayModeWaypoints)
            {
                DebugPlayModeWaypoints();
            }
            if (_debugSettings.debugDensity)
            {
                DrawBounds();
            }
        }

        private void DrawBounds()
        {
            for (int i = 0; i < _positionValidator.MatrixToCheck.Count; i++)
            {

                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = _positionValidator.MatrixToCheck[i];

                // Draw the box
                Gizmos.DrawWireCube(Vector3.zero, _positionValidator.LastSize[i]);

                // Restore the original matrix
                Gizmos.matrix = oldMatrix;
            }
        }

        private void DebugPlayModeWaypoints()
        {
            if (Application.isPlaying)
            {
                bool showBlinkers;
                Vector3 position;
                var allWaypoints = _gridData.GetAllTrafficPlayModeWaypoints();
                for (int i = 0; i < allWaypoints.Count; i++)
                {
                    showBlinkers = false;
                    Gizmos.color = Color.blue;
                    position = _trafficWaypointsData.AllTrafficWaypoints[allWaypoints[i]].Position;
                    if (_debugSettings.ShowPosition)
                    {
                        Gizmos.DrawSphere(position, 0.4f);
                    }
                    if (_debugSettings.ShowIndex)
                    {
                        Handles.Label(position, allWaypoints[i].ToString());
                    }

                    if (_debugSettings.ShowBlinkers)
                    {
                        if (_trafficWaypointsData.AllTrafficWaypoints[allWaypoints[i]].BlinkType == BlinkType.Start)
                        {
                            Gizmos.color = Color.blue;
                            showBlinkers = true;
                        }
                        else
                        {
                            if (_trafficWaypointsData.AllTrafficWaypoints[allWaypoints[i]].BlinkType == BlinkType.Right)
                            {
                                Gizmos.color = Color.red;
                                showBlinkers = true;
                            }
                            else
                            {
                                if (_trafficWaypointsData.AllTrafficWaypoints[allWaypoints[i]].BlinkType == BlinkType.Left)
                                {
                                    Gizmos.color = Color.yellow;
                                    showBlinkers = true;
                                }
                            }
                        }
                        if (showBlinkers)
                        {
                            Gizmos.DrawSphere(position, 0.5f);
                        }
                    }
                }
            }
        }


        private void DebugSpawnWaypoints()
        {
            if (Application.isPlaying)
            {
                var allWaypoints = _gridData.GetAllTrafficSpawnWaypoints();
                for (int i = 0; i < allWaypoints.Count; i++)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(_trafficWaypointsData.AllTrafficWaypoints[allWaypoints[i].WaypointIndex].Position, 0.5f);
                }
            }
        }


        public bool IsDebugWaypointsEnabled()
        {
            return _debugSettings.debugWaypoints;
        }


        private void DrawObstacleLine(int nrOfVehicles)
        {
            for (int i = 0; i < nrOfVehicles; i++)
            {
                if (!_allVehiclesData.AllVehicles[i].MovementInfo.ClosestObstaclePoint.Equals(TrafficSystemConstants.DEFAULT_POSITION))
                {
                    Debug.DrawLine(_allVehiclesData.AllVehicles[i].MovementInfo.ClosestObstaclePoint, _allVehiclesData.AllVehicles[i].FrontTrigger.position, Color.magenta);
                }
            }
        }


        private void DrawBodyForces(int nrOfVehicles, int totalWheels, NativeArray<float3> wheelSuspensionPosition, NativeArray<float3> wheelSuspensionForce, NativeArray<int> wheelAssociatedCar)
        {
            for (int i = 0; i < nrOfVehicles; i++)
            {
                Debug.DrawRay(_allVehiclesData.AllVehicles[i].rb.transform.TransformPoint(_allVehiclesData.AllVehicles[i].rb.centerOfMass), _allVehiclesData.AllVehicles[i].GetVelocity(), Color.red);
                if (_allVehiclesData.AllVehicles[i].HasTrailer)
                {
                    Vector3 localVelocity = _allVehiclesData.AllVehicles[i].trailer.rb.transform.InverseTransformVector(_allVehiclesData.AllVehicles[i].GetVelocity());
                    Debug.DrawRay(_allVehiclesData.AllVehicles[i].trailer.rb.transform.TransformPoint(_allVehiclesData.AllVehicles[i].trailer.rb.centerOfMass), new Vector3(-localVelocity.x, 0, 0) * 100, Color.green, Time.deltaTime, false);
                    Debug.DrawRay(_allVehiclesData.AllVehicles[i].trailer.rb.transform.TransformPoint(_allVehiclesData.AllVehicles[i].trailer.rb.centerOfMass), _allVehiclesData.AllVehicles[i].trailer.Velocity, Color.red);
                }
            }

            for (int j = 0; j < totalWheels; j++)
            {
                Debug.DrawRay(wheelSuspensionPosition[j], wheelSuspensionForce[j] / _allVehiclesData.AllVehicles[wheelAssociatedCar[j]].SpringForce, Color.yellow);
            }
        }


        private void DebugDisabledWaypoints()
        {
            for (int i = 0; i < _disabledWaypointsManager.DisabledWaypoints.Count; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_trafficWaypointsData.AllTrafficWaypoints[_disabledWaypointsManager.DisabledWaypoints[i]].Position, 1);
            }

        }


        private void DebugWaypoints()
        {
            for (int i = 0; i < _allVehiclesData.AllVehicles.Length; i++)
            {
                if (_allVehiclesData.AllVehicles[i].MovementInfo.GetWaypointIndex(0) != TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
                {
                    Vector3 current = _allVehiclesData.AllVehicles[i].MovementInfo.GetPosition(0);
                    Vector3 next = _allVehiclesData.AllVehicles[i].MovementInfo.GetPosition(1);
                    Handles.DrawBezier(_allVehiclesData.AllVehicles[i].GetFrontAxlePosition(), next, current, current, Color.green, null, 2);
                    Gizmos.color = Color.green;
                    Vector3 position = _allVehiclesData.AllVehicles[i].MovementInfo.GetFirstPosition();
                    Gizmos.DrawSphere(position, 1);
                    position.y += 1.5f;
                    Handles.Label(position, i.ToString());

                    for (int j = 0; j < _allVehiclesData.AllVehicles[i].MovementInfo.PathLength; j++)
                    {
                        if (_allVehiclesData.AllVehicles[i].MovementInfo.IsStopWaypoint(j))
                        {
                            Gizmos.color = Color.red;
                        }
                        else
                        {
                            if (_allVehiclesData.AllVehicles[i].MovementInfo.IsGiveWayWaypoint(j))
                            {
                                Gizmos.color = Color.blue;
                            }
                            else
                            {
                                Gizmos.color = Color.grey;
                            }
                        }
                        position = _allVehiclesData.AllVehicles[i].MovementInfo.GetPosition(j);
                        Gizmos.DrawSphere(position, 0.5f);
                        position.y += 1.5f;
                    }
                }
                var coveredWaypoints = _allVehiclesData.AllVehicles[i].MovementInfo.CoveredWaypoints;
                foreach (var waypoint in coveredWaypoints)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(_trafficWaypointsData.AllTrafficWaypoints[waypoint].Position, 0.6f);
                }
            }
        }


        private void DebugIntersections()
        {
            var allIntersections = _intersectionManager.AllIntersections;
            for (int k = 0; k < allIntersections.Length; k++)
            {
                var stopWaypoints = allIntersections[k].GetStopWaypoints();
                for (int i = 0; i < stopWaypoints.Count; i++)
                {
                    if (_trafficWaypointsData.AllTrafficWaypoints[stopWaypoints[i]].Stop == true)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(_trafficWaypointsData.AllTrafficWaypoints[stopWaypoints[i]].Position, 1);
                    }
                }


                //priority intersections
                if (allIntersections[k].GetType().Equals(typeof(PriorityIntersection)))
                {
                    PriorityIntersection intersection = (PriorityIntersection)allIntersections[k];
                    string text = $"In intersection \nVehicles {intersection.GetCarsInIntersection()}";
#if GLEY_PEDESTRIAN_SYSTEM
                    text += $"\nPedestrians {intersection.GetPedestriansCrossing().Count}";
#endif
                    Handles.Label(intersection.GetPosition(), text);
                    for (int i = 0; i < intersection.GetWaypointsToCkeck().Count; i++)
                    {
                        Handles.color = intersection.GetWaypointColors()[i];
                        Handles.DrawWireDisc(_trafficWaypointsData.AllTrafficWaypoints[intersection.GetWaypointsToCkeck()[i]].Position, Vector3.up, 1);
                    }
                }

                //priority crossings
                if (allIntersections[k].GetType().Equals(typeof(PriorityCrossing)))
                {
                    PriorityCrossing intersection = (PriorityCrossing)allIntersections[k];
                    string text = $"Crossing \n";
#if GLEY_PEDESTRIAN_SYSTEM
                    text += $"\nPedestrians {intersection.GetPedestriansCrossing().Count}";
#endif
                    Handles.Label(intersection.GetPosition(), text);
                    for (int i = 0; i < intersection.GetWaypointsToCkeck().Length; i++)
                    {
                        Handles.color = intersection.GetWaypointColors();
                        Handles.DrawWireDisc(_trafficWaypointsData.AllTrafficWaypoints[intersection.GetWaypointsToCkeck()[i]].Position, Vector3.up, 1);
                    }
                }

#if GLEY_PEDESTRIAN_SYSTEM
#if GLEY_TRAFFIC_SYSTEM
                if (Gley.PedestrianSystem.Internal.PedestrianManager.Instance.IsInitialized())
                {
                    int[] pedestrianStopWaypoints = allIntersections[k].GetPedStopWaypoint();
                    for (int l = 0; l < pedestrianStopWaypoints.Length; l++)
                    {
                        if (Gley.PedestrianSystem.Internal.PedestrianManager.Instance.PedestrianWaypointsDataHandler.IsStop(pedestrianStopWaypoints[l]))
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(Gley.PedestrianSystem.Internal.PedestrianManager.Instance.PedestrianWaypointsDataHandler.GetPosition(pedestrianStopWaypoints[l]), 1);
                        }
                    }
                }
#endif
#endif
            }
        }


        private void DebugVehicleActions(bool speedDebug, bool debugPathFinding, bool debugBehaviours)
        {
            VehicleComponent[] allVehicles = _allVehiclesData.AllVehicles;
            for (int i = 0; i < allVehicles.Length; i++)
            {
                string text = $"{allVehicles[i].ListIndex}: ";
                foreach (var behaviour in _behaviourManager.GetActiveBehaviours()[i])
                {
                    text += behaviour.Key + "\n";
                }

                if (speedDebug)
                {
                    if (allVehicles[i].isActiveAndEnabled)
                    {
                        if (allVehicles[i].MovementInfo.GetWaypointIndex(0) != TrafficSystemConstants.INVALID_WAYPOINT_INDEX)
                        {
                            text += "Current Speed " + allVehicles[i].GetCurrentSpeedMS().ToKMH().ToString("N1") + "\n" +
                            "Follow Speed " + allVehicles[i].MovementInfo.GetFollowSpeed() + "\n" +
                            "Waypoint Speed " + _trafficWaypointsData.AllTrafficWaypoints[allVehicles[i].MovementInfo.GetWaypointIndex(0)].MaxSpeed.ToString("N1") + "\n" +
                            "Max Speed" + allVehicles[i].MaxSpeed.ToKMH().ToString("N1") + "\n";
                        }
                    }
                }

                if (debugBehaviours)
                {
                    string possibleBehavioursText = "";
                    var possibleBehaviours = TrafficManager.Instance.BehaviourManager.GetPossibleBehaviours(allVehicles[i].ListIndex);
                    if (possibleBehaviours != null)
                    {
                        foreach (var possibleBehaviour in possibleBehaviours)
                        {
                            if (possibleBehaviour != null)
                            {
                                possibleBehavioursText += possibleBehaviour.Print("") + "\n";
                            }
                        }
                    }
                    text += "Active Behaviour: " + TrafficManager.Instance.BehaviourManager.GetVehicleBehaviour(allVehicles[i].ListIndex)?.Print("") + "\n" + possibleBehavioursText;
                }


                if (debugPathFinding)
                {
                    var movementParams = allVehicles[i].MovementInfo;
                    if (movementParams.HasPath)
                    {
                        text += "Has Path \n";
                        Queue<int> path = movementParams.CustomPath;
                        foreach (int n in path)
                        {
                            Gizmos.color = Color.red;
                            Vector3 position = _pathFindingData.AllPathFindingWaypoints[n].WorldPosition;
                            Gizmos.DrawWireSphere(position, 1);
                            position.y += 1;
                            Handles.Label(position, allVehicles[i].ListIndex.ToString());
                        }
                    }
                }

                Handles.Label(allVehicles[i].transform.position + new Vector3(1, 1, 1), text);
            }
        }
#endif
#endif
    }
}