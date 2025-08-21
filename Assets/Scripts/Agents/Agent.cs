using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum Peformative
{
    Inform,
    Query,
    Request,
    Accept,
    Reject,
    Propose,
    Confirm
}

public enum SystemAction
{
    CommunicationAgent_RegisterInCentralAgent,
    CommunicationAgent_NearbyVehicles,
    CommunicationAgent_UpdateInCentralAgent,
    CommunicationAgent_UpdateVehicleBehind,
    CommunicationAgent_UpdatePlatoon,
    CommunicationAgent_JoinPlatoon,
    CommunicationAgent_CreatePlatoon,
    CommunicationAgent_LeavePlatoon_TransferLeadership,
    CommunicationAgent_LeavePlatoon_NotifyLeader,
    CommunicationAgent_UnregisterInCentralAgent
}

public abstract class Agent: MonoBehaviour {
    // Open conversations with different agents

    [ReadOnly] public string agentName = "NoName";
    [ReadOnly] public bool registeredInAgentPlatform;
    [ReadOnly] public AgentPlatform agentPlatform;


    public Queue<Message> messageQueue = new Queue<Message>();


    protected bool RegisterInAgentPlatform()
    {
        // In real life this function will try to connect to agent server via some protocol
        var response = GameObject.Find("AgentPlatform").GetComponent<AgentPlatform>().RegisterAgent(agentName, this);
        registeredInAgentPlatform = response.Item1;
        agentPlatform = response.Item2;
        return response.Item1;
    }

    protected bool DeregisterInAgentPlatform()
    {
        // In real life this function will try to connect to agent server via some protocol
        var response = GameObject.Find("AgentPlatform").GetComponent<AgentPlatform>().DeregisterAgent(agentName);
        return response;
    }

    /// <summary>
    /// Create a new message
    /// </summary>
    /// <param name="performative">String type of the message </param>
    /// <param name="content">String message content</param>
    /// <param name="sender">String sender´s name</param>
    /// <param name="receiverName">String name of the receiving agent or "all" if is for everybody</param>
    /// <returns>A message instance with the given values</returns>
    private Message CreateMesagge(string performative, string content, string sender, string receiverName){
        Message message = new Message(performative, content, sender, receiverName);   
		return message;
	}
    /// <summary>
    /// Crea e envia un Message a un axente
    /// </summary>
    /// <param name="performative">String type of the message </param>
    /// <param name="content">String message content</param>
    /// <param name="sender">String sender´s name</param>
    /// <param name="receiverName">String name of the receiving agent</param>
    /// <param name="receiver"> MASAgent component of the agent to whom the message is sent</param>
    public void SendMessage(string performative, string content, string senderName, string receiverName)
    {
        if (!senderName.Equals(receiverName)) {
            Message message = CreateMesagge(performative, content, senderName, receiverName);
            if (message != null) {
                agentPlatform.ForwardMessage(message);
            }
            else{
                throw new Exception("Message creation error");
            }
        }
        else
        {
            throw new Exception("Cannot send message to yourself");
        }
	}

    public void EnqueueMessage(Message message)
    {
        messageQueue.Enqueue(message);
    }



    /// <summary>
    /// Validate the received Message and delegate the processing of its content to the corresponding function
    /// </summary>
    /// <param name="Message">Message the received Message</param>
	public Message ReceiveMessage(){

        if (messageQueue.Count > 0)
        {
            return messageQueue.Dequeue();
        }
        else
        {
            return null;
        }
	}
}
