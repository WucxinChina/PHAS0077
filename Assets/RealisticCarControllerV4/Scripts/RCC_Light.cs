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
using UnityEngine.Rendering;

/// <summary>
/// Handles all types of vehicle lighting in RCC, including headlights, brake lights, 
/// indicators, reverse lights, interior lights, and more. This script manages intensity, 
/// lens flare, trail rendering, and damage/breakage for each light.
/// </summary>
[RequireComponent(typeof(Light))]
public class RCC_Light : RCC_Core {

    #region Light Component and Properties

    /// <summary>
    /// Reference to the Unity Light component. Lazily loaded.
    /// </summary>
    public Light LightSource {

        get {

            if (_light == null)
                _light = GetComponent<Light>();

            return _light;

        }

    }
    private Light _light;

    /// <summary>
    /// Optional lens flare component for standard rendering pipelines.
    /// </summary>
    public LensFlare lensFlare;

#if BCG_URP || BCG_HDRP
    /// <summary>
    /// Optional URP-based lens flare component.
    /// </summary>
    public LensFlareComponentSRP lensFlareURP;
#endif

    /// <summary>
    /// Optional trail renderer for some lights (e.g., neon under glow).
    /// </summary>
    public TrailRenderer trail;

    #endregion

    #region Light Configuration Fields

    /// <summary>
    /// Default intensity used when the light is fully on.
    /// </summary>
    public float defaultIntensity = 1f;

    /// <summary>
    /// Maximum brightness factor for the lens flare.
    /// </summary>
    public float flareBrightness = 1.5f;

    /// <summary>
    /// Internally calculated brightness for the lens flare, adjusted by distance and angle.
    /// </summary>
    private float finalFlareBrightness;

    /// <summary>
    /// The category of light this component represents (headlight, brake, reverse, etc.).
    /// </summary>
    public LightType lightType = LightType.HeadLight;
    public enum LightType {

        HeadLight,
        BrakeLight,
        ReverseLight,
        Indicator,
        ParkLight,
        HighBeamHeadLight,
        External,
        Interior

    }

    /// <summary>
    /// Smoothing factor for the light's intensity transitions.
    /// </summary>
    public float inertia = 1f;

    /// <summary>
    /// Render mode (vertex or pixel). Can be overridden or auto-set based on RCC Settings.
    /// </summary>
    public LightRenderMode renderMode = LightRenderMode.Auto;

    /// <summary>
    /// If true, the user manually sets the render mode. If false, RCC applies a recommended mode.
    /// </summary>
    public bool overrideRenderMode = false;

    /// <summary>
    /// Lens flare asset to be used by this light (if lensFlare is set).
    /// </summary>
    public Flare flare;

    /// <summary>
    /// Frequency (in frames per second) at which lens flare calculations are updated.
    /// Higher values mean more frequent updates but higher performance cost.
    /// </summary>
    public int refreshRate = 30;
    private float refreshTimer = 0f;

    /// <summary>
    /// Indicates if the vehicle also has a separate park light, which affects brake light logic.
    /// </summary>
    private bool parkLightFound = false;

    /// <summary>
    /// Indicates if the vehicle also has a dedicated high beam light, which affects normal headlight logic.
    /// </summary>
    private bool highBeamLightFound = false;

    /// <summary>
    /// Emission settings that illuminate the associated mesh or texture, if applicable.
    /// </summary>
    public RCC_Emission[] emission;

    /// <summary>
    /// If true, uses the emission texture to visually enhance brightness on the light’s mesh.
    /// </summary>
    public bool useEmissionTexture = false;

    /// <summary>
    /// Current strength of this light. Collisions can reduce strength, simulating breakage.
    /// </summary>
    public float strength = 100f;

    /// <summary>
    /// Stores the original strength for reset/repair operations.
    /// </summary>
    private float orgStrength = 100f;

    /// <summary>
    /// If true, the light can be broken (by collisions). If false, it’s indestructible.
    /// </summary>
    public bool isBreakable = true;

    /// <summary>
    /// Light breaks when strength is equal or below this threshold.
    /// </summary>
    public int breakPoint = 35;

    /// <summary>
    /// Tracks whether the light is currently broken, which prevents illumination.
    /// </summary>
    private bool broken = false;

    // Used specifically by indicators
    private RCC_CarControllerV4.IndicatorsOn indicatorsOn;
    private AudioSource indicatorSound;

    /// <summary>
    /// The indicator audio clip from RCC settings, played when indicator lights blink.
    /// </summary>
    public AudioClip IndicatorClip { get { return Settings.indicatorClip; } }

    #endregion

    private Camera mCamera;

    private void HandleMainCameraChanged(Camera cam) {
        // update your local reference
        mCamera = cam;
    }

