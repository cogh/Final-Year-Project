using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class NodeDrawerScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        connectState = "idle";
        selection = null;
        floorCollider = floor.GetComponent<Collider>();
        nodeList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCursor();
        CreateNode();
        ConnectNodes();

        // Generate all cells
        if (Input.GetKeyDown(KeyCode.C))
        {
            foreach (GameObject node in nodeList)
            {
                GenerateCell(node, 1.0f);
            }
        }    
    }
    void UpdateCursor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if (floorCollider.Raycast(ray, out hit, 100.0f))
        {
            cursor.transform.position = hit.point;
        }
    }

    void CreateNode()
    {
        if (Input.GetMouseButtonDown(0))
        {
            nodeList.Add(Instantiate(nodePrefab, cursor.transform.position, new Quaternion()));
        }
    }
    void ConnectNodes()
    {
        switch (connectState)
        {
            case "idle":
                if (Input.GetMouseButtonDown(1))
                {
                    selection = GetClosestNode();
                    if (selection != null) { connectState = "active"; }
                }
                break;
            case "active":
                if (Input.GetMouseButtonUp(1))
                {
                    GameObject nearestNode = GetClosestNode();
                    if (nearestNode != null && nearestNode != selection)
                    {
                        selection.GetComponent<NodeScript>().connectedNodes.Add(nearestNode);
                        nearestNode.GetComponent<NodeScript>().connectedNodes.Add(selection);
                    }
                    selection = null;
                    connectState = "idle";
                }
                break;
        }
    }

    void GenerateCell(GameObject originNode, float radius)
    {
        // Original node
        NodeScript originNodeScript = originNode.GetComponent<NodeScript>();
        Vector3 originPoint = originNode.transform.position;

        // Get connected nodes
        List<GameObject> connectedNodes = originNodeScript.connectedNodes;

        // Create connection midpoints
        List<Vector3> connectionPoints = new List<Vector3>();
        foreach (GameObject connectedNode in connectedNodes)
        {
            Vector3 connectionPoint = connectedNode.transform.position;
            Vector3 connectionVector = (originPoint - connectionPoint).normalized;
            connectionPoints.Add(connectionPoint);
        }

        // Create connection vectors
        List<Vector3> connectionVectors = new List<Vector3>();
        foreach (Vector3 midPoint in connectionPoints)
        {
            Vector3 connectionVector = (originPoint - midPoint).normalized;
            connectionVectors.Add(connectionVector);
        }

        // Create connection angles
        List<float> connectionAngles = new List<float>();
        foreach (Vector3 connectionVector in connectionVectors)
        {
            // Angle between
            float angle = Vector3.SignedAngle(Vector3.forward, connectionVector, Vector3.up);        // WHAT IF I DO THIS ALL FROM THE PERSPECTIVE OF THE FIRST POINT? (ANGLE 0)

            // Loop angles to 360 clockwise format
            if (angle < 0) { angle = 360 + angle; }

            // Add to list
            connectionAngles.Add(angle);
        }

        // Sort connection angles
        connectionAngles.Sort();

        // Create in-between angles
        for (int i = 0; i < connectionAngles.Count; i++)
        {
            float angle1 = connectionAngles[i];
            float angle2;
            if (i != connectionAngles.Count-1) { angle2 = connectionAngles[i + 1]; }
            else { angle2 = connectionAngles[0] + 360.0f; }
           
            float angleDifference = angle2 - angle1;

            float inBetweenAngleCount = Mathf.Round(angleDifference / 90);
            float inBetweenAngleDistance = angleDifference / inBetweenAngleCount;

            for (int j = 0; j < inBetweenAngleCount-1; j++)
            {
                float inBetweenAngle = angle1 + (inBetweenAngleDistance * (j+1));
                connectionAngles.Insert(i + 1 + j, inBetweenAngle);
            }
        }

        // Create lerped angles
        List<float> lerpedAngles = new List<float>();
        for (int i = 0; i < connectionAngles.Count; i++)
        {
            float angle1 = connectionAngles[i];
            float angle2;
            if (i != connectionAngles.Count - 1) { angle2 = connectionAngles[i + 1]; }
            else { angle2 = connectionAngles[0] + 360.0f; }

            lerpedAngles.Add(Mathf.Lerp(angle1,angle2,0.5f));
        }

        // Create new vectors
        List<Vector3> newVectors = new List<Vector3>();
        foreach (float angle in lerpedAngles)
        {
            Vector3 newVector = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            newVectors.Add(newVector);
        }

        // Create connection midpoints
        List<Vector3> connectionMidPoints = new List<Vector3>();
        foreach (Vector3 vector in newVectors)
        {
            Vector3 midPoint = originPoint - (vector * radius);
            connectionMidPoints.Add(midPoint);
            // Debug
            //GameObject midPointNode = Instantiate(nodePrefab, midPoint, new Quaternion());
            //midPointNode.GetComponent<NodeScript>().positionColour = Color.blue;
        }

        // Create final nodes
        List<GameObject> cellCornerNodes = new List<GameObject>();
        foreach (Vector3 lerpedMidPoint in connectionMidPoints)
        {
            // Debug
            GameObject node = Instantiate(nodePrefab, lerpedMidPoint, new Quaternion());
            node.GetComponent<NodeScript>().positionColour = Color.green;
            cellCornerNodes.Add(node);
        }

        // Connect final nodes
        for (int i = 0; i < cellCornerNodes.Count; i++)
        {
            GameObject cellCornerNode1, cellCornerNode2;
            cellCornerNode1 = cellCornerNodes[i];
            if (i < cellCornerNodes.Count - 1) { cellCornerNode2 = cellCornerNodes[i + 1]; }
            else { cellCornerNode2 = cellCornerNodes[0]; }

            cellCornerNode1.GetComponent<NodeScript>().connectedNodes.Add(cellCornerNode2);
        }
    }

    GameObject GetClosestNode()
    {
        GameObject closestNode = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;
        foreach (GameObject nodeTransform in nodeList)
        {
            float dist = Vector3.Distance(nodeTransform.transform.position, cursor.transform.position);
            if (dist < minDist)
            {
                closestNode = nodeTransform;
                minDist = dist;
            }
        }
        return closestNode;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(cursor.transform.position, 1.0f);
        if (selection != null) { Gizmos.DrawLine(cursor.transform.position, selection.transform.position); }
    }

    public string connectState;
    public GameObject floor;
    Collider floorCollider;
    public GameObject nodePrefab;
    public List<GameObject> nodeList;
    public GameObject selection;
    public GameObject cursor;
}
