using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is responsible for visualization details only
// It does this skipping message passing logic! But it is only for visualization purposes, not simulation!
// It does not impact the simulation in any way!

public class Visualizer : MonoBehaviour
{
    CommunicationAgent communicationAgent;
    VehicleAgent vehicleAgent;
    GameObject model;

    [Header("Lane Randomization")]
    [MinMaxSlider(0.0f, 5.0f)] public Vector2 laneRandomizationRange = new Vector2(0.2f, 2.0f);
    public float laneRandomizationSmoothness = 0.5f;
    [ReadOnly] public float baseLaneRandomization;
    [ReadOnly] public float currentLaneRandomization;

    [Header("Rotation")]
    public float rotationSmoothness = 2.5f; // Rotation in movement direction turn speed

    [Header("Platoon Line")]
    public Material platoonLineDebugMaterial;
    LineRenderer lineRenderer;

    void Start()
    {
        communicationAgent = transform.parent.GetComponent<CommunicationAgent>();
        vehicleAgent = transform.parent.GetComponent<VehicleAgent>();
        model = this.gameObject;

        baseLaneRandomization = Random.Range(laneRandomizationRange.x, laneRandomizationRange.y);
        currentLaneRandomization = baseLaneRandomization;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.enabled = false;  
        lineRenderer.material = platoonLineDebugMaterial;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.SetPosition(0, new Vector3(0, -10, 0));
        lineRenderer.SetPosition(1, new Vector3(0, -10, 0));
    }

    void Update()
    {
        RotateTowardsMoveDirection();
        RandomizeLane();
        DrawPlatoonLine();
    }

    void RotateTowardsMoveDirection()
    {
        var lookPos = vehicleAgent.GetCurrentTargetNodePosition() - transform.parent.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.parent.rotation = Quaternion.Slerp(transform.parent.rotation, rotation, Time.deltaTime * rotationSmoothness);
    }

    void RandomizeLane()
    {
        if (communicationAgent.isInPlatoon) // Is in platoon
        {
            if (communicationAgent.isPlatoonLeader)
            {
                currentLaneRandomization = baseLaneRandomization;
            }
            else // Set randomization to the same as leader of platoon
            {
                var leaderAgent = GameObject.Find(communicationAgent.currentPlatoonData.leaderName);
                if (leaderAgent != null)
                {
                    currentLaneRandomization = leaderAgent.transform.GetChild(0).GetComponent<Visualizer>().currentLaneRandomization;
                }
                else
                {
                    currentLaneRandomization = baseLaneRandomization;
                }
                
            }
        }

        Vector3 targetModelPosition = new Vector3(currentLaneRandomization, transform.localPosition.y, transform.localPosition.z);
        model.transform.localPosition = Vector3.Lerp(transform.localPosition, targetModelPosition, Time.deltaTime * laneRandomizationSmoothness);
    }

    void DrawPlatoonLine()
    {
        if (communicationAgent.isInPlatoon) // Is in platoon
        {
            if (!communicationAgent.isPlatoonLeader)
            {
                var followAgent = GameObject.Find(communicationAgent.currentPlatoonData.followAgentName);

                if (followAgent != null)
                {
                    lineRenderer.enabled = true;
                    lineRenderer.SetPosition(0, transform.position);
                    lineRenderer.SetPosition(1, followAgent.transform.GetChild(0).position);
                }
                else
                {
                    lineRenderer.enabled = false;
                }
            }
            else
            {
                lineRenderer.enabled = false;
            }
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, communicationAgent.platoonJoinRadius * communicationAgent.simulationSpaceMultiplier);

        if (communicationAgent.target.HasValue)
        {
            Gizmos.DrawSphere(communicationAgent.target.Value, 0.02f);
        }
    }
}