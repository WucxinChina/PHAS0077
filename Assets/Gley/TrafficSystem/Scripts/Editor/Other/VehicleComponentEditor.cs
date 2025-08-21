using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System;
using UnityEditor;
using UnityEngine;

namespace Gley.TrafficSystem.Editor
{
    [CustomEditor(typeof(VehicleComponent))]
    public class VehicleComponentEditor : UnityEditor.Editor
    {
        const string _carHolderName = "CarHolder";
        const string _frontTriggerHolderName = "FrontTriggerHolder";
        const string _frontTriggerName = "FrontTrigger";
        const string _wheelsHolderName = "Wheels";
        const string _shadowHolderName = "ShadowHolder";
        const string _maxTriggerLengthName = "Max Trigger Length";
        const string _updateTriggerName = "Update Trigger";
        const string _trailerComponentName = "Trailer Component";
        const string _trailerAnchorPointName = "Trailer Connection Point";
        const string _backPosition = "VehicleBackPosition";
        const string _frontPosition = "VehicleFrontPosition";

        private VehicleComponent _targetScript;


        private void OnEnable()
        {
            _targetScript = (VehicleComponent)target;
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();

            _targetScript.updateTrigger = EditorGUILayout.Toggle(new GUIContent(_updateTriggerName, "It checked, vehicle trigger length will increase with speed"), _targetScript.updateTrigger);
            if (_targetScript.updateTrigger)
            {
                _targetScript.maxTriggerLength = EditorGUILayout.FloatField(new GUIContent(_maxTriggerLengthName, "The length of the front trigger at max speed"), _targetScript.maxTriggerLength);
            }

            _targetScript.trailer = (TrailerComponent)EditorGUILayout.ObjectField(_trailerComponentName, _targetScript.trailer, typeof(TrailerComponent), true);
            if (_targetScript.trailer)
            {
                _targetScript.trailerConnectionPoint = (Transform)EditorGUILayout.ObjectField(_trailerAnchorPointName, _targetScript.trailerConnectionPoint, typeof(Transform), true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Automatically Computed", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Length: " + _targetScript.length.ToString());
            EditorGUILayout.LabelField("Collider Height: " + _targetScript.coliderHeight.ToString());
            EditorGUILayout.LabelField("Wheel Distance: " + _targetScript.wheelDistance.ToString());

            if (EditorGUI.EndChangeCheck())
            {
                Save(_targetScript);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Configure Car"))
            {
                ConfigureCar(_targetScript);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("View Tutorial"))
            {
                Application.OpenURL("https://youtu.be/moGHcd2Jaa4");
            }
        }


        public static void ConfigureCar(VehicleComponent targetScript)
        {
            bool correct = true;
            SetCarHolder(targetScript, ref correct);
            SetupRigidbody(targetScript, ref correct);
            AssignWheels(targetScript, ref correct);
            CreateFrontTrigger(targetScript, ref correct);
            AddShadow(targetScript);
            SetPivotOnBackWheels(targetScript, ref correct);
            SetWheelDimensions(targetScript, ref correct);
            CheckForZero(targetScript.accelerationTime, nameof(targetScript.accelerationTime), ref correct);
            CheckForZero(targetScript.minPossibleSpeed, nameof(targetScript.minPossibleSpeed), ref correct);
            CheckForZero(targetScript.maxPossibleSpeed, nameof(targetScript.maxPossibleSpeed), ref correct);
            AssignLights(targetScript);
            AssignVisibilityScript(targetScript);
            CheckColliers(targetScript, ref correct);
            ConnectTrailer(targetScript, ref correct);
            CheckLayer(targetScript, ref correct);
            GenerateBackTransform(targetScript, ref correct);

            if (correct)
            {
                Debug.Log("Success! All references for " + targetScript.name + " ware correct");
                Save(targetScript);
            }
            else
            {
                Debug.LogError(targetScript.name + " will not work correctly. See above messages for details");
            }
        }

        private static void GenerateBackTransform(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct || targetScript == null)
                return;

            // Get Bounds of the vehicle
            Bounds totalBounds;
            if (!TryGetVehicleBounds(targetScript, out totalBounds))
            {
                LogError(ref correct, $"No Renderer found on the {targetScript.name} vehicle.");
                return;
            }

            // Destroy existing transform points
            DestroyExistingTransforms(targetScript);

            // Compute world positions
            Vector3 backCenterWorld = new Vector3(totalBounds.center.x, totalBounds.min.y, totalBounds.min.z);
            Vector3 frontCenterWorld = new Vector3(totalBounds.center.x, totalBounds.min.y, totalBounds.max.z);

            // Convert to local positions
            Vector3 backCenterLocal = GetBackLocalPosition(targetScript, backCenterWorld, ref correct);
            Vector3 frontCenterLocal = targetScript.carHolder.InverseTransformPoint(frontCenterWorld);

            // Create and assign transforms
            CreateAndAssignTransforms(targetScript, backCenterLocal, frontCenterLocal);
        }

        private static bool TryGetVehicleBounds(VehicleComponent targetScript, out Bounds totalBounds)
        {
            Renderer[] renderers = targetScript.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                totalBounds = default;
                return false;
            }

            totalBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
            return true;
        }

