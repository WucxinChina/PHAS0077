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
using UnityEngine.Audio;

/// <summary>
/// Comprehensive audio system for vehicle sounds including engine, transmission, brakes, crashes, and environmental effects.
/// Manages multiple audio sources for realistic sound blending and spatial audio positioning.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Audio")]
public class RCCP_Audio : RCCP_Component {

    /// <summary>
    /// Audio mixer group for centralizing volume control and effects processing.
    /// </summary>
    public AudioMixerGroup audioMixer;

    /// <summary>
    /// Minimum threshold for audio volume. Values below this are treated as silence.
    /// </summary>
    private const float AUDIO_DEADZONE = 0.04f;

    /// <summary>
    /// Minimum threshold for pitch values. Prevents extremely low pitch distortions.
    /// </summary>
    private const float PITCH_DEADZONE = 0.04f;

    #region Nested Classes

    /// <summary>
    /// Engine sound configuration for RPM-based audio layers.
    /// Each layer covers a specific RPM range with smooth crossfading.
    /// </summary>
    [System.Serializable]
    public class EngineSound {

        /// <summary>
        /// Audio source for throttle-on (accelerating) engine sound.
        /// </summary>
        [HideInInspector] public AudioSource audioSourceOn;

        /// <summary>
        /// Audio clip played when throttle is pressed.
        /// </summary>
        public AudioClip audioClipOn;

        /// <summary>
        /// Audio source for throttle-off (decelerating/coasting) engine sound.
        /// </summary>
        [HideInInspector] public AudioSource audioSourceOff;

        /// <summary>
        /// Audio clip played when throttle is released.
        /// </summary>
        public AudioClip audioClipOff;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 1.5f);

        /// <summary>
        /// Minimum pitch value at the start of this sound's RPM range.
        /// </summary>
        [Min(0f)] public float minPitch = .1f;

        /// <summary>
        /// Maximum pitch value at the end of this sound's RPM range.
        /// </summary>
        [Min(0f)] public float maxPitch = 1f;

        /// <summary>
        /// Lower RPM boundary where this sound begins fading in.
        /// </summary>
        [Min(0f)] public float minRPM = 600f;

        /// <summary>
        /// Upper RPM boundary where this sound begins fading out.
        /// </summary>
        [Min(0f)] public float maxRPM = 8000f;

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 200f;

        /// <summary>
        /// Peak volume level when this sound is fully active.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Engine sound layers for different RPM ranges.
    /// Typically includes idle, low, medium, and high RPM sounds.
    /// </summary>
    public EngineSound[] engineSounds = new EngineSound[4];

    /// <summary>
    /// Engine startup sound configuration.
    /// </summary>
    [System.Serializable]
    public class EngineStart {

        /// <summary>
        /// Audio source for engine ignition sound.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip played when starting the engine.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 1.5f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for the startup sound.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Engine startup sound effect.
    /// </summary>
    public EngineStart engineStart = new EngineStart();

    /// <summary>
    /// Gearbox shifting sound configuration.
    /// </summary>
    [System.Serializable]
    public class GearboxSound {

        /// <summary>
        /// Temporary audio source for gear shift sounds.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Array of gear shift sound variations for randomization.
        /// </summary>
        public AudioClip[] audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 1f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 10f;

        /// <summary>
        /// Maximum volume for gear shift sounds.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Gear shifting sound effects.
    /// </summary>
    public GearboxSound gearboxSound = new GearboxSound();

    /// <summary>
    /// Tracks the previous gear to detect gear changes.
    /// </summary>
    private int lastGear = 0;

    /// <summary>
    /// Collision impact sound configuration.
    /// </summary>
    [System.Serializable]
    public class CrashSound {

        /// <summary>
        /// Reference to the most recent crash audio source.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Array of crash sound variations for different impact types.
        /// </summary>
        public AudioClip[] audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for crash sounds.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Collision and crash sound effects.
    /// </summary>
    public CrashSound crashSound = new CrashSound();

    /// <summary>
    /// Queue managing active crash audio sources to limit simultaneous sounds.
    /// Maximum of 5 concurrent crash sounds to prevent audio overflow.
    /// </summary>
    private Queue<AudioSource> crashAudioSources = new Queue<AudioSource>();

    /// <summary>
    /// Reverse gear warning beep configuration.
    /// </summary>
    [System.Serializable]
    public class ReverseSound {

        /// <summary>
        /// Looping audio source for reverse beeps.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for reverse warning sound.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Minimum pitch for slower reverse speeds.
        /// </summary>
        [Min(0f)] public float minPitch = .8f;

        /// <summary>
        /// Maximum pitch for faster reverse speeds.
        /// </summary>
        [Min(0f)] public float maxPitch = 1f;

