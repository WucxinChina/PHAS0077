using Gley.UrbanSystem.Internal;
using System.Collections.Generic;

namespace Gley.TrafficSystem.Internal
{
    public class BlinkersController : IDestroyable
    {
        private readonly MovementInfo _movementInfo;
        private readonly TrafficWaypointsData _waypointsData;
        private readonly IVehicleLightsComponent _vehicleLightsComponent;
        private readonly bool _initialized;

        private BlinkType _currentBlinking;

        public BlinkersController(MovementInfo movementInfo, TrafficWaypointsData waypointsData, IVehicleLightsComponent vehicleLightsComponent)
        {
            Assign();
            _movementInfo = movementInfo;
            _waypointsData = waypointsData;
            _vehicleLightsComponent = vehicleLightsComponent;
            if (_vehicleLightsComponent != null)
            {
                _initialized = true;
            }
        }


        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        public void UpdateBlinkers(BlinkType blinkType)
        {
            if (!_initialized)
            {
                return;
            }

            if (blinkType == BlinkType.Start)
            {
                _currentBlinking = DetermineBlinking(_movementInfo.WaypointIndexes, _waypointsData);
                if (_currentBlinking == BlinkType.None)
                {
                    _vehicleLightsComponent.SetBlinker(BlinkType.Stop);
                }
                else
                {
                    _vehicleLightsComponent.SetBlinker(_currentBlinking);
                }
            }
            else
            {
                if (_currentBlinking != blinkType || _currentBlinking == BlinkType.None)
                {
                    _currentBlinking = blinkType;
                    _vehicleLightsComponent.SetBlinker(BlinkType.Stop);
                }
            }
        }


        public void SetHazardLights(bool activate)
        {
            if (activate)
            {
                _vehicleLightsComponent.SetBlinker(BlinkType.StartHazard);
            }
            else
            {
                _vehicleLightsComponent.SetBlinker(BlinkType.StopHazard);
            }
        }


        private BlinkType DetermineBlinking(List<int> pathWaypointIndexes, TrafficWaypointsData _waypointsData)
        {
            for (int i = 0; i < pathWaypointIndexes.Count; i++)
            {
                if (_waypointsData.AllTrafficWaypoints[pathWaypointIndexes[i]].BlinkType != BlinkType.None)
                {
                    return _waypointsData.AllTrafficWaypoints[pathWaypointIndexes[i]].BlinkType;
                }
            }
            return BlinkType.None;
        }


        public void OnDestroy()
        {

        }
    }
}