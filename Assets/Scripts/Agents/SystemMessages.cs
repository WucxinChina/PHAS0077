using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Content
{
    public string action;
    public string contentDetails;
}

[Serializable]
public class VehicleUpdateData // For updating data in centralAgent (communicationAgent-centralAgent)
{
    public Vector3 position;
    public bool inPlatoon;
    public bool isPlatoonLeader;
    public string destinationNodeName;
    public List<string> pathNodeNames;
    public string currentTargetNodeName;
    public List<string> platoonVehiclesNames; // Only for leader
}

[Serializable]
public class PlatoonQueryData // For asking about nearby platoons and lonely vehicles (communicationAgent-centralAgent)
{
    public List<VehicleDataBasic> platoonLeaderCommunicationAgents;
    public List<VehicleDataBasic> lonelyCommunicationAgents;
}

[Serializable]
public class VehicleDataBasic
{
    public string name;
    public List<string> pathNodesNames;
    public string currentTargetNodeName;
    public float distance;
}

[Serializable]
public class PlatoonData // For creating new platoon (send by leader to new member when it joins platoon or when leader is changing) (communicationAgent-communicationAgent)
{
    public string leaderName;
    public List<string> pathNodesNames;
    public string followAgentName; // Name of agent to follow
    public string behindAgentName; // Name of agent behind
}

[Serializable]
public class PlatoonCreateData // For creating new platoon (send by leader to possible member as invitation) (communicationAgent-communicationAgent)
{
    public string leaderName;
    public List<string> pathNodesNames;
    public string currentTargetNodeName;
}

[Serializable]
public class StringList // For passing platoonVehiclesNames when handing over the leadership (communicationAgent-communicationAgent)
{
    public List<string> list;
}

[Serializable]
public class PlatoonUpdateData // For interplatoon communication (communicationAgent-communicationAgent)
{
    public Vector3 position;
    public string targetNodeName;
}




