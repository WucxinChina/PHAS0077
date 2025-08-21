//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class RCCP_ColliderGeneratorWindow : EditorWindow {

    /// <summary>
    /// Maximum number of triangles allowed per generated mesh chunk.
    /// Unity limits convex MeshColliders to 255 triangles. If the
    /// source mesh has more triangles at the chosen resolution, it
    /// will be split into multiple child meshes of this size.
    /// </summary>
    public int MaximumTriangles = 255;

    /// <summary>
    /// If true, each generated MeshCollider will be marked convex.
    /// If false, a single collider is generated using all sampled
    /// triangles (no splitting, regardless of triangle count).
    /// </summary>
    public bool UseConvex = true;

    /// <summary>
    /// Prefix for naming the parent GameObject that holds all
    /// generated colliders. Existing children with this name
    /// will be removed first.
    /// </summary>
    public string ColliderParentName = "RCCP_Colliders";

    /// <summary>
    /// Layer for generated collider GameObjects. Developers can
    /// choose which layer colliders should be placed on.
    /// </summary>
    public int ColliderLayer = 0;

    /// <summary>
    /// PhysicMaterial to assign to generated colliders. By default,
    /// this matches RCCP_Settings.vehicleColliderMaterial.
    /// </summary>
    public Object ColliderMaterial;

    /// <summary>
    /// Resolution factor (0.1 to 1.0) for sampling triangles.
    /// At 1.0, all triangles are used. At lower values, only a
    /// subset of triangles is sampled (e.g., 0.5 uses roughly half).
    /// Lower resolution yields fewer chunks but a rougher shape.
    /// </summary>
    public float Resolution = .25f;

    /// <summary>
    /// Internal: Path (relative to project) where generated mesh assets
    /// will be stored, e.g. "Assets/Models/MyModel_RCCP_ColliderMeshes".
    /// </summary>
    private string ColliderAssetsFolderPath;

    /// <summary>
    /// Called when the window is enabled or opened. Loads the default
    /// collider material from RCCP_Settings.Instance.
    /// </summary>
    private void OnEnable() {

        // Try to load default vehicle collider material from RCCP_Settings
        RCCP_Settings settings = RCCP_Settings.Instance;

        if (settings != null && settings.vehicleColliderMaterial != null) {

            ColliderMaterial = settings.vehicleColliderMaterial;

        }

        if (settings != null && settings.RCCPLayer != "") {

            ColliderLayer = LayerMask.NameToLayer(settings.RCCPLayer);

        }

    }

    /// <summary>
    /// Open the RCCP Collider Generator window from the Unity menu.
    /// </summary>
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Vehicle Colliders Generator Wizard", false, -70)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Quick Vehicle Colliders Generator Wizard", false, -70)]
    public static void OpenWindow() {

        RCCP_ColliderGeneratorWindow window = GetWindow<RCCP_ColliderGeneratorWindow>("Quick Vehicle Colliders Generator Wizard");
        window.minSize = new Vector2(320f, 240f);

    }

    /// <summary>
    /// Draws the GUI for the window, allowing the user to adjust settings
    /// and press a button to generate colliders for the selected GameObject.
    /// Includes info texts for guidance.
    /// </summary>
    private void OnGUI() {

        GUILayout.Space(10f);

        // Informational header
        EditorGUILayout.HelpBox(
            "Generate MeshColliders from a mesh. If your mesh has more triangles " +
            "than the limit for convex colliders, this tool splits it into smaller " +
            "chunks automatically. Use the Resolution slider to sample fewer triangles " +
            "on high‐poly models, saving fewer chunks but producing a rougher collider. " +
            "All generated meshes are saved next to the original model in a new folder.",
            MessageType.Info
        );

        GUILayout.Space(8f);

        EditorGUILayout.LabelField("Selected Object", EditorStyles.boldLabel);

        if (Selection.activeGameObject == null) {

            EditorGUILayout.HelpBox("Please select a GameObject with a MeshFilter in the Hierarchy.", MessageType.Warning);

            return;

        }

        MeshFilter meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null) {

            EditorGUILayout.HelpBox("Selected GameObject does not have a valid MeshFilter.", MessageType.Warning);

            return;

        }

        GUILayout.Space(8f);

        EditorGUILayout.LabelField("Mesh Name: " + meshFilter.sharedMesh.name);

        GUILayout.Space(12f);

        // Parameter: Resolution
        EditorGUILayout.HelpBox(
            "Use this slider to specify how many triangles to sample from the original mesh. " +
            "1.0 = use all triangles. Lower values sample fewer triangles, reducing chunk count.",
            MessageType.None
        );
        Resolution = EditorGUILayout.Slider("Collider Resolution", Resolution, 0.02f, 1.0f);

        GUILayout.Space(8f);

        // Parameter: MaximumTriangles
        EditorGUILayout.HelpBox(
            "Maximum number of triangles each chunk can have. Unity limits convex MeshColliders " +
            "to 255 triangles. If Use Convex Collider is false, no splitting is done.",
            MessageType.None
        );
        MaximumTriangles = EditorGUILayout.IntField("Maximum Triangles", MaximumTriangles);

        GUILayout.Space(8f);

        // Parameter: UseConvex
        EditorGUILayout.HelpBox(
            "When enabled, each generated MeshCollider is marked as convex. If disabled, " +
            "a single collider uses all sampled triangles regardless of count.",
            MessageType.None
        );
        UseConvex = EditorGUILayout.Toggle("Use Convex Collider", UseConvex);

        GUILayout.Space(8f);

        // Parameter: ColliderParentName
        EditorGUILayout.HelpBox(
            "Parent name under which all new colliders are created. Existing children " +
            "with this name will be removed first.",
            MessageType.None
        );
        ColliderParentName = EditorGUILayout.TextField("Collider Parent Name", ColliderParentName);

        GUILayout.Space(8f);

        // Parameter: ColliderLayer
        EditorGUILayout.HelpBox(
            "Choose which Unity layer the generated collider GameObjects will be assigned to.",
            MessageType.None
        );
        ColliderLayer = EditorGUILayout.LayerField("Collider Layer", ColliderLayer);

        GUILayout.Space(8f);

        // Parameter: ColliderMaterial
        EditorGUILayout.HelpBox(
            "Select the PhysicMaterial to assign to all generated colliders. " +
            "Defaults to vehicleColliderMaterial from RCCP_Settings.",
            MessageType.None
        );
        ColliderMaterial = (Object)EditorGUILayout.ObjectField(
            "Collider Material",
            ColliderMaterial,
            typeof(Object),
            false
        );

        GUILayout.Space(16f);

        if (GUILayout.Button("Generate Colliders")) {

            GenerateCollidersForSelection();

        }

    }

    /// <summary>
    /// Main entry point to generate colliders. It first determines the asset
    /// folder path next to the original mesh, deletes any old generated folder,
    /// then samples triangles, creates a new folder, and generates mesh chunks.
    /// Each chunk is saved as a Mesh asset and assigned to a MeshCollider.
    /// </summary>
    private void GenerateCollidersForSelection() {

        GameObject selected = Selection.activeGameObject;

        MeshFilter meshFilter = selected.GetComponent<MeshFilter>();

        Mesh originalMesh = meshFilter.sharedMesh;

        if (originalMesh == null) {

            Debug.LogError("RCCP Collider Generator: No mesh found on selected object.");

            return;

        }

        // 1. Determine where to save generated meshes:
        //    Get the asset path of the original mesh (e.g., "Assets/Models/MyModel.fbx" or ".asset").
        string meshAssetPath = AssetDatabase.GetAssetPath(originalMesh);

        if (string.IsNullOrEmpty(meshAssetPath)) {

            Debug.LogError("RCCP Collider Generator: Cannot determine asset path of the selected mesh.");
            return;

        }

        //    Determine the directory (e.g., "Assets/Models")
        string parentFolder = Path.GetDirectoryName(meshAssetPath).Replace("\\", "/");

        //    Name the new folder "RCCP_ColliderMeshes_<MeshName>"
        string folderName = "RCCP_ColliderMeshes_" + originalMesh.name;

        //    Full relative path to the new folder
        ColliderAssetsFolderPath = parentFolder + "/" + folderName;

        // 2. If the folder already exists, delete it to remove old meshes
        if (AssetDatabase.IsValidFolder(ColliderAssetsFolderPath)) {

            AssetDatabase.DeleteAsset(ColliderAssetsFolderPath);

        }

        // 3. Create a fresh folder
        AssetDatabase.CreateFolder(parentFolder, folderName);

        // 4. Sample triangles based on Resolution
        int[] sampledTriangles = SampleTriangles(originalMesh.triangles);

        Vector3[] allVertices = originalMesh.vertices;
        Vector3[] allNormals = originalMesh.normals;
        Vector2[] allUVs = originalMesh.uv;

        Transform parentTransform = selected.transform;

        // 5. Remove any existing collider parent child
        Transform oldParent = parentTransform.Find(ColliderParentName);

        if (oldParent != null) {

            DestroyImmediate(oldParent.gameObject);

        }

        // 6. Create new parent to hold all generated collider chunks
        GameObject colliderParent = new GameObject(ColliderParentName);
        colliderParent.layer = ColliderLayer;
        colliderParent.transform.SetParent(parentTransform, false);

        // 7. If not using convex, assign the sampled mesh to a single collider
        if (!UseConvex) {

            CreateColliderChild(
                colliderParent,
                allVertices,
                allNormals,
                allUVs,
                sampledTriangles,
                0,
                "0"
            );

        } else {

            // 8. Split sampled triangles into chunks of size MaximumTriangles
            int trianglesPerGroup = MaximumTriangles * 3;
            int totalTriangles = sampledTriangles.Length;

            int chunkIndex = 0;
            int start = 0;

            while (start < totalTriangles) {

                int end = Mathf.Min(start + trianglesPerGroup, totalTriangles);

                List<int> chunkTriangleIndices = new List<int>();

                for (int t = start; t < end; t++) {

                    chunkTriangleIndices.Add(sampledTriangles[t]);

                }

                BuildChunkAndCollider(
                    colliderParent,
                    allVertices,
                    allNormals,
                    allUVs,
                    chunkTriangleIndices.ToArray(),
                    chunkIndex
                );

                chunkIndex++;
                start += trianglesPerGroup;

            }

        }

        // 9. Save and refresh the AssetDatabase so new meshes appear in the Project
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

    }

    /// <summary>
    /// Samples the original triangle array based on the Resolution factor.
    /// At 1.0, it returns the original array. At lower values, it keeps
    /// roughly (Resolution * totalTriangleCount) triangles by selecting
    /// one triangle every 'step' groups.
    /// </summary>
    /// <param name="triangles">Original triangle index array (3 ints per triangle)</param>
    /// <returns>New triangle index array with fewer triangles</returns>
    private int[] SampleTriangles(int[] triangles) {

        // If resolution is 1.0 (or above), use all triangles
        if (Resolution >= 1.0f) {

            return triangles;

        }

        int totalTriangleCount = triangles.Length / 3;

        // Calculate step: one triangle per 'step' groups
        int step = Mathf.Max(1, Mathf.RoundToInt(1f / Resolution));

        List<int> newTriangles = new List<int>();

        for (int triIndex = 0; triIndex < totalTriangleCount; triIndex++) {

            // Keep only every 'step'th triangle
            if (triIndex % step == 0) {

                int baseIndex = triIndex * 3;

                newTriangles.Add(triangles[baseIndex]);
                newTriangles.Add(triangles[baseIndex + 1]);
                newTriangles.Add(triangles[baseIndex + 2]);

            }

        }

        return newTriangles.ToArray();

    }

    /// <summary>
    /// Builds one mesh chunk from the provided vertex and triangle subset,
    /// remapping vertices as needed, then creates a child GameObject with
    /// a MeshCollider referencing that chunk. The mesh asset is saved under
    /// ColliderAssetsFolderPath.
    /// </summary>
    /// <param name="parent">Parent under which to place the collider GameObject</param>
    /// <param name="verts">Original mesh vertices</param>
    /// <param name="normals">Original mesh normals</param>
    /// <param name="uvs">Original mesh UVs</param>
    /// <param name="triangleIndices">Triangle index list for this chunk</param>
    /// <param name="chunkIndex">Index used in naming the new mesh and GameObject</param>
    private void BuildChunkAndCollider(
        GameObject parent,
        Vector3[] verts,
        Vector3[] normals,
        Vector2[] uvs,
        int[] triangleIndices,
        int chunkIndex
    ) {

        // Map from original vertex index to new chunk vertex index
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        List<Vector3> chunkVertices = new List<Vector3>();
        List<Vector3> chunkNormals = new List<Vector3>();
        List<Vector2> chunkUVs = new List<Vector2>();
        List<int> chunkTriangles = new List<int>();

        int newVertexCounter = 0;

        // Remap each triangle reference to a new local index
        for (int i = 0; i < triangleIndices.Length; i++) {

            int originalIndex = triangleIndices[i];

            if (!vertexMap.ContainsKey(originalIndex)) {

                vertexMap.Add(originalIndex, newVertexCounter);

                chunkVertices.Add(verts[originalIndex]);

                if (normals != null && normals.Length > originalIndex) {

                    chunkNormals.Add(normals[originalIndex]);

                }

                if (uvs != null && uvs.Length > originalIndex) {

                    chunkUVs.Add(uvs[originalIndex]);

                }

                newVertexCounter++;

            }

            chunkTriangles.Add(vertexMap[originalIndex]);

        }

        // Create the chunk as a Mesh asset
        CreateColliderChild(
            parent,
            chunkVertices.ToArray(),
            chunkNormals.ToArray(),
            chunkUVs.ToArray(),
            chunkTriangles.ToArray(),
            chunkIndex,
            "chunk"
        );

    }

    /// <summary>
    /// Creates a single child under 'parent' named with chunkIndex and chunkLabel,
    /// assigns the provided geometry to a Mesh, saves it as an asset under
    /// ColliderAssetsFolderPath, then attaches a MeshCollider pointing to it.
    /// Applies the chosen layer, tag, and PhysicMaterial to the collider.
    /// </summary>
    /// <param name="parent">Parent GameObject under which child will be created</param>
    /// <param name="verts">Array of vertices for this chunk</param>
    /// <param name="normals">Array of normals for this chunk (can be empty)</param>
    /// <param name="uvs">Array of UV coordinates for this chunk (can be empty)</param>
    /// <param name="tris">Triangle index array for this chunk mesh</param>
    /// <param name="chunkIndex">Integer index for naming</param>
    /// <param name="chunkLabel">Label to include in the child’s name</param>
    private void CreateColliderChild(
        GameObject parent,
        Vector3[] verts,
        Vector3[] normals,
        Vector2[] uvs,
        int[] tris,
        int chunkIndex,
        string chunkLabel
    ) {

        // Create a new mesh for this chunk
        Mesh chunkMesh = new Mesh();

        chunkMesh.name = parent.name + "_Mesh_" + chunkLabel + "_" + chunkIndex;

        chunkMesh.vertices = verts;
        chunkMesh.triangles = tris;

        if (normals != null && normals.Length == verts.Length) {

            chunkMesh.normals = normals;

        } else {

            chunkMesh.RecalculateNormals();

        }

        chunkMesh.RecalculateBounds();

        if (uvs != null && uvs.Length == verts.Length) {

            chunkMesh.uv = uvs;

        }

        // Save the mesh as an asset under the folder path
        string meshAssetFile = ColliderAssetsFolderPath + "/" + chunkMesh.name + ".asset";
        AssetDatabase.CreateAsset(chunkMesh, meshAssetFile);

        // Create a new child GameObject to hold this chunk’s collider
        GameObject colliderObj = new GameObject(parent.name + "_Collider_" + chunkLabel + "_" + chunkIndex);

        // Assign layer and tag
        colliderObj.layer = ColliderLayer;
        colliderObj.transform.SetParent(parent.transform, false);

        // Add and configure MeshCollider
        MeshCollider meshCol = colliderObj.AddComponent<MeshCollider>();

        meshCol.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetFile);
        meshCol.convex = UseConvex;

        // Assign the chosen PhysicMaterial
        if (ColliderMaterial != null) {

#if UNITY_2023_3_OR_NEWER
            meshCol.sharedMaterial = ColliderMaterial as PhysicsMaterial;
#else
            meshCol.sharedMaterial = ColliderMaterial as PhysicMaterial;
#endif

        }

    }

    /// <summary>
    /// Ensures the window repaints regularly, even when not focused.
    /// This forces OnGUI to be called each editor frame so that any dynamic
    /// UI updates (such as progress bars or real-time status) stay current.
    /// </summary>
    private void Update() {

        Repaint();

    }

}