    private void Awake() {

        // Perform initial setup if this light is attached to a vehicle.
        Initialize();

        // 1) grab whatever camera is Main right now
        mCamera = RCC_MainCameraProvider.MainCamera;

        // 2) listen for any camera‐tag‐swaps later on
        RCC_MainCameraProvider.OnMainCameraChanged += HandleMainCameraChanged;

    }

    void OnDestroy() {
        // clean up your subscription
        RCC_MainCameraProvider.OnMainCameraChanged -= HandleMainCameraChanged;
    }

    /// <summary>
    /// Initializes the light by configuring render mode, lens flare, audio sources, etc.
    /// </summary>
    public void Initialize() {

        lensFlare = GetComponent<LensFlare>();
#if BCG_URP || BCG_HDRP
        lensFlareURP = GetComponent<LensFlareComponentSRP>();
#endif
        trail = GetComponentInChildren<TrailRenderer>();

        LightSource.enabled = true;

        // If defaultIntensity wasn't set, store the current Light intensity as default.
        defaultIntensity = LightSource.intensity;
        orgStrength = strength;

        // If the lens flare components exist, set up initial brightness and color.
        if (lensFlare) {

            lensFlare.brightness = 0f;
            lensFlare.color = Color.white;
            lensFlare.fadeSpeed = 20f;

            // Move the Light's lens flare to our dedicated lensFlare object if assigned.
            if (LightSource.flare != null)
                LightSource.flare = null;

            lensFlare.flare = flare;

        }

#if BCG_URP || BCG_HDRP
        if (lensFlareURP)
            lensFlareURP.intensity = 0f;
#endif

        // Auto-choose render mode if not overridden by the user.
        if (!overrideRenderMode) {

            switch (lightType) {

                case LightType.HeadLight:
                    renderMode = Settings.useHeadLightsAsVertexLights ?
                        LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;
                    break;
                case LightType.BrakeLight:
                    renderMode = Settings.useBrakeLightsAsVertexLights ?
                        LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;
                    break;
                case LightType.ReverseLight:
                    renderMode = Settings.useReverseLightsAsVertexLights ?
                        LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;
                    break;
                case LightType.Indicator:
                    renderMode = Settings.useIndicatorLightsAsVertexLights ?
                        LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;
                    break;
                case LightType.ParkLight:
                case LightType.External:
                    renderMode = Settings.useOtherLightsAsVertexLights ?
                        LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;
                    break;
                    // HighBeamHeadLight and Interior do not have specific RCC toggles by default

            }

        }

        LightSource.renderMode = renderMode;

        // If attached to a vehicle, set up additional logic (like audio source for indicators).
        if (CarController) {

            if (lightType == LightType.Indicator) {

                // Attempt to find/create audio source for indicator sounds on the vehicle.
                if (!CarController.transform.Find("All Audio Sources/Indicator Sound AudioSource")) {

                    indicatorSound = NewAudioSource(
                        Settings.audioMixer,
                        CarController.gameObject,
                        "Indicator Sound AudioSource",
                        1f,
                        3f,
                        1f,
                        IndicatorClip,
                        false,
                        false,
                        false
                    );

                } else {

                    indicatorSound = CarController.transform.Find("All Audio Sources/Indicator Sound AudioSource")
                                                          .GetComponent<AudioSource>();

                }

            }

            // Collect all lights in this vehicle to check if there are park lights or high beams.
            RCC_Light[] allLights = CarController.AllLights;

            for (int i = 0; i < allLights.Length; i++) {

                if (allLights[i].lightType == LightType.ParkLight)
                    parkLightFound = true;

                if (allLights[i].lightType == LightType.HighBeamHeadLight)
                    highBeamLightFound = true;

            }

        }

    }

    private void OnEnable() {

        // Ensure the light is off when first enabled.
        LightSource.intensity = 0f;

    }

