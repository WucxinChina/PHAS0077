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
using UnityEngine.Rendering;

/// <summary>
/// Manages exhaust smoke and flame effects. Can optionally produce a flame when the throttle is cut off (e.g., engine backfire) or under NOS boost.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Exhaust")]
public class RCCP_Exhaust : RCCP_Component {

    /// <summary>
    /// Main camera used for calculating the intesity of the lensflare.
    /// </summary>
    private Camera mainCam;

    /// <summary>
    /// If true, triggers a flame effect when throttle is cut off at high RPM.
    /// </summary>
    public bool flameOnCutOff = false;

    /// <summary>
    /// Primary smoke ParticleSystem.
    /// </summary>
    private ParticleSystem particle;

    /// <summary>
    /// Emission module for the smoke ParticleSystem.
    /// </summary>
    private ParticleSystem.EmissionModule emission;

    /// <summary>
    /// Flame ParticleSystem for backfire or boost flame.
    /// </summary>
    public ParticleSystem flame;

    /// <summary>
    /// Emission module for the flame ParticleSystem.
    /// </summary>
    private ParticleSystem.EmissionModule subEmission;

    /// <summary>
    /// Light component used to illuminate the flame effect.
    /// </summary>
    private Light flameLight;

#if !BCG_URP && !BCG_HDRP
    /// <summary>
    /// Optional LensFlare for the flame effect.
    /// </summary>
    private LensFlare lensFlare;
#else
    /// <summary>
    /// Optional LensFlare for the flame effect.
    /// </summary>
    private LensFlareComponentSRP lensFlare_SRP;
#endif

    /// <summary>
    /// Multiplier for flare brightness.
    /// </summary>
    [Min(0f)] public float flareBrightness = 1f;

    /// <summary>
    /// Final computed brightness for the LensFlare, based on camera angle and distance.
    /// </summary>
    [Min(0f)] private float finalFlareBrightness;

    /// <summary>
    /// Timer for how long the flame effect remains active when triggered.
    /// </summary>
    [Min(0f)] public float flameTime = 0f;

    /// <summary>
    /// Primary color of the flame effect (e.g., a typical orange/red backfire).
    /// </summary>
    public Color flameColor = Color.red;

    /// <summary>
    /// Alternate flame color when under NOS boost.
    /// </summary>
    public Color boostFlameColor = Color.blue;

    /// <summary>
    /// Minimum smoke emission rate.
    /// </summary>
    [Min(0f)] public float minEmission = 5f;

    /// <summary>
    /// Maximum smoke emission rate.
    /// </summary>
    [Min(0f)] public float maxEmission = 20f;

    /// <summary>
    /// Minimum smoke particle size.
    /// </summary>
    [Min(0f)] public float minSize = 1f;

    /// <summary>
    /// Maximum smoke particle size.
    /// </summary>
    [Min(0f)] public float maxSize = 4f;

    /// <summary>
    /// Minimum smoke particle speed.
    /// </summary>
    [Min(0f)] public float minSpeed = .1f;

    /// <summary>
    /// Maximum smoke particle speed.
    /// </summary>
    [Min(0f)] public float maxSpeed = 1f;

    /// <summary>
    /// True if the flame (popping/backfire) is currently active.
    /// </summary>
    public bool popping = false;

    public override void Start() {

        base.Start();

        // Get the main exhaust ParticleSystem.
        particle = GetComponent<ParticleSystem>();

        if (!particle) {

            Debug.LogError("No ParticleSystem found on this exhaust named " + transform.name + ", disabling this script!");
            enabled = false;
            return;

        }

        emission = particle.emission;

        // If a flame ParticleSystem is assigned, set up references.
        if (flame) {

            subEmission = flame.emission;
            flameLight = flame.GetComponentInChildren<Light>();

            // If a flame light exists, force it to vertex mode. 
            if (flameLight)
                flameLight.renderMode = LightRenderMode.ForceVertex;

        }

        InvokeRepeating(nameof(FindMainCamera), 0f, 1f);

#if !BCG_URP && !BCG_HDRP
        // Attempt to find a LensFlare in this object�s children.
        lensFlare = GetComponentInChildren<LensFlare>();
#else
        // Attempt to find a LensFlare in this object�s children.
        lensFlare_SRP = GetComponentInChildren<LensFlareComponentSRP>();
#endif

        // Disable the built-in flare on the Light if it exists.
        if (flameLight && flameLight.flare != null)
            flameLight.flare = null;

    }

    public void FindMainCamera() {

        mainCam = Camera.main;

    }

    private void Update() {

        // If no ParticleSystem is found (or it was disabled), skip.
        if (!particle)
            return;

        // If no Engine component found, disable exhaust emission.
        if (!CarController.Engine) {

            if (emission.enabled)
                emission.enabled = false;

            return;

        }

        Smoke();
        Flame();

        // Optional lens flare adjustments if present.
#if !BCG_URP && !BCG_HDRP
        // Built-in pipeline lens flare
        if (lensFlare)
            LensFlare();
#else
        // URP/HDRP pipeline lens flare
        if (lensFlare_SRP)
            LensFlare_SRP();
#endif

    }

