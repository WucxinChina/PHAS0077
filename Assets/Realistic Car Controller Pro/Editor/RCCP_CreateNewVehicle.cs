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
using UnityEditor;
using UnityEngine.Events;
using System;
using UnityEditor.Events;

public class RCCP_CreateNewVehicle {

    public static RCCP_CarController NewVehicle(GameObject vehicle) {

        if (vehicle == null)
            return null;

        if (vehicle.GetComponentInParent<RCCP_CarController>(true) != null) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Already Has RCCP_CarController", "Selected vehicle already has RCCP_CarController. Are you sure you didn't pick the wrong house, oh vehicle?", "Close");
            return null;

        }

        if (EditorUtility.IsPersistent(vehicle)) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Please select a vehicle in the scene", "Please select a vehicle in the scene, not in the project. Drag and drop the vehicle model to the scene, and try again.", "Close");
            return null;

        }

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(vehicle);

        if (isPrefab) {

            bool isModelPrefab = PrefabUtility.IsPartOfModelPrefab(vehicle);
            bool unpackPrefab = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Unpack Prefab", "This gameobject is connected to a " + (isModelPrefab ? "model" : "") + " prefab. Would you like to unpack the prefab completely? If you don't unpack it, you won't be able to move, reorder, or delete any children instance of the prefab.", "Unpack", "Don't Unpack");

            if (unpackPrefab)
                PrefabUtility.UnpackPrefabInstance(vehicle, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        bool foundRigids = false;

        if (vehicle.GetComponentInChildren<Rigidbody>(true))
            foundRigids = true;

        if (foundRigids) {

            bool removeRigids = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Rigidbodies Found", "Additional rigidbodies found in your vehicle. Additional rigidbodies will affect vehicle behavior directly.", "Remove Them", "Leave Them");

            if (removeRigids) {

                foreach (Rigidbody rigidbody in vehicle.GetComponentsInChildren<Rigidbody>(true))
                    UnityEngine.Object.DestroyImmediate(rigidbody);

            }

        }

        bool foundWheelColliders = false;

        if (vehicle.GetComponentInChildren<WheelCollider>(true))
            foundWheelColliders = true;

        if (foundWheelColliders) {

            bool removeWheelColliders = EditorUtility.DisplayDialog("Realistic Car Controller Pro | WheelColliders Found", "Additional wheelcolliders found in your vehicle.", "Remove Them", "Leave Them");

            if (removeWheelColliders) {

                foreach (WheelCollider wc in vehicle.GetComponentsInChildren<WheelCollider>(true))
                    UnityEngine.Object.DestroyImmediate(wc);

            }

        }

        bool fixPivot = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Fix Pivot Position Of The Vehicle", "Would you like to fix pivot position of the vehicle? If your vehicle has correct pivot position, select no.", "Fix", "No");

        if (fixPivot) {

            GameObject pivot = new GameObject(vehicle.name);
            pivot.transform.position = RCCP_GetBounds.GetBoundsCenter(vehicle.transform);
            pivot.transform.rotation = vehicle.transform.rotation;

            pivot.AddComponent<RCCP_CarController>();

            vehicle.transform.SetParent(pivot.transform);
            Selection.activeGameObject = pivot;
            vehicle = pivot;

        } else {

            GameObject selectedVehicle = vehicle;

            selectedVehicle.AddComponent<RCCP_CarController>();
            Selection.activeGameObject = selectedVehicle;
            vehicle = selectedVehicle;

        }

        Rigidbody rigid = vehicle.GetComponent<Rigidbody>();
        rigid.mass = 1350f;
        rigid.linearDamping = .0025f;
        rigid.angularDamping = .35f;
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
        rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;

        RCCP_CarController newVehicle = vehicle.GetComponent<RCCP_CarController>();

        int answer = EditorUtility.DisplayDialogComplex("Adding Components", "Would you like to add all components (engine, clutch, gearbox, differential, and axle) automatically?", "Yes", "No", "");

        if (answer == 0)
            AddAllComponents(newVehicle);

        return newVehicle;

    }

    public static RCCP_CarController NewVehicle(GameObject vehicle, RCCP_SetupWizard.SetupData setupData) {

        if (vehicle == null)
            return null;

        if (vehicle.GetComponentInParent<RCCP_CarController>(true) != null) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Already Has RCCP_CarController", "Selected vehicle already has RCCP_CarController. Are you sure you didn't pick the wrong house, oh vehicle?", "Close");
            return null;

        }

        if (EditorUtility.IsPersistent(vehicle)) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Please select a vehicle in the scene", "Please select a vehicle in the scene, not in the project. Drag and drop the vehicle model to the scene, and try again.", "Close");
            return null;

        }

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(vehicle);

        if (isPrefab) {

            bool isModelPrefab = PrefabUtility.IsPartOfModelPrefab(vehicle);
            bool unpackPrefab = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Unpack Prefab", "This gameobject is connected to a " + (isModelPrefab ? "model" : "") + " prefab. Would you like to unpack the prefab completely? If you don't unpack it, you won't be able to move, reorder, or delete any children instance of the prefab.", "Unpack", "Don't Unpack");

            if (unpackPrefab)
                PrefabUtility.UnpackPrefabInstance(vehicle, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        bool foundRigids = false;

        if (vehicle.GetComponentInChildren<Rigidbody>(true))
            foundRigids = true;

        if (foundRigids) {

            bool removeRigids = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Rigidbodies Found", "Additional rigidbodies found in your vehicle. Additional rigidbodies will affect vehicle behavior directly.", "Remove Them", "Leave Them");

            if (removeRigids) {

                foreach (Rigidbody rigidbody in vehicle.GetComponentsInChildren<Rigidbody>(true))
                    UnityEngine.Object.DestroyImmediate(rigidbody);

            }

        }

        bool foundWheelColliders = false;

        if (vehicle.GetComponentInChildren<WheelCollider>(true))
            foundWheelColliders = true;

        if (foundWheelColliders) {

            bool removeWheelColliders = EditorUtility.DisplayDialog("Realistic Car Controller Pro | WheelColliders Found", "Additional wheelcolliders found in your vehicle.", "Remove Them", "Leave Them");

            if (removeWheelColliders) {

                foreach (WheelCollider wc in vehicle.GetComponentsInChildren<WheelCollider>(true))
                    UnityEngine.Object.DestroyImmediate(wc);

            }

        }

        bool fixPivot = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Fix Pivot Position Of The Vehicle", "Would you like to fix pivot position of the vehicle? If your vehicle has correct pivot position, select no.", "Fix", "No");

        if (fixPivot) {

            GameObject pivot = new GameObject(vehicle.name);
            pivot.transform.position = RCCP_GetBounds.GetBoundsCenter(vehicle.transform);
            pivot.transform.rotation = vehicle.transform.rotation;

            pivot.AddComponent<RCCP_CarController>();

            vehicle.transform.SetParent(pivot.transform);
            Selection.activeGameObject = pivot;
            vehicle = pivot;

        } else {

            GameObject selectedVehicle = vehicle;

            selectedVehicle.AddComponent<RCCP_CarController>();
            Selection.activeGameObject = selectedVehicle;
            vehicle = selectedVehicle;

        }

        Rigidbody rigid = Selection.activeGameObject.GetComponent<Rigidbody>();
        rigid.mass = setupData.mass;
        rigid.linearDamping = .0025f;
        rigid.angularDamping = .35f;
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
        rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;

        RCCP_CarController newVehicle = vehicle.GetComponent<RCCP_CarController>();

        AddAllComponents(newVehicle);
        AddAddonComponents(newVehicle, setupData);

        RCCP_Axle frontAxle = null;
        RCCP_Axle[] allAxles = newVehicle.GetComponentsInChildren<RCCP_Axle>(true);

        for (int i = 0; i < allAxles.Length; i++) {

            if (allAxles[i].transform.name == "RCCP_Axle_Front")
                frontAxle = allAxles[i];

        }

        if (frontAxle && setupData.frontWheels.Count >= 2 && setupData.frontWheels[0] && setupData.frontWheels[1])
            AssignWheelsToAxle(frontAxle, setupData.frontWheels[0], setupData.frontWheels[1]);

        RCCP_Axle rearAxle = null;

        for (int i = 0; i < allAxles.Length; i++) {

            if (allAxles[i].transform.name == "RCCP_Axle_Rear")
                rearAxle = allAxles[i];

        }

        if (rearAxle && setupData.rearWheels.Count >= 2 && setupData.rearWheels[0] && setupData.rearWheels[1])
            AssignWheelsToAxle(rearAxle, setupData.rearWheels[0], setupData.rearWheels[1]);

        ApplyEngineSettings(newVehicle, setupData);
        ApplyDifferentialSettings(newVehicle, setupData);
        ApplyWheelColliderSettings(newVehicle, setupData);
        ApplyHandlingSettings(newVehicle, setupData);

        newVehicle.transform.name = setupData.vehicleName;

        return newVehicle;

    }

    public static void ApplyEngineSettings(RCCP_CarController prop, RCCP_SetupWizard.SetupData setupData) {

        if (EditorUtility.IsPersistent(prop))
            return;

        RCCP_Engine engine = prop.GetComponentInChildren<RCCP_Engine>();

        engine.maximumTorqueAsNM = setupData.maxEngineTorque;
        engine.minEngineRPM = setupData.minEngineRPM;
        engine.maxEngineRPM = setupData.maxEngineRPM;
        engine.maximumSpeed = setupData.maxSpeed;
        engine.UpdateMaximumSpeed();

    }

    public static void ApplyHandlingSettings(RCCP_CarController prop, RCCP_SetupWizard.SetupData setupData) {

        if (EditorUtility.IsPersistent(prop))
            return;

        RCCP_Stability stability = prop.GetComponentInChildren<RCCP_Stability>();

        switch (setupData.handlingType) {

            case RCCP_SetupWizard.SetupData.HandlingType.Realistic:

                //stability.ABS = true;
                //stability.ESP = true;
                //stability.TCS = true;
                stability.steeringHelper = true;
                stability.tractionHelper = true;
                stability.angularDragHelper = true;

                //stability.ABSIntensity = .35f;
                //stability.ESPIntensity = .5f;
                //stability.TCSIntensity = .35f;

                stability.steerHelperStrength = .025f;
                stability.tractionHelperStrength = .05f;
                stability.angularDragHelperStrength = .075f;

                break;

            case RCCP_SetupWizard.SetupData.HandlingType.Balanced:

                //stability.ABS = true;
                //stability.ESP = true;
                //stability.TCS = true;
                stability.steeringHelper = true;
                stability.tractionHelper = true;
                stability.angularDragHelper = true;

                //stability.ABSIntensity = .5f;
                //stability.ESPIntensity = .65f;
                //stability.TCSIntensity = .5f;

                stability.steerHelperStrength = .1f;
                stability.tractionHelperStrength = .125f;
                stability.angularDragHelperStrength = .15f;

                break;

            case RCCP_SetupWizard.SetupData.HandlingType.Stable:

                //stability.ABS = true;
                //stability.ESP = true;
                //stability.TCS = true;
                stability.steeringHelper = true;
                stability.tractionHelper = true;
                stability.angularDragHelper = true;

                //stability.ABSIntensity = .75f;
                //stability.ESPIntensity = .85f;
                //stability.TCSIntensity = .75f;

                stability.steerHelperStrength = .2f;
                stability.tractionHelperStrength = .3f;
                stability.angularDragHelperStrength = .3f;

                break;

        }

    }

    public static void AssignWheelsToAxle(RCCP_Axle axle, GameObject wheel_L, GameObject wheel_R) {

        axle.leftWheelModel = wheel_L.transform;
        axle.rightWheelModel = wheel_R.transform;

        if (axle.leftWheelCollider) {

            axle.leftWheelCollider.wheelModel = axle.leftWheelModel;
            axle.leftWheelCollider.AlignWheel();

        }

        if (axle.rightWheelCollider) {

            axle.rightWheelCollider.wheelModel = axle.rightWheelModel;
            axle.rightWheelCollider.AlignWheel();

        }

    }

    public static void ApplyDifferentialSettings(RCCP_CarController prop, RCCP_SetupWizard.SetupData setupData) {

        if (EditorUtility.IsPersistent(prop))
            return;

        RCCP_Axle frontAxle = null;
        RCCP_Axle[] allAxles = prop.GetComponentsInChildren<RCCP_Axle>(true);

        for (int i = 0; i < allAxles.Length; i++) {

            if (allAxles[i].transform.name == "RCCP_Axle_Front")
                frontAxle = allAxles[i];

        }

        RCCP_Axle rearAxle = null;

        for (int i = 0; i < allAxles.Length; i++) {

            if (allAxles[i].transform.name == "RCCP_Axle_Rear")
                rearAxle = allAxles[i];

        }

        switch (setupData.driveType) {

            case RCCP_SetupWizard.SetupData.DriveType.FWD:

                prop.GetComponentInChildren<RCCP_Differential>().connectedAxle = frontAxle;
                break;

            case RCCP_SetupWizard.SetupData.DriveType.RWD:

                prop.GetComponentInChildren<RCCP_Differential>().connectedAxle = rearAxle;
                break;

            case RCCP_SetupWizard.SetupData.DriveType.AWD:

                GameObject refDif = prop.GetComponentInChildren<RCCP_Differential>().gameObject;
                RCCP_Differential newDif = GameObject.Instantiate(refDif, refDif.transform.parent).GetComponent<RCCP_Differential>();
                newDif.transform.SetSiblingIndex(refDif.transform.GetSiblingIndex() + 1);
                newDif.transform.name = refDif.transform.name + "_R";

                refDif.GetComponent<RCCP_Differential>().connectedAxle = frontAxle;
                newDif.connectedAxle = rearAxle;

                break;

        }

        AddGearboxToDifferentialListener(prop);

    }

    public static void ApplyWheelColliderSettings(RCCP_CarController prop, RCCP_SetupWizard.SetupData setupData) {

        if (EditorUtility.IsPersistent(prop))
            return;

        RCCP_WheelCollider[] wheelColliders = prop.GetComponentsInChildren<RCCP_WheelCollider>(true);

        for (int i = 0; i < wheelColliders.Length; i++) {

            WheelCollider wc = wheelColliders[i].GetComponent<WheelCollider>();
            JointSpring jS = wc.suspensionSpring;
            jS.spring = setupData.springForce;
            jS.damper = setupData.damperForce;
            wc.suspensionSpring = jS;

            wc.suspensionDistance = setupData.suspensionDistance;

        }

        WheelFrictionCurve wheelFrictionCurve_F = new WheelFrictionCurve();
        WheelFrictionCurve wheelFrictionCurve_S = new WheelFrictionCurve();

        switch (setupData.wheelType) {

            case RCCP_SetupWizard.SetupData.WheelType.Realistic:
                wheelFrictionCurve_F.extremumSlip = .4f;
                wheelFrictionCurve_F.extremumValue = 1f;
                wheelFrictionCurve_F.asymptoteSlip = .8f;
                wheelFrictionCurve_F.asymptoteValue = .5f;
                //
                wheelFrictionCurve_S.extremumSlip = .25f;
                wheelFrictionCurve_S.extremumValue = .95f;
                wheelFrictionCurve_S.asymptoteSlip = .5f;
                wheelFrictionCurve_S.asymptoteValue = .7f;
                break;

            case RCCP_SetupWizard.SetupData.WheelType.Balanced:
                wheelFrictionCurve_F.extremumSlip = .35f;
                wheelFrictionCurve_F.extremumValue = 1f;
                wheelFrictionCurve_F.asymptoteSlip = .8f;
                wheelFrictionCurve_F.asymptoteValue = .5f;
                //
                wheelFrictionCurve_S.extremumSlip = .2f;
                wheelFrictionCurve_S.extremumValue = 1f;
                wheelFrictionCurve_S.asymptoteSlip = .5f;
                wheelFrictionCurve_S.asymptoteValue = .75f;
                break;

            case RCCP_SetupWizard.SetupData.WheelType.Stable:
                wheelFrictionCurve_F.extremumSlip = .3f;
                wheelFrictionCurve_F.extremumValue = 1f;
                wheelFrictionCurve_F.asymptoteSlip = .8f;
                wheelFrictionCurve_F.asymptoteValue = .65f;
                //
                wheelFrictionCurve_S.extremumSlip = .2f;
                wheelFrictionCurve_S.extremumValue = 1f;
                wheelFrictionCurve_S.asymptoteSlip = .5f;
                wheelFrictionCurve_S.asymptoteValue = .65f;
                break;

            case RCCP_SetupWizard.SetupData.WheelType.Slippy:
                wheelFrictionCurve_F.extremumSlip = .4f;
                wheelFrictionCurve_F.extremumValue = 1f;
                wheelFrictionCurve_F.asymptoteSlip = .8f;
                wheelFrictionCurve_F.asymptoteValue = .5f;
                //
                wheelFrictionCurve_S.extremumSlip = .375f;
                wheelFrictionCurve_S.extremumValue = .95f;
                wheelFrictionCurve_S.asymptoteSlip = .5f;
                wheelFrictionCurve_S.asymptoteValue = .6f;
                break;

        }

        for (int i = 0; i < wheelColliders.Length; i++) {

            WheelCollider wc = wheelColliders[i].GetComponent<WheelCollider>();
            wc.forwardFriction = wheelFrictionCurve_F;
            wc.sidewaysFriction = wheelFrictionCurve_S;

        }

    }

    public static void AddAllComponents(RCCP_CarController prop) {

        AddEngine(prop);
        AddClutch(prop);
        AddGearbox(prop);
        AddAxles(prop);
        AddDifferential(prop);
        AddDifferentialToAxle(prop);
        AddEngineToClutchListener(prop);
        AddClutchToGearboxListener(prop);
        AddGearboxToDifferentialListener(prop);

    }

    public static void AddAddonComponents(RCCP_CarController prop, RCCP_SetupWizard.SetupData setupData) {

        if (setupData.addInputs)
            AddInputs(prop);
        if (setupData.addDynamics)
            AddAero(prop);
        if (setupData.addStability)
            AddStability(prop);
        if (setupData.addAudio)
            AddAudio(prop);
        if (setupData.addCustomizer)
            AddCustomizer(prop);

        if (setupData.addDamage)
            AddDamage(prop);
        if (setupData.addLights)
            AddLights(prop);
        if (setupData.addLOD)
            AddLOD(prop);
        if (setupData.addOtherAddons)
            AddOtherAddons(prop);
        if (setupData.addParticles)
            AddParticles(prop);

    }

    public static void AddEngine(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Engine>(true))
            return;

        GameObject subject = new GameObject("RCCP_Engine");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        subject.AddComponent<RCCP_Engine>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddClutch(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Clutch>(true))
            return;

        GameObject subject = new GameObject("RCCP_Clutch");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(1);
        subject.gameObject.AddComponent<RCCP_Clutch>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddGearbox(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Gearbox>(true))
            return;

        GameObject subject = new GameObject("RCCP_Gearbox");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(2);
        subject.gameObject.AddComponent<RCCP_Gearbox>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddDifferential(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Differential");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(3);
        subject.gameObject.AddComponent<RCCP_Differential>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAxles(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Axles>(true))
            return;

        GameObject subject = new GameObject("RCCP_Axles");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(4);
        subject.gameObject.AddComponent<RCCP_Axles>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAxle(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Axle_New");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(4);
        RCCP_Axle axle = subject.gameObject.AddComponent<RCCP_Axle>();
        axle.gameObject.name = "RCCP_Axle_New";
        axle.isBrake = true;
        axle.isHandbrake = true;

        EditorUtility.SetDirty(prop);

    }

    public static void AddInputs(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Input>(true))
            return;

        GameObject subject = new GameObject("RCCP_Inputs");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(5);
        subject.gameObject.AddComponent<RCCP_Input>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAero(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_AeroDynamics>(true))
            return;

        GameObject subject = new GameObject("RCCP_Aero");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(6);
        subject.gameObject.AddComponent<RCCP_AeroDynamics>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAudio(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Audio>(true))
            return;

        GameObject subject = new GameObject("RCCP_Audio");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(7);
        subject.gameObject.AddComponent<RCCP_Audio>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddCustomizer(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Customizer>(true))
            return;

        GameObject subject = new GameObject("RCCP_Customizer");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(8);
        subject.gameObject.AddComponent<RCCP_Customizer>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddStability(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Stability>(true))
            return;

        GameObject subject = new GameObject("RCCP_Stability");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(9);
        subject.gameObject.AddComponent<RCCP_Stability>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddLights(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Lights>(true))
            return;

        GameObject subject = new GameObject("RCCP_Lights");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(10);
        subject.gameObject.AddComponent<RCCP_Lights>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddDamage(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Damage>(true))
            return;

        GameObject subject = new GameObject("RCCP_Damage");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(11);
        subject.gameObject.AddComponent<RCCP_Damage>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddParticles(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Particles>(true))
            return;

        GameObject subject = new GameObject("RCCP_Particles");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(12);
        subject.gameObject.AddComponent<RCCP_Particles>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddLOD(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Lod>(true))
            return;

        GameObject subject = new GameObject("RCCP_LOD");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(13);
        subject.gameObject.AddComponent<RCCP_Lod>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddOtherAddons(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_OtherAddons>(true))
            return;

        GameObject subject = new GameObject("RCCP_OtherAddons");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(13);
        subject.gameObject.AddComponent<RCCP_OtherAddons>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddEngineToClutchListener(RCCP_CarController prop) {

        RCCP_Engine engine = prop.GetComponentInChildren<RCCP_Engine>(true);
        RCCP_Clutch clutch = prop.GetComponentInChildren<RCCP_Clutch>(true);

        engine.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(clutch,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), clutch, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(engine.outputEvent, methodDelegate);

    }

    public static void AddClutchToGearboxListener(RCCP_CarController prop) {

        RCCP_Gearbox gearbox = prop.GetComponentInChildren<RCCP_Gearbox>(true);
        RCCP_Clutch clutch = prop.GetComponentInChildren<RCCP_Clutch>(true);

        clutch.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(gearbox,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), gearbox, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(clutch.outputEvent, methodDelegate);

    }

    public static void AddGearboxToDifferentialListener(RCCP_CarController prop) {

        // find the gearbox component (even if inactive)
        RCCP_Gearbox gearbox = prop.GetComponentInChildren<RCCP_Gearbox>(true);
        // find all differentials in children (even if inactive)
        RCCP_Differential[] differentials = prop.GetComponentsInChildren<RCCP_Differential>(true);

        // ensure we have an output event to bind to
        if (gearbox.outputEvent == null) {
            gearbox.outputEvent = new RCCP_Event_Output();
        }

        gearbox.outputEvent = new RCCP_Event_Output();

        // for each differential, bind its ReceiveOutput method
        foreach (RCCP_Differential diff in differentials) {
            // look up the ReceiveOutput(RCCP_Output) method on this differential
            var methodInfo = UnityEvent.GetValidMethodInfo(
                diff,
                "ReceiveOutput",
                new Type[] { typeof(RCCP_Output) }
            );

            // create a UnityAction<RCCP_Output> delegate pointing at diff.ReceiveOutput
            var action = Delegate.CreateDelegate(
                typeof(UnityAction<RCCP_Output>),
                diff,
                methodInfo
            ) as UnityAction<RCCP_Output>;

            // add it as a persistent listener
            UnityEventTools.AddPersistentListener(gearbox.outputEvent, action);

        }

    }

    public static void AddDifferentialToAxle(RCCP_CarController prop) {

        RCCP_Axles axles = prop.GetComponentInChildren<RCCP_Axles>(true);
        RCCP_Differential differential = prop.GetComponentInChildren<RCCP_Differential>(true);

        if (!axles)
            return;

        float[] indexes = new float[axles.GetComponentsInChildren<RCCP_Axle>(true).Length];

        if (indexes.Length < 1)
            return;

        for (int i = 0; i < indexes.Length; i++)
            indexes[i] = axles.GetComponentsInChildren<RCCP_Axle>(true)[i].leftWheelCollider.transform.localPosition.z;

        int biggestIndex = 0;
        int lowestIndex = 0;

        for (int i = 0; i < indexes.Length; i++) {

            if (indexes[i] >= biggestIndex)
                biggestIndex = i;

            if (indexes[i] <= lowestIndex)
                lowestIndex = i;

        }

        RCCP_Axle rearAxle = axles.GetComponentsInChildren<RCCP_Axle>(true)[lowestIndex];

        if (rearAxle)
            differential.connectedAxle = rearAxle;

    }

}
