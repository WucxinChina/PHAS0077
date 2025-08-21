//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Receives data from the active vehicle in the scene and updates dashboard gauges (needles, texts, images).
/// This includes speed, RPM, turbo, NoS, heat, fuel, and lighting indicators.
/// </summary>
public class RCC_DashboardInputs : RCC_Core {

    /// <summary>
    /// The target vehicle from which data is retrieved for the dashboard.
    /// </summary>
    public RCC_CarControllerV4 vehicle;     //  Target vehicle.

    /// <summary>
    /// If true, automatically assigns the active vehicle from RCC_SceneManager as the target vehicle.
    /// </summary>
    public bool autoAssignVehicle = true;       //  Auto assign target vehicle as player vehicle from the RCC_SceneManager.

    //  Needles.
    [Header("Needles")]
    /// <summary>
    /// The RPM needle in the dashboard.
    /// </summary>
    public GameObject RPMNeedle;

    /// <summary>
    /// The speed (KMH/MPH) needle in the dashboard.
    /// </summary>
    public GameObject KMHNeedle;

    /// <summary>
    /// The turbo gauge parent object. Will be enabled/disabled based on the vehicle's turbo usage.
    /// </summary>
    public GameObject turboGauge;

    /// <summary>
    /// The needle object inside the turbo gauge.
    /// </summary>
    public GameObject turboNeedle;

    /// <summary>
    /// The NoS gauge parent object. Will be enabled/disabled based on the vehicle's NOS usage.
    /// </summary>
    public GameObject NOSGauge;

    /// <summary>
    /// The needle object inside the NoS gauge.
    /// </summary>
    public GameObject NoSNeedle;

    /// <summary>
    /// The heat gauge parent object. Will be enabled/disabled based on the vehicle's engine heat usage.
    /// </summary>
    public GameObject heatGauge;

    /// <summary>
    /// The needle object inside the heat gauge.
    /// </summary>
    public GameObject heatNeedle;

    /// <summary>
    /// The fuel gauge parent object. Will be enabled/disabled based on the vehicle's fuel consumption usage.
    /// </summary>
    public GameObject fuelGauge;

    /// <summary>
    /// The needle object inside the fuel gauge.
    /// </summary>
    public GameObject fuelNeedle;

    //  Needle rotations.

    /// <summary>
    /// Current rotation value for the RPM needle.
    /// </summary>
    private float RPMNeedleRotation = 0f;

    /// <summary>
    /// Current rotation value for the KMH needle.
    /// </summary>
    private float KMHNeedleRotation = 0f;

    /// <summary>
    /// Current rotation value for the turbo needle.
    /// </summary>
    private float BoostNeedleRotation = 0f;

    /// <summary>
    /// Current rotation value for the NoS needle.
    /// </summary>
    private float NoSNeedleRotation = 0f;

    /// <summary>
    /// Current rotation value for the heat needle.
    /// </summary>
    private float heatNeedleRotation = 0f;

    /// <summary>
    /// Current rotation value for the fuel needle.
    /// </summary>
    private float fuelNeedleRotation = 0f;

    //  Variables of the player vehicle.

    /// <summary>
    /// Current RPM of the vehicle.
    /// </summary>
    [HideInInspector] public float RPM;

    /// <summary>
    /// Current speed (KMH or MPH) of the vehicle.
    /// </summary>
    [HideInInspector] public float KMH;

    /// <summary>
    /// Direction of the vehicle. 1 = forward, -1 = reverse.
    /// </summary>
    [HideInInspector] public int direction = 1;

    /// <summary>
    /// Current gear of the vehicle's transmission.
    /// </summary>
    [HideInInspector] public float Gear;

    /// <summary>
    /// Indicates whether the vehicle is in the process of changing gears.
    /// </summary>
    [HideInInspector] public bool changingGear = false;

    /// <summary>
    /// Indicates whether the vehicle is currently in neutral gear.
    /// </summary>
    [HideInInspector] public bool NGear = false;

    /// <summary>
    /// Indicates whether ABS (Anti-lock Braking System) is actively controlling the brakes.
    /// </summary>
    [HideInInspector] public bool ABS = false;

    /// <summary>
    /// Indicates whether ESP (Electronic Stability Program) is actively engaged.
    /// </summary>
    [HideInInspector] public bool ESP = false;

    /// <summary>
    /// Indicates whether the handbrake (parking brake) is engaged.
    /// </summary>
    [HideInInspector] public bool Park = false;

    /// <summary>
    /// Indicates if the vehicle's headlights (low or high beams) are turned on.
    /// </summary>
    [HideInInspector] public bool Headlights = false;

    /// <summary>
    /// Current state of the vehicle's turn signals.
    /// </summary>
    [HideInInspector] public RCC_CarControllerV4.IndicatorsOn indicators;

