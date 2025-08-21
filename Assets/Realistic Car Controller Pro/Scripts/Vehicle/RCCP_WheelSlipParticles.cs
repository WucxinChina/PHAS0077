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

/// <summary>
/// A simple particle system for wheel slip effects.
/// Adjusts emission based on slip intensity, enabling or disabling the particle system for a wheel.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Wheel Slip Particles")]
public class RCCP_WheelSlipParticles : RCCP_Component {

    private ParticleSystem wheelParticles;
    /// <summary>
    /// The underlying Unity ParticleSystem used for slip effects. 
    /// Created externally (e.g., a prefab), and retrieved at runtime.
    /// </summary>
    public ParticleSystem WheelParticles {

        get {

            if (!wheelParticles)
                wheelParticles = GetComponent<ParticleSystem>();

            if (!wheelParticles) {

                Debug.LogError("Particles couldn't be found on this GameObject named " + gameObject.name + "! Ensure your prefab has a proper ParticleSystem if you intend to use wheel slip effects.");
                return null;

            }

            return wheelParticles;

        }

    }

    [Range(0f, 100f)] public float minEmissionRate = 2.5f;
    [Range(0f, 100f)] public float maxEmissionRate = 25f;

    /// <summary>
    /// Enables or disables the particle system, and sets a default emission rate (0.5f).
    /// </summary>
    /// <param name="state">If true, the particle emission is enabled; if false, it's disabled.</param>
    public void Emit(bool state) {

        ParticleSystem.EmissionModule em = WheelParticles.emission;
        em.enabled = state;

        SetEmissionRate(.5f);

    }

    /// <summary>
    /// Enables or disables the particle system, then sets the emission rate 
    /// based on a provided volume factor (e.g., slip intensity).
    /// </summary>
    /// <param name="state">If true, enables the particle system; if false, disables it.</param>
    /// <param name="volume">A normalized factor [0..1] controlling emission rate via a lerp between min and max values.</param>
    public void Emit(bool state, float volume) {

        ParticleSystem.EmissionModule em = WheelParticles.emission;
        em.enabled = state;

        SetEmissionRate(volume);

    }

    /// <summary>
    /// Sets the emission rate over time by lerping between minEmissionRate and maxEmissionRate.
    /// </summary>
    /// <param name="volume">Normalized factor [0..1] controlling how intense the emission should be.</param>
    public void SetEmissionRate(float volume) {

        ParticleSystem.EmissionModule emission = WheelParticles.emission;
        emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, volume);

    }

}