    private void Update() {

        if (!CarController)
            return;

#if BCG_URP || BCG_HDRP
        if (lensFlare || lensFlareURP)
            LensFlare();
#else
        if (lensFlare)
            LensFlare();
#endif

        if (trail)
            TrailRenderer();

        if (useEmissionTexture) {

            foreach (RCC_Emission item in emission) {

                item.Emission(LightSource);

            }

        }

        // If the light has been "broken", it won't illuminate at all.
        if (broken) {

            Lighting(0f);
            return;

        }

        // Determine intensity based on the lightType and vehicle state.
        switch (lightType) {

            case LightType.HeadLight:
                if (highBeamLightFound) {

                    Lighting(CarController.lowBeamHeadLightsOn ? defaultIntensity : 0f, 50f, 90f);
                } else {

                    // Normal headlights with potential high beam override
                    if (!CarController.lowBeamHeadLightsOn && !CarController.highBeamHeadLightsOn)
                        Lighting(0f);
                    else if (CarController.lowBeamHeadLightsOn && !CarController.highBeamHeadLightsOn) {

                        Lighting(defaultIntensity, 50f, 90f);
                        transform.localEulerAngles = new Vector3(10f, 0f, 0f);

                    } else if (CarController.highBeamHeadLightsOn) {

                        Lighting(defaultIntensity, 100f, 45f);
                        transform.localEulerAngles = new Vector3(0f, 0f, 0f);

                    }

                }
                break;

            case LightType.BrakeLight:
                // If park lights exist, brake lights won’t always be lit except when braking.
                if (parkLightFound)
                    Lighting(CarController.brakeInput >= .1f ? defaultIntensity : 0f);
                else
                    Lighting(CarController.brakeInput >= .1f ? defaultIntensity : !CarController.lowBeamHeadLightsOn ? 0f : .25f);
                break;

            case LightType.ReverseLight:
                // Reverse light is on if the vehicle is moving in reverse (-1).
                Lighting(CarController.direction == -1 ? defaultIntensity : 0f);
                break;

            case LightType.ParkLight:
                // Park lights are illuminated if low beams are on.
                Lighting(!CarController.lowBeamHeadLightsOn ? 0f : defaultIntensity);
                break;

            case LightType.Indicator:
                indicatorsOn = CarController.indicatorsOn;
                Indicators();
                break;

            case LightType.HighBeamHeadLight:
                // High beams only when explicitly turned on.
                Lighting(CarController.highBeamHeadLightsOn ? defaultIntensity : 0f, 200f, 45f);
                break;

            case LightType.Interior:
                // Interior lights can be toggled independently.
                Lighting(CarController.interiorLightsOn ? defaultIntensity : 0f);
                break;

            case LightType.External:
                // External lights are not commonly toggled by default but can be lit via custom script.
                // Intensity is not auto-managed here unless user code modifies it.
                break;

        }

    }

    /// <summary>
    /// Gradually sets this light's intensity, smoothly interpolating based on inertia.
    /// </summary>
    /// <param name="input">Target intensity value.</param>
    private void Lighting(float input) {

        if (input >= .05f)
            LightSource.intensity = Mathf.Lerp(LightSource.intensity, input, Time.deltaTime * inertia * 20f);
        else
            LightSource.intensity = 0f;

    }

    /// <summary>
    /// Sets this light's intensity, range, and spot angle, smoothly interpolating intensity.
    /// </summary>
    /// <param name="input">Target intensity value.</param>
    /// <param name="range">Desired light range.</param>
    /// <param name="spotAngle">Desired spot angle (for spotlights).</param>
    private void Lighting(float input, float range, float spotAngle) {

        if (input >= .05f)
            LightSource.intensity = Mathf.Lerp(LightSource.intensity, input, Time.deltaTime * inertia * 20f);
        else
            LightSource.intensity = 0f;

        LightSource.range = range;
        LightSource.spotAngle = spotAngle;

    }

    /// <summary>
    /// Handles indicator blink logic. Switches the light on/off based on the vehicle's blink timer.
    /// Also handles indicator audio if applicable.
    /// </summary>
    private void Indicators() {

        // Determine if this is the left or right indicator by comparing local and vehicle positions.
        Vector3 relativePos = CarController.transform.InverseTransformPoint(transform.position);

        switch (indicatorsOn) {

            case RCC_CarControllerV4.IndicatorsOn.Left:
                if (relativePos.x > 0f) {

                    Lighting(0);
                    break;

                }
                BlinkIndicator();
                break;

            case RCC_CarControllerV4.IndicatorsOn.Right:
                if (relativePos.x < 0f) {

                    Lighting(0);
                    break;

                }
                BlinkIndicator();
                break;

            case RCC_CarControllerV4.IndicatorsOn.All:
                BlinkIndicator();
                break;

            case RCC_CarControllerV4.IndicatorsOn.Off:
                Lighting(0);
                CarController.indicatorTimer = 0f;
                break;

        }

    }

    /// <summary>
    /// Blinks the indicator light according to the vehicle's indicatorTimer, controlling audio playback as well.
    /// </summary>
    private void BlinkIndicator() {

        if (CarController.indicatorTimer >= .5f) {

            // Light off for the second half of the cycle.
            Lighting(0);

            if (indicatorSound && indicatorSound.isPlaying)
                indicatorSound.Stop();

        } else {

            // Light on for the first half of the cycle.
            Lighting(defaultIntensity);

            if (indicatorSound && !indicatorSound.isPlaying && CarController.indicatorTimer <= .05f)
                indicatorSound.Play();

        }

        if (CarController.indicatorTimer >= 1f)
            CarController.indicatorTimer = 0f;

    }

