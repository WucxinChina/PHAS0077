using Gley.UrbanSystem.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Gley.TrafficSystem.Internal;

namespace Gley.TrafficSystem
{
    /// <summary>
    /// Add this script on a vehicle prefab and configure the required parameters
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [HelpURL("https://gley.gitbook.io/mobile-traffic-system-v3/setup-guide/vehicle-implementation")]
    public class VehicleComponent : MonoBehaviour, ITrafficParticipant
    {

        [SerializeField]
        private MonoBehaviour frontVehicle; // 前车引用
        [SerializeField, Tooltip("当前速度 (km/h)")]
        private float currentSpeedKmh;
        public MonoBehaviour mergeFrontAssistTarget;
        public MonoBehaviour mergeBehindAssistTarget;
        [SerializeField]
        private VehicleLightsComponent lightsComponent;

        [SerializeField]
        private Transform speedingVehiclePos;
        public bool isSpeedingTrigger;
        public GameObject speedingVehicle;




        private static float CACC_StandstillDistance = 5f;

        [SerializeField, Tooltip("CACC spacing control gain (Shladover et al., 2012)")]
        private float CACC_K_spacing = 0.35f;

        [SerializeField, Tooltip("CACC speed control gain (Shladover et al., 2012)")]
        private float CACC_K_speed = 0.12f;

        [SerializeField, Tooltip("CACC max acceleration (m/s²) (Shladover et al., 2012)")]
        private float CACC_MaxAccel = 3.8f;

        [SerializeField, Tooltip("CACC max deceleration (m/s²) (Shladover et al., 2012)")]
        private float CACC_MaxDecel = 4.5f;

        // ===== CACC 参数（可在 Inspector 中调）=====
        [SerializeField, Tooltip("CACC 前馈系数 k3（前车加速度前馈）")]
        private float CACC_K_accFF = 0.8f;

        [SerializeField, Tooltip("线性时距策略的静止安全距离 d0（m）")]
        private float CACC_d0 = 5.0f;   // 也可复用 CACC_StandstillDistance

        [SerializeField, Tooltip("最小安全TTC（秒），小于该值不允许正加速")]
        private float CACC_MinTTC = 1.2f;

        [SerializeField, Tooltip("jerk 限制（m/s^3）")]
        private float CACC_JerkLimit = 6.0f;

        // ===== 加速度估计的滤波参数 =====
        [SerializeField, Tooltip("加速度一阶低通 EMA 系数 ∈(0,1]，越大越敏感")]
        private float AccelLPF_Alpha = 0.35f;

        // ===== 状态缓存 =====
        private float _vSelfPrev = 0f;
        private float _vFrontPrev = 0f;
        private float _aSelfFilt = 0f;
        private float _aFrontFilt = 0f;
        private float _aCmdPrev = 0f;



        [Header("MPC (Guo et al., 2020)")]
        [SerializeField, Tooltip("预测步数 p（论文常用1.5–2.0s，Δt=0.1s 则 15–20步）")]
        private int MPC_H = 20;

        [SerializeField, Tooltip("采样/预测步长 Ts（论文仿真常用 0.1s）")]
        private float MPC_Ts = 0.10f;

        // 变量时距 t_h(v, v_rel) = t1 + t2*(v/vmax) + t3*|v_rel|/vmax（论文式(3)）
        [SerializeField] private float VTH_t1 = 0.6f;
        [SerializeField] private float VTH_t2 = 0.4f;
        [SerializeField] private float VTH_t3 = 0.2f;
        [SerializeField] private float VTH_vMax = 36.11f; // 130 km/h

        // 物理/性能约束（论文式(16)–(21)）
        // u 为期望加速度（desired accel），Δu 代表每步变化 -> jerk 约束
        [SerializeField] private float MPC_uMin = -4.5f;
        [SerializeField] private float MPC_uMax = 3.0f;
        [SerializeField] private float MPC_duMin = -0.8f;   // ≈ j_min*Ts（若 j_min=-8 m/s^3 且 Ts=0.1 → -0.8）
        [SerializeField] private float MPC_duMax = 0.8f;   // ≈ j_max*Ts
        [SerializeField] private float MPC_vMin = 0.0f;
        [SerializeField] private float MPC_vMax = 60.0f;    // m/s（护栏上限）
        [SerializeField] private float MPC_aMin = -4.5f;
        [SerializeField] private float MPC_aMax = 3.0f;
        [SerializeField] private float MPC_jMin = -8.0f;    // m/s^3（舒适性）
        [SerializeField] private float MPC_jMax = 8.0f;
        [SerializeField, Tooltip("近端安全间距下界 d_c（论文式(20)）")]
        private float MPC_dClear = 5.0f;

        // 执行器一阶滞后（论文式(5)）
        [SerializeField] private float MPC_tau = 0.5f;

