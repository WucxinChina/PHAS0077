//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// AI driver for RCCP vehicles that relies on Unity NavMesh.
/// Behaviours
///     • FollowWaypoints – loops through a waypoint list.
///     • RaceWaypoints   – same list but corners aggressively.
///     • FollowTarget    – tails a transform at fixed distance.
///     • ChaseTarget     – intercepts a transform.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/AI/RCCP AI")]
public class RCCP_AI : RCCP_Component {

    private RCCP_AIDynamicObstacleAvoidance obstacleAvoidance;

    //──────────────────────────────────────────────────────────────────────────────
    //  Behaviour / public fields
    //──────────────────────────────────────────────────────────────────────────────

    [Tooltip("Select the AI behavior mode: FollowWaypoints, RaceWaypoints, FollowTarget, or ChaseTarget.")]
    public BehaviourType behaviour = BehaviourType.RaceWaypoints;
    private BehaviourType oldBehavior = BehaviourType.RaceWaypoints;

    [Tooltip("Container holding the list of waypoints for waypoint-based behaviors.")]
    public RCCP_AIWaypointsContainer waypointsContainer;

    [Tooltip("If using FollowTarget or ChaseTarget, the Transform to follow or chase.")]
    public Transform target;

    /// <summary>Distance (m) to mark a waypoint reached.</summary>
    [Tooltip("Distance in meters at which a waypoint is considered reached.")]
    public float waypointReachThreshold = 25f;

    /// <summary>Extra look-ahead distance (m) for Race behaviour.</summary>
    [Tooltip("Additional look-ahead distance in meters when racing between waypoints.")]
    public float raceLookAhead = 36f;

    /// <summary>Tyre/road friction μ used to compute safe corner speed.</summary>
    [Tooltip("Friction coefficient (μ) between tyre and road; governs safe cornering speed.")]
    public float roadGrip = 1.1f;      // asphalt ≈ 1.0-1.3

    /// <summary>Max throttle (0-1).</summary>
    [Tooltip("Maximum throttle input (0 to 1).")]
    [Range(0f, 1f)] public float maxThrottle = 1f;

    /// <summary>Max brake (0-1).</summary>
    [Tooltip("Maximum brake input (0 to 1).")]
    [Range(0f, 1f)] public float maxBrake = 1f;

    /// <summary>Max brake (0-1).</summary>
    [Tooltip("Agressive driving factor.")]
    [Range(0f, 3f)] public float agressiveness = 2f;

    /// <summary>Multiplies steering input; raise for quicker response.</summary>
    [Tooltip("Steering sensitivity multiplier; increase for sharper steering response.")]
    [Range(0f, 5f)] public float steerSensitivity = 3f;

    //──────────────────────────────────────────────────────────────────────────────
    //  Steering look-ahead tunables  (Pure-Pursuit)
    //──────────────────────────────────────────────────────────────────────────────

    [Tooltip("Minimum look-ahead distance in meters when stationary.")]
    public float minLookAhead = 5f;         // m  at 0 km/h

    [Tooltip("Additional look-ahead per km/h of speed.")]
    public float lookAheadPerKph = .25f;    // m per km/h

    //──────────────────────────────────────────────────────────────────────────────
    //  Speed PID (longitudinal) tunables
    //──────────────────────────────────────────────────────────────────────────────

    [Tooltip("Proportional gain for throttle PID control.")]
    public float kp = .2f;     // proportional gain  (throttle)

    [Tooltip("Integral gain for throttle PID control (usually zero).")]
    public float ki = .01f;     // integral gain      (optional – leave 0)

    [Tooltip("Derivative gain for throttle PID control (damps overshoot).")]
    public float kd = .02f;     // derivative gain    (damps overshoot)

    //──────────────────────────────────────────────────────────────────────────────
    //  Feed-forward brake factor
    //──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// How much “extra” feed-forward brake to apply [0…1]
    /// based purely on distance vs. braking capability.
    /// </summary>
    [Tooltip("Feed-forward braking factor to apply extra brake when overspeeding.")]
    private float brakeFeedForwardFactor = .25f;

    /// <summary>Distance (m) to maintain behind the target in FollowTarget mode.</summary>
    [Tooltip("Distance (m) to maintain behind the target in FollowTarget mode.")]
    public float followTargetDistance = 5f;

