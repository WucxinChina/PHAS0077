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
/// Upgrades speed of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Speed")]
public class RCCP_VehicleUpgrade_Speed : RCCP_Component {

    private int _speedLevel = 0;

    /// <summary>
    /// Current speed level. Maximum is 5.
    /// </summary>
    public int SpeedLevel {
        get {
            return _speedLevel;
        }
        set {
            if (value <= 5)
                _speedLevel = value;
        }
    }

    /// <summary>
    /// Default maximum speed of the vehicle.
    /// </summary>
    [HideInInspector] public float defMaxSpeed = -1f;

    /// <summary>
    /// Efficiency of the upgrade.
    /// </summary>
    [Range(1f, 2f)] public float efficiency = 1.1f;

    /// <summary>
    /// Updates maximum speed of the engine component and initializes it.
    /// </summary>
    public void Initialize() {

        if (!CarController.Engine) {

            Debug.LogError("Engine couldn't found in the vehicle. RCCP_VehicleUpgrade_Speed needs it to upgrade the speed level");
            enabled = false;
            return;

        }

        if (defMaxSpeed <= 0)
            defMaxSpeed = CarController.Engine.maximumSpeed;

        CarController.Engine.maximumSpeed = Mathf.Lerp(defMaxSpeed, defMaxSpeed * efficiency, SpeedLevel / 5f);
        CarController.Engine.UpdateMaximumSpeed();

    }

    /// <summary>
    /// Updates speed and save it.
    /// </summary>
    public void UpdateStats() {

        if (!CarController.Engine) {

            Debug.LogError("Engine couldn't found in the vehicle. RCCP_VehicleUpgrade_Speed needs it to upgrade the speed level");
            enabled = false;
            return;

        }

        if (defMaxSpeed <= 0)
            defMaxSpeed = CarController.Engine.maximumSpeed;

        CarController.Engine.maximumSpeed = Mathf.Lerp(defMaxSpeed, defMaxSpeed * efficiency, SpeedLevel / 5f);
        CarController.Engine.UpdateMaximumSpeed();

    }

    public void Restore() {

        SpeedLevel = 0;
  
        if (defMaxSpeed <= 0)
            defMaxSpeed = CarController.Engine.maximumSpeed;

        CarController.Engine.maximumSpeed = defMaxSpeed;
        CarController.Engine.UpdateMaximumSpeed();

    }

}
