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
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(RCCP_Engine))]
public class RCCP_EngineEditor : Editor {

    RCCP_Engine prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;
    private bool statsEnabled = true;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Engine)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Main power generator of the vehicle. Produces and transmits the generated power to the clutch.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideEngineRPM"), new GUIContent("Override Engine RPM", "If true, overrides the engine RPM with an externally provided value. All internal calculations will be skipped."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRunning"), new GUIContent("Engine Running", "Indicates whether the engine is currently running."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineStarting"), new GUIContent("Engine Starting", "Indicates whether the engine is in the process of starting with a brief delay."));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumSpeed"), new GUIContent("Maximum Speed", "Maximum speed of the vehicle."));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRPM"), new GUIContent("Engine RPM", "Current engine RPM in revolutions per minute."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minEngineRPM"), new GUIContent("Min Engine RPM", "Minimum engine RPM, typical idle speed."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxEngineRPM"), new GUIContent("Max Engine RPM", "Maximum engine RPM, redline limit."));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineAccelerationRate"), new GUIContent("Acceleration Rate", "Rate at which the engine freely accelerates when the drivetrain is disengaged, such as clutch in or neutral."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDynamicAcceleration"), new GUIContent("Dynamic Acceleration", "If true, enables dynamic adjustment of acceleration rate based on engine load."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineCouplingToWheelsRate"), new GUIContent("Coupling To Wheels Rate", "How strongly the engine couples to the wheels when the clutch is engaged."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineDecelerationRate"), new GUIContent("Deceleration Rate", "Rate at which the engine RPM drops due to friction or no throttle input."));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoCreateNMCurve"), new GUIContent("Auto Create Torque Curve", "Automatically generates the NM curve based on min engine RPM, maximum torque as NM, and max engine RPM when the script is reset."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumTorqueAsNM"), new GUIContent("Maximum Torque As NM", "Peak torque in Newton-meters used when auto create NM curve is enabled."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("peakRPM"), new GUIContent("Peak Torque RPM", "Desired RPM at which the engine produces peak torque, used if auto create NM curve is true."));

        if (!prop.autoCreateNMCurve)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("NMCurve"), new GUIContent("Torque Curve NM", "Torque curve normalized 0-1 for the engine. X axis is RPM, Y axis is normalized torque 0-1."));
        else
            prop.CheckAndCreateNMCurve();

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRevLimiter"), new GUIContent("Rev Limiter", "If true, cuts fuel input once RPM approaches max engine RPM to act as a rev limiter."));

        if (prop.engineRevLimiter) {

            //EditorGUILayout.PropertyField(serializedObject.FindProperty("revLimiterThreshold"), new GUIContent("Rev Limiter Threshold", "RPM threshold where rev limiter starts to engage as percentage of max engine RPM."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("revLimiterCutFrequency"), new GUIContent("Rev Limiter Cut Frequency", "Frequency of rev limiter cuts per second when active."));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("turboCharged"), new GUIContent("Turbo Charged", "Enables forced induction simulation with turbo. If true, turbo charge PSI is calculated each frame."));

        if (prop.turboCharged) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("turboChargePsi"), new GUIContent("Turbo Charge PSI", "Current turbo pressure in PSI."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxTurboChargePsi"), new GUIContent("Max Turbo Charge PSI", "Max turbo boost in PSI that can be reached at full throttle and high RPM."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("turboChargerCoEfficient"), new GUIContent("Turbo Charger Coefficient", "Maximum torque multiplier from the turbo at max boost."));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineFriction"), new GUIContent("Engine Friction", "Engine friction factor. Higher values cause RPM to drop faster when throttle is released."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineInertia"), new GUIContent("Engine Inertia", "Engine inertia factor. Lower values let the engine rev up and down more quickly."));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("simulateEngineTemperature"), new GUIContent("Simulate Engine Temperature", "If true, simulates realistic engine temperature effects on performance."));

        if (prop.simulateEngineTemperature) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineTemperature"), new GUIContent("Engine Temperature", "Current engine operating temperature in Celsius."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("optimalTemperature"), new GUIContent("Optimal Temperature", "Optimal engine operating temperature for peak performance."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientTemperature"), new GUIContent("Ambient Temperature", "Ambient temperature affecting engine cooling rate."));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableVVT"), new GUIContent("Enable VVT", "If true, enables Variable Valve Timing simulation for enhanced torque at specific RPM ranges."));

        if (prop.enableVVT) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("vvtOptimalRange"), new GUIContent("VVT Optimal Range", "RPM range where VVT provides optimal performance boost."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vvtTorqueMultiplier"), new GUIContent("VVT Torque Multiplier", "Torque multiplier applied when engine is in VVT optimal range."));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableKnockDetection"), new GUIContent("Enable Knock Detection", "If true, enables knock detection and protection."));

        EditorGUILayout.Space();

        statsEnabled = EditorGUILayout.BeginToggleGroup(new GUIContent("Realtime Statistics", "Will be updated at runtime."), statsEnabled);

        if (statsEnabled) {

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Statistics will be updated at runtime", MessageType.Info);

            GUI.enabled = false;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRPM"), new GUIContent("Current Engine RPM", "Current engine RPM in revolutions per minute."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("producedTorqueAsNM"), new GUIContent("Produced Torque NM", "Current engine torque output in Newton-meters."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fuelInput"), new GUIContent("Current Fuel Input", "Current fuel input to the engine 0-1."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleInput"), new GUIContent("Current Idle Input", "Current idle compensation input."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineLoad"), new GUIContent("Current Engine Load", "Current engine load factor 0-1."));

            if (prop.turboCharged) {

                EditorGUILayout.PropertyField(serializedObject.FindProperty("turboChargePsi"), new GUIContent("Current Turbo PSI", "Current turbo pressure in PSI."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("turboBlowOut"), new GUIContent("Turbo Blow Out", "True if the turbo is venting due to sudden throttle closure."));

            }

            if (prop.simulateEngineTemperature)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("engineTemperature"), new GUIContent("Current Engine Temperature", "Current engine temperature in Celsius."));

            if (prop.enableKnockDetection)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("knockFactor"), new GUIContent("Current Knock Factor", "Current knock factor 0-1."));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("cutFuel"), new GUIContent("Rev Limiter Active", "True when the rev limiter is actively cutting fuel."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRunning"), new GUIContent("Engine Running", "Current engine running state."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineStarting"), new GUIContent("Engine Starting", "Current engine starting state."));

            GUI.enabled = true;

        }

        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space();

        GUI.skin = null;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("outputEvent"), new GUIContent("Output Event", "Produced torque will be transfered to this component."));
        GUI.skin = skin;

        CheckMisconfig();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Add Output To Clutch")) {

                AddListener();
                EditorUtility.SetDirty(prop);

            }

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

            if (prop.GetComponentInParent<RCCP_CarController>(true).checkComponents) {

                prop.GetComponentInParent<RCCP_CarController>(true).checkComponents = false;

                if (errorMessages.Count > 0) {

                    if (EditorUtility.DisplayDialog("Realistic Car Controller Pro | Errors found", errorMessages.Count + " Errors found!", "Cancel", "Check"))
                        Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

                } else {

                    Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;
                    Debug.Log("No errors found");

                }

            }

            EditorGUILayout.EndVertical();

        }

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (!EditorApplication.isPlaying) {

            prop.UpdateMaximumSpeed();

            if (GUI.changed)
                EditorSceneManager.MarkSceneDirty(prop.gameObject.scene);

        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    private void CheckMisconfig() {

        bool completeSetup = true;
        errorMessages.Clear();

        if (prop.minEngineRPM <= 0)
            errorMessages.Add("Minimum engine RPM couldn't be 0 or below.");

        if (prop.maxEngineRPM <= prop.minEngineRPM)
            errorMessages.Add("Maximum engine RPM couldn't be lower than or equal to minimum engine RPM.");

        if (prop.maximumTorqueAsNM <= 0)
            errorMessages.Add("Maximum torque as NM couldn't be 0 or below.");

        if (prop.peakRPM < prop.minEngineRPM || prop.peakRPM > prop.maxEngineRPM)
            errorMessages.Add("Peak torque RPM should be between minimum and maximum engine RPM.");

        if (prop.engineAccelerationRate <= 0)
            errorMessages.Add("Engine acceleration rate couldn't be 0 or below.");

        if (prop.engineDecelerationRate <= 0)
            errorMessages.Add("Engine deceleration rate couldn't be 0 or below.");

        if (prop.turboCharged && prop.maxTurboChargePsi <= 0)
            errorMessages.Add("Max turbo charge PSI couldn't be 0 or below when turbo charged is enabled.");

        if (prop.turboCharged && prop.turboChargerCoEfficient <= 1)
            errorMessages.Add("Turbo charger coefficient should be greater than 1 when turbo charged is enabled.");

        if (prop.simulateEngineTemperature && prop.ambientTemperature >= prop.optimalTemperature)
            errorMessages.Add("Ambient temperature should be lower than optimal temperature.");

        if (prop.enableVVT && (prop.vvtOptimalRange.x < prop.minEngineRPM || prop.vvtOptimalRange.y > prop.maxEngineRPM))
            errorMessages.Add("VVT optimal range should be within minimum and maximum engine RPM limits.");

        if (prop.enableVVT && prop.vvtOptimalRange.x >= prop.vvtOptimalRange.y)
            errorMessages.Add("VVT optimal range minimum value should be lower than maximum value.");

        if (prop.outputEvent == null)
            errorMessages.Add("Output event not selected");

        if (prop.outputEvent != null && prop.outputEvent.GetPersistentEventCount() < 1)
            errorMessages.Add("Output event not selected");

        if (prop.outputEvent != null && prop.outputEvent.GetPersistentEventCount() > 0 && prop.outputEvent.GetPersistentMethodName(0) == "")
            errorMessages.Add("Output event created, but object or method not selected");

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

    private void AddListener() {

        if (prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Clutch>(true) == null) {

            Debug.LogError("Clutch not found. Event is not added.");
            return;

        }

        prop.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Clutch>(true),
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Clutch>(true), targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(prop.outputEvent, methodDelegate);

    }

}