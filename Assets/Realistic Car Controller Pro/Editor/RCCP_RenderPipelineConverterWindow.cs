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
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;

#if BCG_URP
using UnityEngine.Rendering.Universal;
#endif

#if BCG_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

public class RCCP_RenderPipelineConverterWindow : EditorWindow {

    private RenderPipelineAsset activePipeline;
    private string pipelineName = "Built-in";

    /// <summary>
    /// Enum for supported pipelines
    /// </summary>
    private enum Pipeline { BuiltIn, URP, HDRP }

    private Vector2 scrollPosition;

    public static void Init() {

        RCCP_RenderPipelineConverterWindow window = GetWindow<RCCP_RenderPipelineConverterWindow>("RCCP Pipeline Converter");
        window.Show();
        window.minSize = new Vector2(400, 920);

    }

    private void OnEnable() {

        activePipeline = GraphicsSettings.currentRenderPipeline;

        if (activePipeline == null) {

            pipelineName = "Built-in";

        } else if (activePipeline.GetType().ToString().Contains("Universal")) {

            pipelineName = "URP";

        } else if (activePipeline.GetType().ToString().Contains("HD")) {

            pipelineName = "HDRP";

        } else {

            pipelineName = "Unknown";

        }

    }

    private void OnGUI() {

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("RCCP Render Pipeline Converter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool assists in converting RCCP materials and lighting components for URP or HDRP.\n\n" +
            "Render Pipelines in Unity define how objects are drawn. Built-in is the default pipeline, " +
            "URP (Universal Render Pipeline) is optimized for performance, and HDRP (High Definition Render Pipeline) targets high-end visuals.",
            MessageType.Info
        );

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Detected Render Pipeline:", EditorStyles.label);
        EditorGUILayout.LabelField(pipelineName, EditorStyles.boldLabel);

        if (EditorApplication.isCompiling)
            EditorGUILayout.HelpBox("Scripts are compiling� please wait.", MessageType.Warning);

        EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

        GUILayout.Space(10);

        if (pipelineName == "Built-in") {

            EditorGUILayout.HelpBox("No conversion is needed. RCCP is fully compatible with the Built-in Render Pipeline.", MessageType.Info);

        } else if (pipelineName == "URP" || pipelineName == "HDRP") {

            EditorGUILayout.HelpBox(
                $"{pipelineName} detected.\n\n" +
                "In order to work properly in this pipeline, materials and lens flare components must be converted.\n\n" +
                "RCCP uses a few custom shaders, so please do step by step from No.1 to the last one.",
                MessageType.Warning
            );

            GUILayout.Space(10);
            EditorGUILayout.LabelField("1. Material Conversion", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Click the button below to automatically select all materials under the RCCP folder.\n" +
                "After selection, go to:\nEdit > Rendering > Materials > Convert Selected Built-in Materials",
                MessageType.None
            );

            if (GUILayout.Button("1. Select All RCCP Materials for Conversion")) {

                SelectRCCPMaterials();

            }

            GUILayout.Space(15);
            EditorGUILayout.LabelField("2. Lens Flare Conversion", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "In Built-in RP, Unity uses a legacy LensFlare component which does not work in URP/HDRP.\n" +
                "Click the button below to scan all RCCP vehicle prefabs and replace legacy LensFlares with SRP-compatible ones.",
                MessageType.None
            );

            if (GUILayout.Button("2. Convert RCCP Lens Flares to SRP"))
                ConvertLensFlaresToSRP();

            if (pipelineName == "URP" || pipelineName == "HDRP") {

                // 3. Shader import.
                GUILayout.Space(15);
                EditorGUILayout.LabelField("3. Custom Shader Import", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Imports the corresponding shaders into the project and removes the unnecessary ones.",
                    MessageType.None
                );

                // Buttons to switch pipelines
                if (pipelineName != "URP" && pipelineName != "HDRP") {

                    if (GUILayout.Button("3. Import Built-in Shaders"))
                        SwitchToPipeline(Pipeline.BuiltIn);

                }

                if (pipelineName == "URP") {

                    if (GUILayout.Button("3. Import URP Shaders"))
                        SwitchToPipeline(Pipeline.URP);

                }

                if (pipelineName == "HDRP") {

                    if (GUILayout.Button("3. Import HDRP Shaders"))
                        SwitchToPipeline(Pipeline.HDRP);

                }

                GUILayout.Space(15);
                EditorGUILayout.LabelField("4. Custom Shader Conversion", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Scan all RCCP prefabs and replace materials using the default RCCP_Shader_Body (and other base shaders)\n" +
                    "with their pipeline-specific variants (e.g. RCCP_Shader_Body_URP or RCCP_Shader_Body_HDRP).",
                    MessageType.None
                );

                if (GUILayout.Button("4. Convert RCCP Custom Shaders"))
                    ConvertCustomShaders(false);

                GUILayout.Space(15);
                EditorGUILayout.LabelField("5. Remove Old Shaders", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Removes all other unused shaders and their variants from the project",
                    MessageType.None
                );

                if (GUILayout.Button("5. Remove Old Shaders")) {

                    // Remove shaders for other pipelines first
                    if (pipelineName == "URP" || pipelineName == "HDRP") {
                        RemovePipelineContent(RCCP_DemoContent.Instance.builtinShadersContent);
                    }
                    if (pipelineName != "URP") {
                        RemovePipelineContent(RCCP_DemoContent.Instance.URPShadersContent);
                    }
                    if (pipelineName != "HDRP") {
                        RemovePipelineContent(RCCP_DemoContent.Instance.HDRPShadersContent);
                    }

                }

            }

#if BCG_URP

            if (pipelineName == "URP") {

                // 5. Camera URP components
                GUILayout.Space(15);
                EditorGUILayout.LabelField("- Camera Components", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Scan all demo scenes and update their cameras for URP,\n" +
                    "and add the URP components if missing.",
                    MessageType.None
                );

                if (GUILayout.Button("- Enable Post Processing on All RCCP Cameras"))
                    EnablePostProcessingOnCameras();

            }

#endif

            if (pipelineName == "HDRP") {

                // 5. HDRP Demo Scene Setup
                GUILayout.Space(15);
                EditorGUILayout.LabelField("5. HDRP Demo Scene Setup", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Scan all demo scenes and update their directional lights for HDRP,\n" +
                    "and add the HDRP Volume Profile prefab if missing.",
                    MessageType.None
                );

                if (GUILayout.Button("Convert Demo Scenes for HDRP"))
                    ConvertDemoScenesForHDRP();

            }

        } else {

            EditorGUILayout.HelpBox("Unsupported or unknown render pipeline detected. Please check your project settings.", MessageType.Error);

        }

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Need Help?", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "If you are unfamiliar with Render Pipelines or material conversion in Unity, please visit the official Unity documentation:\n\n" +
            "- URP Guide: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal\n" +
            "- HDRP Guide: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition",
            MessageType.None
        );

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();

    }

    public static void SelectRCCPMaterials() {

        List<string> materialGuids = new List<string>(AssetDatabase.FindAssets("t:Material", new[] { RCCP_AssetUtilities.BasePath }));

        if (Directory.Exists("Assets/BoneCracker Games Shared Assets"))
            materialGuids.AddRange(AssetDatabase.FindAssets("t:Material", new[] { "Assets/BoneCracker Games Shared Assets" }));

        List<Object> materials = new List<Object>();

        foreach (string guid in materialGuids) {

            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                materials.Add(mat);

        }

        Selection.objects = materials.ToArray();

        EditorUtility.DisplayDialog(
            "RCCP Material Selection",
            $"{materials.Count} material(s) found and selected.\n\nGo to:\nEdit > Rendering > Materials > Convert Selected Built-in Materials",
            "OK"
        );

    }