        /// <summary>
        /// Maximum volume for reverse warning.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Reverse gear warning sound.
    /// </summary>
    public ReverseSound reverseSound = new ReverseSound();

    /// <summary>
    /// Wind noise configuration for high-speed effects.
    /// </summary>
    [System.Serializable]
    public class WindSound {

        /// <summary>
        /// Looping audio source for wind noise.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for wind sound effect.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for wind noise.
        /// </summary>
        [Min(0f)] public float maxVolume = .1f;

    }

    /// <summary>
    /// Aerodynamic wind noise effect.
    /// </summary>
    public WindSound windSound = new WindSound();

    /// <summary>
    /// Brake squeal sound configuration.
    /// </summary>
    [System.Serializable]
    public class BrakeSound {

        /// <summary>
        /// Looping audio source for brake sounds.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for brake squeal effect.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for brake sounds.
        /// </summary>
        [Min(0f)] public float maxVolume = .25f;

    }

    /// <summary>
    /// Brake application sound effects.
    /// </summary>
    public BrakeSound brakeSound = new BrakeSound();

    /// <summary>
    /// Nitrous oxide system sound configuration.
    /// </summary>
    [System.Serializable]
    public class NosSound {

        /// <summary>
        /// Looping audio source for NOS hiss.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for NOS activation sound.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for NOS sound.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Nitrous oxide boost sound effect.
    /// </summary>
    public NosSound nosSound = new NosSound();

    /// <summary>
    /// Turbocharger spool sound configuration.
    /// </summary>
    [System.Serializable]
    public class TurboSound {

        /// <summary>
        /// Looping audio source for turbo whine.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for turbo spool sound.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 1.5f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for turbo sound.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Turbocharger spool-up sound.
    /// </summary>
    public TurboSound turboSound = new TurboSound();

    /// <summary>
    /// Exhaust backfire/pop sound configuration.
    /// </summary>
    [System.Serializable]
    public class ExhaustFlameSound {

        /// <summary>
        /// Audio source for exhaust pops.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Array of exhaust pop sound variations.
        /// </summary>
        public AudioClip[] audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, -.5f, -2f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for exhaust pops.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Exhaust backfire and popping sounds.
    /// </summary>
    public ExhaustFlameSound exhaustFlameSound = new ExhaustFlameSound();

    /// <summary>
    /// Turbo blow-off valve sound configuration.
    /// </summary>
    [System.Serializable]
    public class BlowSound {

        /// <summary>
        /// One-shot audio source for blow-off sound.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Array of blow-off sound variations.
        /// </summary>
        public AudioClip[] audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, -1.5f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 1f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 20f;

        /// <summary>
        /// Maximum volume for blow-off sound.
        /// </summary>
        [Min(0f)] public float maxVolume = .2f;

    }

    /// <summary>
    /// Turbo blow-off valve release sound.
    /// </summary>
    public BlowSound blowSound = new BlowSound();

    /// <summary>
    /// Tire deflation sound configuration.
    /// </summary>
    [System.Serializable]
    public class DeflateSound {

        /// <summary>
        /// One-shot audio source for deflation.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for tire deflation sound.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 1f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 20f;

        /// <summary>
        /// Maximum volume for deflation sound.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Tire puncture/deflation sound effect.
    /// </summary>
    public DeflateSound wheelDeflateSound = new DeflateSound();

    /// <summary>
    /// Tire inflation sound configuration.
    /// </summary>
    [System.Serializable]
    public class InflateSound {

        /// <summary>
        /// One-shot audio source for inflation.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for tire inflation sound.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for inflation sound.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

        /// <summary>
        /// Tracks the last inflation state.
        /// </summary>
        public bool lastInflate = true;

    }

    /// <summary>
    /// Tire repair/inflation sound effect.
    /// </summary>
    public InflateSound wheelInflateSound = new InflateSound();

    /// <summary>
    /// Flat tire rolling sound configuration.
    /// </summary>
    [System.Serializable]
    public class FlatSound {

        /// <summary>
        /// Looping audio source for flat tire noise.
        /// </summary>
        [HideInInspector] public AudioSource audioSource;

        /// <summary>
        /// Audio clip for flat tire rolling sound.
        /// </summary>
        public AudioClip audioClips;

        /// <summary>
        /// Local position offset for 3D audio positioning.
        /// </summary>
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        /// <summary>
        /// Minimum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float minDistance = 10f;

        /// <summary>
        /// Maximum distance for 3D audio attenuation.
        /// </summary>
        [Min(0f)] public float maxDistance = 100f;

        /// <summary>
        /// Maximum volume for flat tire sound.
        /// </summary>
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Deflated tire rolling sound effect.
    /// </summary>
    public FlatSound wheelFlatSound;

    #endregion

    #region Private Fields

    /// <summary>
    /// Array of all audio sources managed by this component.
    /// </summary>
    private AudioSource[] allAudioSources;

