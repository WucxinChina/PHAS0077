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

[CustomEditor(typeof(RCCP_BodyTilt))]
public class RCCP_BodyTiltEditor : Editor {

    RCCP_BodyTilt prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;

    // For each child transform, we store whether it's selected for tilting.
    private Dictionary<Transform, bool> _childToggles = new Dictionary<Transform, bool>();
    private bool _initialized = false;

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");
        _initialized = false;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_BodyTilt)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Tilts specified gameobjects based on the car's velocity and/or angular velocity but places all tilt targets as children under a single tiltRoot, so they rotate together. Colliders are placed outside of the tilt root..", MessageType.Info, true);

        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Draw all fields except 'tiltTargets' 
        // (since we�re going to manage tiltTargets ourselves with checkboxes)
        DrawPropertiesExcluding(serializedObject, "tiltTargets");

        EditorGUILayout.EndVertical();

        // Now, custom UI for selecting which transforms to tilt
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Tiltable Child Transforms", EditorStyles.boldLabel);

        // Collect and draw child toggles
        if (!_initialized || _childToggles.Count == 0) {

            InitializeChildToggles();

        }

        DrawChildToggles();

        EditorGUILayout.EndVertical();

        // Root transform from which to gather children
        Transform root = prop.GetComponentInParent<RCCP_CarController>(true).transform;

        List<Transform> components = new List<Transform>(root.GetComponentsInChildren<Transform>(true));
        List<Transform> filtered = new List<Transform>();

        foreach (Transform item in components) {

            if (item.GetComponent<RCCP_Lights>())
                filtered.Add(item);

            if (item.GetComponent<RCCP_Customizer>())
                filtered.Add(item);

            if (item.GetComponent<RCCP_Exhausts>())
                filtered.Add(item);

        }

        if (prop.tiltTargets != null && prop.tiltTargets.Length > 0) {

            List<Transform> current = new List<Transform>(prop.tiltTargets);

            foreach (Transform item in filtered) {

                if (!current.Contains(item))
                    current.Add(item);

            }

            prop.tiltTargets = current.ToArray();

        }

        // If any changes to the serialized object�s other properties,
        // or if we change child toggles, apply them.
        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    /// <summary>
    /// Gathers all possible transforms under CarController (or the tilt�s own transform if no CarController) 
    /// and sets up initial checkbox states based on what's already in tiltTargets.
    /// </summary>
    private void InitializeChildToggles() {

        _childToggles.Clear();

        // Root transform from which to gather children
        Transform root = prop.GetComponentInParent<RCCP_CarController>(true).transform;

        // Get every transform (including the root, if you want that as an option)
        List<Transform> allChildren = new List<Transform>(root.GetComponentsInChildren<Transform>(true));
        List<Transform> filtered = new List<Transform>();

        for (int i = 0; i < allChildren.Count; i++) {

            if (Equals(prop.GetComponentInParent<RCCP_CarController>(true).transform, allChildren[i]))
                filtered.Add(allChildren[i]);

            if (Equals(prop.transform, allChildren[i]))
                filtered.Add(allChildren[i]);

            if (allChildren[i].GetComponent<RCCP_Component>() || allChildren[i].GetComponent<RCCP_MainComponent>() || allChildren[i].GetComponent<RCCP_GenericComponent>()) {

                if (allChildren[i].GetComponent<RCCP_DetachablePart>() == null)
                    filtered.Add(allChildren[i]);

                if (allChildren[i].GetComponent<RCCP_Customizer>()) {

                    foreach (Transform child in allChildren[i].GetComponent<RCCP_Customizer>().GetComponentsInChildren<Transform>(true))
                        filtered.Add(child);

                }

                if (allChildren[i].GetComponent<RCCP_Exhausts>()) {

                    foreach (Transform child in allChildren[i].GetComponent<RCCP_Exhausts>().GetComponentsInChildren<Transform>(true))
                        filtered.Add(child);

                }

                if (allChildren[i].GetComponent<RCCP_Lights>()) {

                    foreach (Transform child in allChildren[i].GetComponent<RCCP_Lights>().GetComponentsInChildren<Transform>(true))
                        filtered.Add(child);

                }

            }

        }

        // Fill dictionary for each child transform
        foreach (Transform t in allChildren) {

            if (!filtered.Contains(t)) {

                bool alreadyInTargets = tiltHasTransform(prop, t);
                _childToggles[t] = alreadyInTargets;

            }

        }

        _initialized = true;

    }

    /// <summary>
    /// Renders checkboxes for each transform and updates tiltTargets accordingly.
    /// </summary>
    private void DrawChildToggles() {

        // Make a snapshot list of the keys, so we don't modify
        // the dictionary while enumerating it.
        var keysSnapshot = new List<Transform>(_childToggles.Keys);

        foreach (Transform childTransform in keysSnapshot) {

            if (!childTransform)
                continue;

            bool oldValue = _childToggles[childTransform];
            bool newValue = EditorGUILayout.ToggleLeft(childTransform.name, oldValue);

            if (newValue != oldValue) {

                Undo.RecordObject(prop, "Change Tilt Targets");

                // Update dictionary value
                _childToggles[childTransform] = newValue;

                // Now add/remove from tiltTargets
                if (newValue)
                    AddTransformToTiltTargets(prop, childTransform);
                else
                    RemoveTransformFromTiltTargets(prop, childTransform);

                EditorUtility.SetDirty(prop);

            }

        }

    }

    /// <summary>
    /// Utility to check if a transform is already in tiltTargets.
    /// </summary>
    private bool tiltHasTransform(RCCP_BodyTilt tilt, Transform t) {

        if (tilt.tiltTargets == null) return false;

        foreach (var item in tilt.tiltTargets)
            if (item == t) return true;

        return false;

    }

    /// <summary>
    /// Adds a transform to tiltTargets if not already present.
    /// </summary>
    private void AddTransformToTiltTargets(RCCP_BodyTilt tilt, Transform newT) {

        if (tiltHasTransform(tilt, newT))
            return; // already in list

        // Expand array
        var old = tilt.tiltTargets;
        int oldLen = (old == null) ? 0 : old.Length;
        var newArr = new Transform[oldLen + 1];

        for (int i = 0; i < oldLen; i++)
            newArr[i] = old[i];

        newArr[oldLen] = newT;
        tilt.tiltTargets = newArr;

    }

    /// <summary>
    /// Removes a transform from tiltTargets if present.
    /// </summary>
    private void RemoveTransformFromTiltTargets(RCCP_BodyTilt tilt, Transform remT) {

        if (!tiltHasTransform(tilt, remT))
            return; // not in list, nothing to remove

        var old = tilt.tiltTargets;
        List<Transform> newList = new List<Transform>(old);
        newList.Remove(remT);
        tilt.tiltTargets = newList.ToArray();

    }

}
