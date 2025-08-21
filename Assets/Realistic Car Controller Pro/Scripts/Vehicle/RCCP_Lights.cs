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
using System.Linq;

/// <summary>
/// Main light manager for an RCCP vehicle. 
/// Gathers and updates all RCCP_Light components (headlights, brake lights, reverse lights, indicators, etc.).
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Lights")]
public class RCCP_Lights : RCCP_Component {

    /// <summary>
    /// List of all vehicle lights associated with this manager.
    /// </summary>
    public List<RCCP_Light> lights = new List<RCCP_Light>();

    /// <summary>
    /// Returns all headlight-type lights (low beam and high beam).
    /// </summary>
    public RCCP_Light[] Headlights {

        get {

            List<RCCP_Light> headlights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.Headlight_LowBeam || lights[i].lightType == RCCP_Light.LightType.Headlight_HighBeam)
                        headlights.Add(lights[i]);

                }

            }

            return headlights.ToArray();

        }

    }

    /// <summary>
    /// Returns brake-type lights (includes both brakelights and taillights).
    /// </summary>
    public RCCP_Light[] Brakelights {

        get {

            List<RCCP_Light> brakelights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.Brakelight || lights[i].lightType == RCCP_Light.LightType.Taillight)
                        brakelights.Add(lights[i]);

                }

            }

            return brakelights.ToArray();

        }

    }

    /// <summary>
    /// Returns reverse-type lights.
    /// </summary>
    public RCCP_Light[] Reverselights {

        get {

            List<RCCP_Light> reverselights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.Reverselight)
                        reverselights.Add(lights[i]);

                }

            }

            return reverselights.ToArray();

        }

    }

    /// <summary>
    /// Returns indicator (turn signal) lights (includes both left and right).
    /// </summary>
    public RCCP_Light[] Indicatorlights {

        get {

            List<RCCP_Light> indicatorlights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.IndicatorLeftLight || lights[i].lightType == RCCP_Light.LightType.IndicatorRightLight)
                        indicatorlights.Add(lights[i]);

                }

            }

            return indicatorlights.ToArray();

        }

    }

    /// <summary>
    /// True if low beam headlights are currently on.
    /// </summary>
    public bool lowBeamHeadlights = false;

    /// <summary>
    /// True if high beam headlights are currently on.
    /// </summary>
    public bool highBeamHeadlights = false;

    /// <summary>
    /// True if brake lights are currently on.
    /// </summary>
    public bool brakeLights = false;

    /// <summary>
    /// True if reverse lights are currently on.
    /// </summary>
    public bool reverseLights = false;

    /// <summary>
    /// True if left indicator is active.
    /// </summary>
    public bool indicatorsLeft = false;

    /// <summary>
    /// True if right indicator is active.
    /// </summary>
    public bool indicatorsRight = false;

    /// <summary>
    /// True if hazard lights (all indicators) are active.
    /// </summary>
    public bool indicatorsAll = false;

    /// <summary>
    /// Used by lights to blink indicators. 0..1 timer repeated each second.
    /// </summary>
    public float indicatorTimer = 0f;

    /// <summary>
    /// True if any taillight is present (used to blend brake intensity with tail intensity).
    /// </summary>
    public bool tailLightFound = false;

    /// <summary>
    /// True if any high-beam light is present on the vehicle.
    /// </summary>
    public bool highBeamLightFound = false;

    /// <summary>
    /// If true, user must turn on low beams before high beams can be activated.
    /// </summary>
    public bool highBeamWithLowBeamOnly = false;

    /// <summary>
    /// Finds all RCCP_Light components under this vehicle and updates the manager�s lights list.
    /// </summary>
    public void GetAllLights() {

        if (lights == null)
            lights = new List<RCCP_Light>();

        lights.Clear();
        lights = GetComponentsInChildren<RCCP_Light>(true).ToList();

    }

    private void Update() {

        CheckLights();
        Inputs();
        IndicatorTimer();

    }

    /// <summary>
    /// Removes any null references from the lights list.
    /// </summary>
    private void CheckLights() {

        if (lights != null) {

            for (int i = 0; i < lights.Count; i++) {

                if (lights[i] == null)
                    lights.RemoveAt(i);

            }

        }

    }

    /// <summary>
    /// Registers a new RCCP_Light component with this manager. 
    /// Also sets internal flags if the light is a taillight or high beam.
    /// </summary>
    /// <param name="newLight">New RCCP_Light to add.</param>
    public void RegisterLight(RCCP_Light newLight) {

        if (!lights.Contains(newLight))
            lights.Add(newLight);

        // If we detect a taillight, note it so brake logic can consider partial tail intensity.
        if (newLight.lightType == RCCP_Light.LightType.Taillight)
            tailLightFound = true;

        if (newLight.lightType == RCCP_Light.LightType.Headlight_HighBeam)
            highBeamLightFound = true;

    }

    /// <summary>
    /// Interprets vehicle states (braking, reversing) and toggles brakeLights/reverseLights booleans accordingly.
    /// </summary>
    private void Inputs() {

        if (CarController.brakeInput_V >= .1f)
            brakeLights = true;
        else
            brakeLights = false;

        if (CarController.reversingNow)
            reverseLights = true;
        else
            reverseLights = false;

    }

    /// <summary>
    /// Keeps an indicator blink timer running if turn signals or hazards are active. Resets if not in use.
    /// </summary>
    private void IndicatorTimer() {

        if (indicatorsLeft || indicatorsRight || indicatorsAll)
            indicatorTimer += Time.deltaTime;
        else
            indicatorTimer = 0f;

        if (indicatorTimer >= 1f)
            indicatorTimer = 0f;

    }

    /// <summary>
    /// Used when reloading/resetting vehicle states to clear the indicator blink timer.
    /// </summary>
    public void Reload() {

        indicatorTimer = 0f;

    }

}