#if BCG_URP || BCG_HDRP
    public static void ConvertLensFlaresToSRP() {

        List<string> prefabGuids = new List<string>(AssetDatabase.FindAssets("t:Prefab", new[] { RCCP_AssetUtilities.BasePath }));

        if (Directory.Exists("Assets/BoneCracker Games Shared Assets"))
            prefabGuids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/BoneCracker Games Shared Assets" }));

        int convertedCount = 0;

        foreach (string guid in prefabGuids) {

            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            //if (prefab.GetComponentInChildren<RCCP_CarController>(true) == null)
            //    continue;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (instance == null)
                continue;

            Light[] lights = instance.GetComponentsInChildren<Light>(true);

            bool modified = false;

            foreach (Light light in lights) {

                LensFlare legacyFlare = light.GetComponent<LensFlare>();
                if (legacyFlare != null) {

                    DestroyImmediate(legacyFlare, true);
                    LensFlareComponentSRP lf = light.gameObject.AddComponent<LensFlareComponentSRP>();
                    lf.attenuationByLightShape = false;
                    lf.intensity = 0f;
                    lf.lensFlareData = RCCP_Settings.Instance.lensFlareData as LensFlareDataSRP;
                    modified = true;

                }

            }

            if (modified) {

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                convertedCount++;

            }

            DestroyImmediate(instance);

        }

        EditorUtility.DisplayDialog("RCCP Lens Flare Conversion", $"Conversion completed.\n{convertedCount} prefab(s) updated.", "OK");

    }
