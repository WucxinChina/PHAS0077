using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum CommunicationAgentState
{
    RegisteringInAgentPlatform,
    RegisteringInCentralAgent_Send,
    RegisteringInCentralAgent_Wait,
    ConnectingToVehicleAgent,
    CentralAgentInitialDataUpdate_Send,
    PlatoonCreateProposal_Wait,
    CreatePlatoonConfirmation_Wait,
    PlatoonSearching_Send,
    PlatoonSearching_Wait,
    JoiningPlatoon_Send,
    JoiningPlatoon_Wait,
    CreatingPlatoon_Send,
    CreatingPlatoon_Wait,
    CreatingNewPlatoon,
    MovingInPlatoon,
}

// What if request to create agent comes from other agent??

public class CommunicationAgent : Agent
{
    [HideInInspector] public float simulationSpaceMultiplier = 0.01f; // To keep short distances in projection space and use real units in parameters we need to multiply real units by simulationSpace factor;

    [Header("General Info")]
    [ReadOnly] public CommunicationAgentState state = CommunicationAgentState.RegisteringInAgentPlatform;
    [ReadOnly] public bool registeredInCentralAgent;

    [Header("Platoon Moving Info")]
    [ReadOnly] public bool isInPlatoon;
    [ReadOnly] public bool isStrictlyInPlatoon = false; // Is vehicle driving just behind other vehicle (For calculating fuel consumption)
    [ReadOnly] public bool isPlatoonLeader;
    [ReadOnly] public PlatoonData currentPlatoonData; // Data about the platoon in which vehicle is moving (sent by CommunicationAgent of platoon leader vehicle)
    [ReadOnly] public Vector3? target; // Data about target to which vehicle should be moving to keep platoon formation (sent by CommunicationAgent of vehicle in front)
    [ReadOnly] public string followingAgentTargetNodeName; // Next node on agent's that this agent follows path
    [ReadOnly] public string lastCommonPlatoonNodeName = "";
    [ReadOnly] public List<string> platoonVehiclesNames; // Only for leader
  
    private VehicleAgent vehicleAgent;
    private List<string> pendingAcceptingVehicles; // List of CommunicationAgents names that accepted platoon creation proposal

    [Header("Settings")]
    public float platoonJoinRadius = 100f; // Radius of detecting other platoons in proximity
    public float reachDestinationRadius = 10f; // Distance to target below which target is assumed as reached
    public string centralAgentName = "CentralAgent"; // This is supposed to be entered via UI by user (mobile app or on vehicle's cockpit screen)
    public float platoonSpeed = 100.0f; // Speed of driving in platoon
    public float waitForPlatoonSpeed = 70.0f; // Speed of vehicle when it needs to wait to be overtook by the platoon it joined
    public float catchUpPlatoonSpeed = 160.0f; // Speed of vehicle when it is further than catchUpPlatoonDistance from vehicle it is following, if distance is smaller then platoon speed is used
    public float catchUpPlatoonDistance = 15f; // Distance to the vehicle that should be followed above which vehicle drives with catchUpPlatoonSpeed
    public float betweenVehicleDistances = 10f; // Distance from vehicle in direction oposite to its driving direction (it should be similar to catchUpPlatoonDistance)
    public int maxPlatoonSize = 5; // Maximal number of vehicles in platoon

    [HideInInspector] public string vehicleAgentName = ""; // Entered via UI by user (mobile app or on vehicle's cockpit screen)
    [HideInInspector] public string vehicleAgentPassword = "";  // Entered via UI by user (mobile app or on vehicle's cockpit screen)

    List<(string, int)> platoonsInProximity; // Names of platoon leaders to join found when asking central agent, ordered on the stack from best to worst candidate in terms of most common nodes on the path (Name of leader and number of common points)
    List<string> lonelyVehiclesInProximity; // Names of communication agents not in platoons found when asking central agent
    string lastTriedToJoinLeaderName = ""; // Name of leader of the column that this agent tried to join last time
    bool lastTriedToJoin = false;

    [Header("Update Settings")]
    public float mainTimerIncrement = 1.0f;

    public float registeringInCentralAgent_Wait_Timeout = 10.0f;
    float registeringInCentralAgent_Wait_Timer = 0.0f;

    public float platoonSearching_Wait_Timeout = 1.3f;
    float platoonSearching_Wait_Timer = 0.0f;

    public float joiningPlatoon_Wait_Timeout = 1.0f;
    float joiningPlatoon_Wait_Timer = 0.0f;

    public float creatingPlatoon_Wait_Timeout = 0.8f;
    float creatingPlatoon_Wait_Timer = 0.0f;

    public float creatingPlatoonProposal_Wait_Timeout = 1.0f;
    float creatingPlatoonProposal_Wait_Timeout_Randomizer = 0.0f;
    float creatingPlatoonProposal_Wait_Timer = 0.0f;

    public float creatingPlatoonConfirmation_Wait_Timeout = 1.0f;
    float creatingPlatoonConfirmation_Wait_Timer = 0.0f;

    public float updateVehicleDataInCentralAgent_Timeout = 1.0f;
    float updateVehicleDataInCentralAgent_Timer = 0.0f;

    public float updateVehicleBehind_Timeout = 0.4f;
    float updateVehicleBehind_Timer = 0.0f;

    CommunicationAgentState[] setupStates = { 
        CommunicationAgentState.RegisteringInAgentPlatform, 
        CommunicationAgentState.RegisteringInCentralAgent_Send, 
        CommunicationAgentState.RegisteringInCentralAgent_Wait, 
        CommunicationAgentState.ConnectingToVehicleAgent,
        CommunicationAgentState.CentralAgentInitialDataUpdate_Send
    };

