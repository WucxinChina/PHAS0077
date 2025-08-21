//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

//using UnityEditor;
//using UnityEditorInternal;
//using UnityEngine;

///// <summary>
///// Editor window that applies a LensFlare component from a reference prefab
///// into all currently selected GameObjects.
///// </summary>
//public class RCCP_FlareSettingsApplier : EditorWindow {

//    // Assign your existing SO here. It must have a public GameObject field
//    // that points to a prefab containing a properly configured LensFlare.
//    public Object sharedSettingsSO;

//    // Drag-in the prefab from your SO via this field at runtime
//    private GameObject _flarePrefab;

//    [MenuItem("RCCP/Apply Flare Settings")]
//    public static void OpenWindow() {

//        // Open the tool window
//        GetWindow<RCCP_FlareSettingsApplier>("RCCP Flare Applier");

//    }

//    private void OnGUI() {

//        EditorGUILayout.Space();

//        // Let the user pick their ScriptableObject asset
//        sharedSettingsSO = EditorGUILayout.ObjectField(
//            "Shared Settings SO",
//            sharedSettingsSO,
//            typeof(Object),
//            false);

//        // If they picked a valid SO, try to extract a GameObject field named "flarePrefab"
//        if (sharedSettingsSO != null) {

//            // Use reflection to find a public GameObject field called "flarePrefab"
//            var so = new SerializedObject(sharedSettingsSO);
//            var prop = so.FindProperty("flarePrefab");
//            if (prop != null && prop.objectReferenceValue is GameObject go) {
//                _flarePrefab = go;
//            } else {
//                _flarePrefab = null;
//            }
//        }

//        // Show warning if we could not find the prefab
//        if (sharedSettingsSO != null && _flarePrefab == null) {
//            EditorGUILayout.HelpBox(
//                "Your SO needs a public GameObject field named \"flarePrefab\" pointing at a prefab\n" +
//                "that has the configured LensFlare component.",
//                MessageType.Warning);
//        }

//        EditorGUILayout.Space();

//        using (new EditorGUI.DisabledScope(_flarePrefab == null)) {
//            if (GUILayout.Button("Apply Flare To Selected")) {
//                ApplyFlareToSelection();
//            }
//        }
//    }

//    /// <summary>
//    /// Copies the LensFlare component from the reference prefab
//    /// into each selected GameObject in the Hierarchy.
//    /// </summary>
//    private void ApplyFlareToSelection() {

//        // Get the LensFlare from the prefab
//        LensFlare source = _flarePrefab.GetComponent<LensFlare>();
//        if (source == null) {
//            Debug.LogWarning("Reference prefab has no LensFlare component");
//            return;
//        }

//        // Iterate over every selected GameObject
//        foreach (GameObject targetGO in Selection.gameObjects) {

//            // Add or find the LensFlare on the target
//            LensFlare target = targetGO.GetComponent<LensFlare>();
//            if (target == null) {
//                target = Undo.AddComponent<LensFlare>(targetGO);
//            }

//            // Allow undo of property changes
//            Undo.RecordObject(target, "Apply LensFlare Settings");

//            // Copy all serialized values (including the Flare asset reference!
//            ComponentUtility.CopyComponent(source);
//            ComponentUtility.PasteComponentValues(target);

//            // Mark scene dirty so changes are saved
//            EditorUtility.SetDirty(target);
//        }

//        Debug.Log($"Applied LensFlare from {_flarePrefab.name} to {Selection.gameObjects.Length} object(s).");
//    }
//}
