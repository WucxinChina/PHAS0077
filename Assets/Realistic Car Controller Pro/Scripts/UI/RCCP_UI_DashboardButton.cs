//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI dashboard buttons for mobile / desktop.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Dashboard Button")]
public class RCCP_UI_DashboardButton : RCCP_UIComponent, IPointerClickHandler {

    /// <summary>
    /// Button types.
    /// </summary>
    public enum ButtonType { StartEngine, StopEngine, ABS, ESP, TCS, Headlights, LeftIndicator, RightIndicator, Low, Med, High, SH, GearUp, GearDown, HazardLights, ChangeCamera, SteeringHelper, TractionHelper, AngularDragHelper, TurnHelper, TrailAttachDetach, GearToggle, AutomaticGear_D, AutomaticGear_N, AutomaticGear_R, AutomaticGear_P, GearToggleAutomatic, GearToggleDNRP, GearToggleManual, Options };
    public ButtonType buttonType = ButtonType.ChangeCamera;

    /// <summary>
    /// On image will be used?
    /// </summary>
    public GameObject imageOn;

    private void OnEnable() {

        CheckImage();
        RCCP_Events.OnVehicleChanged += RCCP_SceneManager_OnVehicleChanged;

    }

    private void RCCP_SceneManager_OnVehicleChanged() {

        CheckImage();

    }

    public void OnPointerClick(PointerEventData eventData) {

        switch (buttonType) {

            case ButtonType.StartEngine:
                RCCP_InputManager.Instance.StartEngine();
                break;

            case ButtonType.StopEngine:
                RCCP_InputManager.Instance.StopEngine();
                break;

            case ButtonType.ABS:
                RCCP_InputManager.Instance.ABS();
                break;

            case ButtonType.ESP:
                RCCP_InputManager.Instance.ESP();
                break;

            case ButtonType.TCS:
                RCCP_InputManager.Instance.TCS();
                break;

            case ButtonType.Headlights:
                RCCP_InputManager.Instance.LowBeamHeadlights();
                break;

            case ButtonType.LeftIndicator:
                RCCP_InputManager.Instance.IndicatorLeftlights();
                break;

            case ButtonType.RightIndicator:
                RCCP_InputManager.Instance.IndicatorRightlights();
                break;

            case ButtonType.HazardLights:
                RCCP_InputManager.Instance.Indicatorlights();
                break;

            case ButtonType.ChangeCamera:
                RCCP_InputManager.Instance.ChangeCamera();
                break;

            case ButtonType.SteeringHelper:
                RCCP_InputManager.Instance.SteeringHelper();
                break;

            case ButtonType.TractionHelper:
                RCCP_InputManager.Instance.TractionHelper();
                break;

            case ButtonType.AngularDragHelper:
                RCCP_InputManager.Instance.AngularDragHelper();
                break;

            case ButtonType.TurnHelper:

                break;

            case ButtonType.GearUp:
                RCCP_InputManager.Instance.GearShiftUp();
                break;

            case ButtonType.GearDown:
                RCCP_InputManager.Instance.GearShiftDown();
                break;

            case ButtonType.GearToggle:
                RCCP_InputManager.Instance.ToggleGear(RCCP_Gearbox.TransmissionType.Automatic);
                break;

            case ButtonType.AutomaticGear_D:
                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.D);
                break;

            case ButtonType.AutomaticGear_N:
                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.N);
                break;

            case ButtonType.AutomaticGear_R:
                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.R);
                break;

            case ButtonType.AutomaticGear_P:
                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.P);
                break;

            case ButtonType.GearToggleAutomatic:
                RCCP_InputManager.Instance.ToggleGear(RCCP_Gearbox.TransmissionType.Automatic);
                break;

            case ButtonType.GearToggleDNRP:
                RCCP_InputManager.Instance.ToggleGear(RCCP_Gearbox.TransmissionType.Automatic_DNRP);
                break;

            case ButtonType.GearToggleManual:
                RCCP_InputManager.Instance.ToggleGear(RCCP_Gearbox.TransmissionType.Manual);
                break;

            case ButtonType.Options:
                RCCP_InputManager.Instance.Options();
                break;

        }

        CheckImage();

    }

    private void CheckImage() {

        if (!imageOn)
            return;

        if (!RCCPSceneManager.activePlayerVehicle)
            return;

        switch (buttonType) {

            case ButtonType.ABS:

                if (RCCPSceneManager.activePlayerVehicle.Stability)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Stability.ABS);

                break;

            case ButtonType.ESP:

                if (RCCPSceneManager.activePlayerVehicle.Stability)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Stability.ESP);

                break;

            case ButtonType.TCS:

                if (RCCPSceneManager.activePlayerVehicle.Stability)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Stability.TCS);

                break;

            case ButtonType.Headlights:

                if (RCCPSceneManager.activePlayerVehicle.Lights)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Lights.lowBeamHeadlights);

                break;

            case ButtonType.SteeringHelper:

                if (RCCPSceneManager.activePlayerVehicle.Stability)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Stability.steeringHelper);

                break;

            case ButtonType.TractionHelper:

                if (RCCPSceneManager.activePlayerVehicle.Stability)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Stability.tractionHelper);

                break;

            case ButtonType.AngularDragHelper:

                if (RCCPSceneManager.activePlayerVehicle.Stability)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Stability.angularDragHelper);

                break;

            case ButtonType.TurnHelper:

                break;

            case ButtonType.TrailAttachDetach:

                if (RCCPSceneManager.activePlayerVehicle.OtherAddonsManager && RCCPSceneManager.activePlayerVehicle.OtherAddonsManager.TrailAttacher) {

                    if (RCCPSceneManager.activePlayerVehicle.OtherAddonsManager.TrailAttacher.attachedTrailer != null)
                        RCCPSceneManager.activePlayerVehicle.OtherAddonsManager.TrailAttacher.attachedTrailer.DetachTrailer();

                }

                break;

            case ButtonType.GearToggleAutomatic:

                if (RCCPSceneManager.activePlayerVehicle.Gearbox)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic ? true : false);

                break;

            case ButtonType.GearToggleDNRP:

                if (RCCPSceneManager.activePlayerVehicle.Gearbox)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic_DNRP ? true : false);

                break;

            case ButtonType.GearToggleManual:

                if (RCCPSceneManager.activePlayerVehicle.Gearbox)
                    imageOn.SetActive(RCCPSceneManager.activePlayerVehicle.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Manual ? true : false);

                break;

        }

    }

    private void OnDisable() {

        RCCP_Events.OnVehicleChanged -= RCCP_SceneManager_OnVehicleChanged;

    }

}
