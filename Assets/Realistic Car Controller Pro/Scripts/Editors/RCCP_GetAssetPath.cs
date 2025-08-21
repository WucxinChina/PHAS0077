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
/// Gets path of the target asset.
/// </summary>
public class RCCP_GetAssetPath {

    public static string GetAssetPath(Object pathObject) {

#if UNITY_EDITOR

        string path = UnityEditor.AssetDatabase.GetAssetPath(pathObject);
        return path;

#else

        return "";

#endif

    }

}
