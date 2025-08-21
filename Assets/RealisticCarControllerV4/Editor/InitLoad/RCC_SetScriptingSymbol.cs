//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build; // for NamedBuildTarget

public static class RCC_SetScriptingSymbol {
    public static void SetEnabled(string defineName, bool enable) {
        foreach (BuildTarget bt in Enum.GetValues(typeof(BuildTarget))) {
            var group = BuildPipeline.GetBuildTargetGroup(bt);
            if (group == BuildTargetGroup.Unknown) continue;

#if UNITY_2021_2_OR_NEWER
            // new API (2021.2+)
            var named = NamedBuildTarget.FromBuildTargetGroup(group);
            string defs = PlayerSettings.GetScriptingDefineSymbols(named);
            var list = defs.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
#else
            // old API (pre-2021.2)
            string defs = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var list = defs.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
#endif

            if (enable) {
                if (!list.Contains(defineName))
                    list.Add(defineName);
            } else {
                list.Remove(defineName);
            }

            string newDefs = string.Join(";", list);
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(named, newDefs);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefs);
#endif
        }
    }
}

