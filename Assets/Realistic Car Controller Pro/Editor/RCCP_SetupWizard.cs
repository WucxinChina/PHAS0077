//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RCCP_SetupWizard : EditorWindow {

    /// <summary>Currently selected GameObject in the Hierarchy (cached each OnGUI).</summary>
    public GameObject selectedVehicle;

    //───────────────────────────────────────────────────────────────────────//
    #region GUI Styles
    //───────────────────────────────────────────────────────────────────────//

    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle helpBoxStyle;
    private GUIStyle progressBarStyle;

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Constants
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Total amount of wizard steps (0‑based index ⇒ 0 … TOTAL_STEPS‑1).</summary>
    private const int TOTAL_STEPS = 6;

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Data Container
    //───────────────────────────────────────────────────────────────────────//

    [System.Serializable]
    public class SetupData {

        public string vehicleName = "";
        public float mass = 1350f;
        public List<GameObject> frontWheels = new List<GameObject>();
        public List<GameObject> rearWheels = new List<GameObject>();
        public float suspensionDistance = 0.2f;
        public float springForce = 35000f;
        public float damperForce = 4500f;
        public float minEngineRPM = 800;
        public float maxEngineRPM = 7000;
        public float maxEngineTorque = 300f;
        public float maxSpeed = 240f;
        public DriveType driveType = DriveType.RWD;
        public WheelType wheelType = WheelType.Balanced;
        public HandlingType handlingType = HandlingType.Balanced;
        public bool addInputs, addDynamics, addStability, addAudio, addCustomizer, addLights, addDamage, addParticles, addLOD, addOtherAddons;

        public enum DriveType { FWD, RWD, AWD }
        public enum WheelType { Balanced, Stable, Realistic, Slippy }
        public enum HandlingType { Balanced, Stable, Realistic }

    }

    private readonly SetupData setupData = new SetupData();

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Menu Item / Lifecycle
    //───────────────────────────────────────────────────────────────────────//

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Vehicle Setup Wizard", false, -85)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Vehicle Setup Wizard", false, -85)]
    public static void ShowWindow() {

        RCCP_SetupWizard window = GetWindow<RCCP_SetupWizard>("RCCP Setup Wizard");
        window.minSize = new Vector2(400f, 520f);

    }

    private int currentStep = 0;

    private void OnEnable() {

        selectedVehicle = Selection.activeGameObject;

        if (!IsSelectionValid())
            selectedVehicle = null;

        // Default‑enable every addon so the user can opt‑out later.
        setupData.addInputs =
        setupData.addDynamics =
        setupData.addStability =
        setupData.addAudio =
        setupData.addCustomizer =
        setupData.addLights =
        setupData.addDamage =
        setupData.addParticles =
        setupData.addLOD =
        setupData.addOtherAddons = true;

    }

    private void OnGUI() {

        InitializeStyles();

        DrawHeader();
        DrawStep();
        DrawFooter();

        // Keeps the window reactive (e.g., to selection changes).
        Repaint();

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Validation Helpers
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Basic sanity‑check for the current editor selection.</summary>
    private bool IsSelectionValid() {

        GameObject go = Selection.activeGameObject;
        if (!go) return false;
        if (EditorUtility.IsPersistent(go)) return false; // Project window asset.
        if (!go.scene.IsValid()) return false; // Not in a valid scene.
        if (!go.activeInHierarchy) return false; // Inactive.
        if (go.GetComponentInParent<RCCP_CarController>()) return false;
        if (Selection.gameObjects.Length > 1) return false;
        return true;

    }

    private bool ValidateWheels() {

        if (setupData.frontWheels.Count < 2 || setupData.rearWheels.Count < 2 ||
           !setupData.frontWheels[0] || !setupData.frontWheels[1] ||
           !setupData.rearWheels[0] || !setupData.rearWheels[1]) {

            EditorUtility.DisplayDialog("Missing Wheels", "Please make sure both front and rear wheels are properly assigned before proceeding.", "OK");
            return false;
        }
        return true;

    }

    private bool ValidateSuspension() {

        if (setupData.springForce <= 1000f || setupData.damperForce <= 100f) {
            EditorUtility.DisplayDialog("Suspension Warning", "Suspension settings seem too low. Please check spring and damper values.", "OK");
            return false;
        }
        return true;

    }

    private bool ValidateEngine() {

        if (setupData.maxEngineTorque <= 100f) {
            EditorUtility.DisplayDialog("Engine Warning", "Max Engine Torque seems too low. Please check your engine settings.", "OK");
            return false;
        }
        if (setupData.minEngineRPM <= 600f) {
            EditorUtility.DisplayDialog("Engine Warning", "Min Engine RPM seems too low. Please check your engine settings.", "OK");
            return false;
        }
        if (setupData.maxEngineRPM >= 10000f) {
            EditorUtility.DisplayDialog("Engine Warning", "Max Engine RPM seems too high. Please check your engine settings.", "OK");
            return false;
        }
        return true;

    }

    /// <summary>
    /// Makes sure the vehicle carries at least one solid body collider.  
    /// If none exist it can optionally guide the user through adding them.
    /// </summary>
    private bool ValidateBodyCollider() {

        // 1) Do we already have a good collider?
        if (HasProperBodyCollider(selectedVehicle, out _))
            return true;

        // 2) Offer the wizard to create them.
        int reply = EditorUtility.DisplayDialogComplex(
            "Missing Body Collider",
            "The selected vehicle has no suitable chassis collider.\n\n" +
            "Would you like the wizard to help you add convex MeshColliders " +
            "to the main body parts now?",
            "Yes – add colliders",          // 0
            "No – I'll add them later",      // 1
            "Cancel Setup");                  // 2

        // 3) User said “No” → allow them to continue anyway.
        if (reply == 1)
            return true;

        // 4) User cancelled → abort the current step.
        if (reply == 2)
            return false;

        List<Transform> allWheelTransforms = new List<Transform>();

        for (int i = 0; i < setupData.frontWheels.Count; i++) {

            if (setupData.frontWheels[i] != null)
                allWheelTransforms.Add(setupData.frontWheels[i].transform);

        }

        for (int i = 0; i < setupData.rearWheels.Count; i++) {

            if (setupData.rearWheels[i] != null)
                allWheelTransforms.Add(setupData.rearWheels[i].transform);

        }

        // 5) User chose “Yes” → open the helper window.
        RCCP_BodyCollidersWizard.ShowWindow(selectedVehicle, allWheelTransforms);
        // We return *true* so the wizard may continue; the window runs independently.
        return true;

    }

    /// <summary>
    /// Ensures that every mesh used by the vehicle can be edited at runtime.
    /// Damage deformation needs Mesh.isReadable == true.
    /// </summary>
    private bool ValidateMeshesReadable() {

        // Collect every mesh under the vehicle hierarchy.
        List<Mesh> unreadableMeshes = new List<Mesh>();
        HashSet<string> alreadyChecked = new HashSet<string>();   // avoid duplicates

        // MeshFilters …
        foreach (MeshFilter mf in selectedVehicle.GetComponentsInChildren<MeshFilter>(true)) {

            Mesh mesh = mf.sharedMesh;
            if (!mesh) continue;

            if (!mesh.isReadable) unreadableMeshes.Add(mesh);

        }

        // … plus SkinnedMeshRenderers (if any).
        foreach (SkinnedMeshRenderer smr in selectedVehicle.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {

            Mesh mesh = smr.sharedMesh;
            if (!mesh) continue;

            if (!mesh.isReadable) unreadableMeshes.Add(mesh);

        }

        // Everything OK?
        if (unreadableMeshes.Count == 0)
            return true;

        // Ask the developer how to proceed.
        int answer = EditorUtility.DisplayDialogComplex(
            "Meshes not Readable",
            $"Damage deformation requires Read/Write enabled meshes, " +
            $"but {unreadableMeshes.Count} mesh asset(s) are currently non-readable.\n\n" +
            "Would you like the wizard to enable the flag and re-import them now? " +
            "(This will slightly increase memory usage.)",
            "Enable Automatically",        // 0
            "Continue Without Fix",        // 1
            "Cancel Setup");               // 2

        // Cancel pressed → abort the step.
        if (answer == 2)
            return false;

        // Fix requested → flip the flag and re-import.
        if (answer == 0)
            EnableReadWriteOnMeshes(unreadableMeshes);

        // Either fixed or dev chose to ignore → carry on.
        return true;

    }

    /// <summary>
    /// Sets Read/Write Enabled = true on each supplied mesh asset and re-imports it.
    /// </summary>
    private void EnableReadWriteOnMeshes(List<Mesh> meshes) {

        // Process each distinct asset only once.
        HashSet<string> processedPaths = new HashSet<string>();

        foreach (Mesh mesh in meshes) {

            string path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path) || processedPaths.Contains(path))
                continue;

            processedPaths.Add(path);

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || importer.isReadable)
                continue;

            Undo.RecordObject(importer, "Enable Read/Write");
            importer.isReadable = true;
            importer.SaveAndReimport();

        }

        Debug.Log($"[RCCP Setup Wizard] Enabled Read/Write on {processedPaths.Count} mesh asset(s).");

    }


    /// <summary>Runs every validation routine in one go.</summary>
    private bool ValidateAll() {

        return ValidateBodyCollider() &&
               ValidateWheels() &&
               ValidateSuspension() &&
               ValidateEngine() &&
               ValidateMeshesReadable();

    }


    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region GUI Drawing
    //───────────────────────────────────────────────────────────────────────//

    private void DrawHeader() {

        GUILayout.Label($"Step {currentStep + 1} / {TOTAL_STEPS}", headerStyle);
        GUILayout.Space(10);
        DrawProgressBar();

    }

    private void DrawProgressBar() {

        float progress = (float)currentStep / (TOTAL_STEPS - 1);
        Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, progress, $"Progress: {Mathf.RoundToInt(progress * 100f)}% Completed");
        GUILayout.Space(10);

    }

    private void DrawStep() {

        GUILayout.BeginVertical(boxStyle);

        switch (currentStep) {

            case 0: DrawBasicSettings(); break;
            case 1: DrawWheelSetup(); break;
            case 2: DrawSuspensionSetup(); break;
            case 3: DrawEngineSetup(); break;
            case 4: DrawComponents(); break;
            case 5: DrawFinalize(); break;

        }

        GUILayout.EndVertical();

    }

    private void DrawFooter() {

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();

        if (currentStep > 0 && GUILayout.Button("Back", buttonStyle))
            currentStep--;

        if (currentStep < TOTAL_STEPS - 1 && GUILayout.Button("Next", buttonStyle))
            currentStep++;
        else if (currentStep == TOTAL_STEPS - 1 && GUILayout.Button("Finish Setup", buttonStyle))
            FinishSetup();

        GUILayout.EndHorizontal();

    }

    //───────────────────────────────────────────────────────────────────────//
    #region Individual Step Drawers (collapsed for brevity)
    //───────────────────────────────────────────────────────────────────────//

    private void DrawBasicSettings() {

        EditorGUILayout.HelpBox("Welcome to RCCP's New Vehicle Creation Wizard!\n\nThis wizard makes it easy to create new controllable vehicles. Just drag and drop your vehicle model into the scene, select it, and the wizard will help you set up a basic controllable vehicle in no time. Once it's created, you can jump in and customize it even further. The wizard takes care of the essentials, and you can fine tune everything later to make it truly yours.", MessageType.None);

        GUILayout.Space(10);

        if (!IsSelectionValid()) {
            EditorGUILayout.HelpBox("Select the vehicle model in the scene to get started.", MessageType.Warning);
            setupData.vehicleName = "";
            return;
        }

        selectedVehicle = Selection.activeGameObject;
        setupData.vehicleName = selectedVehicle.name;

        GUILayout.Label("Basic Settings", subHeaderStyle);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Enter the main identifying and physical parameters for your vehicle.", MessageType.Info);
        GUILayout.Space(5);

        setupData.vehicleName = EditorGUILayout.TextField("Vehicle Name", setupData.vehicleName);
        setupData.mass = EditorGUILayout.FloatField("Mass (kg)", setupData.mass);
        GUILayout.Space(10);
        setupData.handlingType = (SetupData.HandlingType)EditorGUILayout.EnumPopup("Handling Type", setupData.handlingType);

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("According to your selection of handling type, the stabilization systems (ABS, ESP, TCS) will be adjusted for your selection.", MessageType.Info);
        GUILayout.Space(5);

    }

    private void DrawWheelSetup() {

        GUILayout.Label("Wheel Setup", subHeaderStyle);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Assign front and rear wheel gameobjects. Tyr to use the auto detection buttons first.", MessageType.Info);
        GUILayout.Space(10);

        // Ensure list capacity
        while (setupData.frontWheels.Count < 2) setupData.frontWheels.Add(null);
        while (setupData.rearWheels.Count < 2) setupData.rearWheels.Add(null);

        GUILayout.Label("Front Wheels", EditorStyles.boldLabel);
        setupData.frontWheels[0] = (GameObject)EditorGUILayout.ObjectField("Front Left", setupData.frontWheels[0], typeof(GameObject), true);
        setupData.frontWheels[1] = (GameObject)EditorGUILayout.ObjectField("Front Right", setupData.frontWheels[1], typeof(GameObject), true);

        GUILayout.Space(10);
        GUILayout.Label("Rear Wheels", EditorStyles.boldLabel);
        setupData.rearWheels[0] = (GameObject)EditorGUILayout.ObjectField("Rear Left", setupData.rearWheels[0], typeof(GameObject), true);
        setupData.rearWheels[1] = (GameObject)EditorGUILayout.ObjectField("Rear Right", setupData.rearWheels[1], typeof(GameObject), true);

        GUILayout.Space(10);
        if (GUILayout.Button("Auto Detect Front Wheels")) AutoDetectWheels_Front();
        if (GUILayout.Button("Auto Detect Rear Wheels")) AutoDetectWheels_Rear();
        GUILayout.Space(10);
        setupData.wheelType = (SetupData.WheelType)EditorGUILayout.EnumPopup("Wheel Type", setupData.wheelType);

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("According to your selection of wheel type, different wheel friction curves will be applied to the wheelcolliders.", MessageType.Info);
        GUILayout.Space(5);

    }

    private void DrawSuspensionSetup() {

        GUILayout.Label("Suspension Setup", subHeaderStyle);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Tune suspension travel, spring and damping forces.  Use auto calculate for a quick baseline.", MessageType.Info);
        GUILayout.Space(10);

        setupData.suspensionDistance = EditorGUILayout.Slider("Suspension Distance", setupData.suspensionDistance, 0.05f, 0.5f);
        setupData.springForce = EditorGUILayout.FloatField("Spring Force (N)", setupData.springForce);
        setupData.damperForce = EditorGUILayout.FloatField("Damper Force (N·s/m)", setupData.damperForce);


        GUILayout.Space(5);

        if (GUILayout.Button("Auto Calculate Suspension")) {
            setupData.springForce = setupData.mass * 30f;
            setupData.damperForce = setupData.springForce * 0.1f;
        }

    }

    private void DrawEngineSetup() {

        GUILayout.Label("Drive Setup", subHeaderStyle);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Configure drivetrain type and engine parameters.  Recommended torque can be auto calculated.", MessageType.Info);
        GUILayout.Space(10);

        setupData.driveType = (SetupData.DriveType)EditorGUILayout.EnumPopup("Drive Type", setupData.driveType);
        GUILayout.Space(5);
        setupData.maxEngineTorque = EditorGUILayout.FloatField("Max Engine Torque (Nm)", setupData.maxEngineTorque);
        setupData.minEngineRPM = EditorGUILayout.FloatField("Min Engine RPM", setupData.minEngineRPM);
        setupData.maxEngineRPM = EditorGUILayout.FloatField("Max Engine RPM", setupData.maxEngineRPM);
        setupData.maxSpeed = EditorGUILayout.FloatField("Max Speed (km/h)", setupData.maxSpeed);

        GUILayout.Space(5);

        if (GUILayout.Button("Auto Calculate Recommended Torque")) {
            setupData.maxEngineTorque = setupData.mass * 0.2f; // Simple heuristic
            Debug.Log($"[RCCP Setup Wizard] Recommended Torque ≈ {setupData.maxEngineTorque:F0} Nm");
        }

    }

    private void DrawComponents() {

        GUILayout.Label("Addon Components", subHeaderStyle);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Toggle optional systems to include with the generated vehicle.", MessageType.Info);
        GUILayout.Space(10);

        float defaultLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 135f;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        setupData.addInputs = EditorGUILayout.ToggleLeft("Inputs", setupData.addInputs);
        setupData.addDynamics = EditorGUILayout.ToggleLeft("Dynamics", setupData.addDynamics);
        setupData.addStability = EditorGUILayout.ToggleLeft("Stability", setupData.addStability);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        setupData.addCustomizer = EditorGUILayout.ToggleLeft("Customizer", setupData.addCustomizer);
        setupData.addLights = EditorGUILayout.ToggleLeft("Lights", setupData.addLights);
        setupData.addDamage = EditorGUILayout.ToggleLeft("Damage", setupData.addDamage);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        setupData.addParticles = EditorGUILayout.ToggleLeft("Particles", setupData.addParticles);
        setupData.addLOD = EditorGUILayout.ToggleLeft("LOD", setupData.addLOD);
        setupData.addAudio = EditorGUILayout.ToggleLeft("Audio", setupData.addAudio);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        setupData.addOtherAddons = EditorGUILayout.ToggleLeft("Other Addons Manager", setupData.addOtherAddons);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUIUtility.labelWidth = defaultLabelWidth;

        // Select All / None
        EditorGUILayout.BeginHorizontal();
        Color defaultGUIColor = GUI.color;
        GUI.color = Color.cyan;
        if (GUILayout.Button("Select All")) {
            ToggleAllAddons(true);
        }
        if (GUILayout.Button("Select None")) {
            ToggleAllAddons(false);
        }
        GUI.color = defaultGUIColor;
        EditorGUILayout.EndHorizontal();

    }

    private void DrawFinalize() {

        GUILayout.Label("Finalize Setup", subHeaderStyle);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Review your choices below, then press <Finish Setup> to create the vehicle.", MessageType.Info);
        GUILayout.Space(10);

        // Summary tables (basic)
        GUILayout.Label("Vehicle", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Name", setupData.vehicleName);
        EditorGUILayout.LabelField("Mass", $"{setupData.mass:F0} kg");
        EditorGUILayout.LabelField("Wheel Count", (setupData.frontWheels.Count + setupData.rearWheels.Count).ToString());
        GUILayout.Space(5);
        GUILayout.Label("Engine", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Torque", $"{setupData.maxEngineTorque:F0} Nm");
        EditorGUILayout.LabelField("RPM Range", $"{setupData.minEngineRPM} – {setupData.maxEngineRPM}");
        EditorGUILayout.LabelField("Drive Type", setupData.driveType.ToString());
        EditorGUILayout.LabelField("Max Speed", $"{setupData.maxSpeed:F0} km/h");
        GUILayout.Space(5);
        GUILayout.Label("Suspension", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Distance", $"{setupData.suspensionDistance:F2} m");
        EditorGUILayout.LabelField("Spring", $"{setupData.springForce:F0} N");
        EditorGUILayout.LabelField("Damper", $"{setupData.damperForce:F0} N·s/m");
        GUILayout.Space(20);
        EditorGUILayout.HelpBox("When ready, click the button below to instantiate and configure your RCCP vehicle.", MessageType.Info);
        GUILayout.Space(5);

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Finish / Helpers
    //───────────────────────────────────────────────────────────────────────//

    private void FinishSetup() {

        if (!ValidateAll())
            return;

        if (selectedVehicle == null) {
            EditorUtility.DisplayDialog("No Vehicle Selected", "Please select your vehicle GameObject in the hierarchy first.", "OK");
            return;
        }

        RCCP_CreateNewVehicle.NewVehicle(selectedVehicle, setupData);
        EditorUtility.DisplayDialog("Setup Completed", "Vehicle setup successfully completed!", "OK");
        Close();

    }

    private void ToggleAllAddons(bool state) {
        setupData.addInputs = setupData.addDynamics = setupData.addStability = setupData.addAudio =
        setupData.addCustomizer = setupData.addLights = setupData.addDamage = setupData.addParticles =
        setupData.addLOD = setupData.addOtherAddons = state;
    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Utility / Styles
    //───────────────────────────────────────────────────────────────────────//

    private void InitializeStyles() {

        if (headerStyle == null) {
            headerStyle = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(.8f, .8f, .8f) }
            };
        }
        if (subHeaderStyle == null) {
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 14,
                normal = { textColor = new Color(.7f, .7f, .9f) }
            };
        }
        if (boxStyle == null) {
            boxStyle = new GUIStyle(GUI.skin.box) {
                normal = { background = Texture2D.grayTexture, textColor = Color.white },
                fontSize = 12,
                padding = new RectOffset(10, 10, 10, 10)
            };
        }
        if (buttonStyle == null) {
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fixedHeight = 32f };
        }
        if (helpBoxStyle == null) { helpBoxStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 12 }; }
        if (progressBarStyle == null) { progressBarStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 12 }; }

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Wheel Auto‑Detection (unchanged)
    //───────────────────────────────────────────────────────────────────────//

    private void AutoDetectWheels_Front() {
        if (selectedVehicle == null) {
            EditorUtility.DisplayDialog("No Vehicle Selected", "Please select your vehicle GameObject in the hierarchy first.", "OK");
            return;
        }
        GameObject[] possibleFrontWheels = RCCP_DetectPossibleWheels.DetectPossibleFrontWheels(selectedVehicle);
        if (possibleFrontWheels != null && possibleFrontWheels.Length >= 2) {
            RCCP_PopupWindow_PossibleWheels.ShowWindow(possibleFrontWheels, input => {
                if (input != null && input.Length == 2 && input[0] != null && input[1] != null) {
                    bool rightSide = IsOnRight(selectedVehicle, input[0]);
                    setupData.frontWheels[!rightSide ? 0 : 1] = input[0];
                    rightSide = IsOnRight(selectedVehicle, input[1]);
                    setupData.frontWheels[!rightSide ? 0 : 1] = input[1];
                    Debug.Log("[RCCP Setup Wizard] Front wheels assigned.");
                }
            });
        } else {
            EditorUtility.DisplayDialog("No Front Wheels Detected", "Couldn't find any possible wheel models. Ensure your model has separated wheel meshes.", "OK");
        }
    }

    private void AutoDetectWheels_Rear() {
        if (selectedVehicle == null) {
            EditorUtility.DisplayDialog("No Vehicle Selected", "Please select your vehicle GameObject in the hierarchy first.", "OK");
            return;
        }
        GameObject[] possibleRearWheels = RCCP_DetectPossibleWheels.DetectPossibleRearWheels(selectedVehicle);
        if (possibleRearWheels != null && possibleRearWheels.Length >= 2) {
            RCCP_PopupWindow_PossibleWheels.ShowWindow(possibleRearWheels, input => {
                if (input != null && input.Length == 2 && input[0] != null && input[1] != null) {
                    bool rightSide = IsOnRight(selectedVehicle, input[0]);
                    setupData.rearWheels[!rightSide ? 0 : 1] = input[0];
                    rightSide = IsOnRight(selectedVehicle, input[1]);
                    setupData.rearWheels[!rightSide ? 0 : 1] = input[1];
                    Debug.Log("[RCCP Setup Wizard] Rear wheels assigned.");
                }
            });
        } else {
            EditorUtility.DisplayDialog("No Rear Wheels Detected", "Couldn't find any possible wheel models. Ensure your model has separated wheel meshes.", "OK");
        }
    }

    #endregion
    #endregion

    /// <summary>
    /// Returns <c>true</c> if the given vehicle contains at least one
    /// non-trigger, non-WheelCollider whose size is reasonably close to
    /// the model’s overall render bounds.
    /// </summary>
    private bool HasProperBodyCollider(GameObject vehicle, out Collider bodyCollider) {

        bodyCollider = null;

        // 1) Gather every collider in the hierarchy except wheel colliders.
        Collider[] colliders = vehicle.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders) {

            if (col is WheelCollider)   // skip wheels
                continue;

            if (col.isTrigger)          // ignore triggers
                continue;

            bodyCollider = col;
            break;

        }

        if (bodyCollider == null)
            return false;                 // nothing suitable found

        // 2) Compare collider volume with the visual render bounds
        //    (guards against tiny placeholder colliders).
        Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return true;                  // no renderers to compare – accept collider

        Bounds renderBounds = new Bounds(vehicle.transform.position, Vector3.zero);

        foreach (Renderer r in renderers)
            renderBounds.Encapsulate(r.bounds);

        float modelVolume = renderBounds.size.x * renderBounds.size.y * renderBounds.size.z;
        float colliderVolume = bodyCollider.bounds.size.x * bodyCollider.bounds.size.y * bodyCollider.bounds.size.z;

        // Accept if collider covers at least 25 % of the model volume.
        return colliderVolume >= modelVolume * .25f;

    }

    private bool IsOnRight(GameObject vehicle, GameObject wheel) {

        // Transform the child's position to the parent's local space
        Vector3 localPosition = vehicle.transform.InverseTransformPoint(wheel.transform.position);

        // Check if the local X coordinate is positive
        return localPosition.x > 0;

    }

}
