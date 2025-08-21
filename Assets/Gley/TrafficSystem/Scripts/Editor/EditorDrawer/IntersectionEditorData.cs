using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections.Generic;
using System.Linq;

namespace Gley.TrafficSystem.Editor
{
    public class IntersectionEditorData : EditorData
    {
        private GenericIntersectionSettings[] _allIntersections;
        private PriorityIntersectionSettings[] _allPriorityIntersections;
        private PriorityCrossingSettings[] _allPriorityCrossings;
        private TrafficLightsIntersectionSettings[] _allTrafficLightsIntersections;
        private TrafficLightsCrossingSettings[] _allTrafficLightsCrossings;

        public IntersectionEditorData()
        {
            LoadAllData();
        }

        public GenericIntersectionSettings[] GetAllIntersections()
        {
            return _allIntersections;
        }


        public PriorityIntersectionSettings[] GetPriorityIntersections()
        {
            return _allPriorityIntersections;
        }


        public PriorityCrossingSettings[] GetPriorityCrossings()
        {
            return _allPriorityCrossings;
        }


        public TrafficLightsIntersectionSettings[] GetTrafficLightsIntersections()
        {
            return _allTrafficLightsIntersections;
        }


        public TrafficLightsCrossingSettings[] GetTrafficLightsCrossings()
        {
            return _allTrafficLightsCrossings;
        }


        protected override void LoadAllData()
        {
            var allPriorityIntersections = new List<PriorityIntersectionSettings>();
            var allTrafficLightsIntersections = new List<TrafficLightsIntersectionSettings>();
            var allTrafficLightsCrossings = new List<TrafficLightsCrossingSettings>();
            var allPriorityCrossings = new List<PriorityCrossingSettings>();

            var allIntersections = GleyPrefabUtilities.GetAllComponents<GenericIntersectionSettings>();

            foreach (var intersection in allIntersections)
            {
                if (intersection != null)
                {
                    intersection.VerifyAssignments();
                    if (intersection.GetType().Equals(typeof(PriorityIntersectionSettings)))
                    {
                        allPriorityIntersections.Add(intersection as PriorityIntersectionSettings);
                    }

                    if (intersection.GetType().Equals(typeof(TrafficLightsIntersectionSettings)))
                    {
                        allTrafficLightsIntersections.Add(intersection as TrafficLightsIntersectionSettings);
                    }

                    if (intersection.GetType().Equals(typeof(TrafficLightsCrossingSettings)))
                    {
                        allTrafficLightsCrossings.Add(intersection as TrafficLightsCrossingSettings);
                    }

                    if (intersection.GetType().Equals(typeof(PriorityCrossingSettings)))
                    {
                        allPriorityCrossings.Add(intersection as PriorityCrossingSettings);
                    }
                }
            }
            _allIntersections = allIntersections.ToArray();
            _allPriorityCrossings = allPriorityCrossings.ToArray();
            _allPriorityIntersections = allPriorityIntersections.ToArray();
            _allTrafficLightsCrossings = allTrafficLightsCrossings.ToArray();
            _allTrafficLightsIntersections = allTrafficLightsIntersections.ToArray();
        }
    }
}