#else

    private static void ConvertLensFlaresToSRP() {



    }
#endif

#if BCG_URP
    public static void EnablePostProcessingOnCameras() {

        List<string> prefabGuids = new List<string>(AssetDatabase.FindAssets("t:Prefab", new[] { RCCP_AssetUtilities.BasePath }));

        if (Directory.Exists("Assets/BoneCracker Games Shared Assets"))
            prefabGuids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/BoneCracker Games Shared Assets" }));

        int modifiedCount = 0;

        foreach (string guid in prefabGuids) {

            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (instance == null)
                continue;

            Camera[] cameras = instance.GetComponentsInChildren<Camera>(true);
            bool modified = false;

            foreach (Camera cam in cameras) {

                if (!cam.allowHDR)
                    cam.allowHDR = true;

                if (!cam.allowMSAA)
                    cam.allowMSAA = true;

#if UNITY_2021_2_OR_NEWER
                // Enables the Post Processing checkbox for URP/HDRP cameras.
                if (!cam.allowDynamicResolution)
                    cam.allowDynamicResolution = true;

                if (!cam.renderingPath.Equals(RenderingPath.UsePlayerSettings))
                    cam.renderingPath = RenderingPath.UsePlayerSettings;
#endif

                if (!cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()) {

                    var additionalData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    additionalData.renderPostProcessing = true;
                    modified = true;

                } else {

                    var additionalData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    if (!additionalData.renderPostProcessing) {
                        additionalData.renderPostProcessing = true;
                        modified = true;
                    }

                }

            }

            if (modified) {

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                modifiedCount++;

            }

            DestroyImmediate(instance);

        }

        EditorUtility.DisplayDialog("RCCP Camera Post Processing", $"Completed.\n{modifiedCount} prefab(s) modified.", "OK");

    }

