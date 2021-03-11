using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellScript : MonoBehaviour
{

    private void Start()
    {
        
    }

    public void CreateEdge(Vector3 point1, Vector3 point2)
    {
        GameObject newEdge = Instantiate(edgePrefab);
        newEdge.GetComponent<EdgeScript>().point1 = point1;
        newEdge.GetComponent<EdgeScript>().point2 = point2;
        edges.Add(newEdge);
    }

    private void OnDrawGizmos()
    {
        // Draw self
        Gizmos.color = positionColour;
        Gizmos.DrawWireSphere(transform.position, positionDisplayWidth);

        // Draw connections
        Gizmos.color = connectionColour;
        foreach (GameObject cell in connectedCells)
        {
            Gizmos.DrawLine(transform.position, cell.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw self
        Gizmos.color = positionSelectedColour;
        Gizmos.DrawWireSphere(transform.position, positionDisplayWidth);

        // Draw connections
        Gizmos.color = connectionSelectedColour;
        foreach (GameObject cell in connectedCells)
        {
            Gizmos.DrawLine(transform.position, cell.transform.position);
        }
    }

    
    public bool accessible;
    public List<GameObject> cornerNodes = new List<GameObject>();
    public List<GameObject> edges = new List<GameObject>();
    public List<GameObject> connectedCells = new List<GameObject>();
    public Color positionColour;
    public Color positionSelectedColour;
    public Color connectionColour;
    public Color connectionSelectedColour;
    public float positionDisplayWidth;
    public GameObject edgePrefab;
    public GameObject nodePrefab;
    public GameObject parentNode;
    public string type;
}
