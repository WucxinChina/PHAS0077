using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RandomReplaceEditor : EditorWindow
{
    private string targetName = "A";
    private GameObject replacementPrefab;

    [MenuItem("Tools/随机替换一半物体")]
    public static void ShowWindow()
    {
        GetWindow<RandomReplaceEditor>("随机替换一半物体");
    }

    void OnGUI()
    {
        GUILayout.Label("随机替换工具", EditorStyles.boldLabel);

        targetName = EditorGUILayout.TextField("目标物体名称", targetName);
        replacementPrefab = (GameObject)EditorGUILayout.ObjectField("替换Prefab", replacementPrefab, typeof(GameObject), false);

        if (GUILayout.Button("执行替换"))
        {
            ReplaceHalfObjects();
        }
    }

    private void ReplaceHalfObjects()
    {
        if (replacementPrefab == null)
        {
            Debug.LogWarning("请指定替换的Prefab！");
            return;
        }

        // 查找所有目标物体
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        List<GameObject> matchedObjects = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == targetName && obj.scene.IsValid()) // 确保是场景里的
            {
                matchedObjects.Add(obj);
            }
        }

        if (matchedObjects.Count == 0)
        {
            Debug.Log("未找到名字为 " + targetName + " 的物体。");
            return;
        }

        // 随机打乱
        ShuffleList(matchedObjects);

        // 替换前一半
        int halfCount = matchedObjects.Count / 2;
        for (int i = 0; i < halfCount; i++)
        {
            GameObject oldObj = matchedObjects[i];

            // 记录位置、旋转、缩放、父级
            Vector3 pos = oldObj.transform.position;
            Quaternion rot = oldObj.transform.rotation;
            Vector3 scale = oldObj.transform.localScale;
            Transform parent = oldObj.transform.parent;

            Undo.DestroyObjectImmediate(oldObj); // 支持撤销
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(replacementPrefab, parent);
            newObj.transform.position = pos;
            newObj.transform.rotation = rot;
            newObj.transform.localScale = scale;

            Undo.RegisterCreatedObjectUndo(newObj, "Replace Object");
        }

        Debug.Log($"成功替换 {halfCount}/{matchedObjects.Count} 个物体。");
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }
}