#endif

    /// <summary>
    /// Scans every RCCP prefab under Assets/Realistic Car Controller Pro,
    /// finds materials using base RCCP custom shaders, and swaps them
    /// for the pipeline-specific variant.
    /// </summary>
    public static void ConvertCustomShaders(bool bypassDefaultShaderCheck) {

        if (!bypassDefaultShaderCheck) {

            // 0. Import built-in shaders so we can locate the original RCCP_Shader_* names
            if (GraphicsSettings.currentRenderPipeline != null) {
                string builtinPackagePath = AssetDatabase.GetAssetPath(RCCP_AddonPackages.Instance.builtinShaders);
                if (!string.IsNullOrEmpty(builtinPackagePath)) {

                    // Subscribe before we import
                    AssetDatabase.importPackageCompleted += OnBuiltinShadersImported;

                    // This will show the import dialog � when the user clicks OK, our callback fires.
                    AssetDatabase.ImportPackage(builtinPackagePath, false);

                    Debug.Log("[RCCP] Imported Built-in shaders for conversion.");

                    return;

                }
            }

        }

        // 1. Gather all RCCP prefabs
        List<string> prefabGuids = new List<string>(AssetDatabase.FindAssets("t:Prefab", new[] { RCCP_AssetUtilities.BasePath }));

        if (Directory.Exists("Assets/BoneCracker Games Shared Assets"))
            prefabGuids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/BoneCracker Games Shared Assets" }));

        int updatedPrefabs = 0;
        string pipelineName = GetPipelineLabel();

        foreach (string guid in prefabGuids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponentInChildren<RCCP_CarController>(true) == null)
                continue;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                continue;

            bool prefabModified = false;

            // 2. Conversion loop
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers) {
                Material[] mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    Material mat = mats[i];
                    if (mat == null)
                        continue;

                    string currentName = mat.shader.name;
                    if (currentName.EndsWith("_URP") || currentName.EndsWith("_HDRP"))
                        continue;

                    if (currentName.StartsWith("RCCP_Shader_")) {
                        string targetShaderName = currentName + "_" + pipelineName;
                        Shader targetShader = Shader.Find(targetShaderName);
                        if (targetShader != null) {
                            mat.shader = targetShader;
                            prefabModified = true;
                        } else {
                            Debug.LogWarning($"[RCCP] Could not find shader '{targetShaderName}' to replace '{currentName}'.");
                        }
                    }
                }
            }

            // 3. Wheel blur material, same logic
            RCCP_WheelBlur wheelBlur = instance.GetComponentInChildren<RCCP_WheelBlur>(true);
            if (wheelBlur) {
                Material mat = wheelBlur.targetMaterial;
                if (mat != null) {
                    string currentName = mat.shader.name;
                    if (!currentName.EndsWith("_URP") && !currentName.EndsWith("_HDRP") &&
                         currentName.StartsWith("RCCP_Shader_")) {
                        string targetShaderName = currentName + "_" + pipelineName;
                        Shader targetShader = Shader.Find(targetShaderName);
                        if (targetShader != null) {
                            mat.shader = targetShader;
                            prefabModified = true;
                        } else {
                            Debug.LogWarning($"[RCCP] Could not find shader '{targetShaderName}' to replace '{currentName}'.");
                        }
                    }
                }
            }

