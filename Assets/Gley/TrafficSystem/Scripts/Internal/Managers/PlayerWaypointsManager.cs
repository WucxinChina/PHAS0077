using Gley.UrbanSystem.Internal;
using System.Collections.Generic;

namespace Gley.TrafficSystem.Internal
{
    public class PlayerWaypointsManager : IDestroyable
    {
        private readonly Dictionary<int, int> _playerTarget; // PlayerID -> WaypointIndex
        private readonly HashSet<int> _targetWaypoints; // Stores waypoint indices that are targets

        public PlayerWaypointsManager()
        {
            Assign();
            _playerTarget = new Dictionary<int, int>();
            _targetWaypoints = new HashSet<int>();
        }


        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        public void RegisterPlayer(int id, int waypointIndex)
        {
            if (!_playerTarget.ContainsKey(id))
            {
                _playerTarget[id] = waypointIndex;
                _targetWaypoints.Add(waypointIndex);
            }
        }


        public void UpdatePlayerWaypoint(int id, int newWaypointIndex)
        {
            if (_playerTarget.TryGetValue(id, out int oldWaypointIndex))
            {
                _targetWaypoints.Remove(oldWaypointIndex); // Remove old waypoint
            }

            _playerTarget[id] = newWaypointIndex;
            _targetWaypoints.Add(newWaypointIndex); // Add new waypoint
        }


        public bool IsThisWaypointIndexATarget(int waypointIndex)
        {
            return _targetWaypoints.Contains(waypointIndex); // O(1) lookup time
        }


        public void OnDestroy()
        {
            _playerTarget.Clear();
            _targetWaypoints.Clear();
        }
    }
}