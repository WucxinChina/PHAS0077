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
using UnityEngine.Rendering;

/// <summary>
/// Manages exhaust effects using a Particle System. This includes smoke behavior tied to throttle input 
/// and optional flame emission (with light and flare effects) when the engine is running at certain conditions.
/// </summary>
public class RCC_Exhaust : RCC_Core {

    /// <summary>
    /// Primary ParticleSystem for the smoke effect.
    /// </summary>
    private ParticleSystem particle;

    /// <summary>
    /// Emission module of the smoke ParticleSystem for controlling emission rates.
    /// </summary>
    private ParticleSystem.EmissionModule emission;

    /// <summary>
    /// ParticleSystem for flame effects (appears at high RPM or with boost).
    /// </summary>
    public ParticleSystem flame;

    /// <summary>
    /// Emission module for the flame ParticleSystem.
    /// </summary>
    private ParticleSystem.EmissionModule subEmission;

    /// <summary>
    /// Light component for the flame effect, adding illumination around the exhaust.
    /// </summary>
    private Light flameLight;

    /// <summary>
    /// Standard lens flare for the flame light.
    /// </summary>
    private LensFlare lensFlare;

#if BCG_URP || BCG_HDRP
    /// <summary>
    /// URP lens flare component, if using Universal Render Pipeline.
    /// </summary>
    public LensFlareComponentSRP lensFlareURP;
#endif

    /// <summary>
    /// Brightness level for the lens flare effect.
    /// </summary>
    public float defFlameLightIntensity = 1f;

    /// <summary>
    /// Brightness level for the lens flare effect.
    /// </summary>
    public float flareBrightness = 1f;

    /// <summary>
    /// Internal calculated brightness for the lens flare, based on distance and angle to camera.
    /// </summary>
    private float finalFlareBrightness;

    /// <summary>
    /// Counter used for timing flame appearance.
    /// </summary>
    public float flameTime = 0f;

    /// <summary>
    /// AudioSource for playing flame ignition or popping sounds.
    /// </summary>
    private AudioSource flameSource;

    /// <summary>
    /// Base color of the flame.
    /// </summary>
    public Color flameColor = Color.red;

    /// <summary>
    /// Color used when boost or NOS is active, for a more intense flame look (e.g., blue flame).
    /// </summary>
    public Color boostFlameColor = Color.blue;

    /// <summary>
    /// Enables continuous flame preview in the editor.
    /// </summary>
    public bool previewFlames = false;

    /// <summary>
    /// Minimum and maximum emission rates for smoke.
    /// </summary>
    public float minEmission = 5f;
    public float maxEmission = 20f;

    /// <summary>
    /// Minimum and maximum particle sizes for smoke.
    /// </summary>
    public float minSize = 1f;
    public float maxSize = 4f;

    /// <summary>
    /// Minimum and maximum particle speeds for smoke.
    /// </summary>
    public float minSpeed = .1f;
    public float maxSpeed = 1f;

    private Camera mCamera;

    void Awake() {
        // 1) grab whatever camera is Main right now
        mCamera = RCC_MainCameraProvider.MainCamera;

        // 2) listen for any camera‐tag‐swaps later on
        RCC_MainCameraProvider.OnMainCameraChanged += HandleMainCameraChanged;
    }

    void OnDestroy() {
        // clean up your subscription
        RCC_MainCameraProvider.OnMainCameraChanged -= HandleMainCameraChanged;
    }

    private void HandleMainCameraChanged(Camera cam) {
        // update your local reference
        mCamera = cam;
    }

    private void Start() {

        // If particle effects are globally disabled in RCC Settings, remove this component.
        if (Settings.dontUseAnyParticleEffects) {
            Destroy(gameObject);
            return;
        }

        // Cache the main smoke particle system and its emission module.
        particle = GetComponent<ParticleSystem>();
        emission = particle.emission;

        // If a flame ParticleSystem is assigned, configure its emission, light, and audio.
        if (flame) {
            subEmission = flame.emission;
            flameLight = flame.GetComponentInChildren<Light>();
            defFlameLightIntensity = flameLight.intensity;
            flameSource = NewAudioSource(Settings.audioMixer, gameObject, "Exhaust Flame AudioSource", 10f, 25f, .5f, Settings.exhaustFlameClips[0], false, false, false);

            // Adjust render mode for the flame light based on RCC Settings.
            if (flameLight)
                flameLight.renderMode = Settings.useOtherLightsAsVertexLights ? LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;
        }

        // Try to find a standard lens flare in children.
        lensFlare = GetComponentInChildren<LensFlare>();

#if BCG_URP || BCG_HDRP
        // Try to find a URP lens flare component in children.
        lensFlareURP = GetComponentInChildren<LensFlareComponentSRP>();
#endif

        // If a flame light exists, remove default Unity lens flare from it for custom usage.
        if (flameLight && flameLight.flare != null)
            flameLight.flare = null;

    }

    private void Update() {

        // Requires a valid vehicle controller and smoke particle system.
        if (!CarController || !particle)
            return;

        // Update smoke and flame behavior each frame.
        Smoke();
        Flame();

#if BCG_URP || BCG_HDRP
        // If lens flare is available, update it for URP or standard usage.
        if (lensFlare || lensFlareURP)
            LensFlare();
#else
        if (lensFlare)
            LensFlare();
#endif

    }