    /// <summary>
    /// Manages the smoke particle emission, size, and speed based on engine state and throttle input.
    /// </summary>
    private void Smoke() {

        // Only emit smoke if the engine is running and the vehicle is below ~20 km/h. 
        if (CarController.Engine.engineRunning) {

            var main = particle.main;

            if (CarController.absoluteSpeed > 25f) {

                if (emission.enabled)
                    emission.enabled = false;

                return;

            }

            if (!emission.enabled)
                emission.enabled = true;

            emission.rateOverTime = Mathf.Clamp(maxEmission * CarController.throttleInput_V, minEmission, maxEmission);
            main.startSpeed = Mathf.Clamp(maxSpeed * CarController.throttleInput_V, minSpeed, maxSpeed);
            main.startSize = Mathf.Clamp(maxSize * CarController.throttleInput_V, minSize, maxSize);

        } else {

            if (emission.enabled)
                emission.enabled = false;

        }

    }

    /// <summary>
    /// Manages flame/backfire effects, switching color if NOS is in use.
    /// </summary>
    private void Flame() {

        if (!CarController.Engine.engineRunning) {

            if (emission.enabled)
                emission.enabled = false;

            subEmission.enabled = false;

            if (flameLight)
                flameLight.intensity = 0f;

            return;

        }

        var main = flame.main;

        // Reset flame timer if throttle is above ~25%.
        if (CarController.throttleInput_V >= .25f)
            flameTime = 0f;

        // Check conditions for cutting throttle at high RPM, or NOS usage. 
        if ((flameOnCutOff && (CarController.engineRPM >= 5000 && CarController.engineRPM <= 5500 && CarController.throttleInput_V <= .25f && flameTime <= .5f))
            || CarController.nosInput_V >= .75f) {

            popping = true;
            flameTime += Time.deltaTime;
            subEmission.enabled = true;

            if (flameLight)
                flameLight.intensity = 3f * UnityEngine.Random.Range(.25f, 1f);

            // If NOS is in use, switch flame color to boost color.
            if (CarController.nosInput_V >= .75f) {

                main.startColor = boostFlameColor;

                if (flameLight)
                    flameLight.color = main.startColor.color;

            } else {

                main.startColor = flameColor;

                if (flameLight)
                    flameLight.color = main.startColor.color;

            }

        } else {

            popping = false;
            subEmission.enabled = false;

            if (flameLight)
                flameLight.intensity = 0f;

        }

    }

#if !BCG_URP && !BCG_HDRP
    /// <summary>
    /// Built-in pipeline lens flare logic. Adjusts brightness based on camera distance and angle relative to the light.
    /// </summary>
    private void LensFlare() {

        if (!mainCam || !flameLight) {

            finalFlareBrightness = 0f;
            lensFlare.brightness = finalFlareBrightness * flameLight.intensity;
            lensFlare.color = flameLight.color;
            return;

        }

        Vector3 transformPos = transform.position;
        Vector3 transformDir = transform.forward;
        Vector3 camPos = mainCam.transform.position;

        float distanceTocam = Vector3.Distance(transformPos, camPos);
        float angle = Vector3.Angle(transformDir, camPos - transformPos);

        if (!Mathf.Approximately(angle, 0f))
            finalFlareBrightness = flareBrightness * (4f / distanceTocam) * ((300f - (3f * angle)) / 300f) / 3f;
        else
            finalFlareBrightness = flareBrightness;

        if (finalFlareBrightness < 0)
            finalFlareBrightness = 0f;

        lensFlare.brightness = finalFlareBrightness * flameLight.intensity;
        lensFlare.color = flameLight.color;

    }
#else
    /// <summary>
    /// URP/HDRP SRP lens flare logic. Adjusts brightness based on camera distance and angle relative to the light.
    /// </summary>
    private void LensFlare_SRP() {

        if (!mainCam || !flameLight) {

            finalFlareBrightness = 0f;
            lensFlare_SRP.intensity = finalFlareBrightness;
            return;

        }

        float distanceTocam = Vector3.Distance(transform.position, mainCam.transform.position);
        float angle = Vector3.Angle(transform.forward, mainCam.transform.position - transform.position);

        if (!Mathf.Approximately(angle, 0f))
            finalFlareBrightness = flareBrightness * (8f / distanceTocam) * ((300f - (3f * angle)) / 300f) / 3f;
        else
            finalFlareBrightness = flareBrightness;

        if (finalFlareBrightness < 0)
            finalFlareBrightness = 0f;

        lensFlare_SRP.attenuationByLightShape = false;
        lensFlare_SRP.intensity = finalFlareBrightness * flameLight.intensity;

    }
#endif

}
