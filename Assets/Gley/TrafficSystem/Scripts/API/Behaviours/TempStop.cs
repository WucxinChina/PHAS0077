#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gley.TrafficSystem
{
    // Stop and wait a predefined time then resume driving
    public class TempStop : VehicleBehaviour
    {
#if GLEY_TRAFFIC_SYSTEM
        const float _maxStopTime = 10;

        private float _stopTime;
        private float _currentTime;

        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            // set a random waiting time
            _currentTime = 0;
            _stopTime = Random.Range(_maxStopTime / 2, _maxStopTime);
        }


        public override BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear)
        {
            var BehaviourResult = new BehaviourResult();

            // stop the vehicle
            PerformForwardMovement(ref BehaviourResult, 0, 0, TrafficSystemConstants.DEFAULT_POSITION, 1, 1, 0);

            // if speed is close to 0 register stop time
            if (VehicleComponent.GetCurrentSpeedMS() < 1)
            {
                _currentTime += Time.deltaTime;
            }

            // if stop time is greater than the threshold, stop this behaviour 
            if (_currentTime > _stopTime)
            {
                Stop();
            }

            return BehaviourResult;
        }
#endif

        public override void OnDestroy()
        {

        }
    }
}