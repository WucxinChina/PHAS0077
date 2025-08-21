using UnityEngine;

namespace Gley.TrafficSystem
{
    [System.Serializable]
    public struct Area
    {
        public Vector3 center;
        public float radius;
        [HideInInspector]
        public float sqrRadius;

        public Area(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
            sqrRadius = radius * radius;
        }
        public Area(Area area)
        {
            center = area.center;
            radius = area.radius;
            sqrRadius = radius * radius;
        }
    }


    /// <summary>
    /// Stores the traffic properties at initialization.
    /// </summary>
    public class TrafficOptions
    {
        public float MinDistanceToAdd = -1;
        public float DistanceToRemove = -1;
        public float MasterVolume = 1;
        public bool UseWaypointPriority = false;
        public float GreenLightTime = -1;
        public float YellowLightTime = -1;
        public float MinOffset = -0.25f;
        public float MaxOffset = 0.25f;
        public int ActiveSquaresLevel = 1;
        public int InitialDensity = -1; //all vehicles are available from the start
        public int DefaultPathLength = 5;
        public bool LightsOn = false;
        public Area DisableWaypointsArea = default;
        public IBehaviourImplementation BehaviourImplementation;


        private TrafficLightsBehaviour trafficLightsBehaviour;
        public TrafficLightsBehaviour TrafficLightsBehaviour
        {
            get
            {
                if (trafficLightsBehaviour == null)
                {
                    trafficLightsBehaviour = DefaultDelegates.TrafficLightBehaviour;
                }
                return trafficLightsBehaviour;
            }
            set
            {
                trafficLightsBehaviour = value;
            }
        }


        private SpawnWaypointSelector spawnWaypointSelector;
        public SpawnWaypointSelector SpawnWaypointSelector
        {
            get
            {
                if (spawnWaypointSelector == null)
                {
                    spawnWaypointSelector = DefaultDelegates.GetRandomSpawnWaypoint;
                }
                return spawnWaypointSelector;
            }
            set
            {
                spawnWaypointSelector = value;
            }
        }


        private ModifyTriggerSize modifyTriggerSize;
        public ModifyTriggerSize ModifyTriggerSize
        {
            get
            {
                if (modifyTriggerSize == null)
                {
                    modifyTriggerSize = DefaultDelegates.TriggerSizeModifier;
                }
                return modifyTriggerSize;
            }
            set
            {
                modifyTriggerSize = value;
            }
        }


        private IBehaviourList _vehicleBehaviours;
        public IBehaviourList VehicleBehaviours
        {
            get
            {
                if (_vehicleBehaviours == null)
                {
                    _vehicleBehaviours = new DefaultVehicleBehaviours();
                }
                return _vehicleBehaviours;
            }
            set
            {
                _vehicleBehaviours = value;
            }
        }
    }
}