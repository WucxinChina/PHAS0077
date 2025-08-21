#if GLEY_TRAFFIC_SYSTEM
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Compotes the suspension force for each wheel
    /// </summary>
    [BurstCompile]
    public struct WheelJob : IJobParallelFor
    {
        public NativeArray<float3> WheelSuspensionForce;

        [ReadOnly] public NativeArray<float3> WheelNormalDirection;
        [ReadOnly] public NativeArray<float3> WheelVelocity;
        [ReadOnly] public NativeArray<float> SpringForces;
        [ReadOnly] public NativeArray<float> WheelRayCastDistance;
        [ReadOnly] public NativeArray<float> WheelRadius;
        [ReadOnly] public NativeArray<float> WheelMaxSuspension;
        [ReadOnly] public NativeArray<float> SpringStiffness;
        [ReadOnly] public NativeArray<bool> ExcludedWheels;


        public void Execute(int i)
        {
            if (ExcludedWheels[i] == true)
                return;

            float compression;
            if (WheelMaxSuspension[i] != 0)
            {
                if (WheelRayCastDistance[i] == 0)
                {
                    compression = 0;
                }
                else
                {
                    compression = 1f - (WheelRayCastDistance[i] - WheelRadius[i]) / WheelMaxSuspension[i];
                }
                WheelSuspensionForce[i] = ComputeSuspensionForce(SpringForces[i], compression, WheelNormalDirection[i], i);
            }
            else
            {
                WheelSuspensionForce[i] = ComputeSuspensionForce(SpringForces[i], 1, WheelNormalDirection[i], i);
            }
        }


        float3 ComputeSuspensionForce(float springForce, float compression, float3 normalPoint, int index)
        {
            float damping = WheelVelocity[index].y * springForce / 2;

            float displacement = 0.5f - compression;
            if (displacement > -0.1f)
            {
                displacement = 0;
            }

            float force = ((springForce * (compression / 0.5f) - springForce * SpringStiffness[index] * displacement) * normalPoint).y - damping;
            return new float3(0, force, 0);
        }
    }
}
#endif