#if GLEY_PEDESTRIAN_SYSTEM
using Gley.PedestrianSystem.Internal;
#endif
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Updates all intersections
    /// </summary>
    public class IntersectionManager : IDestroyable
    {
        private readonly GenericIntersection[] _allIntersections;
        private readonly TrafficLightsIntersection[] _trafficLightsIntersections;
        private readonly PriorityIntersection[] _priorityIntersections;
        private readonly TrafficLightsCrossing[] _trafficLightsCrossings;
        private readonly PriorityCrossing[] _priorityCrossings;

        private List<GenericIntersection> _activeIntersections;
        private TimeManager _timeManager;

        public GenericIntersection[] AllIntersections => _allIntersections;
        public TrafficLightsIntersection[] TrafficLightsIntersections => _trafficLightsIntersections;
        public PriorityIntersection[] PriorityIntersections => _priorityIntersections;
        public TrafficLightsCrossing[] TrafficLightsCrossings => _trafficLightsCrossings;
        public PriorityCrossing[] PriorityCrossings => _priorityCrossings;


        public IntersectionManager(IntersectionsData intersectionsData, TrafficWaypointsData trafficWaypointsData, IPedestrianWaypointsDataHandler pedestrianWaypointsDataHandler, TrafficLightsBehaviour trafficLightsBehaviour, float greenLightTime, float yellowLightTime, TimeManager timeManager)
        {
            Assign();
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianSystem.Events.OnPedestrianRemoved += PedestrianRemovedHandler;
#endif
            TrafficSystem.Events.OnVehicleDisabled += VehicleRemovedHandler;
            IntersectionEvents.OnActiveIntersectionsChanged += ActiveIntersectionChangedHandler;
            _activeIntersections = new List<GenericIntersection>();
            GridEvents.OnActiveGridCellsChanged += ActiveGridCellsChangedHandler;
            ActiveIntersectionChangedHandler(_activeIntersections);

            var allIntersectionTypes = intersectionsData.AllIntersections;
            _allIntersections = new GenericIntersection[allIntersectionTypes.Length];
            var trafficLightsIntersections = new List<TrafficLightsIntersection>();
            var priorityIntersections = new List<PriorityIntersection>();
            var trafficLightsCrossings = new List<TrafficLightsCrossing>();
            var priorityCrossings = new List<PriorityCrossing>();

            for (int i = 0; i < allIntersectionTypes.Length; i++)
            {
                switch (allIntersectionTypes[i].Type)
                {
                    case IntersectionType.TrafficLights:
                        trafficLightsIntersections.Add(new TrafficLightsIntersection(intersectionsData.AllLightsIntersections[allIntersectionTypes[i].OtherListIndex], trafficWaypointsData, pedestrianWaypointsDataHandler, trafficLightsBehaviour, greenLightTime, yellowLightTime));
                        _allIntersections[i] = trafficLightsIntersections[trafficLightsIntersections.Count - 1];
                        break;
                    case IntersectionType.Priority:
                        priorityIntersections.Add(new PriorityIntersection(intersectionsData.AllPriorityIntersections[allIntersectionTypes[i].OtherListIndex], trafficWaypointsData, pedestrianWaypointsDataHandler));
                        _allIntersections[i] = priorityIntersections[priorityIntersections.Count - 1];
                        break;
                    case IntersectionType.LightsCrossing:
                        trafficLightsCrossings.Add(new TrafficLightsCrossing(intersectionsData.AllLightsCrossings[allIntersectionTypes[i].OtherListIndex], trafficWaypointsData, pedestrianWaypointsDataHandler, trafficLightsBehaviour));
                        _allIntersections[i] = trafficLightsCrossings[trafficLightsCrossings.Count - 1];
                        break;
                    case IntersectionType.PriorityCrossing:
                        priorityCrossings.Add(new PriorityCrossing(intersectionsData.AllPriorityCrossings[allIntersectionTypes[i].OtherListIndex], trafficWaypointsData, pedestrianWaypointsDataHandler));
                        _allIntersections[i] = priorityCrossings[priorityCrossings.Count - 1];
                        break;
                }
            }
            _priorityCrossings = priorityCrossings.ToArray();
            _priorityIntersections = priorityIntersections.ToArray();
            _trafficLightsCrossings = trafficLightsCrossings.ToArray();
            _trafficLightsIntersections = trafficLightsIntersections.ToArray();
            _timeManager = timeManager;
        }


        public void Assign()
        {
            DestroyableManager.Instance.Register(this);
        }


        /// <summary>
        /// Called on every frame to update active intersection road status
        /// </summary>
        public void UpdateIntersections(float realTimeSinceStartup)
        {
            for (int i = 0; i < _activeIntersections.Count; i++)
            {
                _activeIntersections[i].UpdateIntersection(realTimeSinceStartup);
            }
        }


        private void VehicleRemovedHandler(int index)
        {
            for (int i = 0; i < _activeIntersections.Count; i++)
            {
                _activeIntersections[i].RemoveVehicle(index);
            }
        }


        public void SetCrossingState(string crossingName, TrafficLightsColor newColor, bool doNotChangeAgain, float realtimeSinceStartup)
        {
            for (int i = 0; i < _trafficLightsCrossings.Length; i++)
            {
                if (_trafficLightsCrossings[i].GetName() == crossingName)
                {
                    _trafficLightsCrossings[i].SetCrossingState(newColor, doNotChangeAgain, realtimeSinceStartup);
                    return;
                }
            }
            Debug.LogWarning($"{crossingName} not found");
        }


        public TrafficLightsColor GetTrafficLightsCrossingState(string crossingName)
        {
            for (int i = 0; i < _trafficLightsCrossings.Length; i++)
            {
                if (_trafficLightsCrossings[i].GetName() == crossingName)
                {
                    return _trafficLightsCrossings[i].GetCrossingState();
                }
            }
            Debug.LogWarning($"{crossingName} not found");
            return TrafficLightsColor.Red;
        }


        public bool IsPriorityCrossingRed(string crossingName)
        {
            for (int i = 0; i < _priorityCrossings.Length; i++)
            {
                if (_priorityCrossings[i].GetName() == crossingName)
                {
                    return _priorityCrossings[i].GetPriorityCrossingState();
                }
            }
            Debug.LogWarning($"{crossingName} not found");
            return false;
        }


        public void SetPriorityCrossingState(string crossingName, bool stop, bool stopUpdate)
        {
            for (int i = 0; i < _priorityCrossings.Length; i++)
            {
                if (_priorityCrossings[i].GetName() == crossingName)
                {
                    _priorityCrossings[i].SetPriorityCrossingState(stop, stopUpdate);
                }
            }
        }


        public void SetRoadToGreen(string intersectionName, int roadIndex, bool doNotChangeAgain)
        {
            for (int i = 0; i < _trafficLightsIntersections.Length; i++)
            {
                if (_trafficLightsIntersections[i].GetName() == intersectionName)
                {
                    _trafficLightsIntersections[i].SetGreenRoad(roadIndex, doNotChangeAgain, _timeManager.RealTimeSinceStartup);
                    return;
                }
            }
            Debug.LogWarning($"{intersectionName} not found. Make sure it is a Traffic Lights Intersection.");
        }


        public void SetTrafficLightsBehaviour(TrafficLightsBehaviour trafficLightsBehaviour)
        {
            for (int i = 0; i < _trafficLightsIntersections.Length; i++)
            {
                _trafficLightsIntersections[i].SetTrafficLightsBehaviour(trafficLightsBehaviour);
            }

            for (int i = 0; i < _trafficLightsCrossings.Length; i++)
            {
                _trafficLightsCrossings[i].SetTrafficLightsBehaviour(trafficLightsBehaviour);
            }
        }


        public List<GenericIntersection> GetIntersections(List<int> intersectionIndexes)
        {
            List<GenericIntersection> result = new List<GenericIntersection>();
            for (int i = 0; i < intersectionIndexes.Count; i++)
            {
                result.Add(_allIntersections[intersectionIndexes[i]]);
            }
            return result;
        }


        private void PedestrianRemovedHandler(int pedestrianIndex)
        {
            for (int i = 0; i < _activeIntersections.Count; i++)
            {
                _activeIntersections[i].PedestrianPassed(pedestrianIndex);
            }
        }


        private void ActiveGridCellsChangedHandler(CellData[] activeCells)
        {
            List<int> intersectionIndexes = new List<int>();
            for (int i = 0; i < activeCells.Length; i++)
            {
                intersectionIndexes.AddRange(activeCells[i].IntersectionsInCell.Except(intersectionIndexes));
            }

            List<GenericIntersection> result = GetIntersections(intersectionIndexes);

            if (_activeIntersections.Count == result.Count && _activeIntersections.All(result.Contains))
            {
            }
            else
            {
                IntersectionEvents.TriggerActiveIntersectionsChangedEvent(result);
            }
        }


        /// <summary>
        /// Initialize all active intersections
        /// </summary>
        /// <param name="activeIntersections"></param>
        private void ActiveIntersectionChangedHandler(List<GenericIntersection> activeIntersections)
        {
            for (int i = 0; i < activeIntersections.Count; i++)
            {
                if (_activeIntersections != null)
                {
                    if (!_activeIntersections.Contains(activeIntersections[i]))
                    {
                        activeIntersections[i].ResetIntersection();
                    }
                }
            }
            _activeIntersections = activeIntersections;
        }


        public void OnDestroy()
        {
#if GLEY_PEDESTRIAN_SYSTEM
            PedestrianSystem.Events.OnPedestrianRemoved -= PedestrianRemovedHandler;
#endif
            IntersectionEvents.OnActiveIntersectionsChanged -= ActiveIntersectionChangedHandler;
            GridEvents.OnActiveGridCellsChanged -= ActiveGridCellsChangedHandler;
            TrafficSystem.Events.OnVehicleDisabled -= VehicleRemovedHandler;
        }
    }
}