    /// <summary>
    /// Stores active states of audio sources before disabling.
    /// </summary>
    private bool[] audioStatesBeforeDisabling;

    /// <summary>
    /// Cached reference to the front axle for brake calculations.
    /// </summary>
    private RCCP_Axle cachedFrontAxle;

    /// <summary>
    /// Cached engine component reference.
    /// </summary>
    private RCCP_Engine cachedEngine;

    /// <summary>
    /// Cached gearbox component reference.
    /// </summary>
    private RCCP_Gearbox cachedGearbox;

    /// <summary>
    /// Cached other addons manager reference.
    /// </summary>
    private RCCP_OtherAddons cachedOtherAddons;

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes component and starts coroutine to gather audio sources.
    /// </summary>
    public override void Start() {

        base.Start();

        // Cache frequently accessed components
        CacheComponents();

        // Gather all audio sources after initialization
        StartCoroutine(GetAllAudioSources());

    }

    /// <summary>
    /// Restores audio source states when component is re-enabled.
    /// </summary>
    public override void OnEnable() {

        base.OnEnable();

        // Skip if arrays aren't properly initialized
        if (allAudioSources == null || allAudioSources.Length < 1)
            return;

        if (allAudioSources.Length != audioStatesBeforeDisabling.Length)
            return;

        // Restore previous active states
        for (int i = 0; i < audioStatesBeforeDisabling.Length; i++) {

            if (allAudioSources[i] != null)
                allAudioSources[i].gameObject.SetActive(audioStatesBeforeDisabling[i]);

        }

    }

    /// <summary>
    /// Main update loop managing all audio systems.
    /// </summary>
    private void Update() {

        Engine();
        Gearbox();
        Wheel();
        Exhaust();
        Others();

    }

