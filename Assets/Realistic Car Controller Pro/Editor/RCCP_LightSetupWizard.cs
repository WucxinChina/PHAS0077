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
using UnityEngine.Rendering;
using UnityEditorInternal;
using System.Reflection;  // for accessing private fields via reflection


/// <summary>
/// Quick wizard that guides the developer through adding and placing vehicle lights
/// (head-/tail-/brake/indicator/reverse) in just a few clicks.
/// </summary>
public class RCCP_LightSetupWizard : EditorWindow {

    /// <summary>Currently selected root of the vehicle.</summary>
    public GameObject selectedVehicle;

    //───────────────────────────────────────────────────────────────────────//
    #region Wizard Data
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Data container that holds the user’s light selections.</summary>
    [System.Serializable]
    public class LightSetupData {

        public bool addLowBeams = true;
        public bool addHighBeams = false;
        public bool addBrakeLights = true;
        public bool addTailLights = false;
        public bool addReverseLights = true;
        public bool addIndicators = true;

        public float defaultIntensityForHeadlights = 2.5f;
        public float defaultIntensityForBrakeLights = 1f;
        public float defaultIntensityForReverseLights = 1f;
        public float defaultIntensityForIndicatorLights = 1f;

        public Color headlightColor = new Color(1f, 1f, .9f, 1f);
        public Color brakelightColor = new Color(1f, .1f, .05f, 1f);
        public Color taillightColor = new Color(1f, .05f, .05f, 1f);
        public Color reverselightColor = new Color(.9f, 1f, 1f, 1f);
        public Color indicatorColor = new Color(1f, .5f, 0f, 1f);

        public bool useLensFlares = true;

        public Object lensFlareSRP;
        public Flare flare;

        /// <summary>Offset distances used when auto-placing lights from the vehicle bounds.
        /// Offset directions are always calculated in the vehicle’s local space so they now work
        /// no matter how the root transform is rotated in the scene.
        /// </summary>
        public float forwardOffset = .05f;
        public float sideOffset = -.45f;
        public float heightOffset = -.25f;

        public bool showPlacementHandles = true;

    }

    private readonly LightSetupData setupData = new LightSetupData();

    /// <summary>Calculation cache – renderer bounds for the selected vehicle in LOCAL space.</summary>
    private Bounds cachedVehicleBounds;

