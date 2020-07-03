using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellScript : MonoBehaviour
{

    public void CreateEdge(Vector3 point1, Vector3 point2, string key)
    {
        GameObject newEdge = Instantiate(edgePrefab);
        newEdge.GetComponent<EdgeScript>().point1 = point1;
        newEdge.GetComponent<EdgeScript>().point2 = point2;
        edges.Add(key, newEdge);
    }

    private void OnDrawGizmos()
    {
        // Draw self
        Gizmos.color = positionColour;
        Gizmos.DrawWireSphere(transform.position, positionDisplayWidth);

        // Draw connections
        Gizmos.color = connectionColour;
        foreach (KeyValuePair<string, GameObject> cell in connectedCells)
        {
            if (cell.Value != null)
            {
                Gizmos.DrawLine(transform.position, cell.Value.transform.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw self
        Gizmos.color = positionSelectedColour;
        Gizmos.DrawWireSphere(transform.position, positionDisplayWidth);

        // Draw connections
        Gizmos.color = connectionSelectedColour;
        foreach (KeyValuePair<string, GameObject> cell in connectedCells)
        {
            if (cell.Value != null)
            {
                Gizmos.DrawLine(transform.position, cell.Value.transform.position);
            }
        }
    }

    public bool accessible;
    public Dictionary<string, GameObject> cornersNodes = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> edges = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> connectedCells = new Dictionary<string, GameObject>();
    public Color positionColour;
    public Color positionSelectedColour;
    public Color connectionColour;
    public Color connectionSelectedColour;
    public float positionDisplayWidth;
    public GameObject edgePrefab;
    public GameObject nodePrefab;
}