    /// <summary>
    /// Saves audio states and disables sources when component is disabled.
    /// </summary>
    public override void OnDisable() {

        base.OnDisable();

        audioStatesBeforeDisabling = new bool[0];

        if (allAudioSources == null || allAudioSources.Length < 1)
            return;

        // Save current states before disabling
        audioStatesBeforeDisabling = new bool[allAudioSources.Length];

        for (int i = 0; i < allAudioSources.Length; i++) {

            if (allAudioSources[i] != null) {

                audioStatesBeforeDisabling[i] = allAudioSources[i].gameObject.activeSelf;
                allAudioSources[i].gameObject.SetActive(false);

            }

        }

    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Caches frequently accessed components to avoid repeated GetComponent calls.
    /// </summary>
    private void CacheComponents() {

        cachedEngine = CarController.Engine;
        cachedGearbox = CarController.Gearbox;
        cachedOtherAddons = CarController.OtherAddonsManager;
        cachedFrontAxle = CarController.FrontAxle;

    }

    /// <summary>
    /// Coroutine that collects all child audio sources after physics update.
    /// </summary>
    public IEnumerator GetAllAudioSources() {

        yield return new WaitForFixedUpdate();
        allAudioSources = GetComponentsInChildren<AudioSource>(true);

    }

    #endregion

    #region Audio Processing Methods

    /// <summary>
    /// Smoothly interpolates audio source volume and pitch with deadzone handling.
    /// </summary>
    /// <param name="src">Target audio source</param>
    /// <param name="targetVol">Desired volume level</param>
    /// <param name="targetPitch">Desired pitch level</param>
    private void LerpSource(AudioSource src, float targetVol, float targetPitch) {

        if (!src || !src.clip)
            return;

        // Apply deadzone to target values
        if (targetVol < AUDIO_DEADZONE)
            targetVol = 0f;

        if (targetPitch < PITCH_DEADZONE)
            targetPitch = 0f;

        // Smooth interpolation for natural transitions
        src.volume = Mathf.Lerp(src.volume, targetVol, Time.deltaTime * 80f);
        src.pitch = Mathf.Lerp(src.pitch, targetPitch, Time.deltaTime * 60f);

        // Apply deadzone to final values
        if (src.volume < AUDIO_DEADZONE)
            src.volume = 0f;

        if (src.pitch < PITCH_DEADZONE)
            src.pitch = 0f;

    }

    /// <summary>
    /// Processes engine sounds including startup, idle, and RPM-based layers.
    /// </summary>
    private void Engine() {

        if (!cachedEngine)
            return;

        // Handle engine startup sound
        if (engineStart != null && engineStart.audioClips != null) {

            if (!engineStart.audioSource) {

                engineStart.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    engineStart.audioClips.name,
                    engineStart.minDistance,
                    engineStart.maxDistance,
                    engineStart.maxVolume,
                    engineStart.audioClips,
                    false,
                    false,
                    false
                );
                engineStart.audioSource.transform.localPosition = engineStart.localPosition;

            }

            // Play startup sound when engine is cranking
            if (engineStart.audioSource && !engineStart.audioSource.isPlaying && cachedEngine.engineStarting)
                engineStart.audioSource.Play();

        }

        // Process layered engine sounds
        if (engineSounds != null && engineSounds.Length >= 1) {

            // Initialize audio sources for each engine sound layer
            for (int i = 0; i < engineSounds.Length; i++) {

                if (engineSounds[i] == null)
                    continue;

                // Create throttle-on audio source if missing
                if (!engineSounds[i].audioSourceOn && engineSounds[i].audioClipOn) {

                    engineSounds[i].audioSourceOn = RCCP_AudioSource.NewAudioSource(
                        audioMixer,
                        gameObject,
                        engineSounds[i].audioClipOn.name,
                        engineSounds[i].minDistance,
                        engineSounds[i].maxDistance,
                        0f,
                        engineSounds[i].audioClipOn,
                        true,
                        false,
                        false
                    );
                    engineSounds[i].audioSourceOn.transform.localPosition = engineSounds[i].localPosition;

                }

                // Ensure throttle-on sound is playing
                if (engineSounds[i].audioSourceOn && !engineSounds[i].audioSourceOn.isPlaying)
                    engineSounds[i].audioSourceOn.Play();

                // Create throttle-off audio source if missing
                if (!engineSounds[i].audioSourceOff && engineSounds[i].audioClipOff) {

                    engineSounds[i].audioSourceOff = RCCP_AudioSource.NewAudioSource(
                        audioMixer,
                        gameObject,
                        engineSounds[i].audioClipOff.name,
                        engineSounds[i].minDistance,
                        engineSounds[i].maxDistance,
                        0f,
                        engineSounds[i].audioClipOff,
                        true,
                        false,
                        false
                    );
                    engineSounds[i].audioSourceOff.transform.localPosition = engineSounds[i].localPosition;

                }

                // Ensure throttle-off sound is playing
                if (engineSounds[i].audioSourceOff && !engineSounds[i].audioSourceOff.isPlaying)
                    engineSounds[i].audioSourceOff.Play();

            }

            // Update volume and pitch for each engine sound layer
            foreach (EngineSound es in engineSounds) {

                if (es == null)
                    continue;

                float rpm = CarController.engineRPM;
                float load = CarController.throttleInput_V;

                // Calculate normalized RPM position within this sound's range
                float rpmNormalized = Mathf.InverseLerp(es.minRPM, es.maxRPM, rpm);

                // Calculate pitch based on RPM position
                float targetPitch = Mathf.Lerp(es.minPitch, es.maxPitch, rpmNormalized);

                // Calculate base volume using a bell curve for smooth transitions
                float baseVolume = 0f;

                if (rpm >= es.minRPM && rpm <= es.maxRPM) {

                    // Position within the RPM window (0 to 1)
                    float position = (rpm - es.minRPM) / (es.maxRPM - es.minRPM);

                    // Bell curve: peaks at 0.5, falls off at edges
                    float bellCurve = 1f - Mathf.Pow(2f * position - 1f, 2f);
                    baseVolume = es.maxVolume * bellCurve;

                } else if (rpm > es.maxRPM) {

                    // FIXED: Maintain volume/pitch when exceeding max RPM instead of dropping
                    // This prevents the highest engine sound from cutting out at redline
                    targetPitch = es.maxPitch;

                    // Fade out gradually beyond max RPM to prevent harsh cutoff
                    float fadeDistance = 500f; // RPM units for fade out
                    float excessRPM = rpm - es.maxRPM;
                    float fadeFactor = Mathf.Clamp01(1f - (excessRPM / fadeDistance));
                    baseVolume = es.maxVolume * fadeFactor;

                }

                // Split volume between throttle on/off states
                float volumeOn = baseVolume * load;
                float volumeOff = baseVolume * (1f - load);

                // Apply smooth transitions with deadzone
                LerpSource(es.audioSourceOn, volumeOn, targetPitch);
                LerpSource(es.audioSourceOff, volumeOff, targetPitch);

            }

        }

        // Handle turbocharger spool sound
        if (turboSound != null && turboSound.audioClips != null && cachedEngine.turboCharged) {

            if (!turboSound.audioSource) {

                turboSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    turboSound.audioClips.name,
                    turboSound.minDistance,
                    turboSound.maxDistance,
                    0f,
                    turboSound.audioClips,
                    true,
                    true,
                    false
                );
                turboSound.audioSource.transform.localPosition = turboSound.localPosition;

            } else {

                // Volume based on turbo pressure
                float turboVolume = Mathf.Lerp(0f, turboSound.maxVolume, cachedEngine.turboChargePsi / cachedEngine.maxTurboChargePsi);
                turboSound.audioSource.volume = turboVolume < AUDIO_DEADZONE ? 0f : turboVolume;

            }

        }

