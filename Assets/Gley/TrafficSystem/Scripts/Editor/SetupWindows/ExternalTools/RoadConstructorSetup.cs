using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Gley.TrafficSystem.Editor
{
    public class RoadConstructorSetup : SetupWindowBase
    {
#if GLEY_ROADCONSTRUCTOR_TRAFFIC
        private IntersectionType selectedType;
        private float greenLightTime = 10;
        private float yellowLightTime = 3;
        private bool linkLanes = true;
        private int linkDistance = 3;
#endif

        protected override void TopPart()
        {
            base.TopPart();
#if GLEY_ROADCONSTRUCTOR_TRAFFIC
            if (GUILayout.Button("Disable Road Constructor"))
            {
                Gley.Common.PreprocessorDirective.AddToCurrent(TrafficSystemConstants.GLEY_ROADCONSTRUCTOR_TRAFFIC, true);
            }
#else
            if (GUILayout.Button("Enable Road Constructor Support"))
            {
                Gley.Common.PreprocessorDirective.AddToCurrent(TrafficSystemConstants.GLEY_ROADCONSTRUCTOR_TRAFFIC, false);
            }
#endif
            EditorGUILayout.Space();
            if (GUILayout.Button("Download Road Constructor"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/level-design/road-constructor-287445?aid=1011l8QY4");
            }
        }

#if GLEY_ROADCONSTRUCTOR_TRAFFIC
        protected override void ScrollPart(float width, float height)
        {
            base.ScrollPart(width, height);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Select default intersection type to use:");
            selectedType = (IntersectionType)EditorGUILayout.EnumPopup("Intersection type:", selectedType);

            if (selectedType == IntersectionType.TrafficLights)
            {
                greenLightTime = EditorGUILayout.FloatField("Green Light Time", greenLightTime);
                yellowLightTime = EditorGUILayout.FloatField("Yellow Light Time", yellowLightTime);
            }

            linkLanes = EditorGUILayout.Toggle("Link lanes for overtake", linkLanes);
            if (linkLanes)
            {
                linkDistance = EditorGUILayout.IntField("Overtake link distance", linkDistance);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Extract Waypoints"))
            {
                List<int> vehicleTypes = System.Enum.GetValues(typeof(VehicleTypes)).Cast<int>().ToList();
                RoadConstructorMethods.ExtractWaypoints(selectedType, greenLightTime, yellowLightTime, linkLanes, linkDistance, vehicleTypes);
            }
        }
#endif
    }
}