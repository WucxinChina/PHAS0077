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
/// Base class for all modular components.
/// </summary>
public class RCCP_GenericComponent : MonoBehaviour {

    public RCCP_Settings RCCPSettings {

        get {

            if (_RCCPSettings == null)
                _RCCPSettings = RCCP_RuntimeSettings.RCCPSettingsInstance;

            return _RCCPSettings;

        }

    }
    private RCCP_Settings _RCCPSettings;

    public RCCP_GroundMaterials RCCPGroundMaterials {

        get {

            if (_RCCPGroundMaterials == null)
                _RCCPGroundMaterials = RCCP_RuntimeSettings.RCCPGroundMaterialsInstance;

            return _RCCPGroundMaterials;

        }

    }
    private RCCP_GroundMaterials _RCCPGroundMaterials;

    public RCCP_SceneManager RCCPSceneManager {

        get {

            if (_RCCSceneManager == null)
                _RCCSceneManager = RCCP_SceneManager.Instance;

            return _RCCSceneManager;

        }

    }
    private RCCP_SceneManager _RCCSceneManager;

}
