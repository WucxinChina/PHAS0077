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
using UnityEngine.Rendering;

public class RCC_InitLoad : EditorWindow {

    [InitializeOnLoadMethod]
    static void InitOnLoad() {

        EditorApplication.delayCall += EditorUpdate;

    }

    public static void EditorUpdate() {

        bool checkInitialLayersOnPrefabs = false;

#if !BCG_RCC

        // Enable BCG_RCC scripting define symbol
        RCC_SetScriptingSymbol.SetEnabled("BCG_RCC", true);

        checkInitialLayersOnPrefabs = true;

        // Show a single informative dialog
        EditorUtility.DisplayDialog(
            "Realistic Car Controller | Setup Instructions",
            "Thank you for purchasing and using Realistic Car Controller. Please read the documentation before use. Also check out the online documentation for updated info.\n\n"
            + "Important: RCC uses the new Input System. The legacy input system is deprecated. Make sure your project has the Input System package installed via Package Manager.",
            "Got it!"
        );

        // Open welcome window
        RCC_WelcomeWindow.OpenWindow();

#endif

        // Run installation checks
        EditorApplication.delayCall += RCC_Installation.Check;

        if (checkInitialLayersOnPrefabs)
            EditorApplication.delayCall += RCC_Installation.CheckAllLayers;

        CheckRP();

    }

    public static void CheckRP() {

        RenderPipelineAsset activePipeline;

        activePipeline = GraphicsSettings.currentRenderPipeline;

        if (activePipeline == null) {

            RCC_SetScriptingSymbol.SetEnabled("BCG_URP", false);
            RCC_SetScriptingSymbol.SetEnabled("BCG_HDRP", false);

        } else if (activePipeline.GetType().ToString().Contains("Universal")) {

#if !BCG_URP
            RCC_RenderPipelineConverterWindow.ShowWindow();
            RCC_SetScriptingSymbol.SetEnabled("BCG_URP", true);
            RCC_SetScriptingSymbol.SetEnabled("BCG_HDRP", false);
#endif

        } else if (activePipeline.GetType().ToString().Contains("HD")) {

#if !BCG_HDRP
            RCC_RenderPipelineConverterWindow.ShowWindow();
            RCC_SetScriptingSymbol.SetEnabled("BCG_HDRP", true);
            RCC_SetScriptingSymbol.SetEnabled("BCG_URP", false);
#endif

        } else {

            RCC_SetScriptingSymbol.SetEnabled("BCG_URP", false);
            RCC_SetScriptingSymbol.SetEnabled("BCG_HDRP", false);

        }

    }

#if BCG_HDRP

    [InitializeOnLoadMethod]
    private static void LoadHDRPPrefab() {

        if (RCC_Settings.Instance.hdrpVolumeProfilePrefab != null)
            return;

        // Construct the full path based on RCC base path
        string relativePath = "HDRP/RCC_VolumeProfile_HDRP.prefab";
        string fullPath = RCC_AssetUtilities.BasePath + relativePath;

        if (AssetExistsAtPath<GameObject>(fullPath)) {

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            RCC_Settings.Instance.hdrpVolumeProfilePrefab = prefab;
            EditorUtility.SetDirty(RCC_Settings.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

    }

    private static bool AssetExistsAtPath<T>(string path) where T : UnityEngine.Object {

        return AssetDatabase.LoadAssetAtPath<T>(path) != null;

    }

#endif

}
