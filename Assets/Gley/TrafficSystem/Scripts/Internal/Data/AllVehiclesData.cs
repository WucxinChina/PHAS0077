using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Stores all instantiated vehicles.
    /// </summary>
    public class AllVehiclesData
    {
        private readonly VehicleComponent[] _allVehicles;

        public VehicleComponent[] AllVehicles => _allVehicles;


        public AllVehiclesData(Transform parent, VehiclePool vehiclePool, int nrOfVehicles, LayerMask buildingLayers, LayerMask obstacleLayers, LayerMask playerLayers, LayerMask roadLayers, bool lightsOn, ModifyTriggerSize modifyTriggerSize, TrafficWaypointsData trafficWaypointsData, float minOffset, float maxOffset)
        {
            var trafficHolder = MonoBehaviourUtilities.CreateGameObject(TrafficSystemConstants.TrafficHolderName, parent, parent.position, false).transform;
            _allVehicles = new VehicleComponent[nrOfVehicles];
            int currentVehicleIndex = 0;

            // Transform percent into numbers.
            int carsToInstantiate = vehiclePool.trafficCars.Length;
            if (carsToInstantiate > nrOfVehicles)
            {
                carsToInstantiate = nrOfVehicles;
            }
            // Instantiate at least a car from each type.
            for (int i = 0; i < carsToInstantiate; i++)
            {
                _allVehicles[currentVehicleIndex] = LoadVehicle(currentVehicleIndex, vehiclePool.trafficCars[i].VehiclePrefab, trafficHolder, buildingLayers, obstacleLayers, playerLayers, roadLayers, vehiclePool.trafficCars[i].Ignore, lightsOn, modifyTriggerSize, trafficWaypointsData,minOffset,maxOffset);
                currentVehicleIndex++;
            }

            nrOfVehicles -= carsToInstantiate;
            float sum = 0;
            List<float> thresholds = new List<float>();
            for (int i = 0; i < vehiclePool.trafficCars.Length; i++)
            {
                sum += vehiclePool.trafficCars[i].Percent;
                thresholds.Add(sum);
            }
            float perCarValue = sum / nrOfVehicles;

            // instantiate remaining vehicles.
            int vehicleIndex = 0;
            for (int i = 0; i < nrOfVehicles; i++)
            {
                while ((i + 1) * perCarValue > thresholds[vehicleIndex])
                {
                    vehicleIndex++;
                    if (vehicleIndex >= vehiclePool.trafficCars.Length)
                    {
                        vehicleIndex = vehiclePool.trafficCars.Length - 1;
                        break;
                    }
                }
                _allVehicles[currentVehicleIndex] = LoadVehicle(currentVehicleIndex, vehiclePool.trafficCars[vehicleIndex].VehiclePrefab, trafficHolder, buildingLayers, obstacleLayers, playerLayers, roadLayers, vehiclePool.trafficCars[vehicleIndex].Ignore, lightsOn, modifyTriggerSize, trafficWaypointsData, minOffset, maxOffset);
                currentVehicleIndex++;
            }
        }


        /// <summary>
        /// Load vehicle in scene
        /// </summary>
        private VehicleComponent LoadVehicle(int vehicleIndex, GameObject carPrefab, Transform parent, LayerMask buildingLayers, LayerMask obstacleLayers, LayerMask playerLayers, LayerMask roadLayers, bool ignored, bool lightsOn, ModifyTriggerSize modifyTriggerSize, TrafficWaypointsData trafficWaypointsData, float minOffset, float maxOffset)
        {
            VehicleComponent vehicle = MonoBehaviourUtilities.Instantiate(carPrefab, Vector3.zero, Quaternion.identity, parent).GetComponent<VehicleComponent>().
                Initialize(buildingLayers, obstacleLayers, playerLayers, roadLayers, lightsOn, modifyTriggerSize, trafficWaypointsData, vehicleIndex, ignored, minOffset, maxOffset);
            vehicle.name += vehicleIndex;
            vehicle.DeactivateVehicle();
            return vehicle;
        }


        public int GetTotalWheels()
        {
            int totalWheels = 0;
            for (int i = 0; i < _allVehicles.Length; i++)
            {
                totalWheels += _allVehicles[i].allWheels.Length;
                if (_allVehicles[i].trailer != null)
                {
                    totalWheels += _allVehicles[i].trailer.allWheels.Length;
                }
            }
            return totalWheels;
        }


        /// <summary>
        /// Update main lights of the vehicle
        /// </summary>
        /// <param name="on"></param>
        public void UpdateVehicleLights(bool on)
        {
            for (int i = 0; i < _allVehicles.Length; i++)
            {
                _allVehicles[i].SetMainLights(on);
            }
        }


        public int GetVehicleIndex(GameObject vehicle)
        {
            for (int i = 0; i < _allVehicles.Length; i++)
            {
                if (_allVehicles[i].gameObject == vehicle)
                {
                    return i;
                }
            }
            return TrafficSystemConstants.INVALID_VEHICLE_INDEX;
        }


        public VehicleComponent GetVehicle(int vehicleIndex)
        {
            if (IsVehicleIndexValid(vehicleIndex))
            {
                return _allVehicles[vehicleIndex];
            }
            return null;
        }


        public bool IsVehicleIndexValid(int vehicleIndex)
        {
            if (vehicleIndex < 0)
            {
                Debug.LogError($"Vehicle index {vehicleIndex} should be >= 0");
                return false;
            }

            if (vehicleIndex >= _allVehicles.Length)
            {
                Debug.LogError($"Vehicle index {vehicleIndex} should be < {_allVehicles.Length}");
                return false;
            }

            if (_allVehicles[vehicleIndex] == null)
            {
                Debug.LogError($"Vehicle at {vehicleIndex} is null, Verify the setup");
                return false;
            }
            return true;
        }


        public List<VehicleComponent> GetIgnoredVehicleList()
        {
            var result = new List<VehicleComponent>();
            for (int i = 0; i < _allVehicles.Length; i++)
            {
                if (_allVehicles[i].Ignored)
                {
                    result.Add(_allVehicles[i]);
                }
            }
            return result;
        }


        public int GetIgnoredVehicleIndex(GameObject vehicle)
        {
            for (int i = 0; i < _allVehicles.Length; i++)
            {
                if (_allVehicles[i].Ignored)
                {
                    if (_allVehicles[i].gameObject == vehicle)
                    {
                        return i;
                    }
                }
            }
            return TrafficSystemConstants.INVALID_VEHICLE_INDEX;
        }


        public void TriggerColliderRemovedEvent(Collider[] colliders)
        {
            for (int i = 0; i < _allVehicles.Length; i++)
            {
                _allVehicles[i].ColliderRemoved(colliders);
            }
        }


        /// <summary>
        /// Remove the given vehicle from scene
        /// </summary>
        /// <param name="vehicleIndex"></param>
        public void RemoveVehicle(int vehicleIndex)
        {
            _allVehicles[vehicleIndex].DeactivateVehicle();

            for (int i = 0; i < _allVehicles.Length; i++)
            {
                _allVehicles[i].ColliderRemoved(_allVehicles[vehicleIndex].AllColliders);
            }
        }


        public void ModifyTriggerSize(int vehicleIndex, ModifyTriggerSize modifyTriggerSizeDelegate)
        {
            if (vehicleIndex < 0)
            {
                for (int i = 0; i < _allVehicles.Length; i++)
                {
                    _allVehicles[i].SetTriggerSizeModifierDelegate(modifyTriggerSizeDelegate);
                }
            }
            else
            {
                _allVehicles[vehicleIndex].SetTriggerSizeModifierDelegate(modifyTriggerSizeDelegate);
            }
        }
    }
}