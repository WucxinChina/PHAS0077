//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RCCP_SceneErrorChecker {

    /// <summary>
    /// Loads the scene at the given asset path (without needing Build Settings),
    /// suppresses all error‐level logs while it loads, then restores the console
    /// and clears out any residual messages before running SceneHasErrors().
    /// </summary>
    public static bool CheckSceneByPath(string sceneAssetPath) {

        // 1) Ask user to save any dirty scenes
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        // 2) Remember original scene
        Scene original = EditorSceneManager.GetActiveScene();

        // 3) Suppress error‐level messages in the Console window
        bool prevShowErrors = EditorPrefs.GetBool("ConsoleWindowFilter.ShowErrors", true);
        EditorPrefs.SetBool("ConsoleWindowFilter.ShowErrors", false);

        // 4) Load target scene additively
        Scene target = EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Additive);

        // 5) Restore Console error filter
        EditorPrefs.SetBool("ConsoleWindowFilter.ShowErrors", prevShowErrors);

        // 6) Clear any messages that snuck through
        ClearConsole();

        // 7) Run your missing‐script / broken‐prefab check
        bool hasErrors = SceneHasErrors(target);

        // 8) Close the loaded scene and go back to the original
        EditorSceneManager.CloseScene(target, true);

        if (original != null && original.path != "")
            EditorSceneManager.OpenScene(original.path, OpenSceneMode.Single);

        return hasErrors;
    }

    private static void ClearConsole() {
        // Reflection magic to call UnityEditor.LogEntries.Clear()
        var logEntries = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
        var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }

    /// <summary>
    /// Returns true if the scene has any missing‐script components or broken prefab instances.
    /// </summary>
    private static bool SceneHasErrors(Scene scene) {

        foreach (var root in scene.GetRootGameObjects()) {

            // 1) Missing‐script check
            foreach (var c in root.GetComponentsInChildren<Component>(true)) {
                if (c == null)
                    return true;
            }

            // 2) Broken prefab instance check
            PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(root);

            // Check for prefab instance status without using deprecated Disconnected value
            if (status == PrefabInstanceStatus.Connected || status == PrefabInstanceStatus.MissingAsset)
                return true;

        }

        return false;
    }
}
