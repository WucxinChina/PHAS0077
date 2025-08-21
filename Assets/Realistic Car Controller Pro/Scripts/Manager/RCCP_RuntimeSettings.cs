//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;

public static class RCCP_RuntimeSettings {

    // Private field for our one shared clone
    private static RCCP_Settings _runtimeInstance;

    // Public getter returns the clone, creating it if needed
    public static RCCP_Settings RCCPSettingsInstance {

        get {

            if (_runtimeInstance == null)
                _runtimeInstance = ScriptableObject.Instantiate(RCCP_Settings.Instance);

            return _runtimeInstance;

        }

    }

    public static RCCP_GroundMaterials RCCPGroundMaterialsInstance {

        get {

            if (_RCCPGroundMaterials == null)
                _RCCPGroundMaterials = ScriptableObject.Instantiate(RCCP_GroundMaterials.Instance);

            return _RCCPGroundMaterials;

        }

    }
    private static RCCP_GroundMaterials _RCCPGroundMaterials;

    public static RCCP_ChangableWheels RCCPChangableWheelsInstance {

        get {

            if (_RCCPChangableWheels == null)
                _RCCPChangableWheels = ScriptableObject.Instantiate(RCCP_ChangableWheels.Instance);

            return _RCCPChangableWheels;

        }

    }
    private static RCCP_ChangableWheels _RCCPChangableWheels;

    // (Optional) If you ever want to discard it (e.g., on scene unload)
    public static void Clear() {

        _runtimeInstance = null;
        _RCCPGroundMaterials = null;
        _RCCPChangableWheels = null;

    }

}
