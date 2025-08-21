using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rewired;
using Gley.TrafficSystem.Internal;
using static RCCP_Recorder;



#if GLEY_TRAFFIC_SYSTEM
using TrafficManager = Gley.TrafficSystem.Internal.TrafficManager;
using CellData = Gley.UrbanSystem.Internal.CellData;
using PlayerWaypointsManager = Gley.TrafficSystem.Internal.PlayerWaypointsManager;
using GridData = Gley.UrbanSystem.Internal.GridData;
using TrafficWaypointsData = Gley.TrafficSystem.Internal.TrafficWaypointsData;
#endif


namespace Gley.TrafficSystem
{
    public class PlayerComponent : MonoBehaviour, ITrafficParticipant
    {
        [SerializeField, Tooltip("当前速度 (km/h)")]
        private float currentSpeedKmh;
        [SerializeField]
        public List<VehicleComponent> _rightRearAIVehicles = new List<VehicleComponent>();
        [SerializeField] 
        private VehicleComponent _rightRearAICandidate;
        [SerializeField]
        public List<VehicleComponent> _rightFrontAIVehicles = new List<VehicleComponent>();
        [SerializeField]
        private VehicleComponent _rightFrontAICandidate;

        [SerializeField] 
        private RCCP_CarController _carController;

        [SerializeField] private bool indicatorRightOn;
        [SerializeField] private bool indicatorLeftOn;
        [SerializeField] private bool indicatorAllOn;

        // The Rewired player id of this character
        public int playerId = 0;
        private Player player; // The Rewired Player
        [SerializeField] private float overrideSteer = 0f;    // -1 左转，1 右转
        [SerializeField] private float overrideThrottle = 0f; // 0~1
        [SerializeField] private float overrideBrake = 0f;    // 0~1

        public RCCP_Input vehicleInput;


        public GameObject leftArrow, rightArrow;
        private bool turnLeft = false;
        private bool turnRight = false;

        public bool isBehindSpeeding;
        public GameObject dashboard;
        private Rigidbody _rb;
        private Transform _myTransform;

#if GLEY_TRAFFIC_SYSTEM
        private List<TrafficWaypoint> _allWaypoints;
        private List<Vector2Int> _cellNeighbors;     

        private GridData _gridData;
        private CellData _currentCell;
        private PlayerWaypointsManager _playerWaypointsManager;
        private TrafficWaypointsData _trafficWaypointsData;
        private TrafficWaypoint _proposedTarget;
        private TrafficWaypoint _currentTarget;
        private Vector3 _playerPosition;
        private bool _initialized;
        private bool _targetChanged;







        // --- Telemetry accessors for ExperimentLogger ---
        // 原生输入（直接用你现有的 vehicleInput）
        public float RawGas => vehicleInput != null ? vehicleInput.throttleInput : 0f;
        public float RawBrake => vehicleInput != null ? vehicleInput.brakeInput : 0f;
        public float RawSteer => vehicleInput != null ? vehicleInput.steerInput : 0f;

        // 转向灯状态（使用你现有的序列化布尔量）
        public bool IndicatorLeftOn => indicatorLeftOn;
        public bool IndicatorRightOn => indicatorRightOn;
        public bool IndicatorAllOn => indicatorAllOn;

        // 参与者自身的 Transform / Rigidbody
        public Transform PlayerTransform => transform;
        private Rigidbody _cachedRb;
        public Rigidbody PlayerRigidbody => _cachedRb ? _cachedRb : (_cachedRb = GetComponent<Rigidbody>());

        // 右侧并线候选（你已有的私有字段公开为只读）
        public VehicleComponent RightFrontCandidate => _rightFrontAICandidate;
        public VehicleComponent RightRearCandidate => _rightRearAICandidate;







        private void Awake()
        {

            player = ReInput.players.GetPlayer(playerId);

            _carController = GetComponent<RCCP_CarController>();
        }


        private void OnEnable()
        {
            StartCoroutine(Initialize());
        }


