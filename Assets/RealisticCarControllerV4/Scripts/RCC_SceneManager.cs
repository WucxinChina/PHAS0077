//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The central scene manager for RCC (Realistic Car Controller). 
/// Manages the active player vehicle, player camera, UI, record/replay systems, and more. 
/// This script also tracks all spawned vehicles and terrains within the scene.
/// </summary>
public class RCC_SceneManager : RCC_Singleton<RCC_SceneManager> {

    #region Player References

    /// <summary>
    /// The active player vehicle currently under user control. 
    /// May be set automatically when a new vehicle is spawned, or manually.
    /// </summary>
    public RCC_CarControllerV4 activePlayerVehicle;

    /// <summary>
    /// The active RCC camera following the player vehicle. 
    /// Updated whenever a new RCC_Camera is spawned.
    /// </summary>
    public RCC_Camera activePlayerCamera;

    /// <summary>
    /// The active RCC UI dashboard canvas.
    /// </summary>
    public RCC_UI_DashboardDisplay activePlayerCanvas;

    /// <summary>
    /// A reference to the current main camera (if any). Useful for quick access.
    /// </summary>
    public Camera activeMainCamera;

    /// <summary>
    /// Stores the last registered player vehicle. Used to detect changes in the active player vehicle.
    /// </summary>
    private RCC_CarControllerV4 lastActivePlayerVehicle;

    #endregion

    #region Configuration Flags

    /// <summary>
    /// If true, the most recently spawned vehicle will be automatically registered as the player vehicle.
    /// </summary>
    public bool registerLastSpawnedVehicleAsPlayerVehicle = true;

    /// <summary>
    /// If true, hides the UI dashboard when no player vehicle is active.
    /// </summary>
    public bool disableUIWhenNoPlayerVehicle = false;

    /// <summary>
    /// If true, applies saved customizations (e.g., paint, wheels) from RCC_Customization_API when a new vehicle is registered.
    /// </summary>
    public bool loadCustomizationAtFirst = false;

    #endregion

    #region Recording / Replay

    /// <summary>
    /// Determines whether this scene manager uses recording and replay functionality.
    /// </summary>
    public bool useRecord = false;

    /// <summary>
    /// List of all RCC_Recorder components attached to vehicles in the scene.
    /// </summary>
    public List<RCC_Recorder> allRecorders = new List<RCC_Recorder>();

    /// <summary>
    /// Simple enum describing the current global recording/replay state (Neutral, Play, or Record).
    /// </summary>
    public enum RecordMode { Neutral, Play, Record }

    /// <summary>
    /// Stores the global state of record/replay across all recorders.
    /// </summary>
    public RecordMode recordMode = RecordMode.Neutral;

    #endregion

    #region Scene Management Data

    /// <summary>
    /// Stores the original timescale for the scene. Used to restore timescale after slow-motion or other adjustments.
    /// </summary>
    private float orgTimeScale = 1f;

    /// <summary>
    /// A collection of all vehicles (player-controlled or AI) currently in the scene.
    /// Populated whenever a vehicle spawns; updated whenever a vehicle is destroyed.
    /// </summary>
    public List<RCC_CarControllerV4> allVehicles = new List<RCC_CarControllerV4>();

#if BCG_ENTEREXIT
    /// <summary>
    /// The current active player character controller for enter-exit logic (if using BoneCracker Games' EnterExit package).
    /// </summary>
    public BCG_EnterExitPlayer activePlayerCharacter;
#endif

    /// <summary>
    /// All active Unity Terrain objects in the scene.
    /// </summary>
    public Terrain[] allTerrains;

    /// <summary>
    /// Helper class for storing references to specific terrain data.
    /// </summary>
    public class Terrains {

        public Terrain terrain;
        public TerrainData mTerrainData;
#if UNITY_2023_3_OR_NEWER
        public PhysicsMaterial terrainCollider;
#else
        public PhysicMaterial terrainCollider;
#endif
        public int alphamapWidth;
        public int alphamapHeight;
        public float[,,] mSplatmapData;
        public float mNumTextures;

    }

    /// <summary>
    /// An array of <see cref="Terrains"/> storing custom data about each discovered Unity Terrain.
    /// </summary>
    public Terrains[] terrains;

    /// <summary>
    /// True if terrain data has been fully initialized for all active terrains in the scene.
    /// </summary>
    public bool terrainsInitialized = false;

#endregion

#region Events

