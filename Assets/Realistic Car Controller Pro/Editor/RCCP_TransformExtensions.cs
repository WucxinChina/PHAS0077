//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Extra Transform utilities for RCCP.  
/// Currently provides <c>InverseTransformBounds</c>, the opposite of
/// <c>Transform.TransformBounds</c> that Unity never shipped.
/// </summary>
public static class RCCP_TransformExtensions {

    /// <summary>
    /// Converts a world�space <paramref name="worldBounds"/> into the local
    /// space of <paramref name="transform"/> and returns the new Bounds.
    /// </summary>
    /// <remarks>
    /// � Works with any rotation / scale.<br/>
    /// � Size is reconstructed from the transformed axes to stay axis-aligned
    ///   in local space.<br/>
    /// � Suitable for quick placement helpers such as the Light Setup Wizard.
    /// </remarks>
    /// <param name="transform">Target local space.</param>
    /// <param name="worldBounds">World-space AABB to convert.</param>
    public static Bounds InverseTransformBounds(this Transform transform, Bounds worldBounds) {

        // Local centre
        Vector3 centerLS = transform.InverseTransformPoint(worldBounds.center);

        // Transform each world-space axis vector into local space and
        // capture their absolute extents (we need positive half-sizes).
        Vector3 extW = worldBounds.extents;

        Vector3 axisX = transform.InverseTransformVector(new Vector3(extW.x, 0f, 0f));
        Vector3 axisY = transform.InverseTransformVector(new Vector3(0f, extW.y, 0f));
        Vector3 axisZ = transform.InverseTransformVector(new Vector3(0f, 0f, extW.z));

        // Compose aligned size in local space
        Vector3 sizeLS = new Vector3(

            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)

        ) * 2f;   // convert half-size to full size

        return new Bounds(centerLS, sizeLS);

    }

}
