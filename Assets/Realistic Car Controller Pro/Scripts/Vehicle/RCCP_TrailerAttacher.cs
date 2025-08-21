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
/// A specialized component for attaching truck trailers to vehicles. 
/// It should be placed on a trigger-based BoxCollider, usually near the vehicle's back (for the towing vehicle)
/// or near the trailer's front (for the trailer).
/// When two Trailer Attachers meet (one on the towing vehicle, the other on the trailer),
/// the trailer attaches to the vehicle.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Trailer Attacher")]
public class RCCP_TrailerAttacher : RCCP_Component {

    private RCCP_TrailerController _trailer;
    private BoxCollider trigger;

    /// <summary>
    /// If this is on a trailer, returns the RCCP_TruckTrailer component for that trailer.
    /// </summary>
    public RCCP_TrailerController Trailer {

        get {

            if (_trailer == null)
                _trailer = GetComponentInParent<RCCP_TrailerController>(true);

            return _trailer;

        }

    }

    /// <summary>
    /// If this attacher is on a vehicle that tows trailers, once a trailer is attached, it is referenced here.
    /// </summary>
    public RCCP_TrailerController attachedTrailer;

    private void OnTriggerEnter(Collider col) {

        // Check if the other collider has a trailer attacher.
        RCCP_TrailerAttacher otherAttacher = col.gameObject.GetComponent<RCCP_TrailerAttacher>();

        if (!otherAttacher)
            return;

        // If the other side is a trailer attacher but doesn't have an associated trailer, skip.
        if (!otherAttacher.Trailer)
            return;

        // We have encountered the trailer attacher from another object.
        attachedTrailer = otherAttacher.Trailer;

        // If our side belongs to a vehicle (CarController != null), let the other trailer attach.
        if (CarController)
            otherAttacher.Trailer.AttachTrailer(CarController);

    }

    private void Reset() {

        // Prepare the attacher's default collider settings.
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        trigger = GetComponent<BoxCollider>();

        if (!trigger)
            trigger = gameObject.AddComponent<BoxCollider>();

        trigger.isTrigger = true;
        trigger.size = new Vector3(1f, .5f, .1f);

        bool attachedToVehicle = GetComponentInParent<RCCP_CarController>(true);

        if (attachedToVehicle)
            trigger.size = new Vector3(.2f, .75f, .1f);

    }

    // ---------------------------------------------
    // GIZMO DRAWING
    // ---------------------------------------------
    private void OnDrawGizmos() {

        // Only draw if we actually have a BoxCollider
        BoxCollider box = GetComponent<BoxCollider>();

        if (box && box.isTrigger) {

            // Save current Gizmos matrix, then match it to our collider's transform.
            Gizmos.matrix = box.transform.localToWorldMatrix;

            // Set a color for the gizmo (e.g., green).
            Gizmos.color = Color.green;

            // Draw a wireframe cube matching the BoxCollider center & size.
            Gizmos.DrawWireCube(box.center, box.size);

        }

    }

}
