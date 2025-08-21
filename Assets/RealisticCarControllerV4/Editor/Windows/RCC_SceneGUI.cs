//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[InitializeOnLoad]
public static class RCC_SceneGUI {

    #region Configuration

    private const float PanelWidth = 75f;
    private const float ScenePanelHeight = 200f;
    private const float VehiclePanelHeight = 1000f;

    #endregion

    #region State & Resources

    private static bool isEnabled;
    private static GUISkin skin;

    private static Texture2D cameraIcon;
    private static Texture2D canvasIcon;
    private static Texture2D hoodCameraIcon;
    private static Texture2D wheelCameraIcon;
    private static Texture2D headlightIcon;
    private static Texture2D brakelightIcon;
    private static Texture2D reverselightIcon;
    private static Texture2D indicatorlightIcon;
    private static Texture2D interiorlightIcon;
    private static Texture2D exhaustIcon;
    private static Texture2D mirrorIcon;

    #endregion

    #region Initialisation

    static RCC_SceneGUI() {

        // Unity reloads assemblies frequently; keep callback list clean.
        AssemblyReloadEvents.beforeAssemblyReload += Disable;
        LoadSkinAndIcons();

    }

    private static void LoadSkinAndIcons() {

        skin = Resources.Load<GUISkin>("RCC_WindowSkin");

        // All icons are under Resources/Editor/…
        cameraIcon = Resources.Load<Texture2D>("Editor/CameraIcon");
        canvasIcon = Resources.Load<Texture2D>("Editor/CanvasIcon");
        hoodCameraIcon = Resources.Load<Texture2D>("Editor/HoodCameraIcon");
        wheelCameraIcon = Resources.Load<Texture2D>("Editor/WheelCameraIcon");
        headlightIcon = Resources.Load<Texture2D>("Editor/HeadlightIcon");
        brakelightIcon = Resources.Load<Texture2D>("Editor/BrakelightIcon");
        reverselightIcon = Resources.Load<Texture2D>("Editor/ReverselightIcon");
        indicatorlightIcon = Resources.Load<Texture2D>("Editor/IndicatorlightIcon");
        interiorlightIcon = Resources.Load<Texture2D>("Editor/InteriorlightIcon");
        exhaustIcon = Resources.Load<Texture2D>("Editor/ExhaustIcon");
        mirrorIcon = Resources.Load<Texture2D>("Editor/MirrorIcon");

    }

    #endregion

    #region Menu Items

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Enable In-Scene Buttons", false, 5000)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Enable In-Scene Buttons", false, 5000)]
    public static void Enable() {

        if (isEnabled)
            return;

        LoadSkinAndIcons();

        SceneView.duringSceneGui += OnSceneGUI;
        isEnabled = true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Disable In-Scene Buttons", false, 5000)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Disable In-Scene Buttons", false, 5000)]
    public static void Disable() {

        SceneView.duringSceneGui -= OnSceneGUI;
        isEnabled = false;

    }

    #endregion

    #region GUI Logic

    private static void OnSceneGUI(SceneView sceneView) {

        GUI.skin = skin ? skin : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

        Handles.BeginGUI();

        DrawSceneButtons();
        DrawVehicleButtons();

        Handles.EndGUI();

    }

    private static void DrawSceneButtons() {

        GUILayout.BeginArea(new Rect(10f, 10f, PanelWidth, ScenePanelHeight));
        GUILayout.BeginVertical("window");

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Scene");
        GUILayout.EndHorizontal();
        GUILayout.Space(4);

        if (GUILayout.Button(new GUIContent(cameraIcon, "Add / Select RCC Camera")))
            RCC_EditorWindows.CreateRCCCamera();

        if (GUILayout.Button(new GUIContent(canvasIcon, "Add / Select RCC Canvas")))
            RCC_EditorWindows.CreateRCCCanvas();

        Color prev = GUI.color;
        GUI.color = Color.red;

        if (GUILayout.Button(new GUIContent(" X ", "Close the in-scene window. You can enable it from Tools → BCG → RCC")))
            Disable();

        GUI.color = prev;

        GUILayout.EndVertical();
        GUILayout.EndArea();

    }

    private static void DrawVehicleButtons() {

        GUILayout.BeginArea(new Rect(10f, 200f, PanelWidth, VehiclePanelHeight));
        GUILayout.BeginVertical("window");

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Vehicle");
        GUILayout.EndHorizontal();

        RCC_CarControllerV4 selectedCar = Selection.activeGameObject ? Selection.activeGameObject.GetComponentInParent<RCC_CarControllerV4>(true) : null;

        if (selectedCar) {

            if (GUILayout.Button(new GUIContent(hoodCameraIcon, "Add / Select Hood Camera")))
                RCC_EditorWindows.CreateHoodCamera();

            if (GUILayout.Button(new GUIContent(wheelCameraIcon, "Add / Select Wheel Camera")))
                RCC_EditorWindows.CreateWheelCamera();

            if (GUILayout.Button(new GUIContent(headlightIcon, "Add head lights")))
                RCC_EditorWindows.CreateHeadLight();

            if (GUILayout.Button(new GUIContent(brakelightIcon, "Add brake lights")))
                RCC_EditorWindows.CreateBrakeLight();

            if (GUILayout.Button(new GUIContent(reverselightIcon, "Add reverse lights")))
                RCC_EditorWindows.CreateReverseLight();

            if (GUILayout.Button(new GUIContent(indicatorlightIcon, "Add indicator lights")))
                RCC_EditorWindows.CreateIndicatorLight();

            if (GUILayout.Button(new GUIContent(interiorlightIcon, "Add interior lights")))
                RCC_EditorWindows.CreateInteriorLight();

            if (GUILayout.Button(new GUIContent(exhaustIcon, "Add exhaust")))
                RCC_EditorWindows.CreateExhaust();

            if (GUILayout.Button(new GUIContent(mirrorIcon, "Add / Select mirrors")))
                RCC_EditorWindows.CreateMirrors(selectedCar.gameObject);

            // Duplicate selected RCC_Light, if any
            if (Selection.activeGameObject && Selection.activeGameObject.GetComponent<RCC_Light>()) {

                if (GUILayout.Button(new GUIContent(indicatorlightIcon, "Duplicate light to opposite side")))
                    RCC_EditorWindows.DuplicateLight();

            }

        } else {

            GUILayout.Label("Select an RCC vehicle first");

        }

        GUILayout.EndVertical();
        GUILayout.EndArea();

    }

    #endregion

}
