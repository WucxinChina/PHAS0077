using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Utils;

public enum VehicleAgentState {
    SelfGuidedMoving,
    SystemGuidedMoving,
    Idling
}

public enum VehicleAgentPlatoonState
{
    CatchingUp,
    Waiting,
    Normal
}


// In this simulation the VehicleAgent communicates with CommunicationAgent using exposed system API, CommunicationAgent is a program installed in system computer

[ExecuteInEditMode]
public class VehicleAgent : MonoBehaviour
{
    [HideInInspector] public float simulationSpaceMultiplier = 0.01f; // To keep short distances in projection space and use real units in parameters we need to multiply real units by simulationSpace factor
                                                                      // So if eg. reachDestinationRadius is 10[meters] in simulation space (when displayed on the scene it is reachDestinationRadius * simulationSpaceMultiplier = 0.1

    [HideInInspector] public float simulationSpeedMultiplier = 0.01f; // = 0.002f; // To keep speeds at reasonable levels fitted for visualization we need to muliply real units by simulationSpeed factor
                                                                       // So if eg. baseSpeed is 100[km/h] in simulation space (when moving on the scene it is baseSpeed * simulationSpeedMultiplier = 0.2

    [ReadOnly] public VehicleAgentState state = VehicleAgentState.Idling;
    [ReadOnly] public VehicleAgentPlatoonState platoonState = VehicleAgentPlatoonState.Normal;
    [ReadOnly] public string startNodeName; // Entered by user via UI screen
    [ReadOnly] public string destinationNodeName; // Entered by user via UI screen
    [ReadOnly] public NullableVector3 destinationPosition;
    [ReadOnly] public TS_DijkstraPath path = null;

    [ReadOnly] public NullableVector3 target;
    [ReadOnly] public string currentTargetNodeName;
    [ReadOnly] public GameObject currentTargetNode;
    [ReadOnly] public Vector3 currentTargetNodeNamePosition;

    [ReadOnly] public float currentSpeed = 0;
    public float baseSpeed = 100.0f;
    public float systemSpeed = 100.0f;

    public float reachDestinationRadius = 10f; // Distance to target below which target is assumed as reached

    NavSystem navSystem;

    // System guided mode
    private CommunicationAgent communicationAgent;

    void Start()
    {
        navSystem = GameObject.Find("Map").GetComponent<NavSystem>();
        FindPath();
        state = VehicleAgentState.SelfGuidedMoving;
    }

    void Update()
    {
        Move();
    }

    public void SetUp(float baseSpeed)
    {
        this.baseSpeed = baseSpeed;
    }

    private void Move()
    {
        if (state != VehicleAgentState.Idling)
        {
            if (target.HasValue)
            {
                if (Vector3.Distance(transform.position, currentTargetNode.transform.position) < reachDestinationRadius * simulationSpaceMultiplier)
                {
                    int indexOfCurrentNode = path.nodes.IndexOf(currentTargetNode);
                    if (indexOfCurrentNode < path.nodes.Count - 1)
                    {
                        currentTargetNode = path.nodes[indexOfCurrentNode + 1];
                        currentTargetNodeName = currentTargetNode.name;
                        currentTargetNodeNamePosition = currentTargetNode.transform.position;
                    }
                    else // Reached destination
                    {
                        if (state == VehicleAgentState.SelfGuidedMoving)
                        {
                            EndRide();
                        }
                    }
                }

                if (state == VehicleAgentState.SelfGuidedMoving)
                {
                    currentSpeed = baseSpeed;
                    target = currentTargetNode.transform.position;

                    float distanceToDestination = Vector3.Distance(transform.position, destinationPosition.Value);
                    if (distanceToDestination < reachDestinationRadius * simulationSpaceMultiplier)
                    {
                        EndRide();
                    }
                }

                if (state == VehicleAgentState.SystemGuidedMoving)
                {
                    currentSpeed = systemSpeed;
                }

                transform.position = Vector3.MoveTowards(transform.position, target.Value, Time.deltaTime * currentSpeed * simulationSpeedMultiplier);
            }



        }
    }

    private void FindPath()
    {
        path = navSystem.GetPath(startNodeName, destinationNodeName);
        currentTargetNode = path.nodes[0];
        target = currentTargetNode.transform.position;
        destinationPosition = navSystem.GetNodePosition(destinationNodeName).Value;
    }

    private void OnMouseDown()
    {
         

    }

    // --- API ---

    public void ToggleSystemGuidedMode(bool status)
    { 
        if (status)
        {
            currentSpeed = systemSpeed;
            state = VehicleAgentState.SystemGuidedMoving;
        }
        else
        {
            // Recalculate path
            currentSpeed = baseSpeed;
            state = VehicleAgentState.SelfGuidedMoving;
        }
    }

    public void SetPlatoonState(VehicleAgentPlatoonState state)
    {
        platoonState = state;
    }

    public string GetDestinationNodeName()
    {
        return destinationNodeName;
    }

    public Vector3 GetCurrentTargetNodePosition()
    {
        return currentTargetNodeNamePosition;
    }

    public string GetCurrentTargetNodeName()
    {
        return currentTargetNodeName;
    }

    public Vector3? GetDestinationPosition()
    {
        return destinationPosition.Value;
    }

    public void DisconnectCommunicationAgent()
    {
        communicationAgent = null;
        state = VehicleAgentState.SelfGuidedMoving;     
    }

    public Vector3 GetTarget()
    {
        return target;
    }

    public VehicleAgent ConnectCommunicationAgent(CommunicationAgent communicationAgent)
    {
        this.communicationAgent = communicationAgent;
        return this;
    }

    public void SetSpeed(float speed)
    {
        systemSpeed = speed;
    }

    public float GetBaseSpeed()
    {
        return baseSpeed;
    }

    public List<string> GetPathNodesNames()
    {
        return path.nodes.Select(x => x.name).ToList();
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
    }

    public Vector3 GetVehiclePosition()
    {
        return transform.position;
    }

    public void EndRide()
    {
        Destroy(this.gameObject);
    }

    // --- OTHER ---

    void DebugLog(string message)
    {
        #if UNITY_EDITOR
        if (Selection.Contains(gameObject))
        {
            Debug.Log("Vehicle " + gameObject.name + ": " + message);
        }
        #endif
    }


}