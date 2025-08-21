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

[CustomEditor(typeof(RCC_Camera))]
public class RCC_CameraEditor : Editor {

    RCC_Camera prop;

    GUISkin skin;

    // Foldout toggles
    private static bool showTargetSettings = true;
    private static bool showGeneralSettings = true;
    private static bool showTPSSettings = true;
    private static bool showHoodSettings = false;
    private static bool showWheelSettings = false;
    private static bool showFixedSettings = false;
    private static bool showCinematicSettings = false;
    private static bool showTopSettings = false;
    private static bool showOcclusionSettings = false;
    private static bool showOrbitZoomSettings = false;

    // Serialized properties
    private SerializedProperty cameraTargetProp;
    private SerializedProperty isRenderingProp;
    private SerializedProperty actualCameraProp;
    private SerializedProperty pivotProp;
    private SerializedProperty cameraModeProp;

    private SerializedProperty TPSLockXProp;
    private SerializedProperty TPSLockYProp;
    private SerializedProperty TPSLockZProp;
    private SerializedProperty TPSFreeFallProp;
    private SerializedProperty TPSDynamicProp;
    private SerializedProperty TPSDistanceProp;
    private SerializedProperty TPSHeightProp;
    private SerializedProperty TPSRotationDampingProp;
    private SerializedProperty TPSOffsetProp;
    private SerializedProperty TPSStartRotationProp;
    private SerializedProperty TPSAutoFocusProp;
    private SerializedProperty TPSAutoReverseProp;
    private SerializedProperty TPSCollisionProp;
    private SerializedProperty TPSTiltMaximumProp;
    private SerializedProperty TPSTiltMultiplierProp;
    private SerializedProperty TPSYawProp;
    private SerializedProperty TPSPitchProp;
    private SerializedProperty TPSMinimumFOVProp;
    private SerializedProperty TPSMaximumFOVProp;

    private SerializedProperty hoodCameraFOVProp;
    private SerializedProperty useOrbitInHoodCameraModeProp;

    private SerializedProperty wheelCameraFOVProp;
    private SerializedProperty useWheelCameraModeProp;

    private SerializedProperty useFixedCameraModeProp;
    private SerializedProperty useCinematicCameraModeProp;

    private SerializedProperty useTopCameraModeProp;
    private SerializedProperty useOrthoForTopCameraProp;
    private SerializedProperty topCameraAngleProp;
    private SerializedProperty topCameraDistanceProp;
    private SerializedProperty maximumZDistanceOffsetProp;
    private SerializedProperty minimumOrtSizeProp;
    private SerializedProperty maximumOrtSizeProp;

    private SerializedProperty useOcclusionProp;
    private SerializedProperty occlusionLayerMaskProp;

    private SerializedProperty useAutoChangeCameraProp;

    private SerializedProperty useOrbitInTPSCameraModeProp;
    private SerializedProperty minOrbitYProp;
    private SerializedProperty maxOrbitYProp;
    private SerializedProperty orbitXSpeedProp;
    private SerializedProperty orbitYSpeedProp;
    private SerializedProperty orbitResetProp;

