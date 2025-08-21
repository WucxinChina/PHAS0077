//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// Deforms the meshes, wheels, lights, and other parts of the vehicle.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Damage")]
public class RCCP_Damage : RCCP_Component {

    private bool initialized = false;

    public CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    /// <summary>
    /// Save file name will be used to save / load damage data with json.
    /// </summary>
    public string saveName = "";

    /// <summary>
    /// Collected mesh filters.
    /// </summary>
    public MeshFilter[] meshFilters;

    /// <summary>
    /// Collected lights.
    /// </summary>
    public RCCP_Light[] lights;

    /// <summary>
    /// Collected parts.
    /// </summary>
    public RCCP_DetachablePart[] parts;

    /// <summary>
    /// Collected wheels.
    /// </summary>
    public RCCP_WheelCollider[] wheels;

    /// <summary>
    /// If set to enabled, all parts of the vehicle will be processed. If disabled, each part can be selected individually.
    /// </summary>
    public bool automaticInstallation = true;

    /// <summary>
    /// LayerMask filter. Damage will be taken from the objects with these layers.
    /// </summary>
    public LayerMask damageFilter = -1;

    /// <summary>
    /// Maximum Vert Distance For Limiting Damage. 0 Value Will Disable The Limit.
    /// </summary>
    [Min(0f)] public float maximumDamage = .75f;

    /// <summary>
    /// Process inactive gameobjects too?
    /// </summary>
    public bool processInactiveGameobjects = false;

    /// <summary>
    /// Mesh deformation
    /// </summary>
    [Space()] public bool meshDeformation = true;

    /// <summary>
    /// Vertices within this radius will be affected on collisions.
    /// </summary>
    [Min(0f)] public float deformationRadius = .75f;

    /// <summary>
    /// Damage multiplier.
    /// </summary>
    [Min(0f)] public float deformationMultiplier = 1f;

    /// <summary>
    /// Minimum collision force impulse.
    /// </summary>
    private readonly float minimumCollisionImpulse = .5f;

    /// <summary>
    /// Used to compare original vertex positions to the last vertex positions and decide if mesh is repaired or not.
    /// </summary>
    private readonly float minimumVertDistanceForDamagedMesh = .002f;

    /// <summary>
    /// Struct for Original Mesh Vertices positions.
    /// </summary>
    [System.Serializable] public struct OriginalMeshVerts { public Vector3[] meshVerts; }

    /// <summary>
    /// Struct for Original Wheel positions and rotations.
    /// </summary>
    [System.Serializable] public struct OriginalWheelPos { public Vector3 wheelPosition; public Quaternion wheelRotation; }

    public OriginalMeshVerts[] originalMeshData;
    public OriginalMeshVerts[] damagedMeshData;
    public OriginalWheelPos[] originalWheelData;
    public OriginalWheelPos[] damagedWheelData;

    /// <summary>
    /// Damage data class.
    /// </summary> 
    [System.Serializable]
    public class DamageData {

        public OriginalMeshVerts[] originalMeshData;
        public OriginalMeshVerts[] damagedMeshData;
        public OriginalWheelPos[] originalWheelData;
        public OriginalWheelPos[] damagedWheelData;
        public bool[] lightData;

        public void Initialize(RCCP_Damage damageComponent) {

            originalMeshData = damageComponent.originalMeshData;
            damagedMeshData = damageComponent.damagedMeshData;
            originalWheelData = damageComponent.originalWheelData;
            damagedWheelData = damageComponent.damagedWheelData;
            lightData = new bool[damageComponent.lights.Length];

            for (int i = 0; i < lightData.Length; i++) {

                if (damageComponent.lights[i] != null)
                    lightData[i] = damageComponent.lights[i].broken;

            }

        }

    }

    /// <summary>
    /// Creating a new damage data.
    /// </summary>
    public DamageData damageData = new DamageData();

    /// <summary>
    /// Repairing now.
    /// </summary>
    [Space()] public bool repairNow = false;

