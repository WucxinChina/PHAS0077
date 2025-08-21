using UnityEngine;

namespace Gley.TrafficSystem
{
    /// <summary>
    /// This script is used to update the handlebar rotation of a vehicle.
    /// </summary>
    public class UpdateHandlebar : MonoBehaviour
    {
        public Transform handlebar;
        int _listIndex;

        private void Start()
        {
            _listIndex = API.GetVehicleIndex(gameObject);
        }

        private void Update()
        {
            handlebar.localEulerAngles = new Vector3(0, 0, API.GetSteeringAngle(_listIndex));
        }
    }
}
