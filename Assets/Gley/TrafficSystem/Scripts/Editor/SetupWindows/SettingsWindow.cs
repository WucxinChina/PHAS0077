using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;
using System;
using UnityEditor;
using UnityEngine;
namespace Gley.TrafficSystem.Editor
{
    public class SettingsWindow : SettingsWindowBase
    {
        private const string WINDOW_NAME = "Traffic System - v.";
        private const string PATH = "Assets/Gley/TrafficSystem/Scripts/Version.txt";
        private const int MIN_WIDTH = 400;
        private const int MIN_HEIGHT = 500;

        static SettingsWindow _trafficWindow;
        static TrafficWindowNavigationData _windowNavigationData;


        [MenuItem("Tools/Gley/Traffic System", false)]
        private static void Initialize()
        {
            _trafficWindow = WindowLoader.LoadWindow<SettingsWindow>(PATH, WINDOW_NAME, MIN_WIDTH, MIN_HEIGHT);
            _trafficWindow.Init(_trafficWindow, typeof(MainMenuWindow), AllWindowsData.GetWindowsData(), new AllSettingsWindows());
        }


        protected override void Reinitialize()
        {
            if (_trafficWindow == null)
            {
                Initialize();
            }
            else
            {
                _trafficWindow.Init(_trafficWindow, typeof(MainMenuWindow), AllWindowsData.GetWindowsData(), new AllSettingsWindows());
            }

        }


        protected override void ResetToHomeScreen(Type defaultWindow, bool now)
        {
            _windowNavigationData = new TrafficWindowNavigationData();
            _windowNavigationData.InitializeData();
            base.ResetToHomeScreen(defaultWindow, now);
        }


        protected override void MouseMove(Vector3 point)
        {
            if (_activeSetupWindow.GetType() == typeof(EditRoadWindow))
            {
                _activeSetupWindow.MouseMove(point);
            }
        }


        public override LayerMask GetGroundLayer()
        {
            return _windowNavigationData.GetRoadLayers();
        }


        //TODO these should not be static methods
        public static void SetSelectedWaypoint(WaypointSettings waypoint)
        {
            _windowNavigationData.SetSelectedWaypoint(waypoint);
        }


        public static WaypointSettings GetSelectedWaypoint()
        {
            return _windowNavigationData.GetSelectedWaypoint();
        }


        public static void SetSelectedIntersection(GenericIntersectionSettings clickedIntersection)
        {
            _windowNavigationData.SetSelectedIntersection(clickedIntersection);
        }


        public static GenericIntersectionSettings GetSelectedIntersection()
        {
            return _windowNavigationData.GetSelectedIntersection();
        }


        public static Road GetSelectedRoad()
        {
            return _windowNavigationData.GetSelectedRoad();
        }


        public static void SetSelectedRoad(Road road)
        {
            _windowNavigationData.SetSelectedRoad(road);
        }


        public static void UpdateLayers()
        {
            _windowNavigationData.UpdateLayers();
        }


        private void OnInspectorUpdate()
        {
            if (_activeSetupWindow != null)
            {
                _activeSetupWindow.InspectorUpdate();
            }
        }
    }
}