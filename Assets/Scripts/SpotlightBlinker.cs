using UnityEngine;

namespace Gley.TrafficSystem
{
    public class SpotlightBlinker : MonoBehaviour
    {
        [Tooltip("要控制闪烁的Spotlight对象")]
        public GameObject spotFrontlightObject, spotBehindlightObject;

        [Tooltip("闪烁间隔（秒）")]
        public float blinkInterval = 0.5f;

        public VehicleComponent vehicle;

        private float _timer;

        void Update()
        {
            if (spotFrontlightObject == null && spotBehindlightObject == null)
                return;

            _timer += Time.deltaTime;
            if (TrafficSettings.HMI)
            {
                if (_timer >= blinkInterval)
                {
                    if (vehicle.mergeFrontAssistTarget)
                    {
                        spotFrontlightObject.SetActive(!spotFrontlightObject.activeSelf);
                        spotBehindlightObject.SetActive(false);
                    }
                    else
                    {
                        spotBehindlightObject.SetActive(!spotBehindlightObject.activeSelf);
                        spotFrontlightObject.SetActive(false);
                    }

                    // 计时器归零
                    _timer = 0f;
                }
            }else
            {
                spotFrontlightObject.SetActive(false);
                spotBehindlightObject.SetActive(false);
            }
        }
    }
}
