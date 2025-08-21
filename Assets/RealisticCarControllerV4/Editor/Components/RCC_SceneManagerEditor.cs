//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCC_SceneManager))]
public class RCC_SceneManagerEditor : Editor {

    RCC_SceneManager prop;
    GUISkin skin;
    bool showDebug = false;

    public void OnEnable() {

        skin = Resources.Load("RCC_WindowSkin") as GUISkin;

    }

    public override void OnInspectorGUI() {

        prop = (RCC_SceneManager)target;
        serializedObject.Update();
        GUI.skin = skin != null ? skin : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

        EditorGUILayout.HelpBox("Scene manager that contains current player vehicle, current player camera, current player UI Canvas, current player character, recording/playing mechanim, and other vehicles as well.", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("registerLastSpawnedVehicleAsPlayerVehicle"), new GUIContent("Register Last Spawned Vehicle As Player Vehicle",
            "Registers the latest spawned vehicle as a player vehicle. Disable this option if you're spawning new vehicles at runtime, because newly spawned vehicles will be counted as player vehicle if this option is enabled."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("disableUIWhenNoPlayerVehicle"), new GUIContent("Disable UI When No Player Vehicle", "Disables the RCC UI Canvas when there is no any controllable player vehicle in the scene."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loadCustomizationAtFirst"), new GUIContent("Load Customization At First", ""), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useRecord"), new GUIContent("Use Record / Replay"), false);

        EditorGUILayout.Space();

        showDebug = EditorGUILayout.Foldout(showDebug, "Debugging Info", true);

        if (showDebug) {

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("activePlayerVehicle"), new GUIContent("Active Player Vehicle"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activePlayerCamera"), new GUIContent("Active Player Camera"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activePlayerCanvas"), new GUIContent("Active Player UI Canvas"), false);
#if BCG_ENTEREXIT
        EditorGUILayout.PropertyField(serializedObject.FindProperty("activePlayerCharacter"), new GUIContent("Active Player FPS / TPS Character"), false);
#endif
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recordMode"), new GUIContent("Record Mode"), false);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allVehicles"), new GUIContent("All Vehicles"), true);
            EditorGUILayout.Space();

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
