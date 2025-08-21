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

#if BCG_URP
using UnityEngine.Rendering.Universal;
#endif

#if BCG_URP

/// <summary>
/// Upgradable decal.
/// </summary>
[RequireComponent(typeof(DecalProjector))]
public class RCC_Customizer_Decal : RCC_Core {

    private DecalProjector decalRenderer;     //  Renderer, actually a box.

    /// <summary>
    /// Sets target material of the decal.
    /// </summary>
    /// <param name="material"></param>
    public void SetDecal(Material material) {

        //  Getting the mesh renderer.
        if (!decalRenderer)
            decalRenderer = GetComponentInChildren<DecalProjector>();

        //  Return if renderer not found.
        if (!decalRenderer)
            return;

        //  Setting material of the renderer.
        decalRenderer.material = material;

    }

    public void OnValidate() {

        DecalProjector dp = GetComponent<DecalProjector>();

        if (dp == null)
            return;

        dp.scaleMode = DecalScaleMode.InheritFromHierarchy;
        dp.pivot = Vector3.zero;
        dp.drawDistance = 500f;

        if (dp.material == null)
            dp.material = RCC_Settings.Instance.defaultDecalMaterial;

        if (dp.material.name.Contains("Default"))
            dp.material = RCC_Settings.Instance.defaultDecalMaterial;

    }

}

#else

/// <summary>
/// Upgradable decal.
/// </summary>
public class RCC_Customizer_Decal : RCC_Core {

    /// <summary>
    /// Sets target material of the decal.
    /// </summary>
    /// <param name="material"></param>
    public void SetDecal(Material material) {

        //Debug.LogError("Decals are working with URP only!");
        return;

    }

}
#endif