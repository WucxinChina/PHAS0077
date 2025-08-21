using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NavSystemNode : MonoBehaviour
{
    public List<GameObject> connectedNodes = new List<GameObject>();
 
    void Update()
    {
        for (int i = 0; i < connectedNodes.Count; i++)
        {
            //Debug.DrawLine(transform.position, connectedJunctions[i].transform.position, Color.red);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        for (int i = 0; i < connectedNodes.Count; i++)
        {
            if (connectedNodes[i] != null) {
                Gizmos.DrawLine(transform.position, connectedNodes[i].transform.position);
            }
            
        }
    }
}




