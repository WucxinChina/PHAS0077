using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class CentralAgent : Agent
{
    public Dictionary<string, VehicleUpdateData> communicationAgents;
  
    void Start()
    {
        name = agentName;
        communicationAgents = new Dictionary<string, VehicleUpdateData>();
        RegisterInAgentPlatform();           
    }

    void Update()
    {
        while (messageQueue.Count > 0) {

            Message message = base.ReceiveMessage();
            Content receiveContent = message != null ? JsonUtility.FromJson<Content>(message.GetContent()) : null;

            // Process register requests
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_RegisterInCentralAgent.ToString())
            {
                if (message.GetPerformative() == Peformative.Request.ToString())
                {
                    // Add new agent
                    if (!communicationAgents.ContainsKey(message.GetSender()))
                    {
                        communicationAgents.Add(message.GetSender(), null);
                    }

                    // Send message
                    string content = Utils.CreateContent(SystemAction.CommunicationAgent_RegisterInCentralAgent, "");
                    base.SendMessage(Peformative.Accept.ToString(), content, agentName, message.GetSender());

                    return;
                }
            }

            // Process unregister requests
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_UnregisterInCentralAgent.ToString())
            {
                if (message.GetPerformative() == Peformative.Request.ToString())
                {
                    // Remove agent
                    communicationAgents.Remove(message.GetSender());

                    return;
                }
            }

            // Process CommunicationAgent updates
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_UpdateInCentralAgent.ToString())
            {
                if (message.GetPerformative() == Peformative.Inform.ToString())
                {
                    VehicleUpdateData vehicleUpdateData = JsonUtility.FromJson<VehicleUpdateData>(receiveContent.contentDetails);
                    communicationAgents[message.GetSender()] = vehicleUpdateData; // Update data in dictionary

                    return;
                }
            }

            // Process nearby vehicles (platoon and lonely vehicles) query
            if (message != null && receiveContent.action == SystemAction.CommunicationAgent_NearbyVehicles.ToString())
            {
                if (message.GetPerformative() == Peformative.Query.ToString())
                {
                    float platoonJoinRadius = float.Parse(receiveContent.contentDetails);
                    string sender = message.GetSender();

                    // Get vehicles nearby which are in certain radius
                    List<string> agents = communicationAgents.Keys.Where(x => (
                        communicationAgents[x] != null &&
                        x != sender &&
                        Vector3.Distance(communicationAgents[sender].position, communicationAgents[x].position) < platoonJoinRadius)
                    ).ToList();

                    // List of names of agents (which are leaders), their paths, current target node and distance to sender
                    List<VehicleDataBasic> platoonLeaderCommunicationAgents = agents.Where(x => communicationAgents[x].isPlatoonLeader == true).Select(x => new VehicleDataBasic
                    {
                        name = x,
                        pathNodesNames = communicationAgents[x].pathNodeNames,
                        currentTargetNodeName = communicationAgents[x].currentTargetNodeName,
                        distance = Vector3.Distance(communicationAgents[x].position, communicationAgents[sender].position)
                    }).ToList();

                    // List of names of agents (which are lonely), their paths, current target node and distance to sender
                    List<VehicleDataBasic> lonelyCommunicationAgents = agents.Where(x => communicationAgents[x].inPlatoon == false).Select(x => new VehicleDataBasic
                    {
                        name = x,
                        pathNodesNames = communicationAgents[x].pathNodeNames,
                        currentTargetNodeName = communicationAgents[x].currentTargetNodeName,
                        distance = Vector3.Distance(communicationAgents[x].position, communicationAgents[sender].position)
                    }).ToList();

                    PlatoonQueryData platoonQueryData = new PlatoonQueryData()
                    {
                        platoonLeaderCommunicationAgents = platoonLeaderCommunicationAgents,
                        lonelyCommunicationAgents = lonelyCommunicationAgents
                    };

                    // Send message
                    string content = Utils.CreateContent(SystemAction.CommunicationAgent_NearbyVehicles, JsonUtility.ToJson(platoonQueryData));
                    base.SendMessage(Peformative.Inform.ToString(), content, agentName, message.GetSender());

                    return;
                }
            }

        }
    }   
}
