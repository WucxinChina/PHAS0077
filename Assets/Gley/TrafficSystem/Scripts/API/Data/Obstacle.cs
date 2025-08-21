using UnityEngine;

namespace Gley.TrafficSystem
{
    public struct Obstacle
    {
        private readonly Collider _collider;
        private readonly bool _isConvex;
        private readonly ObstacleTypes _obstacleType;
        private readonly ITrafficParticipant _vehicleScript;

        internal readonly Collider Collider => _collider;
        internal readonly bool IsConvex => _isConvex;
        internal readonly ObstacleTypes ObstacleType => _obstacleType;
        internal readonly ITrafficParticipant VehicleScript => _vehicleScript;

        public Obstacle(Collider collider, bool isConvex, ObstacleTypes obstacleTypes, ITrafficParticipant vehicleScript)
        {
            _collider = collider;
            _isConvex = isConvex;
            _obstacleType = obstacleTypes;
            _vehicleScript = vehicleScript;
        }
    }
}