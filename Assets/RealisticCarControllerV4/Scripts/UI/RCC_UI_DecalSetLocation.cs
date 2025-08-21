//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI decal button.
/// </summary>
public class RCC_UI_DecalSetLocation : RCC_Core {

    /// <summary>
    /// Location of the decal. 0 is front, 1 is back, 2 is left, and 3 is right.
    /// </summary>
    [Min(0)] public int location = 0;

    public void Upgrade() {

#if !UNITY_2022_1_OR_NEWER
        RCC_UI_Decal[] decalButtons = FindObjectsOfType<RCC_UI_Decal>(true);
#else
        RCC_UI_Decal[] decalButtons = FindObjectsByType<RCC_UI_Decal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif

        if (decalButtons == null)
            return;

        if (decalButtons.Length < 1)
            return;

        foreach (RCC_UI_Decal item in decalButtons)
            item.SetLocation(location);

    }

    /// <summary>
    /// Sets the location of the decal. 0 is front, 1 is back, 2 is left, and 3 is right.
    /// </summary>
    /// <param name="_location"></param>
    public void SetLocation(int _location) {

        location = _location;

    }

}