    /// <summary>
    /// Manages smoke emission based on engine running state, speed, and throttle input.
    /// </summary>
    private void Smoke() {

        // Smoke is active only when the engine is running.
        if (CarController.engineRunning) {

            ParticleSystem.MainModule main = particle.main;

            // If vehicle speed is below a threshold, enable smoke emission. Otherwise, disable it at higher speeds.
            if (CarController.speed < 20) {

                if (!emission.enabled)
                    emission.enabled = true;

                // Higher throttle input increases smoke intensity.
                if (CarController.throttleInput > .35f) {
                    emission.rateOverTime = maxEmission;
                    main.startSpeed = maxSpeed;
                    main.startSize = maxSize;
                } else {
                    emission.rateOverTime = minEmission;
                    main.startSpeed = minSpeed;
                    main.startSize = minSize;
                }

            } else {

                if (emission.enabled)
                    emission.enabled = false;

            }

        } else {

            // Disable smoke emission if engine is off.
            if (emission.enabled)
                emission.enabled = false;

        }

    }

    /// <summary>
    /// Manages flame emission, related light intensity, and flame sound effects based on RPM, throttle, and boost.
    /// </summary>
    private void Flame() {

        // Only operate flame effects if the engine is running.
        if (CarController.engineRunning) {

            ParticleSystem.MainModule main = flame.main;

            // If throttle is opened above a certain level, reset flame timer.
            if (CarController.throttleInput >= .25f)
                flameTime = 0f;

            // The flame appears if:
            // 1) Exhaust flames are enabled on the car, RPM is between 5000-5500, throttle is low, flameTime is short, or
            // 2) Boost input is high (>= 0.75), or
            // 3) previewFlames is enabled in the editor.
            if (((CarController.useExhaustFlame && CarController.engineRPM >= 5000 && CarController.engineRPM <= 5500 && CarController.throttleInput <= .25f && flameTime <= .5f)
                || CarController.boostInput >= .75f)
                || previewFlames) {

                flameTime += Time.deltaTime;
                subEmission.enabled = true;

                // Flame light intensity is random to simulate flickering.
                if (flameLight)
                    flameLight.intensity = flameSource.pitch * defFlameLightIntensity * Random.Range(.25f, 1f);

                // If boosting, use the boostFlameColor; otherwise, use the normal flame color.
                if (CarController.boostInput >= .75f && flame) {
                    main.startColor = boostFlameColor;
                    if (flameLight)
                        flameLight.color = main.startColor.color;
                } else {
                    main.startColor = flameColor;
                    if (flameLight)
                        flameLight.color = main.startColor.color;
                }

                // Play a flame sound if not already playing.
                if (!flameSource.isPlaying) {
                    flameSource.clip = Settings.exhaustFlameClips[Random.Range(0, Settings.exhaustFlameClips.Length)];
                    flameSource.Play();
                }

            } else {

                // Disable flame emission and reset light intensity.
                subEmission.enabled = false;

                if (flameLight)
                    flameLight.intensity = 0f;

                if (flameSource.isPlaying)
                    flameSource.Stop();

            }

        } else {

            // If engine is off, disable both smoke and flame.
            if (emission.enabled)
                emission.enabled = false;

            subEmission.enabled = false;

            if (flameLight)
                flameLight.intensity = 0f;

            if (flameSource.isPlaying)
                flameSource.Stop();

        }

    }

    /// <summary>
    /// Dynamically adjusts lens flare brightness and color based on the flame light intensity and camera angle/distance.
    /// </summary>
    private void LensFlare() {

        // A main camera is required to calculate lens flare brightness.
        if (!mCamera)
            return;

        // Distance and angle calculations relative to the camera.
        float distanceTocam = Vector3.Distance(transform.position, mCamera.transform.position);
        float angle = Vector3.Angle(transform.forward, mCamera.transform.position - transform.position);

        // If there's a valid angle, calculate a brightness factor based on distance and viewing angle.
        if (angle != 0)
            finalFlareBrightness = flareBrightness * (4f / distanceTocam) * ((100f - (1.11f * angle)) / 100f) / 2f;

        // Combine the final flare brightness with the flame light's intensity.
        if (flameLight) {

            if (lensFlare) {
                lensFlare.brightness = finalFlareBrightness * flameLight.intensity;
                lensFlare.color = flameLight.color;
            }

#if BCG_URP
            if (lensFlareURP)
                lensFlareURP.intensity = finalFlareBrightness * flameLight.intensity;
#endif

#if BCG_HDRP
            if (lensFlareURP)
                lensFlareURP.intensity = flameLight.intensity > .02f ? (finalFlareBrightness * flameLight.intensity / 600f) : 0f;
#endif

        }

    }

    private void OnValidate() {

#if BCG_HDRP

        Light lightComp = GetComponentInChildren<Light>();

        if (lightComp) {

            if (lightComp.intensity <= 1f)
                lightComp.intensity = 1600f;

        }

#endif

    }

}