    /// <summary>
    /// Fires whenever the active driving behavior changes (e.g., switching from arcade to realistic).
    /// </summary>
    public delegate void onBehaviorChanged();
    public static event onBehaviorChanged OnBehaviorChanged;

    /// <summary>
    /// Fires whenever the active player vehicle changes (e.g., new car spawned or user switched).
    /// </summary>
    public delegate void onVehicleChanged();
    public static event onVehicleChanged OnVehicleChanged;

#endregion

#region Unity Callbacks



    private void HandleMainCameraChanged(Camera cam) {
        // update your local reference
        activeMainCamera = cam;
    }

    private void Awake() {

        // Override Unity's fixedDeltaTime if enabled in RCC_Settings.
        if (Settings.overrideFixedTimeStep)
            Time.fixedDeltaTime = Settings.fixedTimeStep;

        // Override the target frame rate if enabled.
        if (Settings.overrideFPS)
            Application.targetFrameRate = Settings.maxFPS;

        // If telemetry is enabled in RCC_Settings, instantiate the Telemetry prefab.
        if (Settings.useTelemetry)
            Instantiate(Settings.RCCTelemetry, Vector3.zero, Quaternion.identity);

        // Subscribe to spawn/destroy events for vehicles & camera, and to input events for slow-motion & record/replay.
        RCC_Camera.OnBCGCameraSpawned += RCC_Camera_OnBCGCameraSpawned;
        RCC_CarControllerV4.OnRCCPlayerSpawned += RCC_CarControllerV4_OnRCCSpawned;
        RCC_AICarController.OnRCCAISpawned += RCC_AICarController_OnRCCAISpawned;
        RCC_CarControllerV4.OnRCCPlayerDestroyed += RCC_CarControllerV4_OnRCCPlayerDestroyed;
        RCC_AICarController.OnRCCAIDestroyed += RCC_AICarController_OnRCCAIDestroyed;
        RCC_InputManager.OnSlowMotion += RCC_InputManager_OnSlowMotion;
        RCC_InputManager.OnRecord += RCC_InputManager_OnRecord;
        RCC_InputManager.OnReplay += RCC_InputManager_OnReplay;

#if BCG_ENTEREXIT
        BCG_EnterExitPlayer.OnBCGPlayerSpawned += BCG_EnterExitPlayer_OnBCGPlayerSpawned;
        BCG_EnterExitPlayer.OnBCGPlayerDestroyed += BCG_EnterExitPlayer_OnBCGPlayerDestroyed;
#endif

        // 1) grab whatever camera is Main right now
        activeMainCamera = RCC_MainCameraProvider.MainCamera;

        // 2) listen for any camera‐tag‐swaps later on
        RCC_MainCameraProvider.OnMainCameraChanged += HandleMainCameraChanged;

#if !UNITY_2022_1_OR_NEWER
        // Try to locate an existing RCC UI dashboard display.
        activePlayerCanvas = FindObjectOfType<RCC_UI_DashboardDisplay>();
#else
        // Try to locate an existing RCC UI dashboard display.
        activePlayerCanvas = FindFirstObjectByType<RCC_UI_DashboardDisplay>();
#endif

        // Save the original time scale for slow-motion toggling.
        orgTimeScale = Time.timeScale;

        // If RCC Settings specify a locked cursor, apply it.
        if (Settings.lockAndUnlockCursor)
            Cursor.lockState = CursorLockMode.Locked;

    }

    void OnDestroy() {
        // clean up your subscription
        RCC_MainCameraProvider.OnMainCameraChanged -= HandleMainCameraChanged;
    }

    private void Start() {

        // Begin gathering terrain data asynchronously.
        StartCoroutine(GetAllTerrains());

    }

    private void Update() {

        // Check if player vehicle changed.
        if (activePlayerVehicle) {

            // If new active vehicle differs from the previous, fire OnVehicleChanged event.
            if (activePlayerVehicle != lastActivePlayerVehicle) {
                OnVehicleChanged?.Invoke();
            }

            lastActivePlayerVehicle = activePlayerVehicle;
        }

        // If UI is set to be disabled with no player vehicle, handle it.
        if (disableUIWhenNoPlayerVehicle && activePlayerCanvas)
            CheckCanvas();

        // If record functionality is enabled, poll recorders to see if their state changed.
        if (useRecord) {

            if (allRecorders != null && allRecorders.Count > 0) {

                switch (allRecorders[0].mode) {
                    case RCC_Recorder.RecorderMode.Neutral:
                        recordMode = RecordMode.Neutral;
                        break;
                    case RCC_Recorder.RecorderMode.Play:
                        recordMode = RecordMode.Play;
                        break;
                    case RCC_Recorder.RecorderMode.Record:
                        recordMode = RecordMode.Record;
                        break;
                }

            }

        }

    }

#endregion

#region Spawned & Destroyed Events

