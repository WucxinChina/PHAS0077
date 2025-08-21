//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Opens and re-saves all RCCP demo (and addon) scenes once after import,
/// waiting for Unity to mark each scene dirty before saving (or timing out).
/// Tracks updated scenes in a JSON file inside the RCCP asset folder so each only runs once.
/// </summary>
public static class RCCP_SceneUpdater {

    // --- CONFIGURATION ---
    // Timeout (in seconds) to wait for scene.isDirty before forcing save
    private const double DirtyTimeoutSeconds = 4;

    // JSON state file location (set up at Init)
    private static string _stateFilePath;

    // GUIDs of scenes we've already processed
    private static HashSet<string> _processedGuids = new HashSet<string>();

    // Pending scene paths to update
    private static readonly Queue<string> _sceneQueue = new Queue<string>();

    // Are we waiting for Unity to mark the open scene dirty?
    private static bool _waitingForDirty = false;

    // Timestamp when the current scene was opened
    private static double _sceneOpenedTime = 0.0;

    private static string _originalScenePath;

    // Unique identifier for this machine/editor install
    private static string _machineId;

    // Serializable container for JSON state
    [Serializable]
    private class State {
        public string machineId;
        public List<string> processedGuids = new List<string>();
    }

    public static void Check() {

        //// Capture a stable machine identifier
        //_machineId = SystemInfo.deviceUniqueIdentifier;

        //// Delay until after import/domain reload finishes
        //EditorApplication.delayCall += CheckAllScenes;

    }

    public static void CheckAllScenes() {

        //_originalScenePath = EditorSceneManager.GetActiveScene().path;

        //SetupStateFilePath();
        //LoadState();
        //EnqueuePendingScenes();

        //if (_sceneQueue.Count > 0) {

        //    // If currently in Play Mode, wait for exit before starting
        //    if (EditorApplication.isPlaying) {
        //        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        //        EditorApplication.ExitPlaymode();
        //        return;
        //    }

        //    bool decision = EditorUtility.DisplayDialog(
        //           "RCCP_SceneUpdated",
        //           "All RCCP demo scenes need to be updated. Please wait until it updates all the demo scenes automatically.\n\n" +
        //           "You can skip this, but you may experience dark or weak lighting in some demo scenes. Just wait a few seconds, Unity will update and refresh the lighting in the demo scene.",
        //           "Ok", "Skip"
        //       );

        //    if (!decision) {
        //        // mark every queued scene GUID as processed so they won’t run again
        //        foreach (var path in _sceneQueue) {
        //            var guid = AssetDatabase.AssetPathToGUID(path);
        //            _processedGuids.Add(guid);
        //        }

        //        SaveState();
        //        return;
        //    }

        //    EditorApplication.update += ProcessNext;

        //}

    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {

        //if (state == PlayModeStateChange.EnteredEditMode) {

        //    bool decision = EditorUtility.DisplayDialog(
        //        "RCCP_SceneUpdated",
        //        "All RCCP demo scenes need to be updated. Please wait until it updates all the demo scenes automatically.\n\n" +
        //        "You can skip this, but you may experience dark or weak lighting in some demo scenes. Just wait a few seconds, Unity will update and refresh the lighting in the demo scene.",
        //        "Ok", "Skip"
        //    );

        //    if (!decision) {
        //        foreach (var path in _sceneQueue) {
        //            var guid = AssetDatabase.AssetPathToGUID(path);
        //            _processedGuids.Add(guid);
        //        }

        //        SaveState();
        //        return;
        //    }

        //    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        //    EditorApplication.update += ProcessNext;

        //}

    }

    /// <summary>
    /// Compute _stateFilePath inside the RCCP asset's Editor folder.
    /// </summary>
    private static void SetupStateFilePath() {
        // BasePath ends with a slash, e.g. "Assets/Realistic Car Controller Pro/"
        string assetRoot = RCCP_AssetUtilities.BasePath.TrimEnd('/');  // :contentReference[oaicite:0]{index=0}
        string editorFolder = Path.Combine(Directory.GetCurrentDirectory(), assetRoot, "Editor");
        if (!Directory.Exists(editorFolder)) {
            Directory.CreateDirectory(editorFolder);
        }
        _stateFilePath = Path.Combine(editorFolder, "RCCP_SceneUpdaterState.json");
    }

    /// <summary>
    /// Read processed GUIDs from JSON, if it exists.
    /// If the saved machineId differs, clear the processed list.
    /// </summary>
    private static void LoadState() {
        try {
            if (File.Exists(_stateFilePath)) {
                string json = File.ReadAllText(_stateFilePath);
                var state = JsonUtility.FromJson<State>(json);
                if (state != null) {
                    if (state.machineId == _machineId && state.processedGuids != null) {
                        // Same computer: restore which scenes have been updated
                        _processedGuids = new HashSet<string>(state.processedGuids);
                    } else {
                        // Different computer or no machineId stored: reset
                        Debug.Log("RCCP Scene Updater: detected new machine, re-running scene updates.");
                        _processedGuids.Clear();
                        // Immediately persist with the new machineId, empty processed list
                        SaveState();
                    }
                }
            }
        } catch (Exception e) {
            Debug.LogWarning($"RCCP Scene Updater: failed to load state file: {e.Message}");
        }
    }

    /// <summary>
    /// Write out the current machineId and GUID set to JSON.
    /// </summary>
    private static void SaveState() {
        try {
            var state = new State {
                machineId = _machineId,
                processedGuids = new List<string>(_processedGuids)
            };
            string json = JsonUtility.ToJson(state, true);
            File.WriteAllText(_stateFilePath, json);
            AssetDatabase.Refresh();
        } catch (Exception e) {
            Debug.LogWarning($"RCCP Scene Updater: failed to save state file: {e.Message}");
        }
    }

    /// <summary>
    /// Find every .unity scene under both the RCCP asset folder and the shared-assets folder,
    /// enqueue those whose GUID isn’t in our processed set yet.
    /// </summary>
    private static void EnqueuePendingScenes() {
        string rccpFolder = RCCP_AssetUtilities.BasePath.TrimEnd('/');  // :contentReference[oaicite:1]{index=1}
        string sharedFolder = "Assets/BoneCracker Games Shared Assets";
        bool sharedFolderFound = Directory.Exists(sharedFolder);

        string[] guids;

        if (sharedFolderFound)
            guids = AssetDatabase.FindAssets("t:Scene", new[] { rccpFolder, sharedFolder });
        else
            guids = AssetDatabase.FindAssets("t:Scene", new[] { rccpFolder });

        foreach (var guid in guids) {
            if (!_processedGuids.Contains(guid)) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                _sceneQueue.Enqueue(path);
            }
        }
    }

