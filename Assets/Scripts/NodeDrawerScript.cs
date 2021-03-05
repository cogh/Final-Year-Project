using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

class Connection
{
    public Connection(GameObject argFrom, GameObject argTo)
    {
        from = argFrom;
        to = argTo;
    }
    public GameObject from;
    public GameObject to;
}

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

        // Create tween nodes
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CreateTweenNodes();
        }

        // Create fill nodes
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Create connection list
            List<Connection> connections = new List<Connection>();

            // Create loop list
            List<List<GameObject>> loopList = new List<List<GameObject>>();

            // Add all connections
            foreach (GameObject node in nodeList)
            {
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                foreach (GameObject connectedNode in nodeScript.connectedNodes)
                {
                    connections.Add(new Connection(node, connectedNode));
                }
            }

            // For each connection, search for loops
            foreach (Connection connection in connections) // remember u gotta neglect going backwards on lonely nodes, or something, maybe do nothing if only 1 connection?
            {
                // Get nodes
                GameObject fromNode = connection.from;
                GameObject toNode   = connection.to  ;

                // Left loop
                List<GameObject> loop = new List<GameObject>();
                GameObject firstNode   = fromNode;
                GameObject lastNode    = fromNode;
                GameObject currentNode = toNode  ;
                loop.Add(firstNode);
                int count = 0;
                while (currentNode != firstNode && currentNode.GetComponent<NodeScript>().connectedNodes.Count > 1 && count < 1000)
                {
                    // Get scripts
                    NodeScript currentScript = currentNode.GetComponent<NodeScript>();
                    NodeScript lastScript    = lastNode   .GetComponent<NodeScript>();

                    // Get last to current
                    Vector3 lastToCurrentVector = currentNode.transform.position - lastNode.transform.position;

                    // Initialise best node arbitrarily
                    GameObject bestNextNode = currentScript.connectedNodes[0]; // needs to not include self, unbeatable?

                    // If best is last, then change
                    if (bestNextNode == lastNode) 
                    {
                        bestNextNode = currentScript.connectedNodes[1]; 
                    }

                    // Find correct next node (leftmost/ccw)
                    foreach (GameObject nextNode in currentScript.connectedNodes) // NEXTNODE SHOULD NEVER BE PREVIOUS FIX THIS
                    {
                        // Get current to best angle - use current to last as base, and current to next/best to evaluate
                        Vector3 currentToBestVector = bestNextNode.transform.position - currentNode.transform.position;
                        float bestAngle = Vector3.SignedAngle(lastToCurrentVector, currentToBestVector, Vector3.up); // I understand the problem now. Signed doesnt work how I thought, has + and -

                        // Get current to next angle
                        Vector3 currentToNextVector = nextNode.transform.position - currentNode.transform.position;
                        float nextAngle = Vector3.SignedAngle(lastToCurrentVector, currentToNextVector, Vector3.up);

                        // Evaluate and replace (if better)
                        if (nextAngle < bestAngle && nextNode != lastNode)
                        {
                            bestNextNode = nextNode;
                        }
                    }

                    // Add current node to loop
                    loop.Add(currentNode);

                    // Increment node
                    lastNode = currentNode;
                    currentNode = bestNextNode;

                    count++;
                }
                
                // Add if loop is valid
                if (currentNode == firstNode)
                {
                    loopList.Add(loop); // Seems to add correct loop, but also some invalids, or possibly slightly different duplicates?

                    // I wonder if perhaps it is reversing, and counting the previous item as a loop?
                }

                // Don't think right loop is needed here but maybe
            }

            // Make only unique set of loops
            List<List<GameObject>> uniqueLoopList = new List<List<GameObject>>(); // NEED TO CHECK IF THIS ALGORITHM GIVES FALSE POSITIVE ON DIFFERENT SIZED LOOPS
            foreach (List<GameObject> loop in loopList)
            {
                // Find if loop exists in unique loops
                bool loopFound = false;
                foreach (List<GameObject> uniqueLoop in uniqueLoopList)
                {
                    // See if loops are identical (same nodes)
                    bool containsSameNodes = true;
                    foreach(GameObject node in loop)
                    {
                        if (!uniqueLoop.Contains(node))
                        {
                            containsSameNodes = false;
                        }
                    }
                    // If loop is found in unique loops, has already been added (loop is found)
                    if (containsSameNodes == true && loop.Count == uniqueLoop.Count)
                    {
                        loopFound = true;
                    }
                }
                // Add loop if not found
                if (!loopFound)
                {
                    uniqueLoopList.Add(loop);
                }
            }

            // For every loop, generate node in the middle
            foreach (List<GameObject> loop in uniqueLoopList)
            {
                Vector3 averagePosition = Vector3.zero;
                int count = 0;
                foreach (GameObject node in loop)
                {
                    averagePosition += node.transform.position;
                    count++;
                }
                averagePosition /= count;

                Instantiate(nodePrefab, averagePosition, Quaternion.identity);
            }
        }

        // Generate star cells from all nodes
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            foreach (GameObject node in nodeList)
            {
                GenerateStarCell(node, 1.0f);
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
                        selection  .GetComponent<NodeScript>().connectedNodes.Add(nearestNode);
                        nearestNode.GetComponent<NodeScript>().connectedNodes.Add(selection);
                    }
                    selection = null;
                    connectState = "idle";
                }
                break;
        }
    }
    void CreateTweenNodes()
    {
        // Create tween nodes
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Get all connections between nodes
            List<Connection> nodeConnections = new List<Connection>();
            foreach (GameObject node in nodeList)
            {
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                foreach (GameObject connectedNode in nodeScript.connectedNodes)
                {
                    // Remove connection from connectedNode to avoid duplicates
                    NodeScript connectedNodeScript = connectedNode.GetComponent<NodeScript>();

                    // Create connection object
                    Connection nodeConnection = new Connection(node, connectedNode);
                    nodeConnections.Add(nodeConnection);
                }
            }

            // Create tween node for each connection
            foreach (Connection nodeConnection in nodeConnections)
            {
                // Get nodes
                GameObject fromNode = nodeConnection.from;
                GameObject toNode   = nodeConnection.to  ;

                // Create node
                Vector3 tweenPosition = Vector3.Lerp(fromNode.transform.position, toNode.transform.position, 0.5f);
                GameObject tweenNode = Instantiate(nodePrefab, tweenPosition, Quaternion.identity);
                nodeList.Add(tweenNode);

                // Get scripts
                NodeScript tweenScript = tweenNode.GetComponent<NodeScript>();
                NodeScript fromScript = fromNode.GetComponent<NodeScript>();
                NodeScript toScript = toNode.GetComponent<NodeScript>();

                // Form new connections
                tweenScript.connectedNodes.Add(fromNode);
                tweenScript.connectedNodes.Add(toNode);
                fromScript.connectedNodes.Add(tweenNode);
                toScript.connectedNodes.Add(tweenNode);

                // Break old connections
                fromScript.connectedNodes.Remove(toNode);
                toScript.connectedNodes.Remove(fromNode);
            }
        }
    }
    void GenerateStarCell(GameObject originNode, float radius)
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
