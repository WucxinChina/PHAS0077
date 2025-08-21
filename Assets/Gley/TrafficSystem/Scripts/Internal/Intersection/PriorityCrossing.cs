using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Controls the priority crossing intersection type.
    /// </summary>
    public class PriorityCrossing : GenericIntersection, IDestroyable
    {
        private List<PedestrianCrossing> _pedestriansCrossing;
        private PriorityCrossingData _priorityCrossingData;
        private Vector3 _position;
        private Color _waypointColor;
        private bool _stopCars;


        public PriorityCrossing(PriorityCrossingData priorityCrossingData, TrafficWaypointsData trafficWaypointsData, IPedestrianWaypointsDataHandler pedestrianWaypointsDataHandler)
        {
            _priorityCrossingData = priorityCrossingData;

            for (int i = 0; i < _priorityCrossingData.ExitWaypoints.Length; i++)
            {
                trafficWaypointsData.AllTrafficWaypoints[_priorityCrossingData.ExitWaypoints[i]].SetIntersection(this, false, false, false, true, false);
            }
            int nr = 0;
            for (int i = 0; i < _priorityCrossingData.StopWaypoints.Length; i++)
            {
                for (int j = 0; j < _priorityCrossingData.StopWaypoints[i].roadWaypoints.Length; j++)
                {
                    trafficWaypointsData.AllTrafficWaypoints[_priorityCrossingData.StopWaypoints[i].roadWaypoints[j]].SetIntersection(this, true, false, true, false, true);
                    _position += trafficWaypointsData.AllTrafficWaypoints[_priorityCrossingData.StopWaypoints[i].roadWaypoints[j]].Position;
                    nr++;
                }
            }
            _position = _position / nr;

            InitializePedestrianWaypoints(pedestrianWaypointsDataHandler);

            _carsInIntersection = new List<int>();
            _waypointColor = Color.green;
            Assign();
        }


        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        /// <summary>
        /// Check if the intersection road is free and update intersection priority
        /// </summary>
        /// <param name="waypointIndex"></param>
        /// <returns></returns>
        public override bool IsPathFree(int waypointIndex)
        {
            _stopCars = IsPedestrianCrossing(0);

            if (_stopCars)
            {
                if (_waypointColor != Color.red)
                {
                    _waypointColor = Color.red;
                    CheckColor();
                }
                return false;
            }
            if (_waypointColor != Color.green)
            {
                _waypointColor = Color.green;
            }

            return true;
        }


        public override string GetName()
        {
            return _priorityCrossingData.Name;
        }


        public override void PedestrianPassed(int pedestrianIndex)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianCrossing ped = _pedestriansCrossing.FirstOrDefault(cond => cond.PedestrianIndex == pedestrianIndex);
            if (ped != null)
            {
                if (ped.Crossing == false)
                {
                    ped.Crossing = true;
                }
                else
                {
                    _pedestriansCrossing.Remove(ped);
                    ////reset stop
                    //for (int i = 0; i < _priorityCrossingData.StopWaypoints[ped.Road].pedestrianWaypoints.Length; i++)
                    //{
                    //    PedestrianSystem.Events.TriggerStopStateChangedEvent(_priorityCrossingData.StopWaypoints[ped.Road].pedestrianWaypoints[i], true);
                    //}
                }
            }
#endif
        }


        public int[] GetWaypointsToCkeck()
        {
            return _priorityCrossingData.StopWaypoints[0].roadWaypoints;
        }


        public Color GetWaypointColors()
        {
            return _waypointColor;
        }


        public override List<int> GetStopWaypoints()
        {
            var result = new List<int>();
            for (int i = 0; i < _priorityCrossingData.StopWaypoints.Length; i++)
            {
                result.AddRange(_priorityCrossingData.StopWaypoints[i].roadWaypoints);
            }
            return result;
        }


        public void SetPriorityCrossingState(bool stop, bool stopUpdate)
        {
            _stopCars = stop;
            IsPathFree(0);
        }


        public bool GetPriorityCrossingState()
        {
            return _waypointColor == Color.red;
        }


        public override void UpdateIntersection(float realtimeSinceStartup)
        {

        }


        public int GetCarsInIntersection()
        {
            return _carsInIntersection.Count;
        }


        public List<PedestrianCrossing> GetPedestriansCrossing()
        {
            return _pedestriansCrossing;
        }



        public override int[] GetPedStopWaypoint()
        {
            return new int[0];
        }


        public Vector3 GetPosition()
        {
            return _position;
        }

        public override void ResetIntersection()
        {
            base.ResetIntersection();
            _pedestriansCrossing = new List<PedestrianCrossing>();
        }


        private void InitializePedestrianWaypoints(IPedestrianWaypointsDataHandler pedestrianWaypointsDataHandler)
        {
            _pedestriansCrossing = new List<PedestrianCrossing>();
#if GLEY_PEDESTRIAN_SYSTEM
            for (int i = 0; i < _priorityCrossingData.StopWaypoints.Length; i++)
            {
                pedestrianWaypointsDataHandler.SetIntersection(_priorityCrossingData.StopWaypoints[i].pedestrianWaypoints, this);
            }
            PedestrianSystem.Events.OnStreetCrossing += PedestrianWantsToCross;
#endif
        }


        private void MakePedestriansCross(int road)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            for (int i = 0; i < _priorityCrossingData.StopWaypoints[road].pedestrianWaypoints.Length; i++)
            {
                PedestrianSystem.Events.TriggerStopStateChangedEvent(_priorityCrossingData.StopWaypoints[road].pedestrianWaypoints[i], false);
            }
#endif
        }


        private void PedestrianWantsToCross(int pedestrianIndex, IIntersection intersection, int waypointIndex)
        {
            if (intersection == this)
            {
                int road = GetRoadToCross(waypointIndex);
                _pedestriansCrossing.Add(new PedestrianCrossing(pedestrianIndex, road));
                CheckColor();
            }
        }


        private void CheckColor()
        {
            if (_pedestriansCrossing.Count > 0)
            {
                if (_waypointColor == Color.red)
                {
                    MakePedestriansCross(0);
                }
                else
                {
                    IsPathFree(0);
                }
            }
        }


        private int GetRoadToCross(int waypoint)
        {
            for (int i = 0; i < _priorityCrossingData.StopWaypoints.Length; i++)
            {
                for (int j = 0; j < _priorityCrossingData.StopWaypoints[i].pedestrianWaypoints.Length; j++)
                {
                    if (_priorityCrossingData.StopWaypoints[i].pedestrianWaypoints[j] == waypoint)
                    {
                        return i;
                    }
                }
            }
            Debug.LogError("Not Good - verify pedestrians assignments in priority intersection");
            return -1;
        }


        private bool IsPedestrianCrossing(int road)
        {
            if (_pedestriansCrossing.Count == 0)
            {
                return false;
            }
            return _pedestriansCrossing.FirstOrDefault(cond => cond.Road == road) != null;
        }


        public void OnDestroy()
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianSystem.Events.OnStreetCrossing -= PedestrianWantsToCross;
#endif
        }
    }
}