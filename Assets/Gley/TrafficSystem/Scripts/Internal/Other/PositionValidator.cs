using System.Collections.Generic;
using UnityEngine;

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Used to check if a vehicle can be instantiated in a given position
    /// </summary>
    public class PositionValidator
    {
        private readonly float _minDistanceToAdd;
        private readonly bool _debugDensity;

        private Collider[] _results;
        private Transform[] _activeCameras;
        private LayerMask _trafficLayer;
        private LayerMask _playerLayer;
        private LayerMask _buildingsLayers;

        private CustomPositionValidation _customValidation;

        public List<Matrix4x4> MatrixToCheck { get; private set; }
        public List<Vector3> LastSize { get; private set; }

        /// <summary>
        /// Setup dependencies
        /// </summary>
        /// <param name="activeCameras"></param>
        /// <param name="trafficLayer"></param>
        /// <param name="buildingsLayers"></param>
        /// <param name="minDistanceToAdd"></param>
        /// <param name="debugDensity"></param>
        /// <returns></returns>
        public PositionValidator(Transform[] activeCameras, LayerMask trafficLayer, LayerMask playerLayer, LayerMask buildingsLayers, float minDistanceToAdd, bool debugDensity)
        {
            UpdateCamera(activeCameras);
            _trafficLayer = trafficLayer;
            _playerLayer = playerLayer;
            _minDistanceToAdd = minDistanceToAdd * minDistanceToAdd;
            _buildingsLayers = buildingsLayers;
            _debugDensity = debugDensity;
            _results = new Collider[1];
            if (debugDensity)
            {
                MatrixToCheck = new List<Matrix4x4>();
                LastSize = new List<Vector3>();
            }
        }

        public void SetCustomPositionValidation(CustomPositionValidation customPositionValidation)
        {
            _customValidation = customPositionValidation;
        }


        /// <summary>
        /// Checks if a vehicle can be instantiated in a given position
        /// </summary>
        /// <param name="position">position to check</param>
        /// <param name="vehicleLength"></param>
        /// <param name="vehicleHeight"></param>
        /// <param name="ignoreLineOfSight">validate position eve if it is in view</param>
        /// <returns></returns>
        public bool IsValid(Vector3 position, float vehicleLength, float vehicleHeight, float vehicleWidth, bool ignoreLineOfSight, float frontWheelOffset, Quaternion rotation)
        {

            if (_customValidation != null)
            {
                return _customValidation.Invoke(position, vehicleLength, vehicleHeight, vehicleWidth, rotation);
            }

            position -= rotation * new Vector3(0, 0, frontWheelOffset);
            for (int i = 0; i < _activeCameras.Length; i++)
            {
                if (!ignoreLineOfSight)
                {
                    //if position if far enough from the player
                    if (Vector3.SqrMagnitude(_activeCameras[i].position - position) < _minDistanceToAdd)
                    {
                        if (!Physics.Linecast(position, _activeCameras[i].position, _buildingsLayers))
                        {
#if UNITY_EDITOR
                            if (_debugDensity)
                            {
                                Debug.Log("Density: Direct view of the camera");
                                Debug.DrawLine(_activeCameras[i].position, position, Color.red, 0.1f);
                            }
#endif
                            return false;
                        }
                        else
                        {
#if UNITY_EDITOR
                            if (_debugDensity)
                            {
                                Debug.DrawLine(_activeCameras[i].position, position, Color.green, 0.1f);
                            }
#endif
                        }
                    }
                }
            }

            //check if the final position is free 
            return IsPositionFree(position, vehicleLength, vehicleHeight, vehicleWidth, rotation);
        }


        /// <summary>
        /// Check if a given position if free
        /// </summary>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public bool IsPositionFree(Vector3 position, float length, float height, float width, Quaternion rotation)
        {
            if (_debugDensity)
            {
                MatrixToCheck.Add(Matrix4x4.TRS(position, rotation, Vector3.one));
                LastSize.Add(new Vector3(width, height * 2, length));

                if (MatrixToCheck.Count > 10)
                {
                    MatrixToCheck.RemoveAt(0);
                    LastSize.RemoveAt(0);
                }
            }


            if (Physics.OverlapBoxNonAlloc(position, new Vector3(width / 2, height, length / 2), _results, rotation, _trafficLayer | _playerLayer) > 0)
            {
#if UNITY_EDITOR
                if (_debugDensity)
                {
                    Debug.Log("Density: Other obstacle is blocking the waypoint");
                }
#endif
                return false;
            }
            return true;
        }


        public bool CheckTrailerPosition(Vector3 position, Quaternion vehicleRotation, Quaternion trailerRotation, VehicleComponent vehicle)
        {
            Vector3 translatedPosition = position - vehicleRotation * Vector3.forward * (vehicle.frontTrigger.transform.localPosition.z + vehicle.carHolder.transform.localPosition.z);
            translatedPosition = translatedPosition - trailerRotation * Vector3.forward * vehicle.trailer.length / 2;
            return IsPositionFree(translatedPosition, vehicle.trailer.length, vehicle.trailer.height, vehicle.trailer.width, trailerRotation);
        }


        /// <summary>
        /// Update player camera transform
        /// </summary>
        /// <param name="activeCameras"></param>
        public void UpdateCamera(Transform[] activeCameras)
        {
            _activeCameras = activeCameras;
        }
    }
}