//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Customization applier for vehicles.
/// 6 Upgrade managers for paints, wheels, upgrades, spoilers, customization, and sirens.
/// </summary>
[DefaultExecutionOrder(10)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Customizer")]
public class RCCP_Customizer : RCCP_Component {

    /// <summary>
    /// Save file name of the vehicle.
    /// </summary>
    public string saveFileName = "";

    /// <summary>
    /// Auto initializes all managers. Disable it for networked vehicles.
    /// </summary>
    public bool autoInitialize = true;

    /// <summary>
    /// Loads the latest loadout.
    /// </summary>
    public bool autoLoadLoadout = true;

    /// <summary>
    /// Auto save.
    /// </summary>
    public bool autoSave = true;

    /// <summary>
    /// Defines the method/timing used for initializing all upgrade managers.
    /// </summary>
    public enum InitializeMethod { Awake, OnEnable, Start, DelayedWithFixedUpdate }

    /// <summary>
    /// Selected method (timing) for initializing all upgrade managers. 
    /// [SerializeField] removed as requested; no extra attributes used.
    /// </summary>
    public InitializeMethod initializeMethod = InitializeMethod.Start;

    /// <summary>
    /// Loadout class.
    /// </summary>
    public RCCP_CustomizationLoadout loadout = new RCCP_CustomizationLoadout();

    #region All upgrade managers

    /// <summary>
    /// Paint manager.
    /// </summary>
    private RCCP_VehicleUpgrade_PaintManager _paintManager;
    public RCCP_VehicleUpgrade_PaintManager PaintManager {

        get {

            if (_paintManager == null)
                _paintManager = GetComponentInChildren<RCCP_VehicleUpgrade_PaintManager>(true);

            return _paintManager;

        }

    }

    /// <summary>
    /// Wheel Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_WheelManager _wheelManager;
    public RCCP_VehicleUpgrade_WheelManager WheelManager {

        get {

            if (_wheelManager == null)
                _wheelManager = GetComponentInChildren<RCCP_VehicleUpgrade_WheelManager>(true);

            return _wheelManager;

        }

    }

    /// <summary>
    /// Upgrade Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_UpgradeManager _upgradeManager;
    public RCCP_VehicleUpgrade_UpgradeManager UpgradeManager {

        get {

            if (_upgradeManager == null)
                _upgradeManager = GetComponentInChildren<RCCP_VehicleUpgrade_UpgradeManager>(true);

            return _upgradeManager;

        }

    }

    /// <summary>
    /// Spoiler Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_SpoilerManager _spoilerManager;
    public RCCP_VehicleUpgrade_SpoilerManager SpoilerManager {

        get {

            if (_spoilerManager == null)
                _spoilerManager = GetComponentInChildren<RCCP_VehicleUpgrade_SpoilerManager>(true);

            return _spoilerManager;

        }

    }

    /// <summary>
    /// Siren Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_SirenManager _sirenManager;
    public RCCP_VehicleUpgrade_SirenManager SirenManager {

        get {

            if (_sirenManager == null)
                _sirenManager = GetComponentInChildren<RCCP_VehicleUpgrade_SirenManager>(true);

            return _sirenManager;

        }

    }

    /// <summary>
    /// Customization Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_CustomizationManager _customizationManager;
    public RCCP_VehicleUpgrade_CustomizationManager CustomizationManager {

        get {

            if (_customizationManager == null)
                _customizationManager = GetComponentInChildren<RCCP_VehicleUpgrade_CustomizationManager>(true);

            return _customizationManager;

        }

    }

    /// <summary>
    /// Decal Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_DecalManager _decalManager;
    public RCCP_VehicleUpgrade_DecalManager DecalManager {

        get {

            if (_decalManager == null)
                _decalManager = GetComponentInChildren<RCCP_VehicleUpgrade_DecalManager>(true);

            return _decalManager;

        }

    }

    /// <summary>
    /// Neon Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_NeonManager _neonManager;
    public RCCP_VehicleUpgrade_NeonManager NeonManager {

        get {

            if (_neonManager == null)
                _neonManager = GetComponentInChildren<RCCP_VehicleUpgrade_NeonManager>(true);

            return _neonManager;

        }

    }

    #endregion

    public override void Awake() {

        base.Awake();

        //  Loads the latest loadout.
        if (autoLoadLoadout)
            Load();

        //  Initializes all managers if selected method is Awake.
        if (initializeMethod == InitializeMethod.Awake)
            Initialize();

    }

    public override void OnEnable() {

        base.OnEnable();

        //  Initializes all managers if selected method is OnEnable.
        if (initializeMethod == InitializeMethod.OnEnable)
            Initialize();

    }

    public override void Start() {

        base.Start();

        //  Initializes all managers if selected method is Start.
        if (initializeMethod == InitializeMethod.Start)
            Initialize();

        //  Initializes all managers if selected method is delayed with fixed update.
        if (initializeMethod == InitializeMethod.DelayedWithFixedUpdate)
            StartCoroutine(Delayed());

    }

    /// <summary>
    /// Delayed initialization via coroutine if needed.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Delayed() {

        yield return new WaitForFixedUpdate();
        Initialize();

    }

    /// <summary>
    /// Initialize all upgrade managers.
    /// </summary>
    public void Initialize() {

        if (loadout == null)
            loadout = new RCCP_CustomizationLoadout();

        //  Paint manager
        if (PaintManager)
            PaintManager.Initialize();

        //  Wheel manager
        if (WheelManager)
            WheelManager.Initialize();

        //  Upgrade manager
        if (UpgradeManager)
            UpgradeManager.Initialize();

        //  Spoiler manager
        if (SpoilerManager)
            SpoilerManager.Initialize();

        //  Siren manager
        if (SirenManager)
            SirenManager.Initialize();

        //  Customization manager
        if (CustomizationManager)
            CustomizationManager.Initialize();

        //  Decal manager
        if (DecalManager)
            DecalManager.Initialize();

        //  Neon manager
        if (NeonManager)
            NeonManager.Initialize();

    }

    /// <summary>
    /// Retrieves the current customization loadout.
    /// </summary>
    /// <returns></returns>
    public RCCP_CustomizationLoadout GetLoadout() {

        if (loadout != null) {

            return loadout;

        } else {

            loadout = new RCCP_CustomizationLoadout();
            return loadout;

        }

    }

    /// <summary>
    /// Saves the current loadout to PlayerPrefs (JSON).
    /// </summary>
    public void Save() {

        if (loadout == null)
            loadout = new RCCP_CustomizationLoadout();

        PlayerPrefs.SetString(saveFileName, JsonUtility.ToJson(loadout));

    }

    /// <summary>
    /// Loads the previously saved loadout from PlayerPrefs (JSON).
    /// </summary>
    public void Load() {

        if (PlayerPrefs.HasKey(saveFileName))
            loadout = (RCCP_CustomizationLoadout)JsonUtility.FromJson(PlayerPrefs.GetString(saveFileName), typeof(RCCP_CustomizationLoadout));

    }

    /// <summary>
    /// Deletes the last saved loadout and restores vehicle upgrades to default.
    /// </summary>
    public void Delete() {

        if (PlayerPrefs.HasKey(saveFileName))
            PlayerPrefs.DeleteKey(saveFileName);

        loadout = new RCCP_CustomizationLoadout();

        //  Restore paint manager
        if (PaintManager)
            PaintManager.Restore();

        //  Restore wheel manager
        if (WheelManager)
            WheelManager.Restore();

        //  Restore upgrade manager
        if (UpgradeManager)
            UpgradeManager.Restore();

        //  Restore spoiler manager
        if (SpoilerManager)
            SpoilerManager.Restore();

        //  Restore siren manager
        if (SirenManager)
            SirenManager.Restore();

        //  Restore customization manager
        if (CustomizationManager)
            CustomizationManager.Restore();

        //  Restore decal manager
        if (DecalManager)
            DecalManager.Restore();

        //  Restore neon manager
        if (NeonManager)
            NeonManager.Restore();

    }

    /// <summary>
    /// Hides all visual upgrades such as spoilers, sirens, decals, and neon.
    /// </summary>
    public void HideAll() {

        if (SpoilerManager)
            SpoilerManager.DisableAll();

        if (SirenManager)
            SirenManager.DisableAll();

        if (DecalManager)
            DecalManager.DisableAll();

        if (NeonManager)
            NeonManager.DisableAll();

    }

    /// <summary>
    /// Shows all visual upgrades such as spoilers, sirens, decals, and neon.
    /// </summary>
    public void ShowAll() {

        if (SpoilerManager)
            SpoilerManager.EnableAll();

        if (SirenManager)
            SirenManager.EnableAll();

        if (DecalManager)
            DecalManager.EnableAll();

        if (NeonManager)
            NeonManager.EnableAll();

    }

    /// <summary>
    /// Reload method reserved for future usage (currently empty).
    /// </summary>
    public void Reload() {

        //

    }

    private void Reset() {

        saveFileName = GetComponentInParent<RCCP_CarController>(true).transform.name;

    }

}
