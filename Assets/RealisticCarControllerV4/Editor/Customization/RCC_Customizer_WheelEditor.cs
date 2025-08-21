//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RCC_Customizer_WheelManager))]
public class RCC_Customizer_WheelEditor : Editor {

    RCC_Customizer_WheelManager prop;

    public override void OnInspectorGUI() {

        prop = (RCC_Customizer_WheelManager)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("All wheels are stored in the configure wheels section", MessageType.None);

        DrawDefaultInspector();

        if (GUILayout.Button("Configure Wheels"))
            Selection.activeObject = RCC_ChangableWheels.Instance;

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        if (GUILayout.Button("Back"))
            Selection.activeGameObject = prop.GetComponentInParent<RCC_Customizer>(true).gameObject;

    }

}