    /// <summary>Prediction time (s) when computing intercept point in ChaseTarget mode.</summary>
    [Tooltip("Prediction time (seconds) for intercepting moving targets.")]
    public float chasePredictionTime = 1f;

    public bool stopNow = false;
    public bool reverseNow = false;
    public bool checkStuck = true;

    //──────────────────────────────────────────────────────────────────────────────
    //  Private state (not shown in inspector)
    //──────────────────────────────────────────────────────────────────────────────

    private NavMeshAgent Agent {
        get {
            if (agent == null)
                agent = GetComponentInChildren<NavMeshAgent>(true);
            if (agent) {
                agent.gameObject.SetActive(true);
            }
            if (agent == null) {
                agent = new GameObject("Agent").AddComponent<NavMeshAgent>();
                agent.transform.SetParent(transform);
                agent.transform.localPosition = Vector3.zero;
                agent.transform.localRotation = Quaternion.identity;
                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.radius = 1.2f;
                agent.height = 3f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.speed = 60f;
                agent.acceleration = 40f;
                agent.angularSpeed = 720f;
            }
            return agent;
        }
    }
    private NavMeshAgent agent;
    public int currentWaypointIndex;
    private float stuckTimer;
    private float pidIntegral;
    private float lastSpeedError;

    public enum BehaviourType {
        FollowWaypoints,
        RaceWaypoints,
        FollowTarget,
        ChaseTarget
    }

    public RCCP_Inputs inputs = new RCCP_Inputs();

    private float[] defaultSteerSpeedOfAxle;
    private bool[] defaultInputStates;

    //──────────────────────────────────────────────────────────────────────────────
    //  Unity methods
    //──────────────────────────────────────────────────────────────────────────────

    public override void Start() {

        base.Start();

        // Agent only supplies path points – disable built-in motion.
        Agent.updatePosition = false;
        Agent.updateRotation = false;

        Agent.radius = 1.2f;
        Agent.height = 3f;
        Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        Agent.speed = 60f;
        Agent.acceleration = 40f;
        Agent.angularSpeed = 720f;

    }

    private int GetClosestWaypoint() {

        if (waypointsContainer == null)
            return 0;

        if (waypointsContainer.waypoints == null)
            return 0;

        if (waypointsContainer.waypoints.Count < 1)
            return 0;

        int closestAll = 0;
        float closestAllDistance = float.MaxValue;
        int closestFront = -1;
        float closestFrontDistance = float.MaxValue;

        Vector3 carPos = CarController.transform.position;
        Vector3 carFwd = CarController.transform.forward;

        // Loop through every waypoint
        for (int i = 0; i < waypointsContainer.waypoints.Count; i++) {

            var wp = waypointsContainer.waypoints[i];
            if (wp == null)
                continue;

            Vector3 wpPos = wp.transform.position;
            float dist = Vector3.Distance(wpPos, carPos);

            // Track the closest overall
            if (dist < closestAllDistance) {
                closestAllDistance = dist;
                closestAll = i;
            }

            // Check if it's in front: dot > 0 means angle < 90°
            Vector3 toWp = wpPos - carPos;
            if (Vector3.Dot(carFwd, toWp) > 0f) {
                if (dist < closestFrontDistance) {
                    closestFrontDistance = dist;
                    closestFront = i;
                }
            }
        }

        // If we found any in front, use that; otherwise use the overall closest
        return (closestFront != -1) ? closestFront : closestAll;

    }


