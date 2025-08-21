#if GLEY_TRAFFIC_SYSTEM
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Jobs;
using Debug = UnityEngine.Debug;



#if GLEY_PEDESTRIAN_SYSTEM
using Gley.PedestrianSystem.Internal;
#endif

namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// This is the core class of the system, it controls everything else
    /// </summary>
    internal class TrafficManager : MonoBehaviour
    {
        #region Variables
        //transforms to update
        private TransformAccessArray _vehicleTrigger;
        private TransformAccessArray _suspensionConnectPoints;
        private TransformAccessArray _wheelsGraphics;

        private NativeArray<float3> _activeCameraPositions;

        //properties for each vehicle
        private NativeArray<VehicleTypes> _vehicleType;
        private Rigidbody[] _vehicleRigidbody;
        private Dictionary<int, Rigidbody> _trailerRigidbody;
        private NativeArray<float3> _vehicleDownDirection;
        private NativeArray<float3> _vehicleForwardDirection;
        private NativeArray<float3> _vehicleRightDirection;
        private NativeArray<float3> _trailerRightDirection;
        private NativeArray<float3> _vehicleTargetWaypointPosition;
        private NativeArray<float3> _vehicleNextWaypointPosition;
        private NativeArray<float3> _vehicleStopPosition;
        private NativeArray<float3> _vehiclePosition;
        private NativeArray<float3> _vehicleGroundDirection;
        private NativeArray<float3> _vehicleForwardForce;
        private NativeArray<float3> _vehicleLaterlForce;
        private NativeArray<float3> _trailerForwardForce;
        private NativeArray<float3> _vehicleVelocity;
        private NativeArray<float3> _trailerVelocity;
        private NativeArray<float> _wheelSpringForce;
        private NativeArray<float> _vehicleMaxSteer;
        private NativeArray<float> _vehicleRotationAngle;
        private NativeArray<float> _vehiclePowerStep;
        private NativeArray<float> _vehicleBrakeStep;
        private NativeArray<float> _vehicleDrag;
        private NativeArray<float> _massDifference;
        private NativeArray<float> _trailerDrag;
        private NativeArray<float> _vehicleMaxAllowedSpeed;
        private NativeArray<float> _vehicleTargetSpeed;
        private NativeArray<float> _vehicleWheelDistance;
        private NativeArray<float> _vehicleSteeringStep;
        private NativeArray<float> _vehicleDistanceToStop;
        private NativeArray<float> _accelerationPercent;
        private NativeArray<float> _steerPercent;
        private NativeArray<float> _brakePercent;
        private NativeArray<float> _requiredBrakePower;
        private NativeArray<bool> _stopTargetReached;
        private NativeArray<int> _vehicleStartWheelIndex;//start index for the wheels of car i (dim nrOfCars)
        private NativeArray<int> _vehicleEndWheelIndex; //number of wheels that car with index i has (nrOfCars)
        private NativeArray<int> _vehicleNrOfWheels;
        private NativeArray<int> _vehicleListIndex;
        private NativeArray<int> _vehicleCurrentGear;
        private NativeArray<int> _vehicleTargetGear;
        private NativeArray<int> _trailerNrWheels;
        private NativeArray<bool> _vehicleReadyToRemove;
        private NativeArray<bool> _vehicleIsBraking;
        private NativeArray<bool> _vehicleNeedWaypoint;
        private NativeArray<bool> _vehicleNotDrivable;
        private NativeArray<bool> _excludedVehicles;
        private NativeArray<bool> _addExcludedVehicle;

        //properties for each wheel
        private NativeArray<RaycastHit> _wheelRaycatsResult;
        private NativeArray<RaycastCommand> _wheelRaycastCommand;
        private NativeArray<float3> _wheelSuspensionPosition;
        private NativeArray<float3> _wheelGroundPosition;
        private NativeArray<float3> _wheelVelocity;
        private NativeArray<float3> _wheelRightDirection;
        private NativeArray<float3> _wheelNormalDirection;
        private NativeArray<float3> _wheelSuspensionForce;
        private NativeArray<float> _wheelRotation;
        private NativeArray<float> _wheelRadius;
        private NativeArray<float> _wheelRaycatsDistance;
        private NativeArray<float> _wheelMaxSuspension;
        private NativeArray<float> _wheelSpringStiffness;
        private NativeArray<int> _groundedWheels;
        private NativeArray<int> _wheelAssociatedCar; //index of the car that contains the wheel
        private NativeArray<bool> _wheelCanSteer;
        private NativeArray<bool> _excludedWheels;

        //properties that should be on each wheel
        private NativeArray<float> _turnAngle;
        private NativeArray<float> _raycastLengths;
        private NativeArray<float> _wCircumferences;

        //jobs
        private UpdateWheelJob _updateWheelJob;
        private UpdateTriggerJob _updateTriggerJob;
        private DriveJob _driveJob;
        private WheelJob _wheelJob;
        private JobHandle _raycastJobHandle;
        private JobHandle _updateWheelJobHandle;
        private JobHandle _updateTriggerJobHandle;
        private JobHandle _driveJobHandle;
        private JobHandle _wheelJobHandle;

        //additional properties
        private Transform[] _activeCameras;
        private LayerMask _roadLayers;
        private Vector3 _forward;
        private Vector3 _up;
        private float _distanceToRemove;
        private float _minDistanceToAdd;
        private int _nrOfVehicles;
        private int _nrOfJobs;
        private int _indexToRemove;
        private int _totalWheels;
        private int _activeSquaresLevel;
        private int _activeCameraIndex;
        private bool _initialized;
        #endregion



        // —— SpeedingVehicle 定时刷车 —— 
        private float _speedingSpawnTimer = 0f;
        private const float SPEEDING_SPAWN_INTERVAL = 10f;   // 每 10s 刷一次
        private const float SPEEDING_BEHIND_EXTRA = 5f;




        public static bool Exists
        {
            get
            {
                return _instance != null;
            }
        }

        public bool Initialized => _initialized;


        private static TrafficManager _instance;
        public static TrafficManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (MonoBehaviourUtilities.TryGetSceneScript<TrafficWaypointsData>(out var result))
                    {
                        _instance = result.Value.gameObject.AddComponent<TrafficManager>();
                    }
                    else
                    {
                        Debug.LogError(result.Error);
                        Debug.LogError(TrafficSystemErrors.FatalError);
                    }

                }
                return _instance;
            }
        }


        private AllVehiclesData _allVehiclesData;
        public AllVehiclesData AllVehiclesData
        {
            get
            {
                if (_allVehiclesData != null)
                {
                    return _allVehiclesData;
                }

                return ReturnError<AllVehiclesData>();
            }
        }

        private DensityManager _densityManager;
        public DensityManager DensityManager
        {
            get
            {
                if (_densityManager != null)
                {
                    return _densityManager;
                }
                return ReturnError<DensityManager>();
            }
        }

        private SoundManager _soundManager;
        public SoundManager SoundManager
        {
            get
            {
                if (_soundManager != null)
                {
                    return _soundManager;
                }
                return ReturnError<SoundManager>();
            }
        }

        private VehicleAI _vehicleAI;
        public VehicleAI VehicleAI
        {
            get
            {
                if (_vehicleAI != null)
                {
                    return _vehicleAI;
                }
                return ReturnError<VehicleAI>();
            }
        }

        private BehaviourManager _behaviourManager;
        public BehaviourManager BehaviourManager
        {
            get
            {
                if (_behaviourManager != null)
                {
                    return _behaviourManager;
                }
                return ReturnError<BehaviourManager>();
            }
        }

        private PositionValidator _positionValidator;
        public PositionValidator PositionValidator
        {
            get
            {
                if (_positionValidator != null)
                {
                    return _positionValidator;
                }
                return ReturnError<PositionValidator>();
            }
        }


        private WaypointSelector _waypointSelector;
        public WaypointSelector WaypointSelector
        {
            get
            {
                if (_waypointSelector != null)
                {
                    return _waypointSelector;
                }
                return ReturnError<WaypointSelector>();
            }
        }

        private TrafficWaypointsData _trafficWaypointsData;
        public TrafficWaypointsData TrafficWaypointsData
        {
            get
            {
                if (_trafficWaypointsData != null)
                {
                    return _trafficWaypointsData;
                }
                return ReturnError<TrafficWaypointsData>();
            }
        }

        private TrafficModules _trafficModules;
        public TrafficModules TrafficModules
        {
            get
            {
                if (_trafficModules != null)
                {
                    return _trafficModules;
                }
                return ReturnError<TrafficModules>();
            }
        }



        private IntersectionManager _intersectionManager;
        public IntersectionManager IntersectionManager
        {
            get
            {
                if (_intersectionManager != null)
                {
                    return _intersectionManager;
                }
                return ReturnError<IntersectionManager>();
            }
        }

        private PathFindingManager _pathFindingManager;
        public PathFindingManager PathFindingManager
        {
            get
            {
                if (TrafficModules.PathFinding)
                {
                    if (_pathFindingManager != null)
                    {
                        return _pathFindingManager;
                    }
                    else
                    {
                        Debug.LogError(TrafficSystemErrors.NullPathFindingData);
                    }
                }
                else
                {
                    Debug.LogError(TrafficSystemErrors.NoPathFindingWaypoints);
                }
                return null;
            }
        }

        private IntersectionsData _intersectionsData;
        public IntersectionsData IntersectionsData
        {
            get
            {
                if (_intersectionsData != null)
                {
                    return _intersectionsData;
                }
                return ReturnError<IntersectionsData>();
            }
        }

        private ActiveCellsManager _activeCellsManager;
        public ActiveCellsManager ActiveCellsManager
        {
            get
            {
                if (_activeCellsManager != null)
                {
                    return _activeCellsManager;
                }
                return ReturnError<ActiveCellsManager>();
            }
        }

        private GridData _gridData;
        public GridData GridData
        {
            get
            {
                if (_gridData != null)
                {
                    return _gridData;
                }
                return ReturnError<GridData>();
            }
        }

        private DisabledWaypointsManager _disabledWaypointsManager;
        public DisabledWaypointsManager DisabledWaypointsManager
        {
            get
            {
                if (_disabledWaypointsManager != null)
                {
                    return _disabledWaypointsManager;
                }
                return ReturnError<DisabledWaypointsManager>();
            }
        }

        private PathFindingData _pathFindingData;
        public PathFindingData PathFindingData
        {
            get
            {
                if (_pathFindingData != null)
                {
                    return _pathFindingData;
                }
                return ReturnError<PathFindingData>();
            }
        }

        private TimeManager _timeManager;
        public TimeManager TimeManager
        {
            get
            {
                if (_timeManager != null)
                {
                    return _timeManager;
                }
                return ReturnError<TimeManager>();
            }
        }

        private PlayerWaypointsManager _playerWaypointsManager;
        public PlayerWaypointsManager PlayerWaypointsManager
        {
            get
            {
                if (_playerWaypointsManager != null)
                {
                    return _playerWaypointsManager;
                }
                return ReturnError<PlayerWaypointsManager>();
            }
        }

        private DebugManager _debugManager;
        public DebugManager DebugManager
        {
            get
            {
                if (_debugManager != null)
                {
                    return _debugManager;
                }
                return ReturnError<DebugManager>();
            }
        }

        private IBehaviourImplementation _behaviourImplementation;
        public IBehaviourImplementation BehaviourImplementation
        {
            get
            {
                if (_behaviourImplementation != null)
                {
                    return _behaviourImplementation;
                }
                return ReturnError<IBehaviourImplementation>();
            }
        }

        BehaviourResult _behaviourResult;
        T ReturnError<T>()
        {
            StackTrace stackTrace = new StackTrace();
            string callingMethodName = string.Empty;
            if (stackTrace.FrameCount >= 3)
            {
                StackFrame callingFrame = stackTrace.GetFrame(1);
                callingMethodName = callingFrame.GetMethod().Name;
            }
            Debug.LogError(TrafficSystemErrors.PropertyError(callingMethodName));
            return default;
        }


        #region TrafficInitialization
        /// <summary>
        /// Initialize the traffic 
        /// </summary>
        public void Initialize(Transform[] activeCameras, int nrOfVehicles, VehiclePool vehiclePool, TrafficOptions trafficOptions)
        {
            //safety checks
            var layerSetup = Resources.Load<LayerSetup>(TrafficSystemConstants.layerSetupData);
            if (layerSetup == null)
            {
                Debug.LogError(TrafficSystemErrors.LayersNotConfigured);
                return;
            }

            // Load grid data.
            if (MonoBehaviourUtilities.TryGetSceneScript<GridData>(out var resultGridData))
            {
                if (resultGridData.Value.IsValid(out var error))
                {
                    _gridData = resultGridData.Value;
                }
                else
                {
                    Debug.LogError(error);
                    return;
                }
            }
            else
            {
                Debug.LogError(resultGridData.Error);
                return;
            }

            // Load waypoints data.
            if (MonoBehaviourUtilities.TryGetSceneScript<TrafficWaypointsData>(out var resultWaypointsData))
            {
                if (resultWaypointsData.Value.IsValid(out var error))
                {
                    _trafficWaypointsData = resultWaypointsData.Value.Initialize();
                }
                else
                {
                    Debug.LogError(error);
                    return;
                }
            }
            else
            {
                Debug.LogError(resultWaypointsData.Error);
                return;
            }

            // Load intersection data.
            if (MonoBehaviourUtilities.TryGetSceneScript<IntersectionsData>(out var resultIntersectionsData))
            {
                if (resultIntersectionsData.Value.IsValid(out var error))
                {
                    _intersectionsData = resultIntersectionsData.Value;
                }
                else
                {
                    Debug.LogError(error);
                    return;
                }
            }
            else
            {
                Debug.LogError(resultIntersectionsData.Error);
                return;
            }

            gameObject.AddComponent<CoroutineManager>();

            // Load pedestrian data if available
            IPedestrianWaypointsDataHandler pedestrianWaypointsDataHandler = new DummyPedestrianWaypointsDataHandler();
#if GLEY_PEDESTRIAN_SYSTEM
            if (MonoBehaviourUtilities.TryGetSceneScript<PedestrianWaypointsData>(out var resultPedestrianWaypoints))
            {
                if (resultPedestrianWaypoints.Value.IsValid(out var error))
                {
                    pedestrianWaypointsDataHandler = new PedestrianWaypointsDataHandler(resultPedestrianWaypoints.Value);
                }
                else
                {
                    Debug.LogWarning($"{TrafficSystemErrors.NoPedestrianWaypoints}\n{error}");
                }
            }
            else
            {
                Debug.LogWarning($"{TrafficSystemErrors.NoPedestrianWaypoints}\n{resultPedestrianWaypoints.Error}");
            }
#endif

            if (MonoBehaviourUtilities.TryGetObjectScript<TrafficModules>(TrafficSystemConstants.PlayHolder, out var trafficModules))
            {
                _trafficModules = trafficModules.Value;
            }
            else
            {
                Debug.LogError(trafficModules.Error);
                return;
            }


            if (TrafficModules.PathFinding)
            {
                // Load path finding data.
                if (MonoBehaviourUtilities.TryGetObjectScript<PathFindingData>(TrafficSystemConstants.PlayHolder, out var resultPathFindingData))
                {
                    if (resultPathFindingData.Value.IsValid(out var error))
                    {
                        _pathFindingData = resultPathFindingData.Value;
                    }
                    else
                    {
                        Debug.LogError(error);
                        return;
                    }
                }
                else
                {
                    Debug.LogError(resultPathFindingData.Error);
                    return;
                }
            }


            if (vehiclePool.trafficCars.Length <= 0)
            {
                Debug.LogError(TrafficSystemErrors.NoVehiclesAvailable);
                return;
            }

            if (nrOfVehicles <= 0)
            {
                Debug.LogError(TrafficSystemErrors.InvalidNrOfVehicles);
                return;
            }

            _nrOfVehicles = nrOfVehicles;
            _activeCameras = activeCameras;
            _activeSquaresLevel = trafficOptions.ActiveSquaresLevel;
            _roadLayers = layerSetup.roadLayers;
            _up = Vector3.up;

            _soundManager = new SoundManager(trafficOptions.MasterVolume);

            // Compute total wheels
            _allVehiclesData = new AllVehiclesData(transform, vehiclePool, nrOfVehicles, layerSetup.buildingsLayers, layerSetup.obstaclesLayers, layerSetup.playerLayers, layerSetup.roadLayers, trafficOptions.LightsOn, trafficOptions.ModifyTriggerSize, TrafficWaypointsData, trafficOptions.MinOffset, trafficOptions.MaxOffset);
            _totalWheels = AllVehiclesData.GetTotalWheels();

            //initialize arrays
            _wheelSuspensionPosition = new NativeArray<float3>(_totalWheels, Allocator.Persistent);
            _wheelVelocity = new NativeArray<float3>(_totalWheels, Allocator.Persistent);
            _wheelGroundPosition = new NativeArray<float3>(_totalWheels, Allocator.Persistent);
            _wheelNormalDirection = new NativeArray<float3>(_totalWheels, Allocator.Persistent);
            _wheelRightDirection = new NativeArray<float3>(_totalWheels, Allocator.Persistent);
            _wheelRaycatsDistance = new NativeArray<float>(_totalWheels, Allocator.Persistent);
            _wheelRadius = new NativeArray<float>(_totalWheels, Allocator.Persistent);
            _wheelAssociatedCar = new NativeArray<int>(_totalWheels, Allocator.Persistent);
            _wheelCanSteer = new NativeArray<bool>(_totalWheels, Allocator.Persistent);
            _wheelSuspensionForce = new NativeArray<float3>(_totalWheels, Allocator.Persistent);
            _wheelMaxSuspension = new NativeArray<float>(_totalWheels, Allocator.Persistent);
            _wheelSpringStiffness = new NativeArray<float>(_totalWheels, Allocator.Persistent);
            _wheelRaycatsResult = new NativeArray<RaycastHit>(_totalWheels, Allocator.Persistent);
            _wheelRaycastCommand = new NativeArray<RaycastCommand>(_totalWheels, Allocator.Persistent);
            _wheelSpringForce = new NativeArray<float>(_totalWheels, Allocator.Persistent);
            _excludedWheels = new NativeArray<bool>(_totalWheels, Allocator.Persistent);
            _groundedWheels = new NativeArray<int>(_totalWheels, Allocator.Persistent);

            _vehicleTrigger = new TransformAccessArray(nrOfVehicles);
            _vehicleType = new NativeArray<VehicleTypes>(nrOfVehicles, Allocator.Persistent);
            _vehicleForwardForce = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleLaterlForce = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _trailerForwardForce = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);

            _vehiclePosition = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleGroundDirection = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleDownDirection = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleRightDirection = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _trailerRightDirection = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleForwardDirection = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleTargetWaypointPosition = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleNextWaypointPosition = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleStopPosition = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _vehicleVelocity = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);
            _trailerVelocity = new NativeArray<float3>(nrOfVehicles, Allocator.Persistent);

            _wheelRotation = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _turnAngle = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleDrag = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _massDifference = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _trailerDrag = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleSteeringStep = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleDistanceToStop = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _accelerationPercent = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _steerPercent = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _brakePercent = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _requiredBrakePower = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleMaxAllowedSpeed = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleTargetSpeed = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleWheelDistance = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehiclePowerStep = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleBrakeStep = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _raycastLengths = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _wCircumferences = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleRotationAngle = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);
            _vehicleMaxSteer = new NativeArray<float>(nrOfVehicles, Allocator.Persistent);


            _vehicleListIndex = new NativeArray<int>(nrOfVehicles, Allocator.Persistent);
            _vehicleEndWheelIndex = new NativeArray<int>(nrOfVehicles, Allocator.Persistent);
            _vehicleStartWheelIndex = new NativeArray<int>(nrOfVehicles, Allocator.Persistent);
            _vehicleNrOfWheels = new NativeArray<int>(nrOfVehicles, Allocator.Persistent);
            _trailerNrWheels = new NativeArray<int>(nrOfVehicles, Allocator.Persistent);


            _vehicleReadyToRemove = new NativeArray<bool>(nrOfVehicles, Allocator.Persistent);
            _vehicleNeedWaypoint = new NativeArray<bool>(nrOfVehicles, Allocator.Persistent);
            _vehicleIsBraking = new NativeArray<bool>(nrOfVehicles, Allocator.Persistent);
            _vehicleNotDrivable = new NativeArray<bool>(nrOfVehicles, Allocator.Persistent);
            _excludedVehicles = new NativeArray<bool>(nrOfVehicles, Allocator.Persistent);
            _addExcludedVehicle = new NativeArray<bool>(nrOfVehicles, Allocator.Persistent);
            _vehicleCurrentGear = new NativeArray<int>(nrOfVehicles, Allocator.Persistent);
            _vehicleTargetGear = new NativeArray<int>(nrOfVehicles, Allocator.Persistent);
            _stopTargetReached = new NativeArray<bool>(nrOfVehicles, Allocator.Persistent);

            _vehicleRigidbody = new Rigidbody[nrOfVehicles];
            _trailerRigidbody = new Dictionary<int, Rigidbody>();

            //initialize other managers
            _activeCameraPositions = new NativeArray<float3>(activeCameras.Length, Allocator.Persistent);
            for (int i = 0; i < _activeCameraPositions.Length; i++)
            {
                _activeCameraPositions[i] = activeCameras[i].position;
            }

            if (trafficOptions.DistanceToRemove < 0)
            {
                float cellSize = GridData.GridCellSize;
                trafficOptions.DistanceToRemove = 2 * cellSize + cellSize / 2;
            }

            if (trafficOptions.MinDistanceToAdd < 0)
            {
                float cellSize = GridData.GridCellSize;
                trafficOptions.MinDistanceToAdd = cellSize + cellSize / 2;
            }

            _distanceToRemove = trafficOptions.DistanceToRemove * trafficOptions.DistanceToRemove;
            _minDistanceToAdd = trafficOptions.MinDistanceToAdd;

            _timeManager = new TimeManager();
            bool debugDensity = false;