    void Start()
    {
        name = agentName;
        gameObject.name = agentName;

        platoonVehiclesNames = new List<string>();
        pendingAcceptingVehicles = new List<string>();
        platoonsInProximity = new List<(string, int)>();
        lonelyVehiclesInProximity = new List<string>();

        creatingPlatoonProposal_Wait_Timer = Random.Range(0.0f, creatingPlatoonProposal_Wait_Timeout); // Randomize timer starting point to avoid stagnation when multiple agents are created in the same time
   
    }

    void Update() // Each frame
    {
        MainLoop();
    }

    void MainLoop()
    {
        Message message = base.ReceiveMessage();
        Content receiveContent = message != null ? JsonUtility.FromJson<Content>(message.GetContent()) : null;

        // --- Setup states --- 

        // Register this agent in Agent Platform (needed to send messages)
        if (state == CommunicationAgentState.RegisteringInAgentPlatform)
        {
            var response = RegisterInAgentPlatform();
            if (response)
            {
                state = CommunicationAgentState.RegisteringInCentralAgent_Send;
            }

            return;
        }

        // Send registration request to Central Agent to register this agent in the System
        if (state == CommunicationAgentState.RegisteringInCentralAgent_Send)
        {
            string content = Utils.CreateContent(SystemAction.CommunicationAgent_RegisterInCentralAgent, "");
            base.SendMessage(Peformative.Request.ToString(), content, agentName, centralAgentName);
            state = CommunicationAgentState.RegisteringInCentralAgent_Wait;

            return;
        }

        // Wait for response for registration from Central Agent (Accept or Reject)
        if (state == CommunicationAgentState.RegisteringInCentralAgent_Wait)
        {
            // Read message from central agent
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_RegisterInCentralAgent.ToString())
            {
                if (message.GetPerformative() == nameof(Peformative.Accept))
                {
                    registeredInCentralAgent = true;
                    state = CommunicationAgentState.ConnectingToVehicleAgent;
                }
                else if (message.GetPerformative() == nameof(Peformative.Reject))
                {
                    state = CommunicationAgentState.RegisteringInCentralAgent_Send; // Try once again
                    return;
                }
            }
            
            // Wait for some time for answers then try to send once again
            if (registeringInCentralAgent_Wait_Timer >= registeringInCentralAgent_Wait_Timeout)
            {
                state = CommunicationAgentState.RegisteringInCentralAgent_Send;
                registeringInCentralAgent_Wait_Timer = 0;

                return;
            }
            else
            {
                registeringInCentralAgent_Wait_Timer += mainTimerIncrement * Time.deltaTime;
            }
        }

        // Connect to Vehicle Agent using its exposed API
        if (state == CommunicationAgentState.ConnectingToVehicleAgent)
        {
            ConnectToVehicleAgent(vehicleAgentName, vehicleAgentPassword);
            state = CommunicationAgentState.CentralAgentInitialDataUpdate_Send;
            return;
        }

        // Send initial vehicle data update to Central Agent
        if (state == CommunicationAgentState.CentralAgentInitialDataUpdate_Send)
        {
            UpdateVehicleDataInCentralAgent();
            state = CommunicationAgentState.PlatoonCreateProposal_Wait;

            return;
        }

        // --- Main states ---

        // Wait for platoon create proposals from other agents, respond with accept or reject
        if (state == CommunicationAgentState.PlatoonCreateProposal_Wait)
        {
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_CreatePlatoon.ToString())
            {
                if (message.GetPerformative() == Peformative.Propose.ToString())
                {
                    PlatoonCreateData platoonVehicleData = JsonUtility.FromJson<PlatoonCreateData>(receiveContent.contentDetails);

                    // If platoon goes in the same direction then accept the proposal
                    List<string> path = vehicleAgent.GetPathNodesNames();
                    string currentNode = vehicleAgent.GetCurrentTargetNodeName();
                    
                    List<string> currentPath = path.GetRange(path.IndexOf(currentNode) - 1, path.Count - path.IndexOf(currentNode) + 1); // Nodes which are left on the path (has not been reached yet) (and last visited node)
                    List<string> currentPlatoonPath = platoonVehicleData.pathNodesNames.GetRange(platoonVehicleData.pathNodesNames.IndexOf(platoonVehicleData.currentTargetNodeName) - 1, platoonVehicleData.pathNodesNames.Count - platoonVehicleData.pathNodesNames.IndexOf(platoonVehicleData.currentTargetNodeName) + 1); // Nodes which are left on the path (has not been reached yet)

                    // Calculate common points count in the same direction
                    List<string> pathLeft1 = currentPlatoonPath.Count >= currentPath.Count ? currentPlatoonPath : currentPath;
                    List<string> pathLeft2 = currentPlatoonPath.Count < currentPath.Count ? currentPlatoonPath : currentPath;
                    int pathCurrentIndex = 0;
                    for (int i = 0; i < pathLeft1.Count; i++) // Iterate longer list and count same items along the way on the shorter list
                    {
                        if (pathCurrentIndex <= pathLeft2.Count - 1) // Check if index is in range
                        {
                            if (pathLeft1[i] == pathLeft2[pathCurrentIndex])
                            {
                                pathCurrentIndex++;
                            }
                        }
                    }
                    int commonPointsCount = pathCurrentIndex;
                    
                    if (commonPointsCount > 1) // Have common points and same direction so accept proposal
                    {
                        string content = Utils.CreateContent(SystemAction.CommunicationAgent_CreatePlatoon, "");
                        base.SendMessage(Peformative.Accept.ToString(), content, agentName, message.GetSender());
                        state = CommunicationAgentState.CreatePlatoonConfirmation_Wait;
                        creatingPlatoonProposal_Wait_Timer = 0;
                    }
                    else // Not same direction so reject proposal
                    {
                        string content = Utils.CreateContent(SystemAction.CommunicationAgent_CreatePlatoon, "");
                        base.SendMessage(Peformative.Reject.ToString(), content, agentName, message.GetSender());
                    }

                    return;
                }
            }
            