    /// <summary>
    /// Called when a new RCC vehicle (player or AI) is spawned. Registers it in allVehicles, sets up recorders, etc.
    /// </summary>
    /// <param name="RCC">The newly spawned player vehicle.</param>
    private void RCC_CarControllerV4_OnRCCSpawned(RCC_CarControllerV4 RCC) {

        // If this vehicle isn't in our master list, add it.
        if (!allVehicles.Contains(RCC)) {
            allVehicles.Add(RCC);

            // If record is enabled, ensure a RCC_Recorder component is attached.
            if (useRecord) {

                allRecorders = new List<RCC_Recorder>();
                allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

                RCC_Recorder recorder = null;

                // Find or create a recorder for this vehicle.
                if (allRecorders.Count > 0) {

                    for (int i = 0; i < allRecorders.Count; i++) {

                        if (allRecorders[i] != null && allRecorders[i].CarController == RCC) {

                            recorder = allRecorders[i];
                            break;

                        }

                    }

                }

                if (recorder == null) {

                    recorder = gameObject.AddComponent<RCC_Recorder>();
                    recorder.CarController = RCC;

                }

            }

        }

        // Validate recorders after a short delay.
        StartCoroutine(CheckMissingRecorders());

        // If configured, register the newly spawned vehicle as the player vehicle by default.
        if (registerLastSpawnedVehicleAsPlayerVehicle)
            RegisterPlayer(RCC);

#if BCG_ENTEREXIT
        // If using BCG_EnterExit, set the camera on the newly spawned vehicle's script, if present.
        if (RCC.gameObject.GetComponent<BCG_EnterExitVehicle>())
            RCC.gameObject.GetComponent<BCG_EnterExitVehicle>().correspondingCamera = activePlayerCamera.gameObject;
#endif

    }

    /// <summary>
    /// Called when a new AI car is spawned.
    /// </summary>
    /// <param name="RCCAI">The AI car controller object that references the RCC_CarControllerV4.</param>
    private void RCC_AICarController_OnRCCAISpawned(RCC_AICarController RCCAI) {

        // Add its main RCC_CarController to the master vehicles list if not present.
        if (!allVehicles.Contains(RCCAI.CarController)) {
            allVehicles.Add(RCCAI.CarController);

            // If record is enabled, ensure a RCC_Recorder component is attached.
            if (useRecord) {

                allRecorders = new List<RCC_Recorder>();
                allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

                RCC_Recorder recorder = null;

                if (allRecorders.Count > 0) {

                    for (int i = 0; i < allRecorders.Count; i++) {

                        if (allRecorders[i] != null && allRecorders[i].CarController == RCCAI.CarController) {

                            recorder = allRecorders[i];
                            break;

                        }

                    }

                }

                if (recorder == null) {

                    recorder = gameObject.AddComponent<RCC_Recorder>();
                    recorder.CarController = RCCAI.CarController;

                }

            }

        }

        // Re-check recorders if we're using the record system.
        if (useRecord)
            StartCoroutine(CheckMissingRecorders());

    }

    /// <summary>
    /// Called when a new RCC camera is spawned in the scene.
    /// </summary>
    private void RCC_Camera_OnBCGCameraSpawned(GameObject BCGCamera) {

        if (BCGCamera.GetComponent<RCC_Camera>())
            activePlayerCamera = BCGCamera.GetComponent<RCC_Camera>();

    }

#if BCG_ENTEREXIT
    /// <summary>
    /// Called when a BCG player character is spawned. 
    /// Useful if you're managing a player avatar that can enter and exit RCC vehicles.
    /// </summary>
    private void BCG_EnterExitPlayer_OnBCGPlayerSpawned(BCG_EnterExitPlayer player) {
        activePlayerCharacter = player;
    }
#endif

