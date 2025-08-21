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

/// <summary>
/// Gets total bound size of a gameobject.
/// </summary>
public class RCCP_GetBounds {

    /// <summary>
    /// Gets the center bounds extent of object, including all child renderers,
    /// but excluding particles and trails, for FOV zooming effect.
    /// </summary>
    /// <returns>The bounds center.</returns>
    /// <param name="obj">Object.</param>
    public static Vector3 GetBoundsCenter(Transform obj) {

        var renderers = obj.GetComponentsInChildren<Renderer>();

        Bounds bounds = new Bounds();
        bool initBounds = false;

        foreach (Renderer r in renderers) {

            if (!((r is TrailRenderer) || (r is ParticleSystemRenderer))) {

                if (!initBounds) {

                    initBounds = true;
                    bounds = r.bounds;

                } else {

                    bounds.Encapsulate(r.bounds);

                }

            }

        }

        Vector3 center = bounds.center;
        return center;

    }

    /// <summary>
    /// Gets the maximum bounds extent of object, including all child renderers,
    /// but excluding particles and trails, for FOV zooming effect.
    /// </summary>
    /// <returns>The bounds extent.</returns>
    /// <param name="obj">Object.</param>
    public static float MaxBoundsExtent(Transform obj) {

        Quaternion quaternion = obj.rotation;
        obj.rotation = Quaternion.identity;

        var renderers = obj.GetComponentsInChildren<Renderer>();

        Bounds bounds = new Bounds();
        bool initBounds = false;

        foreach (Renderer r in renderers) {

            if (!((r is TrailRenderer) || (r is ParticleSystemRenderer))) {

                if (!initBounds) {

                    initBounds = true;
                    bounds = r.bounds;

                } else {

                    bounds.Encapsulate(r.bounds);

                }

            }

        }

        obj.rotation = quaternion;

        float max = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        return max;

    }

    public static MeshFilter GetBiggestMesh(Transform obj) {

        Quaternion quaternion = obj.rotation;
        obj.rotation = Quaternion.identity;

        MeshFilter[] mfs = obj.GetComponentsInChildren<MeshFilter>();
        MeshFilter biggestMesh = mfs[0];

        for (int i = 0; i < mfs.Length; i++) {

            if (mfs[i].mesh.bounds.size.magnitude > biggestMesh.mesh.bounds.size.magnitude)
                biggestMesh = mfs[i];

        }

        obj.rotation = quaternion;

        return biggestMesh;

    }

#if UNITY_EDITOR
    /// <summary>
    /// Returns the MeshFilter whose transformed (world–space) volume is the largest.
    /// </summary>
    public static MeshFilter GetBiggestMeshEditor(Transform root) {

        if (root == null) {
            return null;
        }

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        MeshFilter biggest = null;
        float biggestVolume = 0f;

        for (int i = 0; i < meshFilters.Length; i++) {

            Mesh mesh = meshFilters[i].sharedMesh;
            if (mesh == null) {
                continue;
            }

            // Mesh bounds in authoring space → scale to world space.
            Vector3 localSize = mesh.bounds.size;
            Vector3 worldSize = Vector3.Scale(localSize, meshFilters[i].transform.lossyScale);

            float volume = worldSize.x * worldSize.y * worldSize.z;     // more robust than magnitude

            if (volume > biggestVolume) {
                biggestVolume = volume;
                biggest = meshFilters[i];
            }

        }

        return biggest;

    }
#endif

}