        private static void DestroyExistingTransforms(VehicleComponent targetScript)
        {
            if (targetScript.HasTrailer)
            {
                GleyPrefabUtilities.DestroyTransform(targetScript.trailer?.trailerHolder?.Find(_backPosition));
            }
            else
            {
                GleyPrefabUtilities.DestroyTransform(targetScript.carHolder?.transform.Find(_backPosition));
            }

            GleyPrefabUtilities.DestroyTransform(targetScript.carHolder?.transform.Find(_frontPosition));
        }

        private static Vector3 GetBackLocalPosition(VehicleComponent targetScript, Vector3 backCenterWorld, ref bool correct)
        {
            if (targetScript.HasTrailer)
            {
                if (targetScript.trailer == null)
                {
                    LogError(ref correct, $"Trailer not assigned on {targetScript.name} vehicle.");
                    return Vector3.zero;
                }
                return targetScript.trailer.trailerHolder.InverseTransformPoint(backCenterWorld);
            }

            return targetScript.carHolder.InverseTransformPoint(backCenterWorld);
        }

        private static void CreateAndAssignTransforms(VehicleComponent targetScript, Vector3 backCenterLocal, Vector3 frontCenterLocal)
        {
            var backObject = CreateTransform(_backPosition, backCenterLocal, targetScript, null);
            var frontObject = CreateTransform(_frontPosition, frontCenterLocal, targetScript, targetScript.carHolder);

            targetScript._backPosition = backObject;
            targetScript._frontPosition = frontObject;
        }

        private static Transform CreateTransform(string name, Vector3 localPosition, VehicleComponent targetScript, Transform parent)
        {
            var newObject = new GameObject(name).transform;
            if (parent == null)
            {
                parent = targetScript.HasTrailer
                ? targetScript.trailer?.trailerHolder
                : targetScript.carHolder?.transform;
            }

            newObject.SetParent(parent, false);
            newObject.localPosition = localPosition;
            return newObject;
        }


        private static void CheckLayer(VehicleComponent targetScript, ref bool correct)
        {
            var layerSetup = Resources.Load<LayerSetup>(TrafficSystemConstants.layerSetupData);
            if (layerSetup == null)
            {
                LogError(ref correct, TrafficSystemErrors.LayersNotConfigured);
                return;
            }
            Collider[] allColliders = targetScript.carHolder.GetComponentsInChildren<Collider>();
            foreach (Collider collider in allColliders)
            {
                if ((layerSetup.trafficLayers.value & (1 << collider.gameObject.layer)) == 0)
                {
                    Debug.LogWarning($"Layer {LayerMask.LayerToName(collider.gameObject.layer)} from collider {collider.name} is not marked as a traffic layer. If this is intended, ignore this warning.", collider);
                }
            }
        }

        private static void ConnectTrailer(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct)
                return;

            if (targetScript.trailer == null)
                return;

            if (targetScript.trailerConnectionPoint == null)
            {
                LogError(ref correct, "Trailer Connection Point is not assigned. " +
                    "Please assign it on Vehicle Component.");
                return;
            }

