using UnityEngine;

namespace Gley.TrafficSystem
{
    /// <summary>
    /// Stores the vehicle prefabs used in scene
    /// </summary>
    [CreateAssetMenu(fileName = "VehiclePool", menuName = "TrafficSystem/Vehicle Pool", order = 1)]
    public class VehiclePool : ScriptableObject
    {
        public CarType[] trafficCars;

        public VehiclePool()
        {
            CarType carType = new CarType();
            trafficCars = new CarType[] { carType };
        }
    }


    [System.Serializable]
    public class CarType
    {
        [SerializeField] private GameObject vehiclePrefab;
        [Range(1, 100)]
        [SerializeField] private int percent;
        [SerializeField] private bool ignore;

        public CarType()
        {
            percent = 1;
        }

        public GameObject VehiclePrefab => vehiclePrefab;
        public int Percent => percent;
        public bool Ignore => ignore;
    }
}