//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public class RCCP_MobilePlatformNotifier : IActiveBuildTargetChanged {

    private const string PopupShownKey = "RCCP_MobilePlatformNotifier";

    static RCCP_MobilePlatformNotifier() {

        // Reset the popup flag when the editor is started
        EditorPrefs.SetBool(PopupShownKey, false);

    }

    public int callbackOrder => 0;

    public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget) {

        // Check if the new build target is a mobile platform
        if ((newTarget == BuildTarget.Android || newTarget == BuildTarget.iOS))
            ShowMobilePlatformInfo();

        ShowListInfo();

    }

    private static void ShowMobilePlatformInfo() {

        EditorUtility.DisplayDialog(

            "Realistic Car Controller Pro | Mobile Platform Detected",
            "You have switched the build platform to a mobile platform (Android/iOS). Please ensure all mobile-specific settings are configured. Be sure to enable mobile controllers in the RCCP_Settings (Tools --> BCG --> RCCP --> Edit Settings).\n\nIf you're using URP, you can have additional lights disabled and shadows disabled for better performance on older devices.\n\nMore info can be found in the documentation.",
            "OK"

        );

    }

    private static void ShowListInfo() {

        EditorUtility.DisplayDialog(

    "Realistic Car Controller Pro | Build Platform Changed",
    "You have switched the build platform. If you are having compiler errors related to RCCP after changing it, most likely scripting define symbol list (Edit --> Project Settings --> Player) in your project settings has old keys. This happens if you import an addon package, and delete after a while.\n\nBe sure to have proper keys in the list. Remove keys from the list if your project doesn't have that addon.\n\nMore info can be found in the documentation.",
    "OK"

);

    }

}
