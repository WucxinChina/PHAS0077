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
/// An octree data structure specialized for storing and searching vertices of a mesh.
/// The octree splits a region of space (defined by a node's bounds) into eight smaller child nodes 
/// when a specified vertex count or maximum depth is exceeded. 
/// This approach can accelerate nearest-vertex lookups by reducing the search area.
/// </summary>
[System.Serializable]
public class RCC_Octree {

    /// <summary>
    /// The root node of this octree. Holds a set of vertices or, 
    /// when subdivided, references to its eight child nodes.
    /// </summary>
    public RCC_OctreeNode root;

    /// <summary>
    /// The maximum depth (levels) this octree can have. 
    /// If a node is still over capacity at this depth, it will not subdivide further.
    /// </summary>
    private readonly int maxDepth = 20;

    /// <summary>
    /// The maximum number of vertices a node can hold before subdividing.
    /// </summary>
    private readonly int maxVerticesPerNode = 5000;

    /// <summary>
    /// Constructs a new octree using the bounds of a provided MeshFilter. 
    /// Initializes the root node of the octree with that mesh's bounding region.
    /// </summary>
    /// <param name="meshFilter">The MeshFilter containing the mesh to be inserted into the octree.</param>
    public RCC_Octree(MeshFilter meshFilter) {

        root = new RCC_OctreeNode(meshFilter);

    }

    /// <summary>
    /// Inserts a vertex into the octree, starting from the root node.
    /// </summary>
    /// <param name="vertex">The position of the vertex to insert.</param>
    public void Insert(Vector3 vertex) {

        Insert(root, vertex, 0);

    }

    /// <summary>
    /// Recursively inserts a vertex into a node's children or, if the node is a leaf, 
    /// adds it to the node's list. Subdivides if necessary.
    /// </summary>
    /// <param name="node">The current node we are inserting into.</param>
    /// <param name="vertex">The vertex position being inserted.</param>
    /// <param name="depth">Current depth in the octree.</param>
    private void Insert(RCC_OctreeNode node, Vector3 vertex, int depth) {

        // If node is a leaf, simply add the vertex.
        if (node.IsLeaf) {

            node.vertices.Add(vertex);

            // If we exceed capacity and have not reached max depth, subdivide this node.
            if (node.vertices.Count > maxVerticesPerNode && depth < maxDepth) {
                Subdivide(node);

                // Reinsert existing vertices into the newly created children.
                List<Vector3> verticesToReinsert = new List<Vector3>(node.vertices);
                node.vertices.Clear();

                foreach (Vector3 v in verticesToReinsert)
                    Insert(node, v, depth);
            }

        } else {

            // If this node is subdivided, find which child bounds contain the vertex and insert there.
            foreach (var child in node.children) {

                if (child.bounds.Contains(vertex)) {

                    Insert(child, vertex, depth + 1);
                    break;

                }

            }

        }

    }

    /// <summary>
    /// Subdivides a node into 8 child nodes, each occupying half the size of the current node's bounds.
    /// </summary>
    /// <param name="node">The node to subdivide.</param>
    private void Subdivide(RCC_OctreeNode node) {

        node.children = new RCC_OctreeNode[8];
        Vector3 size = node.bounds.size / 2f;
        Vector3 center = node.bounds.center;

        // Create 8 child nodes by splitting the current bounds into smaller volumes.
        for (int i = 0; i < 8; i++) {

            Vector3 newCenter = center + new Vector3(
                size.x * ((i & 1) == 0 ? -0.5f : 0.5f),
                size.y * ((i & 2) == 0 ? -0.5f : 0.5f),
                size.z * ((i & 4) == 0 ? -0.5f : 0.5f)
            );

            node.children[i] = new RCC_OctreeNode(new Bounds(newCenter, size));

        }

    }

    /// <summary>
    /// Finds the nearest vertex to a given point by traversing the octree. 
    /// Returns the coordinates of the closest vertex found.
    /// </summary>
    /// <param name="point">The point for which we seek the nearest vertex.</param>
    /// <param name="meshFilter">The mesh filter associated with this octree (not used in the current implementation).</param>
    /// <returns>The closest vertex to the specified point.</returns>
    public Vector3 FindNearestVertex(Vector3 point, MeshFilter meshFilter) {

        return FindNearestVertex(root, point, meshFilter);

    }

    /// <summary>
    /// Recursively searches in the octree node and its children for the closest vertex to the specified point.
    /// </summary>
    /// <param name="node">The current node being searched.</param>
    /// <param name="point">The point to measure distance from.</param>
    /// <param name="meshFilter">The mesh filter associated with this octree (not used in the current logic).</param>
    /// <returns>The nearest vertex found in this subtree.</returns>
    private Vector3 FindNearestVertex(RCC_OctreeNode node, Vector3 point, MeshFilter meshFilter) {

        float minDistSqr = Mathf.Infinity;
        Vector3 bestVertex = Vector3.zero;

        // If this node is a leaf, check each stored vertex.
        if (node.IsLeaf) {

            foreach (var vertex in node.vertices) {

                float distSqr = (vertex - point).sqrMagnitude;

                if (distSqr < minDistSqr) {

                    minDistSqr = distSqr;
                    bestVertex = vertex;

                }

            }

        } else {

            // For subdivided nodes, check each child whose bounds might contain a closer vertex.
            foreach (var child in node.children) {

                // Check distance heuristics before searching child nodes.
                if (child != null || child.bounds.SqrDistance(point) < minDistSqr) {

                    // Evaluate all vertices in the child node directly (as no deeper logic is coded here).
                    foreach (var vertex in child.vertices) {

                        float distSqr = (vertex - point).sqrMagnitude;

                        if (distSqr < minDistSqr) {

                            minDistSqr = distSqr;
                            bestVertex = vertex;

                        }

                    }

                }

            }

        }

        return bestVertex;

    }

    /// <summary>
    /// Clears the octree by recursively clearing all nodes and their data.
    /// This helps prevent memory leaks by properly releasing references.
    /// </summary>
    public void Clear() {

        if (root != null) {

            ClearNode(root);
            root = null;

        }

    }

    /// <summary>
    /// Recursively clears a node and all its children.
    /// </summary>
    /// <param name="node">The node to clear.</param>
    private void ClearNode(RCC_OctreeNode node) {

        if (node == null)
            return;

        // Clear vertices list
        if (node.vertices != null) {

            node.vertices.Clear();
            node.vertices = null;

        }

        // Recursively clear children
        if (node.children != null) {

            for (int i = 0; i < node.children.Length; i++) {

                if (node.children[i] != null) {

                    ClearNode(node.children[i]);
                    node.children[i] = null;

                }

            }

            node.children = null;

        }

    }

}