    void OnEnable() {

        skin = Resources.Load("RCC_WindowSkin") as GUISkin;

        // Cache serialized properties
        cameraTargetProp = serializedObject.FindProperty("cameraTarget");

        isRenderingProp = serializedObject.FindProperty("isRendering");
        actualCameraProp = serializedObject.FindProperty("actualCamera");
        pivotProp = serializedObject.FindProperty("pivot");
        cameraModeProp = serializedObject.FindProperty("cameraMode");

        TPSLockXProp = serializedObject.FindProperty("TPSLockX");
        TPSLockYProp = serializedObject.FindProperty("TPSLockY");
        TPSLockZProp = serializedObject.FindProperty("TPSLockZ");
        TPSFreeFallProp = serializedObject.FindProperty("TPSFreeFall");
        TPSDynamicProp = serializedObject.FindProperty("TPSDynamic");
        TPSDistanceProp = serializedObject.FindProperty("TPSDistance");
        TPSHeightProp = serializedObject.FindProperty("TPSHeight");
        TPSRotationDampingProp = serializedObject.FindProperty("TPSRotationDamping");
        TPSOffsetProp = serializedObject.FindProperty("TPSOffset");
        TPSStartRotationProp = serializedObject.FindProperty("TPSStartRotation");
        TPSAutoFocusProp = serializedObject.FindProperty("TPSAutoFocus");
        TPSAutoReverseProp = serializedObject.FindProperty("TPSAutoReverse");
        TPSCollisionProp = serializedObject.FindProperty("TPSCollision");
        TPSTiltMaximumProp = serializedObject.FindProperty("TPSTiltMaximum");
        TPSTiltMultiplierProp = serializedObject.FindProperty("TPSTiltMultiplier");
        TPSYawProp = serializedObject.FindProperty("TPSYaw");
        TPSPitchProp = serializedObject.FindProperty("TPSPitch");
        TPSMinimumFOVProp = serializedObject.FindProperty("TPSMinimumFOV");
        TPSMaximumFOVProp = serializedObject.FindProperty("TPSMaximumFOV");

        hoodCameraFOVProp = serializedObject.FindProperty("hoodCameraFOV");
        useOrbitInHoodCameraModeProp = serializedObject.FindProperty("useOrbitInHoodCameraMode");

        wheelCameraFOVProp = serializedObject.FindProperty("wheelCameraFOV");
        useWheelCameraModeProp = serializedObject.FindProperty("useWheelCameraMode");

        useFixedCameraModeProp = serializedObject.FindProperty("useFixedCameraMode");
        useCinematicCameraModeProp = serializedObject.FindProperty("useCinematicCameraMode");

        useTopCameraModeProp = serializedObject.FindProperty("useTopCameraMode");
        useOrthoForTopCameraProp = serializedObject.FindProperty("useOrthoForTopCamera");
        topCameraAngleProp = serializedObject.FindProperty("topCameraAngle");
        topCameraDistanceProp = serializedObject.FindProperty("topCameraDistance");
        maximumZDistanceOffsetProp = serializedObject.FindProperty("maximumZDistanceOffset");
        minimumOrtSizeProp = serializedObject.FindProperty("minimumOrtSize");
        maximumOrtSizeProp = serializedObject.FindProperty("maximumOrtSize");

        useOcclusionProp = serializedObject.FindProperty("useOcclusion");
        occlusionLayerMaskProp = serializedObject.FindProperty("occlusionLayerMask");

        useAutoChangeCameraProp = serializedObject.FindProperty("useAutoChangeCamera");

        useOrbitInTPSCameraModeProp = serializedObject.FindProperty("useOrbitInTPSCameraMode");
        minOrbitYProp = serializedObject.FindProperty("minOrbitY");
        maxOrbitYProp = serializedObject.FindProperty("maxOrbitY");
        orbitXSpeedProp = serializedObject.FindProperty("orbitXSpeed");
        orbitYSpeedProp = serializedObject.FindProperty("orbitYSpeed");
        orbitResetProp = serializedObject.FindProperty("orbitReset");

    }

