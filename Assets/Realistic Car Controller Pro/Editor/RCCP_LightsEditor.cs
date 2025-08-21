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
using UnityEngine.Rendering;

[CustomEditor(typeof(RCCP_Lights))]
public class RCCP_LightsEditor : Editor {

    RCCP_Lights prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    RCCP_Light.LightType lightType;

    private bool editMode = false;
    private bool symmetry = true;
    private float threshold = 0.05f;

    private List<RCCP_Light> active;    // all active lights
    private Dictionary<RCCP_Light, RCCP_Light> pairs;     // cached symmetry

    private int selectedType = 0;                       // 0 = All Types
    private RCCP_Light.LightType[] typeList;            // enum values present

    // at the top of your class:
    private RCCP_Light _selectedLight;
    private Vector2 _scrollPos;


    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Lights)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Main light manager of the vehicle. All lights are connected to this manager.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("lowBeamHeadlights"), new GUIContent("Low Beam Headlights", "Low beam headlights are on or off?"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("highBeamHeadlights"), new GUIContent("High Beam Headlights", "High beam headlights are on or off?"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("indicatorsAll"), new GUIContent("Indicators", "Indicators are set to on, or off?"), false);

        prop.GetAllLights();

        EditorGUILayout.Space();
        GUILayout.Label("Attached Lights", EditorStyles.boldLabel);

        if (prop.lights != null && prop.lights.Count > 0) {

            for (int i = 0; i < prop.lights.Count; i++) {

                if (prop.lights[i] != null) {

                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(prop.lights[i].name, GUILayout.Width(250f));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Edit", GUILayout.Width(100f)))
                        Selection.activeGameObject = prop.lights[i].gameObject;

                    GUI.color = Color.red;

                    if (GUILayout.Button("X")) {

                        DestroyImmediate(prop.lights[i].gameObject);
                        EditorUtility.SetDirty(prop);

                    }

                    GUI.color = guiColor;
                    EditorGUILayout.EndHorizontal();

                }

            }

        } else {

            EditorGUILayout.HelpBox("No lights found. You can create new lights below.", MessageType.Warning);

        }

        EditorGUILayout.Space();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Create New Light", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            lightType = (RCCP_Light.LightType)EditorGUILayout.EnumPopup(lightType);

            GUI.color = Color.green;

            if (GUILayout.Button("Create New Light")) {

                Selection.activeGameObject = CreateLight();
                SceneView.FrameLastActiveSceneView();
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

        }

        Extension();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

#if BCG_URP

            if (GUILayout.Button("Convert To URP"))
                ConvertToURP();

#endif

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

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    private void OnSceneGUI() {

        if (!editMode || active == null) {
            return;
        }

        // 1) Draw a little floating window in the SceneView to pick lights by name/type
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 220, 200), "Lights", GUI.skin.window);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);
        for (int i = 0; i < active.Count; i++) {

            var light = active[i];
            if (light == null) {
                continue;
            }

            string label = $"{light.lightType} #{i}";
            if (GUILayout.Button(label, GUILayout.ExpandWidth(true))) {
                // toggle selection
                _selectedLight = _selectedLight == light ? null : light;
            }
        }
        if (GUILayout.Button("Clear Selection")) {
            _selectedLight = null;
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        Handles.EndGUI();

        // draw clickable spheres
        foreach (var light in active) {

            if (light == null)
                continue;

            // only show selectedType filter?
            if (selectedType != 0 &&
                 light.lightType != typeList[selectedType - 1])
                continue;

            var pos = light.transform.position;
            float size = HandleUtility.GetHandleSize(pos) * .2f;

            // set the button color per light type
            Handles.color = GetButtonColor(light.lightType);

            if (Handles.Button(pos,
                                 Quaternion.identity,
                                 size,
                                 size,
                                 Handles.SphereHandleCap)) {

                _selectedLight = light;
                Event.current.Use();
            }
        }

        // 3) Decide which lights get real PositionHandles
        var lightsToEdit = _selectedLight != null
            ? new List<RCCP_Light> { _selectedLight }
            : active;

        RCCP_Lights mgr = (RCCP_Lights)target;
        Transform root = mgr.transform;
        bool showAll = selectedType == 0;

        foreach (var light in lightsToEdit) {

            if (light == null) {
                continue;
            }

            if (!showAll && light.lightType != typeList[selectedType - 1]) {
                continue;
            }

            Transform t = light.transform;
            Handles.color = Color.white;

            // ensure handles never scale all the way to zero
            // you can tweak minScale/maxScale to taste
            float dist = HandleUtility.GetHandleSize(t.position);
            
            float raw =
                Mathf.InverseLerp(5f, .5f, dist);               // map [5…20] world units into [0…1]
            float screenSizeFactor =
                Mathf.Lerp(.1f, .6f, raw);                      // now map [0…1] into [minScale…maxScale]


            Matrix4x4 oldMat = Handles.matrix;
            Handles.matrix =
                Matrix4x4.TRS(t.position, Quaternion.identity, Vector3.one * screenSizeFactor) *
                Matrix4x4.TRS(-t.position, Quaternion.identity, Vector3.one) *
                oldMat;

            EditorGUI.BeginChangeCheck();
            Vector3 pos = Handles.PositionHandle(t.position, t.rotation);

            Handles.matrix =
      Matrix4x4.TRS(t.position, Quaternion.identity, Vector3.one * screenSizeFactor * .5f) *
      Matrix4x4.TRS(-t.position, Quaternion.identity, Vector3.one) *
      oldMat;

            Quaternion rot = Handles.RotationHandle(t.rotation, t.position);
            if (EditorGUI.EndChangeCheck()) {

                Undo.SetCurrentGroupName("Move RCCP Light");
                int group = Undo.GetCurrentGroup();

                // primary
                Undo.RecordObject(t, "Move RCCP Light");
                t.position = pos;
                t.rotation = rot;
                EditorUtility.SetDirty(t);

                // mirror
                if (symmetry && pairs.TryGetValue(light, out RCCP_Light mate) && mate) {

                    Transform mt = mate.transform;
                    Undo.RecordObject(mt, "Mirror RCCP Light");

                    Vector3 local = root.InverseTransformPoint(pos);
                    local.x = -local.x;
                    mt.position = root.TransformPoint(local);

                    Quaternion localRot = Quaternion.Inverse(root.rotation) * rot;
                    Vector3 eul = localRot.eulerAngles;
                    eul.y = -eul.y;
                    eul.z = -eul.z;
                    mt.rotation = root.rotation * Quaternion.Euler(eul);

                    EditorUtility.SetDirty(mt);
                }

                Undo.CollapseUndoOperations(group);
            }

            Handles.matrix = oldMat;
            Handles.Label(t.position, light.lightType.ToString(), EditorStyles.miniBoldLabel);
        }
    }

    // helper to pick a distinct color per light type
    private Color GetButtonColor(RCCP_Light.LightType type) {

        switch (type) {

            case RCCP_Light.LightType.Brakelight:
                return new Color(1f, 0.3f, 0.3f);       // soft red

            case RCCP_Light.LightType.Taillight:
                return new Color(0.6f, 0.0f, 0.0f);     // darker red

            case RCCP_Light.LightType.IndicatorLeftLight:
            case RCCP_Light.LightType.IndicatorRightLight:
                return new Color(1f, 0.6f, 0f);         // orange

            case RCCP_Light.LightType.Reverselight:
                return new Color(0.8f, 0.8f, 1f);       // pale blue

            case RCCP_Light.LightType.Headlight_LowBeam:
            case RCCP_Light.LightType.Headlight_HighBeam:
                return new Color(1f, 1f, 0.8f);         // warm white

            default:
                return Color.white;
        }
    }

    private void Extension() {

        GUILayout.Space(10f);

        if (GUILayout.Button("Quick Lights Setup Wizard"))
            RCCP_LightSetupWizard.ShowWindow();

        GUILayout.Space(10f);

        if (GUILayout.Toggle(editMode, "Edit Light Positions", "Button") != editMode) {

            editMode = !editMode;
            if (editMode)
                RebuildEverything();

            SceneView.RepaintAll();

        }

        if (!editMode)
            return;

        symmetry = GUILayout.Toggle(symmetry, "Symmetry (mirror X-axis)");
        threshold = EditorGUILayout.Slider("Pair Threshold (m)", threshold, .001f, .5f);

        BuildTypeArrayIfNeeded();
        string[] typeNames = BuildTypeNames();
        int newSel = EditorGUILayout.Popup("Handle Filter", selectedType, typeNames);

        if (newSel != selectedType) {
            selectedType = newSel;
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Re-pair now"))
            RebuildEverything();

    }

    private GameObject CreateLight() {

        string name = "RCCP_Light";
        Vector3 localPos = Vector3.zero;
        Quaternion localRot = Quaternion.identity;

        RCCP_LightSetupData setupData = RCCP_Settings.Instance.lightsSetupData;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Light");
        go.transform.SetParent(prop.transform);
        Light l = go.AddComponent<Light>();
        l.type = LightType.Spot;

        if (lightType == RCCP_Light.LightType.Headlight_LowBeam || lightType == RCCP_Light.LightType.Headlight_HighBeam) {

            name = "RCCP_Light_Headlight";
            localPos = new Vector3(0f, .5f, 2.5f);
            localRot = Quaternion.identity;
            l.intensity = setupData.defaultIntensityForHeadlights;
            l.color = setupData.headlightColor;

        }

        if (lightType == RCCP_Light.LightType.Brakelight) {

            name = "RCCP_Light_Brakelight";
            localPos = new Vector3(0f, .5f, -2.5f);
            localRot = Quaternion.identity * Quaternion.Euler(0f, 180f, 0f);
            l.intensity = setupData.defaultIntensityForBrakeLights;
            l.color = setupData.brakelightColor;

        }

        if (lightType == RCCP_Light.LightType.Reverselight) {

            name = "RCCP_Light_Reverselight";
            localPos = new Vector3(0f, .5f, -2.5f);
            localRot = Quaternion.identity * Quaternion.Euler(0f, 180f, 0f);
            l.intensity = setupData.defaultIntensityForReverseLights;
            l.color = setupData.reverselightColor;

        }

        if (lightType == RCCP_Light.LightType.IndicatorLeftLight || lightType == RCCP_Light.LightType.IndicatorRightLight) {

            name = "RCCP_Light_Indicatorlight";
            localPos = new Vector3(lightType == RCCP_Light.LightType.IndicatorLeftLight ? -1f : 1f, .5f, 2.5f);
            localRot = Quaternion.identity;
            l.intensity = setupData.defaultIntensityForIndicatorLights;
            l.color = setupData.indicatorColor;

        }

        go.name = name;
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        l.range = 30f;
        l.spotAngle = 90f;
        l.renderMode = LightRenderMode.ForcePixel;
        go.AddComponent<RCCP_Light>().lightType = lightType;

        if (setupData.useLensFlares) {

#if !BCG_URP && !BCG_HDRP

            LensFlare flareComp = go.AddComponent<LensFlare>();
            flareComp.flare = setupData.flare;
            flareComp.brightness = 0f;

#else

            LensFlareComponentSRP flareComp = go.AddComponent<LensFlareComponentSRP>();
            flareComp.lensFlareData = setupData.lensFlareSRP as LensFlareDataSRP;
            flareComp.attenuationByLightShape = false;
            flareComp.intensity = 0f;

#endif

        }

        return go;

    }

#if BCG_URP
    private void ConvertToURP() {

        RCCP_Light[] lights = prop.GetComponentsInChildren<RCCP_Light>(true);

        for (int i = 0; i < lights.Length; i++) {

            if (lights[i].TryGetComponent(out LensFlare oldLensFlare))
                DestroyImmediate(oldLensFlare);

            if (!lights[i].TryGetComponent(out UnityEngine.Rendering.LensFlareComponentSRP newLensFlare)) {

                UnityEngine.Rendering.LensFlareComponentSRP srp = lights[i].gameObject.AddComponent<UnityEngine.Rendering.LensFlareComponentSRP>();
                srp.lensFlareData = RCCP_Settings.Instance.lensFlareData as LensFlareDataSRP;

            }

        }

        if (Camera.main != null && Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() != null && Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing == false) {

            Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;

        }

    }
#endif

    // --------------------------- Build helpers
    private void RebuildEverything() {

        BuildActiveLights();
        BuildPairs();
        BuildTypeArray();

    }

    private void BuildActiveLights() {

        RCCP_Lights mgr = (RCCP_Lights)target;
        active = new List<RCCP_Light>();

        foreach (RCCP_Light l in mgr.lights)
            if (l && l.gameObject.activeInHierarchy)
                active.Add(l);

    }

    /// <summary>
    /// Build symmetry pairs between lights of the same type based on their local X positions relative to the root.
    /// Lights are paired by matching the Nth closest left-side light to the Nth closest right-side light.
    /// </summary>
    private void BuildPairs() {

        pairs = new Dictionary<RCCP_Light, RCCP_Light>();
        RCCP_Lights mgr = (RCCP_Lights)target;
        Transform root = mgr.transform;

        foreach (RCCP_Light a in active) {

            if (pairs.ContainsKey(a))
                continue;

            // local space position
            Vector3 aLoc = a.transform.localPosition;
            // mirror across X
            Vector3 mirror = new Vector3(-aLoc.x, aLoc.y, aLoc.z);

            RCCP_Light best = null;
            float bestDist = float.MaxValue;

            foreach (RCCP_Light b in active) {

                if (a == b || pairs.ContainsKey(b))
                    continue;

                // **allow** same-type OR left/right indicator pairing
                bool typesMatch = b.lightType == a.lightType
                               || (a.lightType == RCCP_Light.LightType.IndicatorLeftLight
                                  && b.lightType == RCCP_Light.LightType.IndicatorRightLight)
                               || (a.lightType == RCCP_Light.LightType.IndicatorRightLight
                                  && b.lightType == RCCP_Light.LightType.IndicatorLeftLight);
                if (!typesMatch)
                    continue;

                // how close is b to the mirrored position?
                float dist = Vector3.Distance(b.transform.localPosition, mirror);
                if (dist < bestDist) {
                    bestDist = dist;
                    best = b;
                }
            }

            if (best != null) {
                pairs[a] = best;
                pairs[best] = a;
            }
        }
    }

    private void BuildTypeArray() {

        HashSet<RCCP_Light.LightType> set = new HashSet<RCCP_Light.LightType>();
        foreach (RCCP_Light l in active)
            set.Add(l.lightType);

        typeList = new RCCP_Light.LightType[set.Count];
        set.CopyTo(typeList);
        selectedType = 0;

    }

    private void BuildTypeArrayIfNeeded() {

        if (typeList == null || typeList.Length == 0)
            BuildTypeArray();

    }

    private string[] BuildTypeNames() {

        string[] names = new string[typeList.Length + 1];
        names[0] = "All Types";
        for (int i = 0; i < typeList.Length; i++)
            names[i + 1] = typeList[i].ToString();
        return names;

    }

    // --------------------------- Colours
    private static Color GetColor(RCCP_Light.LightType t) {

        switch (t) {

            case RCCP_Light.LightType.Headlight_LowBeam:
            case RCCP_Light.LightType.Headlight_HighBeam: return Color.white;
            case RCCP_Light.LightType.Brakelight: return Color.red;
            case RCCP_Light.LightType.Taillight: return Color.magenta;
            case RCCP_Light.LightType.Reverselight: return Color.cyan;
            case RCCP_Light.LightType.IndicatorLeftLight:
            case RCCP_Light.LightType.IndicatorRightLight: return Color.yellow;
            default: return Color.gray;

        }

    }

}
