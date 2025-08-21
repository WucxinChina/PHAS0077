using Gley.UrbanSystem.Editor;
using UnityEngine;

namespace Gley.TrafficSystem.Editor
{
    public class ShowPriorityEditedWaypoints : ShowWaypointsTrafficBase
    {
        private readonly float _scrollAdjustment = 221;

        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _waypointsOfInterest = _trafficWaypointData.GetPriorityEditedWaypoints();
            return this;
        }

        public override void DrawInScene()
        {
            _trafficWaypointDrawer.ShowPriorityEditedWaypoints(_editorSave.EditorColors.WaypointColor, _editorSave.EditorColors.PriorityColor);
            base.DrawInScene();
        }


        protected override void ScrollPart(float width, float height)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(width - SCROLL_SPACE), GUILayout.Height(height - _scrollAdjustment));
            base.ScrollPart(width, height);
            GUILayout.EndScrollView();
        }
    }
}