//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages a collection of brake zones and provides visualization in the Unity Editor.
/// This class stores references to all brake zones in the scene and ensures that their layers
/// are set to ignore raycasts to prevent interference with lens flare occlusion.
/// </summary>
public class RCC_AIBrakeZonesContainer : RCC_Core {

    /// <summary>
    /// List of brake zone transforms. Each transform represents a brake zone in the scene.
    /// </summary>
    public List<Transform> brakeZones = new List<Transform>();

    private void Awake() {

        // Ensures all brake zones are set to the "Ignore Raycast" layer
        // to prevent lens flare occlusion and unintended raycast interactions.
        foreach (var item in brakeZones)
            item.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

    }

    /// <summary>
    /// Draws gizmos in the Unity Editor to visualize the brake zones.
    /// This helps designers easily identify brake zones and adjust them if necessary.
    /// </summary>
    private void OnDrawGizmos() {

        for (int i = 0; i < brakeZones.Count; i++) {

            // Sets the gizmo matrix to match the local-to-world transform of the brake zone.
            Gizmos.matrix = brakeZones[i].transform.localToWorldMatrix;

            // Sets the gizmo color to a semi-transparent red.
            Gizmos.color = new Color(1f, 0f, 0f, .25f);

            // Retrieves the size of the BoxCollider attached to the brake zone.
            Vector3 colliderBounds = brakeZones[i].GetComponent<BoxCollider>().size;

            // Draws a semi-transparent cube to represent the brake zone.
            Gizmos.DrawCube(Vector3.zero, colliderBounds);

        }

    }

}
