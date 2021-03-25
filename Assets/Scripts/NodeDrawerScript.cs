using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;


class Triangle
{
    // Construct
    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
    }

    //Corners
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;
}
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
        cursorState = "idle";
        selection = null;
        floorCollider = floor.GetComponent<Collider>();
        nodeList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {

        // New interface code
        if (Input.GetMouseButtonDown(0))
        {
            // Snap from
            if (snapFrom)
            {
                if (drawFromNode != null)
                {
                    NodeScript script = drawFromNode.GetComponent<NodeScript>();
                    Debug.Log("About to get draw from node. Count: " + script.connectedNodes.Count);
                }
                drawFromNode = GetOrCreateSnappableNode(cursor.transform.position);
                if (drawFromNode != null)
                {
                    NodeScript script = drawFromNode.GetComponent<NodeScript>();
                    Debug.Log("Got draw from node. Count: " + script.connectedNodes.Count);
                }
            }
            else
            {
                // Create
                drawFromNode = Instantiate(nodePrefab, cursor.transform.position, new Quaternion());
                drawFromNode.GetComponent<NodeScript>().type = "star";
                nodeList.Add(drawFromNode);
            }

            // Set state
            cursorState = "active";
        }
        if (Input.GetMouseButtonUp(0))
        {
            // Get draw to node
            if (snapTo)
            {
                if (drawFromNode != null)
                {
                    NodeScript script = drawFromNode.GetComponent<NodeScript>();
                    Debug.Log("About to get draw to. Count: " + script.connectedNodes.Count);
                }
                drawToNode = GetOrCreateSnappableNode(cursor.transform.position); // that isnt draw from node
                if (drawFromNode != null)
                {
                    NodeScript script = drawFromNode.GetComponent<NodeScript>();
                    Debug.Log("Got draw to node. Count: " + script.connectedNodes.Count);
                }
            }
            else
            {
                drawToNode = Instantiate(nodePrefab, cursor.transform.position, new Quaternion());
                drawToNode.GetComponent<NodeScript>().type = "star";
                nodeList.Add(drawToNode);
            }

            // Create intersecting nodes
            List<GameObject> intersectingNodes = new List<GameObject>();
            List<Connection> connections = GetConnections(); // Problem is here. Has doubling
            Connection mainConnection = new Connection(drawFromNode, drawToNode);
            if (drawFromNode != null)
            {
                NodeScript script = drawFromNode.GetComponent<NodeScript>();
                Debug.Log("About to do intersections. Count: " + script.connectedNodes.Count);
            }
            foreach (Connection connection in connections)
            {
                // Points
                Vector3 intersectionPoint1 = Vector3.zero;
                Vector3 intersectionPoint2 = Vector3.zero;
                Vector3 fromPoint1 = mainConnection.from.transform.position;
                Vector3 toPoint1 = mainConnection.to.transform.position;
                Vector3 fromPoint2 = connection.from.transform.position;
                Vector3 toPoint2 = connection.to.transform.position;

                // Subdivide if intersecting
                if (ClosestPointsOnTwoLines(out intersectionPoint1, out intersectionPoint2, fromPoint1, toPoint1, fromPoint2, toPoint2)) // Lines are infinite?
                {
                    if (Vector3.Distance(intersectionPoint1, intersectionPoint2) < 0.1f) // rephrase this as closestPoints, not intersection, and define intersection by epsilon
                    {
                        GameObject subdividedNode = SubdivideConnection(connection, intersectionPoint1);
                        intersectingNodes.Add(subdividedNode);
                    }
                }
            }

            // Connect all nodes (including from/to and all intersecting)
            if (drawFromNode != null)
            {
                NodeScript script = drawFromNode.GetComponent<NodeScript>();
                Debug.Log("About to insert into final list. Count: " + script.connectedNodes.Count);
            }
            intersectingNodes.Insert(0, drawFromNode);
            intersectingNodes.Add(drawToNode);
            for (int i = 1; i < intersectingNodes.Count; i++)
            {
                GameObject fromNode = intersectingNodes[i - 1];
                GameObject toNode = intersectingNodes[i];
                fromNode.GetComponent<NodeScript>().connectedNodes.Add(toNode);
                toNode.GetComponent<NodeScript>().connectedNodes.Add(fromNode);
            }

            // Reset state
            cursorState = "idle";
        }

        UpdateCursor();
        //CreateNodeFromCursor();
        //ConnectNodes();

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
                    tweenEdge1.GetComponent<EdgeScript>().fromNode = fromEdge.GetComponent<EdgeScript>().fromNode;
                    tweenEdge1.GetComponent<EdgeScript>().toNode = toEdge.GetComponent<EdgeScript>().toNode;
                    GameObject tweenEdge2 = Instantiate(edgePrefab, cell.transform);
                    tweenEdge2.GetComponent<EdgeScript>().toNode = fromEdge.GetComponent<EdgeScript>().toNode;
                    tweenEdge2.GetComponent<EdgeScript>().fromNode = toEdge.GetComponent<EdgeScript>().fromNode;
                    cellScript.edges.Add(tweenEdge1);
                    cellScript.edges.Add(tweenEdge2);

                    edgeList.Add(tweenEdge1);
                    edgeList.Add(tweenEdge2);

                    // Get corners for mesh generation
                    cellScript.cornerNodes.Add(tweenEdge1.GetComponent<EdgeScript>().fromNode);
                    cellScript.cornerNodes.Add(tweenEdge1.GetComponent<EdgeScript>().toNode);
                    cellScript.cornerNodes.Add(tweenEdge2.GetComponent<EdgeScript>().fromNode);
                    cellScript.cornerNodes.Add(tweenEdge2.GetComponent<EdgeScript>().toNode);

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

        // Generate star cell edges
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            foreach (GameObject cell in cellList)
            {
                CellScript cellScript = cell.GetComponent<CellScript>();
                List<Vector3> cornerList = new List<Vector3>();
                foreach (GameObject cornerNode in cellScript.cornerNodes)
                {
                    cornerList.Add(cornerNode.transform.position - cell.transform.position); // from cell's transform's perspective
                }
                List<int> triangleList = TriangulateConvexPolygonInts(cornerList);
                MeshFilter cellMeshFilter = cell.GetComponent<MeshFilter>();
                cellMeshFilter.mesh = new Mesh();
                cellMeshFilter.mesh.vertices = cornerList.ToArray();
                cellMeshFilter.mesh.triangles = triangleList.ToArray();
            }
        }
    }

    List<Triangle> TriangulateConvexPolygon(List<Vector3> convexHullpoints)
    {
        List<Triangle> triangles = new List<Triangle>();

        for (int i = 2; i < convexHullpoints.Count; i++)
        {
            Vector3 a = convexHullpoints[0];
            Vector3 b = convexHullpoints[i - 1];
            Vector3 c = convexHullpoints[i];

            triangles.Add(new Triangle(a, b, c));
        }

        return triangles;
    }

    List<int> TriangulateConvexPolygonInts(List<Vector3> convexHullpoints)
    {
        List<int> triangleInts = new List<int>();

        for (int i = 2; i < convexHullpoints.Count; i++)
        {
            Vector3 a = convexHullpoints[0];
            Vector3 b = convexHullpoints[i - 1];
            Vector3 c = convexHullpoints[i];

            triangleInts.Add(0);
            triangleInts.Add(i-1);
            triangleInts.Add(i);
        }

        return triangleInts;
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
        switch (cursorState)
        {
            case "idle":
                if (Input.GetMouseButtonDown(1))
                {
                    selection = GetClosestNode();
                    if (selection != null) { cursorState = "active"; }
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
                    cursorState = "idle";
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
        originCellScript.cornerNodes = cellCornerNodes;

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

    List<Connection> GetConnections()
    {
        List<Connection> nodeConnections = new List<Connection>();
        foreach (GameObject node in nodeList)
        {
            NodeScript nodeScript = node.GetComponent<NodeScript>();
            foreach (GameObject connectedNode in nodeScript.connectedNodes)
            {
                // Look for duplicate connection
                bool found = false;
                foreach (Connection connection in nodeConnections)
                {
                    if (connection.from == connectedNode && connection.to == node)
                    {
                        found = true;
                    }
                }    

                // Create connection object
                if (!found)
                {
                    Connection nodeConnection = new Connection(node, connectedNode);
                    nodeConnections.Add(nodeConnection);
                }
            }
        }
        return nodeConnections;
    }

    List<Connection> GetConnections2() // Why did I have this variant? Is it still needed? Might need to replace something with this one
    {
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
        return nodeConnections;
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

    // From https://wiki.unity3d.com/index.php/3d_Math_functions (I have edited it to be non-infinite, ie. uses line segments, not lines)
    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 line1Start, Vector3 line1End, Vector3 line2Start, Vector3 line2End)
    {
        Vector3 lineVec1 = line1End - line1Start;
        Vector3 lineVec2 = line2End - line2Start;
        float line1Length = lineVec1.magnitude;
        float line2Length = lineVec2.magnitude;

        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {

            Vector3 r = line1Start - line2Start;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = line1Start + lineVec1 * s;
            closestPointLine2 = line2Start + lineVec2 * t;

            Vector3 closestVector1 = closestPointLine1 - line1Start;
            Vector3 closestVector2 = closestPointLine2 - line2Start;

            // Some sort of Dot() and length calculation to work out if in the right place?
            // Should not be behind
            // Should not be more than the distance of the vector
            bool closestPoint1NotBehind = (Vector3.Dot(lineVec1, closestVector1) > 0.0f);
            bool closestPoint2NotBehind = (Vector3.Dot(lineVec2, closestVector2) > 0.0f);
            bool closestPoint1NotTooFar = (closestVector1.magnitude < line1Length);
            bool closestPoint2NotTooFar = (closestVector2.magnitude < line1Length);

            if (closestPoint1NotBehind && closestPoint2NotBehind &&
                closestPoint1NotTooFar && closestPoint2NotTooFar)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    // From https://forum.unity.com/threads/math-problem.8114/#post-59715
    Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
    {
        var vVector1 = vPoint - vA;
        var vVector2 = (vB - vA).normalized;

        var d = Vector3.Distance(vA, vB);
        var t = Vector3.Dot(vVector2, vVector1);

        if (t <= 0)
            return vA;

        if (t >= d)
            return vB;

        var vVector3 = vVector2 * t;

        var vClosestPoint = vA + vVector3;

        return vClosestPoint;
    }

    GameObject SubdivideConnection(Connection connection, Vector3 point)
    {
        // Create node
        GameObject node = Instantiate(nodePrefab, point, Quaternion.identity);
        node.GetComponent<NodeScript>().type = "star";
        nodeList.Add(node);

        // Break old connections
        connection.from.GetComponent<NodeScript>().connectedNodes.Remove(connection.to);
        connection.to.GetComponent<NodeScript>().connectedNodes.Remove(connection.from);

        // Form new connections
        connection.from.GetComponent<NodeScript>().connectedNodes.Add(node);
        connection.to.GetComponent<NodeScript>().connectedNodes.Add(node);
        node.GetComponent<NodeScript>().connectedNodes.Add(connection.from);
        node.GetComponent<NodeScript>().connectedNodes.Add(connection.to);

        // Return
        return node;
    }

    GameObject GetOrCreateSnappableNode(Vector3 position) // Should probably rephrase this as just "GetSnappable", and return null if none (maybe GetSnappableLine, and GetSnappableNode)
    {
        Connection closestLine = null;
        GameObject closestNode = null;
        float distanceToClosestNode = 0.0f;
        float distanceToClosestLine = 0.0f;
        Vector3 closestLinePoint = new Vector3(0.0f, 0.0f, 0.0f);

        // Get closest node
        closestNode = GetClosestNode();
        if (closestNode != null)
        {
            NodeScript script = closestNode.GetComponent<NodeScript>();
            Debug.Log("Got closest node. Count: " + script.connectedNodes.Count);
        }
        distanceToClosestNode = 0.0f;
        if (closestNode != null) { distanceToClosestNode = Vector3.Distance(position, closestNode.transform.position); }

        // Get closest line + line point
        if (closestNode != null)
        {
            NodeScript script = closestNode.GetComponent<NodeScript>();
            Debug.Log("About to get connections. Count: " + script.connectedNodes.Count);
        }
        List<Connection> nodeConnections = GetConnections();
        if (closestNode != null)
        {
            NodeScript script = closestNode.GetComponent<NodeScript>();
            Debug.Log("Got connections. Count: " + script.connectedNodes.Count);
        }
        if (nodeConnections.Count > 0)
        {
            // Get closest
            closestLine = nodeConnections[0];
            closestLinePoint = ClosestPointOnLine(closestLine.from.transform.position, closestLine.to.transform.position, position);
            foreach (Connection line in nodeConnections)
            {
                Vector3 linePoint = ClosestPointOnLine(line.from.transform.position, line.to.transform.position, position);
                if (Vector3.Distance(linePoint, position) < Vector3.Distance(closestLinePoint, position))
                {
                    closestLine = line;
                    closestLinePoint = linePoint;
                }
            }
            distanceToClosestLine = Vector3.Distance(position, closestLinePoint);
        }

        // Check if node, line, or neither
        if (closestNode != null && distanceToClosestNode < snapDistance) // Use close node to snap
        {
            if (closestNode != null)
            {
                NodeScript script = closestNode.GetComponent<NodeScript>();
                Debug.Log("About to return closest node. Count: " + script.connectedNodes.Count);
            }
            return closestNode;
        }
        else if (closestLine != null && distanceToClosestLine < snapDistance) // Use close line to snap
        {
            // Subdivide line to create new node
            GameObject node = SubdivideConnection(closestLine, closestLinePoint);

            // Set as draw from node
            return node;
        }
        else // Create a new node
        {
            // Create
            GameObject node = Instantiate(nodePrefab, position, new Quaternion());
            node.GetComponent<NodeScript>().type = "star";
            nodeList.Add(node);
            return node;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(cursor.transform.position, 1.0f);
        if (selection != null) { Gizmos.DrawLine(cursor.transform.position, selection.transform.position); }
    }

    // Generation
    public GameObject nodePrefab;
    public GameObject cellPrefab;
    public GameObject edgePrefab;
    public GameObject carPrefab;
    public List<GameObject> nodeList;
    public List<GameObject> cellList;
    public List<GameObject> edgeList;

    // Interface
    GameObject drawFromNode;
    GameObject drawToNode;
    public GameObject selection;
    public GameObject cursor;
    public string cursorState;
    public GameObject floor;
    Collider floorCollider;
    bool snapFrom = true;
    bool snapTo = true;
    public float snapDistance;
}
