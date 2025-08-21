using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavSystem : MonoBehaviour
{
    public List<GameObject> nodes = new List<GameObject>();

    public List<Color> colors = new List<Color>();


    void Start()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].GetComponent<MeshRenderer>().material.SetColor("_BaseColor", colors[i]);
        }

        //nodes.AddRange(transform.GetComponentsInChildren<GameObject>());
    }

    void Update()
    {
        
    }

    public Vector3? GetNodePosition(string name)
    {
        return nodes.Find(x => x.name == name).transform.position;
    }

    int FindSmallest(float[] a, bool[] e)
    {
        float min = 999;
        int index = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (a[i] < min && e[i] == false)
            {
                index = i;
                min = a[i];
            }
        }
        return index;
    }

    public TS_DijkstraPath GetPath(string start, string end)
    {
        GameObject startNode = GameObject.Find(start);
        GameObject endNode = GameObject.Find(end);



        int junctionsCount = this.nodes.Count;
        int junctionsEvaluatedCount = 0;

        GameObject[] junctions = this.nodes.ToArray();
        float[] costs = new float[junctionsCount]; for (int z = 0; z < costs.Length; z++) { if (junctions[z] == startNode) { costs[z] = 0; } else { costs[z] = 999; } }
        GameObject[] previousJunctions = new GameObject[junctionsCount];
        bool[] junctionsEvaluated = new bool[junctionsCount];

        int b = 0;
        int i = 0;
        while (junctionsEvaluatedCount != junctionsCount)
        {
            if (junctionsEvaluated[i] == false)
            {
                if (i == FindSmallest(costs, junctionsEvaluated)) //Finding nonevaluated junction with smallest cost
                {
                    for (int x = 0; x < junctions[i].GetComponent<NavSystemNode>().connectedNodes.Count; x++)
                    {
                        int connectedJunctionIndex = System.Array.IndexOf(junctions, junctions[i].GetComponent<NavSystemNode>().connectedNodes[x]);
                        if (junctions[connectedJunctionIndex] != junctions[i] && junctionsEvaluated[connectedJunctionIndex] == false)
                        {
                            float connectedJunctionDistance = Vector3.Distance(junctions[i].transform.position, junctions[connectedJunctionIndex].transform.position) + costs[i];
                            if (costs[connectedJunctionIndex] > connectedJunctionDistance)
                            {
                                costs[connectedJunctionIndex] = connectedJunctionDistance;
                                previousJunctions[connectedJunctionIndex] = junctions[i];
                            }
                        }
                    }
                    junctionsEvaluated[i] = true;
                    junctionsEvaluatedCount++;
                }
            }
            if (i < junctionsCount - 1) { i++; } else { i = 0; }
            if (b > 50) { Debug.Log("Dijkstra Failure!"); break; } else { b++; }
        }

        //Adding path
        TS_DijkstraPath newPath = new TS_DijkstraPath();
        GameObject currentJunction = endNode;
        i = 0;
        while (currentJunction != startNode)
        {
            if (junctions[i] == currentJunction)
            {
                newPath.nodes.Add(currentJunction);
                currentJunction = previousJunctions[i];
            }
            if (i < junctionsCount - 1) { i++; } else { i = 0; }
            if (b > 50) { Debug.Log("Dijkstra Failure!"); break; } else { b++; }
        }
        newPath.nodes.Add(currentJunction);
        newPath.nodes.Reverse();
        return newPath;
    }
}

[System.Serializable]
public class TS_DijkstraPath
{
    public List<GameObject> nodes = new List<GameObject>();
}






