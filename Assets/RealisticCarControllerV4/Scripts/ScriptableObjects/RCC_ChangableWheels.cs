//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages changeable wheels at runtime. 
/// Stores a collection of wheel prefabs that can be dynamically swapped on vehicles.
/// </summary>
[System.Serializable]
public class RCC_ChangableWheels : ScriptableObject {

    #region Singleton Implementation

    /// <summary>
    /// Singleton instance of RCC_ChangableWheels.
    /// Loads the asset from the Resources folder if it hasn't been initialized.
    /// </summary>
    private static RCC_ChangableWheels instance;

    /// <summary>
    /// Provides access to the singleton instance. 
    /// Loads the wheel asset from "RCC Assets/RCC_ChangableWheels" in Resources if not already loaded.
    /// </summary>
    public static RCC_ChangableWheels Instance {

        get {

            if (instance == null)
                instance = Resources.Load("RCC Assets/RCC_ChangableWheels") as RCC_ChangableWheels;

            return instance;

        }

    }

    #endregion

    /// <summary>
    /// Represents a changeable wheel option that holds a wheel prefab.
    /// </summary>
    [System.Serializable]
    public class ChangableWheels {

        /// <summary>
        /// The wheel prefab that can be used as a replacement.
        /// </summary>
        public GameObject wheel;

    }

    /// <summary>
    /// Array of available changeable wheels.
    /// </summary>
    public ChangableWheels[] wheels;

}