    public override void OnEnable() {

        base.OnEnable();

        oldBehavior = behaviour;

        BehaviorChanged();

        CarController.externalControl = true;

#if !UNITY_2022_1_OR_NEWER
        if (!waypointsContainer)
            waypointsContainer = FindObjectOfType<RCCP_AIWaypointsContainer>();
#else
        if (!waypointsContainer)
            waypointsContainer = FindFirstObjectByType<RCCP_AIWaypointsContainer>(FindObjectsInactive.Include);
#endif

        defaultSteerSpeedOfAxle = new float[CarController.AxleManager.Axles.Count];
        defaultInputStates = new bool[4];

        for (int i = 0; i < CarController.AxleManager.Axles.Count; i++) {

            if (CarController.AxleManager.Axles[i] != null) {

                defaultSteerSpeedOfAxle[i] = CarController.AxleManager.Axles[i].steerSpeed;
                CarController.AxleManager.Axles[i].steerSpeed = 10f;

            }

        }

        defaultInputStates[0] = CarController.Inputs.autoReverse;
        defaultInputStates[1] = CarController.Inputs.inverseThrottleBrakeOnReverse;
        defaultInputStates[2] = CarController.Inputs.counterSteering;
        defaultInputStates[3] = CarController.Inputs.steeringLimiter;

        CarController.Inputs.autoReverse = false;
        CarController.Inputs.inverseThrottleBrakeOnReverse = true;
        CarController.Inputs.counterSteering = false;
        CarController.Inputs.steeringLimiter = false;

    }

    public override void OnDisable() {

        base.OnDisable();

        CarController.externalControl = false;

        for (int i = 0; i < defaultSteerSpeedOfAxle.Length; i++) {

            if (CarController.AxleManager.Axles[i] != null)
                CarController.AxleManager.Axles[i].steerSpeed = defaultSteerSpeedOfAxle[i];

        }

        CarController.Inputs.autoReverse = defaultInputStates[0];
        CarController.Inputs.inverseThrottleBrakeOnReverse = defaultInputStates[1];
        CarController.Inputs.counterSteering = defaultInputStates[2];
        CarController.Inputs.steeringLimiter = defaultInputStates[3];

    }

    private void FixedUpdate() {

        if (!Agent)
            return;

        if (oldBehavior != behaviour)
            BehaviorChanged();

        oldBehavior = behaviour;

        UpdateDestination();
        ComputeControls();
        HandleStuckVehicle();
        CheckOverridedInputs();

        CarController.Inputs.OverrideInputs(inputs);

    }

    private void BehaviorChanged() {

        stopNow = false;
        reverseNow = false;

        if (behaviour == BehaviourType.FollowWaypoints || behaviour == BehaviourType.RaceWaypoints)
            currentWaypointIndex = GetClosestWaypoint();

    }

    //──────────────────────────────────────────────────────────────────────────────
    //  Destination selection
    //──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Picks the next target point for the NavMeshAgent based on the chosen behaviour.
    /// Skips any waypoints you’ve already passed and does a pure waypoint-based look-ahead for RaceWaypoints.
    /// </summary>
    private void UpdateDestination() {

        switch (behaviour) {

            case BehaviourType.FollowWaypoints: {

                    // nothing to do if there are no waypoints
                    if (waypointsContainer.waypoints.Count == 0)
                        return;

                    // 1) Skip *all* waypoints within reach
                    float threshSqr = waypointReachThreshold * waypointReachThreshold;
                    int count = waypointsContainer.waypoints.Count;
                    while ((CarController.transform.position - waypointsContainer.waypoints[currentWaypointIndex].transform.position).sqrMagnitude < threshSqr) {
                        currentWaypointIndex = (currentWaypointIndex + 1) % count;
                    }

                    // 2) Send agent to the next not-yet-reached waypoint
                    Agent.SetDestination(waypointsContainer.waypoints[currentWaypointIndex].transform.position);

                    break;
                }

            case BehaviourType.RaceWaypoints: {

                    if (waypointsContainer.waypoints.Count == 0)
                        return;

                    // 1) Skip any waypoints we’re already inside the threshold of
                    float threshSqr = waypointReachThreshold * waypointReachThreshold;
                    int count = waypointsContainer.waypoints.Count;
                    while ((CarController.transform.position - waypointsContainer.waypoints[currentWaypointIndex].transform.position).sqrMagnitude < threshSqr) {
                        currentWaypointIndex = (currentWaypointIndex + 1) % count;
                    }

                    // 2) Compute a waypoint-based look-ahead point at “raceLookAhead” metres
                    float travelled = 0f;
                    int lookIndex = currentWaypointIndex;
                    Vector3 lastPos = transform.position;
                    Vector3 lookPoint = lastPos;

                    while (travelled < raceLookAhead) {
                        Vector3 nextPos = waypointsContainer.waypoints[lookIndex].transform.position;
                        float segment = Vector3.Distance(lastPos, nextPos);

                        if (travelled + segment >= raceLookAhead) {
                            // interpolate along this segment to hit exactly raceLookAhead metres
                            float t = (raceLookAhead - travelled) / segment;
                            lookPoint = Vector3.Lerp(lastPos, nextPos, t);
                            break;
                        }

                        travelled += segment;
                        lastPos = nextPos;
                        lookIndex = (lookIndex + 1) % count;
                    }

                    // 3) Drive toward that waypoint-based look-ahead
                    Agent.SetDestination(lookPoint);

                    break;
                }

            case BehaviourType.FollowTarget: {
                    if (!target) return;

                    // Compute a point behind the target along its forward vector
                    Vector3 desiredPos = target.position - target.forward * followTargetDistance;

                    if (Vector3.Distance(desiredPos, CarController.transform.position) < followTargetDistance)
                        stopNow = true;
                    else
                        stopNow = false;

                    Agent.SetDestination(desiredPos);

                    break;
                }

            case BehaviourType.ChaseTarget: {
                    if (!target) return;

                    // 1) Get target velocity if it has a Rigidbody, else assume zero
                    Vector3 targetVel = Vector3.zero;
                    var rb = target.GetComponent<Rigidbody>();
                    if (rb != null) {
                        targetVel = rb.linearVelocity;
                    }

                    // 2) Estimate time to reach current target position
                    float distance = Vector3.Distance(transform.position, target.position);
                    float agentSpeed = Agent.speed;
                    float timeToReach = agentSpeed > 0f
                        ? distance / agentSpeed
                        : 0f;

                    // 3) Clamp prediction to your chasePredictionTime
                    float predictT = Mathf.Clamp(timeToReach, 0f, chasePredictionTime);

                    // 4) Compute intercept point
                    Vector3 interceptPoint = target.position + targetVel * predictT;

                    // 5) Send the agent to the predicted intercept
                    Agent.SetDestination(interceptPoint);

                    break;
                }
        }

        // keep agent’s internal position in sync with the Rigidbody’s transform
        Agent.nextPosition = transform.position;
    }


