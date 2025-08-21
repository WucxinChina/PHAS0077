//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Used for holding a list of waypoints, drawing gizmos,
/// smoothing the path, and snapping waypoints to the ground.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/AI/RCCP AI Waypoints Container")]
public class RCCP_AIWaypointsContainer : RCCP_GenericComponent {

    /// <summary>
    /// All waypoints in this container.
    /// </summary>
    public List<RCCP_Waypoint> waypoints = new List<RCCP_Waypoint>();

    /// <summary>
    /// Number of subdivisions per segment when smoothing.
    /// Increase for a finer curve.
    /// </summary>
    public int smoothingSubdivisions = 5;

    /// <summary>
    /// Height above the ground to place each waypoint.
    /// </summary>
    public float groundOffset = 1f;

    /// <summary>
    /// Starting height above the current waypoint position
    /// from which to cast downwards.
    /// </summary>
    private float raycastStartHeight = .15f;

    /// <summary>
    /// Layers to include when raycasting for ground.
    /// </summary>
    public LayerMask groundLayerMask = Physics.DefaultRaycastLayers;

    private void Awake() {

        //  Populate the list on load.
        GetAllWaypoints();

    }

    /// <summary>
    /// Finds all RCCP_Waypoint children and caches them.
    /// </summary>
    public void GetAllWaypoints() {

        if (waypoints == null)
            waypoints = new List<RCCP_Waypoint>();

        waypoints.Clear();
        waypoints = GetComponentsInChildren<RCCP_Waypoint>(true).ToList();

    }

    /// <summary>
    /// Draws gizmos for each waypoint and connecting lines.
    /// </summary>
    private void OnDrawGizmos() {

        if (waypoints == null)
            return;

        for (int i = 0; i < waypoints.Count; i++) {

            var wp = waypoints[i];

            if (wp != null && wp.gameObject.activeSelf) {

                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                //Gizmos.DrawSphere(wp.transform.position, 2f);
                Gizmos.DrawWireSphere(wp.transform.position, 20f);

                if (i < waypoints.Count - 1) {
                    var next = waypoints[i + 1];
                    if (next != null) {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(wp.transform.position, next.transform.position);

                        // loop back to first
                        if (i == waypoints.Count - 2) {
                            var first = waypoints[0];
                            if (first != null)
                                Gizmos.DrawLine(next.transform.position, first.transform.position);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Smooths the current waypoint path by performing Catmull–Rom interpolation.
    /// Replaces existing waypoints with new ones along the curve.
    /// </summary>
    [ContextMenu("Smooth Waypoints")]
    public void SmoothWaypoints() {

        if (waypoints == null || waypoints.Count < 2)
            return;

        // collect source positions
        List<Vector3> src = waypoints.Select(w => w.transform.position).ToList();
        List<Vector3> smoothed = new List<Vector3>();

        int count = src.Count;
        for (int i = 0; i < count; i++) {

            // four control points (looping)
            Vector3 p0 = src[(i - 1 + count) % count];
            Vector3 p1 = src[i];
            Vector3 p2 = src[(i + 1) % count];
            Vector3 p3 = src[(i + 2) % count];

            // subdivide
            for (int s = 0; s < smoothingSubdivisions; s++) {
                float t = s / (float)smoothingSubdivisions;
                smoothed.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        // remove old waypoints
        for (int i = waypoints.Count - 1; i >= 0; i--) {
            var wp = waypoints[i];
            if (wp != null)
                DestroyImmediate(wp.gameObject);
        }
        waypoints.Clear();

        // instantiate new curved waypoints
        for (int i = 0; i < smoothed.Count; i++) {
            GameObject go = new GameObject("RCCP_Waypoint_" + i);
            go.transform.parent = transform;
            go.transform.position = smoothed[i];

            var wp = go.AddComponent<RCCP_Waypoint>();
            waypoints.Add(wp);
        }
    }

    /// <summary>
    /// Adjusts all waypoints to sit uniformly above the ground.
    /// It raycasts down from a fixed height and applies the same offset.
    /// </summary>
    [ContextMenu("Place Waypoints Above Ground")]
    public void PlaceWaypointsAboveGround() {

        if (waypoints == null || waypoints.Count == 0)
            return;

        foreach (var wp in waypoints) {

            Vector3 rayOrigin = wp.transform.position + Vector3.up * raycastStartHeight;

            if (Physics.Raycast(rayOrigin,
                                  Vector3.down,
                                  out RaycastHit hit,
                                  raycastStartHeight * 2,
                                  groundLayerMask)) {

                Vector3 pos = wp.transform.position;
                pos.y = hit.point.y + groundOffset;
                wp.transform.position = pos;
            }
        }
    }

    /// <summary>
    /// Returns a point on a Catmull–Rom spline given four control points.
    /// </summary>
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

}