        // 目标函数权重（论文第4节：多目标+Δu平滑）
        [SerializeField] private float W_gap = 10.0f;   // 间距误差
        [SerializeField] private float W_relV = 2.0f;   // 相对速度
        [SerializeField] private float W_acc = 0.1f;   // 自车加速度
        [SerializeField] private float W_jerk = 0.1f;   // jerk
        [SerializeField] private float W_dU = 1.0f;   // Δu 平滑（式(23)）
        [SerializeField, Tooltip("软约束惩罚系数（约束软化，论文第4节）")]
        private float R_soft = 1e4f;


        [Header("Object References")]
        [Tooltip("RigidBody of the vehicle")]
        public Rigidbody rb;
        [Tooltip("Empty GameObject used to rotate the vehicle from the correct point")]
        public Transform carHolder;
        [Tooltip("Front trigger used to detect obstacle. It is automatically generated")]
        public Transform frontTrigger;
        [Tooltip("Assign this object if you need a hard shadow on your vehicle, leave it blank otherwise")]
        public Transform shadowHolder;
        [Tooltip("A transform representing the front of your vehicle")]
        public Transform _frontPosition;
        [Tooltip("A transform representing the back of your vehicle")]
        public Transform _backPosition;



        [Header("Wheels")]
        [Tooltip("All vehicle wheels and their properties")]
        public Internal.Wheel[] allWheels;
        [Tooltip("Max wheel turn amount in degrees")]
        public float maxSteer = 30;
        [Tooltip("If suspension is set to 0, the value of suspension will be half of the wheel radius")]
        public float maxSuspension = 0f;
        [Tooltip("How rigid the suspension will be. Higher the value -> more rigid the suspension")]
        public float springStiffness = 5;


        [Header("Car Properties")]
        [Tooltip("Vehicle type used for making custom paths")]
        public VehicleTypes vehicleType;
        [Tooltip("Min vehicle speed. Actual vehicle speed is picked random between min and max")]
        public float minPossibleSpeed;
        [Tooltip("Max vehicle speed")]
        public float maxPossibleSpeed;
        [Tooltip("Time in seconds to reach max speed (acceleration)")]
        public float accelerationTime = 10;
        [Tooltip("Time in seconds to stop from max speed")]
        public float brakeTime = 10;
        [Tooltip("Time in seconds to turn to maxSteer")]
        public float steeringTime = 1.5f;
        [Tooltip("Distance to keep from an obstacle/vehicle")]
        public float distanceToStop = 3;
        [Tooltip("Car starts braking when an obstacle enters trigger. Total length of the trigger = distanceToStop+minTriggerLength")]
        public float triggerLength = 4;

        [HideInInspector]
        public bool updateTrigger = false;
        [HideInInspector]
        public float maxTriggerLength = 10;
        [HideInInspector]
        public TrailerComponent trailer;
        [HideInInspector]
        public Transform trailerConnectionPoint;
        [HideInInspector]
        public float length = 0;
        [HideInInspector]
        public float coliderHeight = 0;
        [HideInInspector]
        public float wheelDistance;
        [HideInInspector]
        public VisibilityScript visibilityScript;

        private Collider[] _allColliders;
        private List<Obstacle> _obstacleList;
        private Transform _frontAxle;
        private BoxCollider _frontCollider;
        private ModifyTriggerSize _modifyTriggerSize;
        private EngineSoundComponent _engineSound;
        private BlinkersController _blinkersController;
        private MovementInfo _movementInfo;
        private LayerMask _buildingLayers;
        private LayerMask _obstacleLayers;
        private LayerMask _playerLayers;
        private LayerMask _roadLayers;
        private IVehicleLightsComponent _vehicleLights;
        private float _springForce;
        private float _maxSpeedMS;
        private float _storedMaxSpeed;
        private float _minTriggerLength;
        private float _colliderWidth;
        private float _powerStep;
        private float _brakeStep;
        private float _acceleration;
        private float _steerStep;
        private int _listIndex;
        private bool _lightsOn;
        private bool _ignored;

        public Collider[] AllColliders => _allColliders;
        public List<Obstacle> Obstacles => _obstacleList;
        public BlinkersController BlinkersController => _blinkersController;
        public Transform FrontTrigger => frontTrigger;
        public MovementInfo MovementInfo => _movementInfo;
        public VehicleTypes VehicleType => vehicleType;
        public float ColliderWidth => _colliderWidth;
        public float MaxSpeed => _maxSpeedMS;
        public float SpringForce => _springForce;
        public float MaxSteer => maxSteer;
        public float PowerStep => _powerStep;
        public float BrakeStep => _brakeStep;
        public float SteerStep => _steerStep;
        public float SpringStiffness => springStiffness;
        public int ListIndex => _listIndex;


        public Transform BackPosition
        {
            get
            {
                if (_backPosition == null)
                {
                    _backPosition = transform;
                }
                return _backPosition;
            }
        }

        public Transform FrontPosition
        {
            get
            {
                if (_frontPosition == null)
                {
                    _frontPosition = transform;
                }
                return _frontPosition;
            }
        }
        public bool Ignored
        {
            get { return _ignored; }
            set { _ignored = value; }
        }
        public bool HasTrailer
        {
            get
            {
                return trailer != null;
            }
        }