    //──────────────────────────────────────────────────────────────────────────────
    //  Control computation
    //──────────────────────────────────────────────────────────────────────────────

    private void ComputeControls() {

        float predictionTime = .5f;

        // 1) Predict future state
        PredictFutureState(predictionTime, out Vector3 predPos, out Quaternion predRot, out Vector3 predVel, out Vector3 predAngVel);

        // 2) EARLY OUT: no valid path
        if (!Agent.hasPath || stopNow) {
            inputs.steerInput = 0f;
            inputs.throttleInput = 0f;
            inputs.brakeInput = maxBrake;
            inputs.handbrakeInput = 0f;
            return;
        }

        if (reverseNow) {
            inputs.steerInput = 0f;
            inputs.throttleInput = 0f;
            inputs.brakeInput = 1f;
            inputs.handbrakeInput = 0f;
            return;
        }

        //─────────────────── 3) STEERING with prediction ────────────────────
        float speedKph = CarController.speed;
        speedKph = Mathf.Clamp(speedKph, 0f, float.MaxValue);
        float steeringLookAhead = Mathf.Max(minLookAhead, lookAheadPerKph * speedKph);

        Vector3 lookPt = behaviour == BehaviourType.FollowWaypoints || behaviour == BehaviourType.RaceWaypoints ? GetWaypointLookAheadPoint(steeringLookAhead) : GetLookAheadPoint(steeringLookAhead);
        Vector3 localLook = Quaternion.Inverse(predRot) * (lookPt - predPos);
        float rawSteer = Mathf.Atan2(localLook.x, localLook.z);
        float steer = Mathf.Clamp(rawSteer * steerSensitivity, -1f, 1f);

        //───────────────── 4) CURVATURE → SAFE SPEED ────────────────────
        float speedLookAhead;

        if (behaviour == BehaviourType.FollowWaypoints)
            speedLookAhead = steeringLookAhead;
        else if (behaviour == BehaviourType.RaceWaypoints)
            speedLookAhead = raceLookAhead;
        else if (behaviour == BehaviourType.FollowTarget)
            speedLookAhead = steeringLookAhead;
        else if (behaviour == BehaviourType.ChaseTarget)
            speedLookAhead = raceLookAhead;
        else
            speedLookAhead = steeringLookAhead;

        float minRadius = GetTightestRadiusAhead(speedLookAhead);
        if (minRadius < 1f) minRadius = 1f;

        float aLat = roadGrip * 9.81f;
        float safeSpeed = Mathf.Sqrt(aLat * minRadius);      // m/s
        float safeSpeedKph = safeSpeed * 3.6f;

        //──────────────── 5) PID LONGITUDINAL CONTROL ─────────────────
        float error = safeSpeedKph - speedKph;
        pidIntegral += error * Time.fixedDeltaTime;
        float derivative = (error - lastSpeedError) / Time.fixedDeltaTime;
        lastSpeedError = error;

        float control = kp * error + ki * pidIntegral + kd * derivative;
        float throttle = Mathf.Clamp01(control / Mathf.Lerp(30f, 10f, Mathf.InverseLerp(0f, 3f, agressiveness))) * maxThrottle;
        float brakePID = Mathf.Clamp01(-control / Mathf.Lerp(30f, 10f, Mathf.InverseLerp(0f, 3f, agressiveness))) * maxBrake;

        //──────────────── 6) FEED-FORWARD BRAKE (OVERSPEED) ────────────────
        float ffBrake = 0f;
        if (speedKph > safeSpeedKph)
            ffBrake = Mathf.Clamp01((speedKph - safeSpeedKph) / safeSpeedKph) * brakeFeedForwardFactor;

        //──────────────── 7) ANGLE-BASED BRAKE ────────────────────
        Vector3 dirLook = lookPt - predPos;
        float angleToLook = Vector3.Angle(predRot * Vector3.forward, dirLook);
        float angleBrake = Mathf.Clamp01(angleToLook / Mathf.Lerp(20f, 75f, Mathf.InverseLerp(0f, 3f, agressiveness))) * maxBrake;

        //──────────────── 8) COMBINE AND APPLY ────────────────────
        float finalBrake = Mathf.Max(brakePID, ffBrake, angleBrake);
        float cutThrottle = finalBrake;

        if (finalBrake < .3f || speedKph < 25f)
            finalBrake = 0f;

        if (finalBrake >= .3f && speedKph >= 25f)
            throttle = 0f;

        if (speedKph < 25f)
            cutThrottle = 0f;

        if (throttle > .95f)
            throttle = 1f;

        inputs.steerInput = Mathf.Clamp(steer, -1f, 1f);
        inputs.throttleInput = Mathf.Clamp01(throttle - cutThrottle);
        inputs.brakeInput = Mathf.Clamp01(finalBrake);
        inputs.handbrakeInput = 0f;

    }