#if UNITY_EDITOR

            var debugSettings = DebugOptions.LoadOrCreateDebugSettings();
            debugDensity = debugSettings.debugDensity;
#endif

            _positionValidator = new PositionValidator(_activeCameras, layerSetup.trafficLayers, layerSetup.playerLayers, layerSetup.buildingsLayers, _minDistanceToAdd, debugDensity);

            _waypointSelector = new WaypointSelector(GridData, trafficOptions.SpawnWaypointSelector, TrafficWaypointsData);

            _behaviourManager = new BehaviourManager(nrOfVehicles, trafficOptions.VehicleBehaviours, TrafficWaypointsData, AllVehiclesData);

            if (trafficOptions.BehaviourImplementation == null)
            {
                _behaviourImplementation = new DefaultBehaviourImplementation().Initialize(AllVehiclesData.AllVehicles, trafficOptions.DefaultPathLength);
            }
            else
            {
                _behaviourImplementation = trafficOptions.BehaviourImplementation.Initialize(AllVehiclesData.AllVehicles, trafficOptions.DefaultPathLength);
            }

            _playerWaypointsManager = new PlayerWaypointsManager();
            _vehicleAI = new VehicleAI(AllVehiclesData, TrafficWaypointsData, SoundManager, TimeManager, trafficOptions.DefaultPathLength, PlayerWaypointsManager);

            //initialize all vehicles
            var tempWheelOrigin = new Transform[_totalWheels];
            var tempWheelGraphic = new Transform[_totalWheels];
            int wheelIndex = 0;
            for (int i = 0; i < nrOfVehicles; i++)
            {
                VehicleComponent vehicle = AllVehiclesData.AllVehicles[i];
                _vehicleTrigger.Add(vehicle.frontTrigger);
                _vehicleRigidbody[i] = vehicle.rb;
                _vehicleSteeringStep[i] = vehicle.SteerStep;

                _vehicleWheelDistance[i] = vehicle.wheelDistance;
                _accelerationPercent[i] = 1;
                _brakePercent[i] = 1;
                _requiredBrakePower[i] = 0;
                _steerPercent[i] = TrafficSystemConstants.AUTO_STEER;
#if UNITY_6000_0_OR_NEWER
                _vehicleDrag[i] = vehicle.rb.linearDamping;
#else
                _vehicleDrag[i] = vehicle.rb.drag;
#endif
                _raycastLengths[i] = vehicle.GetRayCastLength();
                _wCircumferences[i] = vehicle.GetWheelCircumference();
                _vehicleMaxSteer[i] = vehicle.MaxSteer;
                _vehicleStartWheelIndex[i] = wheelIndex;
                _vehicleNrOfWheels[i] = vehicle.GetNrOfWheels();
                _vehicleEndWheelIndex[i] = _vehicleStartWheelIndex[i] + _vehicleNrOfWheels[i];
                _trailerNrWheels[i] = vehicle.GetTrailerWheels();

                for (int j = 0; j < _vehicleNrOfWheels[i]; j++)
                {
                    tempWheelOrigin[wheelIndex] = vehicle.allWheels[j].wheelTransform;
                    tempWheelGraphic[wheelIndex] = vehicle.allWheels[j].wheelTransform.GetChild(0);
                    _wheelCanSteer[wheelIndex] = vehicle.allWheels[j].wheelPosition == Wheel.WheelPosition.Front;
                    _wheelRadius[wheelIndex] = vehicle.allWheels[j].wheelRadius;
                    _wheelMaxSuspension[wheelIndex] = vehicle.allWheels[j].maxSuspension;
                    _wheelSpringStiffness[wheelIndex] = vehicle.SpringStiffness;
                    _wheelAssociatedCar[wheelIndex] = i;
                    _wheelSpringForce[wheelIndex] = vehicle.SpringForce;
                    wheelIndex++;
                }
                if (vehicle.trailer != null)
                {
                    TrailerComponent trailer = vehicle.trailer;
                    for (int j = 0; j < vehicle.trailer.GetNrOfWheels(); j++)
                    {
                        tempWheelOrigin[wheelIndex] = trailer.allWheels[j].wheelTransform;
                        tempWheelGraphic[wheelIndex] = trailer.allWheels[j].wheelTransform.GetChild(0);
                        _wheelCanSteer[wheelIndex] = false;
                        _wheelRadius[wheelIndex] = trailer.allWheels[j].wheelRadius;
                        _wheelMaxSuspension[wheelIndex] = trailer.allWheels[j].maxSuspension;
                        _wheelSpringStiffness[wheelIndex] = trailer.GetSpringStiffness();
                        _wheelAssociatedCar[wheelIndex] = i;
                        _wheelSpringForce[wheelIndex] = trailer.GetSpringForce();
                        wheelIndex++;
                    }
                    _vehicleEndWheelIndex[i] += trailer.GetNrOfWheels();
#if UNITY_6000_0_OR_NEWER
                    _trailerDrag[i] = trailer.rb.linearDamping;
#else
                    _trailerDrag[i] = trailer.rb.drag;
#endif
                    _massDifference[i] = (trailer.rb.mass / vehicle.rb.mass) * (trailer.joint.connectedMassScale / trailer.joint.massScale);
                    _trailerRigidbody.Add(i, trailer.rb);
                }

                _vehicleListIndex[i] = vehicle.ListIndex;
                _vehicleType[i] = vehicle.VehicleType;
            }

            _suspensionConnectPoints = new TransformAccessArray(tempWheelOrigin);
            _wheelsGraphics = new TransformAccessArray(tempWheelGraphic);

            //set the number of jobs based on processor count
            if (SystemInfo.processorCount != 0)
            {
                _nrOfJobs = _totalWheels / SystemInfo.processorCount + 1;
            }
            else
            {
                _nrOfJobs = nrOfVehicles / 4;
            }

            //add events
            Events.OnChangeDestination += DestinationChanged;
            Events.OnVehicleActivated += NewVehicleAdded;

            //initialize the remaining managers

            _disabledWaypointsManager = new DisabledWaypointsManager(TrafficWaypointsData, WaypointSelector, trafficOptions.DisableWaypointsArea);

            _densityManager = new DensityManager(AllVehiclesData, TrafficWaypointsData, _positionValidator, _activeCameraPositions, nrOfVehicles, activeCameras[0].position, activeCameras[0].forward, _activeSquaresLevel, trafficOptions.UseWaypointPriority, trafficOptions.InitialDensity, debugDensity, WaypointSelector);

            _intersectionManager = new IntersectionManager(IntersectionsData, TrafficWaypointsData, pedestrianWaypointsDataHandler, trafficOptions.TrafficLightsBehaviour, trafficOptions.GreenLightTime, trafficOptions.YellowLightTime, TimeManager);

            _activeCellsManager = new ActiveCellsManager(_activeCameraPositions, GridData, trafficOptions.ActiveSquaresLevel);

            if (TrafficModules.PathFinding)
            {
                _pathFindingManager = new PathFindingManager(GridData, _pathFindingData);
            }
