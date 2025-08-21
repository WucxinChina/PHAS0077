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

[CustomEditor(typeof(RCCP_WheelCollider))]
[CanEditMultipleObjects]
public class RCCP_WheelColliderEditor : Editor {

    // Reference to the target component
    private RCCP_WheelCollider prop;
    // Error message list for misconfiguration checks
    private List<string> errorMessages = new List<string>();
    // Custom GUI skin for styling
    private GUISkin skin;
    // Backup of original GUI color
    private Color guiColor;
    // Toggle for showing runtime statistics
    private bool showStatistics = true;

    // Serialized properties for inspector
    private SerializedProperty alignWheels;
    private SerializedProperty connectedAxle;
    private SerializedProperty wheelModel;
    private SerializedProperty offset;
    private SerializedProperty camber;
    private SerializedProperty caster;
    private SerializedProperty width;
    private SerializedProperty drawSkid;
    private SerializedProperty deflated;
    private SerializedProperty deflatedRadiusMultiplier;
    private SerializedProperty deflatedStiffnessMultiplier;
    private SerializedProperty driftMode;
    private SerializedProperty wheelbase;
    private SerializedProperty trackWidth;

    // Runtime statistics properties
    private SerializedProperty isGrounded;
    private SerializedProperty isSkidding;
    private SerializedProperty groundIndex;
    private SerializedProperty motorTorque;
    private SerializedProperty brakeTorque;
    private SerializedProperty steerInput;
    private SerializedProperty handbrakeInput;
    private SerializedProperty negativeFeedbackIntensity;
    private SerializedProperty wheelRPM2Speed;
    private SerializedProperty wheelRPM;
    private SerializedProperty totalSlip;
    private SerializedProperty wheelSlipAmountForward;
    private SerializedProperty wheelSlipAmountSideways;
    private SerializedProperty totalWheelTemp;
    private SerializedProperty bumpForce;

    /// <summary>
    /// Called when the inspector is enabled. Finds all relevant serialized properties.
    /// </summary>
    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

        // Main settings
        alignWheels = serializedObject.FindProperty("alignWheels");
        connectedAxle = serializedObject.FindProperty("connectedAxle");
        wheelModel = serializedObject.FindProperty("wheelModel");

        // Wheel setup
        offset = serializedObject.FindProperty("offset");
        camber = serializedObject.FindProperty("camber");
        caster = serializedObject.FindProperty("caster");
        width = serializedObject.FindProperty("width");

        // Deflation settings
        deflated = serializedObject.FindProperty("deflated");
        deflatedRadiusMultiplier = serializedObject.FindProperty("deflatedRadiusMultiplier");
        deflatedStiffnessMultiplier = serializedObject.FindProperty("deflatedStiffnessMultiplier");

        // Additional options
        drawSkid = serializedObject.FindProperty("drawSkid");
        driftMode = serializedObject.FindProperty("driftMode");

        // Steering and dimensions
        wheelbase = serializedObject.FindProperty("wheelbase");
        trackWidth = serializedObject.FindProperty("trackWidth");

