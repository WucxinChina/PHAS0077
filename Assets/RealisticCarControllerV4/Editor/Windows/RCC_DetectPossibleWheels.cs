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
using UnityEditor;

public class RCC_DetectPossibleWheels {

    public static GameObject[] DetectPossibleAllWheels(GameObject vehicle) {

        if (vehicle == null) {

            Debug.LogError("RCC: No vehicle GameObject was provided!");
            return new GameObject[0];

        }

        List<GameObject> allWheels = new List<GameObject>();
        MeshFilter[] meshFilters = vehicle.GetComponentsInChildren<MeshFilter>();

        if (meshFilters.Length == 0) {

            Debug.LogWarning("RCC: No MeshFilters found in the vehicle. Are you sure this model has wheels?");
            return new GameObject[0];

        }

        foreach (MeshFilter meshFilter in meshFilters) {

            if (meshFilter.sharedMesh == null)
                continue;

            Bounds bounds = meshFilter.sharedMesh.bounds;
            float depth = bounds.size.x;
            float height = bounds.size.y;
            float width = bounds.size.z;

            // More flexible wheel detection
            float aspectRatioTolerance = 0.1f;
            bool isCylindrical = Mathf.Abs(width - height) < aspectRatioTolerance && depth < width * 1.2f;
            bool isReasonableSize = width > 0.1f && width < 2.5f;

            if (isCylindrical && isReasonableSize) {

                allWheels.Add(meshFilter.gameObject);

            }

        }

        return allWheels.ToArray();

    }

    public static GameObject[] DetectPossibleFrontWheels(GameObject vehicle) {

        if (vehicle == null) {

            Debug.LogError("RCC: No vehicle GameObject was provided!");
            return new GameObject[0];

        }

        GameObject[] allWheels = DetectPossibleAllWheels(vehicle);
        List<GameObject> frontWheels = new List<GameObject>();

        foreach (GameObject wheel in allWheels) {

            if (IsInFront(vehicle, wheel))
                frontWheels.Add(wheel);

        }

        return frontWheels.ToArray();

    }

    public static GameObject[] DetectPossibleRearWheels(GameObject vehicle) {

        if (vehicle == null) {

            Debug.LogError("RCC: No vehicle GameObject was provided!");
            return new GameObject[0];

        }

        GameObject[] allWheels = DetectPossibleAllWheels(vehicle);
        List<GameObject> rearWheels = new List<GameObject>();

        foreach (GameObject wheel in allWheels) {

            if (!IsInFront(vehicle, wheel))
                rearWheels.Add(wheel);

        }

        return rearWheels.ToArray();

    }

    public static bool IsInFront(GameObject vehicle, GameObject wheel) {

        if (vehicle == null || wheel == null)
            return false;

        Vector3 parentForward = vehicle.transform.forward;
        Vector3 directionToChild = wheel.transform.position - vehicle.transform.position;

        return Vector3.Dot(parentForward, directionToChild) > 0;

    }

}
