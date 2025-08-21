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

[CustomEditor(typeof(RCC_WheelCollider))]
[CanEditMultipleObjects]
public class RCC_WheelColliderEditor : Editor {

    RCC_WheelCollider prop;

    // SerializedProperties for the fields we want to expose in the custom inspector.
    private SerializedProperty wheelModel;
    private SerializedProperty alignWheel;
    private SerializedProperty drawSkid;
    private SerializedProperty canPower;
    private SerializedProperty powerMultiplier;
    private SerializedProperty canSteer;
    private SerializedProperty steeringMultiplier;
    private SerializedProperty canBrake;
    private SerializedProperty brakingMultiplier;
    private SerializedProperty canHandbrake;
    private SerializedProperty handbrakeMultiplier;
    private SerializedProperty wheelWidth;
    private SerializedProperty wheelOffset;
    private SerializedProperty camber;
    private SerializedProperty caster;
    private SerializedProperty toe;
    private SerializedProperty forwardGrip;
    private SerializedProperty sidewaysGrip;
    private SerializedProperty deflateRadiusMultiplier;
    private SerializedProperty deflatedStiffnessMultiplier;
    private SerializedProperty ackermanWheelBase;
    private SerializedProperty ackermanSteerReference;
    private SerializedProperty ackermanTrackWidth;

    // Foldouts to organize sections in the Inspector.
    private bool showWheelState = true;
    private bool showFriction = false;
    private bool showDeflation = false;
    private bool showAckerman = false;

    // Called once when this Editor is initialized.
    public void OnEnable() {
        // Link SerializedProperties with fields in RCC_WheelCollider
        wheelModel = serializedObject.FindProperty("wheelModel");
        alignWheel = serializedObject.FindProperty("alignWheel");
        drawSkid = serializedObject.FindProperty("drawSkid");
        canPower = serializedObject.FindProperty("canPower");
        powerMultiplier = serializedObject.FindProperty("powerMultiplier");
        canSteer = serializedObject.FindProperty("canSteer");
        steeringMultiplier = serializedObject.FindProperty("steeringMultiplier");
        canBrake = serializedObject.FindProperty("canBrake");
        brakingMultiplier = serializedObject.FindProperty("brakingMultiplier");
        canHandbrake = serializedObject.FindProperty("canHandbrake");
        handbrakeMultiplier = serializedObject.FindProperty("handbrakeMultiplier");
        wheelWidth = serializedObject.FindProperty("wheelWidth");
        wheelOffset = serializedObject.FindProperty("wheelOffset");
        camber = serializedObject.FindProperty("camber");
        caster = serializedObject.FindProperty("caster");
        toe = serializedObject.FindProperty("toe");
        forwardGrip = serializedObject.FindProperty("forwardGrip");
        sidewaysGrip = serializedObject.FindProperty("sidewaysGrip");
        deflateRadiusMultiplier = serializedObject.FindProperty("deflateRadiusMultiplier");
        deflatedStiffnessMultiplier = serializedObject.FindProperty("deflatedStiffnessMultiplier");
        ackermanWheelBase = serializedObject.FindProperty("ackermanWheelBase");
        ackermanSteerReference = serializedObject.FindProperty("ackermanSteerReference");
        ackermanTrackWidth = serializedObject.FindProperty("ackermanTrackWidth");
    }

