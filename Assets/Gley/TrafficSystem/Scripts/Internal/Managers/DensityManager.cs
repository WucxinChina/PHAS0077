#if GLEY_TRAFFIC_SYSTEM
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Controls the number of active vehicles
    /// </summary>
    public class DensityManager
    {
        private readonly List<VehicleRequest> _requestedVehicles;
        private readonly AllVehiclesData _allVehiclesData;
        private readonly IdleVehiclesData _idleVehiclesData;
        private readonly PositionValidator _positionValidator;
        private readonly WaypointSelector _waypointSelector;
        private readonly TrafficWaypointsData _trafficWaypointsData;
        private readonly bool _useWaypointPriority;
        private readonly bool _debugDensity;

        private int _maxNrOfVehicles;
        private int _currentNrOfVehicles;
        private int _activeSquaresLevel;
        private int _newVehiclesNeeded;




        private float DespawnDistanceSqr = TrafficSettings.DistanceToRemove * TrafficSettings.DistanceToRemove;






        private class VehicleRequest
        {
            public UnityAction<VehicleComponent, int> CompleteMethod { get; set; }
            public List<int> Path { get; set; }
            public VehicleComponent Vehicle { get; set; }
            public VehicleTypes Type { get; set; }
            public Category Category { get; set; }
            public TrafficWaypoint Waypoint { get; set; }
            public bool IgnoreLOS { get; set; }
            public Quaternion TrailerRotation { get; set; }


            internal VehicleRequest(TrafficWaypoint waypoint, VehicleTypes type, Category category, VehicleComponent vehicle, UnityAction<VehicleComponent, int> completeMethod, List<int> path, bool ignoreLos)
            {
                Waypoint = waypoint;
                Type = type;
                Category = category;
                Vehicle = vehicle;
                CompleteMethod = completeMethod;
                Path = path;
                IgnoreLOS = ignoreLos;
                TrailerRotation = Quaternion.identity;
            }
        }


        private enum Category
        {
            Random,
            User,
        }


        public DensityManager(AllVehiclesData allVehiclesData, TrafficWaypointsData trafficWaypointsData, PositionValidator positionValidator, NativeArray<float3> activeCameraPositions, int maxNrOfVehicles, Vector3 playerPosition, Vector3 playerDirection, int activeSquaresLevel, bool useWaypointPriority, int initialDensity, bool debugDensity, WaypointSelector waypointSelector)
        {
            _positionValidator = positionValidator;
            _allVehiclesData = allVehiclesData;
            _activeSquaresLevel = activeSquaresLevel;
            _maxNrOfVehicles = maxNrOfVehicles;
            _useWaypointPriority = useWaypointPriority;
            _trafficWaypointsData = trafficWaypointsData;
            _requestedVehicles = new List<VehicleRequest>();
            _debugDensity = debugDensity;
            _waypointSelector = waypointSelector;

            //disable loaded vehicles
            var idleVehicles = new List<VehicleComponent>();
            for (int i = 0; i < maxNrOfVehicles; i++)
            {
                var vehicle = _allVehiclesData.GetVehicle(i);
                if (!vehicle.Ignored)
                {
                    idleVehicles.Add(vehicle);
                }
            }

            _idleVehiclesData = new IdleVehiclesData(idleVehicles);

            if (initialDensity >= 0)
            {
                SetTrafficDensity(initialDensity);
            }

            //load initial vehicles
            for (int i = 0; i < _maxNrOfVehicles; i++)
            {
                RequestRandomVehicle(playerPosition, playerDirection, true, activeCameraPositions[UnityEngine.Random.Range(0, activeCameraPositions.Length)]);
            }
            UpdateVehicleDensity(default, default, default);
            ClearUninstantiatedRequests();
        }


        /// <summary>
        /// Ads new vehicles if required
        /// </summary>
        public void UpdateVehicleDensity(Vector3 playerPosition, Vector3 playerDirection, Vector3 activeCameraPosition)
        {


            // ② 先尽量填满 priority==100 的点（受 grid/密度限制）
            RequestVehiclesOnPriority100InGrid(activeCameraPosition, playerPosition, playerDirection);

            // ③ 再按原方案补足密度（非100保持原逻辑）
            _newVehiclesNeeded = _maxNrOfVehicles - _currentNrOfVehicles;

            if (_newVehiclesNeeded > 0)
            {
                if (_newVehiclesNeeded > _requestedVehicles.Count)
                {
                    RequestRandomVehicle(playerPosition, playerDirection, false, activeCameraPosition);
                }

                for (int i = _requestedVehicles.Count - 1; i >= 0; i--)
                {
                    if (RequestIsValid(_requestedVehicles[i]))
                    {
                        InstantiateVehicle(_requestedVehicles[i]);
                    }
                }
            }
        }



        public void RequestIgnoredVehicle(int vehicleIndex, Vector3 position, UnityAction<VehicleComponent, int> completeMethod)
        {
            if (position == Vector3.zero)
            {
                return;
            }

            if (!_allVehiclesData.AllVehicles[vehicleIndex].Ignored)
            {
                Debug.LogWarning($"vehicleIndex {vehicleIndex} is not marked as ignored, it will not be instantiated");
                return;
            }
            VehicleComponent vehicle = _allVehiclesData.AllVehicles[vehicleIndex];
            VehicleTypes type = vehicle.VehicleType;
            var waypoint = _waypointSelector.GetClosestSpawnWaypoint(position, type);
            if (waypoint != null)
            {
                _requestedVehicles.Add(new VehicleRequest(waypoint, type, Category.User, _allVehiclesData.AllVehicles[vehicleIndex], completeMethod, null, true));
            }
            else
            {
                Debug.LogWarning("No waypoint found!");
            }
        }


        public void RequestVehicleAtPosition(Vector3 position, VehicleTypes type, UnityAction<VehicleComponent, int> completeMethod, List<int> path)
        {
            var waypoint = _waypointSelector.GetClosestSpawnWaypoint(position, type);

            if (waypoint == null)
            {
                Debug.LogWarning("There are no free waypoints in the current cell");
                return;
            }

            _requestedVehicles.Add(new VehicleRequest(waypoint, type, Category.User, null, completeMethod, path, true));
        }


        public void InstantiateTrafficVehicle(int vehicleIndex, Vector3 vehiclePosition, Quaternion vehicleRotation, Vector3 initialVelocity, Vector3 initialAngularVelocity, int nextWaypointIndex)
        {
            if (_allVehiclesData.IsVehicleIndexValid(vehicleIndex))
            {
                RemoveVehicle(vehicleIndex, true);
                InstantiateVehicle(vehicleIndex, nextWaypointIndex, vehiclePosition, vehicleRotation, initialVelocity, initialAngularVelocity);
            }
            else
            {
                Debug.LogError($"Vehicle index {vehicleIndex} is invalid. It should be between 0 and {_allVehiclesData.AllVehicles.Length}");
            }
        }


        /// <summary>
        /// Update the active camera used to determine if a vehicle is in view
        /// </summary>
        /// <param name="activeCamerasPosition"></param>
        public void UpdateCameraPositions(Transform[] activeCameras)
        {
            _positionValidator.UpdateCamera(activeCameras);
        }


        public void IgnoreVehicle(int vehicleIndex)
        {
            _allVehiclesData.AllVehicles[vehicleIndex].Ignored = true;
            _idleVehiclesData.RemoveVehicle(_allVehiclesData.GetVehicle(vehicleIndex));
        }


        public void RestoreIgnoredVehicle(int vehicleIndex)
        {
            _allVehiclesData.AllVehicles[vehicleIndex].Ignored = false;
            _idleVehiclesData.AddVehicle(_allVehiclesData.GetVehicle(vehicleIndex));
        }


        /// <summary>
        /// Remove a specific vehicle from the scene
        /// </summary>
        /// <param name="index">index of the vehicle to remove</param>
        public void RemoveVehicle(GameObject vehicle)
        {
            int index = _allVehiclesData.GetVehicleIndex(vehicle);
            if (index != TrafficSystemConstants.INVALID_VEHICLE_INDEX)
            {
                RemoveVehicle(index, true);
            }
            else
            {
                Debug.Log($"Vehicle {vehicle} not found");
            }
        }


        /// <summary>
        /// Remove a specific vehicle from the scene
        /// </summary>
        /// <param name="vehicleIndex">index of the vehicle to remove</param>
        public void RemoveVehicle(int vehicleIndex, bool force)
        {
            if ((_allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.HasPath || _allVehiclesData.AllVehicles[vehicleIndex].MovementInfo.DontRemove) && force == false)
            {
                return;
            }
            _allVehiclesData.RemoveVehicle(vehicleIndex);
            _idleVehiclesData.AddVehicle(_allVehiclesData.GetVehicle(vehicleIndex));
            _currentNrOfVehicles--;
            Events.TriggerVehicleDisabledEvent(vehicleIndex);
        }


        /// <summary>
        /// Removes the vehicles on a given circular area
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// param name="condition">if null all vehicles will be removed</param>
        public void ClearTrafficOnArea(Vector3 center, float radius, System.Func<VehicleComponent, bool> condition = null)
        {
            float sqrRadius = radius * radius;
            for (int i = 0; i < _allVehiclesData.AllVehicles.Length; i++)
            {
                var vehicle = _allVehiclesData.AllVehicles[i];
                if (vehicle.gameObject.activeSelf)
                {
                    if (math.distancesq(center, vehicle.transform.position) < sqrRadius)
                    {
                        if (condition == null || condition(vehicle))
                        {
                            RemoveVehicle(i, true);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Change vehicle density
        /// </summary>
        /// <param name="nrOfVehicles">cannot be greater than max vehicle number set on initialize</param>
        public void SetTrafficDensity(int nrOfVehicles)
        {
            _maxNrOfVehicles = nrOfVehicles;
        }


        public void UpdateActiveSquaresLevel(int newLevel)
        {
            _activeSquaresLevel = newLevel;
        }


        private void InstantiateVehicle(VehicleRequest request)
        {
            _requestedVehicles.Remove(request);
            request.CompleteMethod?.Invoke(request.Vehicle, request.Waypoint.ListIndex);
            if (request.Path != null)
            {
                _allVehiclesData.AllVehicles[request.Vehicle.ListIndex].MovementInfo.SetPath(request.Path);
            }

            _currentNrOfVehicles++;
            request.Vehicle.ActivateVehicle(request.Waypoint.Position, _trafficWaypointsData.GetNextOrientation(request.Waypoint), request.TrailerRotation);
            _idleVehiclesData.RemoveVehicle(request.Vehicle);
            Events.TriggerVehicleActivatedEvent(request.Vehicle.ListIndex, request.Waypoint.ListIndex);
        }


        private void InstantiateVehicle(int vehicleIndex, int targetWaypointIndex, Vector3 position, Quaternion rotation, Vector3 initialVelocity, Vector3 initialAngularVelocity)
        {
            var vehicleComponent = _allVehiclesData.GetVehicle(vehicleIndex);
            vehicleComponent.ActivateVehicle(position, rotation, Quaternion.identity);
            vehicleComponent.SetVelocity(initialVelocity, initialAngularVelocity);
            _idleVehiclesData.RemoveVehicle(vehicleComponent);
            Events.TriggerVehicleActivatedEvent(vehicleIndex, targetWaypointIndex);
        }


        private void ClearUninstantiatedRequests()
        {
            for (int i = _requestedVehicles.Count - 1; i >= 0; i--)
            {
                ClearRequest(_requestedVehicles[i]);
            }
        }


        private void ClearRequest(VehicleRequest request)
        {
            _idleVehiclesData.AddVehicle(request.Vehicle);
            _requestedVehicles.Remove(request);
        }


        private bool RequestIsValid(VehicleRequest request)
        {
            if (request.Vehicle == null)
            {
                var vehicleComponent = _idleVehiclesData.GetRandomVehicleOfType(request.Type);
                //if an idle vehicle does not exists
                if (vehicleComponent == null)
                {
                    if (_debugDensity)
                    {
                        Debug.Log($"Density: No vehicle of type {request.Type} is idle");
                    }
                    return false;
                }
                request.Vehicle = vehicleComponent;
                _idleVehiclesData.RemoveVehicle(vehicleComponent);
            }

            if (request.Vehicle.gameObject.activeSelf)
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: already active");
                }
                return false;
            }

            //if a valid waypoint was found, check if it was not manually disabled
            if (request.Waypoint.TemporaryDisabled)
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: waypoint is disabled");
                }
                return false;
            }

            //check if the car type can be instantiated on selected waypoint
            if (!_positionValidator.IsValid(request.Waypoint.Position, request.Vehicle.length * 2, request.Vehicle.coliderHeight, request.Vehicle.ColliderWidth, request.IgnoreLOS, request.Vehicle.frontTrigger.localPosition.z, _trafficWaypointsData.GetNextOrientation(request.Waypoint)))
            {
                if (request.Category == Category.Random)
                {
                    ClearRequest(request);
                }
                return false;
            }

            if (request.Vehicle.trailer != null)
            {
                if (request.TrailerRotation == Quaternion.identity)
                {
                    request.TrailerRotation = _trafficWaypointsData.GetPrevOrientation(request.Waypoint);
                    if (request.TrailerRotation == Quaternion.identity)
                    {
                        request.TrailerRotation = _trafficWaypointsData.GetNextOrientation(request.Waypoint);
                    }
                }

                if (!_positionValidator.CheckTrailerPosition(request.Waypoint.Position, _trafficWaypointsData.GetNextOrientation(request.Waypoint), request.TrailerRotation, request.Vehicle))
                {
                    return false;
                }
            }
            return true;

        }


        private void RequestRandomVehicle(Vector3 position, Vector3 direction, bool ignorLOS, Vector3 cameraPosition)
        {
            //add any vehicle on area
            var vehicle = _idleVehiclesData.GetRandomVehicle();

            //if an idle vehicle does not exists
            if (vehicle == null)
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: No idle vehicle found");
                }
                return;
            }

            var waypoint = _waypointSelector.GetAFreeWaypoint(cameraPosition, _activeSquaresLevel, vehicle.VehicleType, position, direction, _useWaypointPriority);

            if (waypoint == null)
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: No free waypoint found");
                }
                return;
            }

            // 🚗 全局安全距离检测
            float globalSafeDistance = TrafficSettings.MinSpawnDistance;
            bool tooClose = false;
            var allVehicles = _allVehiclesData.AllVehicles;
            foreach (var v in allVehicles)
            {
                if (v == null || !v.gameObject.activeSelf) continue;

                float dist = Vector3.Distance(waypoint.Position, v.FrontPosition.position);
                if (dist < globalSafeDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
            {
                if (_debugDensity)
                {
                    Debug.Log($"Density: waypoint skipped, vehicle too close (<{globalSafeDistance}m)");
                }
                return;
            }

            if (IsTooCloseToPlayer(waypoint.Position, position))  // position 就是 playerPosition
            {
                if (_debugDensity)
                {
                    Debug.Log($"Density: waypoint skipped, too close to player (<{TrafficSettings.MinDistanceToAdd}m)");
                }
                return;
            }





            //if a valid waypoint was found, check if it was not manually disabled
            if (waypoint.TemporaryDisabled)
            {
                if (_debugDensity)
                {
                    Debug.Log("Density: waypoint is disabled");
                }
                return;
            }

            //check if the car type can be instantiated on selected waypoint
            if (!_positionValidator.IsValid(waypoint.Position, vehicle.length * 2, vehicle.coliderHeight, vehicle.ColliderWidth, ignorLOS, vehicle.frontTrigger.localPosition.z, _trafficWaypointsData.GetNextOrientation(waypoint)))
            {
                return;
            }

            Quaternion trailerRotaion = Quaternion.identity;
            if (vehicle.trailer != null)
            {
                trailerRotaion = _trafficWaypointsData.GetPrevOrientation(waypoint);
                if (trailerRotaion == Quaternion.identity)
                {
                    trailerRotaion = _trafficWaypointsData.GetNextOrientation(waypoint);
                }

                if (!_positionValidator.CheckTrailerPosition(waypoint.Position, _trafficWaypointsData.GetNextOrientation(waypoint), trailerRotaion, vehicle))
                {
                    return;
                }
            }

            _idleVehiclesData.RemoveVehicle(vehicle);
            _requestedVehicles.Add(new VehicleRequest(waypoint, vehicle.VehicleType, Category.Random, vehicle, null, null, ignorLOS));
        }


        private void RequestVehiclesOnPriority100InGrid(
    Vector3 cameraPosition, Vector3 playerPosition, Vector3 playerDirection)
        {
            // 还能再放多少车（不会超过 _maxNrOfVehicles）
            int slotsLeft = _maxNrOfVehicles - (_currentNrOfVehicles + _requestedVehicles.Count);
            if (slotsLeft <= 0) return;

            // 给“抽路点”的循环设个上限，防止极端情况下死循环
            int safety = 200;

            while (slotsLeft > 0 && safety-- > 0)
            {
                // 取一辆空闲车；如果没有空闲车，就停止
                var vehicle = _idleVehiclesData.GetRandomVehicle();
                if (vehicle == null) break;

                // 让选择器在当前格网内找一个可用的路点
                // 注意：最后一个参数传 true，强制把“使用优先级”打开
                var wp = _waypointSelector.GetAFreeWaypoint(
           cameraPosition, _activeSquaresLevel, vehicle.VehicleType,
           playerPosition, playerDirection, /*useWaypointPriority*/ true);

                if (wp == null)
                    break; // 当前格网没有可用路点

                // 只对 priority==100 的点生成；其他优先级直接跳过，继续抽下一个
                if (!IsPriority100(wp))
                    continue;

                if (wp.TemporaryDisabled)
                    continue;

                // 全局最小生成间距检查（避免与现有车重叠）
                float minDist = TrafficSettings.MinSpawnDistance;
                float minDistSqr = minDist * minDist;
                var all = _allVehiclesData.AllVehicles;
                bool tooClose = false;
                for (int i = 0; i < all.Length; i++)
                {
                    var v = all[i];
                    if (v == null || !v.gameObject.activeSelf) continue;
                    if ((v.transform.position - wp.Position).sqrMagnitude < minDistSqr)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                if (IsTooCloseToPlayer(wp.Position, playerPosition))
                {
                    if (_debugDensity)
                    {
                        Debug.Log($"Density: waypoint skipped, too close to player (<{TrafficSettings.MinDistanceToAdd}m)");
                    }
                    continue;
                }

                // 位置/尺寸/视线等有效性检查（与原逻辑一致）
                if (!_positionValidator.IsValid(
                        wp.Position,
                        vehicle.length * 2,
                        vehicle.coliderHeight,
                        vehicle.ColliderWidth,
                        /*ignoreLOS*/ true,
                        vehicle.frontTrigger.localPosition.z,
                        _trafficWaypointsData.GetNextOrientation(wp)))
                {
                    continue;
                }

                // 挂车检查（如有）
                Quaternion trailerRot = Quaternion.identity;
                if (vehicle.trailer != null)
                {
                    trailerRot = _trafficWaypointsData.GetPrevOrientation(wp);
                    if (trailerRot == Quaternion.identity)
                        trailerRot = _trafficWaypointsData.GetNextOrientation(wp);

                    if (!_positionValidator.CheckTrailerPosition(
                            wp.Position,
                            _trafficWaypointsData.GetNextOrientation(wp),
                            trailerRot,
                            vehicle))
                    {
                        continue;
                    }
                }

                // 通过全部校验 → 入队创建
                _idleVehiclesData.RemoveVehicle(vehicle);
                _requestedVehicles.Add(new VehicleRequest(
                    wp,
                    vehicle.VehicleType,
                    Category.Random,   // 走原有管线即可
                    vehicle,
                    null,
                    null,
                    /*ignoreLOS*/ true));

                slotsLeft--;
            }
        }


        private static bool TryGetPriority(object waypoint, out int priority)
        {
            priority = 0;
            if (waypoint == null) return false;

            Type t = waypoint.GetType();

            // 先找属性
            string[] propNames = { "Priority", "priority", "WaypointPriority", "priorityLevel", "PriorityLevel" };
            foreach (var name in propNames)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanRead)
                {
                    object val = p.GetValue(waypoint);
                    if (val == null) return false;
                    priority = Convert.ToInt32(val);
                    return true;
                }
            }

            // 再找字段
            string[] fieldNames = { "Priority", "priority", "WaypointPriority", "priorityLevel", "PriorityLevel" };
            foreach (var name in fieldNames)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    object val = f.GetValue(waypoint);
                    if (val == null) return false;
                    priority = Convert.ToInt32(val);
                    return true;
                }
            }

            return false;
        }

        // 是否为优先级 100
        private static bool IsPriority100(TrafficWaypoint wp)
        {
            return TryGetPriority(wp, out int prio) && prio == 100;
        }

        private bool IsTooCloseToPlayer(Vector3 wpPos, Vector3 playerPos)
        {
            float sqr = (wpPos - playerPos).sqrMagnitude;
            return sqr < TrafficSettings.MinDistanceToAdd * TrafficSettings.MinDistanceToAdd;
        }



    }
}
#endif