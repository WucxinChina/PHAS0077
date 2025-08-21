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
/// Customization demo used in the demo scene. Enables disables cameras and canvases.
/// </summary>
public class RCC_CustomizationDemo : RCC_Core {

    private static RCC_CustomizationDemo instance;
    public static RCC_CustomizationDemo Instance {

        get {

#if !UNITY_2022_1_OR_NEWER
            if (instance == null)
                instance = FindObjectOfType<RCC_CustomizationDemo>();
#else
            if (instance == null)
                instance = FindFirstObjectByType<RCC_CustomizationDemo>();
#endif

            return instance;

        }

    }

    private RCC_CarControllerV4 vehicle;

    public RCC_ShowroomCamera showroomCamera;
    public RCC_Camera RCCCamera;
    public RCC_UI_DashboardDisplay RCCCanvas;
    public Transform location;

    public void EnableCustomization(RCC_CarControllerV4 carController) {

        vehicle = carController;

        RCCCamera.gameObject.SetActive(false);
        showroomCamera.gameObject.SetActive(true);
        RCCCanvas.SetDisplayType(RCC_UI_DashboardDisplay.DisplayType.Customization);
        RCC.Transport(vehicle, location.position, location.rotation);
        RCC.SetControl(vehicle, false);

    }

    public void DisableCustomization() {

        if (!vehicle)
            return;

        RCCCamera.gameObject.SetActive(true);
        showroomCamera.gameObject.SetActive(false);
        RCCCanvas.SetDisplayType(RCC_UI_DashboardDisplay.DisplayType.Full);
        RCC.SetControl(vehicle, true);
        vehicle = null;

    }

}