            targetScript.trailer.transform.position = new Vector3(targetScript.trailerConnectionPoint.position.x, targetScript.carHolder.position.y, targetScript.trailerConnectionPoint.position.z);

            if (targetScript.trailer.joint == null)
            {
                LogError(ref correct, "No joint assigned. Please Configure Trailer first.");
                return;
            }

            if (targetScript.trailer.truckConnectionPoint == null)
            {
                LogError(ref correct, "No truckConnectionPoint assigned. Please Configure Trailer first.");
                return;
            }

            targetScript.trailer.joint.connectedBody = targetScript.rb;
            targetScript.trailer.joint.autoConfigureConnectedAnchor = false;
            targetScript.trailer.joint.anchor = targetScript.trailer.truckConnectionPoint.GetLocalPositionRelativeToTopParent(targetScript.trailer.transform);
            targetScript.trailer.joint.connectedAnchor = targetScript.trailerConnectionPoint.GetLocalPositionRelativeToTopParent(targetScript.transform);
            targetScript.trailer.rb.mass = targetScript.rb.mass;
#if UNITY_6000_0_OR_NEWER
            targetScript.trailer.rb.linearDamping = targetScript.rb.linearDamping;
            targetScript.trailer.rb.angularDamping = targetScript.rb.angularDamping;
#else
            targetScript.trailer.rb.drag = targetScript.rb.drag;
            targetScript.trailer.rb.angularDrag = targetScript.rb.angularDrag;
#endif
        }


        static void Save(VehicleComponent targetScript)
        {
            EditorUtility.SetDirty(targetScript);
            AssetDatabase.SaveAssets();
        }


        private static void CheckColliers(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct)
                return;