        // Handle turbo blow-off valve sound
        if (blowSound != null && blowSound.audioClips != null && blowSound.audioClips.Length >= 1 && cachedEngine.turboCharged) {

            if (!blowSound.audioSource) {

                blowSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    blowSound.audioClips[0].name,
                    blowSound.minDistance,
                    blowSound.maxDistance,
                    blowSound.maxVolume,
                    blowSound.audioClips[0],
                    false,
                    false,
                    false
                );
                blowSound.audioSource.transform.localPosition = blowSound.localPosition;

            } else {

                // Play blow-off sound when turbo releases pressure
                if (cachedEngine.turboBlowOut && !blowSound.audioSource.isPlaying) {

                    blowSound.audioSource.clip = blowSound.audioClips[UnityEngine.Random.Range(0, blowSound.audioClips.Length)];
                    blowSound.audioSource.Play();

                }

            }

        }

    }

    /// <summary>
    /// Manages transmission-related sounds including gear shifts and reverse beeps.
    /// </summary>
    private void Gearbox() {

        if (!cachedGearbox)
            return;

        // Handle gear shift sounds
        if (gearboxSound != null && gearboxSound.audioClips != null && gearboxSound.audioClips.Length >= 1) {

            // Detect gear changes
            if (lastGear != CarController.currentGear) {

                int randomClip = UnityEngine.Random.Range(0, gearboxSound.audioClips.Length);

                // Create one-shot audio for gear shift
                gearboxSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    gearboxSound.audioClips[randomClip].name,
                    gearboxSound.minDistance,
                    gearboxSound.maxDistance,
                    gearboxSound.maxVolume,
                    gearboxSound.audioClips[randomClip],
                    false,
                    true,
                    true
                );
                gearboxSound.audioSource.transform.localPosition = gearboxSound.localPosition;

            }

            lastGear = CarController.currentGear;

        }

        // Handle reverse gear warning sound
        if (reverseSound != null && reverseSound.audioClips != null) {

            if (!reverseSound.audioSource) {

                reverseSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    reverseSound.audioClips.name,
                    reverseSound.minDistance,
                    reverseSound.maxDistance,
                    0f,
                    reverseSound.audioClips,
                    true,
                    true,
                    false
                );
                reverseSound.audioSource.transform.localPosition = reverseSound.localPosition;

            } else {

                // Volume and pitch based on reverse speed
                float reverseSpeed = Mathf.Abs(Mathf.Min(0f, CarController.speed));
                float volume = Mathf.InverseLerp(0f, 40f, reverseSpeed) * reverseSound.maxVolume;
                float pitch = Mathf.Lerp(reverseSound.minPitch, reverseSound.maxPitch, Mathf.InverseLerp(0f, 40f, reverseSpeed));

                reverseSound.audioSource.volume = volume < AUDIO_DEADZONE ? 0f : volume;
                reverseSound.audioSource.pitch = pitch < PITCH_DEADZONE ? 0f : pitch;

            }

        }

    }

    /// <summary>
    /// Handles wheel and brake-related sounds including squeals and flat tires.
    /// </summary>
    private void Wheel() {

        if (CarController.AllWheelColliders == null || CarController.AllWheelColliders.Length == 0)
            return;

        // Handle brake squeal sound
        if (brakeSound != null && brakeSound.audioClips != null) {

            if (!brakeSound.audioSource) {

                brakeSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    brakeSound.audioClips.name,
                    brakeSound.minDistance,
                    brakeSound.maxDistance,
                    0f,
                    brakeSound.audioClips,
                    true,
                    true,
                    false
                );
                brakeSound.audioSource.transform.localPosition = brakeSound.localPosition;

            }

            // Calculate brake sound volume based on brake force and wheel speed
            if (cachedFrontAxle != null && cachedFrontAxle.leftWheelCollider && cachedFrontAxle.rightWheelCollider) {

                float leftBrake = cachedFrontAxle.leftWheelCollider.WheelCollider.brakeTorque;
                float rightBrake = cachedFrontAxle.rightWheelCollider.WheelCollider.brakeTorque;
                float totalBrake = leftBrake + rightBrake;
                float maxBrake = cachedFrontAxle.maxBrakeTorque * 2f;

                float leftRPM = Mathf.Abs(cachedFrontAxle.leftWheelCollider.WheelCollider.rpm);
                float rightRPM = Mathf.Abs(cachedFrontAxle.rightWheelCollider.WheelCollider.rpm);
                float avgRPM = (leftRPM + rightRPM) / 2f;

                // Brake sound is louder with more brake force and wheel rotation
                float brakeIntensity = Mathf.Clamp01(totalBrake / maxBrake);
                float wheelMotion = Mathf.Clamp01(avgRPM / 50f);
                float volume = brakeIntensity * wheelMotion * brakeSound.maxVolume;

                brakeSound.audioSource.volume = volume < AUDIO_DEADZONE ? 0f : volume;

            }

        }

