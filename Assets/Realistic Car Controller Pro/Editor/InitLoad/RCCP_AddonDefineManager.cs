//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEditor.Build;

/// <summary>
/// Keeps scripting-define symbols in sync with the actual presence
/// of each RCCP addon (and the shared BoneCracker asset).
/// </summary>
public class RCCP_AddonDefineManager : AssetPostprocessor {

    //–– Define → Folder mapping.  ––
    // For RCCP addons, paths are relative to BasePath.
    // For external ones (BCG_ENTEREXIT), use a static Assets/... path.
    private struct AddonInfo {
        public string define;
        public string relativeOrStaticPath;
        public bool isStaticPath;
    }

    private static readonly AddonInfo[] addons = new[]{
        new AddonInfo{
            define               = "BCG_ENTEREXIT",
            relativeOrStaticPath = "Assets/BoneCracker Games Shared Assets",
            isStaticPath         = true
        },
        new AddonInfo{
            define               = "RCCP_PHOTON",
            relativeOrStaticPath = "Addons/Installed/Photon PUN 2",
            isStaticPath         = false
        },
        new AddonInfo{
            define               = "RCCP_DEMO",
            relativeOrStaticPath = "Addons/Installed/Demo Content",
            isStaticPath         = false
        },
        new AddonInfo{
            define               = "RCCP_MIRROR",
            relativeOrStaticPath = "Addons/Installed/Mirror",
            isStaticPath         = false
        }
    };

    static RCCP_AddonDefineManager() {

        // On project load (after compile), do one sweep to correct any mismatches.
        EditorApplication.delayCall += ValidateDefineOnLoad;
    }

    /// <summary>
    /// Fired by Unity whenever assets are imported, deleted, or moved.
    /// We look for any of our addon-paths in the import/delete lists
    /// and add/remove the corresponding define.
    /// </summary>
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths
    ) {

        foreach (var info in addons) {

            // Compute our project-relative addon path
            string relPath = info.isStaticPath
                ? info.relativeOrStaticPath
                : RCCP_AssetUtilities.BasePath + info.relativeOrStaticPath;

            // Only treat it as "addon removed" if either:
            //  • a folder was deleted (no extension)
            //  • a C# script was deleted
            bool deletedRelevant = deletedAssets.Any(p =>
               p.StartsWith(relPath, StringComparison.OrdinalIgnoreCase) &&
               (
                   // folder deletion → no extension
                   string.IsNullOrEmpty(Path.GetExtension(p)) ||
                   // C# script deletion
                   Path.GetExtension(p).Equals(".cs", StringComparison.OrdinalIgnoreCase)
               )
            );

            if (deletedRelevant && HasDefine(info.define)) {
                RCCP_SetScriptingSymbol.SetEnabled(info.define, false);
            }

        }

    }

    /// <summary>
    /// On Unity startup, verify that each define matches the folder’s existence.
    /// This catches cases where someone deleted the folder by hand but forgot
    /// to remove the symbol.
    /// </summary>
    private static void ValidateDefineOnLoad() {

        foreach (var info in addons) {

            // Build the project-relative path again…
            string relPath = info.isStaticPath
                ? info.relativeOrStaticPath
                : RCCP_AssetUtilities.BasePath + info.relativeOrStaticPath;

            // Convert to an absolute path on disk.
            string fullPath = Path.GetFullPath(relPath);

            bool exists = Directory.Exists(fullPath);
            bool hasDefine = HasDefine(info.define);

            if (!exists && hasDefine) {
                RCCP_SetScriptingSymbol.SetEnabled(info.define, false);
            }

        }

    }

    /// <summary>
    /// Returns true if any build target currently has that define.
    /// </summary>
    private static bool HasDefine(string define) {

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

            return list.Contains(define);

        }

        return false;

    }

}