    public override void OnInspectorGUI() {

        prop = (RCC_Camera)target;
        serializedObject.Update();

        GUI.skin = skin != null ? skin : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Main Camera designed for RCC. Includes 6 different camera modes. Doesn't use many cameras for different modes like other assets. Just one single camera handles them.", MessageType.Info);
        EditorGUILayout.Space();

        // Target Settings
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showTargetSettings = EditorGUILayout.Foldout(showTargetSettings, "Target Settings", true);
        if (showTargetSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cameraTargetProp.FindPropertyRelative("playerVehicle"),
                new GUIContent("Player Vehicle"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // General Settings
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
        if (showGeneralSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cameraModeProp, new GUIContent("Camera Mode"));
            EditorGUILayout.PropertyField(isRenderingProp, new GUIContent("Is Rendering"));
            EditorGUILayout.PropertyField(actualCameraProp, new GUIContent("Actual Camera"));
            EditorGUILayout.PropertyField(pivotProp, new GUIContent("Pivot"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // TPS Camera Settings
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showTPSSettings = EditorGUILayout.Foldout(showTPSSettings, "TPS Camera Settings", true);
        if (showTPSSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(TPSLockXProp, new GUIContent("Lock X Rotation"));
            EditorGUILayout.PropertyField(TPSLockYProp, new GUIContent("Lock Y Rotation"));
            EditorGUILayout.PropertyField(TPSLockZProp, new GUIContent("Lock Z Rotation"));
            EditorGUILayout.PropertyField(TPSFreeFallProp, new GUIContent("Free Fall"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(TPSDistanceProp, new GUIContent("Distance"));
            EditorGUILayout.PropertyField(TPSHeightProp, new GUIContent("Height"));
            EditorGUILayout.PropertyField(TPSRotationDampingProp, new GUIContent("Rotation Damping"));
            EditorGUILayout.PropertyField(TPSDynamicProp, new GUIContent("Dynamic (based on velocity)"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(TPSTiltMaximumProp, new GUIContent("Max Tilt Angle"));
            EditorGUILayout.PropertyField(TPSTiltMultiplierProp, new GUIContent("Tilt Multiplier"));
            EditorGUILayout.PropertyField(TPSYawProp, new GUIContent("Yaw Offset"));
            EditorGUILayout.PropertyField(TPSPitchProp, new GUIContent("Pitch Offset"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(TPSMinimumFOVProp, new GUIContent("Min FOV"));
            EditorGUILayout.PropertyField(TPSMaximumFOVProp, new GUIContent("Max FOV"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(TPSOffsetProp, new GUIContent("Position Offset"));
            EditorGUILayout.PropertyField(TPSStartRotationProp, new GUIContent("Start Rotation"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(TPSAutoFocusProp, new GUIContent("Auto Focus"));
            EditorGUILayout.PropertyField(TPSAutoReverseProp, new GUIContent("Auto Reverse"));
            EditorGUILayout.PropertyField(TPSCollisionProp, new GUIContent("Enable Collision Effects"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // Orbit & Zoom Settings (common for TPS and optionally Hood)
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showOrbitZoomSettings = EditorGUILayout.Foldout(showOrbitZoomSettings, "Orbit & Zoom Settings", true);
        if (showOrbitZoomSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useOrbitInTPSCameraModeProp, new GUIContent("Use Orbit in TPS Mode"));
            EditorGUILayout.PropertyField(useOrbitInHoodCameraModeProp, new GUIContent("Use Orbit in Hood Mode"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Orbit Limits & Speeds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minOrbitYProp, new GUIContent("Min Orbit Y"));
            EditorGUILayout.PropertyField(maxOrbitYProp, new GUIContent("Max Orbit Y"));
            EditorGUILayout.PropertyField(orbitXSpeedProp, new GUIContent("Orbit X Speed"));
            EditorGUILayout.PropertyField(orbitYSpeedProp, new GUIContent("Orbit Y Speed"));
            EditorGUILayout.PropertyField(orbitResetProp, new GUIContent("Orbit Reset"));

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // FPS (Hood) Camera
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showHoodSettings = EditorGUILayout.Foldout(showHoodSettings, "Hood (FPS) Camera Settings", true);
        if (showHoodSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(hoodCameraFOVProp, new GUIContent("Hood Camera FOV"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // Wheel Camera
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showWheelSettings = EditorGUILayout.Foldout(showWheelSettings, "Wheel Camera Settings", true);
        if (showWheelSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(wheelCameraFOVProp, new GUIContent("Wheel Camera FOV"));
            EditorGUILayout.PropertyField(useWheelCameraModeProp, new GUIContent("Use Wheel Camera Mode"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // Fixed Camera
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showFixedSettings = EditorGUILayout.Foldout(showFixedSettings, "Fixed Camera Settings", true);
        if (showFixedSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useFixedCameraModeProp, new GUIContent("Use Fixed Camera Mode"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // Cinematic Camera
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showCinematicSettings = EditorGUILayout.Foldout(showCinematicSettings, "Cinematic Camera Settings", true);
        if (showCinematicSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useCinematicCameraModeProp, new GUIContent("Use Cinematic Camera Mode"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // Top Camera
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showTopSettings = EditorGUILayout.Foldout(showTopSettings, "Top Camera Settings", true);
        if (showTopSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useTopCameraModeProp, new GUIContent("Use Top Camera Mode"));
            EditorGUILayout.PropertyField(useOrthoForTopCameraProp, new GUIContent("Use Orthographic"));
            EditorGUILayout.PropertyField(topCameraAngleProp, new GUIContent("Top Camera Angle"));
            EditorGUILayout.PropertyField(topCameraDistanceProp, new GUIContent("Top Camera Distance"));
            EditorGUILayout.PropertyField(maximumZDistanceOffsetProp, new GUIContent("Max Z Distance Offset"));
            EditorGUILayout.PropertyField(minimumOrtSizeProp, new GUIContent("Min Orthographic Size"));
            EditorGUILayout.PropertyField(maximumOrtSizeProp, new GUIContent("Max Orthographic Size"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // Occlusion & Collisions
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showOcclusionSettings = EditorGUILayout.Foldout(showOcclusionSettings, "Occlusion Settings", true);
        if (showOcclusionSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useOcclusionProp, new GUIContent("Use Occlusion"));
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
            EditorGUILayout.PropertyField(occlusionLayerMaskProp, new GUIContent("Occlusion Layer Mask"));
            GUI.skin = skin != null ? skin : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;

        // Auto Change Camera
        EditorGUILayout.PropertyField(useAutoChangeCameraProp, new GUIContent("Use Auto Change Camera"));

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();
    }
}
