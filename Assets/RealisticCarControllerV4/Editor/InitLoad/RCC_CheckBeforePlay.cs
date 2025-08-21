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
using System.Collections;
using System.Collections.Generic;

// ensure class initializer is called whenever scripts recompile
[InitializeOnLoad]
public static class PlayModeStateChangedExample {

    // register an event handler when the class is initialized
    static PlayModeStateChangedExample() {

        EditorApplication.delayCall += () => {

            EditorApplication.playModeStateChanged += LogPlayModeState;

        };

    }

    public static void LogPlayModeState(PlayModeStateChange state) {

        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (EditorPrefs.GetBool("RCC_IgnorePlatformWarnings", false))
            return;

        if (RCC_Settings.Instance == null)
            return;

        Debug.Log("RCC: Checking Mobile Controller settings based on target platform...");

        if (!RCC_Settings.Instance.mobileControllerEnabled && (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)) {

            int i = EditorUtility.DisplayDialogComplex(
                "Realistic Car Controller | Mobile Controller.",
                "Your target platform is mobile, but it's not enabled in RCC Settings yet.",
                "Enable it",
                "Ignore for now",
                "Ignore and don't warn me again"
            );

            switch (i) {
                case 0:
                    RCC_Settings.Instance.mobileControllerEnabled = true;
                    EditorUtility.SetDirty(RCC_Settings.Instance);
                    Debug.Log("RCC: Mobile Controller enabled.");
                    break;
                case 2:
                    EditorPrefs.SetBool("RCC_IgnorePlatformWarnings", true);
                    Debug.Log("RCC: Mobile Controller warning disabled permanently.");
                    break;
            }
        }

        if (RCC_Settings.Instance.mobileControllerEnabled && (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)) {

            int i = EditorUtility.DisplayDialogComplex(
                "Realistic Car Controller | Mobile Controller.",
                "Your target platform is not mobile, but it's still enabled in RCC Settings.",
                "Disable it",
                "Ignore for now",
                "Ignore and don't warn me again"
            );

            switch (i) {

                case 0:
                    RCC_Settings.Instance.mobileControllerEnabled = false;
                    EditorUtility.SetDirty(RCC_Settings.Instance);
                    Debug.Log("RCC: Mobile Controller disabled.");
                    break;
                case 2:
                    EditorPrefs.SetBool("RCC_IgnorePlatformWarnings", true);
                    Debug.Log("RCC: Mobile Controller warning disabled permanently.");
                    break;

            }

        }

    }

}