    /// <summary>
    /// Updates the lens flare brightness based on camera distance and angle. 
    /// Uses a fixed refresh rate to reduce performance overhead.
    /// </summary>
    private void LensFlare() {

        if (refreshTimer > (1f / refreshRate)) {

            refreshTimer = 0f;

            if (!mCamera)
                return;

            float distanceTocam = Vector3.Distance(transform.position, mCamera.transform.position);

            if (lightType != LightType.External && lightType != LightType.Interior) {

                // For standard lights, angle influences flare brightness more strongly.
                float angle = Vector3.Angle(transform.forward, mCamera.transform.position - transform.position);
                finalFlareBrightness = flareBrightness * (4f / distanceTocam) * ((300f - (3f * angle)) / 300f) / 3f;

            } else {

                // External or interior lights have simpler fallback logic.
                finalFlareBrightness = flareBrightness * (4f / distanceTocam) / 3f;

            }

            // Apply final flare brightness multiplied by the light intensity.
            if (lensFlare) {

                lensFlare.brightness = finalFlareBrightness * LightSource.intensity;
                lensFlare.color = LightSource.color;

            }

#if BCG_URP
            if (lensFlareURP)
                lensFlareURP.intensity = finalFlareBrightness * LightSource.intensity;
#endif
#if BCG_HDRP
            if (lensFlareURP)
                lensFlareURP.intensity = LightSource.intensity > .02f ? (finalFlareBrightness * LightSource.intensity / 600f) : 0f;
#endif

        }

        refreshTimer += Time.deltaTime;

    }

    /// <summary>
    /// Toggles emission of the trail renderer based on the light’s intensity, and syncs color.
    /// </summary>
    private void TrailRenderer() {

        trail.emitting = LightSource.intensity > .1f;
        trail.startColor = LightSource.color;

    }

    /// <summary>
    /// Ensures the light's orientation is correct based on whether it's mounted at the front or back of the vehicle.
    /// </summary>
    private void CheckRotation() {

        Vector3 relativePos = CarController.transform.InverseTransformPoint(transform.position);

        if (relativePos.z > 0f) {

            // If the light is at the front, ensure local Y rotation is near 0.
            if (Mathf.Abs(transform.localRotation.y) > .5f)
                transform.localRotation = Quaternion.identity;
        } else {

            // If the light is at the rear, ensure local Y rotation is near 180 degrees.
            if (Mathf.Abs(transform.localRotation.y) < .5f)
                transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        }

    }

    #region Damage and Repair

    /// <summary>
    /// Called by the vehicle's damage system upon collision. Decreases the light's strength and may break it.
    /// </summary>
    /// <param name="impulse">Magnitude of the collision impulse.</param>
    public void OnCollision(float impulse) {

        if (broken || !isBreakable)
            return;

        strength -= impulse * 20f;
        strength = Mathf.Clamp(strength, 0f, Mathf.Infinity);

        if (strength <= breakPoint)
            broken = true;

    }

    /// <summary>
    /// Restores the light to its original strength and un-broken state.
    /// </summary>
    public void OnRepair() {

        strength = orgStrength;
        broken = false;

    }

    #endregion

    private void OnDisable() {

        // Turn off the light when this component is disabled.
        LightSource.intensity = 0f;

    }

    private void Reset() {

        // Ensures correct local rotation if front/rear oriented incorrectly in prefab.
        CheckRotation();

    }

    private void OnValidate() {

        // Ensures emission multipliers are not zero.
        if (emission != null) {

            foreach (RCC_Emission item in emission) {

                if (item == null)
                    continue;

                if (item.multiplier == 0)
                    item.multiplier = 1f;

#if BCG_HDRP

                Light lightComp = GetComponent<Light>();

                if (lightComp) {

                    if (lightComp.intensity <= 1f)
                        lightComp.intensity = 600f;

                    if (lightType == LightType.HeadLight && lightComp.intensity <= 600f)
                        lightComp.intensity = 1600f;

                }

                // HDRP Lit shader expects "_EmissiveColor" rather than "_EmissionColor"
                item.shaderKeywordEmissionEnable = string.IsNullOrEmpty(item.shaderKeywordEmissionEnable)
                    ? "_EMISSION"
                    : item.shaderKeywordEmissionEnable;

                item.shaderKeywordEmissionColor = string.IsNullOrEmpty(item.shaderKeywordEmissionColor) || item.shaderKeywordEmissionColor == "_EmissionColor"
                    ? "_EmissiveColor"
                    : item.shaderKeywordEmissionColor;

#else

                // Ensures all additional emissive renderers have default color and keywords if not set.
                item.shaderKeywordEmissionEnable = string.IsNullOrEmpty(item.shaderKeywordEmissionEnable)
    ? "_EMISSION"
    : item.shaderKeywordEmissionEnable;

                item.shaderKeywordEmissionColor = string.IsNullOrEmpty(item.shaderKeywordEmissionColor)
            ? "_EmissionColor"
            : item.shaderKeywordEmissionColor;

#endif

            }

        }

    }

}