    public override void OnInspectorGUI() {

        prop = (RCC_WheelCollider)target;

        // Mandatory call for serialized objects in custom inspectors
        serializedObject.Update();

        // Main title
        EditorGUILayout.LabelField("RCC Wheel Collider", EditorStyles.boldLabel);

        // Wheel Model & Alignment
        EditorGUILayout.PropertyField(
            wheelModel,
            new GUIContent("Wheel Model", "Reference to the visual wheel mesh used for alignment.")
        );
        EditorGUILayout.PropertyField(
            alignWheel,
            new GUIContent("Align Wheel Model", "If true, updates the wheel model's position/rotation each frame.")
        );
        EditorGUILayout.PropertyField(
            drawSkid,
            new GUIContent("Draw Skidmarks", "If true, this wheel can generate skidmarks when slipping.")
        );

        EditorGUILayout.Space();
        showWheelState = EditorGUILayout.Foldout(showWheelState, "Wheel State & Geometry", true);
        if (showWheelState) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(
                canPower,
                new GUIContent("Can Power", "If true, this wheel can receive engine torque (motor torque).")
            );
            if (canPower.boolValue) {
                EditorGUILayout.PropertyField(
                    powerMultiplier,
                    new GUIContent("Power Multiplier", "Scales the torque input for this wheel.")
                );
            }

            EditorGUILayout.PropertyField(
                canSteer,
                new GUIContent("Can Steer", "If true, this wheel is steerable (affects steer angle).")
            );
            if (canSteer.boolValue) {
                EditorGUILayout.PropertyField(
                    steeringMultiplier,
                    new GUIContent("Steering Multiplier", "Scales the steering angle for this wheel.")
                );
            }

            EditorGUILayout.PropertyField(
                canBrake,
                new GUIContent("Can Brake", "If true, this wheel can receive brake torque.")
            );
            if (canBrake.boolValue) {
                EditorGUILayout.PropertyField(
                    brakingMultiplier,
                    new GUIContent("Braking Multiplier", "Scales the brake torque for this wheel.")
                );
            }

            EditorGUILayout.PropertyField(
                canHandbrake,
                new GUIContent("Can Handbrake", "If true, this wheel can be affected by the handbrake.")
            );
            if (canHandbrake.boolValue) {
                EditorGUILayout.PropertyField(
                    handbrakeMultiplier,
                    new GUIContent("Handbrake Multiplier", "Scales the handbrake force for this wheel.")
                );
            }

            EditorGUILayout.PropertyField(
                wheelWidth,
                new GUIContent("Wheel Width", "Width of the wheel for visual and skidmark calculations.")
            );
            EditorGUILayout.PropertyField(
                wheelOffset,
                new GUIContent("Wheel Offset", "Horizontal or lateral offset from the default wheel position.")
            );

            EditorGUILayout.Slider(
                camber, -5f, 5f,
                new GUIContent("Camber", "Angle (in degrees) that tilts the wheel inwards or outwards.")
            );
            EditorGUILayout.Slider(
                caster, -5f, 5f,
                new GUIContent("Caster", "Angle (in degrees) tilting the steering axis forward or backward.")
            );
            EditorGUILayout.Slider(
                toe, -5f, 5f,
                new GUIContent("Toe", "Angle (in degrees) that points wheels inward or outward from the center.")
            );
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        showFriction = EditorGUILayout.Foldout(showFriction, "Friction & Grip", true);
        if (showFriction) {
            EditorGUI.indentLevel++;
            EditorGUILayout.Slider(
                forwardGrip, 0f, 1f,
                new GUIContent("Forward Grip", "Global multiplier for the wheel's forward friction.")
            );
            EditorGUILayout.Slider(
                sidewaysGrip, 0f, 1f,
                new GUIContent("Sideways Grip", "Global multiplier for the wheel's sideways friction.")
            );
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        showDeflation = EditorGUILayout.Foldout(showDeflation, "Deflation Settings", true);
        if (showDeflation) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(
                deflateRadiusMultiplier,
                new GUIContent("Deflate Radius Multiplier", "How much the wheel radius shrinks when deflated.")
            );
            EditorGUILayout.PropertyField(
                deflatedStiffnessMultiplier,
                new GUIContent("Deflated Stiffness Multiplier", "How much the wheel's friction stiffness is reduced when deflated.")
            );
            EditorGUILayout.HelpBox(
                "Deflation settings reduce wheel radius and stiffness, " +
                "useful for simulating flat tires or punctures.",
                MessageType.Info
            );
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        showAckerman = EditorGUILayout.Foldout(showAckerman, "Ackerman Steering", true);
        if (showAckerman) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(
                ackermanWheelBase,
                new GUIContent("Wheel Base", "Distance between the front and rear axles.")
            );
            EditorGUILayout.PropertyField(
                ackermanSteerReference,
                new GUIContent("Steer Reference", "Used in the Ackerman angle calculation.")
            );
            EditorGUILayout.PropertyField(
                ackermanTrackWidth,
                new GUIContent("Track Width", "Distance between the left and right wheels on the same axle.")
            );
            EditorGUILayout.HelpBox(
                "Ackerman parameters help simulate realistic front wheel angles in tight turns. " +
                "It adjusts the inside/outside wheel steer angles for a more accurate turn radius.",
                MessageType.Info
            );
            EditorGUI.indentLevel--;
        }

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        // Apply changes back to the target object
        serializedObject.ApplyModifiedProperties();

    }

}

