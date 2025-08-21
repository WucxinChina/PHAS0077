using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Stores all idle vehicles.
    /// </summary>
    public class IdleVehiclesData
    {
        private readonly List<VehicleComponent> _idleVehicles;

        public List<VehicleComponent> IdleVehicles => _idleVehicles;


        public IdleVehiclesData (List<VehicleComponent> idleVehicles)
        {
            _idleVehicles = idleVehicles;
        }


        public void AddVehicle(VehicleComponent vehicle)
        {
            if (!_idleVehicles.Contains(vehicle))
            {
                if (vehicle.gameObject.activeSelf == false)
                {
                    if (!vehicle.Ignored)
                    {
                        _idleVehicles.Add(vehicle);
                    }
                }
            }
        }


        public void RemoveVehicle(VehicleComponent vehicle)
        {
            _idleVehicles.Remove(vehicle);
        }


        /// <summary>
        /// Get a random index of an idle vehicle
        /// </summary>
        /// <returns></returns>
        public VehicleComponent GetRandomVehicleOfType(VehicleTypes type)
        {
            var possibleVehicles = _idleVehicles.Where(cond => cond.vehicleType == type).ToList();

            if (possibleVehicles.Count > 0)
            {
                return possibleVehicles[Random.Range(0, possibleVehicles.Count)];
            }

            return null;
        }


        public VehicleComponent GetRandomVehicle()
        {
            if (_idleVehicles.Count==0)
            {
                return null;
            }
            return _idleVehicles[Random.Range(0, _idleVehicles.Count)];
        }
    }
}