            Collider[] allColliders = targetScript.carHolder.GetComponentsInChildren<Collider>();
            targetScript.coliderHeight = 0;
            bool hasColliders = false;
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (!allColliders[i].isTrigger)
                {
                    float colliderDimension = allColliders[i].bounds.size.y * allColliders[i].transform.lossyScale.y;
                    if (targetScript.coliderHeight < colliderDimension)
                    {
                        targetScript.coliderHeight = colliderDimension;
                    }
                    hasColliders = true;
                }
            }

            if (!hasColliders)
            {
                LogError(ref correct, "No collider found -> Please assign a collider on the car.");
            }

            if (targetScript.coliderHeight == 0)
            {
                LogError(ref correct, "Collider height is 0 -> Verify your colliders or press configure car in prefab mode.");
            }
        }


        private static void AssignVisibilityScript(VehicleComponent targetScript)
        {
            VisibilityScript[] allComponents = targetScript.gameObject.GetComponentsInChildren<VisibilityScript>();
            if (allComponents.Length > 1)
            {
                for (int i = 1; i < allComponents.Length; i++)
                {
                    DestroyImmediate(allComponents[i]);
                }
            }
            if (allComponents.Length == 1)
            {
                if (allComponents[0].GetComponent<Renderer>() != null)
                {
                    targetScript.visibilityScript = allComponents[0];
                    return;
                }
                else
                {
                    DestroyImmediate(allComponents[0]);
                }
            }

            var renderers = targetScript.GetComponentsInChildren<Renderer>();
            var objectToAdd = renderers[0];
            float maxSize = 0;
            foreach (Renderer renderer in renderers)
            {
                bool isWheel = false;
                for (int i = 0; i < targetScript.allWheels.Length; i++)
                {
                    if (targetScript.allWheels[i].wheelGraphics == renderer.transform)
                    {
                        isWheel = true;
                        objectToAdd = renderer;
                        break;
                    }
                }

                if (!isWheel)
                {
                    float meshSIze = renderer.GetComponent<MeshFilter>().sharedMesh.bounds.size.sqrMagnitude;
                    if (meshSIze > maxSize)
                    {
                        maxSize = meshSIze;
                        objectToAdd = renderer;
                    }
                }
            }

            targetScript.visibilityScript = objectToAdd.gameObject.AddComponent<VisibilityScript>();
        }


        private static void AssignLights(VehicleComponent targetScript)
        {
            VehicleLightsComponent vehicleLights = targetScript.GetComponent<VehicleLightsComponent>();
            if (vehicleLights)
            {
                AssignGameobject(targetScript, ref vehicleLights.frontLights, "FrontLights");
                AssignGameobject(targetScript, ref vehicleLights.reverseLights, "ReverseLights");
                AssignGameobject(targetScript, ref vehicleLights.rearLights, "RearLights");
                AssignGameobject(targetScript, ref vehicleLights.stopLights, "StopLights");
                AssignGameobject(targetScript, ref vehicleLights.blinkerLeft, "BlinkersLeft");
                AssignGameobject(targetScript, ref vehicleLights.blinkerRight, "BlinkersRight");
            }
        }


        private static void AssignGameobject(VehicleComponent targetScript, ref GameObject objToAssign, string objectName)
        {
            Transform go = targetScript.transform.FindDeepChild(objectName);
            if (go == null)
            {
                Debug.Log(objectName + " not found");
                return;
            }
            objToAssign = go.gameObject;
        }


        private static void SetWheelDimensions(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct)
                return;
            for (int i = 0; i < targetScript.allWheels.Length; i++)
            {
                MeshFilter[] allMeshes = targetScript.allWheels[i].wheelTransform.GetComponentsInChildren<MeshFilter>();
                if (allMeshes.Length == 0)
                {
                    Debug.LogWarning("No meshes found inside " + targetScript.allWheels[0].wheelTransform);
                }

                float maxSize = 0;
                float wheelRadius = 0;
                //create mesh
                for (int j = 0; j < allMeshes.Length; j++)
                {
                    float xSize = allMeshes[j].sharedMesh.bounds.size.x * allMeshes[j].transform.lossyScale.x;
                    float ySize = allMeshes[j].sharedMesh.bounds.size.y * allMeshes[j].transform.lossyScale.y;
                    float zSize = allMeshes[j].sharedMesh.bounds.size.z * allMeshes[j].transform.lossyScale.z;
                    bool changed = false;
                    if (xSize > maxSize)
                    {
                        maxSize = xSize;
                        changed = true;
                    }
                    if (ySize > maxSize)
                    {
                        maxSize = ySize;
                        changed = true;
                    }
                    if (zSize > maxSize)
                    {
                        maxSize = zSize;
                        changed = true;
                    }
                    if (changed)
                    {
                        wheelRadius = Mathf.Max(xSize, ySize, zSize);
                        wheelRadius /= 2;
                    }
                }
                if (targetScript.allWheels[i].wheelRadius == 0)
                {
                    targetScript.allWheels[i].wheelRadius = wheelRadius;
                }
                if (targetScript.maxSuspension == 0)
                {
                    targetScript.allWheels[i].maxSuspension = wheelRadius / 2;
                }
                else
                {
                    targetScript.allWheels[i].maxSuspension = targetScript.maxSuspension;
                }

                targetScript.allWheels[i].raycastLength = targetScript.allWheels[i].wheelRadius + targetScript.allWheels[i].maxSuspension;
                targetScript.allWheels[i].wheelCircumference = targetScript.allWheels[i].wheelRadius * Mathf.PI * 2;
            }
        }


        private static void CheckForZero(float value, string propertyName, ref bool correct)
        {
            if (value <= 0)
            {
                LogError(ref correct, propertyName + " needs to be > 0");
            }
        }


        private static void SetPivotOnBackWheels(VehicleComponent targetScript, ref bool correct)
        {
            Vector3 poz = new Vector3();
            int nr = 0;
            for (int i = 0; i < targetScript.allWheels.Length; i++)
            {
                if (targetScript.allWheels[i].wheelPosition == Wheel.WheelPosition.Back)
                {
                    nr++;
                    poz += targetScript.allWheels[i].wheelTransform.localPosition;
                }
            }
            if (nr == 0)
            {
                LogError(ref correct, "No back wheels set. Mark Wheel Position as Back for the back car wheels");
                return;
            }

            targetScript.carHolder.localPosition = new Vector3(0, 0, -(poz / nr).z);
        }


        private static void AssignWheels(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct)
                return;
            Transform wheelsHolder = targetScript.carHolder.Find(_wheelsHolderName);
            if (wheelsHolder == null)
            {
                if(targetScript.allWheels == null || targetScript.allWheels.Length==0)
                {
                    LogError(ref correct, "A GameObject named " + _wheelsHolderName + " was not found under " + targetScript.name+". Manually assign the wheels.");
                    return;
                }
                for (int i = 0; i < targetScript.allWheels.Length; i++)
                {
                    if (targetScript.allWheels[i].wheelTransform == null)
                    {
                        LogError(ref correct, "A GameObject named " + _wheelsHolderName + " was not found under " + targetScript.name);
                        return;
                    }
                }
                Debug.Log("All wheels ware manually assigned, make sure they are correct");
                return;
            }

            //verify if wheels ware already assigned
            bool allAssigned = true;
            if (targetScript.allWheels != null)
            {
                if (targetScript.allWheels.Length < 2)
                {
                    allAssigned = false;
                }
                for (int i = 0; i < targetScript.allWheels.Length; i++)
                {
                    if (targetScript.allWheels[i].wheelTransform == null)
                    {
                        allAssigned = false;
                    }
                }
            }
            else
            {
                allAssigned = false;
            }

            if (allAssigned == true)
            {
                Debug.Log("All wheels ware manually assigned, make sure they are correct");
                return;
            }

            if (wheelsHolder.childCount == 0)
            {
                LogError(ref correct, "No GameObject was not found under " + _wheelsHolderName);
            }

            targetScript.allWheels = new Wheel[wheelsHolder.childCount];
            for (int i = 0; i < wheelsHolder.childCount; i++)
            {
                targetScript.allWheels[i] = new Wheel();
                targetScript.allWheels[i].wheelTransform = wheelsHolder.GetChild(i);
                if (targetScript.allWheels[i].wheelTransform.name.Contains("Front"))
                {
                    targetScript.allWheels[i].wheelPosition = Wheel.WheelPosition.Front;
                }
                if (targetScript.allWheels[i].wheelTransform.name.Contains("Back") || targetScript.allWheels[i].wheelTransform.name.Contains("Rear"))
                {
                    targetScript.allWheels[i].wheelPosition = Wheel.WheelPosition.Back;
                }
                try
                {
                    targetScript.allWheels[i].wheelGraphics = wheelsHolder.GetChild(i).GetChild(0);
                }
                catch
                {
                    LogError(ref correct, "No GameObject was not found under " + wheelsHolder.GetChild(i).name);
                }
            }
        }


        private static void AddShadow(VehicleComponent targetScript)
        {
            Transform shadowHolder = targetScript.carHolder.transform.Find(_shadowHolderName);
            if (shadowHolder)
            {
                targetScript.shadowHolder = shadowHolder;
            }
            else
            {
                Debug.Log("Shadow not found. Please assign the shadow manually if you need it");
            }
        }


        private static void CreateFrontTrigger(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct)
                return;
            float triggerLength = targetScript.distanceToStop + targetScript.triggerLength;
            float triggerHeight = 1;

            Transform frontTriggerHolder = targetScript.carHolder.transform.Find(_frontTriggerHolderName);
            GleyPrefabUtilities.DestroyTransform(frontTriggerHolder);
            if (frontTriggerHolder == null)
            {
                if (GleyPrefabUtilities.EditingInsidePrefab())
                {
                    GameObject prefabRoot = GleyPrefabUtilities.GetScenePrefabRoot();
                    frontTriggerHolder = new GameObject(_frontTriggerHolderName).transform;
                    frontTriggerHolder.SetParent(prefabRoot.transform.Find(targetScript.carHolder.name).transform, false);

                }
                else
                {
                    frontTriggerHolder = new GameObject(_frontTriggerHolderName).transform;
                    frontTriggerHolder.SetParent(targetScript.carHolder.transform, false);
                }
            }

            Vector3 triggerPoz = new Vector3();
            int nr = 0;
            for (int i = 0; i < targetScript.allWheels.Length; i++)
            {
                if (targetScript.allWheels[i].wheelPosition == Wheel.WheelPosition.Front)
                {
                    nr++;
                    triggerPoz += targetScript.allWheels[i].wheelTransform.position;
                }
            }

            if (nr == 0)
            {
                LogError(ref correct, "No front wheels set. Mark Wheel Position as Front for the front car wheels");
                return;
            }

            triggerPoz = triggerPoz / nr;
            frontTriggerHolder.position = triggerPoz;
            targetScript.frontTrigger = frontTriggerHolder;

            targetScript.wheelDistance = targetScript.transform.InverseTransformPoint(triggerPoz).z;
            if (targetScript.wheelDistance == 0)
            {
                LogError(ref correct, " Distance between wheels is 0. Make sure your wheel assignments are correct");
                return;
            }

            Transform frontTrigger = frontTriggerHolder.Find(_frontTriggerName);
            if (frontTrigger == null)
            {
                MeshFilter[] allMeshes = targetScript.carHolder.transform.GetComponentsInChildren<MeshFilter>();
                if (allMeshes.Length == 0)
                {
                    LogError(ref correct, "No meshes found inside " + targetScript.name);
                    return;
                }

                float maxSize = 0;
                float triggerSize = 0;
                //create mesh
                for (int i = 0; i < allMeshes.Length; i++)
                {
                    float xSize = allMeshes[i].sharedMesh.bounds.size.x * allMeshes[i].transform.localScale.x;
                    float ySize = allMeshes[i].sharedMesh.bounds.size.y * allMeshes[i].transform.localScale.y;
                    float zSize = allMeshes[i].sharedMesh.bounds.size.z * allMeshes[i].transform.localScale.z;
                    bool changed = false;
                    if (xSize > maxSize)
                    {
                        maxSize = xSize;
                        changed = true;
                    }
                    if (zSize > maxSize)
                    {
                        maxSize = zSize;
                        changed = true;
                    }
                    if (changed)
                    {
                        triggerSize = Mathf.Min(xSize, zSize);
                        targetScript.length = Mathf.Max(xSize, zSize);
                    }

                    if (ySize > triggerHeight)
                    {
                        triggerHeight = ySize;
                    }

                }
                frontTrigger = new GameObject(_frontTriggerName).transform;
                frontTrigger.SetParent(frontTriggerHolder);
                BoxCollider boxCollider = frontTrigger.gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(triggerSize, triggerHeight, triggerLength);
                boxCollider.center = new Vector3(0, 0, triggerLength / 2);
                boxCollider.isTrigger = true;
                frontTriggerHolder.gameObject.SetLayer(targetScript.gameObject.layer);
                frontTrigger.transform.localPosition = new Vector3(0, triggerHeight / 2, 0);
            }
        }


        private static void SetupRigidbody(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct)
                return;

            Rigidbody rb = targetScript.GetComponent<Rigidbody>();
            if (rb == null)
            {
                LogError(ref correct, "RigidBody not found on " + targetScript.name);
            }
            else
            {
                targetScript.rb = rb;
#if UNITY_6000_0_OR_NEWER
                if (rb.linearDamping == 0)
                {
                    targetScript.rb.linearDamping = 0.1f;
                    targetScript.rb.angularDamping = 3;
                }
#else
                if (rb.drag == 0)
                {
                    targetScript.rb.drag = 0.1f;
                    targetScript.rb.angularDrag = 3;
                }
#endif
            }
        }


        private static void SetCarHolder(VehicleComponent targetScript, ref bool correct)
        {
            if (!correct)
                return;
            Transform carHolder = targetScript.transform.Find(_carHolderName);
            if (carHolder == null)
            {
                if (targetScript.carHolder != null)
                {
                    Debug.Log("A GameObject was manually assigned inside " + nameof(targetScript.carHolder) + ". Make sure is the root GameObject");
                }
                else
                {
                    LogError(ref correct, "A GameObject named " + _carHolderName + " was not found under " + targetScript.name);
                }
            }
            else
            {
                targetScript.carHolder = carHolder;
            }
        }


        private static void LogError(ref bool correct, string message)
        {
            correct = false;
            Debug.LogError(message + " Auto assign will stop now.");
            Debug.Log("Please fix the above errors and press Configure Car again or assign missing references and values manually.");
        }
    }
}