        private void Awake()
        {
            lightsComponent = GetComponent<VehicleLightsComponent>();
            if (vehicleType == VehicleTypes.SpeedingVehicle)
            {
                minPossibleSpeed = 150;
                maxPossibleSpeed = 150;
            }
            else
            {
                minPossibleSpeed = TrafficSettings.MinPossibleSpeed;
                maxPossibleSpeed = TrafficSettings.MaxPossibleSpeed;
            }

        }

        private void Update()
        {
            currentSpeedKmh = GetCurrentSpeedMS() * 3.6f; // m/s → km/h

            if (mergeFrontAssistTarget || mergeBehindAssistTarget)
            {
                lightsComponent.blinkerLeft.SetActive(true);
            }

        }






        /// <summary>
        /// Initialize vehicle
        /// </summary>
        /// <param name="buildingLayers">static colliders to interact with</param>
        /// <param name="obstacleLayers">dynamic colliders to interact with</param>
        /// <param name="playerLayers">player colliders to interact with</param>
        /// <returns>the vehicle</returns>
        public virtual VehicleComponent Initialize(LayerMask buildingLayers, LayerMask obstacleLayers, LayerMask playerLayers, LayerMask roadLayers, bool lightsOn, ModifyTriggerSize modifyTriggerSize, TrafficWaypointsData trafficWaypointsData, int vehicleIndex, bool ignored, float minOffset, float maxOffset)
        {
            _ignored = ignored;
            _buildingLayers = buildingLayers;
            _obstacleLayers = obstacleLayers;
            _playerLayers = playerLayers;
            _roadLayers = roadLayers;
            _modifyTriggerSize = modifyTriggerSize;
            _allColliders = GetComponentsInChildren<Collider>();
            _springForce = ((rb.mass * -Physics.gravity.y) / allWheels.Length);

            _frontCollider = frontTrigger.GetChild(0).GetComponent<BoxCollider>();
            _colliderWidth = _frontCollider.size.x;
            _minTriggerLength = _frontCollider.size.z;
            _frontAxle = new GameObject("FrontAxle").transform;
            _frontAxle.transform.SetParent(frontTrigger.parent);
            _frontAxle.transform.position = frontTrigger.position;
            DeactivateVehicle();

            //compute center of mass based on the wheel position
            Vector3 centerOfMass = Vector3.zero;
            for (int i = 0; i < allWheels.Length; i++)
            {
                allWheels[i].wheelTransform.Translate(Vector3.up * (allWheels[i].maxSuspension / 2 + allWheels[i].wheelRadius));
                centerOfMass += allWheels[i].wheelTransform.position;
            }
            rb.centerOfMass = centerOfMass / allWheels.Length;

            //set additional components
            _engineSound = GetComponent<EngineSoundComponent>();
            if (_engineSound)
            {
                _engineSound.Initialize();
            }

            _lightsOn = lightsOn;
            _vehicleLights = GetComponent<VehicleLightsComponent>();
            if (_vehicleLights == null)
            {
                _vehicleLights = GetComponent<VehicleLightsComponentV2>();
            }
            if (_vehicleLights != null)
            {
                _vehicleLights.Initialize();
            }

            if (trailer != null)
            {
                trailer.Initialize(this);
            }

            _listIndex = vehicleIndex;
            _movementInfo = new MovementInfo(_listIndex, ColliderWidth, Random.Range(minOffset, maxOffset));

            _blinkersController = new BlinkersController(_movementInfo, trafficWaypointsData, _vehicleLights);
            _steerStep = maxSteer / steeringTime * Time.fixedDeltaTime;

            return this;
        }


        /// <summary>
        /// Check for collisions
        /// </summary>
        /// <param name="collision"></param>
        protected virtual void OnCollisionEnter(Collision collision)
        {
            Events.TriggerVehicleCrashEvent(_listIndex, GetObstacleTypes(collision.collider), collision.collider);
        }


        /// <summary>
        /// Remove a collider from the list
        /// </summary>
        /// <param name="other"></param>
        protected virtual void OnTriggerExit(Collider other)
        {
            if (frontVehicle)
            {
                if (frontVehicle.GetComponent<PlayerComponent>() && vehicleType == VehicleTypes.SpeedingVehicle)
                {
                    frontVehicle.GetComponent<PlayerComponent>().isBehindSpeeding = false;
                }
            }
            if (!other.isTrigger)
            {
                if (other.gameObject.layer == gameObject.layer ||
                    (_buildingLayers == (_buildingLayers | (1 << other.gameObject.layer))) ||
                    (_obstacleLayers == (_obstacleLayers | (1 << other.gameObject.layer))) ||
                    (_playerLayers == (_playerLayers | (1 << other.gameObject.layer))))
                {
                    for (int i = 0; i < _obstacleList.Count; i++)
                    {
                        if (_obstacleList[i].Collider == other)
                        {
                            var obstacle = _obstacleList[i];
                            _obstacleList.RemoveAt(i);
                            VehicleEvents.TriggerObstacleInTriggerRemovedEvent(_listIndex, obstacle);

                            // 如果是前车，清空引用
                            if (frontVehicle != null && other == frontVehicle.GetComponent<Collider>())
                            {
                                frontVehicle = null;
                            }
                        }
                    }
                }
            }
        }



