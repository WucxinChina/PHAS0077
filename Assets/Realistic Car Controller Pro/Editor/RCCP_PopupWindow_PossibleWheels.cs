//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class RCCP_PopupWindow_PossibleWheels : EditorWindow {

    //────────────────────────────────────────────────────────────────────//
    #region Fields
    //────────────────────────────────────────────────────────────────────//

    private GUISkin skin;

    private GameObject[] possibleWheels;
    private readonly List<GameObject> allSelectedWheels = new List<GameObject>();
    private Action<GameObject[]> onButtonClick;

    private Vector2 scrollPos;

    //  Currently hovered wheel – used for Scene gizmo preview.
    private GameObject hoveredWheel;

    #endregion

    //────────────────────────────────────────────────────────────────────//
    #region API
    //────────────────────────────────────────────────────────────────────//

    public static void ShowWindow(GameObject[] allPossibleWheels, Action<GameObject[]> onButtonClickAction) {

        RCCP_PopupWindow_PossibleWheels window =
            GetWindow<RCCP_PopupWindow_PossibleWheels>(true, "Select Wheels", true);

        window.possibleWheels = allPossibleWheels;
        window.onButtonClick = onButtonClickAction;

        window.minSize = new Vector2(500f, 300f);
        window.ShowUtility();

    }

    #endregion

    //────────────────────────────────────────────────────────────────────//
    #region Unity Events
    //────────────────────────────────────────────────────────────────────//

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");
        allSelectedWheels.Clear();

        SceneView.duringSceneGui += OnSceneGUI;

    }

    private void OnDisable() {

        SceneView.duringSceneGui -= OnSceneGUI;
        possibleWheels = null;
        allSelectedWheels.Clear();
        hoveredWheel = null;

    }

    private void OnGUI() {

        GUI.skin = skin ? skin : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

        GUILayout.Label(
            "Possible wheel transforms found in the selected vehicle.\n" +
            "Select the PARENT wheel objects, not child meshes.",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Tip: Hover a row to preview the wheel in the Scene view. Green rows are already picked.",
            MessageType.Info);

        DrawWheelList();

        GUILayout.FlexibleSpace();

        EditorGUILayout.HelpBox(
            "If your model axes are inverted, the wizard may mis‑calculate offsets.",
            MessageType.None);

        if (GUILayout.Button("Save & Close", GUILayout.Height(28f))) {

            onButtonClick?.Invoke(allSelectedWheels.ToArray());
            Close();

            if (onButtonClick == null)
                Debug.LogWarning("RCCP_PopupWindow_PossibleWheels closed but no callback was assigned.");

        }

    }

    #endregion

    //────────────────────────────────────────────────────────────────────//
    #region Scene GUI Preview
    //────────────────────────────────────────────────────────────────────//

    private void OnSceneGUI(SceneView sceneView) {

        if (hoveredWheel == null)
            return;

        Handles.color = Color.yellow;
        float radius = .4f;
        if (hoveredWheel.TryGetComponent(out WheelCollider wc))
            radius = wc.radius;

        Handles.DrawWireDisc(hoveredWheel.transform.position, Vector3.up, radius);
        Handles.DrawWireDisc(hoveredWheel.transform.position, Vector3.right, radius);
        Handles.DrawWireDisc(hoveredWheel.transform.position, Vector3.forward, radius);

        sceneView.Repaint();

    }

    #endregion

    //────────────────────────────────────────────────────────────────────//
    #region Helpers
    //────────────────────────────────────────────────────────────────────//

    private void DrawWheelList() {

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (possibleWheels != null && possibleWheels.Length > 0) {

            foreach (GameObject wheel in possibleWheels) {

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                bool isSelected = allSelectedWheels.Contains(wheel);

                Color old = GUI.color;
                if (isSelected)
                    GUI.color = Color.green;

                string label = (isSelected ? "Selected: " : "Select: ") + wheel.name;
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true))) {

                    if (isSelected)
                        allSelectedWheels.Remove(wheel);
                    else
                        allSelectedWheels.Add(wheel);

                    GUI.FocusControl(null);

                }

                GUI.color = old;

                if (GUILayout.Button("Ping", GUILayout.Width(50f))) {

                    EditorGUIUtility.PingObject(wheel);
                    Selection.activeObject = wheel;
                    SceneView.lastActiveSceneView?.FrameSelected();

                }

                EditorGUILayout.EndHorizontal();

                //  Hover detection – must be after layout has ended, so use rowRect.
                if (rowRect.Contains(Event.current.mousePosition)) {

                    if (hoveredWheel != wheel) {
                        hoveredWheel = wheel;
                        Repaint();
                    }

                }

            }

        } else {

            EditorGUILayout.HelpBox("No wheel candidates were supplied.", MessageType.Warning);

        }

        EditorGUILayout.EndScrollView();

        //  Clear highlight if mouse is outside the scroll area.
        if (Event.current.type == EventType.MouseMove && !position.Contains(Event.current.mousePosition)) {
            hoveredWheel = null;
        }

    }

    #endregion

}
