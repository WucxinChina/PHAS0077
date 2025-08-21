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
/// Additional camera manager for hood (interior/FPS) camera and wheel cameras.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Exterior Cameras")]
public class RCCP_Exterior_Cameras : RCCP_Component {

    /// <summary>
    /// Internal reference to the hood camera (often used as an interior or first-person perspective).
    /// </summary>
    public RCCP_HoodCamera _hoodCamera;

    /// <summary>
    /// Provides access to the hood camera component found in this vehicle's child objects.
    /// </summary>
    public RCCP_HoodCamera HoodCamera {

        get {

            if (_hoodCamera == null)
                _hoodCamera = GetComponentInChildren<RCCP_HoodCamera>(true);

            return _hoodCamera;

        }

    }

    /// <summary>
    /// Internal reference to the wheel camera, often used to show close-up shots of the wheels in motion.
    /// </summary>
    public RCCP_WheelCamera _wheelCamera;

    /// <summary>
    /// Provides access to the wheel camera component found in this vehicle's child objects.
    /// </summary>
    public RCCP_WheelCamera WheelCamera {

        get {

            if (_wheelCamera == null)
                _wheelCamera = GetComponentInChildren<RCCP_WheelCamera>(true);

            return _wheelCamera;

        }

    }

}
