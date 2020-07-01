using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Direction
        direction = (targetNode.transform.position - transform.position).normalized;

        // Move towards node
        transform.position += direction * moveSpeed;

        // Collide with node
        if (Vector3.Distance(transform.position, targetNode.transform.position) < nodeCollisionDistance)
        {
            // Get new target node
            int nextTargetNodeIndex = Random.Range(0, targetNode.GetComponent<NodeScript>().connectedNodes.Count);
            GameObject nextTargetNode = targetNode.GetComponent<NodeScript>().connectedNodes[nextTargetNodeIndex];

            // Guarantee next target node is not a dead end
            if (nextTargetNode.GetComponent<NodeScript>().connectedNodes.Count == 0)
            {
                targetNode = lastNode;
            }

            // Set new target node
            lastNode = targetNode;
            targetNode = nextTargetNode;
        }
    }

    public GameObject targetNode;
    public GameObject lastNode;
    public float moveSpeed;
    public float nodeCollisionDistance;
    Vector3 direction;
}
