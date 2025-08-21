//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(RCCP_Waypoint))]
public class RCCP_WaypointEditor : Editor {

    RCCP_Waypoint prop;
    GUISkin skin;

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
    public static void OnDrawSceneGizmos(RCCP_Waypoint waypoint, GizmoType gizmoType) {

        if ((gizmoType & GizmoType.Selected) != 0) {

            Gizmos.color = Color.yellow;

        } else {

            Gizmos.color = Color.yellow;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, .25f);

        }

        Gizmos.DrawSphere(waypoint.transform.position, 1.5f);

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;

        //if (SceneView.lastActiveSceneView && Vector3.Distance(waypoint.transform.position, SceneView.lastActiveSceneView.camera.transform.position) < 300f)
        //    Handles.Label((waypoint.transform.position + Vector3.up * 3f) + (Vector3.forward * -0f), waypoint.transform.parent.name + "_" + (waypoint.transform.GetSiblingIndex() + 0).ToString(), style);

        Gizmos.matrix = waypoint.transform.localToWorldMatrix;
        Gizmos.color = Gizmos.color = Color.yellow;

    }

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Waypoint)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Single waypoint used in waypoint container.", MessageType.Info, true);

        DrawDefaultInspector();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_AIWaypointsContainer>(true).gameObject;

            EditorGUILayout.EndVertical();

        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
