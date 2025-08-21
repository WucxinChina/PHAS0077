//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class RCCP_RebindSaveLoad {

    public static void Save() {

        InputActionAsset actions = RCCP_InputActions.Instance.inputActions;

        var rebinds = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);

    }

    public static void Load() {

        InputActionAsset actions = RCCP_InputActions.Instance.inputActions;
        var rebinds = PlayerPrefs.GetString("rebinds");

        if (!string.IsNullOrEmpty(rebinds))
            actions.LoadBindingOverridesFromJson(rebinds);

    }

}
