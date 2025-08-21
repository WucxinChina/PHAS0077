//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RCCP_OctreeNode {

    public MeshFilter meshFilter;

    public Bounds bounds;

    [System.NonSerialized]
    public List<Vector3> vertices;

    [System.NonSerialized]
    public RCCP_OctreeNode[] children;

    public bool IsLeaf => children == null;

    public RCCP_OctreeNode(MeshFilter meshFilter) {

        this.meshFilter = meshFilter;
        this.bounds = meshFilter.mesh.bounds;
        this.bounds.center = meshFilter.mesh.bounds.center;

        vertices = new List<Vector3>();

    }

    public RCCP_OctreeNode(Bounds bounds) {

        this.bounds = bounds;
        this.bounds.center = bounds.center;

        vertices = new List<Vector3>();

    }

}
