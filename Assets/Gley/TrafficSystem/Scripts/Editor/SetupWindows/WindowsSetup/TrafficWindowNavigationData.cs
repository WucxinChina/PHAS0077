using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;
using UnityEngine;

namespace Gley.TrafficSystem.Editor
{
    public class TrafficWindowNavigationData
    {
        private Road _selectedRoad;
        private WaypointSettings _selectedWaypoint;
        private GenericIntersectionSettings _selectedIntersection;
        private LayerMask _roadLayers;


        public void InitializeData()
        {
            UpdateLayers();
            _selectedRoad = null;
        }


        public Road GetSelectedRoad()
        {
            return _selectedRoad;
        }


        public void SetSelectedRoad(Road road)
        {
            _selectedRoad = road;
        }


        public WaypointSettings GetSelectedWaypoint()
        {
            return _selectedWaypoint;
        }


        public void SetSelectedWaypoint( WaypointSettings waypoint)
        {
            _selectedWaypoint = waypoint;
        }


        public GenericIntersectionSettings GetSelectedIntersection()
        {
            return _selectedIntersection;
        }


        public void SetSelectedIntersection(GenericIntersectionSettings intersection)
        {
            _selectedIntersection = intersection;
        }


        public void UpdateLayers()
        {
            var layers = FileCreator.LoadScriptableObject<LayerSetup>(TrafficSystemConstants.layerPath);
            if (layers != null)
            {
                _roadLayers = layers.roadLayers;
            }
        }


        public LayerMask GetRoadLayers()
        {
            return _roadLayers;
        }
    }
}