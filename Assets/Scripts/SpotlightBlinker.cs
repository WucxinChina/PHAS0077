using UnityEngine;

namespace Gley.TrafficSystem
{
    public class SpotlightBlinker : MonoBehaviour
    {
        [Tooltip("Ҫ������˸��Spotlight����")]
        public GameObject spotFrontlightObject, spotBehindlightObject;

        [Tooltip("��˸������룩")]
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

                    // ��ʱ������
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
