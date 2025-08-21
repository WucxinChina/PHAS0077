using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AgentPlatform : MonoBehaviour
{
    public Dictionary<string, Agent> registeredAgents;
    public List<string> registeredAgentsNames;

    void Start()
    {
        registeredAgents = new Dictionary<string, Agent>();
    }

    public (bool, AgentPlatform) RegisterAgent(string agentName, Agent agent)
    {
        registeredAgents.Add(agentName, agent);
        registeredAgentsNames.Add(agentName);
        return (true, this);
    }

    public bool DeregisterAgent(string agentName)
    {
        registeredAgents.Remove(agentName);
        registeredAgentsNames.Remove(agentName);
        //Debug.Log("Deregistered" + agentName);
        return true;
    }

    public bool ForwardMessage(Message message)
    {
        // Find receiver and forward message to it
        Agent receiver = null;
        registeredAgents.TryGetValue(message.GetReceiver(), out receiver);
        if (receiver != null) 
        {
            receiver.EnqueueMessage(message);
            return true;
        }
        return false;
    }

    public Dictionary<string, Agent> GetRegisteredAgents()
    {
        return registeredAgents;
    }
}