    /// <summary>
    /// Returns true if vehicle is completely repaired.
    /// </summary>
    public bool repaired = true;

    /// <summary>
    /// Deforming the mesh now.
    /// </summary>
    public bool deformingNow = false;

    /// <summary>
    /// Returns true if vehicle is completely deformed.
    /// </summary>
    public bool deformed = false;

    /// <summary>
    /// Recalculate normals while deforming / restoring the mesh.
    /// </summary>
    [Space()] public bool recalculateNormals = false;

    /// <summary>
    /// Recalculate bounds while deforming / restoring the mesh.
    /// </summary>
    public bool recalculateBounds = false;

    /// <summary>
    /// If true, uses wheel damage (wheel deformation).
    /// </summary>
    [Space()] public bool wheelDamage = true;

    /// <summary>
    /// Wheel damage radius.
    /// </summary>
    [Min(0f)] public float wheelDamageRadius = 2f;

    /// <summary>
    /// Wheel damage multiplier.
    /// </summary>
    [Min(0f)] public float wheelDamageMultiplier = 1f;

    /// <summary>
    /// If true, allows wheel detachment when heavily damaged.
    /// </summary>
    public bool wheelDetachment = true;

    /// <summary>
    /// If true, uses light damage (light deformation).
    /// </summary>
    [Space()] public bool lightDamage = true;

    /// <summary>
    /// Light damage radius.
    /// </summary>
    [Min(0f)] public float lightDamageRadius = .75f;

    /// <summary>
    /// Light damage multiplier.
    /// </summary>
    [Min(0f)] public float lightDamageMultiplier = 1f;

    /// <summary>
    /// If true, uses part damage (detachable parts).
    /// </summary>
    [Space()] public bool partDamage = true;

    /// <summary>
    /// Part damage radius.
    /// </summary>
    [Min(0f)] public float partDamageRadius = 1f;

    /// <summary>
    /// Part damage multiplier.
    /// </summary>
    [Min(0f)] public float partDamageMultiplier = 1f;

    /// <summary>
    /// Stores the contact point from the last collision.
    /// </summary>
    private ContactPoint contactPoint = new ContactPoint();

    public RCCP_Octree[] octrees;

    /// <summary>
    /// Collecting all meshes and detachable parts of the vehicle.
    /// </summary>
    public override void Start() {

        base.Start();
        Initialize();

    }

    private void Initialize() {

        if (initialized)
            return;

        if (automaticInstallation) {

            RCCP_CarController carController = CarController;

            List<MeshFilter> properMeshFilters = new List<MeshFilter>(
                carController.GetComponentsInChildren<MeshFilter>(processInactiveGameobjects)
            );

            List<MeshFilter> filteredMeshFilters = new List<MeshFilter>();

            List<RCCP_WheelCollider> wheelColliders = new List<RCCP_WheelCollider>(
                carController.AllWheelColliders
            );

            foreach (MeshFilter meshFilter in properMeshFilters) {

                if (meshFilter == null)
                    continue;

                MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();

                if (renderer == null)
                    continue;

                // Check if the mesh is readable
                if (!meshFilter.mesh.isReadable) {
                    Debug.LogError(
                        "Not deformable mesh detected. Mesh of the "
                        + meshFilter.transform.name
                        + " isReadable is false; Read/Write must be enabled in import settings!"
                    );

                }

                // We'll use a 'skip' flag to decide if we should exclude this MeshFilter
                bool skip = false;

                // If we do have wheelColliders, let's see if this mesh belongs to any wheel
                if (wheelColliders != null && wheelColliders.Count > 0) {

                    foreach (RCCP_WheelCollider wc in wheelColliders) {

                        if (wc == null)
                            continue;

                        // If the wheelModel is null, decide what you want to do:
                        // The original code added the mesh automatically if wheelModel was null.
                        // If you want to skip, set skip = true; or if you want to add, do nothing here.
                        // For now, let's do nothing, so we only skip if it's actually the child 
                        // of a real wheelModel that exists.
                        if (wc.wheelModel == null)
                            continue;

                        // If it's the same transform OR a child of the wheelModel, then skip
                        if (meshFilter.transform == wc.wheelModel ||
                            meshFilter.transform.IsChildOf(wc.wheelModel)) {

                            skip = true;
                            break;  // No need to check other wheels

                        }

                    }

                }

                // If we haven't marked it 'skip', then add to filtered list
                if (!skip && !filteredMeshFilters.Contains(meshFilter))
                    filteredMeshFilters.Add(meshFilter);

            }

            meshFilters = filteredMeshFilters.ToArray();

            parts = CarController.GetComponentsInChildren<RCCP_DetachablePart>(processInactiveGameobjects);
            lights = CarController.GetComponentsInChildren<RCCP_Light>(processInactiveGameobjects);
            wheels = CarController.GetComponentsInChildren<RCCP_WheelCollider>(processInactiveGameobjects);

        }

        CheckMeshData();
        CheckWheelData();

        initialized = true;

    }