#if UNITY_EDITOR
            _debugManager = new DebugManager(debugSettings, AllVehiclesData, _pathFindingData, IntersectionManager, TrafficWaypointsData, GridData, BehaviourManager, DisabledWaypointsManager, _positionValidator);
#endif
            _initialized = true;
        }
        #endregion

        public void InitializeBehaviourImplementation(params object[] parameters)
        {
            _behaviourImplementation.Initialize(parameters);
        }

        private void FixedUpdate()
        {
            if (!_initialized)
                return;

            #region Suspensions

            //for each wheel check where the ground is by performing a RayCast downwards using job system
            for (int i = 0; i < _totalWheels; i++)
            {
                if (_excludedWheels[i] == true)
                    continue;
                _wheelSuspensionPosition[i] = _suspensionConnectPoints[i].position;
                _wheelVelocity[i] = _vehicleRigidbody[_wheelAssociatedCar[i]].GetPointVelocity(_wheelSuspensionPosition[i]);
            }

            for (int i = 0; i < _nrOfVehicles; i++)
            {
                if (_excludedVehicles[i] == true)
                    continue;

                if (_vehicleRigidbody[i].IsSleeping())
                {
                    continue;
                }
                if (_trailerNrWheels[i] > 0)
                {
#if UNITY_6000_0_OR_NEWER
                    _trailerVelocity[i] = _trailerRigidbody[i].linearVelocity;
#else
                    _trailerVelocity[i] = _trailerRigidbody[i].velocity;
#endif
                    _trailerRightDirection[i] = _trailerRigidbody[i].transform.right;

                }
#if UNITY_6000_0_OR_NEWER
                _vehicleVelocity[i] = _vehicleRigidbody[i].linearVelocity;
#else
                _vehicleVelocity[i] = _vehicleRigidbody[i].velocity;
#endif

                _vehicleDownDirection[i] = -AllVehiclesData.GetVehicle(i).GetFrontAxleUpVector();
                _forward = AllVehiclesData.GetVehicle(i).GetFrontAxleForwardVector();
                _vehicleForwardDirection[i] = _forward;
                _vehicleRightDirection[i] = AllVehiclesData.GetVehicle(i).GetFrontAxleRightVector();
                _vehiclePosition[i] = AllVehiclesData.GetVehicle(i).GetFrontAxlePosition();
                _vehicleGroundDirection[i] = float3.zero; //AllVehiclesDataHandler.GetGroundDirection(i);
                _groundedWheels[i] = 0;

                _behaviourResult = BehaviourManager.ExecuteBehaviour(AllVehiclesData.AllVehicles[i].MovementInfo, i, _requiredBrakePower[i], _stopTargetReached[i], _vehicleCurrentGear[i], AllVehiclesData.AllVehicles[i].FrontTrigger.position, _vehicleStopPosition[i]);
                _vehicleMaxAllowedSpeed[i] = _behaviourResult.MaxAllowedSpeed; //min
                _brakePercent[i] = _behaviourResult.BrakePercent; //max
                _vehicleStopPosition[i] = _behaviourResult.SpeedInPoint.StopPosition; //closest
                _vehicleDistanceToStop[i] = _behaviourResult.SpeedInPoint.DistanceToStop; // max
                _vehicleTargetGear[i] = _behaviourResult.TargetGear; // 
                _steerPercent[i] = _behaviourResult.SteerPercent; //max
                _vehicleTargetSpeed[i] = _behaviourResult.SpeedInPoint.TargetSpeed; //min

            }

            for (int i = 0; i < _totalWheels; i++)
            {
                if (_excludedWheels[i] == true)
                    continue;

                if (_vehicleRigidbody[_wheelAssociatedCar[i]].IsSleeping())
                {
                    continue;
                }
#if UNITY_2022_2_OR_NEWER
                _wheelRaycastCommand[i] = new RaycastCommand(_wheelSuspensionPosition[i], _vehicleDownDirection[_wheelAssociatedCar[i]], new QueryParameters(layerMask: _roadLayers), _raycastLengths[_wheelAssociatedCar[i]]);
#else
                _wheelRaycastCommand[i] = new RaycastCommand(_wheelSuspensionPosition[i], _vehicleDownDirection[_wheelAssociatedCar[i]], _raycastLengths[_wheelAssociatedCar[i]], _roadLayers);
#endif
            }
            _raycastJobHandle = RaycastCommand.ScheduleBatch(_wheelRaycastCommand, _wheelRaycatsResult, _nrOfJobs, default);
            _raycastJobHandle.Complete();

            for (int i = 0; i < _totalWheels; i++)
            {
                if (_excludedWheels[i] == true)
                    continue;

                if (_vehicleRigidbody[_wheelAssociatedCar[i]].IsSleeping())
                {
                    continue;
                }

                _wheelRaycatsDistance[i] = _wheelRaycatsResult[i].distance;
                _wheelNormalDirection[i] = _wheelRaycatsResult[i].normal;
                _wheelGroundPosition[i] = _wheelRaycatsResult[i].point;
                if (_wheelRaycatsDistance[i] != 0)
                {
                    _vehicleGroundDirection[_wheelAssociatedCar[i]] += _wheelNormalDirection[i];
                    _groundedWheels[_wheelAssociatedCar[i]]++;
                }
            }
            #endregion

            #region Driving
            //execute job for wheel turn and driving
            _wheelJob = new WheelJob()
            {
                WheelSuspensionForce = _wheelSuspensionForce,
                SpringForces = _wheelSpringForce,
                WheelMaxSuspension = _wheelMaxSuspension,
                WheelRayCastDistance = _wheelRaycatsDistance,
                WheelRadius = _wheelRadius,
                WheelNormalDirection = _wheelNormalDirection,
                WheelVelocity = _wheelVelocity,
                SpringStiffness = _wheelSpringStiffness,
                ExcludedWheels = _excludedWheels,
            };

            _driveJob = new DriveJob()
            {
                WheelCircumferences = _wCircumferences,
                CarVelocity = _vehicleVelocity,
                FixedDeltaTime = Time.fixedDeltaTime,
                TargetWaypointPosition = _vehicleTargetWaypointPosition,
                NextWaypointPosition = _vehicleNextWaypointPosition,
                StopPosition = _vehicleStopPosition,
                AllBotsPosition = _vehiclePosition,
                MaxSteer = _vehicleMaxSteer,
                ForwardDirection = _vehicleForwardDirection,
                WorldUp = _up,
                WheelRotation = _wheelRotation,
                TurnAngle = _turnAngle,
                VehicleRotationAngle = _vehicleRotationAngle,
                ReadyToRemove = _vehicleReadyToRemove,
                NeedsWaypoint = _vehicleNeedWaypoint,
                DistanceToRemove = _distanceToRemove,
                CameraPositions = _activeCameraPositions,
                ForwardForce = _vehicleForwardForce,
                LateralForce = _vehicleLaterlForce,
                RightDirection = _vehicleRightDirection,
                PowerStep = _vehiclePowerStep,
                BrakeStep = _vehicleBrakeStep,
                IsBraking = _vehicleIsBraking,
                Drag = _vehicleDrag,
                MaximumAllowedSpeed = _vehicleMaxAllowedSpeed,
                CurrentGear = _vehicleCurrentGear,
                TargetGear = _vehicleTargetGear,
                GroundDirection = _vehicleGroundDirection,
                SteeringStep = _vehicleSteeringStep,
                WheelDistance = _vehicleWheelDistance,
                NrOfWheels = _vehicleNrOfWheels,
                TrailerVelocity = _trailerVelocity,
                TrailerForce = _trailerForwardForce,
                TrailerRightDirection = _trailerRightDirection,
                TrailerNrOfWheels = _trailerNrWheels,
                MassDifference = _massDifference,
                TrailerDrag = _trailerDrag,
                DistanceToStop = _vehicleDistanceToStop,
                ExcludedVehicles = _excludedVehicles,
                GroundedWheels = _groundedWheels,
                AccelerationPercent = _accelerationPercent,
                BrakePercent = _brakePercent,
                RequiredBrakePower = _requiredBrakePower,
                SteerPercent = _steerPercent,
                TargetReached = _stopTargetReached,
                TargetSpeed = _vehicleTargetSpeed,
            };

            _wheelJobHandle = _wheelJob.Schedule(_totalWheels, _nrOfJobs);
            _driveJobHandle = _driveJob.Schedule(_nrOfVehicles, _nrOfJobs);
            _wheelJobHandle.Complete();
            _driveJobHandle.Complete();

            //store job values
            _wheelSuspensionForce = _wheelJob.WheelSuspensionForce;
            _wheelRotation = _driveJob.WheelRotation;
            _turnAngle = _driveJob.TurnAngle;
            _vehicleRotationAngle = _driveJob.VehicleRotationAngle;
            _vehicleReadyToRemove = _driveJob.ReadyToRemove;
            _vehicleNeedWaypoint = _driveJob.NeedsWaypoint;
            _vehicleForwardForce = _driveJob.ForwardForce;
            _vehicleLaterlForce = _driveJob.LateralForce;
            _vehicleIsBraking = _driveJob.IsBraking;
            _vehicleCurrentGear = _driveJob.CurrentGear;
            _trailerForwardForce = _driveJob.TrailerForce;


            //make vehicle actions based on job results
            for (int i = 0; i < _nrOfVehicles; i++)
            {
                if (_excludedVehicles[i] == true)
                    continue;

                if (!_vehicleRigidbody[i].IsSleeping())
                {
                    int groundedWheels = 0;
                    for (int j = _vehicleStartWheelIndex[i]; j < _vehicleEndWheelIndex[i] - _trailerNrWheels[i]; j++)
                    {
                        if (_wheelRaycatsDistance[j] != 0)
                        {
                            groundedWheels++;

                            //apply suspension
                            _vehicleRigidbody[i].AddForceAtPosition(_wheelSuspensionForce[j], _wheelGroundPosition[j]);

                            //apply friction
                            _vehicleRigidbody[i].AddForceAtPosition(_vehicleLaterlForce[i], new Vector3(_wheelSuspensionPosition[j].x, _wheelGroundPosition[j].y + (_wheelSuspensionPosition[j].y - _wheelGroundPosition[j].y) / 1.8f, _wheelSuspensionPosition[j].z), ForceMode.Acceleration);

                            if (_vehicleNotDrivable[i] == false)
                            {
                                //apply traction
                                _vehicleRigidbody[i].AddForceAtPosition(_vehicleForwardForce[i], _wheelGroundPosition[j], ForceMode.Acceleration);
                            }
                        }
                        else
                        {
                            //if the wheel is not grounded apply additional gravity to stabilize the vehicle for a more realistic movement
                            _vehicleRigidbody[i].AddForceAtPosition(Physics.gravity * _vehicleRigidbody[i].mass / (_vehicleEndWheelIndex[i] - _vehicleStartWheelIndex[i]), _wheelSuspensionPosition[j]);
                        }
                    }

                    //TODO Change this
                    if (_trailerNrWheels[i] > 0)
                    {
                        for (int j = _vehicleEndWheelIndex[i] - _trailerNrWheels[i]; j < _vehicleEndWheelIndex[i]; j++)
                        {
                            if (_wheelRaycatsDistance[j] != 0)
                            {
                                //if wheel is grounded apply suspension force
                                _trailerRigidbody[i].AddForceAtPosition(_wheelSuspensionForce[j], _wheelGroundPosition[j]);

                                //apply side friction
                                _trailerRigidbody[i].AddForceAtPosition(_trailerForwardForce[i], _wheelSuspensionPosition[j], ForceMode.VelocityChange);

                                if (_vehicleIsBraking[i])
                                {
                                    _trailerRigidbody[i].AddForceAtPosition(_vehicleForwardForce[i] / _trailerNrWheels[i], _wheelSuspensionPosition[j], ForceMode.Acceleration);
                                }
                            }
                            else
                            {
                                //if the wheel is not grounded apply additional gravity to stabilize the vehicle for a more realistic movement
                                //_trailerRigidbody[i].AddForceAtPosition(Physics.gravity * _trailerRigidbody[i].mass / _trailerNrWheels[i], _wheelSuspensionPosition[j]);
                            }
                        }
                        //_trailerRigidbody[i].angularVelocity = Vector3.zero;
                    }

                    if (_vehicleNotDrivable[i] == true)
                        continue;

                    Vector3 angVel = _vehicleRigidbody[i].angularVelocity;
                    angVel.y = 0;
                    _vehicleRigidbody[i].angularVelocity = angVel;
                    //apply rotation 
                    if (groundedWheels != 0)
                    {
                        _vehicleRigidbody[i].MoveRotation(_vehicleRigidbody[i].rotation * Quaternion.Euler(0, _vehicleRotationAngle[i], 0));
                    }

                    //run AI
                    VehicleAI.Drive(i, _vehicleCurrentGear[i], _vehicleNeedWaypoint[i], AllVehiclesData.AllVehicles[i].VehicleType);
                    _allVehiclesData.AllVehicles[i].ApplyAdditionalForces(_turnAngle[i]);
                }
            }
            #endregion
        }


        private void Update()
        {
            if (!_initialized)
                return;

            TimeManager.UpdateTime();

            //update brake lights
            for (int i = 0; i < _nrOfVehicles; i++)
            {
                AllVehiclesData.AllVehicles[i].SetBrakeLights(_vehicleIsBraking[i]);
            }

            #region WheelUpdate
            //update wheel graphics
            for (int i = 0; i < _totalWheels; i++)
            {
                if (_excludedWheels[i] == true)
                {
                    continue;
                }

                _wheelSuspensionPosition[i] = _suspensionConnectPoints[i].position;
                _wheelRightDirection[i] = _suspensionConnectPoints[i].right;
            }

            _updateWheelJob = new UpdateWheelJob()
            {
                WheelsOrigin = _wheelSuspensionPosition,
                DownDirection = _vehicleDownDirection,
                WheelRotation = _wheelRotation,
                TurnAngle = _turnAngle,
                WheelRadius = _wheelRadius,
                MaxSuspension = _wheelMaxSuspension,
                RayCastDistance = _wheelRaycatsDistance,
                NrOfVehicles = _nrOfVehicles,
                CanSteer = _wheelCanSteer,
                VehicleIndex = _wheelAssociatedCar,
                ExcludedWheels = _excludedWheels,
            };
            _updateWheelJobHandle = _updateWheelJob.Schedule(_wheelsGraphics);
            _updateWheelJobHandle.Complete();
            #endregion

            #region TriggerUpdate
            //update trigger orientation
            _updateTriggerJob = new UpdateTriggerJob()
            {
                TurnAngle = _turnAngle,
                ExcludedVehicles = _excludedVehicles,
            };
            _updateTriggerJobHandle = _updateTriggerJob.Schedule(_vehicleTrigger);
            _updateTriggerJobHandle.Complete();
            #endregion

            #region RemoveVehicles
            // —— 每10s在玩家后方刷一辆 SpeedingVehicle ——
            // 选择当前被用于密度管理的那台“活跃相机”做为玩家位姿（你已有 _activeCameraIndex 的随机选择）
            _speedingSpawnTimer += Time.deltaTime;
            if (_speedingSpawnTimer >= SPEEDING_SPAWN_INTERVAL)
            {
                _speedingSpawnTimer = 0f;

                // 玩家当前位置与朝向（这里用活跃相机近似玩家）
                Vector3 playerPos = _activeCameras[_activeCameraIndex].position;
                Vector3 playerFwd = _activeCameras[_activeCameraIndex].forward;

                // 刷在玩家后方：距离 = MinDistanceToAdd（系统安全半径） + 额外偏移，确保不会太近
                float spawnDistance = _minDistanceToAdd + SPEEDING_BEHIND_EXTRA;
                Vector3 desiredPos = playerPos - playerFwd * spawnDistance;

                // 尝试请求在该位置生成一个 SpeedingVehicle（若池里没有这类车或格网无可用点，请求会自动失败并给出警告）
                DensityManager.RequestVehicleAtPosition(
                    desiredPos,
                    VehicleTypes.SpeedingVehicle,   // 需要你的枚举里有这个项
                    /*completeMethod*/ null,
                    /*path*/ null
                );
            }


            //remove vehicles that are too far away and not in view
            _indexToRemove++;
            if (_indexToRemove == _nrOfVehicles)
            {
                _indexToRemove = 0;
            }
            _activeCameraIndex = UnityEngine.Random.Range(0, _activeCameraPositions.Length);
            DensityManager.UpdateVehicleDensity(_activeCameras[_activeCameraIndex].position, _activeCameras[_activeCameraIndex].forward, _activeCameraPositions[_activeCameraIndex]);

            if (_vehicleReadyToRemove[_indexToRemove] == true)
            {
                if (_excludedVehicles[_indexToRemove] == false || (_excludedVehicles[_indexToRemove] == true && _addExcludedVehicle[_indexToRemove] == true))
                {
                    if (_vehicleRigidbody[_indexToRemove].gameObject.activeSelf)
                    {
                        if (AllVehiclesData.AllVehicles[_vehicleListIndex[_indexToRemove]].CanBeRemoved() == true)
                        {
                            _vehicleReadyToRemove[_indexToRemove] = false;
                            DensityManager.RemoveVehicle(_indexToRemove, false);
                        }
                    }
                }
            }
            #endregion

            //update additional managers
            for (int i = 0; i < _activeCameras.Length; i++)
            {
                _activeCameraPositions[i] = _activeCameras[i].transform.position;
            }
            IntersectionManager.UpdateIntersections(TimeManager.RealTimeSinceStartup);
            ActiveCellsManager.UpdateGrid(_activeSquaresLevel, _activeCameraPositions);

            #region Debug
#if UNITY_EDITOR
            DebugManager.Update(_nrOfVehicles, _totalWheels, _wheelSuspensionPosition, _wheelSuspensionForce, _wheelAssociatedCar);
#endif
            #endregion
        }

        #region API Methods

        /// <summary>
        /// Update active camera that is used to remove vehicles when are not in view
        /// </summary>
        /// <param name="activeCamera">represents the camera or the player prefab</param>
        public void UpdateCamera(Transform[] activeCameras)
        {
            if (!_initialized)
                return;

            if (activeCameras.Length != _activeCameraPositions.Length)
            {
                _activeCameraPositions = new NativeArray<float3>(activeCameras.Length, Allocator.Persistent);
            }

            _activeCameras = activeCameras;
            DensityManager.UpdateCameraPositions(activeCameras);

        }


        public float GetSteeringAngle(int vehicleIndex)
        {
            if (!_initialized)
                return 0;
            if (vehicleIndex >= 0 && vehicleIndex < _nrOfVehicles)
            {
                return _turnAngle[vehicleIndex];
            }
            else
            {
                Debug.LogError($"Vehicle index {vehicleIndex} is invalid. It should be between 0 and {_nrOfVehicles}");
                return 0;
            }
        }


        public void SetActiveSquaresLevel(int activeSquaresLevel)
        {
            if (!_initialized)
                return;

            _activeSquaresLevel = activeSquaresLevel;
            DensityManager.UpdateActiveSquaresLevel(activeSquaresLevel);
        }


        public void StopVehicleDriving(GameObject vehicle)
        {
            if (!_initialized)
                return;

            int vehicleIndex = AllVehiclesData.GetVehicleIndex(vehicle);
            if (vehicleIndex != TrafficSystemConstants.INVALID_VEHICLE_INDEX)
            {
                _vehicleNotDrivable[vehicleIndex] = true;
            }
        }


        public void InstantiateVehicleWithPath(Vector3 position, VehicleTypes vehicleType, Vector3 destination, UnityAction<VehicleComponent, int> completeMethod)
        {
            List<int> path = PathFindingManager.GetPath(position, destination, vehicleType);
            if (path != null)
            {
                //aici tre sa vina un callback
                DensityManager.RequestVehicleAtPosition(position, vehicleType, completeMethod, path);
            }
        }


        public void SetDestination(int vehicleIndex, Vector3 position)
        {
            if (PathFindingManager != null)
            {
                var path = PathFindingManager.GetPathToDestination(vehicleIndex, AllVehiclesData.AllVehicles[vehicleIndex].MovementInfo.GetWaypointIndex(0), position, AllVehiclesData.AllVehicles[vehicleIndex].VehicleType);
                if (path != null)
                {
                    AllVehiclesData.AllVehicles[vehicleIndex].MovementInfo.SetPath(path);
                }
            }
        }

        public void ExcludeVehicleFromSystem(int vehicleIndex)
        {
            if (vehicleIndex >= 0 && vehicleIndex < _nrOfVehicles)
            {
                _excludedVehicles[vehicleIndex] = true;
                for (int i = 0; i < _wheelAssociatedCar.Length; i++)
                {
                    if (_wheelAssociatedCar[i] == vehicleIndex)
                    {
                        _excludedWheels[i] = true;
                    }
                }
            }
            else
            {
                Debug.LogError($"Vehicle index {vehicleIndex} is invalid. It should be between 0 and {_nrOfVehicles}");
            }
        }

        public void RestoreRemovedVehicleToSystem(int vehicleIndex)
        {
            if (vehicleIndex >= 0 && vehicleIndex < _nrOfVehicles)
            {
                _addExcludedVehicle[vehicleIndex] = true;
            }
            else
            {
                Debug.LogError($"Vehicle index {vehicleIndex} is invalid. It should be between 0 and {_nrOfVehicles}");
            }
        }

        #endregion


        #region EventHandlers
        /// <summary>
        /// Called every time a new vehicle is enabled
        /// </summary>
        /// <param name="vehicleIndex">index of the vehicle</param>
        /// <param name="targetWaypointPosition">target position</param>
        /// <param name="maxSpeed">max possible speed</param>
        /// <param name="powerStep">acceleration power</param>
        /// <param name="brakeStep">brake power</param>
        private void NewVehicleAdded(int vehicleIndex, int waypointIndex)
        {
            //set new vehicle parameters
            _vehicleTargetWaypointPosition[vehicleIndex] = _vehicleStopPosition[vehicleIndex] = TrafficWaypointsData.AllTrafficWaypoints[waypointIndex].Position;
            _vehicleNextWaypointPosition[vehicleIndex] = TrafficWaypointsData.AllTrafficWaypoints[waypointIndex].Position;
            _vehicleStopPosition[vehicleIndex] = TrafficSystemConstants.DEFAULT_POSITION;
            _vehiclePowerStep[vehicleIndex] = AllVehiclesData.AllVehicles[vehicleIndex].PowerStep;
            _vehicleBrakeStep[vehicleIndex] = AllVehiclesData.AllVehicles[vehicleIndex].BrakeStep;

            _vehicleIsBraking[vehicleIndex] = false;
            _vehicleNeedWaypoint[vehicleIndex] = false;
            _vehicleNotDrivable[vehicleIndex] = false;
            _addExcludedVehicle[vehicleIndex] = false;
            if (_excludedVehicles[vehicleIndex] == true)
            {
                _excludedVehicles[vehicleIndex] = false;
                for (int i = 0; i < _wheelAssociatedCar.Length; i++)
                {
                    if (_wheelAssociatedCar[i] == vehicleIndex)
                    {
                        _excludedWheels[i] = false;
                    }
                }
            }
            _vehicleCurrentGear[vehicleIndex] = 1;
            _vehicleTargetGear[vehicleIndex] = 1;
            _turnAngle[vehicleIndex] = 0;

            //reset AI

            //set initial velocity
            var initialVelocity = AllVehiclesData.AllVehicles[vehicleIndex].GetForwardVector() * AllVehiclesData.AllVehicles[vehicleIndex].MovementInfo.GetFirstWaypointSpeed() / 2;
            //var initialVelocity = VehiclePositioningSystem.GetForwardVector(vehicleIndex) * 0;
#if UNITY_6000_0_OR_NEWER
            _vehicleRigidbody[vehicleIndex].linearVelocity = initialVelocity;
#else
            _vehicleRigidbody[vehicleIndex].velocity = initialVelocity;
#endif
            if (_trailerNrWheels[vehicleIndex] != 0)
            {
#if UNITY_6000_0_OR_NEWER
                _trailerRigidbody[vehicleIndex].linearVelocity = _vehicleRigidbody[vehicleIndex].linearVelocity;
#else
                _trailerRigidbody[vehicleIndex].velocity = _vehicleRigidbody[vehicleIndex].velocity;
#endif
            }
            //vehicleRigidbody[vehicleIndex].velocity = Vector3.zero;
        }


        /// <summary>
        /// Called when waypoint changes
        /// </summary>
        /// <param name="vehicleIndex">vehicle index</param>
        /// <param name="targetWaypointPosition">new waypoint position</param>
        /// <param name="maxSpeed">new possible speed</param>
        /// <param name="blinkType">blinking is required or not</param>
        private void DestinationChanged(int vehicleIndex, Vector3 newPosition)
        {
            _vehicleNeedWaypoint[vehicleIndex] = false;
            _vehicleTargetWaypointPosition[vehicleIndex] = newPosition;
            _vehicleNextWaypointPosition[vehicleIndex] = AllVehiclesData.GetVehicle(vehicleIndex).MovementInfo.GetPosition(1);
        }
        #endregion


        #region Cleanup
        /// <summary>
        /// Cleanup
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                _wheelSpringForce.Dispose();
                _raycastLengths.Dispose();
                _wCircumferences.Dispose();
                _wheelRadius.Dispose();
                _vehicleVelocity.Dispose();
                _trailerVelocity.Dispose();
                _vehicleMaxSteer.Dispose();
                _suspensionConnectPoints.Dispose();
                _wheelsGraphics.Dispose();
                _wheelGroundPosition.Dispose();
                _wheelVelocity.Dispose();
                _wheelRotation.Dispose();
                _turnAngle.Dispose();
                _wheelRaycatsResult.Dispose();
                _wheelRaycastCommand.Dispose();
                _wheelCanSteer.Dispose();
                _wheelAssociatedCar.Dispose();
                _vehicleEndWheelIndex.Dispose();
                _vehicleStartWheelIndex.Dispose();
                _vehicleNrOfWheels.Dispose();
                _trailerNrWheels.Dispose();
                _vehicleDownDirection.Dispose();
                _vehicleForwardDirection.Dispose();
                _vehicleRotationAngle.Dispose();
                _vehicleStopPosition.Dispose();
                _vehicleRightDirection.Dispose();
                _vehicleTargetWaypointPosition.Dispose();
                _vehicleNextWaypointPosition.Dispose();
                _vehiclePosition.Dispose();
                _vehicleGroundDirection.Dispose();
                _vehicleReadyToRemove.Dispose();
                _vehicleListIndex.Dispose();
                _vehicleNeedWaypoint.Dispose();
                _wheelRaycatsDistance.Dispose();
                _wheelRightDirection.Dispose();
                _wheelNormalDirection.Dispose();
                _wheelMaxSuspension.Dispose();
                _wheelSuspensionForce.Dispose();
                _vehicleForwardForce.Dispose();
                _vehicleLaterlForce.Dispose();
                _vehicleSteeringStep.Dispose();
                _vehicleCurrentGear.Dispose();
                _vehicleTargetGear.Dispose();
                _vehicleDrag.Dispose();
                _vehicleWheelDistance.Dispose();
                _vehiclePowerStep.Dispose();
                _vehicleBrakeStep.Dispose();
                _vehicleTrigger.Dispose();
                _vehicleType.Dispose();
                _vehicleIsBraking.Dispose();
                _vehicleNotDrivable.Dispose();
                _activeCameraPositions.Dispose();
                _trailerForwardForce.Dispose();
                _trailerRightDirection.Dispose();
                _trailerDrag.Dispose();
                _massDifference.Dispose();
                _wheelSuspensionPosition.Dispose();
                _vehicleDistanceToStop.Dispose();
                _wheelSpringStiffness.Dispose();
                _excludedVehicles.Dispose();
                _excludedWheels.Dispose();
                _addExcludedVehicle.Dispose();
                _groundedWheels.Dispose();
                _accelerationPercent.Dispose();
                _brakePercent.Dispose();
                _requiredBrakePower.Dispose();
                _steerPercent.Dispose();
                _vehicleMaxAllowedSpeed.Dispose();
                _stopTargetReached.Dispose();
                _vehicleTargetSpeed.Dispose();
            }
            catch { }
            Events.OnChangeDestination -= DestinationChanged;
            Events.OnVehicleActivated -= NewVehicleAdded;

            DestroyableManager.Instance.DestroyAll();
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_initialized)
                return;
            DebugManager.DrawGizmos();
        }
#endif
    }
}
#endif