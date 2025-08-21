using Gley.UrbanSystem.Internal;
using System.Collections.Generic;

namespace Gley.TrafficSystem.Internal
{
    public class DisabledWaypointsManager : IDestroyable
    {
        private readonly TrafficWaypointsData _trafficWaypointsData;
        private readonly WaypointSelector _waypointSelector;

        private List<int> _disabledWaypoints;

        public List<int> DisabledWaypoints => _disabledWaypoints;

        public DisabledWaypointsManager(TrafficWaypointsData trafficWaypointsData, WaypointSelector waypointSelector, Area area)
        {
            Assign();
            _disabledWaypoints = new List<int>();
            _trafficWaypointsData = trafficWaypointsData;
            _waypointSelector = waypointSelector;
            if (area.radius > 0)
            {
                DisableAreaWaypoints(area);
            }
        }


        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        public void EnableAllWaypoints()
        {
            foreach (var waypointIndex in _disabledWaypoints)
            {
                _trafficWaypointsData.AllTrafficWaypoints[waypointIndex].TemporaryDisabled = false;
            }
            _disabledWaypoints = new List<int>();
        }


        /// <summary>
        /// Mark a waypoint as disabled
        /// </summary>
        /// <param name="waypointIndex"></param>
        public void AddDisabledWaypoint(int waypointIndex)
        {
            _disabledWaypoints.Add(waypointIndex);
            _trafficWaypointsData.AllTrafficWaypoints[waypointIndex].TemporaryDisabled = true;
        }


        public void DisableAreaWaypoints(Area area)
        {
            var waypoints = _waypointSelector.GetAreaWaypoints(area);
            foreach (var waypoint in waypoints)
            {
                AddDisabledWaypoint(waypoint);
            }
        }


        public void OnDestroy()
        {

        }
    }
}