        private void UpdateFrontVehicle()
        {
            // 1. 如果有玩家汇入辅助对象，优先作为前车
            if (mergeFrontAssistTarget != null)
            {
                frontVehicle = mergeFrontAssistTarget;
                return;
            }

            // 2. 没有障碍物列表
            if (_obstacleList == null || _obstacleList.Count == 0)
            {
                frontVehicle = null;
                return;
            }

            float minDistance = float.MaxValue;
            MonoBehaviour nearest = null;

            // 3. 遍历所有障碍物，筛选TrafficVehicle或Player类型
            foreach (var obs in _obstacleList)
            {
                if ((obs.ObstacleType == ObstacleTypes.TrafficVehicle || obs.ObstacleType == ObstacleTypes.Player) &&
                    obs.VehicleScript is ITrafficParticipant tp &&
                    obs.VehicleScript is MonoBehaviour mb &&
                    tp != this)
                {
                    // 获取前车位置
                    Vector3 frontPos = (tp is VehicleComponent vc) ? vc.BackPosition.position : mb.transform.position;

                    float dist = Vector3.Distance(FrontPosition.position, frontPos);
                    Vector3 dirToVehicle = (frontPos - FrontPosition.position).normalized;

                    // 只考虑在前方的车辆
                    if (Vector3.Dot(transform.forward, dirToVehicle) > 0.5f && dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = mb;
                    }
                }
            }

            // 4. 如果当前 frontVehicle 依然在CACC期望车距内，则不替换
            if (frontVehicle != null)
            {
                Vector3 currentFrontPos = (frontVehicle is VehicleComponent vc) ? vc.BackPosition.position : frontVehicle.transform.position;

                float distToCurrent = Vector3.Distance(FrontPosition.position, currentFrontPos);

                // CACC期望车距 = v_self * h + d0
                float desiredGap = GetCurrentSpeedMS() * TrafficSettings.CACC_TimeHeadway + 5f; // 5m静止安全距离

                if (distToCurrent > 0 && distToCurrent <= desiredGap * 1.3f)
                {
                    return; // 当前前车依然合适
                }
            }

            // 5. 更新前车
            frontVehicle = nearest;
        }


        private void SyncMaxSpeedWithFront_CACC()
        {
            if (frontVehicle == null || !(frontVehicle is ITrafficParticipant frontP))
            {
                MovementInfo.SetMaxSpeedCorrectionPercent(1f);
                _aCmdPrev = 0f;
                return;
            }

            // 运行时步长：与物理步一致
            float dtRun = (Time.fixedDeltaTime > 0f) ? Time.fixedDeltaTime : Time.deltaTime;
            if (dtRun <= 1e-5f) dtRun = 1f / 60f;

            // 当前量测
            float v_self = GetCurrentSpeedMS();
            float v_front = frontP.GetCurrentSpeedMS();

            Vector3 frontPos = (frontVehicle is VehicleComponent vc)
                ? vc.BackPosition.position : frontVehicle.transform.position;
            float s_real = Vector3.Distance(FrontPosition.position, frontPos); // Δx_real
            float v_rel = v_self - v_front; // 自车相对前车正向速度（>0 表示逼近）

            // 前车加速度估计（论文模型第3节：a_p 作为扰动）
            _aFrontFilt = EstimateAccelFiltered(v_front, ref _vFrontPrev, _aFrontFilt, dtRun, AccelLPF_Alpha);
            _aSelfFilt = EstimateAccelFiltered(v_self, ref _vSelfPrev, _aSelfFilt, dtRun, AccelLPF_Alpha);

            float aCmd;

            if (TrafficSettings.CACC)
            {
                // —— 你的 CACC 分支（保持原样） ——
                float h = TrafficSettings.CACC_TimeHeadway;
                float d0 = CACC_d0;

                float sStar = d0 + h * v_self;
                float e_s = s_real - sStar;
                float e_v = v_front - v_self;

                aCmd = CACC_K_spacing * e_s + CACC_K_speed * e_v + CACC_K_accFF * _aFrontFilt;

                float maxDa = CACC_JerkLimit * dtRun;
                aCmd = Mathf.Clamp(aCmd, _aCmdPrev - maxDa, _aCmdPrev + maxDa);
                aCmd = Mathf.Clamp(aCmd, -CACC_MaxDecel, CACC_MaxAccel);

                float relV = v_self - v_front;
                if (relV > 0.05f)
                {
                    float ttc = s_real / (relV + 1e-3f);
                    if (ttc < CACC_MinTTC) aCmd = Mathf.Min(aCmd, 0f);
                }
            }
            else // ======== MPC_Guo2020 分支 ========
            {
                aCmd = ComputeMPC_Guo2020(
                    s_real, v_self, v_rel,
                    _aSelfFilt, _aFrontFilt,
                    _aCmdPrev
                );
            }

            // 输出到你现有速度修正通道
            float v_target = Mathf.Max(0f, v_self + aCmd * dtRun);
            float v_max_ms = _storedMaxSpeed.KMHToMS();
            float percent = (v_max_ms > 0.1f) ? (v_target / v_max_ms) : 1f;
            percent = Mathf.Clamp(percent, 0.0f, 1.2f);
            MovementInfo.SetMaxSpeedCorrectionPercent(percent);

            _aCmdPrev = aCmd;
        }



