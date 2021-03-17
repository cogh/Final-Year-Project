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
        CreateNodeFromCursor();
        ConnectNodes();

        // Create tween nodes
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CreateTweenNodes();
        }

        // Create fill nodes
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CreateFillNodes();
        }

        // Create all cells as children of their parent nodes and inherit connections
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Create cells
            foreach (GameObject node in nodeList)
            {
                // Get node script
                NodeScript nodeScript = node.GetComponent<NodeScript>();

                // Create cell
                GameObject cell = Instantiate(cellPrefab, node.transform);

                // Get cell script
                CellScript cellScript = cell.GetComponent<CellScript>();

                // Inherit type
                cellScript.type = nodeScript.type;

                // Connect cell to node
                nodeScript.cell = cell;
                cellScript.parentNode = node;

                // Add to cell list
                cellList.Add(cell);
            }

            // Inherit connections
            foreach (GameObject node in nodeList)
            {
                // Get node and cell script
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                GameObject cell = nodeScript.cell;
                CellScript cellScript = cell.GetComponent<CellScript>();

                // Inherit connections
                foreach (GameObject connectedNode in nodeScript.connectedNodes)
                {
                    cellScript.connectedCells.Add(connectedNode.GetComponent<NodeScript>().cell);
                }
            }
        }

        // Generate star cell edges
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            foreach (GameObject node in nodeList)
            {
                if (node.GetComponent<NodeScript>().type == "star")
                {
                    GameObject cell = node.GetComponent<NodeScript>().cell;
                    GenerateStarCellEdges(cell, 0.5f);
                }
            }
        }

        // Generate tween cell edges
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            foreach (GameObject node in nodeList)
            {
                // Get relevant node/cell
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                GameObject cell = nodeScript.cell;
                CellScript cellScript = cell.GetComponent<CellScript>();

                // Adopt edges of adjacent star cells
                if (nodeScript.type == "tween")
                {
                    // Adopt edges of adjacent star cells
                    foreach (GameObject connectedCell in cellScript.connectedCells)
                    {
                        CellScript connectedCellScript = connectedCell.GetComponent<CellScript>();
                        // Adopt edges of star cells
                        if (connectedCellScript.type == "star")
                        {
                            // Get edge to adopt based on which edge is connected to this cell
                            GameObject edgeToAdopt = null;
                            foreach (GameObject connectedCellEdge in connectedCellScript.edges)
                            {
                                EdgeScript connectedCellEdgeScript = connectedCellEdge.GetComponent<EdgeScript>();
                                if (connectedCellEdgeScript.toCell == cell)
                                {
                                    edgeToAdopt = connectedCellEdge;
                                }
                            }

                            // Adopt edge
                            cellScript.edges.Add(edgeToAdopt);
                        }
                    }

                    // Create new tween edges
                    GameObject fromEdge = cellScript.edges[0];
                    GameObject toEdge = cellScript.edges[1];
                    GameObject tweenEdge1 = Instantiate(edgePrefab, cell.transform);
                    if (fromEdge == null)
                    {
                        //
                        int here = 0;
                        cell.name = "yoyoyo";
                    }
                    tweenEdge1.GetComponent<EdgeScript>().fromNode = fromEdge.GetComponent<EdgeScript>().fromNode;
                    tweenEdge1.GetComponent<EdgeScript>().toNode = toEdge.GetComponent<EdgeScript>().toNode;
                    GameObject tweenEdge2 = Instantiate(edgePrefab, cell.transform);
                    tweenEdge2.GetComponent<EdgeScript>().toNode = fromEdge.GetComponent<EdgeScript>().toNode;
                    tweenEdge2.GetComponent<EdgeScript>().fromNode = toEdge.GetComponent<EdgeScript>().fromNode;
                    cellScript.edges.Add(tweenEdge1);
                    cellScript.edges.Add(tweenEdge2);

                    edgeList.Add(tweenEdge1);
                    edgeList.Add(tweenEdge2);

                    // Need to add fromCell and toCell to tweenEdge1, and tweenEdge2
                    // Can probably do this with clockwise logic, finding the connection in between tween connections, since can only have 2-4 connections
                    // Join tween edge to third cell
                    //tweenEdge1.GetComponent<EdgeScript>().toCell = cellScript.connectedCells[2];
                    if (cellScript.connectedCells.Count > 3)
                    {
                        //tweenEdge2.GetComponent<EdgeScript>().toCell = cellScript.connectedCells[3];
                    }

                    // Temporarily disable tween edges entirely
                    tweenEdge1.GetComponent<EdgeScript>().accessible = false;
                    tweenEdge2.GetComponent<EdgeScript>().accessible = false;
                }
            }
        }

        // Adopt edges from tween to fill cells
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            foreach (GameObject node in nodeList)
            {
                // Get relevant node/cell
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                GameObject cell = nodeScript.cell;
                CellScript cellScript = cell.GetComponent<CellScript>();

                // Adopt edges of adjacent tween cells
                if (nodeScript.type == "fill")
                {
                    foreach (GameObject connectedCell in cellScript.connectedCells)
                    {
                        CellScript connectedCellScript = connectedCell.GetComponent<CellScript>();
                        foreach (GameObject connectedCellEdge in connectedCellScript.edges)
                        {
                            EdgeScript connectedCellEdgeScript = connectedCellEdge.GetComponent<EdgeScript>();
                            if (connectedCellEdgeScript.toCell == cell)
                            {
                                cellScript.edges.Add(connectedCellEdge);
                            }
                        }
                    }
                }
            }
        }

        // Create edge nodes
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            foreach (GameObject edge in edgeList)
            {
                EdgeScript edgeScript = edge.GetComponent<EdgeScript>();
                edgeScript.CreateNodes();
            }
        }

        // Connect edge nodes
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            foreach (GameObject cell in cellList)
            {
                CellScript cellScript = cell.GetComponent<CellScript>();
                foreach (GameObject edge in cellScript.edges)
                {
                    EdgeScript edgeScript = edge.GetComponent<EdgeScript>();
                    foreach (GameObject edge2 in cellScript.edges)
                    {
                        EdgeScript edgeScript2 = edge2.GetComponent<EdgeScript>();
                        if (edge != edge2 && edgeScript.accessible == true && edgeScript2.accessible == true)
                        {
                            foreach (GameObject entranceNode in edgeScript.entranceNodes)
                            {
                                foreach (GameObject exitNode in edgeScript2.exitNodes)
                                {
                                    if (cellScript.type == "star")
                                    {
                                        entranceNode.GetComponent<NodeScript>().connectedNodes.Add(exitNode);
                                    }
                                    else if(cellScript.type == "tween")
                                    {
                                        exitNode.GetComponent<NodeScript>().connectedNodes.Add(entranceNode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Create car
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            // Get potential nodes
            List<GameObject> potentialNodes = new List<GameObject>();
            foreach (GameObject edge in edgeList)
            {
                EdgeScript edgeScript = edge.GetComponent<EdgeScript>();
                if (edgeScript.accessible == true)
                {
                    foreach (GameObject node in edgeScript.entranceNodes)
                    {
                        potentialNodes.Add(node);
                    }
                }
            }

            // Choose node
            GameObject chosenNode = potentialNodes[Random.Range(0, potentialNodes.Count - 1)];

            // Spawn car
            GameObject car = Instantiate(carPrefab);
            car.GetComponent<CarScript>().targetNode = chosenNode;
            car.transform.position = chosenNode.transform.position;
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


    void CreateNodeFromCursor()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject node = Instantiate(nodePrefab, cursor.transform.position, new Quaternion());
            node.GetComponent<NodeScript>().type = "star";
            nodeList.Add(node);
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
    void CreateTweenNodes() // Subdivides nodeweb/graph
    {
        // Turn all current nodes into star nodes so repeated subdivision is possible
        foreach (GameObject node in nodeList)
        {
            if (node.GetComponent<NodeScript>().type != "star")
            {
                node.GetComponent<NodeScript>().type = "star";
            }
        }

        // Create new tween nodes
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
                    connectedNodeScript.connectedNodes.Remove(node);

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
                tweenNode.GetComponent<NodeScript>().type = "tween";
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

    List<List<GameObject>> DetectLoops(List<GameObject> nodes)
    {
        // Create loop list
        List<List<GameObject>> loopList = new List<List<GameObject>>();

        // Create connection list
        List<Connection> connections = new List<Connection>();

        // Add all connections
        foreach (GameObject node in nodes)
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
            GameObject toNode = connection.to;

            // Left loop
            List<GameObject> loop = new List<GameObject>();
            GameObject firstNode = fromNode;
            GameObject lastNode = fromNode;
            GameObject currentNode = toNode;
            loop.Add(firstNode);
            while (currentNode != firstNode && currentNode.GetComponent<NodeScript>().connectedNodes.Count > 1)
            {
                // Get scripts
                NodeScript currentScript = currentNode.GetComponent<NodeScript>();
                NodeScript lastScript = lastNode.GetComponent<NodeScript>();

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
            }

            // Add if loop is valid
            if (currentNode == firstNode)
            {
                loopList.Add(loop); // Seems to add correct loop, but also some invalids, or possibly slightly different duplicates?

                // I wonder if perhaps it is reversing, and counting the previous item as a loop?
            }
        }

        // Return looplist
        return loopList;
    }

    List<List<GameObject>> CreateUniqueLoopList(List<List<GameObject>> loopList)
    {
        // Create loop list
        List<List<GameObject>> uniqueLoopList = new List<List<GameObject>>(); // NEED TO CHECK IF THIS ALGORITHM GIVES FALSE POSITIVE ON DIFFERENT SIZED LOOPS

        // Iterate through loops
        foreach (List<GameObject> loop in loopList)
        {
            // Find if loop exists in unique loops
            bool loopFound = false;
            foreach (List<GameObject> uniqueLoop in uniqueLoopList)
            {
                // See if loops are identical (same nodes)
                bool containsSameNodes = true;
                foreach (GameObject node in loop)
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

        // Return unique loop list
        return uniqueLoopList;
    }

    void CreateFillNodes()
    {
        // Create loop list
        List<List<GameObject>> loopList = new List<List<GameObject>>();

        // Populate looplist
        loopList = DetectLoops(nodeList);

        // Make only unique set of loops
        loopList = CreateUniqueLoopList(loopList);

        // Remove largest loop as it is an inverse loop
        if (loopList.Count > 1)
        {
            List<GameObject> biggestLoop = loopList[0];
            foreach (List<GameObject> loop in loopList)
            {
                if (loop.Count > biggestLoop.Count)
                {
                    biggestLoop = loop;
                }
            }
            loopList.Remove(biggestLoop);
        }

        // For every loop, generate node in the middle
        foreach (List<GameObject> loop in loopList)
        {
            Vector3 averagePosition = Vector3.zero;
            int count = 0;
            foreach (GameObject node in loop)
            {
                averagePosition += node.transform.position;
                count++;
            }
            averagePosition /= count;

            // Instantiate node
            GameObject fillNode = Instantiate(nodePrefab, averagePosition, Quaternion.identity);

            // Set node connections to all surrounding tween nodes
            NodeScript fillNodeScript = fillNode.GetComponent<NodeScript>();
            foreach (GameObject tweenNode in loop)
            {
                NodeScript tweenNodeScript = tweenNode.GetComponent<NodeScript>();
                if (tweenNodeScript.type == "tween")
                {
                    fillNodeScript.connectedNodes.Add(tweenNode);
                    tweenNodeScript.connectedNodes.Add(fillNode);
                }
            }

            // Set type
            fillNodeScript.type = "fill";

            // Add to list
            nodeList.Add(fillNode);
        }
    }

    void GenerateStarCellEdges(GameObject originCell, float radius)
    {
        // Original node
        CellScript originCellScript = originCell.GetComponent<CellScript>();
        Vector3 originPoint = originCell.transform.position;

        // Get connected nodes
        List<GameObject> connectedCells = originCellScript.connectedCells;

        // Create connection midpoints
        List<Vector3> connectionPoints = new List<Vector3>();
        foreach (GameObject connectedCell in connectedCells)
        {
            Vector3 connectionPoint = connectedCell.transform.position;
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
        }

        // Create corner nodes
        List<GameObject> cellCornerNodes = new List<GameObject>();
        foreach (Vector3 lerpedMidPoint in connectionMidPoints)
        {
            // Debug
            GameObject node = Instantiate(nodePrefab, lerpedMidPoint, new Quaternion(), originCell.transform);
            node.GetComponent<NodeScript>().positionColour = Color.green;
            cellCornerNodes.Add(node);
        }

        // Connect corner nodes
        for (int i = 0; i < cellCornerNodes.Count; i++)
        {
            //GameObject cellCornerNode1, cellCornerNode2;
            //cellCornerNode1 = cellCornerNodes[i];
            //if (i < cellCornerNodes.Count - 1) { cellCornerNode2 = cellCornerNodes[i + 1]; }
            //else { cellCornerNode2 = cellCornerNodes[0]; }
        }

        // Create cell edges based on nodes
        GameObject firstNode, secondNode;
        for (int i = 1; i <= cellCornerNodes.Count; i++)
        {
            firstNode = cellCornerNodes[i - 1];
            if (i == cellCornerNodes.Count)
            {
                secondNode = cellCornerNodes[0];
            }
            else
            {
                secondNode = cellCornerNodes[i];
            }

            Vector3 positionMid = Vector3.Lerp(firstNode.transform.position, secondNode.transform.position, 0.5f);

            GameObject edge = Instantiate(edgePrefab, originCell.transform);
            edge.transform.position = positionMid;
            EdgeScript edgeScript = edge.GetComponent<EdgeScript>();

            edgeScript.fromNode = firstNode;
            edgeScript.toNode = secondNode;

            originCellScript.edges.Add(edge);

            edgeList.Add(edge);
        }

        // Connect edges to their connecting cells
        foreach (GameObject edge in originCellScript.edges)
        {
            // Work out what cell the edge should connect to based on angle difference between cell->edge and cell->connectedCell
            EdgeScript edgeScript = edge.GetComponent<EdgeScript>();
            GameObject cellToConnectTo = null;
            float maxDegreesDifference = 20.0f;
            foreach (GameObject connectedCell in connectedCells)
            {
                // Vectors
                Vector3 vectorToCell = connectedCell.transform.position - originCell.transform.position;
                Vector3 vectorToEdge = edge.transform.position - originCell.transform.position;

                // Angles
                float angleBetween = Vector3.SignedAngle(vectorToCell, vectorToEdge, Vector3.up);

                // Comparison
                if (Mathf.Abs(angleBetween) < maxDegreesDifference)
                {
                    cellToConnectTo = connectedCell;
                }
            }
            edgeScript.fromCell = originCell;
            edgeScript.toCell = cellToConnectTo; 
            /*
             * Sometimes, unfortunately, the toCell stays null
             * This appears to be due to floating point errors when getting the two angles
             * A wide margin of error is fixing this, but I eventually will need a more
             * robust solution to tie connection and edge together
             */
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
    public GameObject cellPrefab;
    public GameObject edgePrefab;
    public GameObject carPrefab;
    public List<GameObject> nodeList;
    public List<GameObject> cellList;
    public List<GameObject> edgeList;
    public GameObject selection;
    public GameObject cursor;
}
