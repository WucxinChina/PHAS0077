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

public class RCCP_EditorWindows : Editor {

    // Renamed from "Edit RCCP Settings" to "RCCP Settings"
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/RCCP Settings", false, -100)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/RCCP Settings", false, -100)]
    public static void OpenRCCSettings() {
        Selection.activeObject = RCCP_Settings.Instance;
    }

    #region Setup

    // Renamed folder from "Configure" to "Setup"
    // Ground Materials --> Ground Physics
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configuration/Ground Physics", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configuration/Ground Physics", false, -65)]
    public static void OpenGroundMaterialsSettings() {
        Selection.activeObject = RCCP_GroundMaterials.Instance;
    }

    // Changable Wheels --> Wheel Configurations
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configuration/Wheels", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configuration/Wheels", false, -65)]
    public static void OpenChangableWheelSettings() {
        Selection.activeObject = RCCP_ChangableWheels.Instance;
    }

    // Recorded Clips --> Recordings
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configuration/Recordings", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configuration/Recordings", false, -65)]
    public static void OpenRecordSettings() {
        Selection.activeObject = RCCP_Records.Instance;
    }

    // Demo Vehicles --> Demo Vehicles & Prefabs
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configuration/Demo/Demo Vehicles and Prefabs", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configuration/Demo/Demo Vehicles and Prefabs", false, -65)]
    public static void OpenDemoVehiclesSettings() {
        Selection.activeObject = RCCP_DemoVehicles.Instance;
    }

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING
    // Demo Vehicles (Photon) --> Photon Demo Vehicles
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configuration/Photon Demo Vehicles", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configuration/Photon Demo Vehicles", false, -65)]
    public static void OpenPhotonDemoVehiclesSettings() {
        Selection.activeObject = RCCP_DemoVehicles_Photon.Instance;
    }
#endif

    // Demo Scenes --> Demo Scenes & Levels
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configuration/Demo/Demo Scenes and Levels", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configuration/Demo/Demo Scenes and Levels", false, -65)]
    public static void OpenDemoScenesSettings() {
        Selection.activeObject = RCCP_DemoScenes.Instance;
    }

    #endregion

    #region Scene Managers

    // Renamed folder from "Create/Managers" to "Create/Scene Managers"
    // Add RCCP Scene Manager To Scene --> Add Scene Manager
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene Managers/Add Scene Manager", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene Managers/Add Scene Manager", false, -50)]
    public static void CreateRCCPSceneManager() {
        Selection.activeObject = RCCP_SceneManager.Instance;
    }

    // Add RCCP Skidmarks Manager To Scene --> Add Skidmarks Manager
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene Managers/Add Skidmarks Manager", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene Managers/Add Skidmarks Manager", false, -50)]
    public static void CreateRCCPSkidmarksManager() {
        Selection.activeObject = RCCP_SkidmarksManager.Instance;
    }

    #endregion

    // Kept "Create/Scene" as is, but simplified item names:
    // Add RCCP Camera To Scene --> Add RCCP Camera
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add RCCP Camera", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add RCCP Camera", false, -50)]
    public static void CreateRCCCamera() {

#if UNITY_2022_1_OR_NEWER

        if (FindFirstObjectByType<RCCP_Camera>(FindObjectsInactive.Include)) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Scene has RCCP Camera already!", "Scene has RCCP Camera already!", "Close");
            Selection.activeGameObject = FindFirstObjectByType<RCCP_Camera>(FindObjectsInactive.Include).gameObject;

        } else {

            GameObject cam = Instantiate(RCCP_Settings.Instance.RCCPMainCamera.gameObject);
            cam.name = RCCP_Settings.Instance.RCCPMainCamera.name;
            Selection.activeGameObject = cam.gameObject;

        }

#else

        if (FindObjectOfType<RCCP_Camera>(true)) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Scene has RCCP Camera already!", "Scene has RCCP Camera already!", "Close");
            Selection.activeGameObject = FindObjectOfType<RCCP_Camera>(true).gameObject;

        } else {

            GameObject cam = Instantiate(RCCP_Settings.Instance.RCCPMainCamera.gameObject);
            cam.name = RCCP_Settings.Instance.RCCPMainCamera.name;
            Selection.activeGameObject = cam.gameObject;

        }

#endif

    }

    // Add RCCP UI Canvas To Scene --> Add UI Canvas
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add UI Canvas", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add UI Canvas", false, -50)]
    public static void CreateRCCUICanvas() {

#if UNITY_2022_1_OR_NEWER

        if (FindFirstObjectByType<RCCP_UIManager>(FindObjectsInactive.Include)) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Scene has RCCP UI Canvas already!", "Scene has RCCP UI Canvas already!", "Close");
            Selection.activeGameObject = FindFirstObjectByType<RCCP_UIManager>(FindObjectsInactive.Include).gameObject;

        } else {

            GameObject cam = Instantiate(RCCP_Settings.Instance.RCCPCanvas.gameObject);
            cam.name = RCCP_Settings.Instance.RCCPCanvas.name;
            Selection.activeGameObject = cam.gameObject;

        }

#else

        if (FindObjectOfType<RCCP_UIManager>(true)) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Scene has RCCP UI Canvas already!", "Scene has RCCP UI Canvas already!", "Close");
            Selection.activeGameObject = FindObjectOfType<RCCP_UIManager>(true).gameObject;

        } else {

            GameObject cam = Instantiate(RCCP_Settings.Instance.RCCPCanvas.gameObject);
            cam.name = RCCP_Settings.Instance.RCCPCanvas.name;
            Selection.activeGameObject = cam.gameObject;

        }

#endif

    }

    // Add AI Waypoints Container To Scene --> Add AI Waypoints
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Waypoints", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Waypoints", false, -50)]
    public static void CreateRCCAIWaypointManager() {

        GameObject wpContainer = new GameObject("RCCP_AI_WaypointsContainer");
        wpContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        wpContainer.AddComponent<RCCP_AIWaypointsContainer>();
        Selection.activeGameObject = wpContainer;

    }

    // Add AI Brake Zones Container To Scene --> Add AI Brake Zones
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Brake Zones", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Brake Zones", false, -50)]
    public static void CreateRCCAIBrakeManager() {

        GameObject bzContainer = new GameObject("RCCP_AI_BrakeZonesContainer");
        bzContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        bzContainer.AddComponent<RCCP_AIBrakeZonesContainer>();
        Selection.activeGameObject = bzContainer;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Render Pipeline Converter", false, -40)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Render Pipeline Converter", false, -40)]
    public static void PipelineConverter() {

        RCCP_RenderPipelineConverterWindow.Init();

    }

#region Help
    // Renamed "Help" to "Documentation & Support"
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Documentation & Support", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Documentation & Support", false, 0)]
    public static void Help() {

        EditorUtility.DisplayDialog("Realistic Car Controller Pro | Contact", "Please include your invoice number while sending a contact form. I usually respond within a business day.", "Close");

        string url = "https://www.bonecrackergames.com/contact/";
        Application.OpenURL(url);

    }
#endregion Help

}