    private bool closing = false;

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Menu Item / Lifecycle
    //───────────────────────────────────────────────────────────────────────//

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Light Setup Wizard", false, -80)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Light Setup Wizard", false, -80)]
    public static void ShowWindow() {

        RCCP_LightSetupWizard window = GetWindow<RCCP_LightSetupWizard>("RCCP Light Wizard");
        window.minSize = new Vector2(420f, 740f);

    }

    private void OnEnable() {

        AttemptCacheSelection();
        CacheVehicleBounds();

        SceneView.duringSceneGui += OnSceneGUI;

#if BCG_URP || BCG_HDRP

        if (RCCP_Settings.Instance.lensFlareData)
            setupData.lensFlareSRP = RCCP_Settings.Instance.lensFlareData;

#else

        if (RCCP_Settings.Instance.flare)
            setupData.flare = RCCP_Settings.Instance.flare;

#endif

    }

    private void OnDisable() {

        SceneView.duringSceneGui -= OnSceneGUI;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region GUI
    //───────────────────────────────────────────────────────────────────────//

    private void OnGUI() {

        AttemptCacheSelection();
        DrawHeader();
        DrawVehicleField();
        DrawOptions();
        DrawPlacementPreviewToggle();
        DrawFooter();
        Repaint();
        SceneView.RepaintAll();

    }

    private void DrawHeader() {

        GUILayout.Space(5);
        GUILayout.Label("RCCP Quick Light Setup", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("This wizard adds common vehicle lights and auto positions them at reasonable locations. You can further fine tune placement by dragging the handles in the Scene view before pressing <Create Lights>.", MessageType.Info);
        GUILayout.Space(5);

    }

    private void DrawVehicleField() {

        selectedVehicle = (GameObject)EditorGUILayout.ObjectField("Vehicle Root", selectedVehicle, typeof(GameObject), true);

        if (!selectedVehicle) {
            EditorGUILayout.HelpBox("Please select the root GameObject of your vehicle.", MessageType.Warning);
            return;
        }

    }

    private void DrawOptions() {

        GUILayout.Space(10);
        GUILayout.Label("Light Types", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        setupData.addLowBeams = EditorGUILayout.ToggleLeft("Low Beam Headlights", setupData.addLowBeams);
        setupData.addHighBeams = EditorGUILayout.ToggleLeft("High Beam Headlights", setupData.addHighBeams);
        setupData.addBrakeLights = EditorGUILayout.ToggleLeft("Brake Lights", setupData.addBrakeLights);
        setupData.addTailLights = EditorGUILayout.ToggleLeft("Tail Lights", setupData.addTailLights);
        setupData.addReverseLights = EditorGUILayout.ToggleLeft("Reverse Lights", setupData.addReverseLights);
        setupData.addIndicators = EditorGUILayout.ToggleLeft("Indicators (Left / Right)", setupData.addIndicators);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        GUILayout.Label("Intensity Settings", EditorStyles.boldLabel);

        setupData.defaultIntensityForHeadlights = EditorGUILayout.Slider("Default Intensity For Head Lights", setupData.defaultIntensityForHeadlights, .5f, 8f);
        setupData.defaultIntensityForBrakeLights = EditorGUILayout.Slider("Default Intensity For Brake Lights", setupData.defaultIntensityForBrakeLights, .5f, 8f);
        setupData.defaultIntensityForReverseLights = EditorGUILayout.Slider("Default Intensity For Reverse Lights", setupData.defaultIntensityForReverseLights, .5f, 8f);
        setupData.defaultIntensityForIndicatorLights = EditorGUILayout.Slider("Default Intensity For Indicator Lights", setupData.defaultIntensityForIndicatorLights, .5f, 8f);

        GUILayout.Space(10);
        GUILayout.Label("Color Settings", EditorStyles.boldLabel);

        setupData.headlightColor = EditorGUILayout.ColorField("Color For Head Lights", setupData.headlightColor);
        setupData.brakelightColor = EditorGUILayout.ColorField("Color For Brake Lights", setupData.brakelightColor);
        setupData.reverselightColor = EditorGUILayout.ColorField("Color For Reverse Lights", setupData.reverselightColor);
        setupData.indicatorColor = EditorGUILayout.ColorField("Color For Indicator Lights", setupData.indicatorColor);

        GUILayout.Space(10);
        GUILayout.Label("Flare Settings", EditorStyles.boldLabel);

        setupData.useLensFlares = EditorGUILayout.ToggleLeft("Add Lens Flares", setupData.useLensFlares);

#if !BCG_URP && !BCG_HDRP
        setupData.flare = (Flare)EditorGUILayout.ObjectField("Lensflare Data", setupData.flare, typeof(Flare), false);
#elif BCG_URP || BCG_HDRP
        setupData.lensFlareSRP = (LensFlareDataSRP)EditorGUILayout.ObjectField("Lensflare Data", setupData.lensFlareSRP as LensFlareDataSRP, typeof(LensFlareDataSRP), false);
#endif

        GUILayout.Space(5);
        setupData.forwardOffset = EditorGUILayout.Slider("Forward Offset", setupData.forwardOffset, -1f, 1f);
        setupData.sideOffset = EditorGUILayout.Slider("Side Offset", setupData.sideOffset, -1f, 1f);
        setupData.heightOffset = EditorGUILayout.Slider("Height Offset", setupData.heightOffset, -3, 3f);

    }

    private void DrawPlacementPreviewToggle() {

        GUILayout.Space(10);
        setupData.showPlacementHandles = EditorGUILayout.ToggleLeft("Show Placement Handles In Scene", setupData.showPlacementHandles);

    }

    private void DrawFooter() {

        GUILayout.Space(15);
        GUI.enabled = selectedVehicle;
        if (GUILayout.Button("Create Lights", GUILayout.Height(34)))
            CreateLights();
        GUI.enabled = true;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Scene Preview Handles
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>
    /// Draws preview handles for each light type, using the matching color from setupData.
    /// </summary>
    private void OnSceneGUI(SceneView sceneView) {

        if (!selectedVehicle || !setupData.showPlacementHandles)
            return;

        // ensure bounds are up to date
        CacheVehicleBounds();

        Vector3 ext = cachedVehicleBounds.extents;
        float sx = setupData.sideOffset;
        float fx = setupData.forwardOffset;
        float hx = setupData.heightOffset;
        Vector3 forward = selectedVehicle.transform.forward;

        //  HEADLIGHT PREVIEWS (low & high)
        if (setupData.addLowBeams) {
            Handles.color = setupData.headlightColor;
            Vector3 localL = new Vector3(-(ext.x + sx), hx + ext.y * .5f, ext.z + fx);
            Vector3 localR = new Vector3((ext.x + sx), hx + ext.y * .5f, ext.z + fx);
            Handles.DrawWireDisc(LocalToWorld(localL), forward, .085f);
            Handles.DrawWireDisc(LocalToWorld(localR), forward, .085f);
        }

        if (setupData.addHighBeams) {
            Handles.color = setupData.headlightColor;
            Vector3 localL = new Vector3(-(ext.x * .6f + sx), hx + ext.y * .6f, ext.z + fx);
            Vector3 localR = new Vector3((ext.x * .6f + sx), hx + ext.y * .6f, ext.z + fx);
            Handles.DrawWireDisc(LocalToWorld(localL), forward, .085f);
            Handles.DrawWireDisc(LocalToWorld(localR), forward, .085f);
        }

        //  BRAKE LIGHT PREVIEWS
        if (setupData.addBrakeLights) {
            Handles.color = setupData.brakelightColor;
            Vector3 localL = new Vector3(-(ext.x + sx), hx + ext.y * .5f, -ext.z - fx);
            Vector3 localR = new Vector3((ext.x + sx), hx + ext.y * .5f, -ext.z - fx);
            Handles.DrawWireDisc(LocalToWorld(localL), forward, .085f);
            Handles.DrawWireDisc(LocalToWorld(localR), forward, .085f);
        }

        //  TAIL LIGHT PREVIEWS
        if (setupData.addTailLights) {
            Handles.color = setupData.taillightColor;
            Vector3 localL = new Vector3(-(ext.x * .8f + sx), hx + ext.y * .45f, -ext.z - fx);
            Vector3 localR = new Vector3((ext.x * .8f + sx), hx + ext.y * .45f, -ext.z - fx);
            Handles.DrawWireDisc(LocalToWorld(localL), forward, .085f);
            Handles.DrawWireDisc(LocalToWorld(localR), forward, .085f);
        }

        //  REVERSE LIGHT PREVIEWS
        if (setupData.addReverseLights) {
            Handles.color = setupData.reverselightColor;
            Vector3 localL = new Vector3(-(ext.x * .6f + sx), hx + ext.y * .3f, -ext.z - fx);
            Vector3 localR = new Vector3((ext.x * .6f + sx), hx + ext.y * .3f, -ext.z - fx);
            Handles.DrawWireDisc(LocalToWorld(localL), forward, .085f);
            Handles.DrawWireDisc(LocalToWorld(localR), forward, .085f);
        }

        //  INDICATOR PREVIEWS
        if (setupData.addIndicators) {
            Handles.color = setupData.indicatorColor;
            float y = hx + ext.y * .4f;

            Vector3[] locals = new Vector3[] {
            new Vector3(-(ext.x + sx), y, ext.z + fx),
            new Vector3((ext.x + sx), y, ext.z + fx),
            new Vector3(-(ext.x + sx), y, -ext.z - fx),
            new Vector3((ext.x + sx), y, -ext.z - fx)
        };

            foreach (Vector3 local in locals) {
                Handles.DrawWireDisc(LocalToWorld(local), forward, .085f);
            }
        }

        if (closing)
            Close();

    }


    /// <summary>
    /// Returns preview world positions of each light according to the current settings.
    /// </summary>
    private IEnumerable<Vector3> GetPreviewPositions() {

        CacheVehicleBounds();

        Vector3 ext = cachedVehicleBounds.extents;
        float sx = setupData.sideOffset;
        float fx = setupData.forwardOffset;
        float hx = setupData.heightOffset;

        //  Head- & tail- positions.
        if (setupData.addLowBeams || setupData.addHighBeams) {
            yield return LocalToWorld(new Vector3(-(ext.x + sx), hx + ext.y * .5f, ext.z + fx));
            yield return LocalToWorld(new Vector3((ext.x + sx), hx + ext.y * .5f, ext.z + fx));
        }

        if (setupData.addBrakeLights || setupData.addTailLights) {
            yield return LocalToWorld(new Vector3(-(ext.x + sx), hx + ext.y * .5f, -ext.z - fx));
            yield return LocalToWorld(new Vector3((ext.x + sx), hx + ext.y * .5f, -ext.z - fx));
        }

        if (setupData.addIndicators) {
            float y = hx + ext.y * .4f;
            yield return LocalToWorld(new Vector3(-(ext.x + sx), y, ext.z + fx));
            yield return LocalToWorld(new Vector3((ext.x + sx), y, ext.z + fx));
            yield return LocalToWorld(new Vector3(-(ext.x + sx), y, -ext.z - fx));
            yield return LocalToWorld(new Vector3((ext.x + sx), y, -ext.z - fx));
        }

        if (setupData.addReverseLights) {
            float y = hx + ext.y * .3f;
            yield return LocalToWorld(new Vector3(-(ext.x * .6f + sx), y, -ext.z - fx));
            yield return LocalToWorld(new Vector3((ext.x * .6f + sx), y, -ext.z - fx));
        }

    }

    private Vector3 LocalToWorld(Vector3 localPos) {

        return selectedVehicle.transform.TransformPoint(localPos);

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Creation
    //───────────────────────────────────────────────────────────────────────//

    private void CreateLights() {

        if (!selectedVehicle)
            return;

        Undo.RegisterCompleteObjectUndo(selectedVehicle, "Add Vehicle Lights");

        //  Ensure a RCCP_Lights manager exists.
        RCCP_Lights manager = selectedVehicle.GetComponentInChildren<RCCP_Lights>();
        if (!manager) {
            manager = selectedVehicle.AddComponent<RCCP_Lights>();
        }
        manager.GetAllLights();

        List<GameObject> newLights = new List<GameObject>();
        Vector3 ext = cachedVehicleBounds.extents;
        float sx = setupData.sideOffset;
        float fx = setupData.forwardOffset;
        float hx = setupData.heightOffset;

        //  Helper local function to create a light.
        GameObject SpawnLight(string name, Vector3 localPos, RCCP_Light.LightType type, Color color) {

            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Light");
            go.transform.SetParent(manager.transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color;

            if (type == RCCP_Light.LightType.Headlight_LowBeam || type == RCCP_Light.LightType.Headlight_HighBeam)
                l.intensity = setupData.defaultIntensityForHeadlights;

            if (type == RCCP_Light.LightType.Brakelight)
                l.intensity = setupData.defaultIntensityForBrakeLights;

            if (type == RCCP_Light.LightType.Reverselight)
                l.intensity = setupData.defaultIntensityForReverseLights;

            if (type == RCCP_Light.LightType.IndicatorLeftLight || type == RCCP_Light.LightType.IndicatorRightLight)
                l.intensity = setupData.defaultIntensityForIndicatorLights;

            l.range = 30f;
            l.spotAngle = 90f;
            l.renderMode = LightRenderMode.ForcePixel;
            go.AddComponent<RCCP_Light>().lightType = type;

            if (setupData.useLensFlares) {

#if !BCG_URP && !BCG_HDRP

                LensFlare flareComp = go.AddComponent<LensFlare>();
                flareComp.flare = setupData.flare;
                flareComp.brightness = 0f;

#else

                LensFlareComponentSRP flareComp = go.AddComponent<LensFlareComponentSRP>();
                flareComp.lensFlareData = setupData.lensFlareSRP as LensFlareDataSRP;
                flareComp.attenuationByLightShape = false;
                flareComp.intensity = 0f;

#endif

            }

            newLights.Add(go);
            return go;

        }

        //  HEADLIGHTS
        if (setupData.addLowBeams) {
            SpawnLight("RCCP_LowBeam_L", new Vector3(-(ext.x + sx), hx + ext.y * .5f, ext.z + fx), RCCP_Light.LightType.Headlight_LowBeam, setupData.headlightColor);
            SpawnLight("RCCP_LowBeam_R", new Vector3((ext.x + sx), hx + ext.y * .5f, ext.z + fx), RCCP_Light.LightType.Headlight_LowBeam, setupData.headlightColor);
        }
        if (setupData.addHighBeams) {
            SpawnLight("RCCP_HighBeam_L", new Vector3(-(ext.x * .6f + sx), hx + ext.y * .6f, ext.z + fx), RCCP_Light.LightType.Headlight_HighBeam, setupData.headlightColor);
            SpawnLight("RCCP_HighBeam_R", new Vector3((ext.x * .6f + sx), hx + ext.y * .6f, ext.z + fx), RCCP_Light.LightType.Headlight_HighBeam, setupData.headlightColor);
        }

        //  BRAKE & TAIL LIGHTS
        if (setupData.addBrakeLights) {
            SpawnLight("RCCP_Brake_L", new Vector3(-(ext.x + sx), hx + ext.y * .5f, -ext.z - fx), RCCP_Light.LightType.Brakelight, setupData.brakelightColor);
            SpawnLight("RCCP_Brake_R", new Vector3((ext.x + sx), hx + ext.y * .5f, -ext.z - fx), RCCP_Light.LightType.Brakelight, setupData.brakelightColor);
        }
        if (setupData.addTailLights) {
            SpawnLight("RCCP_Tail_L", new Vector3(-(ext.x * .8f + sx), hx + ext.y * .45f, -ext.z - fx), RCCP_Light.LightType.Taillight, setupData.taillightColor);
            SpawnLight("RCCP_Tail_R", new Vector3((ext.x * .8f + sx), hx + ext.y * .45f, -ext.z - fx), RCCP_Light.LightType.Taillight, setupData.taillightColor);
        }

        //  REVERSE
        if (setupData.addReverseLights) {
            SpawnLight("RCCP_Reverse_L", new Vector3(-(ext.x * .6f + sx), hx + ext.y * .3f, -ext.z - fx), RCCP_Light.LightType.Reverselight, setupData.reverselightColor);
            SpawnLight("RCCP_Reverse_R", new Vector3((ext.x * .6f + sx), hx + ext.y * .3f, -ext.z - fx), RCCP_Light.LightType.Reverselight, setupData.reverselightColor);
        }

        //  INDICATORS
        if (setupData.addIndicators) {
            float yInd = hx + ext.y * .4f;
            //  FRONT
            SpawnLight("RCCP_Indicator_FL", new Vector3(-(ext.x + sx), yInd, ext.z + fx), RCCP_Light.LightType.IndicatorLeftLight, setupData.indicatorColor);
            SpawnLight("RCCP_Indicator_FR", new Vector3((ext.x + sx), yInd, ext.z + fx), RCCP_Light.LightType.IndicatorRightLight, setupData.indicatorColor);
            //  REAR
            SpawnLight("RCCP_Indicator_RL", new Vector3(-(ext.x + sx), yInd, -ext.z - fx), RCCP_Light.LightType.IndicatorLeftLight, setupData.indicatorColor);
            SpawnLight("RCCP_Indicator_RR", new Vector3((ext.x + sx), yInd, -ext.z - fx), RCCP_Light.LightType.IndicatorRightLight, setupData.indicatorColor);
        }

        //  Register each new light with the manager.
        for (int i = 0; i < newLights.Count; i++) {
            manager.RegisterLight(newLights[i].GetComponent<RCCP_Light>());
        }
        manager.GetAllLights();

        for (int i = 0; i < manager.lights.Count; i++) {

            if (manager.lights[i] == null)
                continue;

            CheckRotationOfLight(manager.lights[i].gameObject);

#if !BCG_URP && !BCG_HDRP
            ApplyFlares(manager.lights[i].gameObject);
#endif

        }

        EditorUtility.DisplayDialog("Lights Added", $"Created {newLights.Count} lights and registered them to RCCP_Lights.", "OK");
        Selection.activeGameObject = manager.gameObject;
        Close();

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Helpers
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>
    /// Caches the current selection if valid.
    /// </summary>
    private void AttemptCacheSelection() {

        if (Selection.activeGameObject && Selection.activeGameObject.activeInHierarchy && Selection.activeGameObject != selectedVehicle) {

            RCCP_CarController mainComponent = Selection.activeGameObject.GetComponentInParent<RCCP_CarController>();

            if (mainComponent != null && !EditorUtility.IsPersistent(Selection.activeGameObject))
                selectedVehicle = mainComponent.gameObject;

            if (!selectedVehicle)
                return;

            CacheVehicleBounds();

            RCCP_Lights vehicleLights = selectedVehicle.GetComponentInChildren<RCCP_Lights>(true);

            if (!vehicleLights)
                return;

            vehicleLights.GetAllLights();
            bool lightsFound = vehicleLights.lights != null && vehicleLights.lights.Count > 0;

            if (!closing && lightsFound) {

                closing = true;
                string warnMessaage = "This vehicle already has lights, creating new lights through this wizard will make them double. Remove all lights in RCCP_Lights component of your vehicle to use this quick lights setup wizard.";
                EditorUtility.DisplayDialog("Vehicle Already Has Lights", warnMessaage, "Ok");
                Debug.LogWarning(warnMessaage);

            }

        }

    }

    /// <summary>
    /// Computes renderer bounds for the selected vehicle in its own local space
    /// so that placement handles stay accurate even if the root object is rotated.
    /// This implementation iterates mesh filters to gather true local extents.
    /// </summary>
    private void CacheVehicleBounds() {

        if (!selectedVehicle)
            return;

        // Initialize local bounds
        Bounds localBounds = new Bounds();
        bool boundsInitialized = false;

        // Gather all MeshFilters under the vehicle root
        MeshFilter[] meshFilters = selectedVehicle.GetComponentsInChildren<MeshFilter>(false);

        foreach (MeshFilter mf in meshFilters) {

            if (mf.sharedMesh == null)
                continue;

            // Get mesh's local-space bounds
            Bounds meshBounds = mf.sharedMesh.bounds;
            Vector3 centerLS = meshBounds.center;
            Vector3 extentsLS = meshBounds.extents;

            // Iterate each corner of the mesh bounds
            for (int xi = -1; xi <= 1; xi += 2) {
                for (int yi = -1; yi <= 1; yi += 2) {
                    for (int zi = -1; zi <= 1; zi += 2) {

                        // Corner in mesh local space
                        Vector3 cornerMeshLocal = centerLS + Vector3.Scale(extentsLS, new Vector3(xi, yi, zi));

                        // Transform corner to world space via mesh transform
                        Vector3 cornerWorld = mf.transform.TransformPoint(cornerMeshLocal);

                        // Convert corner to vehicle-local space
                        Vector3 cornerVehicleLocal = selectedVehicle.transform.InverseTransformPoint(cornerWorld);

                        if (!boundsInitialized) {
                            localBounds = new Bounds(cornerVehicleLocal, Vector3.zero);
                            boundsInitialized = true;
                        } else {
                            localBounds.Encapsulate(cornerVehicleLocal);
                        }

                    }
                }
            }

        }

        // Assign fallback if no meshes were found
        if (!boundsInitialized) {
            cachedVehicleBounds = new Bounds(Vector3.zero, Vector3.one);
        } else {
            cachedVehicleBounds = localBounds;
        }

    }

    private void CheckRotationOfLight(GameObject light) {

        if (!selectedVehicle)
            return;

        Vector3 relativePos = selectedVehicle.transform.InverseTransformPoint(light.transform.position);

        if (relativePos.z > 0f) {

            if (Mathf.Abs(light.transform.localRotation.y) > .5f)
                light.transform.localRotation = Quaternion.identity;

        } else {

            if (Mathf.Abs(light.transform.localRotation.y) < .5f)
                light.transform.localRotation = Quaternion.Euler(light.transform.localRotation.x, 180f, light.transform.localRotation.z);

        }

        RCCP_Lights lightsManager = selectedVehicle.GetComponentInChildren<RCCP_Lights>();

        if (lightsManager)
            lightsManager.GetAllLights();

    }

#if !BCG_URP && !BCG_HDRP
    /// <summary>
    /// Copies the LensFlare component from the reference prefab
    /// </summary>
    private void ApplyFlares(GameObject light) {

        GameObject _flarePrefab = RCCP_Settings.Instance.flarePrefab;

        // Get the LensFlare from the prefab
        LensFlare source = _flarePrefab.GetComponent<LensFlare>();
        if (source == null) {
            Debug.LogWarning("Reference prefab has no LensFlare component");
            return;
        }

        // Add or find the LensFlare on the target
        LensFlare target = light.GetComponent<LensFlare>();
        if (target == null) {
            target = Undo.AddComponent<LensFlare>(light);
        }

        // Copy all serialized values (including the Flare asset reference!
        ComponentUtility.CopyComponent(source);
        ComponentUtility.PasteComponentValues(target);

    }
#endif

    #endregion

}
