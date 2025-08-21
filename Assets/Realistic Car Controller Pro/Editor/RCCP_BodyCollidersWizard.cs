//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window that lists every mesh under a vehicle hierarchy (largest → smallest),
/// lets the user pick which parts should receive a MeshCollider, and highlights the
/// chosen parts live in the Scene view.
/// </summary>
public class RCCP_BodyCollidersWizard : EditorWindow {

    //───────────────────────────────────────────────────────────────────────//
    #region Fields
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Currently selected root GameObject (vehicle).</summary>
    public GameObject selectedVehicle;

    private List<Transform> candidates = new List<Transform>();     // all mesh parts
    private List<Transform> excludedCandidates = new List<Transform>();     // all excluded mesh parts

    private bool[] selected;                               // toggle state per part

    // highlight settings
    private Color highlightColor = new Color(1f, .55f, 0f, .35f);   // default orange
    private bool solidOverlay = true;                                 // solid vs wire overlay

    // collider settings
    private bool convexColliders = true;

    private static Material highlightMat;                           // shared GL material
    private Vector2 scrollPos;                              // scroll in list

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Menu & Lifecycle
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Opens the wizard.</summary>
    public static void ShowWindow(GameObject _selectedVehicle, List<Transform> excluded) {

        RCCP_BodyCollidersWizard window = GetWindow<RCCP_BodyCollidersWizard>("Quick Body Colliders Wizard");
        window.minSize = new Vector2(420f, 560f);
        window.selectedVehicle = _selectedVehicle;
        window.excludedCandidates.Clear();
        window.excludedCandidates = excluded;
        window.RefreshCandidates();

    }

