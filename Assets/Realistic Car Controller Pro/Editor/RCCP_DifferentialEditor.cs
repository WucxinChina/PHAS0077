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
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCCP_Differential))]
public class RCCP_DifferentialEditor : Editor {

    RCCP_Differential prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Differential)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox(
            "Transmits the received power from the engine → clutch → gearbox to the axle. " +
            "Open differential = RPM difference between both wheels will decide to which wheel needs more traction or not. " +
            "Limited = almost same with open with slip limitation. Higher percents = more close to the locked system. " +
            "Locked = both wheels will have the same traction.",
            MessageType.Info,
            true
        );

        if (BehaviorSelected())
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("differentialType"),
            new GUIContent("Differential Type", "Differential types. Types are explained above.")
        );

        GUI.color = guiColor;

        if (prop.differentialType == RCCP_Differential.DifferentialType.Limited)
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("limitedSlipRatio"),
                new GUIContent("Limited Slip Ratio", "Limited slip ratio (0–100%). Lower values = more open; higher values = closer to locked.")
            );

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("finalDriveRatio"),
            new GUIContent("Final Drive Ratio", "Multiplier applied to received torque from the gearbox.")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("connectedAxle"),
            new GUIContent("Connected Axle", "Assign the axle to receive torque outputs."),
            true
        );

        EditorGUILayout.Space();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("receivedTorqueAsNM"),
            new GUIContent("Received Torque As NM", "Torque input from the gearbox, in Nm.")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("producedTorqueAsNM"),
            new GUIContent("Produced Torque As NM", "Total torque after final drive ratio, in Nm.")
        );
        GUI.enabled = true;

        DrawConnectionButtons();

        CheckMisconfig();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

            if (prop.GetComponentInParent<RCCP_CarController>(true).checkComponents) {

                prop.GetComponentInParent<RCCP_CarController>(true).checkComponents = false;

                if (errorMessages.Count > 0) {

                    if (EditorUtility.DisplayDialog(
                        "Realistic Car Controller Pro | Errors found",
                        errorMessages.Count + " Errors found!",
                        "Cancel",
                        "Check"
                    ))
                        Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

                } else {

                    Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;
                    Debug.Log("No errors found");

                }

            }

            EditorGUILayout.EndVertical();

        }

        if (BehaviorSelected())
            EditorGUILayout.HelpBox(
                "Settings with red labels will be overridden by the selected behavior in RCCP_Settings",
                MessageType.None
            );

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        // --- Statistics Section ---
        DrawStatisticsSection();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    /// <summary>
    /// Draws read-only differential calculation statistics in the inspector.
    /// </summary>
    private void DrawStatisticsSection() {

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        GUI.enabled = false;

        EditorGUILayout.FloatField(
            new GUIContent("Left Wheel RPM", "Absolute RPM of the left wheel."),
            prop.leftWheelRPM
        );
        EditorGUILayout.FloatField(
            new GUIContent("Right Wheel RPM", "Absolute RPM of the right wheel."),
            prop.rightWheelRPM
        );
        EditorGUILayout.FloatField(
            new GUIContent("Wheel Slip Ratio", "Normalized slip between wheels (0 = none, 1 = max)."),
            prop.wheelSlipRatio
        );
        EditorGUILayout.FloatField(
            new GUIContent("Left Wheel Slip Ratio", "Slip ratio applied to the left wheel."),
            prop.leftWheelSlipRatio
        );
        EditorGUILayout.FloatField(
            new GUIContent("Right Wheel Slip Ratio", "Slip ratio applied to the right wheel."),
            prop.rightWheelSlipRatio
        );
        EditorGUILayout.FloatField(
            new GUIContent("Output Left (Nm)", "Torque output to the left wheel, in Nm."),
            prop.outputLeft
        );
        EditorGUILayout.FloatField(
            new GUIContent("Output Right (Nm)", "Torque output to the right wheel, in Nm."),
            prop.outputRight
        );
        EditorGUILayout.FloatField(
            new GUIContent("Produced Torque As NM", "Total torque produced (left + right), in Nm."),
            prop.producedTorqueAsNM
        );

        GUI.enabled = true;
        EditorGUILayout.EndVertical();

    }

    private void DrawConnectionButtons() {

        if (prop.connectedAxle == null) {

            RCCP_Axle[] axle = prop.GetComponentInParent<RCCP_CarController>(true)
                .GetComponentsInChildren<RCCP_Axle>(true);

            if (axle != null && axle.Length > 0) {

                for (int i = 0; i < axle.Length; i++) {

                    if (GUILayout.Button("Connect to " + axle[i].gameObject.name)) {

                        prop.connectedAxle = axle[i];
                        EditorUtility.SetDirty(prop);

                    }

                }

            }

        } else {

            GUI.color = Color.red;

            if (GUILayout.Button(
                "Remove connection to " + prop.connectedAxle.gameObject.name
            )) {

                bool decision = EditorUtility.DisplayDialog(
                    "Realistic Car Controller Pro | Remove connection to " +
                    prop.connectedAxle.gameObject.name,
                    "Are you sure want to remove connection to the " +
                    prop.connectedAxle.gameObject.name + "?",
                    "Yes",
                    "No"
                );

                if (decision) {

                    prop.connectedAxle = null;
                    EditorUtility.SetDirty(prop);

                }

            }

            GUI.color = guiColor;

        }

    }

    private void CheckMisconfig() {

        bool completeSetup = true;
        errorMessages.Clear();

        if (prop.connectedAxle == null)
            errorMessages.Add("Output axle not selected");

        if (errorMessages.Count > 0)
            completeSetup = false;

        prop.completeSetup = completeSetup;

        if (!completeSetup)
            EditorGUILayout.HelpBox("Errors found!", MessageType.Error, true);

        GUI.color = Color.red;

        for (int i = 0; i < errorMessages.Count; i++) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(errorMessages[i]);
            EditorGUILayout.EndVertical();

        }

        GUI.color = guiColor;

    }

    private bool BehaviorSelected() {

        bool state = RCCP_Settings.Instance.overrideBehavior;

        if (prop.GetComponentInParent<RCCP_CarController>(true).ineffectiveBehavior)
            state = false;

        return state;

    }

}