        // Runtime statistics
        isGrounded = serializedObject.FindProperty("isGrounded");
        isSkidding = serializedObject.FindProperty("isSkidding");
        groundIndex = serializedObject.FindProperty("groundIndex");
        motorTorque = serializedObject.FindProperty("motorTorque");
        brakeTorque = serializedObject.FindProperty("brakeTorque");
        steerInput = serializedObject.FindProperty("steerInput");
        handbrakeInput = serializedObject.FindProperty("handbrakeInput");
        negativeFeedbackIntensity = serializedObject.FindProperty("negativeFeedbackIntensity");
        wheelRPM2Speed = serializedObject.FindProperty("wheelRPM2Speed");
        totalWheelTemp = serializedObject.FindProperty("totalWheelTemp");
        bumpForce = serializedObject.FindProperty("bumpForce");

    }

    /// <summary>
    /// Draws the custom inspector GUI for RCCP_WheelCollider.
    /// </summary>
    public override void OnInspectorGUI() {

        prop = (RCCP_WheelCollider)target;
        serializedObject.Update();
        GUI.skin = skin;

        float wheelRPM = prop.WheelRPM;
        float totalSlip = prop.TotalSlip;
        float wheelSlipAmountForward = prop.ForwardSlip;
        float wheelSlipAmountSideways = prop.SidewaysSlip;

        // Main properties
        EditorGUILayout.PropertyField(
            connectedAxle,
            new GUIContent("Connected Axle", "Which axle this wheel belongs to.")
        );
        EditorGUILayout.PropertyField(
            wheelModel,
            new GUIContent("Wheel Model", "Transform for the visual wheel representation.")
        );
        EditorGUILayout.PropertyField(
            alignWheels,
            new GUIContent("Align Wheels", "Match visual model to collider each frame.")
        );

        EditorGUILayout.Space();

        // Wheel Setup
        EditorGUILayout.LabelField("Wheel Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            offset,
            new GUIContent("Wheel Offset", "Horizontal offset for visual adjustment.")
        );
        EditorGUILayout.PropertyField(
            camber,
            new GUIContent("Camber Angle", "Tilt around the forward axis.")
        );
        EditorGUILayout.PropertyField(
            caster,
            new GUIContent("Caster Angle", "Tilt of steering pivot.")
        );
        EditorGUILayout.PropertyField(
            width,
            new GUIContent("Wheel Width", "Width used for skid and visuals.")
        );

        EditorGUILayout.Space();

        // Deflation Settings
        EditorGUILayout.LabelField("Deflation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            deflated,
            new GUIContent("Deflated", "Mark wheel as deflated.")
        );
        EditorGUILayout.PropertyField(
            deflatedRadiusMultiplier,
            new GUIContent("Radius Multiplier", "Scale for radius when deflated.")
        );
        EditorGUILayout.PropertyField(
            deflatedStiffnessMultiplier,
            new GUIContent("Stiffness Multiplier", "Scale for stiffness when deflated.")
        );

        EditorGUILayout.Space();

        // Additional Options
        EditorGUILayout.LabelField("Additional Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            drawSkid,
            new GUIContent("Draw Skidmarks", "Generate skidmarks on slip.")
        );
        EditorGUILayout.PropertyField(
            driftMode,
            new GUIContent("Drift Mode", "Modify friction for drifting.")
        );

        EditorGUILayout.Space();

        // Steering / Dimensions
        EditorGUILayout.LabelField("Steering / Dimensions", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(
            wheelbase,
            new GUIContent("Wheelbase", "Distance between front and rear axles.")
        );
        EditorGUILayout.PropertyField(
         trackWidth,
         new GUIContent("Track Width", "Distance between left and right wheels.")
     );

        EditorGUILayout.Space();

        // Runtime Statistics Foldout
        showStatistics = EditorGUILayout.Foldout(showStatistics, "Runtime Statistics");
        if (showStatistics) {
            if (Application.isPlaying) {
                EditorGUI.indentLevel++;

                // Ground Status
                EditorGUILayout.LabelField("Ground Status", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    isGrounded,
                    new GUIContent("Is Grounded", "Whether the wheel is in contact.")
                );
                EditorGUILayout.PropertyField(
                    isSkidding,
                    new GUIContent("Is Skidding", "Whether the wheel is slipping.")
                );
                EditorGUILayout.PropertyField(
                 groundIndex,
                 new GUIContent("Ground Index", "Index of ground material.")
             );

                EditorGUILayout.Space();

                // Input / Forces
                EditorGUILayout.LabelField("Input / Forces", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    motorTorque,
                    new GUIContent("Motor Torque", "Torque applied in Nm.")
                );
                EditorGUILayout.PropertyField(
                    brakeTorque,
                    new GUIContent("Brake Torque", "Brake torque in Nm.")
                );
                EditorGUILayout.PropertyField(
                    steerInput,
                    new GUIContent("Steer Input", "Steering angle input.")
                );
                EditorGUILayout.PropertyField(
                    handbrakeInput,
                    new GUIContent("Handbrake Input", "Handbrake pressure.")
                );
                EditorGUILayout.PropertyField(
                    negativeFeedbackIntensity,
                    new GUIContent("Negative Feedback", "Intensity of negative feedback.")
                );

                EditorGUILayout.Space();

                // Slip & Speed
                EditorGUILayout.LabelField("Slip & Speed", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                 wheelRPM2Speed,
                 new GUIContent("Wheel Speed", "Calculated speed from RPM.")
             );

                EditorGUILayout.LabelField("Wheel RPM", wheelRPM.ToString("F2"));
                EditorGUILayout.LabelField("Total Slip", totalSlip.ToString("F2"));
                EditorGUILayout.LabelField("Forward Slip", wheelSlipAmountForward.ToString("F2"));
                EditorGUILayout.LabelField("Sideways Slip", wheelSlipAmountSideways.ToString("F2"));

                EditorGUILayout.Space();

                // Temperature & Bump
                EditorGUILayout.LabelField("Temperature & Bump", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    totalWheelTemp,
                    new GUIContent("Wheel Temperature", "Thermal state of the wheel.")
                );
                EditorGUILayout.PropertyField(
                    bumpForce,
                    new GUIContent("Bump Force", "Impact force magnitude.")
                );

                EditorGUI.indentLevel--;
            } else {
                EditorGUILayout.HelpBox("Runtime statistics are available in Play Mode only.", MessageType.Info);
            }
        }

        // Reset GUI color
        GUI.color = guiColor;

        // Show Back button and auto-align in Editor
        if (!EditorUtility.IsPersistent(prop)) {
            if (GUILayout.Button("Back")) {
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;
            }
            if (!EditorApplication.isPlaying && prop.connectedAxle != null && prop.connectedAxle.autoAlignWheelColliders) {
                prop.AlignWheel();
            }
        }

        // Misconfiguration checks
        CheckMisconfig();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed) {
            EditorUtility.SetDirty(prop);
        }

    }

    /// <summary>
    /// Checks for missing setup and displays errors.
    /// </summary>
    private void CheckMisconfig() {

        errorMessages.Clear();
        bool completeSetup = true;

        if (prop.connectedAxle == null) {
            errorMessages.Add("Axle not selected");
            completeSetup = false;
        }
        if (prop.wheelModel == null) {
            errorMessages.Add("Wheel model not selected");
            completeSetup = false;
        }

        prop.completeSetup = completeSetup;

        if (!completeSetup) {
            EditorGUILayout.HelpBox("Errors found!", MessageType.Error, true);
            GUI.color = Color.red;
            foreach (string msg in errorMessages) {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(msg);
                EditorGUILayout.EndVertical();
            }
            GUI.color = guiColor;
        }

    }

}
