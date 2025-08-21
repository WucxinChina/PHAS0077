//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/AI/RCCP AI Dynamic Obstacle Avoidance")]
public class RCCP_AIDynamicObstacleAvoidance : MonoBehaviour {

    private RCCP_CarController CarController {
        get {
            if (carController == null)
                carController = GetComponentInParent<RCCP_CarController>(true);
            return carController;
        }
    }
    private RCCP_CarController carController;

    [Tooltip("Manual list of obstacle Transforms. Use AddObstacle/RemoveObstacle to manage.")]
    public List<Transform> possibleObstacles = new List<Transform>();

    [Tooltip("Enable to automatically populate possibleObstacles from scene colliders within radius.")]
    public bool autoDetectObstacles = true;

    [Tooltip("Select layers to consider for automatic detection.")]
    public LayerMask dynamicObstacleLayers;

    [Tooltip("Maximum detection radius for auto-detecting obstacles.")]
    public float detectionRadius = 10f;

    [Tooltip("Seconds to look ahead when predicting agent and obstacle positions.")]
    public float predictionTime = 1f;

    [Tooltip("Update interval for path prediction and obstacle pool refresh.")]
    public float poolUpdateInterval = 0.2f;

    [Tooltip("Maximum spacing (in metres) between raycast samples on obstacle bounds.")]
    public float sampleSpacing = 1f;

    [Header("Avoidance Tuning")]
    [Tooltip("Maximum steering input magnitude (0-1)")]
    public float maxSteerInput = 1f;

    [Tooltip("Maximum brake input magnitude (0-1)")]
    public float maxBrakeInput = 1f;

    [Tooltip("Distance at which maximum braking is applied")]
    public float emergencyBrakeDistance = 2f;

    [Tooltip("Multiplier for steering sensitivity")]
    public float steerSensitivity = 2f;

    [Tooltip("Minimum speed threshold for avoidance (m/s)")]
    public float minSpeedThreshold = 0.5f;

    public float steerInput;
    public float brakeInput;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private float updateTimer;

    [Header("Risk Assessment")]
    [Tooltip("Minimum risk threshold to trigger avoidance (0-1)")]
    public float riskThreshold = 0.3f;

    [Tooltip("Vehicle width for collision prediction")]
    public float vehicleWidth = 2f;

    [Tooltip("Vehicle length for collision prediction")]
    public float vehicleLength = 4f;

    [Tooltip("Safety margin added to vehicle dimensions")]
    public float safetyMargin = 0.5f;

    // Risk assessment structure
    [System.Serializable]
    public struct RiskAssessment {
        public float collisionRisk;           // 0-1, probability of collision
        public float timeToCollision;         // seconds until potential collision
        public Vector3 collisionPoint;        // predicted collision location
        public Vector3 safeAvoidanceDirection; // direction with lowest risk
        public float avoidanceUrgency;        // how urgent the avoidance is (0-1)
        public bool canSafelyPass;           // whether we can pass without major input
    }

    // Calculate risk for all obstacles
    public List<RiskAssessment> risks = new List<RiskAssessment>();
    public RiskAssessment highestRisk = new RiskAssessment();

    private void Awake() {
        agent = GetComponentInChildren<NavMeshAgent>(true);
        rb = CarController.Rigid;
    }