        IEnumerator Initialize()
        {
            while (!TrafficManager.Instance.Initialized)
            {
                yield return null;
            }
            _rb = GetComponent<Rigidbody>();
            _myTransform = transform;
            _gridData = TrafficManager.Instance.GridData;
            _trafficWaypointsData = TrafficManager.Instance.TrafficWaypointsData;
            _playerWaypointsManager = TrafficManager.Instance.PlayerWaypointsManager;
            _playerWaypointsManager.RegisterPlayer(GetInstanceID(), -1);
            _allWaypoints = new List<TrafficWaypoint>();
            _initialized = true;
        }


        void Update()
        {
            /*if (isBehindSpeeding)
            {
                dashboard.SetActive(true);
            }
            else
            {
                dashboard.SetActive(false);
            }*/

            UpdateIndicatorDebug();
            UpdateRightRearCandidate();
            UpdateRightFrontCandidate();

            if (indicatorRightOn && _rightRearAICandidate != null)
            {
                _rightRearAICandidate.SetFrontMergeAssistTarget(this);
            } else if (!indicatorRightOn && _rightRearAICandidate != null)
            {
                _rightRearAICandidate.RemoveFrontMergeAssistTarget();
            }

            if (indicatorRightOn && _rightFrontAICandidate != null)
            {
                _rightFrontAICandidate.SetBehindMergeAssistTarget(this);
            }
            else if (!indicatorRightOn && _rightFrontAICandidate != null)
            {
                _rightFrontAICandidate.RemoveBehindMergeAssistTarget();
            }

            ApplyOverrideInput();



            if (!_initialized)
            {
                return;
            }
            _playerPosition = _myTransform.position;
            CellData cell = _gridData.GetCell(_playerPosition);

            // Update waypoints only if the player changes the grid cell
            if (cell != _currentCell)
            {
                _currentCell = cell;
                _cellNeighbors = _gridData.GetCellNeighbors(cell.CellProperties.Row, cell.CellProperties.Column, 1, false);
                _allWaypoints.Clear();

                foreach (var neighbor in _cellNeighbors)
                {
                    _allWaypoints.AddRange(_gridData.GetAllTrafficWaypointsInCell(neighbor).Select(index => _trafficWaypointsData.AllTrafficWaypoints[index]));
                }
            }

            // Find closest valid waypoint
            float minDistance = Mathf.Infinity;
            TrafficWaypoint bestWaypoint = null;

            foreach (var waypoint in _allWaypoints)
            {
                float newDistance = Vector3.SqrMagnitude(_playerPosition - waypoint.Position);
                if (newDistance < minDistance && CheckOrientation(waypoint, out TrafficWaypoint proposedTarget))
                {
                    minDistance = newDistance;
                    bestWaypoint = waypoint;
                    _proposedTarget = proposedTarget; // Store proposed target when orientation is valid
                }
            }

            if (_currentTarget == _proposedTarget)
            {
                return;
            }

            // Determine if we need to change target
            _targetChanged = false;

            if (_currentTarget != null)
            {
                if (_currentTarget.Neighbors.Contains(_proposedTarget.ListIndex))
                {
                    _targetChanged = true;
                }
                else
                {
                    Vector3 forward = _myTransform.forward;
                    float angle1 = Vector3.SignedAngle(forward, _proposedTarget.Position - _playerPosition, Vector3.up);
                    float angle2 = Vector3.SignedAngle(forward, _currentTarget.Position - _playerPosition, Vector3.up);

                    if (Mathf.Abs(angle1) < Mathf.Abs(angle2))
                    {
                        _targetChanged = true;
                    }
                    else
                    {
                        float dist1 = Vector3.SqrMagnitude(_playerPosition - _proposedTarget.Position);
                        float dist2 = Vector3.SqrMagnitude(_playerPosition - _currentTarget.Position);
                        if (dist1 < dist2) _targetChanged = true;
                    }
                }
            }
            else
            {
                _targetChanged = true;
            }

            if (_targetChanged)
            {
                _currentTarget = _proposedTarget;
                _playerWaypointsManager.UpdatePlayerWaypoint(GetInstanceID(), _proposedTarget.ListIndex);
            }








            currentSpeedKmh = GetCurrentSpeedMS() * 3.6f; // m/s → km/h
        }


