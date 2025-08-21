//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Color Picker with UI Sliders.
/// </summary>
public class RCC_ColorPickerBySliders : RCC_Core {

    /// <summary>
    /// Color types.
    /// </summary>
    public ColorType colorType = ColorType.WheelSmoke;
    public enum ColorType {

        WheelSmoke,
        Headlights

    }

    /// <summary>
    /// Main color.
    /// </summary>
    public Color color;
    private Color oldColor;

    /// <summary>
    /// Sliders per color channel.
    /// </summary>
    public Slider redSlider;

    /// <summary>
    /// Sliders per color channel.
    /// </summary>
    public Slider greenSlider;

    /// <summary>
    /// Sliders per color channel.
    /// </summary>
    public Slider blueSlider;

    private void OnEnable() {

        //  Finding the player vehicle.
        RCC_CarControllerV4 playerVehicle = RCCSceneManager.activePlayerVehicle;

        //  If player vehicle doesn't have the customizer component, return.
        if (playerVehicle && playerVehicle.Customizer) {

            switch (colorType) {

                case ColorType.Headlights:

                    for (int i = 0; i < playerVehicle.AllLights.Length; i++) {

                        if (playerVehicle.AllLights[i].lightType == RCC_Light.LightType.HeadLight) {

                            color = playerVehicle.AllLights[i].LightSource.color;
                            break;

                        }

                    }
                    break;

                case ColorType.WheelSmoke:

                    for (int i = 0; i < playerVehicle.AllWheelColliders.Length; i++) {

                        if (playerVehicle.AllWheelColliders[i] != null) {

                            foreach (ParticleSystem wheelParticle in playerVehicle.AllWheelColliders[i].GetComponentsInChildren<ParticleSystem>(true)) {

                                if (wheelParticle.transform.GetSiblingIndex() == 0) {

                                    ParticleSystem.MainModule psmain = wheelParticle.main;
                                    color = psmain.startColor.color;
                                    break;

                                }

                            }

                        }

                    }
                    break;

            }

        }

        oldColor = color;

        redSlider.SetValueWithoutNotify(color.r);
        greenSlider.SetValueWithoutNotify(color.g);
        blueSlider.SetValueWithoutNotify(color.b);

    }

    private void Update() {

        // Assigning new color to main color.
        color = new Color(redSlider.value, greenSlider.value, blueSlider.value);

        if (oldColor != color) {

            if (!enabled)
                return;

            //  Finding the player vehicle.
            RCC_CarControllerV4 playerVehicle = RCCSceneManager.activePlayerVehicle;

            //  If no player vehicle found, return.
            if (!playerVehicle)
                return;

            //  If player vehicle doesn't have the customizer component, return.
            if (!playerVehicle.Customizer)
                return;

            switch (colorType) {

                case ColorType.Headlights:

                    //  If player vehicle doesn't have the decal manager component, return.
                    if (!playerVehicle.Customizer.CustomizationManager)
                        return;

                    //  Set the decal.
                    playerVehicle.Customizer.CustomizationManager.SetHeadlightsColor(color);

                    break;

                case ColorType.WheelSmoke:

                    //  If player vehicle doesn't have the decal manager component, return.
                    if (!playerVehicle.Customizer.CustomizationManager)
                        return;

                    //  Set the decal.
                    playerVehicle.Customizer.CustomizationManager.SetSmokeColor(color);

                    break;

            }

        }

        oldColor = color;

    }

}
