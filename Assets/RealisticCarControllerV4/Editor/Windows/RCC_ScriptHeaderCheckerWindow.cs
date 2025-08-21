//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor window to scan, preview, fix, and clean license headers on C# scripts under the RCC asset folder.
/// </summary>
public class RCC_ScriptHeaderCheckerWindow : EditorWindow {

    private class ScriptEntry {
        public string path;
        public bool selected;
        public bool hasHeader;
        public ScriptEntry(string path, bool hasHeader) { this.path = path; this.hasHeader = hasHeader; selected = true; }
    }

    private static readonly string licenseHeader = @"//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

";

    private List<ScriptEntry> scriptEntries = new List<ScriptEntry>();
    private Vector2 scrollPos = Vector2.zero;

    public static void ShowWindow() {
        var window = GetWindow<RCC_ScriptHeaderCheckerWindow>("RCC Script Header Checker");
        window.ScanScripts();
    }

    /// <summary>
    /// Scans all scripts under the RCC asset BasePath.
    /// </summary>
    private void ScanScripts() {
        scriptEntries.Clear();
        string basePath = RCC_AssetUtilities.BasePath;
        string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { basePath });
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetExtension(path) != ".cs") continue;
            string content = File.ReadAllText(path);
            bool hasHeader = content.StartsWith(licenseHeader);
            scriptEntries.Add(new ScriptEntry(path, hasHeader));
        }
    }

    private void OnGUI() {
        GUILayout.Label("RCC Script Header Checker", EditorStyles.boldLabel);

        if (scriptEntries.Count == 0) {
            EditorGUILayout.HelpBox("No C# scripts found under RCC asset.", MessageType.Info);
            if (GUILayout.Button("Rescan")) ScanScripts();
            return;
        }

        // Bulk actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All")) {
            foreach (var e in scriptEntries) e.selected = true;
        }
        if (GUILayout.Button("Select None")) {
            foreach (var e in scriptEntries) e.selected = false;
        }
        if (GUILayout.Button("Fix Missing Headers")) {
            EditorApplication.delayCall += () => {
                foreach (var entry in new List<ScriptEntry>(scriptEntries)) {
                    if (entry.selected && !entry.hasHeader) FixScript(entry);
                }
                ScanScripts();
            };
        }
        if (GUILayout.Button("Clean All Headers")) {
            EditorApplication.delayCall += () => {
                foreach (var entry in new List<ScriptEntry>(scriptEntries)) {
                    if (entry.selected && entry.hasHeader) CleanScript(entry);
                }
                ScanScripts();
            };
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Prepare deferred operations
        ScriptEntry toFix = null;
        ScriptEntry toClean = null;

        foreach (var entry in scriptEntries) {
            EditorGUILayout.BeginHorizontal();
            entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(20));
            EditorGUILayout.LabelField(entry.path);

            if (!entry.hasHeader) {
                if (GUILayout.Button("Preview", GUILayout.Width(60))) {
                    string original = File.ReadAllText(entry.path);
                    string preview = licenseHeader + "\n" + original;
                    RCC_ScriptHeaderPreviewWindow.Show(entry.path, original, preview);
                }
                if (GUILayout.Button("Fix", GUILayout.Width(50))) {
                    toFix = entry;
                }
            } else {
                if (GUILayout.Button("Clean", GUILayout.Width(60))) {
                    toClean = entry;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(4);
        if (GUILayout.Button("Rescan")) ScanScripts();

        // Execute deferred operations
        if (toFix != null) {
            FixScript(toFix);
            EditorApplication.delayCall += ScanScripts;
        }
        if (toClean != null) {
            CleanScript(toClean);
            EditorApplication.delayCall += ScanScripts;
        }

        if (GUILayout.Button("Fix Endings"))
            NormalizeAllLineEndings();

    }

    /// <summary>
    /// Inserts header into a single script.
    /// </summary>
    private void FixScript(ScriptEntry entry) {
        string content = File.ReadAllText(entry.path);
        File.WriteAllText(entry.path, licenseHeader + "\n" + content);
    }

    /// <summary>
    /// Removes the header from a single script if present.
    /// </summary>
    private void CleanScript(ScriptEntry entry) {
        string content = File.ReadAllText(entry.path);
        if (content.StartsWith(licenseHeader)) {
            string newContent = content.Substring(licenseHeader.Length).TrimStart('\r', '\n');
            File.WriteAllText(entry.path, newContent);
        }
    }

    private static void NormalizeAllLineEndings() {

        // Get every C# file under Assets
        var allCS = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        int fixedCount = 0;

        foreach (var path in allCS) {

            // Read raw bytes as UTF8
            var text = File.ReadAllText(path);

            // First normalize everything to LF
            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");

            // Then convert LF → CRLF
            var normalized = text.Replace("\n", "\r\n");

            if (normalized != File.ReadAllText(path)) {
                File.WriteAllText(path, normalized);
                fixedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"RCC: Normalized line endings on {fixedCount} scripts.");
    }

}

/// <summary>
/// Popup window to preview original vs. header-inserted content.
/// </summary>
public class RCC_ScriptHeaderPreviewWindow : EditorWindow {

    private static string original;
    private static string preview;
    private Vector2 scrollOriginal, scrollPreview;

    public static void Show(string path, string originalContent, string previewContent) {
        original = originalContent;
        preview = previewContent;
        var window = GetWindow<RCC_ScriptHeaderPreviewWindow>("Preview: " + Path.GetFileName(path));
        window.scrollOriginal = Vector2.zero;
        window.scrollPreview = Vector2.zero;
    }

    private void OnGUI() {
        GUILayout.Label("Header Preview", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        GUILayout.Label("Original", EditorStyles.miniBoldLabel);
        scrollOriginal = EditorGUILayout.BeginScrollView(scrollOriginal, GUILayout.Height(position.height - 60));
        EditorGUILayout.TextArea(original, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        GUILayout.Label("With Header", EditorStyles.miniBoldLabel);
        scrollPreview = EditorGUILayout.BeginScrollView(scrollPreview, GUILayout.Height(position.height - 60));
        EditorGUILayout.TextArea(preview, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Close")) Close();
    }

}