    /// <summary>
    /// Called when a player RCC vehicle is destroyed. 
    /// Removes it from the allVehicles list and updates recorders.
    /// </summary>
    private void RCC_CarControllerV4_OnRCCPlayerDestroyed(RCC_CarControllerV4 RCC) {

        if (allVehicles.Contains(RCC))
            allVehicles.Remove(RCC);

        StartCoroutine(CheckMissingRecorders());

    }

    /// <summary>
    /// Called when an AI RCC vehicle is destroyed. 
    /// Removes it from the allVehicles list and updates recorders.
    /// </summary>
    private void RCC_AICarController_OnRCCAIDestroyed(RCC_AICarController RCCAI) {

        if (allVehicles.Contains(RCCAI.CarController))
            allVehicles.Remove(RCCAI.CarController);

        StartCoroutine(CheckMissingRecorders());

    }

#if BCG_ENTEREXIT
    /// <summary>
    /// Called when a BCG player character is destroyed. 
    /// Currently empty, but can be extended for cleanup logic if required.
    /// </summary>
    private void BCG_EnterExitPlayer_OnBCGPlayerDestroyed(BCG_EnterExitPlayer player) {
        // Implementation can be added if needed.
    }
#endif

#endregion

#region Terrain Initialization

    /// <summary>
    /// Retrieves references to all active Terrains in the scene and initializes custom terrain data (if any).
    /// </summary>
    public IEnumerator GetAllTerrains() {

        yield return new WaitForFixedUpdate();

        allTerrains = Terrain.activeTerrains;

        yield return new WaitForFixedUpdate();

        // If terrains are found, create Terrains[] instances.
        if (allTerrains != null && allTerrains.Length >= 1) {

            terrains = new Terrains[allTerrains.Length];

            // Validate each Terrain has a valid terrain data.
            for (int i = 0; i < allTerrains.Length; i++) {

                if (allTerrains[i].terrainData == null) {

                    Debug.LogError("Terrain data for " + allTerrains[i].transform.name + " is missing. Check the terrain's data reference...");
                    yield return null;

                }

            }

            // Initialize Terrains array with relevant data.
            for (int i = 0; i < terrains.Length; i++) {

                terrains[i] = new Terrains {

                    terrain = allTerrains[i],
                    mTerrainData = allTerrains[i].terrainData,
                    terrainCollider = allTerrains[i].GetComponent<TerrainCollider>().sharedMaterial,
                    alphamapWidth = allTerrains[i].terrainData.alphamapWidth,
                    alphamapHeight = allTerrains[i].terrainData.alphamapHeight,

                };

                terrains[i].mSplatmapData = allTerrains[i].terrainData.GetAlphamaps(0, 0, terrains[i].alphamapWidth, terrains[i].alphamapHeight);
                terrains[i].mNumTextures = terrains[i].mSplatmapData.Length / (terrains[i].alphamapWidth * terrains[i].alphamapHeight);

            }

            terrainsInitialized = true;

        }

    }

#endregion

#region UI and Display Checks

    /// <summary>
    /// Disables or adjusts the UI canvas if there's no valid player vehicle or the player vehicle is inactive.
    /// </summary>
    public void CheckCanvas() {

        if (!activePlayerVehicle ||
            !activePlayerVehicle.canControl ||
            !activePlayerVehicle.gameObject.activeInHierarchy ||
            !activePlayerVehicle.enabled) {

            activePlayerCanvas.SetDisplayType(RCC_UI_DashboardDisplay.DisplayType.Off);
            return;
        }

        if (activePlayerCanvas.displayType != RCC_UI_DashboardDisplay.DisplayType.Customization)
            activePlayerCanvas.displayType = RCC_UI_DashboardDisplay.DisplayType.Full;

    }

#endregion

#region Record / Replay Controls

    /// <summary>
    /// Initiates or stops recording across all RCC_Recorder instances in the scene.
    /// </summary>
    public void Record() {

        if (!useRecord)
            return;

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++)
                allRecorders[i].Record();

        }

    }

    /// <summary>
    /// Initiates or stops replay across all RCC_Recorder instances in the scene.
    /// </summary>
    public void Play() {

        if (!useRecord)
            return;

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++)
                allRecorders[i].Play();

        }

    }

    /// <summary>
    /// Stops all recorders currently in record or play mode.
    /// </summary>
    public void Stop() {

        if (!useRecord)
            return;

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++)
                allRecorders[i].Stop();

        }

    }

    /// <summary>
    /// Searches for any recorders with missing references and destroys them. Ensures consistent data.
    /// </summary>
    private IEnumerator CheckMissingRecorders() {

        if (!useRecord)
            yield break;

        yield return new WaitForFixedUpdate();

        allRecorders = new List<RCC_Recorder>();
        allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

        if (allRecorders != null && allRecorders.Count > 0) {

            for (int i = 0; i < allRecorders.Count; i++) {

                if (allRecorders[i].CarController == null)
                    Destroy(allRecorders[i]);

            }

        }

        yield return new WaitForFixedUpdate();

        allRecorders = new List<RCC_Recorder>();
        allRecorders.AddRange(gameObject.GetComponentsInChildren<RCC_Recorder>());

    }

