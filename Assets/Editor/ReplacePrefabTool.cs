// Assets/Editor/ReplacePrefabTool.cs
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class ReplacePrefabTool : EditorWindow
{
    public GameObject sourcePrefab;  // 要被替换的Prefab（可留空，表示用当前Selection）
    public GameObject targetPrefab;  // 替换成的Prefab
    public bool keepWorldPosition = true;  // 保留世界位姿
    public bool keepName = true;           // 保留名称
    public bool keepLayerTagStatic = true; // 保留Layer/Tag/Static
    public bool keepParent = true;         // 保留父物体与兄弟顺序

    [MenuItem("Tools/Replace Prefab (Batch)")]
    static void Open() => GetWindow<ReplacePrefabTool>("Replace Prefab");

    void OnGUI()
    {
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab (optional)", sourcePrefab, typeof(GameObject), false);
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);
        keepWorldPosition = EditorGUILayout.Toggle("Keep World Transform", keepWorldPosition);
        keepName = EditorGUILayout.Toggle("Keep Name", keepName);
        keepLayerTagStatic = EditorGUILayout.Toggle("Keep Layer/Tag/Static", keepLayerTagStatic);
        keepParent = EditorGUILayout.Toggle("Keep Parent & Sibling Index", keepParent);

        EditorGUILayout.Space();
        if (GUILayout.Button("Replace Selected Instances"))
            ReplaceSelected();

        if (GUILayout.Button("Replace All Instances Of Source Prefab"))
            ReplaceAllInstancesOfSource();
    }

    static bool IsInstanceOf(GameObject go, GameObject prefab)
    {
        if (prefab == null) return false;
        var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
        return src == prefab;
    }

    void ReplaceAllInstancesOfSource()
    {
        if (targetPrefab == null || sourcePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign both Source and Target Prefab.", "OK");
            return;
        }

        var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var list = new List<GameObject>();
        foreach (var root in allRoots)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (IsInstanceOf(t.gameObject, sourcePrefab))
                    list.Add(t.gameObject);
            }
        }

        Selection.objects = list.ToArray();
        ReplaceSelected();
    }

    void ReplaceSelected()
    {
        if (targetPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign Target Prefab.", "OK");
            return;
        }

        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Select instances to replace, or use 'Replace All...'", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int replaced = 0;
        foreach (var inst in selection)
        {
            if (sourcePrefab != null && !IsInstanceOf(inst, sourcePrefab))
                continue;

            var parent = inst.transform.parent;
            int sibling = inst.transform.GetSiblingIndex();

            Vector3 pos = keepWorldPosition ? inst.transform.position : inst.transform.localPosition;
            Quaternion rot = keepWorldPosition ? inst.transform.rotation : inst.transform.localRotation;
            Vector3 scale = inst.transform.localScale;

            string name = inst.name;
            int layer = inst.layer;
            string tag = inst.tag;
            bool isStatic = inst.isStatic;

            // 实例化目标
            var newObj = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            Undo.RegisterCreatedObjectUndo(newObj, "Replace Prefab");

            if (keepParent && parent) newObj.transform.SetParent(parent, true);

            if (keepWorldPosition)
            {
                newObj.transform.SetPositionAndRotation(pos, rot);
                newObj.transform.localScale = scale;
            }
            else
            {
                newObj.transform.localPosition = pos;
                newObj.transform.localRotation = rot;
                newObj.transform.localScale = scale;
            }

            if (keepParent && parent) newObj.transform.SetSiblingIndex(sibling);
            if (keepName) newObj.name = name;
            if (keepLayerTagStatic)
            {
                newObj.layer = layer;
                try { newObj.tag = tag; } catch { /* 目标不存在该Tag时忽略 */ }
                newObj.isStatic = isStatic;
            }

            Undo.DestroyObjectImmediate(inst);
            replaced++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Replaced {replaced} instance(s).");
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }
}
