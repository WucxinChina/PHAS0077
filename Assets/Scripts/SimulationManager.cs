using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
[System.Serializable]
public class SpawningWave {
    public GameObject startNode;
    public int vehiclesToSpawn;
}

public class SimulationModifiers
{
    public bool? platooningSystemEnabled = true;
    public int? maxPlatoonSize;
    public float? betweenVehicleDistances;
};

public class SimulationManager : MonoBehaviour
{
    [Header("Info")]
    [ReadOnly] public int spawnedCount = 0;
    [ReadOnly] public float speedMultiplier = 1.0f;
    [ReadOnly] public bool spawning;
    List<SpawningWave> spawningWaves;
    [ReadOnly] public float totalFuelUsed;

    int vehicleCount;

    [Header("References")]
    public GameObject vehicleAgentPrefab;
    public GameObject centralAgentPrefab;
    public NavSystem navSystem;
    public GameObject agentsParent;
    public AgentPlatform agentPlatform;
    public CentralAgent centralAgent;
    private EventSystem eventSystem;

    [Header("UI")]
    public Text simulationSpeedText;
    public Text agentsCountText;
    public Text spawnButtonText;
    public Text fuelUsedText;

    public InputField agentCountInputField;
    public InputField distanceBetweenVehiclesInputField;
    public InputField maxNumberOfAgentsInPlatoonsInputField;

    [Header("Vehicle Agent Settings")]
    [MinMaxSlider(20.0f, 200.0f)] public Vector2 speedRange = new Vector2(70.0f, 120.0f);
    int[][] neighbours =
    {
    new int[] { 2, 3, 4},
    new int[] { 1, 5, 6 },
    new int[] { 1 }, 
    new int[] { 1, 5, 7 },
    new int[] { 2, 4, 7 },
    new int[] { 2 },
    new int[] { 4, 5 },

    };
    float speed = 100.0f;
    [Header("Spawning Settings")]
    public int minVehiclesInSpawningWave = 1;
    public int maxVehiclesInSpawningWave = 4;

    public float waveSpawn_Timeout = 50.0f;
    float waveSpawn_Timer;

    public float spawn_Timeout = 50.0f;
    float spawn_Timer;

    private List<int> startNodes = new List<int>();
    private List<int> endNodes = new List<int>();

    void Start()
    {
        eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        Time.timeScale = speedMultiplier;
        spawningWaves = new List<SpawningWave>();
        SpawnCentralAgent();
    }

    void Update()
    {
        vehicleCount = agentPlatform.GetRegisteredAgents().Count();
        agentsCountText.text = vehicleCount.ToString();

        if (spawning)
        {
            SimulatedSpawning_SpawnWave();
        }

        SimulatedSpawning_ProcessSpawningWaves();
        SpawnAgentsManually();
        CalculateTotalFuelUsed();
    }

    public void ResetScene()
    {
        
        SceneManager.LoadScene(0);
    }

    void CalculateTotalFuelUsed()
    {
        foreach (Transform agent in agentPlatform.transform)
        {
            if (agent != null && agent.tag == "Vehicle")
            {
                totalFuelUsed += agent.transform.GetComponent<Fuel>().currentConsumption * (Time.deltaTime * 10);
            }
        }

        fuelUsedText.text = totalFuelUsed.ToString();
    }

    void SimulatedSpawning_SpawnWave()
    {
        if (waveSpawn_Timer > waveSpawn_Timeout)
        {
            if (vehicleCount < 100)
            {
                GameObject startNode = navSystem.nodes[Random.Range(0, navSystem.nodes.Count)];
                spawningWaves.Add(new SpawningWave() { startNode = startNode, vehiclesToSpawn = Random.Range(1, maxVehiclesInSpawningWave) });
                waveSpawn_Timer = 0;
            }
        }
        else
        {
            waveSpawn_Timer++;
        }
    }

    void SimulatedSpawning_ProcessSpawningWaves()
    {
        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = true
        };