    //──────────────────────────────────────────────────────────────────────────────
    //  Helper – get point 'dist' metres along current path
    //──────────────────────────────────────────────────────────────────────────────

    private Vector3 GetLookAheadPoint(float dist) {

        Vector3 pos = CarController.transform.position;
        int i = 0;
        float travelled = 0f;

        while (i < Agent.path.corners.Length - 1) {

            Vector3 a = Agent.path.corners[i];
            Vector3 b = Agent.path.corners[i + 1];
            float seg = Vector3.Distance(a, b);

            if (travelled + seg > dist) {

                float t = (dist - travelled) / seg;
                return Vector3.Lerp(a, b, t);

            }

            travelled += seg;
            i++;

        }

        return Agent.path.corners[Agent.path.corners.Length - 1];

    }

    //──────────────────────────────────────────────────────────────────────────────
    //  Helper – tightest turn radius within 'scanDist' metres ahead
    //──────────────────────────────────────────────────────────────────────────────

    private float GetTightestRadiusAhead(float scanDist) {
        if (!Agent.hasPath || Agent.path.corners.Length < 3) return 1000f;

        float minRadius = float.MaxValue;
        float travelled = 0f;

        for (int i = 1; i < Agent.path.corners.Length - 1; i++) {
            Vector3 p0 = Agent.path.corners[i - 1];
            Vector3 p1 = Agent.path.corners[i];
            Vector3 p2 = Agent.path.corners[i + 1];

            float segmentLength = Vector3.Distance(p0, p1);
            travelled += segmentLength;

            if (travelled > scanDist) break;

            // Calculate turn radius using the law of cosines
            float a = Vector3.Distance(p0, p1);
            float b = Vector3.Distance(p1, p2);
            float c = Vector3.Distance(p0, p2);

            if (a > 0.1f && b > 0.1f && c > 0.1f) {
                float angle = Mathf.Acos(Mathf.Clamp((a * a + b * b - c * c) / (2f * a * b), -1f, 1f));
                if (angle > 0.01f) {
                    float radius = a / (2f * Mathf.Sin(angle * 0.5f));
                    minRadius = Mathf.Min(minRadius, radius);
                }
            }
        }

        return minRadius == float.MaxValue ? 1000f : Mathf.Max(minRadius, 5f);
    }