    /// <summary>Opens the wizard.</summary>
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Body Colliders Wizard", false, -75)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Body Colliders Wizard", false, -75)]
    public static void ShowWindow() {

        RCCP_BodyCollidersWizard window = GetWindow<RCCP_BodyCollidersWizard>("Quick Body Colliders Wizard");
        window.minSize = new Vector2(420f, 560f);
        window.excludedCandidates.Clear();
        window.RefreshCandidates();

    }

    private void OnEnable() {

        SceneView.duringSceneGui += OnSceneGUI;
        RefreshCandidates();

    }

    private void OnDisable() {

        SceneView.duringSceneGui -= OnSceneGUI;

        if (highlightMat)
            DestroyImmediate(highlightMat);

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region GUI
    //───────────────────────────────────────────────────────────────────────//

    private void OnGUI() {

        selectedVehicle = (GameObject)EditorGUILayout.ObjectField("Root Vehicle", selectedVehicle, typeof(GameObject), true);

        if (GUILayout.Button("Refresh List"))
            RefreshCandidates();

        if (candidates == null || candidates.Count == 0) {

            EditorGUILayout.HelpBox("No mesh parts found under the selected vehicle.", MessageType.Info);
            return;

        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
            SetAll(true);
        if (GUILayout.Button("Select None"))
            SetAll(false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Parts (largest → smallest)", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(220f));
        for (int i = 0; i < candidates.Count; i++) {

            if (!candidates[i])
                continue;

            EditorGUILayout.BeginHorizontal();
            selected[i] = EditorGUILayout.Toggle(selected[i], GUILayout.Width(20f));
            GUILayout.Label(candidates[i].name);
            EditorGUILayout.EndHorizontal();

        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        GUILayout.Label("Highlight Options", EditorStyles.boldLabel);

        highlightColor = EditorGUILayout.ColorField("Color", highlightColor);
        solidOverlay = EditorGUILayout.ToggleLeft("Solid overlay (otherwise wireframe)", solidOverlay);

        EditorGUILayout.Space();
        GUILayout.Label("Collider Options", EditorStyles.boldLabel);

        convexColliders = EditorGUILayout.ToggleLeft("Convex MeshColliders", convexColliders);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add MeshColliders To Selected Parts", GUILayout.Height(32f))) {

            AddMeshColliders();
            //Close();

        }

        // repaint Scene view instantly on changes
        if (GUI.changed)
            SceneView.RepaintAll();

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Core Logic
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Collects every mesh under the root, sorts them by volume, and resets the toggle array.</summary>
    private void RefreshCandidates() {

        candidates.Clear();

        if (!selectedVehicle) {

            selected = new bool[0];
            return;

        }

        List<MeshFilter> mfs = new List<MeshFilter>(selectedVehicle.GetComponentsInChildren<MeshFilter>(true));
        List<MeshFilter> properMfs = new List<MeshFilter>();

        for (int i = 0; i < mfs.Count; i++) {

            if (mfs[i] == null)
                continue;

            if (excludedCandidates.Contains(mfs[i].transform))
                continue;

            properMfs.Add(mfs[i]);

        }

        candidates = properMfs
            .OrderByDescending(mf => MeshVolume(mf.sharedMesh))
            .Select(mf => mf.transform)
            .ToList();

        selected = new bool[candidates.Count];

    }

    /// <summary>Returns the approximate mesh volume used for sorting.</summary>
    private float MeshVolume(Mesh mesh) {

        if (mesh == null)
            return 0f;

        Vector3 size = mesh.bounds.size;
        return size.x * size.y * size.z;

    }

    /// <summary>Sets every toggle on or off.</summary>
    private void SetAll(bool value) {

        for (int i = 0; i < selected.Length; i++)
            selected[i] = value;

        SceneView.RepaintAll();

    }

    /// <summary>Adds MeshCollider components to every ticked part.</summary>
    private void AddMeshColliders() {

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Add MeshColliders");
        int undoGroup = Undo.GetCurrentGroup();

        int added = 0;

        for (int i = 0; i < candidates.Count; i++) {

            if (!selected[i] || !candidates[i])
                continue;

            MeshCollider mc = candidates[i].GetComponent<MeshCollider>();
            if (!mc) {

                mc = Undo.AddComponent<MeshCollider>(candidates[i].gameObject);
                mc.convex = convexColliders;
                added++;

            } else {

                mc.convex = convexColliders;

            }

        }

        Undo.CollapseUndoOperations(undoGroup);

        if (added > 0)
            Debug.Log($"<b>RCCP:</b> Added MeshCollider to {added} part(s).");
        else
            Debug.Log("<b>RCCP:</b> No new colliders were added.");

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Scene Drawing
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Draws the coloured overlay for every selected part.</summary>
    private void OnSceneGUI(SceneView sv) {

        if (candidates == null || highlightColor.a <= 0f)
            return;

        EnsureMaterial();
        highlightMat.SetColor("_Color", highlightColor);
        highlightMat.SetPass(0);

        for (int i = 0; i < candidates.Count; i++) {

            if (!selected[i] || !candidates[i])
                continue;

            foreach (Renderer r in candidates[i].GetComponentsInChildren<Renderer>(true))
                DrawRenderer(r);

        }

        sv.Repaint();

    }

    /// <summary>Makes sure the GL material exists and is configured.</summary>
    private void EnsureMaterial() {

        if (highlightMat)
            return;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        highlightMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

        highlightMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        highlightMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        highlightMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
        highlightMat.SetInt("_ZWrite", 0);

    }

    /// <summary>Draws either a solid translucent mesh or a wireframe outline.</summary>
    private void DrawRenderer(Renderer r) {

        if (!r)
            return;

        if (r is MeshRenderer meshRenderer) {

            MeshFilter mf = meshRenderer.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
                DrawMesh(mf.sharedMesh, r.localToWorldMatrix);

        } else if (r is SkinnedMeshRenderer smr) {

            if (smr.sharedMesh)
                DrawMesh(smr.sharedMesh, r.localToWorldMatrix);

        }

    }

    private void DrawMesh(Mesh mesh, Matrix4x4 matrix) {

        if (solidOverlay) {

            Graphics.DrawMeshNow(mesh, matrix);

        } else {

            HandlesExtension.DrawWireMesh(mesh, matrix);

        }

    }

    #endregion

}

//───────────────────────────────────────────────────────────────────────────//
//  HandlesExtension – replacement for missing Handles.DrawWireMesh
//───────────────────────────────────────────────────────────────────────────//

/// <summary>
/// Compatibility helper that draws a wireframe representation of any mesh when
/// running on Unity versions that lack <c>Handles.DrawWireMesh</c>.
/// </summary>
public static class HandlesExtension {

    private static readonly Dictionary<Mesh, Vector3[]> cache = new Dictionary<Mesh, Vector3[]>();

    /// <summary>
    /// Draws the mesh as lines in the Scene view.
    /// </summary>
    public static void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {

        if (mesh == null)
            return;

        if (!cache.TryGetValue(mesh, out Vector3[] lines)) {

            lines = BuildLines(mesh);
            cache.Add(mesh, lines);

        }

        Handles.matrix = matrix;
        Handles.DrawLines(lines);
        Handles.matrix = Matrix4x4.identity;

    }

    /// <summary>Converts triangles into a unique list of line pairs.</summary>
    private static Vector3[] BuildLines(Mesh mesh) {

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        HashSet<ulong> edges = new HashSet<ulong>();

        for (int i = 0; i < tris.Length; i += 3) {

            AddEdge(edges, tris[i], tris[i + 1]);
            AddEdge(edges, tris[i + 1], tris[i + 2]);
            AddEdge(edges, tris[i + 2], tris[i]);

        }

        List<Vector3> pts = new List<Vector3>(edges.Count * 2);
        foreach (ulong e in edges) {

            ushort a = (ushort)(e & 0xFFFF);
            ushort b = (ushort)(e >> 16);

            pts.Add(verts[a]);
            pts.Add(verts[b]);

        }

        return pts.ToArray();

    }

    /// <summary>Adds the edge if it is not already present, otherwise removes it (dedup).</summary>
    private static void AddEdge(HashSet<ulong> set, int a, int b) {

        if (a < b) {

            ulong key = ((ulong)b << 16) | (uint)a;
            if (!set.Remove(key))
                set.Add(key);

        } else if (b < a) {

            ulong key = ((ulong)a << 16) | (uint)b;
            if (!set.Remove(key))
                set.Add(key);

        }

    }

}

#endif
