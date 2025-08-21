//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single node within an octree. Each node either holds a collection of vertices (leaf node) 
/// or has up to eight child nodes if it has been subdivided. The node also contains bounding volume information 
/// to quickly determine whether a vertex falls within its region of space.
/// </summary>
[System.Serializable]
public class RCC_OctreeNode {

    /// <summary>
    /// An optional reference to the MeshFilter from which this node's initial bounds were derived. 
    /// Used primarily for the root node.
    /// </summary>
    public MeshFilter meshFilter;

    /// <summary>
    /// The 3D bounding volume representing this node's region of space.
    /// </summary>
    public Bounds bounds;

    /// <summary>
    /// A list of vertex positions stored in this node if it is a leaf node.
    /// </summary>
    [System.NonSerialized]
    public List<Vector3> vertices;

    /// <summary>
    /// References to the child nodes if this node has subdivided. 
    /// If <c>null</c>, the node is considered a leaf.
    /// </summary>
    [System.NonSerialized]
    public RCC_OctreeNode[] children;

    /// <summary>
    /// A convenience property indicating whether this node is a leaf (no children).
    /// </summary>
    public bool IsLeaf => children == null;

    /// <summary>
    /// Constructs a new octree node using the bounds from a specified mesh filter.
    /// Typically used for the root node.
    /// </summary>
    /// <param name="meshFilter">The MeshFilter whose mesh defines the initial bounding region.</param>
    public RCC_OctreeNode(MeshFilter meshFilter) {

        this.meshFilter = meshFilter;

        // Initialize the bounding volume using the mesh's local bounds.
        this.bounds = meshFilter.mesh.bounds;
        this.bounds.center = meshFilter.mesh.bounds.center;

        // Prepare a list to store vertex positions that fall under this node.
        vertices = new List<Vector3>();

    }

    /// <summary>
    /// Constructs a new octree node with a specified bounding volume. 
    /// Typically used for subdivided child nodes.
    /// </summary>
    /// <param name="bounds">The bounding volume defining this node's region of space.</param>
    public RCC_OctreeNode(Bounds bounds) {

        this.bounds = bounds;
        this.bounds.center = bounds.center;

        // Prepare a list to store vertex positions that fall under this node.
        vertices = new List<Vector3>();

    }

}