    //──────────────────────────────────────────────────────────────────────────────
    //  Stuck handling
    //──────────────────────────────────────────────────────────────────────────────

    private void HandleStuckVehicle() {

        if (!CarController.canControl) {
            stuckTimer = 0f;
            return;
        }

        if (!checkStuck) {
            stuckTimer = 0f;
            return;
        }

        if (reverseNow)
            stuckTimer = 0f;

        float speedKph = CarController.absoluteSpeed;

        // 1) Forward stuck: applying throttle but not moving forward
        if (CarController.direction == 1 && speedKph < 2f && inputs.throttleInput >= .3f) {
            stuckTimer += Time.fixedDeltaTime;
        }

        if (stuckTimer > 2f) {

            stuckTimer = 0f;
            StartCoroutine(FixStuck());

        }

    }

    private IEnumerator FixStuck() {

        CarController.Inputs.autoReverse = true;
        reverseNow = true;
        yield return new WaitForSeconds(1.5f);
        reverseNow = false;
        CarController.Inputs.autoReverse = false;
        CarController.Gearbox.ShiftToGear(0);

    }

    //──────────────────────────────────────────────────────────────────────────────
    //  Gizmos – visual debug aid
    //──────────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmos() {

        if (!Application.isPlaying)
            return;

        // Abort if we have no agent yet (e.g. in edit-time prefab view).
        if (Agent == null || !Agent.isActiveAndEnabled)
            return;

        // Base car position lifted slightly
        Vector3 carPos = CarController.transform.position + Vector3.up * .25f;
        float speedKph = CarController.speed;

        // 1) Behaviour & speed label
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
        style.normal.textColor = Color.white;
        UnityEditor.Handles.Label(carPos + Vector3.up * 1.0f,
                      $"{behaviour}  |  {speedKph:0} km/h", style);
#endif

        // 2) Destination line (green)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(carPos, Agent.destination + Vector3.up * .25f);
        Gizmos.DrawWireSphere(Agent.destination + Vector3.up * .25f, .5f);

        // 3) Highlight next waypoint (cyan)
        if (waypointsContainer != null && waypointsContainer.waypoints.Count > 0) {
            var nextWp = waypointsContainer.waypoints[currentWaypointIndex].transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(nextWp + Vector3.up * .3f, .4f);
        }

        // 4) Agent path corners (cyan)
        if (Agent.hasPath) {
            Gizmos.color = Color.cyan;
            var pts = Agent.path.corners;
            for (int i = 0; i < pts.Length - 1; i++) {
                Gizmos.DrawLine(pts[i] + Vector3.up * .1f, pts[i + 1] + Vector3.up * .1f);
                Gizmos.DrawSphere(pts[i] + Vector3.up * .1f, .20f);
            }
            Gizmos.DrawSphere(pts[pts.Length - 1] + Vector3.up * .1f, .20f);

            // Steering target arrow
#if UNITY_EDITOR
            UnityEditor.Handles.ArrowHandleCap(0,
                                   Agent.steeringTarget,
                                   Quaternion.LookRotation(Agent.steeringTarget - carPos),
                                   2f,
                                   EventType.Repaint);
#endif
        }

