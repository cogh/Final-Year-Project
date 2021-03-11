using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class EdgeScript : MonoBehaviour
{

    public void CreateNodes()
    {
        float laneWidth = (Vector3.Distance(point1, point2)) / (laneCount + 2);
        Vector3 edgeVector = (point2 - point1).normalized;
        for (int i = 0; i < laneCount; i++)
        {
            // Get next lane position
            float margin = laneWidth * 0.5f;
            float laneDisplacement = i * laneWidth;
            float sidewalkWidth = laneWidth;
            Vector3 position = point1 + (edgeVector * (margin + laneDisplacement + sidewalkWidth));

            // Create node
            GameObject newNode = Instantiate(nodePrefab, position, new Quaternion());

            // Add to relevant list
            if (i < laneCount / 2)
            {
                exitNodes.Add(newNode);
            }
            else
            {
                entranceNodes.Add(newNode);
            }
        }
    }

    public void ConnectTo(GameObject targetEdge_)
    {
        targetEdge = targetEdge_;
    }

    private void OnDrawGizmos()
    {
        // Draw self
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, gizmoSphereWidth);

        // Draw connections
        Gizmos.color = color;
        Gizmos.DrawLine(fromNode.transform.position, toNode.transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw self
        Gizmos.color = selectedColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSphereWidth);

        // Draw connections
        Gizmos.color = selectedColor;
        Gizmos.DrawLine(fromNode.transform.position, toNode.transform.position);
    }

    public GameObject fromNode;
    public GameObject toNode;
    public GameObject fromCell;
    public GameObject toCell;
    public List<GameObject> exitNodes;
    public List<GameObject> entranceNodes;
    public GameObject nodePrefab;
    public int laneCount;
    public float sidewalkWidth;
    public Vector3 point1;
    public Vector3 point2;
    public GameObject parentCell;
    public GameObject targetCell;
    public GameObject targetEdge;
    public Color color;
    public Color selectedColor;
    public float gizmoSphereWidth;
}