    private void FixedUpdate() {
        updateTimer += Time.fixedDeltaTime;
        if (updateTimer < poolUpdateInterval) return;
        updateTimer = 0f;

        if (autoDetectObstacles) {
            RefreshObstaclePool();
        }

        CalculateAvoidanceInputs();

        // Apply speed factor only to steering, not braking
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed < minSpeedThreshold) {
            float speedFactor = Mathf.Clamp01(currentSpeed / minSpeedThreshold);
            steerInput *= speedFactor;
            // Don't reduce braking at low speeds - you might still need to stop
        }
    }

    private void RefreshObstaclePool() {
        Collider[] hits = Physics.OverlapSphere(CarController.transform.position, detectionRadius, dynamicObstacleLayers);
        possibleObstacles.Clear();

        foreach (var col in hits) {
            if (col.isTrigger) continue;
            if (col.transform.IsChildOf(CarController.transform)) continue;

            Vector3 toObs = col.transform.position - CarController.transform.position;
            // Only consider obstacles in front (with some side tolerance)
            if (Vector3.Dot(CarController.transform.forward, toObs.normalized) > 0.2f) {
                // only truly "in front"
                AddObstacle(col.transform);
            }
        }
    }

    private void CalculateAvoidanceInputs() {
        steerInput = 0f;
        brakeInput = 0f;

        // Calculate risk for all obstacles
        risks.Clear();
        highestRisk = new RiskAssessment();

        if (possibleObstacles.Count == 0) return;

        float currentSpeed = CarController.Rigid.linearVelocity.magnitude;

        // Skip avoidance if moving too slowly
        if (currentSpeed < minSpeedThreshold) return;

        // Calculate dynamic avoidance distance based on speed and braking capability
        float brakingDeceleration = CalculateBrakingDeceleration();
        float safeDecel = Mathf.Max(brakingDeceleration, 0.1f);
        float stoppingDistance = currentSpeed * currentSpeed / (2f * safeDecel);
        float avoidanceDistance = currentSpeed * predictionTime
                                + stoppingDistance * 1.2f;
        avoidanceDistance = Mathf.Clamp(avoidanceDistance, emergencyBrakeDistance, detectionRadius);


        Vector3 agentPos = CarController.transform.position;
        Vector3 predictedPos = PredictAgentFuturePosition();

        float mostUrgentThreat = float.MaxValue;
        Vector3 bestAvoidanceDirection = Vector3.zero;
        float strongestBrakeForce = 0f;

        foreach (Transform obstacle in possibleObstacles) {
            if (obstacle == null) continue;
            if (obstacle.IsChildOf(CarController.transform)) continue;

            Collider obsCol = obstacle.GetComponent<Collider>();
            if (obsCol == null) continue;

            RiskAssessment risk = AssessCollisionRisk(obstacle, obsCol, currentSpeed);
            if (risk.collisionRisk > riskThreshold) {
                risks.Add(risk);
            }

            // Analyze this obstacle
            ObstacleAnalysis analysis = AnalyzeObstacle(obstacle, obsCol, agentPos, predictedPos, avoidanceDistance, currentSpeed);

            if (analysis.isValid && analysis.distance < mostUrgentThreat) {
                mostUrgentThreat = analysis.distance;
                bestAvoidanceDirection = analysis.avoidanceDirection;
                strongestBrakeForce = analysis.brakeIntensity;
            }
        }

        // Apply the inputs from the most urgent threat
        if (mostUrgentThreat < float.MaxValue) {
            steerInput = CalculateSteerInput(bestAvoidanceDirection, mostUrgentThreat, avoidanceDistance);
            brakeInput = Mathf.Clamp01(strongestBrakeForce * maxBrakeInput);
        }

        if (risks.Count > 0) {

            // Find the highest risk scenario
            highestRisk = risks[0];
            foreach (var risk in risks) {
                if (risk.collisionRisk > highestRisk.collisionRisk) {
                    highestRisk = risk;
                }
            }

            // Apply inputs based on risk assessment
            ApplyRiskBasedInputs(highestRisk, currentSpeed);

        } else {

            steerInput = 0f;
            brakeInput = 0f;

        }

    }

    private struct ObstacleAnalysis {
        public bool isValid;
        public float distance;
        public Vector3 avoidanceDirection;
        public float brakeIntensity;
    }

    private ObstacleAnalysis AnalyzeObstacle(Transform obstacle, Collider obsCol, Vector3 agentPos, Vector3 predictedPos, float maxAvoidDist, float currentSpeed) {
        ObstacleAnalysis analysis = new ObstacleAnalysis();

        Bounds bounds = obsCol.bounds;
        Vector3 closestPoint = bounds.ClosestPoint(agentPos);
        Vector3 toObstacle = closestPoint - agentPos;

        // Check if obstacle is roughly ahead
        float forwardDot = Vector3.Dot(CarController.transform.forward, toObstacle.normalized);
        if (forwardDot <= 0.1f) {
            return analysis;
        }

        float distance = toObstacle.magnitude;
        if (distance > maxAvoidDist) {
            return analysis;
        }

        // Sample the obstacle's bounds with multiple raycasts
        List<Vector3> threatPoints = SampleObstacleBounds(obsCol, agentPos, maxAvoidDist);

        if (threatPoints.Count == 0) {
            return analysis; // No actual line-of-sight threats found
        }

        // Calculate the average threat direction and find the most appropriate avoidance
        Vector3 averageThreatDirection = Vector3.zero;
        float totalWeight = 0f;

        foreach (Vector3 threatPoint in threatPoints) {
            Vector3 toThreat = threatPoint - agentPos;
            float threatDistance = toThreat.magnitude;
            float weight = 1f / (threatDistance + 0.1f); // Closer threats have more weight

            averageThreatDirection += toThreat.normalized * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0) {
            averageThreatDirection /= totalWeight;
        }

        // Determine which side the threat is on and steer away from it
        Vector3 carRight = CarController.transform.right;
        float rightDot = Vector3.Dot(averageThreatDirection, carRight);

        // If threat is on the right side (rightDot > 0), steer left
        // If threat is on the left side (rightDot < 0), steer right
        Vector3 avoidanceDir = (rightDot > 0) ? -carRight : carRight; // Steer away from threat

        // Check if that direction has clearance - if not, try the other direction
        float avoidanceClearance = GetDirectionalClearance(agentPos, avoidanceDir, maxAvoidDist);
        float oppositeClearance = GetDirectionalClearance(agentPos, -avoidanceDir, maxAvoidDist);

        // If the preferred avoidance direction is blocked, use the opposite direction
        if (avoidanceClearance < oppositeClearance * 0.5f) {
            avoidanceDir = -avoidanceDir;
        }

        analysis.isValid = true;
        analysis.distance = distance;
        analysis.avoidanceDirection = avoidanceDir;

        // Calculate brake intensity based on distance and approach angle
        float distanceRatio = Mathf.Clamp01((maxAvoidDist - distance) / maxAvoidDist);
        float approachAngle = Vector3.Angle(CarController.transform.forward, toObstacle.normalized);
        float angleRatio = Mathf.Clamp01(1f - (approachAngle / 90f));
        analysis.brakeIntensity = distanceRatio * angleRatio;

        if (distance < emergencyBrakeDistance) {
            analysis.brakeIntensity = Mathf.Max(analysis.brakeIntensity, 0.8f);
        }

        return analysis;
    }

    private List<Vector3> SampleObstacleBounds(Collider obsCol, Vector3 agentPos, float maxDistance) {
        List<Vector3> threatPoints = new List<Vector3>();
        Bounds bounds = obsCol.bounds;

        // Calculate number of samples based on obstacle size and sample spacing
        int xSamples = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / sampleSpacing));
        int zSamples = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / sampleSpacing));
        int ySamples = Mathf.Max(1, Mathf.CeilToInt(bounds.size.y / sampleSpacing));

        // Sample points across the obstacle's bounds
        for (int x = 0; x <= xSamples; x++) {
            for (int z = 0; z <= zSamples; z++) {
                for (int y = 0; y <= ySamples; y++) {
                    // Calculate sample point within bounds
                    Vector3 samplePoint = new Vector3(
                        Mathf.Lerp(bounds.min.x, bounds.max.x, x / (float)xSamples),
                        Mathf.Lerp(bounds.min.y, bounds.max.y, y / (float)ySamples),
                        Mathf.Lerp(bounds.min.z, bounds.max.z, z / (float)zSamples)
                    );

                    Vector3 toSample = samplePoint - agentPos;
                    Vector3 direction = toSample.normalized;
                    float sampleDistance = toSample.magnitude;

                    // Skip samples that are too far or behind the vehicle
                    if (sampleDistance > maxDistance) continue;

                    float forwardDot = Vector3.Dot(CarController.transform.forward, direction);
                    if (forwardDot <= 0.1f) continue; // Only consider samples roughly ahead

                    // Only consider samples within a reasonable forward cone (adjust angle as needed)
                    float angle = Vector3.Angle(CarController.transform.forward, direction);
                    if (angle > 30f) continue; // 45 degree cone ahead

                    // Raycast to see if this sample point is actually visible/threatening
                    if (Physics.Raycast(agentPos, direction, out RaycastHit hit, sampleDistance + 0.5f, dynamicObstacleLayers)) {
                        // Check if we hit the target obstacle (or something closer)
                        if (hit.transform == obsCol.transform || hit.distance < sampleDistance) {
                            threatPoints.Add(hit.point);
                        }
                    }
                }
            }
        }

        return threatPoints;
    }

    private float GetDirectionalClearance(Vector3 from, Vector3 direction, float maxDistance) {
        if (Physics.Raycast(from, direction, out RaycastHit hit, maxDistance, dynamicObstacleLayers)) {
            return hit.distance;
        }
        return maxDistance; // No obstacle found in this direction
    }

    private float CalculateSteerInput(Vector3 avoidanceDirection, float obstacleDistance, float maxAvoidDistance) {

        Vector3 carRight = CarController.transform.right;

        float rightComponent = Vector3.Dot(avoidanceDirection, carRight);
        float baseSteer = Mathf.Sign(rightComponent)
                             * Mathf.Clamp01(Mathf.Abs(rightComponent) * steerSensitivity);

        float urgencyFactor = Mathf.Clamp01((maxAvoidDistance - obstacleDistance) / maxAvoidDistance);
        urgencyFactor = Mathf.Pow(urgencyFactor, 0.7f);

        return Mathf.Clamp(baseSteer * urgencyFactor, -maxSteerInput, maxSteerInput);

    }

    private float CalculateBrakingDeceleration() {
        // Estimate maximum braking deceleration based on car properties
        // This is a simplified calculation - adjust based on your car controller's actual braking system
        float maxBrakeForce = CarController.brakeInput_V * 1000f; // Assuming this is in Newtons
        float mass = CarController.Rigid.mass;
        float maxDeceleration = maxBrakeForce / mass;

        // Add some safety margin and realistic limits
        return Mathf.Clamp(maxDeceleration, 2f, 15f); // Reasonable range for vehicle deceleration
    }

    private Vector3 PredictAgentFuturePosition() {
        if (agent == null || !agent.hasPath) {
            // Fallback: simple linear prediction
            return CarController.transform.position + CarController.Rigid.linearVelocity * predictionTime;
        }

        NavMeshPath path = agent.path;
        float currentSpeed = CarController.Rigid.linearVelocity.magnitude;
        float travelDistance = currentSpeed * predictionTime;
        float coveredDistance = 0f;

        Vector3 currentPos = CarController.transform.position;

        // Find the closest point on the path to current position
        int startCorner = 0;
        float minDistToPath = float.MaxValue;

        for (int i = 0; i < path.corners.Length - 1; i++) {
            Vector3 closestOnSegment = GetClosestPointOnLineSegment(path.corners[i], path.corners[i + 1], currentPos);
            float distToSegment = Vector3.Distance(currentPos, closestOnSegment);

            if (distToSegment < minDistToPath) {
                minDistToPath = distToSegment;
                startCorner = i;
            }
        }

        // Travel along the path from the closest point
        for (int i = startCorner; i < path.corners.Length - 1; i++) {
            Vector3 segmentStart = (i == startCorner) ?
                GetClosestPointOnLineSegment(path.corners[i], path.corners[i + 1], currentPos) :
                path.corners[i];
            Vector3 segmentEnd = path.corners[i + 1];

            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);

            if (coveredDistance + segmentLength >= travelDistance) {
                float t = (travelDistance - coveredDistance) / segmentLength;
                return Vector3.Lerp(segmentStart, segmentEnd, t);
            }

            coveredDistance += segmentLength;
        }

        // If we've gone through all corners, return the destination
        return path.corners[path.corners.Length - 1];
    }

    private Vector3 GetClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p) {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    public void AddObstacle(Transform obstacle) {
        if (obstacle == null) return;
        Collider col = obstacle.GetComponent<Collider>();
        if (col != null && col.isTrigger) return;
        if (obstacle.IsChildOf(CarController.transform)) return;
        if (!possibleObstacles.Contains(obstacle)) possibleObstacles.Add(obstacle);
    }

    public void RemoveObstacle(Transform obstacle) {
        if (possibleObstacles.Contains(obstacle)) possibleObstacles.Remove(obstacle);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (!Application.isPlaying) return;
        if (CarController == null) return;

        Vector3 agentPos = CarController.transform.position;

        // Draw obstacle threat points and sample rays
        Gizmos.color = Color.yellow;
        foreach (Transform obstacle in possibleObstacles) {
            if (obstacle == null) continue;
            Collider obsCol = obstacle.GetComponent<Collider>();
            if (obsCol == null) continue;

            // Draw threat points found by sampling
            List<Vector3> threatPoints = SampleObstacleBounds(obsCol, agentPos, detectionRadius);
            Gizmos.color = Color.red;
            foreach (Vector3 threatPoint in threatPoints) {
                Gizmos.DrawSphere(threatPoint, 0.1f);
                Gizmos.DrawLine(agentPos, threatPoint);
            }
        }

        // Draw steering direction
        if (Mathf.Abs(steerInput) > 0.1f) {
            Gizmos.color = steerInput > 0 ? Color.green : Color.red;
            Vector3 steerDir = CarController.transform.right * steerInput * 3f;
            GizmoDrawArrow(agentPos, steerDir, 0.2f);
        }

        // Draw clearance rays for debugging
        if (possibleObstacles.Count > 0) {
            Transform nearestObstacle = null;
            float nearestDist = float.MaxValue;

            foreach (Transform obs in possibleObstacles) {
                if (obs == null) continue;
                float dist = Vector3.Distance(agentPos, obs.position);
                if (dist < nearestDist) {
                    nearestDist = dist;
                    nearestObstacle = obs;
                }
            }
        }
    }

    private void GizmoDrawArrow(Vector3 from, Vector3 direction, float headSize) {
        Vector3 to = from + direction;
        Gizmos.DrawLine(from, to);

        Vector3 dir = direction.normalized;
        Vector3 perpendicular = Vector3.Cross(dir, Vector3.up) * headSize;
        Vector3 back = -dir * headSize;

        Gizmos.DrawLine(to, to + back + perpendicular);
        Gizmos.DrawLine(to, to + back - perpendicular);
    }
#endif

    private RiskAssessment AssessCollisionRisk(Transform obstacle, Collider obsCol, float currentSpeed) {
        RiskAssessment assessment = new RiskAssessment();

        Vector3 agentPos = CarController.transform.position;
        Vector3 agentVelocity = CarController.Rigid.linearVelocity;

        // Predict both positions
        Vector3 predictedAgentPos = PredictAgentFuturePosition();
        Vector3 predictedObstaclePos = PredictObstacleFuturePosition(obstacle);

        // Calculate time to collision first
        assessment.timeToCollision = CalculateTimeToCollision(agentPos, agentVelocity, obstacle);

        // Always calculate trajectory risk for comprehensive assessment
        float trajectoryRisk = CalculateTrajectoryRisk(agentPos, agentVelocity, obstacle, obsCol);

        // Create vehicle bounding box at predicted position
        Bounds vehicleBounds = CreateVehicleBounds(predictedAgentPos, CarController.transform.rotation);
        Bounds obstacleBounds = obsCol.bounds;

        // Expand obstacle bounds to predicted position
        Vector3 obstacleMovement = predictedObstaclePos - obstacle.position;
        obstacleBounds.center += obstacleMovement;

        // Check if predicted paths will intersect
        bool willIntersect = vehicleBounds.Intersects(obstacleBounds);

        if (willIntersect) {
            // Direct intersection predicted - combine both risks
            float intersectionRisk = CalculateIntersectionRisk(vehicleBounds, obstacleBounds, currentSpeed);
            assessment.collisionRisk = Mathf.Max(trajectoryRisk, intersectionRisk);
            assessment.canSafelyPass = false;
        } else {
            // Use trajectory risk
            assessment.collisionRisk = trajectoryRisk;
            assessment.canSafelyPass = assessment.collisionRisk < 0.2f;
        }

        // Improved urgency calculation
        assessment.avoidanceUrgency = CalculateAvoidanceUrgency(assessment.collisionRisk, assessment.timeToCollision, currentSpeed);

        // Find safest avoidance direction
        assessment.safeAvoidanceDirection = FindSafestDirection(agentPos, obstacle, obsCol);

        // Set collision point
        assessment.collisionPoint = GetClosestApproachPoint(predictedAgentPos, predictedObstaclePos);

        return assessment;
    }

    private float CalculateAvoidanceUrgency(float collisionRisk, float timeToCollision, float currentSpeed) {
        // If no collision risk, no urgency
        if (collisionRisk <= 0.01f) return 0f;

        // If collision is imminent (less than 1 second), urgency should be very high
        if (timeToCollision < 1f) {
            return Mathf.Clamp01(collisionRisk * (2f - timeToCollision)); // Amplify urgency as time decreases
        }

        // For longer time horizons, consider both risk and time
        float timeUrgency = Mathf.Clamp01(5f / Mathf.Max(timeToCollision, 0.1f)); // More urgent as time decreases
        float speedFactor = Mathf.Clamp01(currentSpeed / 20f); // Higher speed = more urgent

        return Mathf.Clamp01(collisionRisk * timeUrgency * (1f + speedFactor));
    }

    private float CalculateTrajectoryRisk(Vector3 agentPos, Vector3 agentVelocity, Transform obstacle, Collider obsCol) {
        float maxRisk = 0f;
        float checkInterval = 0.04f; // Check more frequently for better accuracy
        float maxCheckTime = Mathf.Max(predictionTime, 3f); // Ensure we check at least 3 seconds ahead

        Rigidbody obstacleRb = obstacle.GetComponent<Rigidbody>();
        Vector3 obstacleVelocity = obstacleRb != null ? obstacleRb.linearVelocity : Vector3.zero;

        for (float t = checkInterval; t <= maxCheckTime; t += checkInterval) {
            Vector3 futureAgentPos = agentPos + agentVelocity * t;
            Vector3 futureObstaclePos = obstacle.position + obstacleVelocity * t;

            // Create bounds for both objects at future positions
            Bounds futureBounds = CreateVehicleBounds(futureAgentPos, CarController.transform.rotation);
            Bounds futureObsBounds = obsCol.bounds;
            futureObsBounds.center = futureObstaclePos;

            // Calculate distance between centers
            float distance = Vector3.Distance(futureAgentPos, futureObstaclePos);
            float minSafeDistance = (futureBounds.size.magnitude + futureObsBounds.size.magnitude) * 0.5f;

            if (futureBounds.Intersects(futureObsBounds)) {
                // Direct intersection - very high risk
                float timeRisk = 1f - (t / maxCheckTime);
                float intersectionRisk = 0.9f; // High base risk for intersection
                maxRisk = Mathf.Max(maxRisk, timeRisk * intersectionRisk + 0.1f);
            } else if (distance < minSafeDistance) {
                // Close proximity - moderate to high risk
                float timeRisk = 1f - (t / maxCheckTime);
                float proximityRisk = 1f - (distance / minSafeDistance);
                float combinedRisk = timeRisk * proximityRisk * 0.7f; // Scale down proximity risk
                maxRisk = Mathf.Max(maxRisk, combinedRisk);
            }
        }

        return maxRisk;
    }

    private float CalculateIntersectionRisk(Bounds vehicleBounds, Bounds obstacleBounds, float speed) {
        // Calculate 3D overlap for better accuracy
        Vector3 overlap = Vector3.zero;
        overlap.x = Mathf.Max(0, Mathf.Min(vehicleBounds.max.x, obstacleBounds.max.x) -
                                Mathf.Max(vehicleBounds.min.x, obstacleBounds.min.x));
        overlap.y = Mathf.Max(0, Mathf.Min(vehicleBounds.max.y, obstacleBounds.max.y) -
                                Mathf.Max(vehicleBounds.min.y, obstacleBounds.min.y));
        overlap.z = Mathf.Max(0, Mathf.Min(vehicleBounds.max.z, obstacleBounds.max.z) -
                                Mathf.Max(vehicleBounds.min.z, obstacleBounds.min.z));

        float overlapVolume = overlap.x * overlap.y * overlap.z;
        float vehicleVolume = vehicleBounds.size.x * vehicleBounds.size.y * vehicleBounds.size.z;

        // If there's any overlap, risk should be high
        if (overlapVolume > 0) {
            float overlapRatio = overlapVolume / vehicleVolume;
            float speedFactor = Mathf.Clamp01(speed / 20f);
            return Mathf.Clamp01(0.7f + overlapRatio * 0.3f + speedFactor * 0.2f); // Minimum 70% risk for any intersection
        }

        // Even if bounds barely don't overlap, check distance between centers
        float centerDistance = Vector3.Distance(vehicleBounds.center, obstacleBounds.center);
        float combinedRadius = (vehicleBounds.size.magnitude + obstacleBounds.size.magnitude) * 0.5f;

        if (centerDistance < combinedRadius) {
            float proximityRisk = 1f - (centerDistance / combinedRadius);
            return Mathf.Clamp01(proximityRisk * 0.8f); // Up to 80% risk for very close proximity
        }

        return 0f;
    }

    private Vector3 FindSafestDirection(Vector3 agentPos, Transform obstacle, Collider obsCol) {
        Vector3 carRight = CarController.transform.right;
        Vector3 carLeft = -carRight;

        // Check clearance in both directions
        float rightClearance = GetDirectionalClearance(agentPos, carRight, detectionRadius);
        float leftClearance = GetDirectionalClearance(agentPos, carLeft, detectionRadius);

        // Also check if the directions lead away from the obstacle
        Vector3 toObstacle = obstacle.position - agentPos;
        float rightDot = Vector3.Dot(carRight, toObstacle.normalized);
        float leftDot = Vector3.Dot(carLeft, toObstacle.normalized);

        // Prefer direction that has more clearance AND leads away from obstacle
        float rightScore = rightClearance * (1f - Mathf.Max(0, rightDot));
        float leftScore = leftClearance * (1f - Mathf.Max(0, leftDot));

        return rightScore > leftScore ? carRight : carLeft;
    }

    private float CalculateTimeToCollision(Vector3 agentPos, Vector3 agentVelocity, Transform obstacle) {
        Rigidbody obstacleRb = obstacle.GetComponent<Rigidbody>();
        Vector3 obstacleVelocity = obstacleRb != null ? obstacleRb.linearVelocity : Vector3.zero;

        Vector3 relativePos = obstacle.position - agentPos;
        Vector3 relativeVel = agentVelocity - obstacleVelocity;

        // If objects are moving away from each other, no collision
        if (Vector3.Dot(relativePos, relativeVel) <= 0) {
            return float.MaxValue;
        }

        // Calculate time to closest approach
        float timeToClosest = Vector3.Dot(relativePos, relativeVel) / Vector3.Dot(relativeVel, relativeVel);
        return Mathf.Max(0, timeToClosest);
    }

    private Bounds CreateVehicleBounds(Vector3 position, Quaternion rotation) {
        // Calculate the AABB that encompasses the rotated vehicle
        Vector3[] corners = new Vector3[4];
        float halfWidth = (vehicleWidth + safetyMargin) * 0.5f;
        float halfLength = (vehicleLength + safetyMargin) * 0.5f;

        // Local corners of the vehicle
        corners[0] = new Vector3(-halfWidth, 0, -halfLength);
        corners[1] = new Vector3(halfWidth, 0, -halfLength);
        corners[2] = new Vector3(halfWidth, 0, halfLength);
        corners[3] = new Vector3(-halfWidth, 0, halfLength);

        // Transform to world space
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        for (int i = 0; i < corners.Length; i++) {
            Vector3 worldCorner = position + rotation * corners[i];
            min = Vector3.Min(min, worldCorner);
            max = Vector3.Max(max, worldCorner);
        }

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        size.y = 2f; // Keep original height

        return new Bounds(center, size);
    }

    private Vector3 PredictObstacleFuturePosition(Transform obstacle) {
        Rigidbody obstacleRb = obstacle.GetComponent<Rigidbody>();
        if (obstacleRb != null) {
            return obstacle.position + obstacleRb.linearVelocity * predictionTime;
        }
        return obstacle.position; // Static obstacle
    }

    private Vector3 GetClosestApproachPoint(Vector3 pos1, Vector3 pos2) {
        return (pos1 + pos2) * 0.5f;
    }

    private void ApplyRiskBasedInputs(RiskAssessment risk, float currentSpeed) {

        // If we can safely pass, reduce input intensity significantly
        //float inputMultiplier = risk.canSafelyPass ? 0.3f : 1f;
        float inputMultiplier = risk.avoidanceUrgency;

        steerInput *= inputMultiplier;
        steerInput = Mathf.Clamp(steerInput, -maxSteerInput, maxSteerInput);

        // Braking based on collision risk and time
        if (risk.timeToCollision < emergencyBrakeDistance / currentSpeed) {
            brakeInput = risk.collisionRisk * maxBrakeInput;
        } else {
            // Gentle speed reduction for high-risk scenarios
            brakeInput = risk.collisionRisk * .3f * maxBrakeInput * inputMultiplier;
        }

        brakeInput = Mathf.Clamp01(brakeInput);
    }

}