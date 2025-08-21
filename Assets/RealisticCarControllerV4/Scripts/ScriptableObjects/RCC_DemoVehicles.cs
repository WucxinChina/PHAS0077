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
/// Holds a collection of demo vehicle prefabs (RCC_CarControllerV4) for easy retrieval and spawning in demo scenes.
/// </summary>
public class RCC_DemoVehicles : ScriptableObject {

    /// <summary>
    /// An array of RCC_CarControllerV4 objects representing different demo vehicles.
    /// </summary>
    public RCC_CarControllerV4[] vehicles;

    #region singleton

    /// <summary>
    /// The private singleton instance. Loaded from the "RCC Assets/RCC_DemoVehicles" resource if not already set.
    /// </summary>
    private static RCC_DemoVehicles instance;

    /// <summary>
    /// Public accessor for the singleton instance of RCC_DemoVehicles.
    /// Automatically loads the ScriptableObject from "Resources/RCC Assets/RCC_DemoVehicles" if needed.
    /// </summary>
    public static RCC_DemoVehicles Instance {

        get {

            if (instance == null)
                instance = Resources.Load("RCC Assets/RCC_DemoVehicles") as RCC_DemoVehicles;

            return instance;

        }

    }

    #endregion

}
