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
using System.Collections.Generic;

[CustomEditor(typeof(RCC_AICarController))]
public class RCC_AICarControllerEditor : Editor {

    GUISkin skin;

    // Foldout toggles for grouping
    private bool showNavigationFoldout = true;
    private bool showRaycastingFoldout = false;
    private bool showInputFoldout = false;
    private bool showSpeedFoldout = false;
    private bool showLapFoldout = false;
    private bool showDistanceFoldout = false;
    private bool showTargetsFoldout = false;

    // Serialized Properties (All Public Fields)
    private SerializedProperty waypointsContainer;
    private SerializedProperty currentWaypointIndex;
    private SerializedProperty targetTag;
    private SerializedProperty navigationMode;

    private SerializedProperty raycastLength;
    private SerializedProperty raycastAngle;
    private SerializedProperty obstacleLayers;
    private SerializedProperty obstacle;
    private SerializedProperty useRaycasts;
    private SerializedProperty rayOrigin;

    private SerializedProperty steerInput;
    private SerializedProperty throttleInput;
    private SerializedProperty brakeInput;
    private SerializedProperty handbrakeInput;

    private SerializedProperty limitSpeed;
    private SerializedProperty maximumSpeed;
    private SerializedProperty smoothedSteer;

    private SerializedProperty lap;
    private SerializedProperty stopAfterLap;
    private SerializedProperty stopLap;
    private SerializedProperty totalWaypointPassed;
    private SerializedProperty ignoreWaypointNow;

    private SerializedProperty detectorRadius;
    private SerializedProperty startFollowDistance;
    private SerializedProperty stopFollowDistance;

    private SerializedProperty targetsInZone;
    private SerializedProperty brakeZones;
    private SerializedProperty targetChase;
    private SerializedProperty targetBrake;

    // Called when script is loaded or values are changed in Inspector
    public void OnEnable() {

        skin = Resources.Load("RCC_WindowSkin") as GUISkin;

        // Cache all public serialized properties
        waypointsContainer = serializedObject.FindProperty("waypointsContainer");
        currentWaypointIndex = serializedObject.FindProperty("currentWaypointIndex");
        targetTag = serializedObject.FindProperty("targetTag");
        navigationMode = serializedObject.FindProperty("navigationMode");

        raycastLength = serializedObject.FindProperty("raycastLength");
        raycastAngle = serializedObject.FindProperty("raycastAngle");
        obstacleLayers = serializedObject.FindProperty("obstacleLayers");
        obstacle = serializedObject.FindProperty("obstacle");
        useRaycasts = serializedObject.FindProperty("useRaycasts");
        rayOrigin = serializedObject.FindProperty("rayOrigin");

        steerInput = serializedObject.FindProperty("steerInput");
        throttleInput = serializedObject.FindProperty("throttleInput");
        brakeInput = serializedObject.FindProperty("brakeInput");
        handbrakeInput = serializedObject.FindProperty("handbrakeInput");

        limitSpeed = serializedObject.FindProperty("limitSpeed");
        maximumSpeed = serializedObject.FindProperty("maximumSpeed");
        smoothedSteer = serializedObject.FindProperty("smoothedSteer");

        lap = serializedObject.FindProperty("lap");
        stopAfterLap = serializedObject.FindProperty("stopAfterLap");
        stopLap = serializedObject.FindProperty("stopLap");
        totalWaypointPassed = serializedObject.FindProperty("totalWaypointPassed");
        ignoreWaypointNow = serializedObject.FindProperty("ignoreWaypointNow");

        detectorRadius = serializedObject.FindProperty("detectorRadius");
        startFollowDistance = serializedObject.FindProperty("startFollowDistance");
        stopFollowDistance = serializedObject.FindProperty("stopFollowDistance");

        targetsInZone = serializedObject.FindProperty("targetsInZone");
        brakeZones = serializedObject.FindProperty("brakeZones");
        targetChase = serializedObject.FindProperty("targetChase");
        targetBrake = serializedObject.FindProperty("targetBrake");
    }