        private float EstimateAccelFiltered(float vNow, ref float vPrev, float aPrevFilt, float dt, float alpha)
        {
            float aRaw = (vNow - vPrev) / Mathf.Max(1e-3f, dt);
            vPrev = vNow;
            // EMA: a_filt = (1-alpha)*a_prev + alpha*a_raw
            float aFilt = (1f - alpha) * aPrevFilt + alpha * aRaw;
            return aFilt;
        }



        private float ComputeMPC_Guo2020(
    float s_real,       // 当前位置的实际间距 Δx_real
    float v_self,       // 自车速度
    float v_rel,        // 自车-前车 相对速度 (v_self - v_front)
    float a_self_est,   // 自车加速度估计
    float a_front_est,  // 前车加速度估计（扰动）
    float u_prev        // 上一步期望加速度（用于 Δu）
)
        {
            int H = Mathf.Max(5, MPC_H);
            float Ts = Mathf.Max(1e-3f, MPC_Ts);

            // 初始状态（论文式(11)的输出量 y 包含 δ, v_rel, a, j）
            // 这里用状态向前模拟；jerk 由 u(k)-u(k-1) 与执行器滞后近似得到
            float s = s_real;
            float vs = v_self;
            float vf = v_self - v_rel;     // 前车速度
            float a = a_self_est;
            float ap = a_front_est;
            float u0 = Mathf.Clamp(u_prev, MPC_uMin, MPC_uMax); // 初值

            // —— PGD 初始化 —— （求最小化 J(u) s.t. 盒约束 与 Δu 约束）
            float u = u0;
            float lr = 0.5f;  // 学习率（可调小一点更稳）
            int iters = 30;

            // 预先把 jerk 和 Δu 的界换算成 Δu 的界（论文式(17)(18)）
            float dUmin = Mathf.Max(MPC_duMin, MPC_jMin * Ts);
            float dUmax = Mathf.Min(MPC_duMax, MPC_jMax * Ts);

            for (int it = 0; it < iters; it++)
            {
                // 数值梯度（单变量 move-blocking，u 在[ k..k+H-1 ]保持常值）
                float eps = 1e-3f;
                float J0 = RolloutCost(u, false);
                float J1 = RolloutCost(u + eps, false);
                float g = (J1 - J0) / eps;

                // 梯度一步
                float u_new = u - lr * g;

                // 施加 Δu 约束（对首步相对 u_prev），再投影到 [uMin, uMax]
                float du = Mathf.Clamp(u_new - u_prev, dUmin, dUmax);
                u_new = u_prev + du;
                u_new = Mathf.Clamp(u_new, MPC_uMin, MPC_uMax);

                // 若代价没有下降，缩小步长
                float Jnew = RolloutCost(u_new, false);
                if (Jnew > J0) { lr *= 0.5f; } else { u = u_new; }
            }

            // 返回首步控制
            return Mathf.Clamp(u, MPC_aMin, MPC_aMax);

            // ===== 内部：前向预测并计算代价（严格按论文项构造） =====
            float RolloutCost(float uConst, bool record)
            {
                // 复制一份状态
                float s_k = s;
                float vs_k = vs;
                float vf_k = vf;
                float a_k = a;
                float u_km1 = u_prev;
                float cost = 0f;

                for (int k = 0; k < H; k++)
                {
                    // 变量时距（论文式(3)）：t_h = t1 + t2*(v/vmax) + t3*|v_rel|/vmax
                    float vrel_k = vs_k - vf_k;
                    float th = VTH_t1 + VTH_t2 * (vs_k / Mathf.Max(1e-3f, VTH_vMax))
                                         + VTH_t3 * (Mathf.Abs(vrel_k) / Mathf.Max(1e-3f, VTH_vMax));
                    // 目标间距 δ_target = d0 + t_h * v
                    float delta_target = MPC_dClear + th * vs_k;
                    float delta_err = s_k - delta_target; // 论文式(1)(2)

                    // jerk & Δu：以 desired accel uConst 经执行器一阶滞后（论文式(5)）
                    // a_{k+1} = a_k + (u - a_k)*(Ts/tau)
                    float a_next = a_k + (uConst - a_k) * (Ts / Mathf.Max(1e-3f, MPC_tau));
                    float jerk = (a_next - a_k) / Ts;

                    // 车辆学：前向（论文式(6)），速度与间距
                    float vs_next = Mathf.Clamp(vs_k + a_k * Ts, MPC_vMin, MPC_vMax);
                    float vf_next = Mathf.Clamp(vf_k + ap * Ts, MPC_vMin, MPC_vMax);
                    float s_next = Mathf.Max(0f,
                        s_k + (vf_k - vs_k) * Ts + 0.5f * ((ap - a_k) * Ts * Ts));

                    // —— 目标函数（论文第4节+式(23)）——
                    // J = Σ [ W_gap*δ^2 + W_relV*(v_rel)^2 + W_acc*a^2 + W_jerk*j^2 + W_dU*(u-u_prev)^2 ]
                    float dU = (uConst - u_prev);
                    cost += W_gap * (delta_err * delta_err)
                          + W_relV * (vrel_k * vrel_k)
                          + W_acc * (a_k * a_k)
                          + W_jerk * (jerk * jerk)
                          + W_dU * (dU * dU);

                    // —— 软约束惩罚（论文“constraints softening”）——
                    // 1) 间距下界 s >= d_c ；若违反，按二次罚（可视作松弛变量的二次代价）
                    if (s_k < MPC_dClear)
                    {
                        float slack = (MPC_dClear - s_k);
                        cost += R_soft * (slack * slack);
                    }
                    // 2) 速度/加速度/jerk 的软约束（式(20)(21)→(22)）
                    if (vs_k < MPC_vMin) { float q = MPC_vMin - vs_k; cost += R_soft * q * q; }
                    if (vs_k > MPC_vMax) { float q = vs_k - MPC_vMax; cost += R_soft * q * q; }
                    if (a_k < MPC_aMin) { float q = MPC_aMin - a_k; cost += R_soft * q * q; }
                    if (a_k > MPC_aMax) { float q = a_k - MPC_aMax; cost += R_soft * q * q; }
                    if (jerk < MPC_jMin) { float q = MPC_jMin - jerk; cost += R_soft * q * q; }
                    if (jerk > MPC_jMax) { float q = jerk - MPC_jMax; cost += R_soft * q * q; }

                    // 推进
                    s_k = s_next;
                    vs_k = vs_next;
                    vf_k = vf_next;
                    a_k = a_next;
                }
                return cost;
            }
        }