        // Handle flat tire sound
        if (wheelFlatSound != null && wheelFlatSound.audioClips != null) {

            bool anyWheelDeflated = false;

            // Check if any wheel is deflated
            for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

                if (CarController.AllWheelColliders[i].deflated) {
                    anyWheelDeflated = true;
                    break;
                }

            }

            if (anyWheelDeflated) {

                // Create flat tire sound if needed
                if (wheelFlatSound.audioSource == null) {

                    wheelFlatSound.audioSource = RCCP_AudioSource.NewAudioSource(
                        audioMixer,
                        gameObject,
                        wheelFlatSound.audioClips.name,
                        wheelFlatSound.minDistance,
                        wheelFlatSound.maxDistance,
                        0f,
                        wheelFlatSound.audioClips,
                        true,
                        false,
                        false
                    );
                    wheelFlatSound.audioSource.transform.localPosition = wheelFlatSound.localPosition;

                } else {

                    // Volume based on wheel rotation speed
                    float wheelRPM = Mathf.Abs(CarController.tractionWheelRPM2EngineRPM);
                    float volume = Mathf.Clamp01(wheelRPM * 0.001f) * wheelFlatSound.maxVolume;

                    // Only play when grounded
                    volume *= CarController.IsGrounded ? 1f : 0f;

                    wheelFlatSound.audioSource.volume = volume < AUDIO_DEADZONE ? 0f : volume;

                    if (!wheelFlatSound.audioSource.isPlaying)
                        wheelFlatSound.audioSource.Play();

                }

            } else {

                // Stop flat tire sound when no wheels are deflated
                if (wheelFlatSound.audioSource != null && wheelFlatSound.audioSource.isPlaying)
                    wheelFlatSound.audioSource.Stop();

            }

        }

    }

    /// <summary>
    /// Manages exhaust system sounds including NOS and backfire effects.
    /// </summary>
    private void Exhaust() {

        if (!cachedOtherAddons)
            return;

        // Handle NOS sound
        if (cachedOtherAddons.Nos != null && nosSound != null && nosSound.audioClips != null) {

            if (!nosSound.audioSource) {

                nosSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    nosSound.audioClips.name,
                    nosSound.minDistance,
                    nosSound.maxDistance,
                    0f,
                    nosSound.audioClips,
                    true,
                    true,
                    false
                );
                nosSound.audioSource.transform.localPosition = nosSound.localPosition;

            } else {

                // NOS sound active when boost is engaged
                float volume = cachedOtherAddons.Nos.nosInUse ? nosSound.maxVolume : 0f;
                nosSound.audioSource.volume = volume < AUDIO_DEADZONE ? 0f : volume;

            }

        }

        // Handle exhaust backfire sounds
        if (cachedOtherAddons.Exhausts != null &&
            cachedOtherAddons.Exhausts.Exhaust.Length >= 1 &&
            cachedOtherAddons.Exhausts.Exhaust[0] != null &&
            exhaustFlameSound != null &&
            exhaustFlameSound.audioClips != null &&
            exhaustFlameSound.audioClips.Length > 0) {

            if (!exhaustFlameSound.audioSource) {

                AudioClip randomExhaustClip = exhaustFlameSound.audioClips[Random.Range(0, exhaustFlameSound.audioClips.Length)];

                exhaustFlameSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    randomExhaustClip.name,
                    exhaustFlameSound.minDistance,
                    exhaustFlameSound.maxDistance,
                    0f,
                    randomExhaustClip,
                    true,
                    true,
                    false
                );
                exhaustFlameSound.audioSource.transform.localPosition = exhaustFlameSound.localPosition;

            } else {

                bool isPopping = cachedOtherAddons.Exhausts.Exhaust[0].popping;

                // Play backfire sound when exhaust is popping
                if (!exhaustFlameSound.audioSource.isPlaying && isPopping) {

                    exhaustFlameSound.audioSource.clip = exhaustFlameSound.audioClips[Random.Range(0, exhaustFlameSound.audioClips.Length)];
                    exhaustFlameSound.audioSource.volume = exhaustFlameSound.maxVolume;
                    exhaustFlameSound.audioSource.Play();

                }

                // Stop when popping ends
                if (exhaustFlameSound.audioSource.isPlaying && !isPopping) {

                    exhaustFlameSound.audioSource.Stop();

                }

            }

        }

    }