    /// <summary>
    /// Called every Editor frame until the queue is empty.
    /// Opens each scene, waits for it to go dirty (or times out), then saves and records its GUID.
    /// Shows a cancelable progress bar.
    /// </summary>
    private static void ProcessNext() {
        if (_sceneQueue.Count == 0) {
            EditorApplication.update -= ProcessNext;
            EditorUtility.ClearProgressBar();
            Debug.Log("RCCP: All pending demo scenes have been updated.");

            // Prompt about restoring the original scene
            if (!string.IsNullOrEmpty(_originalScenePath) &&
                File.Exists(Path.Combine(Directory.GetCurrentDirectory(), _originalScenePath))) {

                int choice = EditorUtility.DisplayDialogComplex(
                    "Restore Original Scene",
                    "The scene you had open before updating demo scenes may have changes.\n" +
                    "Would you like to Save, Discard changes, or Cancel?",
                    "Save",
                    "Discard",
                    "Cancel"
                );

                if (choice == 0) {
                    var orig = EditorSceneManager.OpenScene(_originalScenePath, OpenSceneMode.Single);
                    if (orig.isDirty) {
                        EditorSceneManager.SaveScene(orig);
                    }
                } else if (choice == 1) {
                    EditorSceneManager.OpenScene(_originalScenePath, OpenSceneMode.Single);
                } // Cancel: do nothing
            }

            return;
        }

        int alreadyDone = _processedGuids.Count;
        int total = alreadyDone + _sceneQueue.Count;
        string path = _sceneQueue.Peek();

        // ——— QUICK ADDITIVE ERROR CHECK ———
        // Open it additively, inspect, then close it again.
        Scene test = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        bool hasErrors = RCCP_SceneErrorChecker.CheckSceneByPath(test.path);
        EditorSceneManager.CloseScene(test, true);

        if (hasErrors) {
            Debug.LogWarning($"RCCP: Skipping “{path}” — missing‐scripts or broken prefabs.");
            // mark as done so we don’t come back
            string guid = AssetDatabase.AssetPathToGUID(path);
            _processedGuids.Add(guid);
            SaveState();
            _sceneQueue.Dequeue();
            return;
        }

        // Step A: Open scene if not already waiting
        if (!_waitingForDirty) {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            _waitingForDirty = true;
            _sceneOpenedTime = EditorApplication.timeSinceStartup;

            bool cancel = EditorUtility.DisplayCancelableProgressBar(
                "RCCP Scene Updater",
                $"Opening scene {alreadyDone + 1}/{total}\n{path}",
                (float)alreadyDone / total
            );
            if (cancel) { Abort(); }
            return;
        }

        // Step B: Scene is open — wait for dirty OR timeout
        var scene = EditorSceneManager.GetActiveScene();
        bool timedOut = (EditorApplication.timeSinceStartup - _sceneOpenedTime) > DirtyTimeoutSeconds;
        if (scene.isDirty || timedOut) {
            if (timedOut && !scene.isDirty) {
                Debug.LogWarning($"RCCP: timed out waiting for scene to become dirty. Saving anyway: {path}");
            }

            EditorSceneManager.SaveScene(scene);

            // record GUID and persist
            string guid = AssetDatabase.AssetPathToGUID(path);
            _processedGuids.Add(guid);
            SaveState();

            bool cancel = EditorUtility.DisplayCancelableProgressBar(
                "RCCP Scene Updater",
                $"Saving scene {alreadyDone + 1}/{total}\n{path}",
                (float)(alreadyDone + 1) / total
            );
            if (cancel) { Abort(); return; }

            _sceneQueue.Dequeue();
            _waitingForDirty = false;
        }
    }

    /// <summary>
    /// Abort processing, clear progress bar, and unsubscribe.
    /// </summary>
    private static void Abort() {
        EditorApplication.update -= ProcessNext;
        EditorUtility.ClearProgressBar();
        Debug.LogWarning("RCCP: Scene update aborted by user.");
    }

}