        public void SetFrontMergeAssistTarget(MonoBehaviour playerCar)
        {
            mergeFrontAssistTarget = playerCar;
        }

        public void RemoveFrontMergeAssistTarget()
        {
            mergeFrontAssistTarget = null;
        }

        public void SetBehindMergeAssistTarget(MonoBehaviour playerCar)
        {
            mergeBehindAssistTarget = playerCar;
        }

        public void RemoveBehindMergeAssistTarget()
        {
            mergeBehindAssistTarget = null;
        }















        /// <summary>
        /// CHeck trigger objects
        /// </summary>
        /// <param name="other"></param>
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!other.isTrigger)
            {
                NewColliderHit(other);
            }

            if (frontVehicle)
            {
                if (frontVehicle.GetComponent<PlayerComponent>() && vehicleType == VehicleTypes.SpeedingVehicle)
                {
                    frontVehicle.GetComponent<PlayerComponent>().isBehindSpeeding = true;
                }

                if (frontVehicle.GetComponent<PlayerComponent>() && isSpeedingTrigger)
                {

                    speedingVehicle = GameObject.FindGameObjectWithTag("Speeding");
                    speedingVehicle.transform.position = 
                        new Vector3(speedingVehiclePos.position.x, speedingVehicle.transform.position.y, speedingVehiclePos.position.z);
                    speedingVehicle = null;

                    Debug.Log("SPEEDING REPOSITION");
                }
            }
        }


        public virtual void ApplyAdditionalForces(float wheelTurnAngle)
        {

        }

        public void SetVelocity(Vector3 initialVelocity, Vector3 angularVelocity)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = initialVelocity;
#else
            rb.velocity = initialVelocity;