    /// <summary>
    /// Handles environmental and miscellaneous sounds like wind noise.
    /// </summary>
    private void Others() {

        // Handle wind noise at high speeds
        if (windSound != null && windSound.audioClips != null) {

            if (!windSound.audioSource) {

                windSound.audioSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    windSound.audioClips.name,
                    windSound.minDistance,
                    windSound.maxDistance,
                    0f,
                    windSound.audioClips,
                    true,
                    true,
                    false
                );
                windSound.audioSource.transform.localPosition = windSound.localPosition;

            } else {

                // Wind volume increases with speed
                float volume = Mathf.InverseLerp(0f, 200f, CarController.absoluteSpeed) * windSound.maxVolume * 0.2f;
                windSound.audioSource.volume = volume < AUDIO_DEADZONE ? 0f : volume;

            }

        }

    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Handles collision impact sounds with dynamic volume based on impact force.
    /// </summary>
    /// <param name="collision">Collision data containing impact information</param>
    public void OnCollision(Collision collision) {

        // Clean up any destroyed audio sources from the queue
        while (crashAudioSources.Count > 0 && crashAudioSources.Peek() == null) {
            crashAudioSources.Dequeue();
        }

        // Process crash sounds if configured
        if (crashSound != null && crashSound.audioClips != null && crashSound.audioClips.Length >= 1) {

            // Calculate volume based on impact force (0 to 20000 units mapped to 0-1)
            float impactMagnitude = collision.impulse.magnitude;
            float volume = Mathf.InverseLerp(0f, 20000f, impactMagnitude) * crashSound.maxVolume;

            // Only create sound if impact is significant
            if (volume > AUDIO_DEADZONE) {

                // Limit concurrent crash sounds to prevent audio overflow
                if (crashAudioSources.Count >= 5) {
                    AudioSource oldestSource = crashAudioSources.Dequeue();
                    if (oldestSource != null) {
                        Destroy(oldestSource.gameObject);
                    }
                }

                // Select random crash sound
                int randomClip = UnityEngine.Random.Range(0, crashSound.audioClips.Length);
                AudioClip clipToPlay = crashSound.audioClips[randomClip];

                // Create positioned one-shot audio at impact point
                AudioSource newSource = RCCP_AudioSource.NewAudioSource(
                    audioMixer,
                    gameObject,
                    clipToPlay.name,
                    crashSound.minDistance,
                    crashSound.maxDistance,
                    volume,
                    clipToPlay,
                    false,
                    true,
                    true
                );

                // Position at collision contact point
                newSource.transform.position = collision.GetContact(0).point;

                // Track active crash sounds
                crashAudioSources.Enqueue(newSource);
                crashSound.audioSource = newSource;

            }

        }

    }

    /// <summary>
    /// Plays tire deflation sound effect.
    /// </summary>
    public void DeflateWheel() {

        if (wheelDeflateSound != null && wheelDeflateSound.audioClips != null) {

            wheelDeflateSound.audioSource = RCCP_AudioSource.NewAudioSource(
                audioMixer,
                gameObject,
                wheelDeflateSound.audioClips.name,
                wheelDeflateSound.minDistance,
                wheelDeflateSound.maxDistance,
                wheelDeflateSound.maxVolume,
                wheelDeflateSound.audioClips,
                false,
                true,
                true
            );
            wheelDeflateSound.audioSource.transform.localPosition = wheelDeflateSound.localPosition;

        }

    }

    /// <summary>
    /// Plays tire inflation/repair sound effect.
    /// </summary>
    public void InflateWheel() {

        if (wheelInflateSound != null && wheelInflateSound.audioClips != null) {

            wheelInflateSound.audioSource = RCCP_AudioSource.NewAudioSource(
                audioMixer,
                gameObject,
                wheelInflateSound.audioClips.name,
                wheelInflateSound.minDistance,
                wheelInflateSound.maxDistance,
                wheelInflateSound.maxVolume,
                wheelInflateSound.audioClips,
                false,
                true,
                true
            );
            wheelInflateSound.audioSource.transform.localPosition = wheelInflateSound.localPosition;

        }

    }

    /// <summary>
    /// Disables and destroys all engine sound sources.
    /// </summary>
    public void DisableEngineSounds() {

        if (engineSounds == null)
            return;

        // Clean up all engine audio sources
        for (int i = 0; i < engineSounds.Length; i++) {

            if (engineSounds[i] != null) {

                if (engineSounds[i].audioSourceOn)
                    Destroy(engineSounds[i].audioSourceOn.gameObject);

                if (engineSounds[i].audioSourceOff)
                    Destroy(engineSounds[i].audioSourceOff.gameObject);

            }

        }

        engineSounds = null;

    }

    /// <summary>
    /// Reloads audio configuration. Reserved for future implementation.
    /// </summary>
    public void Reload() {

        // Placeholder for potential audio system reload functionality

    }

    #endregion

    #region Editor Reset