#if BCG_URP || BCG_HDRP
            // 4. Decal and neon shaders
            DecalProjector[] projectors = instance.GetComponentsInChildren<DecalProjector>(true);
            bool decalProcessed = false;
            bool neonProcessed = false;

            if (projectors != null && projectors.Length > 0) {

                for (int i = 0; i < projectors.Length; i++) {

                    Material mat = projectors[i].material;

                    if (mat != null) {

                        if (projectors[i].transform.name.Contains("Decal") && !decalProcessed) {

                            string matPath = AssetDatabase.GetAssetPath(mat);
                            string folderPath = Path.GetDirectoryName(matPath);    // Directory path

                            // Convert to full system path
                            string systemPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), folderPath);

                            // Get all material files in that folder
                            string[] materialFiles = Directory.GetFiles(systemPath, "*.mat");

                            List<Material> relatives = new List<Material>();

                            foreach (string file in materialFiles) {

                                // Convert system path back to relative asset path
                                string assetRelativePath = "Assets" + file.Replace(Application.dataPath, "").Replace("\\", "/");

                                if (file.Contains("Decal_"))
                                    relatives.Add(AssetDatabase.LoadAssetAtPath<Material>(assetRelativePath));

                            }

                            foreach (Material item in relatives) {

                                string currentName = item.shader != null ? item.shader.name : "";

                                if (currentName.Contains("Hidden"))
                                    currentName = "";

                                if ((!currentName.EndsWith("_URP") && !currentName.EndsWith("_HDRP")) || currentName == "") {

                                    if (currentName == "")
                                        currentName = "RCCP_Shader_Decal";

                                    string targetShaderName = "Shader Graphs/" + currentName + "_" + pipelineName;
                                    Shader targetShader = Shader.Find(targetShaderName);

                                    if (targetShader != null) {
                                        item.shader = targetShader;
                                        prefabModified = true;
                                    } else {
                                        Debug.LogWarning($"[RCCP] Could not find shader '{targetShaderName}' to replace '{currentName}'.");
                                    }
                                }

                            }

                            decalProcessed = true;

                        }

                        if (projectors[i].transform.name.Contains("Neon") && !neonProcessed) {

                            string matPath = AssetDatabase.GetAssetPath(mat);
                            string folderPath = Path.GetDirectoryName(matPath);    // Directory path

                            // Convert to full system path
                            string systemPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), folderPath);

                            // Get all material files in that folder
                            string[] materialFiles = Directory.GetFiles(systemPath, "*.mat");

                            List<Material> relatives = new List<Material>();

                            foreach (string file in materialFiles) {

                                // Convert system path back to relative asset path
                                string assetRelativePath = "Assets" + file.Replace(Application.dataPath, "").Replace("\\", "/");

                                if (file.Contains("Neon_"))
                                    relatives.Add(AssetDatabase.LoadAssetAtPath<Material>(assetRelativePath));

                            }

                            foreach (Material item in relatives) {

                                string currentName = item.shader != null ? item.shader.name : "";

                                if (currentName.Contains("Hidden"))
                                    currentName = "";

                                if ((!currentName.EndsWith("_URP") && !currentName.EndsWith("_HDRP")) || currentName == "") {

                                    if (currentName == "")
                                        currentName = "RCCP_Shader_Neon";

                                    string targetShaderName = "Shader Graphs/" + currentName + "_" + pipelineName;
                                    Shader targetShader = Shader.Find(targetShaderName);

                                    if (targetShader != null) {
                                        item.shader = targetShader;
                                        prefabModified = true;
                                    } else {
                                        Debug.LogWarning($"[RCCP] Could not find shader '{targetShaderName}' to replace '{currentName}'.");
                                    }
                                }

                            }

                            neonProcessed = true;

                        }

                    }

                }

            }
#endif

            // 5. Save back to prefab if modified
            if (prefabModified) {
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                updatedPrefabs++;
            }

            DestroyImmediate(instance);
        }

        // 5. Remove built-in shaders if we�re on URP/HDRP
        if (GraphicsSettings.currentRenderPipeline != null) {
            var builtinContent = RCCP_DemoContent.Instance.builtinShadersContent;
            if (builtinContent != null) {
                string contentPath = AssetDatabase.GetAssetPath(builtinContent);
                if (!string.IsNullOrEmpty(contentPath)) {
                    if (AssetDatabase.DeleteAsset(contentPath)) {
                        Debug.Log("[RCCP] Removed Built-in shaders after conversion.");
                    } else {
                        Debug.LogWarning("[RCCP] Failed to remove built-in shader content at " + contentPath);
                    }
                }
            }
        }

        // 6. Final dialog
        EditorUtility.DisplayDialog(
            "RCCP Custom Shader Conversion",
            $"Completed! {updatedPrefabs} prefab(s) were updated to use the {GetPipelineLabel()} custom shaders.",
            "OK"
        );
    }