        /// <summary>
        /// Checks if the waypoint's direction is valid and returns the correct next target.
        /// </summary>
        private bool CheckOrientation(TrafficWaypoint waypoint, out TrafficWaypoint proposedTarget)
        {
            proposedTarget = null;

            if (waypoint.Neighbors.Length < 1)
            {
                return false;
            }

            TrafficWaypoint neighbor = _trafficWaypointsData.AllTrafficWaypoints[waypoint.Neighbors[0]];
            float angle = Vector3.SignedAngle(_myTransform.forward, neighbor.Position - waypoint.Position, Vector3.up);

            if (Mathf.Abs(angle) < 90)
            {
                proposedTarget = neighbor;
                return true;
            }

            return false;
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
/*            if (Application.isPlaying)
            {
                if (_initialized)
                {
                    if (TrafficManager.Instance.DebugManager.IsDebugWaypointsEnabled())
                    {
                        if (_currentTarget != null)
                        {
                            Gizmos.color = Color.green;
                            Vector3 position = _currentTarget.Position;
                            Gizmos.DrawSphere(position, 1);
                        }
                    }
                }
            }*/

            if (!Application.isPlaying) return;

            // 当前速度 (km/h)
            float speedKmh = GetCurrentSpeedMS() * 3.6f;

            // 在 Scene 视图上方绘制文字
            UnityEditor.Handles.color = Color.cyan;
            string text = $"Speed: {speedKmh:F1} km/h";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, text);
        }
#endif
#endif

        public float GetCurrentSpeedMS()
        {
            if (_rb == null)
                return 0f; // 或者一个安全默认值
#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity.magnitude;
#else
    return _rb.velocity.magnitude;
#endif
        }



        public Vector3 GetHeading()
        {
            return _myTransform.forward;
        }

        public bool AlreadyCollidingWith(Collider[] allColliders)
        {
            return false;
        }

        private void UpdateRightRearCandidate()
        {
            if (_rightRearAIVehicles.Count == 0)
            {
                _rightRearAICandidate = null;
                return;
            }

            VehicleComponent nearest = null;
            float minDist = Mathf.Infinity;
            foreach (var ai in _rightRearAIVehicles)
            {
                float dist = Vector3.Distance(transform.position, ai.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = ai;
                }
            }
            _rightRearAICandidate = nearest;
        }

        private void UpdateRightFrontCandidate()
        {
            if (_rightFrontAIVehicles.Count == 0)
            {
                _rightFrontAICandidate = null;
                return;
            }

            VehicleComponent nearest = null;
            float minDist = Mathf.Infinity;
            foreach (var ai in _rightFrontAIVehicles)
            {
                float dist = Vector3.Distance(transform.position, ai.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = ai;
                }
            }
            _rightFrontAICandidate = nearest;
        }

        private void UpdateIndicatorDebug()
        {
            if (_carController != null && _carController.Lights != null)
            {
                indicatorRightOn = _carController.Lights.indicatorsRight;
                indicatorLeftOn = _carController.Lights.indicatorsLeft;
                indicatorAllOn = _carController.Lights.indicatorsAll;
            }
        }


        private void ApplyOverrideInput()
        {
            if (player == null) return;

            // 假设 Rewired 的轴名称是 "Steer", "Throttle", "Brake"
            vehicleInput.throttleInput = player.GetAxis("Accelerator");
            vehicleInput.brakeInput = player.GetAxis("Brake");
            float steerDeadzone = 0;
            if (Mathf.Abs(player.GetAxis("Wheel")) < steerDeadzone)
            {
                vehicleInput.steerInput = 0;
            }
            else
            {
                vehicleInput.steerInput = player.GetAxis("Wheel");
            }
            if (player.GetButtonDown("TurnLeft"))
            {
                turnLeft = !turnLeft;
                leftArrow.SetActive(turnLeft);
            }
            if (player.GetButtonDown("TurnRight"))
            {
                turnRight = !turnRight;
                rightArrow.SetActive(turnRight);
            }
            _carController.Lights.indicatorsLeft = turnLeft;
            _carController.Lights.indicatorsRight = turnRight;

        }
    }
}