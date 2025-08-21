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

public class RCCP_CopyMeshCollidersEditor : EditorWindow {

    /// <summary>
    /// A simple container to hold one source‐target prefab pair.
    /// </summary>
    private class PrefabPair {

        /// <summary>
        /// Prefab asset to copy MeshColliders from.
        /// </summary>
        public GameObject sourcePrefab;

        /// <summary>
        /// Prefab asset to paste MeshColliders into.
        /// </summary>
        public GameObject targetPrefab;
    }

    /// <summary>
    /// List of all source–target couples.
    /// </summary>
    private List<PrefabPair> prefabPairs = new List<PrefabPair>();

    private Vector2 scrollPos;

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Transfer Colliders From Source to Target", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Transfer Colliders From Source to Target", false, -65)]
    public static void ShowWindow() {

        GetWindow<RCCP_CopyMeshCollidersEditor>("Transfer Colliders From Source to Target");
    }

    private void OnEnable() {

        // Ensure at least one empty pair to start with
        if (prefabPairs.Count == 0) {

            prefabPairs.Add(new PrefabPair());
        }
    }

    private void OnGUI() {

        EditorGUILayout.LabelField("Copy RCCP_Colliders from Source to Target Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Scroll view in case there are many pairs
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

        for (int i = 0; i < prefabPairs.Count; i++) {

            PrefabPair pair = prefabPairs[i];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Pair #{i + 1}", EditorStyles.boldLabel);

            // Source prefab field
            pair.sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Source Prefab",
                pair.sourcePrefab,
                typeof(GameObject),
                false
            );

            // Target prefab field
            pair.targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Target Prefab",
                pair.targetPrefab,
                typeof(GameObject),
                false
            );

            // Remove‐pair button
            if (prefabPairs.Count > 1) {

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Remove This Pair", GUILayout.MaxWidth(150))) {

                    prefabPairs.RemoveAt(i);
                    i--; // adjust index for next iteration
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        // Add a new empty pair
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add New Pair", GUILayout.MaxWidth(120))) {

            prefabPairs.Add(new PrefabPair());
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Copy button
        if (GUILayout.Button("Copy MeshColliders for All Pairs")) {

            if (ValidateAllPairs()) {

                CopyAllPairs();
            }
        }
    }

    /// <summary>
    /// Ensures that every pair has both a source and a target assigned.
    /// </summary>
    private bool ValidateAllPairs() {

        for (int i = 0; i < prefabPairs.Count; i++) {

            if (prefabPairs[i].sourcePrefab == null || prefabPairs[i].targetPrefab == null) {

                EditorUtility.DisplayDialog(
                    "Error",
                    $"Pair #{i + 1} is missing a Source or Target prefab. Assign both before proceeding.",
                    "OK"
                );
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Iterates through each prefab pair and performs the copy operation.
    /// </summary>
    private void CopyAllPairs() {

        int successCount = 0;

        foreach (PrefabPair pair in prefabPairs) {

            bool didCopy = CopyMeshCollidersBetween(pair.sourcePrefab, pair.targetPrefab);

            if (didCopy) {

                successCount++;
            }
        }

        if (successCount > 0) {

            EditorUtility.DisplayDialog(
                "Done",
                $"MeshColliders copied for {successCount} out of {prefabPairs.Count} pair(s).",
                "OK"
            );
        }
    }

    /// <summary>
    /// Removes all MeshCollider components from the target prefab, then copies each 'RCCP_Colliders' GameObject
    /// (and its children) from the source prefab into the same hierarchy path under the target prefab.
    ///
    /// Returns true if operation succeeded, false on any error.
    /// </summary>
    private bool CopyMeshCollidersBetween(GameObject sourcePrefab, GameObject targetPrefab) {

        // 1. Resolve asset paths for both prefabs
        string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
        string targetPath = AssetDatabase.GetAssetPath(targetPrefab);

        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath)) {

            Debug.LogError($"[RCCP] Failed to get asset path for source or target prefab.");
            return false;
        }

        // 2. Load the prefab contents into editable temporary roots
        GameObject sourceRoot = PrefabUtility.LoadPrefabContents(sourcePath);
        GameObject targetRoot = PrefabUtility.LoadPrefabContents(targetPath);

        if (sourceRoot == null || targetRoot == null) {

            Debug.LogError($"[RCCP] Could not load prefab contents for:\n    Source: {sourcePath}\n    Target: {targetPath}");
            return false;
        }

        // 3. Remove all existing MeshCollider components from target hierarchy
        MeshCollider[] existing = targetRoot.GetComponentsInChildren<MeshCollider>(true);

        foreach (MeshCollider mc in existing) {

            DestroyImmediate(mc);
        }

        // 4. Find every "RCCP_Colliders" node in the source prefab
        Transform[] sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform srcNode in sourceTransforms) {

            if (srcNode.name == "RCCP_Colliders") {

                // Build full relative path from sourceRoot to this node (e.g. "Chassis/Trunk/RCCP_Colliders")
                string relPath = GetRelativePath(sourceRoot.transform, srcNode);
                string parentPath = System.IO.Path.GetDirectoryName(relPath).Replace("\\", "/");

                // Find the matching parentTransform under targetRoot
                Transform targetParent = null;

                if (string.IsNullOrEmpty(parentPath)) {

                    // If the collider sits directly under the root
                    targetParent = targetRoot.transform;
                } else {

                    targetParent = targetRoot.transform.Find(parentPath);
                }

                if (targetParent != null) {

                    // If a previous "RCCP_Colliders" exists under targetParent, delete it first
                    Transform oldChild = targetParent.Find("RCCP_Colliders");
                    if (oldChild != null) {

                        DestroyImmediate(oldChild.gameObject);
                    }

                    // Instantiate a duplicate of the source's "RCCP_Colliders" GameObject
                    GameObject newGO = Instantiate(srcNode.gameObject);
                    newGO.name = srcNode.name;
                    newGO.transform.SetParent(targetParent, false);
                }
                // If targetParent is null, simply skip: source and target hierarchy mismatch
            }
        }

        // 5. Save changes back into the target prefab asset
        PrefabUtility.SaveAsPrefabAsset(targetRoot, targetPath);

        // 6. Unload both prefab contents
        PrefabUtility.UnloadPrefabContents(sourceRoot);
        PrefabUtility.UnloadPrefabContents(targetRoot);

        return true;
    }

    /// <summary>
    /// Returns the hierarchy path (child of 'root') to 'node', e.g. if root is Vehicle,
    /// and node is Vehicle/Trunk/RCCP_Colliders, this returns "Trunk/RCCP_Colliders".
    /// </summary>
    private string GetRelativePath(Transform root, Transform node) {

        if (node == root) {

            return "";
        }

        string path = node.name;
        Transform current = node.parent;

        while (current != null && current != root) {

            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
