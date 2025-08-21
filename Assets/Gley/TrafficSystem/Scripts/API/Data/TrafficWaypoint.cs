using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem
{
    /// <summary>
    /// Stores all properties of a play mode waypoint.
    /// </summary>
    [System.Serializable]
    public class TrafficWaypoint : Waypoint
    {
        private List<IIntersection> _associatedIntersections;

        [SerializeField] private VehicleTypes[] _allowedVehicles;
        [SerializeField] private int[] _angle;
        [SerializeField] private int[] _giveWayList;
        [SerializeField] private int[] _otherLanes;
        [SerializeField] private Vector3 _leftDirection;
        [SerializeField] private BlinkType _blinkType;
        [SerializeField] private string _eventData;
        [SerializeField] private float _laneWidth;
        [SerializeField] private int _maxSpeed;
        [SerializeField] private bool _giveWay;
        [SerializeField] private bool _complexGiveWay;
        [SerializeField] private bool _zipperGiveWay;
        [SerializeField] private bool _intersectionGiveWay;
        [SerializeField] private bool _triggerEvent;
        [SerializeField] private bool _enter;
        [SerializeField] private bool _exit;
        [SerializeField] private bool _stop;

        public int[] GiveWayList => _giveWayList;
        public VehicleTypes[] AllowedVehicles => _allowedVehicles;
        public List<IIntersection> AssociatedIntersections => _associatedIntersections;
        public int[] OtherLanes => _otherLanes;
        public int[] Angle => _angle;
        public Vector3 LeftDirection => _leftDirection;
        public float LaneWidth => _laneWidth;
        public int MaxSpeed => _maxSpeed;
        public bool ComplexGiveWay => _complexGiveWay;
        public bool ZipperGiveWay => _zipperGiveWay;
        public bool IntersectionGiveWay => _intersectionGiveWay;
        public bool Enter => _enter;
        public bool Exit => _exit;
        public bool Stop => _stop;
        public bool GiveWay => _giveWay;

        public bool HasNeighbors
        {
            get
            {
                return Neighbors.Length > 0;
            }
        }

        public bool HasPrevs
        {
            get
            {
                return Prevs.Length > 0;
            }
        }

        public bool HasOtherLanes
        {
            get
            {
                return OtherLanes.Length > 0;
            }
        }

        public BlinkType BlinkType
        {
            get
            {
                return _blinkType;
            }
            set
            {
                _blinkType = value;
            }
        }

        public bool TriggerEvent
        {
            get
            {
                return _triggerEvent;
            }
            set
            {
                _triggerEvent = value;
            }
        }

        public string EventData
        {
            get
            {
                return _eventData;
            }
            set
            {
                _eventData = value;
            }
        }


        public TrafficWaypoint(string name, int listIndex, Vector3 position, List<VehicleTypes> allowedVehicles, int[] neighbors, int[] prev, int[] otherLanes, int maxSpeed, bool giveWay,
            bool complexGiveWay, bool zipperGiveWay, bool triggerEvent, float laneWidth, Vector3 left, string eventData, int[] giveWayList, int[] angle)
            : base(name, listIndex, position, neighbors, prev)
        {
            _maxSpeed = maxSpeed;
            _giveWay = giveWay;
            _complexGiveWay = complexGiveWay;
            _zipperGiveWay = zipperGiveWay;
            _giveWayList = giveWayList;
            _laneWidth = laneWidth;
            _leftDirection = left;
            _otherLanes = otherLanes;
            _enter = false;
            _exit = false;
            _stop = false;
            _triggerEvent = triggerEvent;
            _eventData = eventData;
            _allowedVehicles = allowedVehicles.ToArray();
            _angle = angle;
            _blinkType = BlinkType.None;
        }


        /// <summary>
        /// Initializes current waypoint properties
        /// Used by intersections
        /// </summary>
        /// <param name="intersection"></param>
        /// <param name="giveWay"></param>
        /// <param name="stop"></param>
        /// <param name="enter"></param>
        /// <param name="exit"></param>
        public void SetIntersection(IIntersection intersection, bool giveWay, bool stop, bool enter, bool exit, bool intersectionGiveWay)
        {
            if (_associatedIntersections == null)
            {
                _associatedIntersections = new List<IIntersection>();
            }
            if (!_associatedIntersections.Contains(intersection))
            {
                _associatedIntersections.Add(intersection);
            }
            _stop = stop;
            _giveWay = giveWay;
            _exit = exit;
            _enter = enter;
            _intersectionGiveWay = intersectionGiveWay;
            WaypointEvents.TriggerGiveWayStateChangedEvent(ListIndex, GetGiveWayState());
        }


        public void SetStopValue(bool newValue)
        {
            if (_stop != newValue)
            {
                _stop = newValue;
                WaypointEvents.TriggerStopStateChangedEvent(ListIndex, newValue);
            }
        }


        public void SetGiveWayValue(bool newValue)
        {
            if (_giveWay != newValue)
            {
                _giveWay = newValue;
                WaypointEvents.TriggerGiveWayStateChangedEvent(ListIndex, GetGiveWayState());
            }
        }


        public void ComputeBlinkerData(TrafficWaypointsData trafficWaypointsData)
        {
            if (Neighbors.Length == 0)
            {
                _blinkType = BlinkType.StartHazard;
                return;
            }

            if (Neighbors.Length > 1)
            {
                _blinkType = BlinkType.Start;

                for(int i=0;i<Neighbors.Length;i++)
                {
                    Vector3 forward = GetForwardVector(trafficWaypointsData);
                    Vector3 toNext = (trafficWaypointsData.AllTrafficWaypoints[Neighbors[i]].Position - Position).normalized;
                    float turnDirection = Vector3.Cross(forward, toNext).y; // Determines left or right turn

                    if (turnDirection > 0.1f)
                    {
                        trafficWaypointsData.AllTrafficWaypoints[Neighbors[i]].BlinkType = BlinkType.Right;
                    }
                    else
                    {
                        if (turnDirection < -0.1f)
                        {
                            trafficWaypointsData.AllTrafficWaypoints[Neighbors[i]].BlinkType = BlinkType.Left;
                        }
                    }
                }
                return;
            }

            if (Neighbors.Length == 1) // Intersection or possible turn
            {
                if (_blinkType == BlinkType.None)
                {
                    Vector3 forward = GetForwardVector(trafficWaypointsData);
                    Vector3 toNext = (trafficWaypointsData.AllTrafficWaypoints[Neighbors[0]].Position - Position).normalized;
                    float turnDirection = Vector3.Cross(forward, toNext).y; // Determines left or right turn

                    if (turnDirection > 0.1f)
                    {
                        _blinkType = BlinkType.Right;
                    }
                    else
                    {
                        if (turnDirection < -0.1f)
                        {
                            _blinkType = BlinkType.Left;
                        }
                        else
                        {
                            _blinkType = BlinkType.None;
                        }
                    }
                }
            }
        }


        public Vector3 GetForwardVector(TrafficWaypointsData trafficWaypointsData)
        {
            if (Prevs.Length == 0)
            {
                Debug.LogWarning("Waypoint has no previous waypoints, defaulting to (0,0,1)");
                return Vector3.forward; // Default direction
            }

            Vector3 forward = Vector3.zero;

            // Compute forward direction based on all previous waypoints
            foreach (int prev in Prevs)
            {
                forward += (Position - trafficWaypointsData.AllTrafficWaypoints[prev].Position).normalized;
            }

            return forward.normalized; // Normalize for correct direction
        }


        public GiveWayType GetGiveWayState()
        {
            if (IntersectionGiveWay && Enter)
            {
                return GiveWayType.Intersection;
            }

            if (ComplexGiveWay)
            {
                return GiveWayType.Complex;
            }

            if (ZipperGiveWay)
            {
                return GiveWayType.Zipper;
            }

            if (GiveWay)
            {
                return GiveWayType.Standard;
            }

            return GiveWayType.None;
        }


        public void AddOtherLane(int waypointIndex)
        {
            List<int> prevList = _otherLanes.ToList();
            if (!prevList.Contains(waypointIndex))
            {
                prevList.Add(waypointIndex);
                _otherLanes = prevList.ToArray();
            }
        }
    }
}