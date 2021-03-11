using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NodeScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        // Draw self
        Gizmos.color = positionColour;
        Gizmos.DrawWireSphere(transform.position, positionDisplayWidth);

        // Draw connections
        Gizmos.color = connectionColour;
        foreach (GameObject node in connectedNodes)
        {
            if (node != null)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
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
        foreach (GameObject node in connectedNodes)
        {
            if (node != null)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
            }
        }
    }

    public List<GameObject> connectedNodes;
    public Color positionColour;
    public Color positionSelectedColour;
    public Color connectionColour;
    public Color connectionSelectedColour;
    public float positionDisplayWidth;
    public string type;
    public GameObject cell;
}
