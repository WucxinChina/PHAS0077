using Gley.UrbanSystem.Internal;
using System.Collections.Generic;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Base class for all intersections
    /// </summary>
    [System.Serializable]
    public abstract class GenericIntersection : IIntersection
    {
        protected List<int> _carsInIntersection;

        #region InterfactImplementation
        public abstract bool IsPathFree(int waypointIndex);

        public void VehicleEnter(int vehicleIndex)
        {
            _carsInIntersection.Add(vehicleIndex);
        }

        public void VehicleLeft(int vehicleIndex)
        {
            _carsInIntersection.Remove(vehicleIndex);
        }

        public abstract void PedestrianPassed(int agentIndex);
        #endregion

        public abstract void UpdateIntersection(float realtimeSinceStartup);

        public abstract int[] GetPedStopWaypoint();

        public abstract string GetName();

        public abstract List<int> GetStopWaypoints();

        public void RemoveVehicle(int index)
        {
            VehicleLeft(index);
        }

        public virtual void ResetIntersection()
        {
            _carsInIntersection = new List<int>();
        }
    }
}