#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // Stores a point and the required speed in that point
    public struct TargetSpeedPoint
    {
#if GLEY_TRAFFIC_SYSTEM
        public float3 StopPosition { get; set; }
#endif
        public string Name { get; set; }
        public float TargetSpeed { get; set; }
        public float DistanceToStop { get; set; }

#if GLEY_TRAFFIC_SYSTEM
        public TargetSpeedPoint(float3 stopPosition, float targetSpeed, float distanceToStop, string name)
        {
            StopPosition = stopPosition;
            TargetSpeed = targetSpeed;
            DistanceToStop = distanceToStop;
            Name = name;
        }
#endif
    }
}
