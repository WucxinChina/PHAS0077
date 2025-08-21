using Gley.TrafficSystem.Internal;
using UnityEngine;
#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.TrafficSystem
{
    // The base class of any vehicle behaviour
    public abstract class VehicleBehaviour
    {
        private bool _debugBehaviours = false;
        private bool _behaviourIsActive;
#if GLEY_TRAFFIC_SYSTEM
        private bool _startBraking;
#endif

        protected VehicleComponent VehicleComponent { get; private set; }
        protected TrafficWaypointsData TrafficWaypointsData { get; private set; }
        protected AllVehiclesData AllVehiclesData { get; private set; }
        protected int VehicleIndex { get; private set; }

        public string Name { get; }

        public abstract void OnDestroy();
#if GLEY_TRAFFIC_SYSTEM
        public abstract BehaviourResult Execute(MovementInfo knownWaypointsList, float requiredBrakePower, bool stopTargetReached, float3 stopPosition, int currentGear);
#endif

        public VehicleBehaviour()
        {
            Name = GetType().Name;
        }


        public virtual void Initialize(int vehicleIndex, VehicleComponent vehicleComponent, TrafficWaypointsData trafficWaypointsData, AllVehiclesData allVehiclesData)
        {
            VehicleIndex = vehicleIndex;
            VehicleComponent = vehicleComponent;
            TrafficWaypointsData = trafficWaypointsData;
            AllVehiclesData = allVehiclesData;
        }


        /// <summary>
        /// Used to pass additional parameters to the behaviour.
        /// </summary>
        /// <param name="parameters"></param>
        public virtual void SetParams(object[] parameters)
        {

        }


        /// <summary>
        /// Start the execution of the behaviour
        /// </summary>
        public void Start()
        {
#if GLEY_TRAFFIC_SYSTEM
            if (_debugBehaviours)
            {
                Debug.Log($"Can run {Name} for {VehicleIndex}");
            }

            if (_behaviourIsActive == false)
            {
                _startBraking = false;
                _behaviourIsActive = true;
                OnBecomeActive();
                Events.TriggerBehaviourStartedEvent(VehicleIndex, this);
            }
#endif
        }


        /// <summary>
        /// Stop behaviour from executing
        /// </summary>
        public void Stop()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"Can't run {Name} for {VehicleIndex}");
            }

            if (_behaviourIsActive == true)
            {
                _behaviourIsActive = false;
                OnBecameInactive();
                Events.TriggerBehaviourStoppedEvent(VehicleIndex, this);
            }
        }


        /// <summary>
        /// Triggered every time the behaviour is started
        /// </summary>
        protected virtual void OnBecomeActive()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"{Name} became active for {VehicleIndex}");
            }
        }


        /// <summary>
        /// Triggered every time this behaviour is stopped
        /// </summary>
        protected virtual void OnBecameInactive()
        {
            if (_debugBehaviours)
            {
                Debug.Log($"{Name} became inactive for {VehicleIndex}");
            }
        }


        /// <summary>
        /// Set the steer percent
        /// </summary>
        /// <param name="behaviourResult">A reference to the result struct</param>
        /// <param name="steerPercent">the amount of steer to apply [-1 max left, 1 max right]</param>
        protected void Steer(ref BehaviourResult behaviourResult, float steerPercent)
        {
            behaviourResult.SteerPercent = steerPercent;
        }

#if GLEY_TRAFFIC_SYSTEM
        /// <summary>
        /// Calculate the maximum speed and brake for the next frame
        /// </summary>
        /// <param name="behaviourResult">A reference to the result struct</param>
        /// <param name="currentAllowedSpeed">The maximum speed of the current waypoint or maximum engine speed</param>
        /// <param name="futureAllowedSpeed">The target speed</param>
        /// <param name="positionToReachTargetSpeed">The point where the target speed should be reached</param>
        /// <param name="brakePercent">The amount of brake required to slow down to the target speed until the target position is reached</param>
        /// <param name="minBrakePercent">The amount of brake required power required to really start braking</param>
        /// <param name="distanceToStop">if 0 the vehicle stops on the target position with the front wheels. Any positive distance make is stop earlier</param>
        protected void PerformForwardMovement(ref BehaviourResult behaviourResult, float currentAllowedSpeed, float futureAllowedSpeed, float3 positionToReachTargetSpeed, float brakePercent, float minBrakePercent, float distanceToStop)
        {
            behaviourResult.Name = Name;

            // if brake percent is less the vehicle will stop braking 
            if (brakePercent < 0.1f)
            {
                _startBraking = false;
            }

            // if the brake percent is greater than the brake threshold, start to apply the braked
            if (!_startBraking)
            {
                if (brakePercent >= minBrakePercent)
                {
                    _startBraking = true;
                }
            }

            // else do not apply any brake
            if (!_startBraking)
            {
                brakePercent = 0;
            }

            // set values
            behaviourResult.BrakePercent = brakePercent;
            behaviourResult.MaxAllowedSpeed = currentAllowedSpeed;
            behaviourResult.SpeedInPoint = new TargetSpeedPoint(positionToReachTargetSpeed, futureAllowedSpeed, distanceToStop, Name);
        }
#endif
    }
}