#if BCG_HDRP

    /// <summary>
    /// Scans every demo scene in the Demo Scenes folder,
    /// updates directional lights with HDRP components,
    /// and ensures a Volume Profile is present.
    /// </summary>
    public static void ConvertDemoScenesForHDRP() {

        // Get the HDRP Volume Profile prefab from RCCP_Settings
        var settings = RCCP_Settings.Instance;
        var volumeProfilePrefab = settings.hdrpVolumeProfilePrefab;
        if (volumeProfilePrefab == null) {
            Debug.LogError("[RCCP] HDRP Volume Profile Prefab is not assigned in RCCP_Settings.");
            return;
        }

        List<string> sceneGuids = new List<string>(AssetDatabase.FindAssets("t:SceneAsset", new[] { RCCP_AssetUtilities.BasePath }));

        if (Directory.Exists("Assets/BoneCracker Games Shared Assets"))
            sceneGuids.AddRange(AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets/BoneCracker Games Shared Assets" }));

        int processed = 0;

        string lastScene = UnityEditor.SceneManagement.EditorSceneManager
        .GetActiveScene().path;

        UnityEditor.SceneManagement.EditorSceneManager
                .NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        foreach (string guid in sceneGuids) {

            // Resolve path and open scene additively
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            var scene = UnityEditor.SceneManagement.EditorSceneManager
                .OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            bool sceneModified = false;
            bool oldDirectionalLightFound = false;
            Quaternion oldDirectionalLightRotation = Quaternion.Euler(50f, -30f, 0f);

            // --- REMOVE OLD DIRECTIONAL LIGHTS ---
            // Find every Light of type Directional
            var oldLights = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Light>(true))
                .Where(light => light.type == LightType.Directional && light.transform.name.Contains("Directional"))
                .ToArray();

            foreach (Light oldLight in oldLights) {
                oldDirectionalLightFound = true;
                oldDirectionalLightRotation = oldLight.transform.rotation;
                // destroy the entire GameObject so we do not leave orphan components
                GameObject.DestroyImmediate(oldLight.gameObject);
                sceneModified = true;
            }

            if (oldDirectionalLightFound) {

                // --- CREATE NEW DIRECTIONAL LIGHT ---
                GameObject newDirLightGO = new GameObject("Sun");
                Light newLight = newDirLightGO.AddComponent<Light>();
                newLight.type = LightType.Directional;
                newLight.shadows = LightShadows.Soft;
                newLight.shadowResolution = LightShadowResolution.High;
                // orient the light at a nice default angle
                newDirLightGO.transform.rotation = oldDirectionalLightRotation;

                // move into the demo scene
                EditorSceneManager.MoveGameObjectToScene(newDirLightGO, scene);
                sceneModified = true;

            }

            // 1) Update all Directional Lights
#if UNITY_2022_1_OR_NEWER
            Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Light[] lights = GameObject.FindObjectsOfType<Light>(true);
#endif

            foreach (Light light in lights) {

                if (light.GetComponentInParent<RCCP_CarController>(true) != null)
                    continue;

                if (light.type != LightType.Directional) {
                    // In HDRP, lights require HDAdditionalLightData
#if UNITY_2021_2_OR_NEWER
                    if (light.GetComponent<global::UnityEngine.Rendering.HighDefinition
                        .HDAdditionalLightData>() == null) {
                        light.gameObject.AddComponent<global::UnityEngine.Rendering.HighDefinition
                            .HDAdditionalLightData>();
                        sceneModified = true;
                    }
#endif
                }
            }

            // 2) Ensure a Volume Profile exists
#if !UNITY_2022_1_OR_NEWER
            var volumes = GameObject.FindObjectsOfType<UnityEngine.Rendering.Volume>(true);
#else
            var volumes = GameObject.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif

            if (volumes.Length == 0) {
                // Instantiate the prefab and move it into this scene
                GameObject volumeGO = PrefabUtility
                    .InstantiatePrefab(volumeProfilePrefab) as GameObject;
                UnityEditor.SceneManagement.EditorSceneManager
                    .MoveGameObjectToScene(volumeGO, scene);
                sceneModified = true;
            }

            // 3) Save changes if needed
            if (sceneModified) {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                processed++;
            }

            // Close the additive scene before moving on
            if (EditorSceneManager.sceneCount > 1) {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        EditorUtility.DisplayDialog(
            "RCCP HDRP Scene Conversion",
            $"Completed HDRP setup on {processed} demo scene(s).",
            "OK"
        );

        if (lastScene.Length > 0)
            EditorSceneManager.OpenScene(lastScene, OpenSceneMode.Single);

    }

#else

    private static void ConvertDemoScenesForHDRP() {



    }

#endif

    /// <summary>
    /// Helper to get a friendly pipeline label for dialogs.
    /// </summary>
    private static string GetPipelineLabel() {

        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null) return "Built-in";
        var type = pipeline.GetType().ToString();
        if (type.Contains("Universal")) return "URP";
        if (type.Contains("HD")) return "HDRP";
        return "Unknown";
    }

    /// <summary>
    /// Main method: removes unused shaders and imports the selected pipeline package
    /// </summary>
    private static void SwitchToPipeline(Pipeline pipelineType) {

        // Import selected pipeline package
        switch (pipelineType) {
            case Pipeline.BuiltIn:
                ImportPackage(RCCP_AddonPackages.Instance.builtinShaders);
                break;
            case Pipeline.HDRP:

                string hdrp = RCCP_PipelineVersion.GetHDRPVersion();

                if (!string.IsNullOrEmpty(hdrp))
                    Debug.Log($"HDRP version detected: { hdrp }");

                // Subscribe before we import
                AssetDatabase.importPackageCompleted += OnHDRPShadersImported;

                // Example branch: require URP 17+
                if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.high-definition", 17))
                    ImportPackage(RCCP_AddonPackages.Instance.HDRPShaders_6);
                else if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.high-definition", 16))
                    ImportPackage(RCCP_AddonPackages.Instance.HDRPShaders_2023);
                else if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.high-definition", 15))
                    ImportPackage(RCCP_AddonPackages.Instance.HDRPShaders_2023);
                else if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.high-definition", 14))
                    ImportPackage(RCCP_AddonPackages.Instance.HDRPShaders_2022);
                else {

                    EditorUtility.DisplayDialog("Custom HDRP Shaders", "Custom HDRP shaders are not available for before Unity 2022 versions", "OK");
                    AssetDatabase.importPackageCompleted -= OnHDRPShadersImported;

                }

                break;
            case Pipeline.URP:

                string urp = RCCP_PipelineVersion.GetURPVersion();

                if (!string.IsNullOrEmpty(urp))
                    Debug.Log($"URP version detected: { urp }");

                // Example branch: require URP 17+
                if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.universal", 17))
                    ImportPackage(RCCP_AddonPackages.Instance.URPShaders_6);
                else if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.universal", 16))
                    ImportPackage(RCCP_AddonPackages.Instance.URPShaders_2023);
                else if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.universal", 15))
                    ImportPackage(RCCP_AddonPackages.Instance.URPShaders_2023);
                else if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.universal", 14))
                    ImportPackage(RCCP_AddonPackages.Instance.URPShaders_2022);
                else if (RCCP_PipelineVersion.HasAtLeast("com.unity.render-pipelines.universal", 13))
                    ImportPackage(RCCP_AddonPackages.Instance.URPShaders_2021);
                else
                    EditorUtility.DisplayDialog("Custom URP Shaders", "Custom URP shaders are not available for before Unity 2021 versions", "OK");

                break;
        }
    }

    /// <summary>
    /// Deletes all assets at the path of the given content object
    /// </summary>
    private static void RemovePipelineContent(Object contentObject) {
        if (contentObject == null) {
            Debug.LogWarning("Content object is null, skipping removal.");
            return;
        }

        var path = AssetDatabase.GetAssetPath(contentObject);

        if (string.IsNullOrEmpty(path)) {
            Debug.LogWarning("Could not find asset path for content object: " + contentObject.name);
            return;
        }

        // If folder, delete folder, else delete asset file
        if (AssetDatabase.IsValidFolder(path)) {
            if (AssetDatabase.DeleteAsset(path)) {
                Debug.Log("Deleted folder at " + path);
            } else {
                Debug.LogError("Failed to delete folder at " + path);
            }
        } else {
            if (AssetDatabase.DeleteAsset(path)) {
                Debug.Log("Deleted asset at " + path);
            } else {
                Debug.LogError("Failed to delete asset at " + path);
            }
        }
    }

    /// <summary>
    /// Called when any package import completes.
    /// </summary>
    private static void OnBuiltinShadersImported(string packageName) {

        // Unsubscribe immediately so we only handle our own package once
        AssetDatabase.importPackageCompleted -= OnBuiltinShadersImported;

        // 1) Gather and convert all RCCP prefabs here (same code you already have)�
        ConvertCustomShaders(true);

        // 2) Clean up built-in shaders if needed
        RemovePipelineContent(RCCP_DemoContent.Instance.builtinShadersContent);

        // 3) Let user know you�re done
        EditorUtility.DisplayDialog(
            "RCCP Shader Conversion",
            "Built-in shaders were imported, prefabs updated, and built-in shaders removed.",
            "OK"
        );

    }

    /// <summary>
    /// Called when any package import completes.
    /// </summary>
    private static void OnHDRPShadersImported(string packageName) {

        // Unsubscribe immediately so we only handle our own package once
        AssetDatabase.importPackageCompleted -= OnHDRPShadersImported;

        ImportPackage(RCCP_AddonPackages.Instance.HDRPVolumeProfile);

    }

    /// <summary>
    /// Imports a .unitypackage from the path of the given package object
    /// </summary>
    private static void ImportPackage(Object packageObject) {
        if (packageObject == null) {
            Debug.LogError("Package object is null, cannot import.");
            return;
        }

        var packagePath = AssetDatabase.GetAssetPath(packageObject);

        if (string.IsNullOrEmpty(packagePath)) {
            Debug.LogError("Could not find package path for object: " + packageObject.name);
            return;
        }

        AssetDatabase.ImportPackage(packagePath, true);
        Debug.Log("Imported package: " + packagePath);
    }

    /// <summary>
    /// Quick helpers for querying installed render-pipeline package versions.
    /// Works only in the Editor; returns an empty string in a built player.
    /// </summary>
    public static class RCCP_PipelineVersion {

        /// <summary>
        /// Returns the full version of Universal Render Pipeline
        /// (e.g. "17.0.3"), or <c>string.Empty</c> if URP is not installed.
        /// </summary>
        public static string GetURPVersion() {

            return GetPackageVersion("com.unity.render-pipelines.universal");

        }

        /// <summary>
        /// Returns the full version of High Definition Render Pipeline
        /// (e.g. "17.0.3"), or <c>string.Empty</c> if HDRP is not installed.
        /// </summary>
        public static string GetHDRPVersion() {

            return GetPackageVersion("com.unity.render-pipelines.high-definition");

        }

        /// <summary>
        /// Looks up a package by its name and returns its <see cref="PackageInfo.version"/>.
        /// </summary>
        private static string GetPackageVersion(string packageName) {

#if UNITY_EDITOR

            // This is a very fast, synchronous lookup that does not allocate
            // a full Client.List() request.
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{packageName}");

            if (packageInfo != null)
                return packageInfo.version;

#endif
            return string.Empty;

        }

        /// <summary>
        /// Returns <c>true</c> if the given pipeline is installed and its
        /// <see cref="PackageInfo.major"/> is at least <paramref name="minimumMajor"/>.
        /// Handy when you need to branch on URP 17 vs URP 15, etc.
        /// </summary>
        public static bool HasAtLeast(string packageName, int minimumMajor) {

            string ver = GetPackageVersion(packageName);

            if (string.IsNullOrEmpty(ver))
                return false;

            // Package versions use �major.minor.patch�, so split on �.�
            if (int.TryParse(ver.Split('.')[0], out int major))
                return major >= minimumMajor;

            return false;

        }

    }

}
