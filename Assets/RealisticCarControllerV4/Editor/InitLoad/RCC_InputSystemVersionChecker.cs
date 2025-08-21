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
using System.Linq;

[InitializeOnLoad]
public static class RCC_InputSystemVersionChecker {

    private const string TARGET_VERSION = "1.11.0";
    private const string PREF_KEY = "InputSystemVersionWarningShown";

    static RCC_InputSystemVersionChecker() {
        // run once when the domain reloads
        EditorApplication.delayCall += CheckInputSystemVersion;
    }

    // --------------------------------------------------------------------

    private static void CheckInputSystemVersion() {

        if (SessionState.GetBool(PREF_KEY, false))
            return;                      // already warned this editor session

        var listRequest = UnityEditor.PackageManager.Client.List();
        EditorApplication.CallbackFunction poll = null;

        poll = () => {

            if (!listRequest.IsCompleted)
                return;

            EditorApplication.update -= poll;     // stop polling

            if (listRequest.Status != UnityEditor.PackageManager.StatusCode.Success)
                return;                           // offline or other failure

            var pkg = listRequest.Result.FirstOrDefault(p => p.name == "com.unity.inputsystem");
            if (pkg == null)
                return;                           // package not installed

            if (IsVersionAtRisk(pkg.version, TARGET_VERSION)) {
                // Delay dialog until editor is stable
                EditorApplication.delayCall += () => ShowUpdateDialog(pkg.version);
                SessionState.SetBool(PREF_KEY, true);
            }
        };

        EditorApplication.update += poll;
    }

    // --------------------------------------------------------------------
    //  Version helpers
    // --------------------------------------------------------------------

    /// <summary>
    ///  Returns true when <paramref name="current"/> is *older or equal* to <paramref name="target"/>.
    ///  Works across all Unity versions and ignores "-pre", "-rc", etc.
    /// </summary>
    private static bool IsVersionAtRisk(string current, string target) {
        return VersionOrNull(current) <= VersionOrNull(target);
    }

    private static Version VersionOrNull(string v) {
        // keep only the "major.minor.patch" part
        string core = v.Split('-', '+')[0];
        Version.TryParse(core, out var result);
        return result ?? new Version(0, 0);
    }

    // --------------------------------------------------------------------
    //  UI helpers
    // --------------------------------------------------------------------

    private static void ShowUpdateDialog(string current) {

        bool open = EditorUtility.DisplayDialog(
            "Input System Update Recommended",
            $"Your project is using Input System {current}.\n\n" +
            "This build is known to have compatibility issues.\n\n" +
            "Open Package Manager to update now?",
            "Open Package Manager",
            "Later"
        );

        if (open)
            UnityEditor.PackageManager.UI.Window.Open("com.unity.inputsystem");
    }

    // --------------------------------------------------------------------
    //  Menu
    // --------------------------------------------------------------------

    public static void ManualCheck() {
        SessionState.SetBool(PREF_KEY, false); // force a fresh check
        CheckInputSystemVersion();
    }
}