#endif
            rb.angularVelocity = angularVelocity;
        }


        /// <summary>
        /// Apply new trigger size delegate
        /// </summary>
        /// <param name="triggerSizeModifier"></param>
        public void SetTriggerSizeModifierDelegate(ModifyTriggerSize triggerSizeModifier)
        {
            _modifyTriggerSize = triggerSizeModifier;
        }


        /// <summary>
        /// Add a vehicle on scene
        /// </summary>
        /// <param name="position"></param>
        /// <param name="vehicleRotation"></param>
        /// <param name="masterVolume"></param>
        public virtual void ActivateVehicle(Vector3 position, Quaternion vehicleRotation, Quaternion trailerRotation)
        {
            _storedMaxSpeed = Random.Range(minPossibleSpeed, maxPossibleSpeed);

            _maxSpeedMS = _storedMaxSpeed.KMHToMS();

            int nrOfFrames = (int)(accelerationTime / Time.fixedDeltaTime);
            _powerStep = MaxSpeed / nrOfFrames;

            _acceleration = _powerStep / Time.fixedDeltaTime;

            nrOfFrames = (int)(brakeTime / Time.fixedDeltaTime);
            _brakeStep = MaxSpeed / nrOfFrames;

            gameObject.transform.SetPositionAndRotation(position, vehicleRotation);

            //position vehicle with front wheels on the waypoint
            float distance = Vector3.Distance(position, frontTrigger.transform.position);
            transform.Translate(-transform.forward * distance, Space.World);

            if (trailer != null)
            {
                trailer.transform.rotation = trailerRotation;
            }

            gameObject.SetActive(true);


            if (_engineSound)
            {
                _engineSound.Play(0);
            }

            SetMainLights(_lightsOn);
        }


        /// <summary>
        /// Remove a vehicle from scene
        /// </summary>
        public virtual void DeactivateVehicle()
        {
            gameObject.SetActive(false);
            _obstacleList = new List<Obstacle>();
            visibilityScript.Reset();

            if (_engineSound)
            {
                _engineSound.Stop();
            }

            _vehicleLights?.DeactivateLights();

            if (trailer)
            {
                trailer.DeactivateVehicle();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>Max RayCast length</returns>
        public float GetRayCastLength()
        {
            return allWheels[0].raycastLength;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>Wheel circumference</returns>
        public float GetWheelCircumference()
        {
            return allWheels[0].wheelCircumference;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>Vehicle velocity vector</returns>
        public Vector3 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }


        /// <summary>
        /// Returns current speed in m/s
        /// </summary>
        /// <returns></returns>
        public float GetCurrentSpeedMS()
        {
            return GetVelocity().magnitude;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>Trigger orientation</returns>
        public Vector3 GetHeading()
        {
            return frontTrigger.forward;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>vehicle orientation</returns>
        public Vector3 GetForwardVector()
        {
            return transform.forward;
        }


        /// <summary>
        /// Check if the vehicle is not in view
        /// </summary>
        /// <returns></returns>
        public bool CanBeRemoved()
        {
            return visibilityScript.IsNotInView();
        }


        public Vector3 GetFrontAxleUpVector()
        {
            return _frontAxle.up;
        }


        public Vector3 GetFrontAxleForwardVector()
        {
            return _frontAxle.forward;
        }


        public Vector3 GetFrontAxleRightVector()
        {
            return _frontAxle.right;
        }


        public Vector3 GetFrontAxlePosition()
        {
            return _frontAxle.position;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>number of vehicle wheels</returns>
        public int GetNrOfWheels()
        {
            return allWheels.Length;
        }


        /// <summary>
        /// Returns the nr of wheels of the trailer
        /// </summary>
        /// <returns></returns>
        public int GetTrailerWheels()
        {
            if (trailer == null)
            {
                return 0;
            }
            return trailer.GetNrOfWheels();
        }


        /// <summary>
        /// Check if current collider is from a new object
        /// </summary>
        /// <param name="colliders"></param>
        /// <returns></returns>
        public bool AlreadyCollidingWith(Collider[] colliders)
        {
            for (int i = 0; i < _obstacleList.Count; i++)
            {
                for (int j = 0; j < colliders.Length; j++)
                {
                    if (_obstacleList[i].Collider == colliders[j])
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Remove a collider from the trigger if the collider was destroyed
        /// </summary>
        /// <param name="collider"></param>
        public void ColliderRemoved(Collider collider)
        {
            if (_obstacleList != null)
            {
                if (_obstacleList.Any(cond => cond.Collider == collider))
                {
                    OnTriggerExit(collider);
                }
            }
        }


        /// <summary>
        /// Removed a list of colliders from the trigger if the colliders ware destroyed
        /// </summary>
        /// <param name="colliders"></param>
        public void ColliderRemoved(Collider[] colliders)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (_obstacleList.Any(cond => cond.Collider == colliders[i]))
                {
                    OnTriggerExit(colliders[i]);
                }
            }
        }


        //update the lights component if required
        #region Lights
        internal void SetMainLights(bool on)
        {
            if (on != _lightsOn)
            {
                _lightsOn = on;
            }
            if (_vehicleLights != null)
            {
                _vehicleLights.SetMainLights(on);
            }
        }


        public void SetReverseLights(bool active)
        {
            if (_vehicleLights != null)
            {
                _vehicleLights.SetReverseLights(active);
            }
        }


        public void SetBrakeLights(bool active)
        {
            if (_vehicleLights != null)
            {
                _vehicleLights.SetBrakeLights(active);
            }
        }


        public virtual void SetBlinker(BlinkType blinkType)
        {
            if (_vehicleLights != null)
            {
                _vehicleLights.SetBlinker(blinkType);
            }
        }


        public void UpdateLights(float realtimeSinceStartup)
        {
            if (_vehicleLights != null)
            {
                _vehicleLights.UpdateLights(realtimeSinceStartup);
            }
        }
        #endregion


        //update the sound component if required
        #region Sound
        public void UpdateEngineSound(float masterVolume)
        {
            if (_engineSound)
            {
                _engineSound.UpdateEngineSound(GetCurrentSpeedMS(), MaxSpeed, masterVolume);
            }
        }
        #endregion


        /// <summary>
        /// Modify the dimension of the front trigger
        /// </summary>
        public void UpdateColliderSize()
        {
            if (updateTrigger)
            {
                _modifyTriggerSize?.Invoke(GetVelocity().magnitude * 3.6f, _frontCollider, _storedMaxSpeed, _minTriggerLength, maxTriggerLength);
            }
        }


        public virtual void UpdateVehicleScripts(float volume, float realTimeSinceStartup, bool reverseLightsOn)
        {
            UpdateEngineSound(volume);
            UpdateLights(realTimeSinceStartup);
            UpdateColliderSize();
            SetReverseLights(reverseLightsOn);


            // 调用跟车控制
            UpdateFrontVehicle();  // 新增：稳定前车识别
            SyncMaxSpeedWithFront_CACC(); // <== 新增
        }


        public float GetTimeToCoverDistance(float distance)
        {
            // Calculate time and distance to reach max speed
            float timeToMaxSpeed = (MaxSpeed - GetCurrentSpeedMS()) / _acceleration;
            float distanceToMaxSpeed = (GetCurrentSpeedMS() * timeToMaxSpeed) + (0.5f * _acceleration * timeToMaxSpeed * timeToMaxSpeed);

            if (distanceToMaxSpeed >= distance)
            {
                // The vehicle can reach the target before hitting max speed
                return (-GetCurrentSpeedMS() + Mathf.Sqrt(GetCurrentSpeedMS() * GetCurrentSpeedMS() + 2 * _acceleration * distance)) / _acceleration;
            }
            else
            {
                // The vehicle hits max speed, calculate remaining distance
                float remainingDistance = distance - distanceToMaxSpeed;
                float timeAtMaxSpeed = remainingDistance / MaxSpeed;

                return timeToMaxSpeed + timeAtMaxSpeed;
            }
        }


        /// <summary>
        /// Every time a new collider is hit it is added inside the list
        /// </summary>
        /// <param name="other"></param>
        private void NewColliderHit(Collider other)
        {
            ObstacleTypes obstacleType = GetObstacleTypes(other);

            if (obstacleType != ObstacleTypes.Other && obstacleType != ObstacleTypes.Road)
            {
                if (!_obstacleList.Any(cond => cond.Collider == other))
                {
                    bool isConvex = true;
                    if (other is MeshCollider meshCollider)
                    {
                        isConvex = meshCollider.convex;
                    }

                    ITrafficParticipant component = null;
                    if (obstacleType == ObstacleTypes.TrafficVehicle || obstacleType == ObstacleTypes.Player)
                    {
                        Rigidbody otherRb = other.attachedRigidbody;
                        if (otherRb != null)
                        {
                            component = otherRb.GetComponent<ITrafficParticipant>();
                            // 如果是前车则保存引用
                            if (component is VehicleComponent vc)
                            {
                                //frontVehicle = vc;
                            }
                        }
                    }

                    _obstacleList.Add(new Obstacle(other, isConvex, obstacleType, component));
                    VehicleEvents.TriggerObstacleInTriggerAddedEvent(_listIndex, _obstacleList[_obstacleList.Count - 1]);
                }
            }
        }



        /// <summary>
        /// Returns the type of obstacle that just entered the front trigger
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private ObstacleTypes GetObstacleTypes(Collider other)
        {
            bool carHit = other.gameObject.layer == gameObject.layer;
            //possible vehicle hit
            if (carHit)
            {
                Rigidbody otherRb = other.attachedRigidbody;
                if (otherRb != null)
                {
                    if (otherRb.GetComponent<ITrafficParticipant>() != null)
                    {
                        return ObstacleTypes.TrafficVehicle;
                    }
                }
                //if it is on traffic layer but it lacks a vehicle component, it is a dynamic object
                return ObstacleTypes.DynamicObject;
            }
            else
            {
                //trigger the corresponding event based on object layer
                if (_buildingLayers == (_buildingLayers | (1 << other.gameObject.layer)))
                {
                    return ObstacleTypes.StaticObject;
                }
                else
                {
                    if (_obstacleLayers == (_obstacleLayers | (1 << other.gameObject.layer)))
                    {
                        return ObstacleTypes.DynamicObject;
                    }
                    else
                    {
                        if (_playerLayers == (_playerLayers | (1 << other.gameObject.layer)))
                        {
                            return ObstacleTypes.Player;
                        }
                        else
                        {
                            if (_roadLayers == (_roadLayers | (1 << other.gameObject.layer)))
                            {
                                return ObstacleTypes.Road;
                            }
                        }
                    }
                }
            }
            return ObstacleTypes.Other;
        }



#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // 当前速度 (km/h)
            float speedKmh = GetCurrentSpeedMS() * 3.6f;

            // 与前车的距离
            float distToFront = -1f;
            if (frontVehicle != null)
            {
                Vector3 frontPos;
                if (frontVehicle is VehicleComponent vc)
                {
                    frontPos = vc.BackPosition.position;
                }
                else
                {
                    frontPos = frontVehicle.transform.position; // 玩家车用位置
                }
                distToFront = Vector3.Distance(FrontPosition.position, frontPos);
            }

            // 在 Scene 视图上方绘制文字
            UnityEditor.Handles.color = Color.white;
            string text = $"Speed: {speedKmh:F1} km/h\nDist: {(distToFront >= 0 ? distToFront.ToString("F1") + " m" : "No Front")}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, text);
        }
#endif


    }
}