        // 5) Predicted future position (1s ahead) in blue
        PredictFutureState(1f, out Vector3 predPos, out _, out _, out _);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(predPos + Vector3.up * .25f, .2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(predPos + Vector3.up * .5f, "Predicted+1s");
#endif

        // 6) Velocity vector (white)
        Gizmos.color = Color.white;
        Gizmos.DrawLine(carPos, carPos + CarController.Rigid.linearVelocity.normalized * 2f);

        // 8) Safe-turn radius ring (orange)
        float lookDist = Mathf.Max(minLookAhead, lookAheadPerKph * speedKph);
        float safeR = GetTightestRadiusAhead(lookDist);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(carPos, safeR);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(carPos + Vector3.right * safeR, $"SafeR {safeR:0.0}m");
#endif

        // 9) Dynamic look-ahead point (magenta)
        Vector3 lookPt = GetLookAheadPoint(lookDist);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(lookPt + Vector3.up * .15f, .30f);
        Gizmos.DrawLine(carPos, lookPt + Vector3.up * .15f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(lookPt + Vector3.up * .45f, $"Look {lookDist:0.0}m");
#endif

    }


    /// <summary>
    /// Resets the timer used for flipping the vehicle.
    /// </summary>
    public void Reload() {

        //autoResetTimer = 0f;

    }

    Vector3 GetWaypointLookAheadPoint(float lookAheadDist) {
        float travelled = 0f;
        int i = currentWaypointIndex;
        int cnt = waypointsContainer.waypoints.Count;
        Vector3 last = CarController.transform.position;
        while (travelled < lookAheadDist) {
            Vector3 nextPt = waypointsContainer.waypoints[i].transform.position;
            float seg = Vector3.Distance(last, nextPt);
            if (travelled + seg >= lookAheadDist)
                return Vector3.Lerp(last, nextPt, (lookAheadDist - travelled) / seg);
            travelled += seg;
            last = nextPt;
            i = (i + 1) % cnt;
        }
        return last;
    }

    /// <summary>
    /// Predicts future rigidbody state after a given time using simple integration.
    /// </summary>
    private void PredictFutureState(float dt, out Vector3 predictedPosition, out Quaternion predictedRotation, out Vector3 predictedVelocity, out Vector3 predictedAngularVelocity) {

        // Copy current rigidbody state
        predictedVelocity = CarController.Rigid.linearVelocity;
        predictedAngularVelocity = CarController.Rigid.angularVelocity;

        // Linear prediction (constant velocity)
        predictedPosition = CarController.transform.position + predictedVelocity * dt;

        // Angular prediction (constant angular velocity)
        predictedRotation = CarController.transform.rotation * Quaternion.Euler(predictedAngularVelocity * Mathf.Rad2Deg * dt);

    }

    /// <summary>
    /// Overrides AI inputs based on obstacle avoidance feedback:
    ///  • Small brake values (or very low speeds) are ignored.
    ///  • Throttle is scaled down by (1 – brakeInput).
    ///  • If brakeInput exceeds full-brake threshold, throttle is cut entirely.
    /// </summary>
    public void CheckOverridedInputs() {

        // Ensure we have the avoidance component
        if (!obstacleAvoidance) {
            obstacleAvoidance = GetComponent<RCCP_AIDynamicObstacleAvoidance>();
        }

        if (!obstacleAvoidance || Mathf.Abs(obstacleAvoidance.steerInput) < .1f) {
            return;
        }

        if (stuckTimer >= 2f)
            return;

        // Grab raw brake value from the avoidance script
        float brakeValue = obstacleAvoidance.brakeInput;

        // Filter out tiny brake signals and very low speeds
        const float minBrakeToApply = .5f;
        const float minSpeedToBrake = 25f;
        //const float fullBrakeThreshold = .5f;

        if (brakeValue < minBrakeToApply || CarController.speed < minSpeedToBrake) {
            brakeValue = 0f;
        }

        // Apply filtered brake
        //inputs.brakeInput = Mathf.Clamp01(brakeValue);

        //// Scale throttle down by the brake amount
        //inputs.throttleInput = Mathf.Clamp01(inputs.throttleInput * (1f - inputs.brakeInput));

        //// If brake demand is high, cut throttle completely
        //if (inputs.brakeInput > fullBrakeThreshold) {
        //    inputs.throttleInput = 0f;
        //}

        // Steer override as before, clamped to [-1,1]
        inputs.steerInput += obstacleAvoidance.steerInput * 2f;
        inputs.steerInput = Mathf.Clamp(inputs.steerInput, -1f, 1f);
    }


    private void Reset() {

        NavMeshAgent agentRef = Agent;

    }

}