            // Wait for some time for answers then try to find or create platoon
            if (creatingPlatoonProposal_Wait_Timer >= creatingPlatoonProposal_Wait_Timeout + creatingPlatoonProposal_Wait_Timeout_Randomizer)
            {
                state = CommunicationAgentState.PlatoonSearching_Send;
                creatingPlatoonProposal_Wait_Timer = 0;

                // It may happen that multiple Communication Agents will fall into CreatePlatoonProposal_Wait-CreatingPlatoon_Send loop at the same time
                // In such case they will never form a platoon because they will be asking each other and ignoring proposal messages
                // So the job of this timer is to prevent such situation by randomly delaying CreatingPlatoonProposal_Wait
                creatingPlatoonProposal_Wait_Timeout_Randomizer = (Random.Range(0, 1) == 1) ? creatingPlatoonProposal_Wait_Timeout * 2.0f : -creatingPlatoonProposal_Wait_Timeout / 2.0f;

                return;
            }
            else
            {
                creatingPlatoonProposal_Wait_Timer += mainTimerIncrement * Time.deltaTime;
            }
        }
        // Reject all proposals (because now agent is in different state)
        else
        {
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_CreatePlatoon.ToString())
            {
                if (message.GetPerformative() == Peformative.Propose.ToString())
                {
                    string content = Utils.CreateContent(SystemAction.CommunicationAgent_CreatePlatoon, "");
                    base.SendMessage(Peformative.Reject.ToString(), content, agentName, message.GetSender());
                }
            }          
        }

        // Wait for platoon create confirmation from future platoon leader (the one that has send the proposal) when proposal has been accepted
        if (state == CommunicationAgentState.CreatePlatoonConfirmation_Wait)
        {
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_CreatePlatoon.ToString())
            {
                if (message.GetPerformative() == Peformative.Confirm.ToString())
                {
                    currentPlatoonData = JsonUtility.FromJson<PlatoonData>(receiveContent.contentDetails); // Receive and store info about this platoon
                    isInPlatoon = true;
                    lastCommonPlatoonNodeName = LastCommonNodeOnPath();
                    state = CommunicationAgentState.MovingInPlatoon;
                    creatingPlatoonConfirmation_Wait_Timer = 0;
                        
                    return;
                }
            }
            
            // Wait for some time for answers then try to find or create platoon
            if (creatingPlatoonConfirmation_Wait_Timer >= creatingPlatoonConfirmation_Wait_Timeout)
            {
                state = CommunicationAgentState.PlatoonSearching_Send;
                creatingPlatoonConfirmation_Wait_Timer = 0;

                return;
            }
            else
            {
                creatingPlatoonConfirmation_Wait_Timer += mainTimerIncrement * Time.deltaTime;
            }
        }

        // Send query to Central Agent to find platoons and lonely vehicles in proximity
        if (state == CommunicationAgentState.PlatoonSearching_Send)
        {
            string content = Utils.CreateContent(SystemAction.CommunicationAgent_NearbyVehicles, (platoonJoinRadius * simulationSpaceMultiplier).ToString());
            base.SendMessage(Peformative.Query.ToString(), content, agentName, centralAgentName);
            state = CommunicationAgentState.PlatoonSearching_Wait;

            return;
        }

        // Wait for response with platoons and lonely vehicles in proximity from Central Agent, and find best platoon to join or create new if not platoons in proximity
        if (state == CommunicationAgentState.PlatoonSearching_Wait) 
        {
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_NearbyVehicles.ToString()) 
            {
                if (message.GetPerformative() == Peformative.Inform.ToString())
                {
                    PlatoonQueryData platoonVehicleData = JsonUtility.FromJson<PlatoonQueryData>(receiveContent.contentDetails);

                    DebugLog(platoonVehicleData.platoonLeaderCommunicationAgents.Count + " - " + platoonVehicleData.lonelyCommunicationAgents.Count);

                    // If platoon found and last time agent tried to create column (not find it) - try to join
                    {
                        if (platoonVehicleData.platoonLeaderCommunicationAgents.Count > 0 && lastTriedToJoin == false)
                        {
                            // First find platoons that have the greatest number of common nodes with path of this agent and are going in the same direction
                            // Store them ordered in list, if list is not empty then call best agent, remove it from list and wait for response. 
                            // If list is emptied ask central agent again for platoons in proximity
                            
                            List<string> path = vehicleAgent.GetPathNodesNames();
                            string currentNode = vehicleAgent.GetCurrentTargetNodeName();
                            List<string> currentPath = path.GetRange(path.IndexOf(currentNode) - 1, path.Count - path.IndexOf(currentNode) + 1); // Nodes which are left on the path (has not been reached yet) (and last visited node)
                          
                            foreach (var agent in platoonVehicleData.platoonLeaderCommunicationAgents)
                            {
                                List<string> currentPlatoonPath = agent.pathNodesNames.GetRange(agent.pathNodesNames.IndexOf(agent.currentTargetNodeName) - 1, agent.pathNodesNames.Count - agent.pathNodesNames.IndexOf(agent.currentTargetNodeName) + 1); // Nodes which are left on the path (has not been reached yet)

                                // Calculate common points count in the same direction
                                List<string> pathLeft1 = currentPlatoonPath.Count >= currentPath.Count ? currentPlatoonPath : currentPath;
                                List<string> pathLeft2 = currentPlatoonPath.Count < currentPath.Count ? currentPlatoonPath : currentPath;
                                int pathCurrentIndex = 0;
                                for (int i = 0; i < pathLeft1.Count; i++) // Iterate longer list and count same items along the way on the shorter list
                                {
                                    if (pathCurrentIndex <= pathLeft2.Count - 1) // Check if index is in range
                                    {
                                        if (pathLeft1[i] == pathLeft2[pathCurrentIndex])
                                        {
                                            pathCurrentIndex++;
                                        }
                                    }
                                }
                                int commonPointsCount = pathCurrentIndex;
                                if (commonPointsCount > 1)
                                {
                                    platoonsInProximity.Add((agent.name, commonPointsCount));
                                }     
                            }

                            if (platoonsInProximity != null && platoonsInProximity.Count > 0)
                            {
                                platoonsInProximity.OrderBy(x => x.Item2); // Order list in terms of greatest number of common points
                                state = CommunicationAgentState.JoiningPlatoon_Send;
                                lastTriedToJoin = true;
                                return;
                            }
                        }
                    }

                    // If no platoons found but some lonely vehicles found - try to form new platoon, send proposal to all nearby vehicles
                    {
                        platoonVehicleData.lonelyCommunicationAgents.Sort((x, y) => x.distance.CompareTo(y.distance)); // Sort by distance
                        if (platoonVehicleData.lonelyCommunicationAgents.Count > 0)
                        {
                            lonelyVehiclesInProximity = platoonVehicleData.lonelyCommunicationAgents.Select(x => x.name).ToList();  
                            state = CommunicationAgentState.CreatingPlatoon_Send;
                            lastTriedToJoin = false;
                            return;
                        }
                    } 
                }
            }
            
            // Wait for some time for answers then try to send once again
            if (platoonSearching_Wait_Timer >= platoonSearching_Wait_Timeout)
            {
                state = CommunicationAgentState.PlatoonCreateProposal_Wait;
                platoonSearching_Wait_Timer = 0;
            }
            else
            {
                platoonSearching_Wait_Timer += mainTimerIncrement * Time.deltaTime;
            }
        }

        // Send join request to the best platoon candidate from list and remove it from list
        if (state == CommunicationAgentState.JoiningPlatoon_Send)
        {
            //string platoonLeaderName = platoonsInProximity.Where(o => o.Item1 != lastTriedToJoinLeaderName).First().Item1;
            string platoonLeaderName = platoonsInProximity[platoonsInProximity.Count - 1].Item1;
            platoonsInProximity.RemoveAt(platoonsInProximity.Count - 1);

            //lastTriedToJoinLeaderName = platoonLeaderName;

            // Send request to join the platoon
            string content = Utils.CreateContent(SystemAction.CommunicationAgent_JoinPlatoon, "");
            base.SendMessage(Peformative.Request.ToString(), content, agentName, platoonLeaderName);
            state = CommunicationAgentState.JoiningPlatoon_Wait;
        }

        // Wait for response from platoon leader (if no message coming or all were rejecting then start waiting for creating proposals again) and join platoon if receive acceptation
        if (state == CommunicationAgentState.JoiningPlatoon_Wait) 
        {
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_JoinPlatoon.ToString()) // Receive decision about joining platoon from its leader
            {
                if (message.GetPerformative() == Peformative.Accept.ToString()) // Join platoon
                {
                    currentPlatoonData = JsonUtility.FromJson<PlatoonData>(receiveContent.contentDetails); // Receive and store info about this platoon
                    isInPlatoon = true;
                    platoonsInProximity.Clear();
                    lastCommonPlatoonNodeName = LastCommonNodeOnPath();
                    state = CommunicationAgentState.MovingInPlatoon;

                    return;
                }
                if (message.GetPerformative() == Peformative.Reject.ToString())
                {
                    if (platoonsInProximity.Count > 0)
                    {
                        state = CommunicationAgentState.JoiningPlatoon_Send;
                    }
                    else
                    {
                        state = CommunicationAgentState.PlatoonCreateProposal_Wait;
                    }

                    joiningPlatoon_Wait_Timer = 0;
                    return;
                }
            }
            
            // Wait for some time for answers then try to find or create platoon again
            if (joiningPlatoon_Wait_Timer >= joiningPlatoon_Wait_Timeout)
            {
                if (platoonsInProximity.Count > 0)
                {
                    state = CommunicationAgentState.JoiningPlatoon_Send;
                }
                else
                {
                    state = CommunicationAgentState.PlatoonCreateProposal_Wait;
                }
                joiningPlatoon_Wait_Timer = 0;
                return;
            }
            else
            {
                joiningPlatoon_Wait_Timer += mainTimerIncrement * Time.deltaTime;
            }       
        }

        // Send create new platoon proposal to all lonely vehicles in proximity (discovered earlier via querying the Central Agent)
        if (state == CommunicationAgentState.CreatingPlatoon_Send)
        {
            PlatoonCreateData platoonCreateData = new PlatoonCreateData()
            {
                leaderName = agentName,
                pathNodesNames = vehicleAgent.GetPathNodesNames(),
                currentTargetNodeName = vehicleAgent.GetCurrentTargetNodeName()
            };
            string contentDetails = JsonUtility.ToJson(platoonCreateData);
            string content = Utils.CreateContent(SystemAction.CommunicationAgent_CreatePlatoon, contentDetails);
            lonelyVehiclesInProximity = lonelyVehiclesInProximity.Count() > maxPlatoonSize - 1 ? lonelyVehiclesInProximity.GetRange(0, maxPlatoonSize - 1) : lonelyVehiclesInProximity; // Limit number of vehicles in platoon
            foreach (var agent in lonelyVehiclesInProximity)
            {
                base.SendMessage(Peformative.Propose.ToString(), content, agentName, agent);
            }
            

            state = CommunicationAgentState.CreatingPlatoon_Wait;
        }

        // Wait for response from lonely vehicles in proximity and create new platoon (if no message coming or all were rejecting then start waiting for platoon creating proposals again) 
        if (state == CommunicationAgentState.CreatingPlatoon_Wait)
        {
            // Receive decision about joining this platoon from lonely vehicles
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_CreatePlatoon.ToString())
            {
                if (message.GetPerformative() == Peformative.Accept.ToString())
                {
                    pendingAcceptingVehicles.Add(message.GetSender());
                }
            }
            
            // Wait for some time for answers then if there are answers create platoon, if there are not wait for creating platoon proposals again
            if (creatingPlatoon_Wait_Timer >= creatingPlatoon_Wait_Timeout || pendingAcceptingVehicles.Count == lonelyVehiclesInProximity.Count)
            {
                if (pendingAcceptingVehicles.Count > 0)
                {
                    platoonVehiclesNames.Add(agentName); // Add self to list of vehicles in platoon

                    for (int i = 0; i < pendingAcceptingVehicles.Count; i++)
                    {            
                        PlatoonData platoonData = new PlatoonData()
                        {
                            leaderName = agentName,
                            pathNodesNames = vehicleAgent.GetPathNodesNames(),
                            followAgentName = platoonVehiclesNames[platoonVehiclesNames.Count - 1], // Follow last vehicle
                            behindAgentName = i + 1 <= pendingAcceptingVehicles.Count - 1 ? pendingAcceptingVehicles[i+1] : "" // Vehicle behind or nothing if i is last
                        };

                        string contentDetails = JsonUtility.ToJson(platoonData);
                        string content = Utils.CreateContent(SystemAction.CommunicationAgent_CreatePlatoon, contentDetails);
                        base.SendMessage(Peformative.Confirm.ToString(), content, agentName, pendingAcceptingVehicles[i]);
                        platoonVehiclesNames.Add(pendingAcceptingVehicles[i]);
                    }

                    // Create new platoon and add itself to it as leader
                    isInPlatoon = true;
                    isPlatoonLeader = true;
                    currentPlatoonData = new PlatoonData()
                    {
                        leaderName = agentName,
                        pathNodesNames = vehicleAgent.GetPathNodesNames(),
                        followAgentName = "",
                        behindAgentName = platoonVehiclesNames[1]
                    };
                    lastCommonPlatoonNodeName = currentPlatoonData.pathNodesNames[currentPlatoonData.pathNodesNames.Count - 1];
                    pendingAcceptingVehicles.Clear();
                    creatingPlatoon_Wait_Timer = 0;
                    state = CommunicationAgentState.MovingInPlatoon;

                    lonelyVehiclesInProximity.Clear();

                    return;
                }
                else
                {
                    lonelyVehiclesInProximity.Clear();

                    creatingPlatoon_Wait_Timer = 0;
                    state = CommunicationAgentState.PlatoonCreateProposal_Wait;

                    return;
                }
            }
            else
            {
                creatingPlatoon_Wait_Timer += mainTimerIncrement * Time.deltaTime;
            }
        }

        // Moving in platoon
        if (state == CommunicationAgentState.MovingInPlatoon)
        {
            vehicleAgent.ToggleSystemGuidedMode(true);
                        
            if (isPlatoonLeader)
            {
                // Respond to join platoon requests
                if (message != null && receiveContent.action == SystemAction.CommunicationAgent_JoinPlatoon.ToString())
                {
                    if (message.GetPerformative() == Peformative.Request.ToString())
                    {
                        // Accept
                        if (platoonVehiclesNames.Count < maxPlatoonSize)
                        {
                            // Update data in last vehicle in the platoon
                            {
                                PlatoonData platoonData = new PlatoonData()
                                {
                                    leaderName = agentName,
                                    pathNodesNames = vehicleAgent.GetPathNodesNames(),
                                    followAgentName = platoonVehiclesNames[platoonVehiclesNames.Count - 2], // Same as before
                                    behindAgentName = message.GetSender() // New
                                };
                                string contentDetails = JsonUtility.ToJson(platoonData);
                                string content = Utils.CreateContent(SystemAction.CommunicationAgent_UpdatePlatoon, contentDetails);
                                base.SendMessage(Peformative.Inform.ToString(), content, agentName, platoonVehiclesNames[platoonVehiclesNames.Count - 1]);
                            }

                            // Add new vehicle to platoon
                            {
                                platoonVehiclesNames.Add(message.GetSender());
                                PlatoonData platoonData = new PlatoonData()
                                {
                                    leaderName = agentName,
                                    pathNodesNames = vehicleAgent.GetPathNodesNames(),
                                    followAgentName = platoonVehiclesNames[platoonVehiclesNames.Count - 2], // Follow last vehicle (except self)
                                    behindAgentName = "" // Nothing is behind
                                };

                                string contentDetails = JsonUtility.ToJson(platoonData);
                                string content = Utils.CreateContent(SystemAction.CommunicationAgent_JoinPlatoon, contentDetails);
                                base.SendMessage(Peformative.Accept.ToString(), content, agentName, message.GetSender());
                            }
                        }
                        // Reject request if platoon size reached the limit
                        else
                        {
                            string content = Utils.CreateContent(SystemAction.CommunicationAgent_JoinPlatoon, "");
                            base.SendMessage(Peformative.Reject.ToString(), content, agentName, message.GetSender());  
                        }   
                    }
                }
                
                // Respond to platoon leave by other vehicle
                if (message != null && receiveContent.action == SystemAction.CommunicationAgent_LeavePlatoon_NotifyLeader.ToString())
                {
                    if (message.GetPerformative() == Peformative.Inform.ToString())
                    {
                        // Remove vehicle from platoon and notify other vehicles (update their data about follow and behind VehicleAgents)
                        {
                            // Delete platoon (since only this agent and agent that is about to be removed are left)
                            if (platoonVehiclesNames.Count <= 2)
                            {
                                currentPlatoonData = null;
                                isInPlatoon = false;
                                platoonVehiclesNames.Clear();
                                lastCommonPlatoonNodeName = "";
                                state = CommunicationAgentState.PlatoonCreateProposal_Wait;

                                return;
                            }

                            int leavingVehicleIndex = platoonVehiclesNames.IndexOf(message.GetSender());

                            // Update data in vehicle in front of leaving vehicle
                            {
                                if (leavingVehicleIndex - 1 != 0) //  if vehicle is not this agent (leader)
                                {
                                    PlatoonData platoonData = new PlatoonData()
                                    {
                                        leaderName = agentName,
                                        pathNodesNames = vehicleAgent.GetPathNodesNames(),
                                        followAgentName = leavingVehicleIndex - 2 >= 0 ? platoonVehiclesNames[leavingVehicleIndex - 2] : "", // Same as before leave
                                        behindAgentName = leavingVehicleIndex + 1 <= platoonVehiclesNames.Count - 1 ? platoonVehiclesNames[leavingVehicleIndex + 1] : "" // New
                                    };
                                    string contentDetails = JsonUtility.ToJson(platoonData);
                                    string content = Utils.CreateContent(SystemAction.CommunicationAgent_UpdatePlatoon, contentDetails);
                                    base.SendMessage(Peformative.Inform.ToString(), content, agentName, platoonVehiclesNames[leavingVehicleIndex - 1]);
                                }
                                else // if vehicle is this agent (leader)
                                {
                                    currentPlatoonData.behindAgentName = platoonVehiclesNames[leavingVehicleIndex + 1];
                                }
                            }

                            // Update data in vehicle behind leaving vehicle if any
                            {
                                if (leavingVehicleIndex != platoonVehiclesNames.Count - 1) // Check if there is vehicle behind
                                {
                                    PlatoonData platoonData = new PlatoonData()
                                    {
                                        leaderName = agentName,
                                        pathNodesNames = vehicleAgent.GetPathNodesNames(),
                                        followAgentName = leavingVehicleIndex - 1 >= 0 ? platoonVehiclesNames[leavingVehicleIndex - 1] : "", // New
                                        behindAgentName = leavingVehicleIndex + 2 <= platoonVehiclesNames.Count - 1 ? platoonVehiclesNames[leavingVehicleIndex + 2] : "" // Same as before leave
                                    };
                                    string contentDetails = JsonUtility.ToJson(platoonData);
                                    string content = Utils.CreateContent(SystemAction.CommunicationAgent_UpdatePlatoon, contentDetails);
                                    base.SendMessage(Peformative.Inform.ToString(), content, agentName, platoonVehiclesNames[leavingVehicleIndex + 1]);
                                }      
                            }

                            // Remove leaving vehicle from list
                            platoonVehiclesNames.RemoveAt(leavingVehicleIndex);
                        }
                    }
                }

                vehicleAgent.SetTarget(vehicleAgent.GetCurrentTargetNodePosition()); // Just follow its path, node by node because for the leader there is no agent in front to follow
                vehicleAgent.SetSpeed(platoonSpeed);
                isStrictlyInPlatoon = true;
            }
            else
            {
                // Receive data updates from the Communication Agent in the vehicle in front and update target position based on it
                if (message != null && receiveContent.action == SystemAction.CommunicationAgent_UpdateVehicleBehind.ToString())
                {
                    if (message.GetPerformative() == Peformative.Inform.ToString())
                    {
                       

                        PlatoonUpdateData platoonUpdateData = JsonUtility.FromJson<PlatoonUpdateData>(receiveContent.contentDetails);
                        target = platoonUpdateData.position;
                        followingAgentTargetNodeName = platoonUpdateData.targetNodeName;

                        DebugLog("Up Received " + platoonUpdateData.targetNodeName);

                        if (followingAgentTargetNodeName != vehicleAgent.GetCurrentTargetNodeName()) 
                        {
                            vehicleAgent.SetTarget(vehicleAgent.GetCurrentTargetNodePosition()); // Go to node, do not follow agent (without this vehicle will leave path when following agent will be on different edge)
                        }
                        else
                        {
                            Vector3 toTarget = (target.Value - vehicleAgent.GetVehiclePosition()).normalized;
                            if (Vector3.Dot(toTarget, transform.forward) > 0) // If target is in front (direction to target node) then follow it
                            {
                                vehicleAgent.SetTarget(target.Value); // Follow agent

                                if (Vector3.Distance(vehicleAgent.GetVehiclePosition(), target.Value) > catchUpPlatoonDistance * simulationSpaceMultiplier)
                                {
                                    vehicleAgent.SetSpeed(catchUpPlatoonSpeed);
                                    isStrictlyInPlatoon = false;
                                }
                                else
                                {
                                    vehicleAgent.SetSpeed(platoonSpeed);
                                    isStrictlyInPlatoon = true;
                                }
                            }
                            // Slow down and move to current target node waiting to be overtook by platoon
                            else
                            {
                                vehicleAgent.SetTarget(vehicleAgent.GetCurrentTargetNodePosition());
                                vehicleAgent.SetSpeed(waitForPlatoonSpeed);
                                isStrictlyInPlatoon = false;
                            }
                            
                        }
                        
                    }
                }

                // Respond to data update sent by leader when vehicle other left then platoon
                if (message != null && receiveContent.action == SystemAction.CommunicationAgent_UpdatePlatoon.ToString())
                {
                    if (message.GetPerformative() == Peformative.Inform.ToString())
                    {
                        currentPlatoonData = JsonUtility.FromJson<PlatoonData>(receiveContent.contentDetails);
                        lastCommonPlatoonNodeName = LastCommonNodeOnPath(); // Recalculate last common node
                    }
                }
                
                // Respond to transfer leadership request
                if (message != null && receiveContent.action == SystemAction.CommunicationAgent_LeavePlatoon_TransferLeadership.ToString())
                {
                    if (message.GetPerformative() == Peformative.Request.ToString())
                    {
                        List<string> platoonVehiclesNames = JsonUtility.FromJson<StringList>(receiveContent.contentDetails).list; // Get list of names of vehicles in platoon from message
                        
                        // Delete platoon (since only previous leader and this vehicle are left, and previous leader is leaving)
                        if (platoonVehiclesNames.Count <= 2)
                        {
                            isInPlatoon = false;
                            isStrictlyInPlatoon = false;
                            currentPlatoonData = null;
                            platoonVehiclesNames.Clear();
                            lastCommonPlatoonNodeName = "";
                            state = CommunicationAgentState.PlatoonCreateProposal_Wait;
                            return;
                        }
                        // Take over leadership
                        else
                        {
                            this.platoonVehiclesNames = platoonVehiclesNames;
                            this.platoonVehiclesNames.Remove(message.GetSender()); // Remove old leader
                            isPlatoonLeader = true;

                            // Update self
                            currentPlatoonData = new PlatoonData()
                            {
                                leaderName = agentName,
                                pathNodesNames = vehicleAgent.GetPathNodesNames(),
                                followAgentName = "",
                                behindAgentName = platoonVehiclesNames[1]
                            };

                            // Update data in all CommunicationAgents left
                            for (int i = 1; i < platoonVehiclesNames.Count; i++)
                            {
                                PlatoonData platoonData = new PlatoonData()
                                {
                                    leaderName = agentName,
                                    pathNodesNames = vehicleAgent.GetPathNodesNames(),
                                    followAgentName = platoonVehiclesNames[i - 1],
                                    behindAgentName = (i + 1) <= platoonVehiclesNames.Count - 1 ? platoonVehiclesNames[i + 1] : ""
                                };
                                string contentDetails = JsonUtility.ToJson(platoonData);
                                string content = Utils.CreateContent(SystemAction.CommunicationAgent_UpdatePlatoon, contentDetails);
                                base.SendMessage(Peformative.Inform.ToString(), content, agentName, platoonVehiclesNames[i]);
                            }

                            return;
                        }    
                    }
                }
            }

            // Send data updates to vehicle behind
            if (updateVehicleBehind_Timer >= updateVehicleBehind_Timeout)
            {
                if(currentPlatoonData.behindAgentName != "")
                {
                    Vector3 moveDirection = (vehicleAgent.GetTarget() - vehicleAgent.GetVehiclePosition()).normalized;

                    PlatoonUpdateData platoonUpdateData = new PlatoonUpdateData()
                    {
                        position = vehicleAgent.GetVehiclePosition() - moveDirection * betweenVehicleDistances * simulationSpaceMultiplier,
                        targetNodeName = vehicleAgent.GetCurrentTargetNodeName()
                    };
                    string content = Utils.CreateContent(SystemAction.CommunicationAgent_UpdateVehicleBehind, JsonUtility.ToJson(platoonUpdateData));
                    base.SendMessage(Peformative.Inform.ToString(), content, agentName, currentPlatoonData.behindAgentName);
                    updateVehicleBehind_Timer = 0;

                    DebugLog("UpSend " + platoonUpdateData.targetNodeName);

                }     
            }
            else
            {
                updateVehicleBehind_Timer += mainTimerIncrement * Time.deltaTime;
            }

        }
        // When not in platoon just follow the calculated path
        else
        {
            try
            {
                vehicleAgent.SetTarget(vehicleAgent.GetCurrentTargetNodePosition()); // Just follow its path, node by node
            }
            catch
            {
                //DebugLog("Test");
            }
        }

        // --- Other ---

        // Update vehicle data in Central Agent
        {
            if (!setupStates.Contains(state)) // Is not in any setup state
            {           
                if (updateVehicleDataInCentralAgent_Timer >= updateVehicleDataInCentralAgent_Timeout)
                {
                    UpdateVehicleDataInCentralAgent();
                    updateVehicleDataInCentralAgent_Timer = 0;
                }
                else
                {
                    updateVehicleDataInCentralAgent_Timer += mainTimerIncrement * Time.deltaTime;
                }
            }
        }

        // Reach current target
        {
            if (!setupStates.Contains(state)) // Is not in any setup state
            {
                float distanceToCurrentTargetNode = Vector3.Distance(vehicleAgent.GetVehiclePosition(), vehicleAgent.GetCurrentTargetNodePosition());
                if (distanceToCurrentTargetNode < reachDestinationRadius * simulationSpaceMultiplier)
                {
                    if (vehicleAgent.GetCurrentTargetNodeName() == vehicleAgent.GetDestinationNodeName()) // Reaching destination, leave platoon and end ride
                    {
                        if (state == CommunicationAgentState.MovingInPlatoon)
                        {
                            // Hand over the leadership to vehicle behind
                            if (isPlatoonLeader)
                            {
                                StringList stringList = new StringList() { list = platoonVehiclesNames };

                                string contentDetails = JsonUtility.ToJson(stringList);
                                string content = Utils.CreateContent(SystemAction.CommunicationAgent_LeavePlatoon_TransferLeadership, contentDetails);
                                base.SendMessage(Peformative.Request.ToString(), content, agentName, currentPlatoonData.behindAgentName);
                            }
                            // Notify leader about leaving
                            else
                            {
                                string content = Utils.CreateContent(SystemAction.CommunicationAgent_LeavePlatoon_NotifyLeader, "");
                                base.SendMessage(Peformative.Inform.ToString(), content, agentName, currentPlatoonData.leaderName);
                            }
                        }

                        // Deregister agent in Central Agent
                        {
                            string content = Utils.CreateContent(SystemAction.CommunicationAgent_UnregisterInCentralAgent, "");
                            base.SendMessage(Peformative.Request.ToString(), content, agentName, centralAgentName);
                        }

                        // Deregister agent in Agent Platform
                        {
                            //Debug.Log("Deregister" + agentName);
                            base.DeregisterInAgentPlatform();
                        }

                        vehicleAgent.EndRide();
                    }
                    // Reaching other node
                    else 
                    {
                        if (vehicleAgent.GetCurrentTargetNodeName() == lastCommonPlatoonNodeName) // Reaching node last common node with platoon, leave platoon and go other side
                        {
                            // Hand over the leadership to vehicle behind
                            if (isPlatoonLeader)
                            {
                                StringList stringList = new StringList() { list = platoonVehiclesNames };

                                string contentDetails = JsonUtility.ToJson(stringList);
                                string content1 = Utils.CreateContent(SystemAction.CommunicationAgent_LeavePlatoon_TransferLeadership, contentDetails);
                                base.SendMessage(Peformative.Request.ToString(), content1, agentName, currentPlatoonData.behindAgentName);

                                isPlatoonLeader = false;
                                isInPlatoon = false;
                                isStrictlyInPlatoon = false;
                                currentPlatoonData = null;
                                platoonVehiclesNames.Clear();
                                lastCommonPlatoonNodeName = "";
                            }
                            // Just leave platoon
                            else
                            {
                                string content = Utils.CreateContent(SystemAction.CommunicationAgent_LeavePlatoon_NotifyLeader, "");
                                base.SendMessage(Peformative.Inform.ToString(), content, agentName, currentPlatoonData.leaderName);

                                isInPlatoon = false;
                                isStrictlyInPlatoon = false;
                                currentPlatoonData = null;
                                platoonVehiclesNames.Clear();
                                lastCommonPlatoonNodeName = "";
                            }
                            
                            state = CommunicationAgentState.PlatoonCreateProposal_Wait;

                            return;
                        }
                    }

                }
            }
        }
       
    }

    void UpdateVehicleDataInCentralAgent()
    {
        VehicleUpdateData vehicleUpdateData = new VehicleUpdateData()
        {
            position = vehicleAgent.GetVehiclePosition(),
            inPlatoon = isInPlatoon,
            isPlatoonLeader = isPlatoonLeader,
            destinationNodeName = vehicleAgent.GetDestinationNodeName(),
            pathNodeNames = vehicleAgent.GetPathNodesNames(),
            currentTargetNodeName = vehicleAgent.GetCurrentTargetNodeName(),
            platoonVehiclesNames = isPlatoonLeader ? platoonVehiclesNames : null // Only for leader
        };

        string contentDetails = JsonUtility.ToJson(vehicleUpdateData);
        string content = Utils.CreateContent(SystemAction.CommunicationAgent_UpdateInCentralAgent, contentDetails);
        base.SendMessage(Peformative.Inform.ToString(), content, agentName, centralAgentName);     
    }

    void ConnectToVehicleAgent(string name, string password)
    {
        vehicleAgent = gameObject.GetComponent<VehicleAgent>().ConnectCommunicationAgent(this); // Connect to VehicleAgent API which is located in the vehicle system
        vehicleAgent.ToggleSystemGuidedMode(true);  
    }

    void DebugLog(string message)
    {
        #if UNITY_EDITOR
        if (Selection.Contains(gameObject))
        {
            Debug.Log(agentName + ": " + message);
        }
        #endif

    }

    string LastCommonNodeOnPath()
    {
        var path = vehicleAgent.GetPathNodesNames();
        string result = "";
        for (int i = 0; i < path.Count; i++)
        {
            if (currentPlatoonData.pathNodesNames.Contains(path[i]))
            {
                result = path[i];
            }   
        }

        return result;
    }
}
