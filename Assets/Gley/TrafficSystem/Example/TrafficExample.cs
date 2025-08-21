using Gley.UrbanSystem.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gley.TrafficSystem.Internal
{
    public class TrafficExample : MonoBehaviour
    {
        [SerializeField] private Transform _busStops;
        private bool _pathSet;
        private int _stopNumber;
        private bool _followVehicle;
        private Transform _player;

        private const int _vehicleToFollow = 23;

        private void Start()
        {
            _player = GameObject.Find("Player").transform;
        }

        //every time a destination is reached, a new one is selected
        private void BusStationReached(int vehicleIndex)
        {
            //remove listener otherwise this method will be called on each frame
            Events.OnDestinationReached -= BusStationReached;
            if (vehicleIndex == 0)
            {
                _stopNumber++;
                if (_stopNumber == _busStops.childCount)
                {
                    _stopNumber = 0;
                }
                //stop and wait for 5 seconds, then move to the next destination
                Invoke("ContinueDriving", 5);
            }
        }

        /// <summary>
        /// Continue on path
        /// </summary>
        private void ContinueDriving()
        {
            Events.OnDestinationReached += BusStationReached;
            API.SetDestination(0, _busStops.GetChild(_stopNumber).transform.position);
        }

        private void Update()
        {
            if (!_pathSet)
            {
                if (API.IsInitialized())
                {
                    _pathSet = true;
                    SetPath();
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                _followVehicle = !_followVehicle;
                if (_followVehicle)
                {
                    GameObject.Find("Main Camera").GetComponent<CameraFollow>().target = API.GetVehicleComponent(_vehicleToFollow).transform;
                    API.SetCamera(API.GetVehicleComponent(_vehicleToFollow).transform);
                }
                else
                {
                    GameObject.Find("Main Camera").GetComponent<CameraFollow>().target = _player;
                    API.SetCamera(_player);
                }
            }

            if(Input.GetKeyDown(KeyCode.I))
            {
                API.StartVehicleBehaviour<IgnoreTrafficRules>(_vehicleToFollow);
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                API.StopVehicleBehaviour<IgnoreTrafficRules>(_vehicleToFollow);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

            if(Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(0);
            }
        }

        /// <summary>
        /// set a path towards destination
        /// </summary>
        private void SetPath()
        {
            VehicleComponent vehicleComponent = API.GetVehicleComponent(0);
            if (vehicleComponent.gameObject.activeSelf)
            {
                Events.OnDestinationReached += BusStationReached;
                API.SetDestination(0, _busStops.GetChild(_stopNumber).transform.position);
            }
            else
            {
                Invoke("SetPath", 1);
            }
        }

        //remove listeners
        private void OnDestroy()
        {
            Events.OnDestinationReached -= BusStationReached;
        }
    }
}