        if (spawn_Timer > spawn_Timeout)
        {
            for (int i = spawningWaves.Count - 1; i >= 0; i--)
            {
                if (spawningWaves[i].vehiclesToSpawn > 0)
                {
                    GameObject destinationNode = spawningWaves[i].startNode;
                    while (destinationNode == spawningWaves[i].startNode)
                        destinationNode = navSystem.nodes[Random.Range(minVehiclesInSpawningWave, navSystem.nodes.Count)];
                    SpawnVehicle(spawningWaves[i].startNode, destinationNode, Mathf.Lerp(speedRange.x, speedRange.y, Random.Range(0.0f, 1.0f)), simulationModifiers);
                    spawningWaves[i].vehiclesToSpawn--;
                }
                else
                {
                    spawningWaves.RemoveAt(i);
                }
            }
            spawn_Timer = Random.Range(0.0f, 1.0f) > 0.5f ? 0 : 0.5f; // Randomize timer
        }
        else
        {
            spawn_Timer++;
        }
    }

    void SpawnCentralAgent()
    {
        GameObject newCentralAgent = Instantiate(centralAgentPrefab, Vector3.zero, Quaternion.identity);
        newCentralAgent.transform.parent = agentsParent.transform;

        // Setup CentralAgent
        centralAgent = newCentralAgent.GetComponent<CentralAgent>();
        centralAgent.agentName = "CentralAgent";
    }

    void SpawnVehicleAtRandomNode()
    {
        GameObject startNode = navSystem.nodes[Random.Range(0, navSystem.nodes.Count)];

        GameObject destinationNode = startNode;
        while (destinationNode == startNode)
            destinationNode = navSystem.nodes[Random.Range(0, navSystem.nodes.Count)];
        
        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = true
        };

        SpawnVehicle(startNode, destinationNode, Mathf.Lerp(speedRange.x, speedRange.y, Random.Range(0.0f, 1.0f)), simulationModifiers);
    }

    void SpawnVehicle(GameObject startNode, GameObject destinationNode, float baseSpeed, SimulationModifiers simulationModifiers)
    {
        GameObject newVehicle = Instantiate(vehicleAgentPrefab, startNode.transform.position, Quaternion.identity);
        newVehicle.transform.parent = agentsParent.transform;

        // Setup rendering
        newVehicle.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetColor("_BaseColor", navSystem.colors[navSystem.nodes.IndexOf(destinationNode)]);
        newVehicle.tag = "Vehicle";

        // Setup VehicleAgent
        var vehicleAgent = newVehicle.GetComponent<VehicleAgent>();
        vehicleAgent.startNodeName = startNode.name;
        vehicleAgent.destinationNodeName = destinationNode.name;
        vehicleAgent.SetUp(baseSpeed);

        // Setup CommunicationAgent


        if (simulationModifiers.platooningSystemEnabled.Value)
        {
            var communicationAgent = newVehicle.GetComponent<CommunicationAgent>();
            communicationAgent.agentName = "CommunicationAgent_" + spawnedCount.ToString();
            communicationAgent.centralAgentName = "CentralAgent";
            communicationAgent.agentPlatform = agentPlatform;

            communicationAgent.maxPlatoonSize = simulationModifiers.maxPlatoonSize.HasValue ? simulationModifiers.maxPlatoonSize.Value : communicationAgent.maxPlatoonSize;
            communicationAgent.betweenVehicleDistances = simulationModifiers.betweenVehicleDistances.HasValue ? simulationModifiers.betweenVehicleDistances.Value : communicationAgent.betweenVehicleDistances;
        }
        else
        {
            Destroy(newVehicle.GetComponent<CommunicationAgent>());
        }
        spawnedCount++;
    }

   

    void SpawnAgentsManually()
    {
        if (eventSystem.IsPointerOverGameObject())
            return;

        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = true
        };

        if (Input.GetMouseButtonDown(0))
        {
            //create a ray cast and set it to the mouses cursor position in game
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000, LayerMask.GetMask("Ground")))
            {
                //draw invisible ray cast/vector
                Debug.DrawLine(ray.origin, hit.point);

                // Find closest node
                var closestNode = navSystem.nodes.OrderBy(o => Vector3.Distance(o.transform.position, hit.point)).First();

                // Find other random node
                GameObject destinationNode = closestNode;
                while (destinationNode == closestNode)
                    destinationNode = navSystem.nodes[Random.Range(0, navSystem.nodes.Count)];

                SpawnVehicle(closestNode, destinationNode, Mathf.Lerp(speedRange.x, speedRange.y, Random.Range(0.0f, 1.0f)), simulationModifiers);
            }
        }
    }

    public void SimulationSpeedUp(float increment)
    {
        speedMultiplier = Mathf.Min(speedMultiplier + increment, 50.0f);
        simulationSpeedText.text = speedMultiplier.ToString();
        Time.timeScale = speedMultiplier;
    }

    public void SimulationSpeedDown(float decrement)
    {
        speedMultiplier = Mathf.Max(0.0f, speedMultiplier - decrement);
        simulationSpeedText.text = speedMultiplier.ToString();
        Time.timeScale = speedMultiplier;
    }




    public void SpawnButtonToggle()
    {
        spawning = !spawning;
        spawnButtonText.text = spawning ? "Stop" : "Start";
    }

    // --------------------- SIMULATION ---------------------

    public void LaunchSimulationScenario(int index)
    {
        IEnumerator coroutine = null;
        switch (index)
        {
            case 1:
                coroutine = SpawnScenario1_1();
                break;
            case 2:
                coroutine = SpawnScenario1_2();
                break;
            case 3:
                coroutine = SpawnScenario2_1();
                break;
            case 4:
                coroutine = SpawnScenario2_2();
                break;
            case 5:
                coroutine = SpawnScenario3_1();
                break;
            case 6:
                coroutine = SpawnScenario3_2();
                break;
            default:
                break;
        }
        StartCoroutine(coroutine);

        foreach (var button in GameObject.FindGameObjectsWithTag("ScenarioButton"))
        {
            button.GetComponent<Button>().interactable = false;
        }
    }

    IEnumerator SpawnScenario1_1()
    {  
        // // 10 agents start at node 1, form different platoons and move to node 7

        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = true,
            maxPlatoonSize = 5,
            betweenVehicleDistances = 10
        };

        yield return new WaitForSeconds(0.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.7f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.2f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.3f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
    }

    IEnumerator SpawnScenario1_2()
    {
        // Same as scenario 1 but with platooning system disabled

        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = false
        };

        yield return new WaitForSeconds(0.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.7f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.2f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.3f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
        yield return new WaitForSeconds(0.1f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
    }

    IEnumerator SpawnScenario2_1()
    {
        // 3 agents start at node 1, form a platoon and move to node 7
        // 3 agents start at node 3, form a platoon, move to last common point which is node 4, two of them go to node 5 and one of them move to node 7
        // 1 agent start at node 2 move to node 1 where it joins platoon and move with it to node 4 and ends there

        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = true,
            maxPlatoonSize = 5,
            betweenVehicleDistances = 10
        };

        yield return new WaitForSeconds(0.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.7f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);

        yield return new WaitForSeconds(0.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (3)"), navSystem.nodes.Find(o => o.name == "Node (5)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(1.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (3)"), navSystem.nodes.Find(o => o.name == "Node (5)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(1.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (3)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);

        yield return new WaitForSeconds(1.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (2)"), navSystem.nodes.Find(o => o.name == "Node (4)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
    }

    IEnumerator SpawnScenario2_2()
    {
        // Same as Scenario 2.1 but with platooning system disabled

        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = false
        };

        yield return new WaitForSeconds(0.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(0.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(0.7f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (1)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);

        yield return new WaitForSeconds(0.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (3)"), navSystem.nodes.Find(o => o.name == "Node (5)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.5f), simulationModifiers);
        yield return new WaitForSeconds(1.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (3)"), navSystem.nodes.Find(o => o.name == "Node (5)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.6f), simulationModifiers);
        yield return new WaitForSeconds(1.5f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (3)"), navSystem.nodes.Find(o => o.name == "Node (7)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);

        yield return new WaitForSeconds(1.0f);
        SpawnVehicle(navSystem.nodes.Find(o => o.name == "Node (2)"), navSystem.nodes.Find(o => o.name == "Node (4)"), Mathf.Lerp(speedRange.x, speedRange.y, 0.7f), simulationModifiers);
    }

    IEnumerator SpawnScenario3_1()
    {
        speedMultiplier = 1;
        simulationSpeedText.text = speedMultiplier.ToString();
        Time.timeScale = speedMultiplier;
        var numberOfAgents = System.Int32.Parse(agentCountInputField.text);
        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = true,
            maxPlatoonSize = System.Int32.Parse(maxNumberOfAgentsInPlatoonsInputField.text), // 3
            betweenVehicleDistances = System.Int32.Parse(distanceBetweenVehiclesInputField.text) // 15
        };
        if(numberOfAgents<100)
            AssignStartingAndDestinationNodes(100);
        else 
            AssignStartingAndDestinationNodes(numberOfAgents);
        yield return new WaitForSeconds(0.0f);
        Debug.Log("Spawning Wave 1...");
        int i = 0;
      
        for (; i < numberOfAgents; i++)
        {
            GameObject startNode = navSystem.nodes[startNodes[i]];
            GameObject destinationNode = navSystem.nodes[endNodes[i]];
            float waitTime = 0.5f;
            
            yield return new WaitForSeconds(waitTime);
            

            SpawnVehicle(startNode, destinationNode, speed, simulationModifiers);
            if((i+1)%25==0)
                yield return new WaitForSeconds(waitTime);
            
        }
       
    }

    IEnumerator SpawnScenario3_2()
    {
        speedMultiplier = 9;
        simulationSpeedText.text = speedMultiplier.ToString();
        Time.timeScale = speedMultiplier;
        var numberOfAgents = System.Int32.Parse(agentCountInputField.text);
        SimulationModifiers simulationModifiers = new SimulationModifiers()
        {
            platooningSystemEnabled = false,
            maxPlatoonSize = System.Int32.Parse(maxNumberOfAgentsInPlatoonsInputField.text), // 5
            betweenVehicleDistances = System.Int32.Parse(distanceBetweenVehiclesInputField.text) // 10
        };

        FromStringFileToList();
        yield return new WaitForSeconds(0.0f);
        Debug.Log("Spawning Wave 1...");
        int i = 0;
        for (; i < numberOfAgents; i++)
        {
            GameObject startNode = navSystem.nodes[startNodes[i]];
            GameObject destinationNode = navSystem.nodes[endNodes[i]];
            float waitTime =0.5f;
            
            yield return new WaitForSeconds(waitTime);
            

            SpawnVehicle(startNode, destinationNode, speed, simulationModifiers);
            if((i+1)%25==0)
                yield return new WaitForSeconds(waitTime);
            
        }
    }
    List<string> NumListToStringList(List<int> tmpNodes)
    {
        var listOfText = new List<string>();
        foreach(var num in tmpNodes)
        {
            listOfText.Add(num.ToString());
        }
        return listOfText;
    }

    
    void FromStringFileToList()
    {
        string path = Directory.GetCurrentDirectory();
        string startPath = @"/Assets/Scripts/UnityAgentTest/StartNodes.txt";
        string endPath = @"/Assets/Scripts/UnityAgentTest/EndNodes.txt";

        if (!IsLinux)
        {
            startPath = startPath.Replace("/",@"\");
            endPath = endPath.Replace("/",@"\");

        }
        string[] startText = File.ReadAllLines(path+startPath);
        foreach (string s in startText)
        {
            int x = int.Parse(s);
            startNodes.Add(x);
        }
        string[] endText = File.ReadAllLines(path+endPath);
        foreach (string s in endText)
        {
            int x = int.Parse(s);
            endNodes.Add(x);
        }

    }
    void AssignStartingAndDestinationNodes(int numberOfAgents)
    {
        
        for (int i = 0; i < numberOfAgents; i++)
        {
            int startNode = Random.Range(0, navSystem.nodes.Count);
            int destinationNode = Random.Range(0, navSystem.nodes.Count);
            while (destinationNode == startNode || neighbours[startNode].Contains(destinationNode+1))
                destinationNode = Random.Range(0, navSystem.nodes.Count);
            startNodes.Add(startNode);
            endNodes.Add(destinationNode);

            
        }
        var tmpStartOfStrings = NumListToStringList(startNodes);
        var tmpEndOfStrings = NumListToStringList(endNodes);   
        string path = Directory.GetCurrentDirectory();
        string startPath = @"/Assets/Scripts/UnityAgentTest/StartNodes.txt";
        string endPath = @"/Assets/Scripts/UnityAgentTest/EndNodes.txt";

        if (!IsLinux)
        {
            startPath = startPath.Replace("/",@"\");
            endPath = endPath.Replace("/",@"\");

        }
        System.IO.File.WriteAllLines(path+startPath, tmpStartOfStrings);
        System.IO.File.WriteAllLines(path+endPath, tmpEndOfStrings);
    }

    public static bool IsLinux
    {
        get
        {
            int p = (int)System.Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }
    }



}