    public override void OnInspectorGUI() {
        // Update the serialized object
        serializedObject.Update();

        // Reference to the actual script
        RCC_AICarController ai = (RCC_AICarController)target;

        GUI.skin = skin != null ? skin : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

        // Title
        EditorGUILayout.LabelField("RCC AI Car Controller", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // NAVIGATION / WAYPOINTS
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;
        showNavigationFoldout = EditorGUILayout.Foldout(showNavigationFoldout, "Navigation & Waypoints");
        if (showNavigationFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(navigationMode, new GUIContent("Navigation Mode"));
            EditorGUILayout.PropertyField(waypointsContainer, new GUIContent("Waypoints Container"));
            EditorGUILayout.PropertyField(currentWaypointIndex, new GUIContent("Current Waypoint Index"));
            EditorGUILayout.PropertyField(targetTag, new GUIContent("Target Tag"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // RAYCAST / OBSTACLE DETECTION
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;
        showRaycastingFoldout = EditorGUILayout.Foldout(showRaycastingFoldout, "Raycasting & Obstacle Detection");
        if (showRaycastingFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useRaycasts, new GUIContent("Use Raycasts"));
            if (useRaycasts.boolValue) {
                EditorGUILayout.PropertyField(raycastLength, new GUIContent("Raycast Length"));
                EditorGUILayout.PropertyField(raycastAngle, new GUIContent("Raycast Angle"));
                EditorGUILayout.PropertyField(rayOrigin, new GUIContent("Ray Origin"));
                EditorGUILayout.PropertyField(obstacleLayers, new GUIContent("Obstacle Layers"));
            }
            EditorGUILayout.PropertyField(obstacle, new GUIContent("Current Obstacle"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // VEHICLE INPUT (steer, throttle, brake, handbrake)
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;
        showInputFoldout = EditorGUILayout.Foldout(showInputFoldout, "Vehicle Input (Runtime)");
        if (showInputFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(steerInput, new GUIContent("Steer Input"));
            EditorGUILayout.PropertyField(throttleInput, new GUIContent("Throttle Input"));
            EditorGUILayout.PropertyField(brakeInput, new GUIContent("Brake Input"));
            EditorGUILayout.PropertyField(handbrakeInput, new GUIContent("Handbrake Input"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // SPEED SETTINGS
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;
        showSpeedFoldout = EditorGUILayout.Foldout(showSpeedFoldout, "Speed Settings");
        if (showSpeedFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(limitSpeed, new GUIContent("Limit Speed"));
            if (limitSpeed.boolValue) {
                EditorGUILayout.PropertyField(maximumSpeed, new GUIContent("Maximum Speed"));
            }
            EditorGUILayout.PropertyField(smoothedSteer, new GUIContent("Smoothed Steering"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // LAP & WAYPOINT INFO
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;
        showLapFoldout = EditorGUILayout.Foldout(showLapFoldout, "Lap & Waypoint Info");
        if (showLapFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lap, new GUIContent("Current Lap"));
            EditorGUILayout.PropertyField(stopAfterLap, new GUIContent("Stop After Lap?"));
            if (stopAfterLap.boolValue) {
                EditorGUILayout.PropertyField(stopLap, new GUIContent("Stop Lap Index"));
            }
            EditorGUILayout.PropertyField(totalWaypointPassed, new GUIContent("Total Waypoints Passed"));
            EditorGUILayout.PropertyField(ignoreWaypointNow, new GUIContent("Ignore Waypoint Now"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // DETECTION RADII
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;
        showDistanceFoldout = EditorGUILayout.Foldout(showDistanceFoldout, "Detection Distances");
        if (showDistanceFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(detectorRadius, new GUIContent("Detector Radius"));
            EditorGUILayout.PropertyField(startFollowDistance, new GUIContent("Start Follow Distance"));
            EditorGUILayout.PropertyField(stopFollowDistance, new GUIContent("Stop Follow Distance"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // TARGETS & BRAKE ZONES
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;
        showTargetsFoldout = EditorGUILayout.Foldout(showTargetsFoldout, "Targets & Brake Zones");
        if (showTargetsFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(targetsInZone, new GUIContent("Targets In Zone"), true);
            EditorGUILayout.PropertyField(brakeZones, new GUIContent("Brake Zones"), true);
            EditorGUILayout.PropertyField(targetChase, new GUIContent("Chase Target"));
            EditorGUILayout.PropertyField(targetBrake, new GUIContent("Brake Zone Target"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        if (GUI.changed)
            EditorUtility.SetDirty(ai);

        // Apply changes back to the object
        serializedObject.ApplyModifiedProperties();
    }
}
