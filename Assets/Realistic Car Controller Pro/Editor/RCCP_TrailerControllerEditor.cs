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
using UnityEngine.Rendering;

[CustomEditor(typeof(RCCP_TrailerController))]
public class RCCP_TrailerControllerEditor : Editor {

    private RCCP_TrailerController prop;
    RCCP_Light.LightType lightType;

    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_TrailerController)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox(
    "This component manages the physics and attachment logic for a trailer in the Realistic Car Controller Pro setup. " +
    "Make sure you have assigned all required references (COM, trailer wheels, attacher, etc.). " +
        "ConfigurableJoint component will be used to attach / detach the trailer, so it should have correct setup.",
    MessageType.Info);

        // Draw the default inspector first (shows all public fields, etc.)
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (!EditorApplication.isPlaying) {

            prop.GetAllLights();

            Vector3 anchorPoint = prop.transform.InverseTransformPoint(prop.TrailerConnectionPoint.position);
            prop.Joint.anchor = anchorPoint;

        }

        EditorGUILayout.Space();
        GUILayout.Label("Attached Lights", EditorStyles.boldLabel);

        if (prop.allLights != null && prop.allLights.Count > 0) {

            for (int i = 0; i < prop.allLights.Count; i++) {

                if (prop.allLights[i] != null) {

                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(prop.allLights[i].name, GUILayout.Width(250f));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Edit", GUILayout.Width(100f)))
                        Selection.activeGameObject = prop.allLights[i].gameObject;

                    GUI.color = Color.red;

                    if (GUILayout.Button("X")) {

                        DestroyImmediate(prop.allLights[i].gameObject);
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

                Selection.activeGameObject = CreateNewLight(lightType).gameObject;
                SceneView.FrameLastActiveSceneView();
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

        if (!EditorUtility.IsPersistent(prop)) {

#if BCG_URP

            if (GUILayout.Button("Convert To URP"))
                ConvertToURP();

#endif

        }

        if (!EditorApplication.isPlaying && !EditorUtility.IsPersistent(prop)) {

            if (RCCP_Settings.Instance.setLayers)
                SetLayers();

        }

        serializedObject.ApplyModifiedProperties();

        // Apply any changes made to the serializedObject
        if (GUI.changed)
            EditorUtility.SetDirty(prop);
    }

    private RCCP_Light CreateNewLight(RCCP_Light.LightType lightType) {

        //        switch (lightType) {

        //            case RCCP_Light.LightType.Headlight_LowBeam:

        //                GameObject newLightSource_Headlight = Instantiate(RCCP_Settings.Instance.headLights_Low, prop.transform, false);
        //                newLightSource_Headlight.transform.name = RCCP_Settings.Instance.headLights_Low.transform.name + "_D";
        //                newLightSource_Headlight.transform.localPosition = new Vector3(0f, 0f, 2.5f);
        //#if BCG_URP || BCG_HDRP
        //                ConvertToURP();
        //#endif
        //                prop.GetAllLights();
        //                return newLightSource_Headlight.GetComponent<RCCP_Light>();

        //            case RCCP_Light.LightType.Headlight_HighBeam:

        //                GameObject newLightSource_Headlight_High = Instantiate(RCCP_Settings.Instance.headLights_High, prop.transform, false);
        //                newLightSource_Headlight_High.transform.name = RCCP_Settings.Instance.headLights_High.transform.name + "_D";
        //                newLightSource_Headlight_High.transform.localPosition = new Vector3(0f, 0f, 2.5f);
        //#if BCG_URP || BCG_HDRP
        //                ConvertToURP();
        //#endif
        //                prop.GetAllLights();
        //                return newLightSource_Headlight_High.GetComponent<RCCP_Light>();

        //            case RCCP_Light.LightType.Brakelight:

        //                GameObject newLightSource_Brakelight = Instantiate(RCCP_Settings.Instance.brakeLights, prop.transform, false);
        //                newLightSource_Brakelight.transform.name = RCCP_Settings.Instance.brakeLights.transform.name + "_D";
        //                newLightSource_Brakelight.transform.localPosition = new Vector3(0f, 0f, -2.5f);
        //#if BCG_URP || BCG_HDRP
        //                ConvertToURP();
        //#endif
        //                prop.GetAllLights();
        //                return newLightSource_Brakelight.GetComponent<RCCP_Light>();

        //            case RCCP_Light.LightType.Reverselight:

        //                GameObject newLightSource_Reverselight = Instantiate(RCCP_Settings.Instance.reverseLights, prop.transform, false);
        //                newLightSource_Reverselight.transform.name = RCCP_Settings.Instance.reverseLights.transform.name + "_D";
        //                newLightSource_Reverselight.transform.localPosition = new Vector3(0f, 0f, -2.5f);
        //#if BCG_URP || BCG_HDRP
        //                ConvertToURP();
        //#endif
        //                prop.GetAllLights();
        //                return newLightSource_Reverselight.GetComponent<RCCP_Light>();

        //            case RCCP_Light.LightType.IndicatorLeftLight:

        //                GameObject newLightSource_IndicatorL = Instantiate(RCCP_Settings.Instance.indicatorLights_L, prop.transform, false);
        //                newLightSource_IndicatorL.transform.name = RCCP_Settings.Instance.indicatorLights_L.transform.name + "_D";
        //                newLightSource_IndicatorL.transform.localPosition = new Vector3(-.5f, 0f, -2.5f);
        //#if BCG_URP || BCG_HDRP
        //                ConvertToURP();
        //#endif
        //                prop.GetAllLights();
        //                return newLightSource_IndicatorL.GetComponent<RCCP_Light>();

        //            case RCCP_Light.LightType.IndicatorRightLight:

        //                GameObject newLightSource_IndicatorR = Instantiate(RCCP_Settings.Instance.indicatorLights_R, prop.transform, false);
        //                newLightSource_IndicatorR.transform.name = RCCP_Settings.Instance.indicatorLights_R.transform.name + "_D";
        //                newLightSource_IndicatorR.transform.localPosition = new Vector3(.5f, 0f, -2.5f);
        //#if BCG_URP || BCG_HDRP
        //                ConvertToURP();
        //#endif
        //                prop.GetAllLights();
        //                return newLightSource_IndicatorR.GetComponent<RCCP_Light>();

        //            case RCCP_Light.LightType.Taillight:

        //                GameObject newLightSource_Taillight = Instantiate(RCCP_Settings.Instance.tailLights, prop.transform, false);
        //                newLightSource_Taillight.transform.name = RCCP_Settings.Instance.tailLights.transform.name + "_D";
        //                newLightSource_Taillight.transform.localPosition = new Vector3(0f, 0f, -2.5f);
        //#if BCG_URP || BCG_HDRP
        //                ConvertToURP();
        //#endif
        //                prop.GetAllLights();
        //                return newLightSource_Taillight.GetComponent<RCCP_Light>();

        //        }

        return null;

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

    private void SetLayers() {

        prop.transform.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

        var children = prop.transform.GetComponentsInChildren<Transform>(true);

        if (RCCP_Settings.Instance.RCCPLayer != "") {

            foreach (var child in children)
                child.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

        }

        if (RCCP_Settings.Instance.RCCPWheelColliderLayer != "") {

            foreach (RCCP_WheelCollider item in prop.gameObject.GetComponentsInChildren<RCCP_WheelCollider>(true))
                item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

        }

        if (RCCP_Settings.Instance.RCCPDetachablePartLayer != "") {

            foreach (RCCP_DetachablePart item in prop.gameObject.GetComponentsInChildren<RCCP_DetachablePart>(true))
                item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

        }

    }

}
