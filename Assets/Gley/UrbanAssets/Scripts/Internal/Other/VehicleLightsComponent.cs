using UnityEngine;
namespace Gley.UrbanSystem.Internal
{
    /// <summary>
    /// Used to control vehicle lights if needed
    /// Light objects are enabled or disabled based on car actions 
    /// Not all lights are mandatory
    /// </summary>
    public class VehicleLightsComponent : MonoBehaviour, IVehicleLightsComponent
    {
        [Tooltip("Blinking interval")]
        public float blinkTime = 0.5f;
        [Tooltip("A GameObject containing all main lights - will be active based on Manager API calls")]
        public GameObject frontLights;
        [Tooltip("A GameObject containing all reverse lights - will be active if a vehicle is reversing")]
        public GameObject reverseLights;
        [Tooltip("A GameObject containing all rear lights - will be active if main lights are active")]
        public GameObject rearLights;
        [Tooltip("A GameObject containing all brake lights - will be active when a vehicle is braking")]
        public GameObject stopLights;
        [Tooltip("A GameObject containing all blinker left lights - will be active when car turns left")]
        public GameObject blinkerLeft;
        [Tooltip("A GameObject containing all blinker right lights - will be active when car turns right")]
        public GameObject blinkerRight;

        private float _currentTime;
        private bool _updateBlinkers;
        private bool _updateLeftBlinker;
        private bool _updateRightBlinker;
        private BlinkType _currentBlinkType;


        /// <summary>
        /// Initialize the component if required
        /// </summary>
        public void Initialize()
        {
            _currentTime = 0;
            LightsSetup();
        }


        /// <summary>
        /// Disable all lights
        /// </summary>
        public void DeactivateLights()
        {
            LightsSetup();
            _updateLeftBlinker = false;
            _updateRightBlinker = false;
        }


        /// <summary>
        /// Set lights state
        /// </summary>
        private void LightsSetup()
        {
            if (frontLights != null)
            {
                frontLights.SetActive(false);
            }
            if (reverseLights != null)
            {
                reverseLights.SetActive(false);
            }
            if (rearLights != null)
            {
                rearLights.SetActive(false);
            }
            if (stopLights != null)
            {
                stopLights.SetActive(false);
            }
            if (blinkerLeft != null)
            {
                blinkerLeft.SetActive(false);
                _updateBlinkers = true;
            }
            if (blinkerRight != null)
            {
                blinkerRight.SetActive(false);
                _updateBlinkers = true;
            }
        }


        /// <summary>
        /// Activate brake lights
        /// </summary>
        /// <param name="active"></param>
        public void SetBrakeLights(bool active)
        {
            if (stopLights)
            {
                if (stopLights.activeSelf != active)
                {
                    stopLights.SetActive(active);
                }
            }
        }


        /// <summary>
        /// Activate main lights
        /// </summary>
        /// <param name="active"></param>
        public void SetMainLights(bool active)
        {
            if (frontLights)
            {
                frontLights.SetActive(active);
            }
            if (rearLights)
            {
                rearLights.SetActive(active);
            }
        }


        /// <summary>
        /// Activate reverse lights
        /// </summary>
        /// <param name="active"></param>
        public void SetReverseLights(bool active)
        {
            if (reverseLights)
            {
                if (reverseLights.activeSelf != active)
                {
                    reverseLights.SetActive(active);
                }
            }
        }


        /// <summary>
        /// Activate blinker lights
        /// </summary>
        /// <param name="blinkType"></param>
        public void SetBlinker(BlinkType blinkType)
        {
            /*if (_currentBlinkType == BlinkType.StartHazard && blinkType != BlinkType.StopHazard)
            {
                return;
            }*/
            _currentBlinkType = blinkType;
            if (blinkerLeft && blinkerRight)
            {
                switch (blinkType)
                {
                    case BlinkType.Stop:
                        if (_updateLeftBlinker == true)
                        {
                            _updateLeftBlinker = false;
                        }
                        if (_updateRightBlinker == true)
                        {
                            _updateRightBlinker = false;
                        }
                        break;
                    case BlinkType.Left:
                        if (_updateLeftBlinker == false)
                        {
                            _updateLeftBlinker = true;
                        }
                        if (_updateRightBlinker == true)
                        {
                            _updateRightBlinker = false;
                        }
                        break;
                    case BlinkType.Right:
                        if (_updateRightBlinker == false)
                        {
                            _updateRightBlinker = true;
                        }
                        if (_updateLeftBlinker == true)
                        {
                            _updateLeftBlinker = false;
                        }
                        break;

                    case BlinkType.StartHazard:
                        if (_updateRightBlinker == false)
                        {
                            _updateRightBlinker = true;
                        }
                        if (_updateLeftBlinker == false)
                        {
                            _updateLeftBlinker = true;
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// Perform blinking
        /// </summary>
        public void UpdateLights(float realTimeSinceStartup)
        {
            if (_updateBlinkers)
            {
                if (realTimeSinceStartup - _currentTime > blinkTime)
                {
                    _currentTime = realTimeSinceStartup;
                    if (_updateLeftBlinker == false)
                    {
                        if (blinkerLeft.activeSelf == true)
                        {
                            blinkerLeft.SetActive(false);
                        }
                    }
                    else
                    {
                        blinkerLeft.SetActive(!blinkerLeft.activeSelf);
                    }
                    if (_updateRightBlinker == false)
                    {
                        if (blinkerRight.activeSelf == true)
                        {
                            blinkerRight.SetActive(false);
                        }
                    }
                    else
                    {
                        blinkerRight.SetActive(!blinkerRight.activeSelf);
                    }
                }
            }
        }
    }
}