    private void Update() {

        if (RCCP_SceneManager.multithreadingSupported) {

            if (!repaired && repairNow)
                CheckRepair();

            if (!deformed && deformingNow)
                CheckDamage();

        } else {

            if (!repaired && repairNow)
                CheckRepairRaw();

            if (!deformed && deformingNow)
                CheckDamageRaw();

        }

    }

    /// <summary>
    /// We will be using two structs for deformed sections: original and damaged. 
    /// All damaged meshes and wheel transforms will use these structs. At this step, we're creating them using the original structure.
    /// </summary>
    private void CheckMeshData() {

        originalMeshData = new OriginalMeshVerts[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++) {

            meshFilters[i].mesh.MarkDynamic();
            originalMeshData[i].meshVerts = meshFilters[i].mesh.vertices;

        }

        damagedMeshData = new OriginalMeshVerts[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
            damagedMeshData[i].meshVerts = meshFilters[i].mesh.vertices;

    }

    /// <summary>
    /// We will be using two structs for deformed sections: original and damaged. 
    /// All damaged meshes and wheel transforms will use these structs. At this step, we're creating them using the original structure.
    /// </summary>
    private void CheckWheelData() {

        originalWheelData = new OriginalWheelPos[CarController.AllWheelColliders.Length];

        for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

            originalWheelData[i].wheelPosition = CarController.AllWheelColliders[i].transform.localPosition;
            originalWheelData[i].wheelRotation = CarController.AllWheelColliders[i].transform.localRotation;

        }

        damagedWheelData = new OriginalWheelPos[CarController.AllWheelColliders.Length];

        for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

            damagedWheelData[i].wheelPosition = CarController.AllWheelColliders[i].transform.localPosition;
            damagedWheelData[i].wheelRotation = CarController.AllWheelColliders[i].transform.localRotation;

        }

    }

    /// <summary>
    /// Repairs the mesh data (moving deformed vertices back to original positions, done in the main thread).
    /// </summary>
    public void CheckRepairRaw() {

        if (!CarController || !initialized)
            return;

        //  If vehicle is not fully repaired and 'repairNow' is true, restore all deformed meshes to their original structure.
        if (!repaired && repairNow) {

            repaired = true;
            repairNow = false;

            int k;

            for (k = 0; k < meshFilters.Length; k++) {

                if (meshFilters[k] != null && meshFilters[k].mesh != null) {

                    Vector3[] vertices = meshFilters[k].mesh.vertices;

                    for (int i = 0; i < vertices.Length; i++) {

                        vertices[i] += (originalMeshData[k].meshVerts[i] - vertices[i]);

                        if ((originalMeshData[k].meshVerts[i] - vertices[i]).sqrMagnitude >= (minimumVertDistanceForDamagedMesh * minimumVertDistanceForDamagedMesh))
                            repaired = false;

                    }

                    damagedMeshData[k].meshVerts = vertices;

                    meshFilters[k].mesh.SetVertices(vertices);

                    if (recalculateNormals)
                        meshFilters[k].mesh.RecalculateNormals();

                    if (recalculateBounds)
                        meshFilters[k].mesh.RecalculateBounds();

                }

            }

            for (k = 0; k < CarController.AllWheelColliders.Length; k++) {

                if (CarController.AllWheelColliders[k] != null) {

                    Vector3 wheelPos = CarController.AllWheelColliders[k].transform.localPosition;
                    wheelPos += (originalWheelData[k].wheelPosition - wheelPos);

                    if ((originalWheelData[k].wheelPosition - wheelPos).sqrMagnitude >= (minimumVertDistanceForDamagedMesh * minimumVertDistanceForDamagedMesh))
                        repaired = false;

                    damagedWheelData[k].wheelPosition = wheelPos;

                    CarController.AllWheelColliders[k].transform.localPosition = wheelPos;
                    CarController.AllWheelColliders[k].transform.localRotation = Quaternion.identity;

                    CarController.AllWheelColliders[k].OnRepair();
                    CarController.AllWheelColliders[k].Inflate();

                }

            }

            for (int i = 0; i < parts.Length; i++) {

                if (parts[i] != null)
                    parts[i].OnRepair();

            }

            if (CarController.Lights) {

                for (int i = 0; i < CarController.Lights.lights.Count; i++) {

                    if (CarController.Lights.lights[i] != null)
                        CarController.Lights.lights[i].OnRepair();

                }

            }

            if (repaired)
                repairNow = false;

        }

    }

    /// <summary>
    /// Damages the mesh data (moving original vertices to deformed positions, done in the main thread).
    /// </summary>
    public void CheckDamageRaw() {

        if (!CarController || !initialized)
            return;

        if (!deformed && deformingNow) {

            deformed = true;
            deformingNow = false;

            int k;

            for (k = 0; k < meshFilters.Length; k++) {

                if (meshFilters[k] != null && meshFilters[k].mesh != null) {

                    Vector3[] vertices = meshFilters[k].mesh.vertices;
                    Mesh mesh = meshFilters[k].mesh;

                    for (int i = 0; i < vertices.Length; i++)
                        vertices[i] += (damagedMeshData[k].meshVerts[i] - vertices[i]);

                    mesh.SetVertices(vertices);

                    if (recalculateNormals)
                        mesh.RecalculateNormals();

                    if (recalculateBounds)
                        mesh.RecalculateBounds();

                }

            }

            for (k = 0; k < CarController.AllWheelColliders.Length; k++) {

                if (CarController.AllWheelColliders[k] != null) {

                    Vector3 vertices = CarController.AllWheelColliders[k].transform.localPosition;
                    vertices += (damagedWheelData[k].wheelPosition - vertices);
                    CarController.AllWheelColliders[k].transform.localPosition = vertices;

                }

            }

            deformingNow = false;
            deformed = true;

        }

    }

    /// <summary>
    /// Deforms meshes around the contact point based on collision impulse.
    /// </summary>
    /// <param name="impulse"></param>
    private void DamageMesh(float impulse) {

        if (!CarController || !initialized)
            return;

        if (meshFilters == null || (meshFilters != null && meshFilters.Length < 1))
            return;

        if (originalMeshData == null || originalMeshData.Length < 1)
            CheckMeshData();

        Transform carTransform = CarController.transform;
        Vector3 localContactPointRelativeToRoot = carTransform.InverseTransformPoint(contactPoint.point);
        Vector3 collisionDirection = -(localContactPointRelativeToRoot).normalized;

        if (octrees == null || (octrees != null && octrees.Length < 1))
            octrees = new RCCP_Octree[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++) {

            MeshFilter currentMeshFilter = meshFilters[i];

            if (currentMeshFilter == null || currentMeshFilter.mesh == null || !currentMeshFilter.gameObject.activeSelf)
                continue;

            if (octrees[i] == null) {

                octrees[i] = new RCCP_Octree(currentMeshFilter);

                foreach (var vertex in currentMeshFilter.mesh.vertices)
                    octrees[i].Insert(vertex);

            }

            Vector3 localContactPointRelativeToMesh = currentMeshFilter.transform.InverseTransformPoint(contactPoint.point);
            Vector3 nearestVert = NearestVertexWithOctree(i, localContactPointRelativeToMesh, currentMeshFilter);
            float distance = (nearestVert - localContactPointRelativeToMesh).sqrMagnitude;

            if (distance <= (deformationRadius * deformationRadius)) {

                Vector3[] vertices = damagedMeshData[i].meshVerts;

                for (int k = 0; k < vertices.Length; k++) {

                    float distanceToVertSqr = (localContactPointRelativeToMesh - vertices[k]).sqrMagnitude;

                    if (distanceToVertSqr <= (deformationRadius * deformationRadius)) {

                        float damage = impulse * (1f - Mathf.Clamp01(Mathf.Sqrt(distanceToVertSqr) / deformationRadius));
                        vertices[k] += collisionDirection * damage * (deformationMultiplier / 10f);

                        if (maximumDamage > 0f && (vertices[k] - originalMeshData[i].meshVerts[k]).sqrMagnitude > (maximumDamage * maximumDamage))
                            vertices[k] = originalMeshData[i].meshVerts[k] + (vertices[k] - originalMeshData[i].meshVerts[k]).normalized * maximumDamage;

                    }

                }

            }

        }

    }

    /// <summary>
    /// Repairs mesh data asynchronously (moving deformed vertices back to original positions).
    /// </summary>
    public async void CheckRepair() {

        if (!CarController || !initialized)
            return;

        if (!repaired && repairNow) {

            repaired = true;
            repairNow = false;

            int k;

            for (k = 0; k < meshFilters.Length; k++) {

                if (meshFilters[k] != null && meshFilters[k].mesh != null) {

                    Vector3[] vertices = meshFilters[k].mesh.vertices;

                    if (cancellationTokenSource == null)
                        cancellationTokenSource = new CancellationTokenSource();

                    await Task.Run(() => {

                        for (int i = 0; i < vertices.Length; i++) {

                            vertices[i] += (originalMeshData[k].meshVerts[i] - vertices[i]);

                            if ((originalMeshData[k].meshVerts[i] - vertices[i]).sqrMagnitude >= (minimumVertDistanceForDamagedMesh * minimumVertDistanceForDamagedMesh))
                                repaired = false;

                        }

                    }, cancellationTokenSource.Token);

                    if (cancellationTokenSource.IsCancellationRequested)
                        return;

                    damagedMeshData[k].meshVerts = vertices;
                    meshFilters[k].mesh.SetVertices(vertices);

                    if (recalculateNormals)
                        meshFilters[k].mesh.RecalculateNormals();

                    if (recalculateBounds)
                        meshFilters[k].mesh.RecalculateBounds();

                }

            }

            for (k = 0; k < CarController.AllWheelColliders.Length; k++) {

                if (CarController.AllWheelColliders[k] != null) {

                    Vector3 wheelPos = CarController.AllWheelColliders[k].transform.localPosition;
                    wheelPos += (originalWheelData[k].wheelPosition - wheelPos);

                    if ((originalWheelData[k].wheelPosition - wheelPos).sqrMagnitude >= (minimumVertDistanceForDamagedMesh * minimumVertDistanceForDamagedMesh))
                        repaired = false;

                    damagedWheelData[k].wheelPosition = wheelPos;

                    CarController.AllWheelColliders[k].transform.localPosition = wheelPos;
                    CarController.AllWheelColliders[k].transform.localRotation = Quaternion.identity;

                    CarController.AllWheelColliders[k].OnRepair();
                    CarController.AllWheelColliders[k].Inflate();

                }

            }

            for (int i = 0; i < parts.Length; i++) {

                if (parts[i] != null)
                    parts[i].OnRepair();

            }

            if (CarController.Lights) {

                for (int i = 0; i < CarController.Lights.lights.Count; i++) {

                    if (CarController.Lights.lights[i] != null)
                        CarController.Lights.lights[i].OnRepair();

                }

            }

            if (repaired)
                repairNow = false;

        }

    }

    /// <summary>
    /// Damages mesh data asynchronously (moving original vertices to deformed positions).
    /// </summary>
    public async void CheckDamage() {

        if (!CarController || !initialized)
            return;

        if (!deformed && deformingNow) {

            deformed = true;
            deformingNow = false;

            int k;

            for (k = 0; k < meshFilters.Length; k++) {

                if (meshFilters[k] != null && meshFilters[k].mesh != null) {

                    Vector3[] vertices = meshFilters[k].mesh.vertices;
                    Mesh mesh = meshFilters[k].mesh;

                    if (cancellationTokenSource == null)
                        cancellationTokenSource = new CancellationTokenSource();

                    await Task.Run(() => {

                        for (int i = 0; i < vertices.Length; i++) {

                            if (cancellationTokenSource.IsCancellationRequested)
                                return;

                            vertices[i] += (damagedMeshData[k].meshVerts[i] - vertices[i]);

                        }

                    }, cancellationTokenSource.Token);

                    if (cancellationTokenSource.IsCancellationRequested)
                        return;

                    mesh.SetVertices(vertices);

                    if (recalculateNormals)
                        mesh.RecalculateNormals();

                    if (recalculateBounds)
                        mesh.RecalculateBounds();

                }

            }

            for (k = 0; k < CarController.AllWheelColliders.Length; k++) {

                if (CarController.AllWheelColliders[k] != null) {

                    Vector3 vertices = CarController.AllWheelColliders[k].transform.localPosition;
                    vertices += (damagedWheelData[k].wheelPosition - vertices);
                    CarController.AllWheelColliders[k].transform.localPosition = vertices;

                }

            }

        }

    }

    /// <summary>
    /// Deforms wheels by adjusting their local positions based on collision impulse.
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="impulse"></param>
    private void DamageWheel(float impulse) {

        if (!CarController || !initialized)
            return;

        if (originalWheelData == null || originalWheelData.Length < 1)
            CheckWheelData();

        Transform carTransform = CarController.transform;
        Vector3 collisionDirection = -((contactPoint.point - CarController.transform.position).normalized);

        for (int i = 0; i < wheels.Length; i++) {

            if (wheels[i] != null && wheels[i].gameObject.activeSelf) {

                Vector3 wheelPos = damagedWheelData[i].wheelPosition;
                Vector3 closestPoint = wheels[i].WheelCollider.ClosestPointOnBounds(contactPoint.point);
                float distanceSqr = (closestPoint - contactPoint.point).sqrMagnitude;

                if (distanceSqr < (wheelDamageRadius * wheelDamageRadius)) {

                    float damage = (impulse * wheelDamageMultiplier) / 30f;
                    damage -= damage * Mathf.Clamp01(distanceSqr / (wheelDamageRadius * wheelDamageRadius)) * .5f;

                    Vector3 vW = carTransform.TransformPoint(wheelPos);
                    vW += (collisionDirection * damage);
                    wheelPos = carTransform.InverseTransformPoint(vW);

                    if (maximumDamage > 0 && (wheelPos - originalWheelData[i].wheelPosition).sqrMagnitude > (maximumDamage * maximumDamage)) {

                        // If we want to limit damage instead of detaching:
                        // wheelPos = originalWheelData[i].wheelPosition + (wheelPos - originalWheelData[i].wheelPosition).normalized * maximumDamage;

                        if (wheelDetachment && wheels[i].gameObject.activeSelf)
                            DetachWheel(wheels[i]);

                    }

                    damagedWheelData[i].wheelPosition = wheelPos;

                }

            }

        }

    }

    /// <summary>
    /// Deforms the detachable parts.
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="impulse"></param>
    private void DamagePart(float impulse) {

        if (!CarController)
            return;

        if (!initialized)
            return;

        if (parts != null && parts.Length >= 1) {

            for (int i = 0; i < parts.Length; i++) {

                if (parts[i] != null && parts[i].gameObject.activeSelf) {

                    if (parts[i].partColliders != null) {

                        Vector3 closestPoint = parts[i].partColliders[0].ClosestPointOnBounds(contactPoint.point);
                        float distance = (closestPoint - contactPoint.point).sqrMagnitude;
                        float damage = impulse * partDamageMultiplier;

                        damage -= damage * Mathf.Clamp01(distance / (partDamageRadius * partDamageRadius)) * .5f;

                        if (distance <= (partDamageRadius * partDamageRadius))
                            parts[i].OnCollision(damage);

                    } else {

                        if ((contactPoint.point - parts[i].transform.position).sqrMagnitude < (partDamageRadius * partDamageRadius))
                            parts[i].OnCollision(impulse);

                    }

                }

            }

        }

    }

    /// <summary>
    /// Deforms the lights.
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="impulse"></param>
    private void DamageLight(float impulse) {

        if (!CarController)
            return;

        if (!initialized)
            return;

        if (lights != null && lights.Length >= 1) {

            for (int i = 0; i < lights.Length; i++) {

                if (lights[i] != null && lights[i].gameObject.activeSelf) {

                    if ((contactPoint.point - lights[i].transform.position).sqrMagnitude < (lightDamageRadius * lightDamageRadius)) {

                        float distance = (lights[i].transform.position - contactPoint.point).sqrMagnitude;
                        float damage = impulse * lightDamageMultiplier;

                        damage -= damage * Mathf.Clamp01(distance / (lightDamageRadius * lightDamageRadius)) * .5f;

                        if (distance <= (lightDamageRadius * lightDamageRadius))
                            lights[i].OnCollision(damage);

                    }

                }

            }

        }

    }

    /// <summary>
    /// Detaches the specified wheel from the vehicle.
    /// </summary>
    /// <param name="wheelCollider"></param>
    public void DetachWheel(RCCP_WheelCollider wheelCollider) {

        if (!CarController || !initialized)
            return;

        if (!wheelCollider)
            return;

        if (!wheelCollider.enabled)
            return;

        wheelCollider.DetachWheel();

    }

    /// <summary>
    /// Called when a collision occurs, calculates damage if the collision layer is within the damageFilter.
    /// </summary>
    /// <param name="collision">Collision data.</param>
    public void OnCollision(Collision collision) {

        if (!enabled)
            return;

        if (((1 << collision.gameObject.layer) & damageFilter) != 0) {

            float impulse = collision.impulse.magnitude / 7500f;

            if (impulse < minimumCollisionImpulse)
                impulse = 0f;

            if (impulse > 10f)
                impulse = 10f;

            if (impulse > 0f) {

                deformingNow = true;
                deformed = false;

                repairNow = false;
                repaired = false;

                contactPoint = collision.GetContact(0);

                if (meshDeformation && meshFilters != null && meshFilters.Length >= 1)
                    DamageMesh(impulse);

                if (wheelDamage && CarController.AllWheelColliders != null && CarController.AllWheelColliders.Length >= 1)
                    DamageWheel(impulse);

                if (partDamage && parts != null && parts.Length >= 1)
                    DamagePart(impulse);

                if (lightDamage && CarController.Lights && CarController.Lights.lights.Count >= 1)
                    DamageLight(impulse);

            }

        }

    }

    /// <summary>
    /// Finds the nearest vertex in local space to the given point, iterating all vertices (no octree).
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="mf"></param>
    /// <param name="point"></param>
    /// <returns>Nearest vertex in world space</returns>
    public Vector3 NearestVertexRaw(Transform trans, MeshFilter mf, Vector3 point) {

        point = trans.InverseTransformPoint(point);

        float minDistanceSqr = Mathf.Infinity;
        Vector3 nearestVertex = Vector3.zero;
        Vector3[] vertex = mf.mesh.vertices;

        for (int i = 0; i < vertex.Length; i++) {

            Vector3 diff = point - vertex[i];
            float distSqr = diff.sqrMagnitude;

            if (distSqr < minDistanceSqr) {

                minDistanceSqr = distSqr;
                nearestVertex = vertex[i];

            }

        }

        return nearestVertex;

    }

    /// <summary>
    /// Asynchronously finds the nearest vertex in local space to the given point (no octree).
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="mf"></param>
    /// <param name="point"></param>
    /// <returns>Nearest vertex in world space</returns>
    public async Task<Vector3> NearestVertex(Transform trans, MeshFilter mf, Vector3 point) {

        point = trans.InverseTransformPoint(point);

        float minDistanceSqr = Mathf.Infinity;
        Vector3 nearestVertex = Vector3.zero;
        Vector3[] vertex = mf.mesh.vertices;

        if (cancellationTokenSource == null)
            cancellationTokenSource = new CancellationTokenSource();

        await Task.Run(() => {

            for (int i = 0; i < vertex.Length; i++) {

                Vector3 diff = point - vertex[i];
                float distSqr = diff.sqrMagnitude;

                if (distSqr < minDistanceSqr) {

                    minDistanceSqr = distSqr;
                    nearestVertex = vertex[i];

                }

            }

            return nearestVertex;

        }, cancellationTokenSource.Token);

        if (cancellationTokenSource.IsCancellationRequested)
            return nearestVertex;

        return nearestVertex;

    }

    /// <summary>
    /// Finds the closest vertex to the contact point using an Octree for faster lookups.
    /// </summary>
    /// <param name="meshIndex">Index of the mesh in the meshFilters array.</param>
    /// <param name="contactPoint">Collision contact point in local space.</param>
    /// <param name="meshFilter">MeshFilter whose octree we want to query.</param>
    /// <returns>Nearest vertex in local space.</returns>
    public Vector3 NearestVertexWithOctree(int meshIndex, Vector3 contactPoint, MeshFilter meshFilter) {

        if (meshIndex < 0 || meshIndex >= octrees.Length || octrees[meshIndex] == null) {

            Debug.LogWarning("Invalid Octree or mesh index.");
            return Vector3.zero;

        }

        return octrees[meshIndex].FindNearestVertex(contactPoint, meshFilter);

    }

    /// <summary>
    /// Resets relevant states for the damage component.
    /// </summary>
    public void Reload() {

        // Reset process states
        repairNow = false;
        repaired = true;
        deformingNow = false;
        deformed = false;

        // Cancel any running async operations
        if (cancellationTokenSource != null) {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = null;
        }

        // Reset contact point data
        contactPoint = new ContactPoint();

    }

    /// <summary>
    /// Saves the current damage states into PlayerPrefs (JSON-based).
    /// </summary>
    public void Save() {

        if (!CarController || !initialized)
            return;

        if (RCCP_SceneManager.multithreadingSupported)
            RCCP_DamageData.SaveDamage(CarController, saveName);
        else
            RCCP_DamageData.SaveDamageRaw(CarController, saveName);

    }

    /// <summary>
    /// Loads the saved damage states from PlayerPrefs (JSON-based).
    /// </summary>
    public void Load() {

        if (!CarController || !initialized)
            return;

        if (RCCP_SceneManager.multithreadingSupported)
            RCCP_DamageData.LoadDamage(CarController, saveName);
        else
            RCCP_DamageData.LoadDamageRaw(CarController, saveName);

    }

    /// <summary>
    /// Deletes the saved damage data from PlayerPrefs.
    /// </summary>
    public void Delete() {

        PlayerPrefs.DeleteKey(saveName + "_DamageData");

    }

    /// <summary>
    /// Called when this component is destroyed or disabled; cancels any running tasks.
    /// </summary>
    public void OnDestroy() {

        if (cancellationTokenSource == null)
            cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.Cancel();

    }

    private void OnValidate() {

        if (saveName == "")
            saveName = GetComponentInParent<RCCP_CarController>(true).transform.name;

    }

}
