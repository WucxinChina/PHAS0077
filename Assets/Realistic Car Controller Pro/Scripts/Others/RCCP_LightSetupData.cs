//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Data container that holds the user�s light selections.</summary>
[System.Serializable]
public class RCCP_LightSetupData {

    public float defaultIntensityForHeadlights = 2.5f;
    public float defaultIntensityForBrakeLights = 1f;
    public float defaultIntensityForReverseLights = 1f;
    public float defaultIntensityForIndicatorLights = 1f;

    public Color headlightColor = new Color(1f, 1f, .9f, 1f);
    public Color brakelightColor = new Color(1f, .1f, .05f, 1f);
    public Color taillightColor = new Color(1f, .05f, .05f, 1f);
    public Color reverselightColor = new Color(.9f, 1f, 1f, 1f);
    public Color indicatorColor = new Color(1f, .5f, 0f, 1f);

    public bool useLensFlares = true;

    public Object lensFlareSRP;
    public Flare flare;

}