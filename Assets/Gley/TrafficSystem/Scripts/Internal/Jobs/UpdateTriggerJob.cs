#if GLEY_TRAFFIC_SYSTEM
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Rotates the vehicle trigger on the heading direction
    /// </summary>
    [BurstCompile]
    public struct UpdateTriggerJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float> TurnAngle;
        [ReadOnly] public NativeArray<bool> ExcludedVehicles;

        public void Execute(int index, TransformAccess transform)
        {
            if (ExcludedVehicles[index] == true)
                return;
            transform.localRotation = quaternion.EulerZXY(0, math.radians(TurnAngle[index]), 0);
        }
    }
}
#endif