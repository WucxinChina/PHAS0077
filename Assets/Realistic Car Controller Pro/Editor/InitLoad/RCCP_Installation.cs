//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RCCP_Installation {

    public static void CheckProjectLayers() {

        bool layer_RCC;
        bool layer_RCC_WheelCollider;
        bool layer_RCC_DetachablePart;
        bool layer_RCC_Prop;
        bool layer_RCC_Obstacle;

        string[] missingLayers = new string[5];

        layer_RCC = LayerExists(RCCP_Settings.Instance.RCCPLayer);
        layer_RCC_WheelCollider = LayerExists(RCCP_Settings.Instance.RCCPWheelColliderLayer);
        layer_RCC_DetachablePart = LayerExists(RCCP_Settings.Instance.RCCPDetachablePartLayer);
        layer_RCC_Prop = LayerExists(RCCP_Settings.Instance.RCCPPropLayer);
        layer_RCC_Obstacle = LayerExists(RCCP_Settings.Instance.RCCPObstacleLayer);

        if (!layer_RCC)
            missingLayers[0] = RCCP_Settings.Instance.RCCPLayer;

        if (!layer_RCC_WheelCollider)
            missingLayers[1] = RCCP_Settings.Instance.RCCPWheelColliderLayer;

        if (!layer_RCC_DetachablePart)
            missingLayers[2] = RCCP_Settings.Instance.RCCPDetachablePartLayer;

        if (!layer_RCC_Prop)
            missingLayers[3] = RCCP_Settings.Instance.RCCPPropLayer;

        if (!layer_RCC_Obstacle)
            missingLayers[4] = RCCP_Settings.Instance.RCCPObstacleLayer;

        if (!layer_RCC || !layer_RCC_DetachablePart || !layer_RCC_WheelCollider || !layer_RCC_Prop || !layer_RCC_Obstacle) {

            if (EditorUtility.DisplayDialog("Realistic Car Controller Pro | Found Missing Layers For Realistic Car Controller Pro", "These layers will be added to the Tags and Layers\n\n" + missingLayers[0] + "\n" + missingLayers[1] + "\n" + missingLayers[2] + "\n" + missingLayers[3] + "\n" + missingLayers[4], "Add")) {

                CheckLayer(RCCP_Settings.Instance.RCCPLayer);
                CheckLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);
                CheckLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);
                CheckLayer(RCCP_Settings.Instance.RCCPPropLayer);
                CheckLayer(RCCP_Settings.Instance.RCCPObstacleLayer);

            }

        }

    }

    public static void CheckMissingWheelSlipParticles() {

        RCCP_GroundMaterials.Instance.CheckWheelPrefabsForMissingScript();

    }

    public static bool CheckPrefabHasMissingScripts(GameObject prefab) {

        // Get all components in the prefab
        Component[] components = prefab.GetComponentsInChildren<Component>(true);

        foreach (Component component in components) {

            // Check if the component is null, which indicates a missing script
            if (component == null) {

                Debug.LogWarning("Prefab '" + prefab.name + "' has missing script.");
                return true;

            }

        }

        return false;

    }

    public static void CollisionMatrix() {

        bool layer_RCC;
        bool layer_RCC_WheelCollider;
        bool layer_RCC_DetachablePart;
        bool layer_RCC_Prop;

        layer_RCC = LayerExists("RCCP_Vehicle");
        layer_RCC_WheelCollider = LayerExists("RCCP_WheelCollider");
        layer_RCC_DetachablePart = LayerExists("RCCP_DetachablePart");
        layer_RCC_Prop = LayerExists("RCCP_Prop");

        if (layer_RCC && layer_RCC_DetachablePart)
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer), LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer), true);

        if (layer_RCC_WheelCollider && layer_RCC_DetachablePart)
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer), LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer), true);

        if (layer_RCC_WheelCollider && layer_RCC_Prop)
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer), LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPPropLayer), true);

    }

    public static bool CheckTag(string tagName) {

        if (TagExists(tagName))
            return true;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName)) {

            int index = tagsProp.arraySize;

            tagsProp.InsertArrayElementAtIndex(index);
            SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);

            sp.stringValue = tagName;
            Debug.Log("Tag: " + tagName + " has been added.");

            tagManager.ApplyModifiedProperties();

            return true;

        }

        return false;

    }

    public static string NewTag(string name) {

        CheckTag(name);

        if (name == null || name == "")
            name = "Untagged";

        return name;

    }

    public static bool RemoveTag(string tagName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        if (PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName)) {

            SerializedProperty sp;

            for (int i = 0, j = tagsProp.arraySize; i < j; i++) {

                sp = tagsProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == tagName) {

                    tagsProp.DeleteArrayElementAtIndex(i);
                    Debug.Log("Tag: " + tagName + " has been removed.");
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

            }

        }

        return false;

    }

    public static bool TagExists(string tagName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        return PropertyExists(tagsProp, 0, 10000, tagName);

    }

    public static bool CheckLayer(string layerName) {

        if (LayerExists(layerName))
            return true;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        if (!PropertyExists(layersProp, 0, 31, layerName)) {

            SerializedProperty sp;

            for (int i = 8, j = 31; i < j; i++) {

                sp = layersProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == "") {

                    sp.stringValue = layerName;
                    Debug.Log("Layer: " + layerName + " has been added.");
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

                if (i == j)
                    Debug.Log("All allowed layers have been filled.");

            }

        }

        return false;

    }

    public static string NewLayer(string name) {

        if (name != null || name != "")
            CheckLayer(name);

        return name;

    }

    public static bool RemoveLayer(string layerName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        if (PropertyExists(layersProp, 0, layersProp.arraySize, layerName)) {

            SerializedProperty sp;

            for (int i = 0, j = layersProp.arraySize; i < j; i++) {

                sp = layersProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == layerName) {

                    sp.stringValue = "";
                    Debug.Log("Layer: " + layerName + " has been removed.");
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

            }

        }

        return false;

    }

    public static bool LayerExists(string layerName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        return PropertyExists(layersProp, 0, 31, layerName);

    }

    private static bool PropertyExists(SerializedProperty property, int start, int end, string value) {

        for (int i = start; i < end; i++) {

            SerializedProperty t = property.GetArrayElementAtIndex(i);

            if (t.stringValue.Equals(value))
                return true;

        }

        return false;

    }

    public static void CheckAllLayers() {

        CheckAllVehicleLayers();
        CheckAllDetachablePartLayers();
        CheckAllTrailerLayers();
        CheckAllPropLayers();

    }

    public static void CheckAllVehicleLayers() {

        List<RCCP_CarController> foundPrefabs = new List<RCCP_CarController>();

        bool cancelled = false;

        try {

            string[] paths = SearchByFilter("t:prefab").ToArray();
            int progress = 0;

            foreach (string path in paths) {

                if (EditorUtility.DisplayCancelableProgressBar("Searching for RCCP Vehicles..", $"Scanned {progress}/{paths.Length} prefabs. Found {foundPrefabs.Count} new vehicles.", progress / (float)paths.Length)) {

                    cancelled = true;
                    break;

                }

                progress++;

                RCCP_CarController rccp = AssetDatabase.LoadAssetAtPath<RCCP_CarController>(path);

                if (!rccp)
                    continue;

                foundPrefabs.Add(rccp);

            }

        } finally {

            EditorUtility.ClearProgressBar();

            if (!cancelled) {

                List<RCCP_CarController> allVehicles = new List<RCCP_CarController>();

                for (int i = 0; i < foundPrefabs.Count; i++) {

                    if (!allVehicles.Contains(foundPrefabs[i]))
                        allVehicles.Add(foundPrefabs[i]);

                }

                foreach (RCCP_CarController target in allVehicles) {

                    target.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

                    foreach (Transform item in target.GetComponentsInChildren<Transform>(true))
                        item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

                    foreach (RCCP_WheelCollider item in target.GetComponentsInChildren<RCCP_WheelCollider>(true))
                        item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

                    foreach (RCCP_DetachablePart item in target.GetComponentsInChildren<RCCP_DetachablePart>(true))
                        item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

                }

            }

            Resources.UnloadUnusedAssets();

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CollisionMatrix();

    }

    public static void CheckAllDetachablePartLayers() {

        List<RCCP_DetachablePart> foundPrefabs = new List<RCCP_DetachablePart>();

        bool cancelled = false;

        try {

            string[] paths = SearchByFilter("t:prefab").ToArray();
            int progress = 0;

            foreach (string path in paths) {

                if (EditorUtility.DisplayCancelableProgressBar("Searching for RCCP Detachable Parts..", $"Scanned {progress}/{paths.Length} prefabs. Found {foundPrefabs.Count} new detachable parts.", progress / (float)paths.Length)) {

                    cancelled = true;
                    break;

                }

                progress++;

                RCCP_DetachablePart rccp = AssetDatabase.LoadAssetAtPath<RCCP_DetachablePart>(path);

                if (!rccp)
                    continue;

                foundPrefabs.Add(rccp);

            }

        } finally {

            EditorUtility.ClearProgressBar();

            if (!cancelled) {

                List<RCCP_DetachablePart> allParts = new List<RCCP_DetachablePart>();

                for (int i = 0; i < foundPrefabs.Count; i++) {

                    if (!allParts.Contains(foundPrefabs[i]))
                        allParts.Add(foundPrefabs[i]);

                }

                foreach (RCCP_DetachablePart target in allParts) {

                    target.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

                    foreach (Transform item in target.GetComponentsInChildren<Transform>(true))
                        item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

                }

            }

            Resources.UnloadUnusedAssets();

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CollisionMatrix();

    }

    public static void CheckAllPropLayers() {

        List<RCCP_Prop> foundPrefabs = new List<RCCP_Prop>();

        bool cancelled = false;

        try {

            string[] paths = SearchByFilter("t:prefab").ToArray();
            int progress = 0;

            foreach (string path in paths) {

                if (EditorUtility.DisplayCancelableProgressBar("Searching for RCCP Props..", $"Scanned {progress}/{paths.Length} prefabs. Found {foundPrefabs.Count} new props.", progress / (float)paths.Length)) {

                    cancelled = true;
                    break;

                }

                progress++;

                RCCP_Prop rccp = AssetDatabase.LoadAssetAtPath<RCCP_Prop>(path);

                if (!rccp)
                    continue;

                foundPrefabs.Add(rccp);

            }

        } finally {

            EditorUtility.ClearProgressBar();

            if (!cancelled) {

                List<RCCP_Prop> allProps = new List<RCCP_Prop>();

                for (int i = 0; i < foundPrefabs.Count; i++) {

                    if (!allProps.Contains(foundPrefabs[i]))
                        allProps.Add(foundPrefabs[i]);

                }

                foreach (RCCP_Prop target in allProps) {

                    target.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPPropLayer);

                    foreach (Transform item in target.GetComponentsInChildren<Transform>(true))
                        item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPPropLayer);

                }

            }

            Resources.UnloadUnusedAssets();

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CollisionMatrix();

    }

    public static void CheckAllTrailerLayers() {

        List<RCCP_TrailerController> foundPrefabs = new List<RCCP_TrailerController>();

        bool cancelled = false;

        try {

            string[] paths = SearchByFilter("t:prefab").ToArray();
            int progress = 0;

            foreach (string path in paths) {

                if (EditorUtility.DisplayCancelableProgressBar("Searching for RCCP Trailers..", $"Scanned {progress}/{paths.Length} prefabs. Found {foundPrefabs.Count} new trailers.", progress / (float)paths.Length)) {

                    cancelled = true;
                    break;

                }

                progress++;

                RCCP_TrailerController rccp = AssetDatabase.LoadAssetAtPath<RCCP_TrailerController>(path);

                if (!rccp)
                    continue;

                foundPrefabs.Add(rccp);

            }

        } finally {

            EditorUtility.ClearProgressBar();

            if (!cancelled) {

                List<RCCP_TrailerController> allTrailers = new List<RCCP_TrailerController>();

                for (int i = 0; i < foundPrefabs.Count; i++) {

                    if (!allTrailers.Contains(foundPrefabs[i]))
                        allTrailers.Add(foundPrefabs[i]);

                }

                foreach (RCCP_TrailerController target in allTrailers) {

                    target.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

                    foreach (Transform item in target.GetComponentsInChildren<Transform>(true))
                        item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

                }

            }

            Resources.UnloadUnusedAssets();

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CollisionMatrix();

    }

    public static IEnumerable<string> SearchByFilter(string filter) {

        foreach (string guid in AssetDatabase.FindAssets(filter))
            yield return AssetDatabase.GUIDToAssetPath(guid);

    }

}
