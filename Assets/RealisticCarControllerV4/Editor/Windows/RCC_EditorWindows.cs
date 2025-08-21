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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class RCC_EditorWindows : Editor {

    public static RCC_CarControllerV4 SelectedCar() {

        if (Selection.activeGameObject == null)
            return null;

        return Selection.activeGameObject.GetComponentInParent<RCC_CarControllerV4>(true);

    }

    #region Main Settings (Priority: 0-100)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Edit RCC Settings", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Edit RCC Settings", false, 0)]
    public static void OpenRCCSettings() {
        Selection.activeObject = RCC_Settings.Instance;
    }
    #endregion

    #region Configuration (Priority: 100-200)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Configure/Configure Demo Vehicles", false, 100)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Configure/Configure Demo Vehicles", false, 100)]
    public static void OpenDemoVehiclesSettings() {
        Selection.activeObject = RCC_DemoVehicles.Instance;
    }

#if RCC_PHOTON && PHOTON_UNITY_NETWORKING
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Configure/Configure Photon Demo Vehicles", false, 101)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Configure/Configure Photon Demo Vehicles", false, 101)]
    public static void OpenPhotonDemoVehiclesSettings() {
        Selection.activeObject = RCC_PhotonDemoVehicles.Instance;
    }
#endif

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Configure/Configure Ground Materials", false, 110)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Configure/Configure Ground Materials", false, 110)]
    public static void OpenGroundMaterialsSettings() {
        Selection.activeObject = RCC_GroundMaterials.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Configure/Configure Changable Wheels", false, 111)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Configure/Configure Changable Wheels", false, 111)]
    public static void OpenChangableWheelSettings() {
        Selection.activeObject = RCC_ChangableWheels.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Configure/Configure Recorded Clips", false, 112)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Configure/Configure Recorded Clips", false, 112)]
    public static void OpenRecordSettings() {
        Selection.activeObject = RCC_Records.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Configure/Configure Initial Vehicle Setup Settings", false, 113)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Configure/Configure Initial Vehicle Setup Settings", false, 113)]
    public static void OpenInitialSettings() {
        Selection.activeObject = RCC_InitialSettings.Instance;
    }
    #endregion

    #region Create Tools (Priority: 200-600)

    #region Managers (Priority: 200-250)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Managers/Add RCC Scene Manager", false, 200)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Managers/Add RCC Scene Manager", false, 200)]
    public static void AddRCCSceneManager() {
        Selection.activeObject = RCC_SceneManager.Instance.gameObject;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Managers/Add RCC Skidmarks Manager", false, 201)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Managers/Add RCC Skidmarks Manager", false, 201)]
    public static void AddRCCSkidmarksManager() {
        Selection.activeObject = RCC_SkidmarksManager.Instance.gameObject;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Managers/Add RCC Customization Manager", false, 202)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Managers/Add RCC Customization Manager", false, 202)]
    public static void AddCustomizationManager() {
        Selection.activeObject = RCC_CustomizationManager.Instance.gameObject;
    }
    #endregion

    #region Cameras (Priority: 300-350)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add RCC Camera To Scene", false, 300)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add RCC Camera To Scene", false, 300)]
    public static void CreateRCCCamera() {

#if !UNITY_2022_1_OR_NEWER

        if (FindObjectOfType<RCC_Camera>()) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Scene has RCC Camera already!", "Scene has RCC Camera already!", "Close");
            Selection.activeGameObject = FindObjectOfType<RCC_Camera>().gameObject;

        } else {

            GameObject cam = Instantiate(RCC_Settings.Instance.RCCMainCamera.gameObject);
            cam.name = RCC_Settings.Instance.RCCMainCamera.name;
            Selection.activeGameObject = cam.gameObject;

            Undo.RegisterCreatedObjectUndo(cam, "Create RCC Camera");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

#else

        if (FindFirstObjectByType<RCC_Camera>()) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Scene has RCC Camera already!", "Scene has RCC Camera already!", "Close");
            Selection.activeGameObject = FindFirstObjectByType<RCC_Camera>().gameObject;

        } else {

            GameObject cam = Instantiate(RCC_Settings.Instance.RCCMainCamera.gameObject);
            cam.name = RCC_Settings.Instance.RCCMainCamera.name;
            Selection.activeGameObject = cam.gameObject;

            Undo.RegisterCreatedObjectUndo(cam, "Create RCC Camera");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

#endif

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Hood Camera To Vehicle", false, 301)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Hood Camera To Vehicle", false, 301)]
    public static void CreateHoodCamera() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            if (SelectedCar().gameObject.GetComponentInChildren<RCC_HoodCamera>()) {

                EditorUtility.DisplayDialog("Realistic Car Controller | Your Vehicle Has Hood Camera Already!", "Your vehicle has hood camera already!", "Close");
                Selection.activeGameObject = SelectedCar().gameObject.GetComponentInChildren<RCC_HoodCamera>().gameObject;
                return;

            }

            GameObject hoodCam = (GameObject)Instantiate(RCC_Settings.Instance.hoodCamera, SelectedCar().transform.position, SelectedCar().transform.rotation);
            hoodCam.name = RCC_Settings.Instance.hoodCamera.name;
            hoodCam.transform.SetParent(SelectedCar().transform, true);
            hoodCam.GetComponent<ConfigurableJoint>().connectedBody = SelectedCar().gameObject.GetComponent<Rigidbody>();
            hoodCam.GetComponent<ConfigurableJoint>().connectedMassScale = 0f;
            Selection.activeGameObject = hoodCam;

            Undo.RegisterCreatedObjectUndo(hoodCam, "Create Hood Camera");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Hood Camera To Vehicle", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Hood Camera To Vehicle", true)]
    public static bool CheckCreateHoodCamera() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Wheel Camera To Vehicle", false, 302)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Wheel Camera To Vehicle", false, 302)]
    public static void CreateWheelCamera() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            if (SelectedCar().gameObject.GetComponentInChildren<RCC_WheelCamera>()) {

                EditorUtility.DisplayDialog("Realistic Car Controller | Your Vehicle Has Wheel Camera Already!", "Your vehicle has wheel camera already!", "Close");
                Selection.activeGameObject = SelectedCar().gameObject.GetComponentInChildren<RCC_WheelCamera>().gameObject;
                return;

            }

            GameObject wheelCam = new GameObject("WheelCamera");
            wheelCam.transform.SetParent(SelectedCar().transform, false);
            wheelCam.AddComponent<RCC_WheelCamera>();
            Selection.activeGameObject = wheelCam;

            Undo.RegisterCreatedObjectUndo(wheelCam, "Create WheelCamera");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Wheel Camera To Vehicle", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Cameras/Add Wheel Camera To Vehicle", true)]
    public static bool CheckCreateWheelCamera() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }
    #endregion

    #region Lights (Priority: 400-450)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/HeadLight", false, 400)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/HeadLight", false, 400)]
    public static void CreateHeadLight() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            GameObject lightsMain;

            if (!SelectedCar().transform.Find("Lights")) {

                lightsMain = new GameObject("Lights");
                lightsMain.transform.SetParent(SelectedCar().transform, false);

            } else {

                lightsMain = SelectedCar().transform.Find("Lights").gameObject;

            }

            GameObject headLight = Instantiate(RCC_Settings.Instance.headLights, lightsMain.transform.position, lightsMain.transform.rotation) as GameObject;
            headLight.name = RCC_Settings.Instance.headLights.name;
            headLight.transform.SetParent(lightsMain.transform);
            headLight.transform.localRotation = Quaternion.identity;
            headLight.transform.localPosition = new Vector3(0f, 0f, 2f);
            Selection.activeGameObject = headLight;

            Undo.RegisterCreatedObjectUndo(headLight, "Create Headlight");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/HeadLight", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/HeadLight", true)]
    public static bool CheckHeadLight() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Brake", false, 401)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Brake", false, 401)]
    public static void CreateBrakeLight() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            GameObject lightsMain;

            if (!SelectedCar().transform.Find("Lights")) {

                lightsMain = new GameObject("Lights");
                lightsMain.transform.SetParent(SelectedCar().transform, false);

            } else {

                lightsMain = SelectedCar().transform.Find("Lights").gameObject;

            }

            GameObject brakeLight = Instantiate(RCC_Settings.Instance.brakeLights, lightsMain.transform.position, lightsMain.transform.rotation) as GameObject;
            brakeLight.name = RCC_Settings.Instance.brakeLights.name;
            brakeLight.transform.SetParent(lightsMain.transform);
            brakeLight.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            brakeLight.transform.localPosition = new Vector3(0f, 0f, -2f);
            Selection.activeGameObject = brakeLight;

            Undo.RegisterCreatedObjectUndo(brakeLight, "Create Brakelight");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Brake", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Brake", true)]
    public static bool CheckBrakeLight() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Reverse", false, 402)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Reverse", false, 402)]
    public static void CreateReverseLight() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            GameObject lightsMain;

            if (!SelectedCar().transform.Find("Lights")) {

                lightsMain = new GameObject("Lights");
                lightsMain.transform.SetParent(SelectedCar().transform, false);

            } else {

                lightsMain = SelectedCar().transform.Find("Lights").gameObject;

            }

            GameObject reverseLight = Instantiate(RCC_Settings.Instance.reverseLights, lightsMain.transform.position, lightsMain.transform.rotation) as GameObject;
            reverseLight.name = RCC_Settings.Instance.reverseLights.name;
            reverseLight.transform.SetParent(lightsMain.transform);
            reverseLight.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            reverseLight.transform.localPosition = new Vector3(0f, 0f, -2f);
            Selection.activeGameObject = reverseLight;

            Undo.RegisterCreatedObjectUndo(reverseLight, "Create Reverselight");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Reverse", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Reverse", true)]
    public static bool CheckReverseLight() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Indicator", false, 403)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Indicator", false, 403)]
    public static void CreateIndicatorLight() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            GameObject lightsMain;

            if (!SelectedCar().transform.Find("Lights")) {

                lightsMain = new GameObject("Lights");
                lightsMain.transform.SetParent(SelectedCar().transform, false);

            } else {

                lightsMain = SelectedCar().transform.Find("Lights").gameObject;

            }

            GameObject indicatorLight = Instantiate(RCC_Settings.Instance.indicatorLights, lightsMain.transform.position, lightsMain.transform.rotation) as GameObject;
            Vector3 relativePos = SelectedCar().transform.InverseTransformPoint(indicatorLight.transform.position);
            indicatorLight.name = RCC_Settings.Instance.indicatorLights.name;
            indicatorLight.transform.SetParent(lightsMain.transform);

            if (relativePos.z > 0f)
                indicatorLight.transform.localRotation = Quaternion.identity;
            else
                indicatorLight.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            indicatorLight.transform.localPosition = new Vector3(0f, 0f, -2f);
            Selection.activeGameObject = indicatorLight;

            Undo.RegisterCreatedObjectUndo(indicatorLight, "Create Indicatorlight");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Indicator", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Indicator", true)]
    public static bool CheckIndicatorLight() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Interior", false, 404)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Interior", false, 404)]
    public static void CreateInteriorLight() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            GameObject lightsMain;

            if (!SelectedCar().transform.Find("Lights")) {

                lightsMain = new GameObject("Lights");
                lightsMain.transform.SetParent(SelectedCar().transform, false);

            } else {

                lightsMain = SelectedCar().transform.Find("Lights").gameObject;

            }

            GameObject interiorLight = Instantiate(RCC_Settings.Instance.interiorLights, lightsMain.transform.position, lightsMain.transform.rotation) as GameObject;
            interiorLight.name = RCC_Settings.Instance.interiorLights.name;
            interiorLight.transform.SetParent(lightsMain.transform);
            interiorLight.transform.localRotation = Quaternion.identity;
            interiorLight.transform.localPosition = Vector3.zero;
            Selection.activeGameObject = interiorLight;

            Undo.RegisterCreatedObjectUndo(interiorLight, "Create Interiorlight");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Interior", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Add Lights To Vehicle/Interior", true)]
    public static bool CheckInteriorLight() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Duplicate Selected Light", false, 420)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Duplicate Selected Light", false, 420)]
    public static void DuplicateLight() {

        GameObject duplicatedLight = Instantiate(Selection.activeGameObject);

        duplicatedLight.transform.name = Selection.activeGameObject.transform.name + "_D";
        duplicatedLight.transform.SetParent(Selection.activeGameObject.transform.parent);
        duplicatedLight.transform.localPosition = new Vector3(-Selection.activeGameObject.transform.localPosition.x, Selection.activeGameObject.transform.localPosition.y, Selection.activeGameObject.transform.localPosition.z);
        duplicatedLight.transform.localRotation = Selection.activeGameObject.transform.localRotation;
        duplicatedLight.transform.localScale = Selection.activeGameObject.transform.localScale;

        Selection.activeGameObject = duplicatedLight;

        Undo.RegisterCreatedObjectUndo(duplicatedLight, "Duplicated light");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Lights/Duplicate Selected Light", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Lights/Duplicate Selected Light", true)]
    public static bool CheckDuplicateLight() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }
    #endregion

    #region UI (Priority: 500-550)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/UI/Add RCC Canvas To Scene", false, 500)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/UI/Add RCC Canvas To Scene", false, 500)]
    public static void CreateRCCCanvas() {

#if !UNITY_2022_1_OR_NEWER

        if (FindObjectOfType<RCC_DashboardInputs>()) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Scene has RCC Canvas already!", "Scene has RCC Canvas already!", "Close");
            Selection.activeGameObject = FindObjectOfType<RCC_DashboardInputs>().gameObject;

        } else {

            GameObject canvas = Instantiate(RCC_Settings.Instance.RCCCanvas);
            canvas.name = RCC_Settings.Instance.RCCCanvas.name;
            Selection.activeGameObject = canvas;

            Undo.RegisterCreatedObjectUndo(canvas, "Create RCC Canvas");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

#else

        if (FindFirstObjectByType<RCC_DashboardInputs>()) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Scene has RCC Canvas already!", "Scene has RCC Canvas already!", "Close");
            Selection.activeGameObject = FindFirstObjectByType<RCC_DashboardInputs>().gameObject;

        } else {

            GameObject canvas = Instantiate(RCC_Settings.Instance.RCCCanvas);
            canvas.name = RCC_Settings.Instance.RCCCanvas.name;
            Selection.activeGameObject = canvas;

            Undo.RegisterCreatedObjectUndo(canvas, "Create RCC Canvas");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

#endif

    }
    #endregion

    #region Misc (Priority: 550-600)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Exhaust To Vehicle", false, 550)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Exhaust To Vehicle", false, 550)]
    public static void CreateExhaust() {

        if (SelectedCar() == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");

        } else {

            GameObject exhaustsMain;

            if (!SelectedCar().transform.Find("Exhausts")) {
                exhaustsMain = new GameObject("Exhausts");
                exhaustsMain.transform.SetParent(SelectedCar().transform, false);
            } else {
                exhaustsMain = SelectedCar().transform.Find("Exhausts").gameObject;
            }

            GameObject exhaust = (GameObject)Instantiate(RCC_Settings.Instance.exhaustGas, SelectedCar().transform.position, SelectedCar().transform.rotation * Quaternion.Euler(0f, 180f, 0f));
            exhaust.name = RCC_Settings.Instance.exhaustGas.name;
            exhaust.transform.SetParent(exhaustsMain.transform);
            exhaust.transform.localPosition = new Vector3(1f, 0f, -2f);
            Selection.activeGameObject = exhaust;

            Undo.RegisterCreatedObjectUndo(exhaust, "Create Exhaust");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Exhaust To Vehicle", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Exhaust To Vehicle", true)]
    public static bool CheckCreateExhaust() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Mirrors To Vehicle", false, 551)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Mirrors To Vehicle", false, 551)]
    public static void CreateBehavior() {

        if (SelectedCar() == null)
            EditorUtility.DisplayDialog("Realistic Car Controller | Select a vehicle controlled by Realistic Car Controller!", "Select a vehicle controlled by Realistic Car Controller!", "Close");
        else
            CreateMirrors(SelectedCar().gameObject);

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Mirrors To Vehicle", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Create/Misc/Add Mirrors To Vehicle", true)]
    public static bool CheckCreateBehavior() {

        if (!Selection.activeGameObject)
            return false;

        if (Selection.gameObjects.Length > 1)
            return false;

        if (!Selection.activeTransform.gameObject.activeSelf)
            return false;

        return true;

    }
    #endregion

    #endregion

    #region AI Tools (Priority: 700-800)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/AI/Add AI Controller To Vehicle", false, 700)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/AI/Add AI Controller To Vehicle", false, 700)]
    static void CreateAIBehavior() {

        if (!Selection.activeGameObject.GetComponentInParent<RCC_CarControllerV4>(true)) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Your Vehicle Has Not RCC_CarControllerV4", "Your Vehicle Has Not RCC_CarControllerV3.", "Close");
            return;

        }

        if (Selection.activeGameObject.GetComponentInParent<RCC_AICarController>(true)) {

            EditorUtility.DisplayDialog("Realistic Car Controller | Your Vehicle Already Has AI Car Controller", "Your Vehicle Already Has AI Car Controller", "Close");
            return;

        }

        RCC_AICarController ai = Selection.activeGameObject.GetComponentInParent<RCC_CarControllerV4>(true).gameObject.AddComponent<RCC_AICarController>();
        GameObject vehicle = Selection.activeGameObject.GetComponentInParent<RCC_CarControllerV4>(true).gameObject;
        Selection.activeGameObject = vehicle;

        Undo.RegisterCreatedObjectUndo(ai, "Add AI Controller");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/AI/Add AI Controller To Vehicle", true)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/AI/Add AI Controller To Vehicle", true)]
    static bool CheckAIBehavior() {

        if (Selection.gameObjects.Length > 1 || !Selection.activeTransform)
            return false;
        else
            return true;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/AI/Add Waypoints Container To Scene", false, 720)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/AI/Add Waypoints Container To Scene", false, 720)]
    static void CreateWaypointsContainer() {

        GameObject wp = new GameObject("Waypoints Container");
        wp.transform.position = Vector3.zero;
        wp.transform.rotation = Quaternion.identity;
        wp.AddComponent<RCC_AIWaypointsContainer>();
        Selection.activeGameObject = wp;

        Undo.RegisterCreatedObjectUndo(wp, "Create Waypoints Container");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/AI/Add BrakeZones Container To Scene", false, 721)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/AI/Add BrakeZones Container To Scene", false, 721)]
    static void CreateBrakeZonesContainer() {

#if !UNITY_2022_1_OR_NEWER

        if (FindObjectOfType<RCC_AIBrakeZonesContainer>() == null) {

            GameObject bz = new GameObject("Brake Zones Container");
            bz.transform.position = Vector3.zero;
            bz.transform.rotation = Quaternion.identity;
            bz.AddComponent<RCC_AIBrakeZonesContainer>();
            Selection.activeGameObject = bz;

            Undo.RegisterCreatedObjectUndo(bz, "Create Brake Zones Container");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        } else {

            EditorUtility.DisplayDialog("Realistic Car Controller | Your Scene Already Has Brake Zones Container", "Your Scene Already Has Brake Zones", "Close");

        }

#else

        if (FindFirstObjectByType<RCC_AIBrakeZonesContainer>() == null) {

            GameObject bz = new GameObject("Brake Zones Container");
            bz.transform.position = Vector3.zero;
            bz.transform.rotation = Quaternion.identity;
            bz.AddComponent<RCC_AIBrakeZonesContainer>();
            Selection.activeGameObject = bz;

            Undo.RegisterCreatedObjectUndo(bz, "Create Brake Zones Container");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        } else {

            EditorUtility.DisplayDialog("Realistic Car Controller | Your Scene Already Has Brake Zones Container", "Your Scene Already Has Brake Zones", "Close");

        }

#endif

    }
    #endregion

    #region Tools (Priority: 900-1000)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Render Pipeline Converter", false, 900)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Render Pipeline Converter", false, 900)]
    public static void PipelineConverter() {

        RCC_RenderPipelineConverterWindow.ShowWindow();

    }
    #endregion

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Other/Script Header Checker", false, 1000)]
    public static void ScriptHeaderChecker() {

        RCC_ScriptHeaderCheckerWindow.ShowWindow();

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Other/Input System Version Checker", false, 1001)]
    public static void InputSystemVersionChecker() {

        RCC_InputSystemVersionChecker.ManualCheck();

    }

    #region Help and Upgrade (Priority: 2000+)
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Upgrade to Realistic Car Controller Pro", false, 2000)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Upgrade to Realistic Car Controller Pro", false, 2000)]
    public static void Pro() {

        string url = "http://u3d.as/22Bf";
        Application.OpenURL(url);

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Help", false, 2001)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller/Help", false, 2001)]
    public static void Help() {

        EditorUtility.DisplayDialog("Realistic Car Controller | Contact", "Please include your invoice number while sending a contact form.", "Close");

        string url = "http://www.bonecrackergames.com/contact/";
        Application.OpenURL(url);

    }
    #endregion

    #region Static Methods
    public static void CreateMirrors(GameObject vehicle) {

        if (!vehicle.transform.GetComponentInChildren<RCC_Mirror>()) {

            GameObject mirrors = (GameObject)Instantiate(RCC_Settings.Instance.mirrors, vehicle.transform.position, vehicle.transform.rotation);
            mirrors.transform.SetParent(vehicle.GetComponent<RCC_CarControllerV4>().transform, true);
            mirrors.name = "Mirrors";
            Selection.activeGameObject = mirrors;
            EditorUtility.DisplayDialog("Realistic Car Controller | Created Mirrors!", "Created mirrors. Adjust their positions.", "Close");

            Undo.RegisterCreatedObjectUndo(mirrors, "Create Mirrors");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        } else {

            EditorUtility.DisplayDialog("Realistic Car Controller | Vehicle Has Mirrors Already", "Vehicle has mirrors already!", "Close");

        }

    }
    #endregion

}