    private void Update() {

        // If autoAssignVehicle is true and an active player vehicle is present, assign it to 'vehicle'.
        if (autoAssignVehicle && RCCSceneManager.activePlayerVehicle)
            vehicle = RCCSceneManager.activePlayerVehicle;
        else
            vehicle = null;

        // If no player vehicle is found, return.
        if (!vehicle)
            return;

        // If the vehicle cannot be controlled or is controlled by AI, return.
        if (!vehicle.canControl || vehicle.externalController)
            return;

        // Manage visibility of the NoS gauge based on whether NoS is used by the vehicle.
        if (NOSGauge) {

            if (vehicle.useNOS) {

                if (!NOSGauge.activeSelf)
                    NOSGauge.SetActive(true);

            } else {

                if (NOSGauge.activeSelf)
                    NOSGauge.SetActive(false);

            }

        }

        // Manage visibility of the turbo gauge based on whether turbo is used by the vehicle.
        if (turboGauge) {

            if (vehicle.useTurbo) {

                if (!turboGauge.activeSelf)
                    turboGauge.SetActive(true);

            } else {

                if (turboGauge.activeSelf)
                    turboGauge.SetActive(false);

            }

        }

        // Manage visibility of the heat gauge based on whether engine heat is used by the vehicle.
        if (heatGauge) {

            if (vehicle.useEngineHeat) {

                if (!heatGauge.activeSelf)
                    heatGauge.SetActive(true);

            } else {

                if (heatGauge.activeSelf)
                    heatGauge.SetActive(false);

            }

        }

        // Manage visibility of the fuel gauge based on whether fuel consumption is used by the vehicle.
        if (fuelGauge) {

            if (vehicle.useFuelConsumption) {

                if (!fuelGauge.activeSelf)
                    fuelGauge.SetActive(true);

            } else {

                if (fuelGauge.activeSelf)
                    fuelGauge.SetActive(false);

            }

        }

        // Fetch various parameters from the vehicle.
        RPM = vehicle.engineRPM;
        KMH = vehicle.speed;
        direction = vehicle.direction;
        Gear = vehicle.currentGear;
        changingGear = vehicle.changingGear;
        NGear = vehicle.NGear;
        ABS = vehicle.ABSAct;
        ESP = vehicle.ESPAct;
        Park = vehicle.handbrakeInput > .1f ? true : false;
        Headlights = vehicle.lowBeamHeadLightsOn || vehicle.highBeamHeadLightsOn;
        indicators = vehicle.indicatorsOn;

        // If RPM needle exists, rotate it according to the engine RPM.
        if (RPMNeedle) {

            RPMNeedleRotation = (vehicle.engineRPM / 50f);
            RPMNeedleRotation = Mathf.Clamp(RPMNeedleRotation, 0f, 180f);
            RPMNeedle.transform.eulerAngles = new Vector3(RPMNeedle.transform.eulerAngles.x, RPMNeedle.transform.eulerAngles.y, -RPMNeedleRotation);

        }

        // If KMH needle exists, rotate it according to speed, depending on KMH or MPH settings.
        if (KMHNeedle) {

            if (Settings.units == RCC_Settings.Units.KMH)
                KMHNeedleRotation = (vehicle.speed);
            else
                KMHNeedleRotation = (vehicle.speed * 0.62f);

            KMHNeedle.transform.eulerAngles = new Vector3(KMHNeedle.transform.eulerAngles.x, KMHNeedle.transform.eulerAngles.y, -KMHNeedleRotation);

        }

        // If turbo needle exists, rotate it according to the turbo boost level.
        if (turboNeedle) {

            BoostNeedleRotation = (vehicle.turboBoost / 30f) * 270f;
            turboNeedle.transform.eulerAngles = new Vector3(turboNeedle.transform.eulerAngles.x, turboNeedle.transform.eulerAngles.y, -BoostNeedleRotation);

        }

        // If NoS needle exists, rotate it according to the NoS remaining level.
        if (NoSNeedle) {

            NoSNeedleRotation = (vehicle.NoS / 100f) * 270f;
            NoSNeedle.transform.eulerAngles = new Vector3(NoSNeedle.transform.eulerAngles.x, NoSNeedle.transform.eulerAngles.y, -NoSNeedleRotation);

        }

        // If heat needle exists, rotate it according to the engine heat level.
        if (heatNeedle) {

            heatNeedleRotation = (vehicle.engineHeat / 110f) * 270f;
            heatNeedle.transform.eulerAngles = new Vector3(heatNeedle.transform.eulerAngles.x, heatNeedle.transform.eulerAngles.y, -heatNeedleRotation);

        }

        // If fuel needle exists, rotate it according to the fuel remaining level.
        if (fuelNeedle) {

            fuelNeedleRotation = (vehicle.fuelTank / vehicle.fuelTankCapacity) * 270f;
            fuelNeedle.transform.eulerAngles = new Vector3(fuelNeedle.transform.eulerAngles.x, fuelNeedle.transform.eulerAngles.y, -fuelNeedleRotation);

        }

    }

}