#endregion

#region Player Vehicle Registration

    /// <summary>
    /// Registers the specified vehicle as the active player vehicle. Optionally sets its camera target.
    /// </summary>
    /// <param name="playerVehicle">Vehicle to register as player vehicle.</param>
    public void RegisterPlayer(RCC_CarControllerV4 playerVehicle) {

        activePlayerVehicle = playerVehicle;

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

        if (loadCustomizationAtFirst)
            RCC_Customization_API.LoadStats(RCC_SceneManager.Instance.activePlayerVehicle);

    }

    /// <summary>
    /// Registers the vehicle and optionally sets it to be controllable.
    /// </summary>
    /// <param name="playerVehicle">Vehicle to register.</param>
    /// <param name="isControllable">Whether the vehicle should accept user controls.</param>
    public void RegisterPlayer(RCC_CarControllerV4 playerVehicle, bool isControllable) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

        if (loadCustomizationAtFirst)
            RCC_Customization_API.LoadStats(RCC_SceneManager.Instance.activePlayerVehicle);

    }

    /// <summary>
    /// Registers the vehicle, sets its controllable state, and toggles its engine on/off.
    /// </summary>
    /// <param name="playerVehicle">Vehicle to register.</param>
    /// <param name="isControllable">Whether the vehicle should accept user controls.</param>
    /// <param name="engineState">If true, starts the engine; if false, kills the engine.</param>
    public void RegisterPlayer(RCC_CarControllerV4 playerVehicle, bool isControllable, bool engineState) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);
        activePlayerVehicle.SetEngine(engineState);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

        if (loadCustomizationAtFirst)
            RCC_Customization_API.LoadStats(RCC_SceneManager.Instance.activePlayerVehicle);

    }

    /// <summary>
    /// De-registers the current player vehicle, removing its controllable state and camera lock.
    /// </summary>
    public void DeRegisterPlayer() {

        if (activePlayerVehicle)
            activePlayerVehicle.SetCanControl(false);

        activePlayerVehicle = null;

        if (activePlayerCamera)
            activePlayerCamera.RemoveTarget();

    }

#endregion

#region Behavior / Camera Controls

    /// <summary>
    /// Fires the OnBehaviorChanged event, indicating a global change of driving behavior (e.g., arcade to realistic).
    /// </summary>
    /// <param name="behaviorIndex">Index of the new behavior in RCC_Settings.</param>
    public void SetBehavior(int behaviorIndex) {

        Settings.overrideBehavior = true;
        Settings.behaviorSelectedIndex = behaviorIndex;

        OnBehaviorChanged?.Invoke();

    }

    /// <summary>
    /// Cycles the active camera mode (e.g., hood, orbit, chase).
    /// </summary>
    public void ChangeCamera() {

        if (activePlayerCamera)
            activePlayerCamera.ChangeCamera();

    }

#endregion