    /// <summary>
    /// Configures default audio settings when component is first added.
    /// </summary>
    private void Reset() {

        audioMixer = RCCPSettings.audioMixer;

        // Initialize engine sound array with default configurations
        engineSounds = new EngineSound[4];

        for (int i = 0; i < engineSounds.Length; i++) {

            engineSounds[i] = new EngineSound();
            engineSounds[i].minDistance = 10f;
            engineSounds[i].maxDistance = 120f;

        }

        // Configure RPM ranges for each engine sound layer
        // Idle sound (0-1200 RPM)
        engineSounds[0].minRPM = 0f;
        engineSounds[0].maxRPM = 4000f;
        engineSounds[0].minPitch = .85f;
        engineSounds[0].maxPitch = 1.45f;
        engineSounds[0].maxVolume = .6f;

        // Medium RPM sound (2000-7000 RPM)
        engineSounds[1].minRPM = 2000f;
        engineSounds[1].maxRPM = 7000f;
        engineSounds[1].minPitch = .85f;
        engineSounds[1].maxPitch = 1.75f;
        engineSounds[1].maxVolume = .65f;

        // High RPM sound (5000-8000 RPM)
        engineSounds[2].minRPM = 5000f;
        engineSounds[2].maxRPM = 8000f;
        engineSounds[2].minPitch = .9f;
        engineSounds[2].maxPitch = 1.3f;
        engineSounds[2].maxVolume = .7f;

        // Idle emphasis (0-1200 RPM)
        engineSounds[3].minRPM = 0f;
        engineSounds[3].maxRPM = 1200f;
        engineSounds[3].minPitch = .55f;
        engineSounds[3].maxPitch = 1.55f;
        engineSounds[3].maxVolume = .6f;

        // Assign default audio clips from settings
        engineSounds[0].audioClipOn = RCCPSettings.engineLowClipOn;
        engineSounds[0].audioClipOff = RCCPSettings.engineLowClipOff;

        engineSounds[1].audioClipOn = RCCPSettings.engineMedClipOn;
        engineSounds[1].audioClipOff = RCCPSettings.engineMedClipOff;

        engineSounds[2].audioClipOn = RCCPSettings.engineHighClipOn;
        engineSounds[2].audioClipOff = RCCPSettings.engineHighClipOff;

        engineSounds[3].audioClipOn = RCCPSettings.engineIdleClipOn;
        engineSounds[3].audioClipOff = RCCPSettings.engineIdleClipOff;

        // Initialize other sound systems with default settings
        gearboxSound = new GearboxSound();
        gearboxSound.minDistance = 1f;
        gearboxSound.maxDistance = 10f;
        gearboxSound.maxVolume = 1f;

        crashSound = new CrashSound();
        crashSound.minDistance = 10f;
        crashSound.maxDistance = 100f;
        crashSound.maxVolume = 1f;

        engineStart = new EngineStart();

        if (RCCPSettings.engineStartClip)
            engineStart.audioClips = RCCPSettings.engineStartClip;

        gearboxSound = new GearboxSound();

        if (RCCPSettings.gearClips != null)
            gearboxSound.audioClips = RCCPSettings.gearClips;

        crashSound = new CrashSound();

        if (RCCPSettings.crashClips != null)
            crashSound.audioClips = RCCPSettings.crashClips;

        reverseSound = new ReverseSound();

        if (RCCPSettings.reversingClip != null)
            reverseSound.audioClips = RCCPSettings.reversingClip;

        windSound = new WindSound();

        if (RCCPSettings.windClip != null)
            windSound.audioClips = RCCPSettings.windClip;

        brakeSound = new BrakeSound();

        if (RCCPSettings.brakeClip != null)
            brakeSound.audioClips = RCCPSettings.brakeClip;

        nosSound = new NosSound();

        if (RCCPSettings.NOSClip != null)
            nosSound.audioClips = RCCPSettings.NOSClip;

        exhaustFlameSound = new ExhaustFlameSound();

        if (RCCPSettings.exhaustFlameClips != null)
            exhaustFlameSound.audioClips = RCCPSettings.exhaustFlameClips;

        turboSound = new TurboSound();

        if (RCCPSettings.turboClip != null)
            turboSound.audioClips = RCCPSettings.turboClip;

        blowSound = new BlowSound();

        if (RCCPSettings.blowoutClip != null)
            blowSound.audioClips = RCCPSettings.blowoutClip;

        wheelDeflateSound = new DeflateSound();

        if (RCCPSettings.wheelDeflateClip != null)
            wheelDeflateSound.audioClips = RCCPSettings.wheelDeflateClip;

        wheelInflateSound = new InflateSound();

        if (RCCPSettings.wheelInflateClip != null)
            wheelInflateSound.audioClips = RCCPSettings.wheelInflateClip;

        wheelFlatSound = new FlatSound();

        if (RCCPSettings.wheelFlatClip != null)
            wheelFlatSound.audioClips = RCCPSettings.wheelFlatClip;

    }

    #endregion

}