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
/// Base class for upgrade components.
/// </summary>
public class RCCP_UpgradeComponent : RCCP_Component {

    /// <summary>
    /// Current loadout.
    /// </summary>
    public RCCP_CustomizationLoadout Loadout {

        get {

            if (!CarController.Customizer) {

                Debug.LogError("Customizer component couldn't found on the " + CarController.transform.name + "!");
                return null;

            }

            return CarController.Customizer.GetLoadout();

        }

    }

    /// <summary>
    /// Saves the current loadout.
    /// </summary>
    public void Save() {

        if (!CarController.Customizer) {

            Debug.LogError("Customizer component couldn't found on the " + CarController.transform.name + "!");
            return;

        }

        CarController.Customizer.Save();

    }

    /// <summary>
    /// Loads the latest saved loadout if existing.
    /// </summary>
    public void Load() {

        if (!CarController.Customizer) {

            Debug.LogError("Customizer component couldn't found on the " + CarController.transform.name + "!");
            return;

        }

        CarController.Customizer.Load();

    }

    /// <summary>
    /// Updates the loadout and all managers.
    /// </summary>
    /// <param name="component"></param>
    public void Refresh(MonoBehaviour component) {

        if (!CarController.Customizer) {

            Debug.LogError("Customizer component couldn't found on the " + CarController.transform.name + "!");
            return;

        }

        IRCCP_LoadoutComponent loadoutComponent = CarController.Customizer.GetLoadout() as IRCCP_LoadoutComponent;
        loadoutComponent.UpdateLoadout(component);

    }

}