#region Teleport / Freeze

    /// <summary>
    /// Teleports the active player vehicle to a new position/rotation and temporarily disables control for 1 second.
    /// </summary>
    /// <param name="position">Target position.</param>
    /// <param name="rotation">Target rotation.</param>
    public void Transport(Vector3 position, Quaternion rotation) {

        if (activePlayerVehicle) {

            // Reset Rigidbody velocity to prevent sudden movement
            activePlayerVehicle.Rigid.linearVelocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            // Move using physics-friendly methods
            activePlayerVehicle.Rigid.MovePosition(position);
            activePlayerVehicle.Rigid.MoveRotation(rotation);

            // Reset vehicle state
            activePlayerVehicle.throttleInput = 0f;
            activePlayerVehicle.brakeInput = 1f;
            activePlayerVehicle.engineRPM = activePlayerVehicle.minEngineRPM;
            activePlayerVehicle.currentGear = 0;

            // Reset all wheels
            for (int i = 0; i < activePlayerVehicle.AllWheelColliders.Length; i++)
                activePlayerVehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            // Reset Rigidbody velocity to prevent sudden movement
            activePlayerVehicle.Rigid.linearVelocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            Rigidbody[] childRigids = activePlayerVehicle.GetComponentsInChildren<Rigidbody>();

            for (int i = 0; i < childRigids.Length; i++) {

                if (childRigids[i].transform == activePlayerVehicle.transform)
                    continue;

                childRigids[i].linearVelocity = Vector3.zero;
                childRigids[i].angularVelocity = Vector3.zero;

            }

            // Force physics update
            Physics.SyncTransforms();

        }

    }

    /// <summary>
    /// Teleports the specified vehicle to a new position/rotation without a timed freeze by default.
    /// </summary>
    /// <param name="vehicle">Vehicle to teleport.</param>
    /// <param name="position">Target position.</param>
    /// <param name="rotation">Target rotation.</param>
    public void Transport(RCC_CarControllerV4 vehicle, Vector3 position, Quaternion rotation) {

        if (vehicle) {

            // Reset Rigidbody velocity to prevent sudden movement
            vehicle.Rigid.linearVelocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            // Move using physics-friendly methods
            vehicle.Rigid.MovePosition(position);
            vehicle.Rigid.MoveRotation(rotation);

            // Reset vehicle state
            vehicle.throttleInput = 0f;
            vehicle.brakeInput = 1f;
            vehicle.engineRPM = vehicle.minEngineRPM;
            vehicle.currentGear = 0;

            // Reset all wheels
            for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            // Reset Rigidbody velocity to prevent sudden movement
            vehicle.Rigid.linearVelocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            Rigidbody[] childRigids = vehicle.GetComponentsInChildren<Rigidbody>();

            for (int i = 0; i < childRigids.Length; i++) {

                if (childRigids[i].transform == vehicle.transform)
                    continue;

                childRigids[i].linearVelocity = Vector3.zero;
                childRigids[i].angularVelocity = Vector3.zero;

            }

            // Force physics update
            Physics.SyncTransforms();

        }

    }

#endregion

#region Slow-Motion Handling

    /// <summary>
    /// Called by RCC_InputManager when toggling slow-motion. Adjusts time scale accordingly.
    /// </summary>
    private void RCC_InputManager_OnSlowMotion(bool state) {

        if (state)
            Time.timeScale = .2f;
        else
            Time.timeScale = orgTimeScale;

    }

#endregion

#region Input Manager Hooks for Record/Replay

    /// <summary>
    /// Called when user triggers replay from input. Invokes Play() on all recorders.
    /// </summary>
    private void RCC_InputManager_OnReplay() {

        Play();

    }

    /// <summary>
    /// Called when user triggers record from input. Invokes Record() on all recorders.
    /// </summary>
    private void RCC_InputManager_OnRecord() {

        Record();

    }

#endregion

#region OnDisable

    private void OnDisable() {

        // Unsubscribe from events to prevent memory leaks or callbacks after destruction.
        RCC_Camera.OnBCGCameraSpawned -= RCC_Camera_OnBCGCameraSpawned;
        RCC_CarControllerV4.OnRCCPlayerSpawned -= RCC_CarControllerV4_OnRCCSpawned;
        RCC_AICarController.OnRCCAISpawned -= RCC_AICarController_OnRCCAISpawned;
        RCC_CarControllerV4.OnRCCPlayerDestroyed -= RCC_CarControllerV4_OnRCCPlayerDestroyed;
        RCC_AICarController.OnRCCAIDestroyed -= RCC_AICarController_OnRCCAIDestroyed;
        RCC_InputManager.OnSlowMotion -= RCC_InputManager_OnSlowMotion;
        RCC_InputManager.OnRecord -= RCC_InputManager_OnRecord;
        RCC_InputManager.OnReplay -= RCC_InputManager_OnReplay;

#if BCG_ENTEREXIT
        BCG_EnterExitPlayer.OnBCGPlayerSpawned -= BCG_EnterExitPlayer_OnBCGPlayerSpawned;
        BCG_EnterExitPlayer.OnBCGPlayerDestroyed -= BCG_EnterExitPlayer_OnBCGPlayerDestroyed;
#endif

    }

#endregion

}
