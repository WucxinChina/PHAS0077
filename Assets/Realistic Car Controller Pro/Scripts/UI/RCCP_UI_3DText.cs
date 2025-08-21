//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI 3D Text")]
public class RCCP_UI_3DText : RCCP_UIComponent {

    private void Update() {

        if (!Camera.main)
            return;

        transform.rotation = Camera.main.transform.rotation;

    }

}
