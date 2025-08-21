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

/// <summary>
/// Custom inspector for RCCP_AI component.
/// Provides organized, styled layout and enforces non-negative values where required.
/// </summary>
[CustomEditor(typeof(RCCP_AI))]
public class RCCP_AIEditor : Editor {

    // Serialized properties
    private SerializedProperty behaviourProp;
    private SerializedProperty waypointsContainerProp;
    private SerializedProperty targetProp;
    private SerializedProperty waypointReachThresholdProp;
    private SerializedProperty raceLookAheadProp;
    private SerializedProperty roadGripProp;
    private SerializedProperty maxThrottleProp;
    private SerializedProperty maxBrakeProp;
    private SerializedProperty agressivenessProp;
    private SerializedProperty steerSensitivityProp;
    private SerializedProperty minLookAheadProp;
    private SerializedProperty lookAheadPerKphProp;
    private SerializedProperty kpProp;
    private SerializedProperty kiProp;
    private SerializedProperty kdProp;
    private SerializedProperty followTargetDistanceProp;
    private SerializedProperty chasePredictionTimeProp;
    private SerializedProperty stopNowProp;
    private SerializedProperty reverseNowProp;
    private SerializedProperty checkStuckProp;

    // Toggle for displaying detailed info
    private bool showInfo = false;

    private void OnEnable() {
        // Cache properties
        behaviourProp = serializedObject.FindProperty("behaviour");
        waypointsContainerProp = serializedObject.FindProperty("waypointsContainer");
        targetProp = serializedObject.FindProperty("target");
        waypointReachThresholdProp = serializedObject.FindProperty("waypointReachThreshold");
        raceLookAheadProp = serializedObject.FindProperty("raceLookAhead");
        roadGripProp = serializedObject.FindProperty("roadGrip");
        maxThrottleProp = serializedObject.FindProperty("maxThrottle");
        maxBrakeProp = serializedObject.FindProperty("maxBrake");
        agressivenessProp = serializedObject.FindProperty("agressiveness");
        steerSensitivityProp = serializedObject.FindProperty("steerSensitivity");
        minLookAheadProp = serializedObject.FindProperty("minLookAhead");
        lookAheadPerKphProp = serializedObject.FindProperty("lookAheadPerKph");
        kpProp = serializedObject.FindProperty("kp");
        kiProp = serializedObject.FindProperty("ki");
        kdProp = serializedObject.FindProperty("kd");
        followTargetDistanceProp = serializedObject.FindProperty("followTargetDistance");
        chasePredictionTimeProp = serializedObject.FindProperty("chasePredictionTime");
        stopNowProp = serializedObject.FindProperty("stopNow");
        reverseNowProp = serializedObject.FindProperty("reverseNow");
        checkStuckProp = serializedObject.FindProperty("checkStuck");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        // Constant info at the top describing RCCP_AI component
        EditorGUILayout.HelpBox(
            "RCCP_AI drives RCCP vehicles using Unity NavMesh.\n" +
            "Supports behaviours: FollowWaypoints, RaceWaypoints, FollowTarget, ChaseTarget.\n" +
            "Configure waypoint thresholds, look-ahead distances, driving sensitivity, PID gains,\n" +
            "and target modes here to fine-tune AI-controlled vehicle behavior.",
            MessageType.Info
        );
        EditorGUILayout.Space();

        // Show Info toggle button
        string buttonLabel = showInfo ? "Hide Info" : "Show Info";
        if (GUILayout.Button(buttonLabel)) {
            showInfo = !showInfo;
        }
        if (showInfo) {
            EditorGUILayout.HelpBox(
                "Behaviour Settings:\n  � behaviour: Select AI mode (FollowWaypoints, RaceWaypoints, FollowTarget, ChaseTarget)\n" +
                "Waypoint Settings:\n  � waypointReachThreshold: Distance to mark waypoints reached\n  � raceLookAhead: Extra look-ahead distance for racing\n" +
                "Driving Settings:\n  � roadGrip: Friction coefficient\n  � maxThrottle, maxBrake: Throttle/brake limits\n  � agressiveness: Driving aggressiveness factor\n  � steerSensitivity: Steering response multiplier\n" +
                "Look-Ahead Settings:\n  � minLookAhead: Minimum look-ahead distance\n  � lookAheadPerKph: Additional look-ahead per km/h\n" +
                "PID Settings:\n  � kp, ki, kd: PID coefficients for throttle control\n" +
                "Target Modes:\n  � followTargetDistance: Distance behind target in FollowTarget mode\n  � chasePredictionTime: Prediction time in ChaseTarget mode\n" +
                "Runtime Options:\n  � stopNow, reverseNow, checkStuck: Runtime control toggles",
                MessageType.None
            );
            EditorGUILayout.Space();
        }

        // Separator
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space();

        // Behaviour Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Behaviour Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(behaviourProp);
        EditorGUILayout.PropertyField(waypointsContainerProp);
        EditorGUILayout.PropertyField(targetProp);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space();

        // Waypoint Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Waypoint Settings", EditorStyles.boldLabel);
        waypointReachThresholdProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(waypointReachThresholdProp.displayName, waypointReachThresholdProp.tooltip),
            waypointReachThresholdProp.floatValue
        ));
        raceLookAheadProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(raceLookAheadProp.displayName, raceLookAheadProp.tooltip),
            raceLookAheadProp.floatValue
        ));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space();

        // Driving Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Driving Settings", EditorStyles.boldLabel);
        roadGripProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(roadGripProp.displayName, roadGripProp.tooltip),
            roadGripProp.floatValue
        ));
        EditorGUILayout.PropertyField(maxThrottleProp);
        EditorGUILayout.PropertyField(maxBrakeProp);
        EditorGUILayout.PropertyField(agressivenessProp);
        EditorGUILayout.PropertyField(steerSensitivityProp);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space();

        // Look-Ahead Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Look-Ahead Settings", EditorStyles.boldLabel);
        minLookAheadProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(minLookAheadProp.displayName, minLookAheadProp.tooltip),
            minLookAheadProp.floatValue
        ));
        lookAheadPerKphProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(lookAheadPerKphProp.displayName, lookAheadPerKphProp.tooltip),
            lookAheadPerKphProp.floatValue
        ));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space();

        // PID Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("PID Settings", EditorStyles.boldLabel);
        kpProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(kpProp.displayName, kpProp.tooltip), kpProp.floatValue
        ));
        kiProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(kiProp.displayName, kiProp.tooltip), kiProp.floatValue
        ));
        kdProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent(kdProp.displayName, kdProp.tooltip), kdProp.floatValue
        ));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space();

        // Target Modes
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Target Modes", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(followTargetDistanceProp);
        EditorGUILayout.PropertyField(chasePredictionTimeProp);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space();

        // Runtime Options
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Runtime Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stopNowProp);
        EditorGUILayout.PropertyField(reverseNowProp);
        EditorGUILayout.PropertyField(checkStuckProp);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
