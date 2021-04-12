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

class GameObjectAxisAngleComparer : IComparer<GameObject>
{
    public GameObjectAxisAngleComparer(Vector3 argOriginPoint)
    {
        originPoint = argOriginPoint;
    }
    public int Compare(GameObject gameObject1, GameObject gameObject2)
    {
        Vector3 vector1 = (originPoint - gameObject1.transform.position).normalized;
        Vector3 vector2 = (originPoint - gameObject2.transform.position).normalized;
        float angle1 = Vector3.SignedAngle(Vector3.forward, vector1, Vector3.up);
        float angle2 = Vector3.SignedAngle(Vector3.forward, vector2, Vector3.up);

        // Wrap negatives
        if (angle1 < 0) { angle1 += 360; }
        if (angle2 < 0) { angle2 += 360; }

        return angle1.CompareTo(angle2);
    }

    public Vector3 originPoint;
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
                }
                drawFromNode = GetOrCreateSnappableNode(cursor.transform.position);
                if (drawFromNode != null)
                {
                    NodeScript script = drawFromNode.GetComponent<NodeScript>();
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
                }
                drawToNode = GetOrCreateSnappableNode(cursor.transform.position); // that isnt draw from node
                if (drawFromNode != null)
                {
                    NodeScript script = drawFromNode.GetComponent<NodeScript>();
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

            // Remove connections from and to the fromline and toline
            for (int i = 0; i < connections.Count; i++)
            {
                Connection connection = connections[i];
                if (connection.from == drawFromNode || connection.from == drawToNode ||
                    connection.to == drawFromNode || connection.to == drawToNode)
                {
                    connections.RemoveAt(i);
                    i--;
                }
            }

            Connection mainConnection = new Connection(drawFromNode, drawToNode);
            if (drawFromNode != null)
            {
                NodeScript script = drawFromNode.GetComponent<NodeScript>();
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

        // Subdivide nodes
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SubdivideNodeList(nodeList);
        }

        // Create all cells as children of their parent nodes and inherit connections
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Need to sort connection order for all nodes clockwise here before inheriting
            foreach (GameObject node in nodeList)
            {
                // Sort node connections clockwise
                node.GetComponent<NodeScript>().connectedNodes.Sort(new GameObjectAxisAngleComparer(node.transform.position));
            }

            // Now need to insert null (border) connections for tween nodes
            foreach (GameObject node in nodeList)
            {
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                if (nodeScript.type == "tween")
                {
                    for (int i = 0; i < nodeScript.connectedNodes.Count; i++)
                    {
                        // Indexes
                        int firstIndex = i;
                        int secondIndex = i + 1;
                        if (secondIndex == nodeScript.connectedNodes.Count)
                        {
                            secondIndex = 0;
                        }

                        // Nodes
                        GameObject firstNode = nodeScript.connectedNodes[firstIndex];
                        GameObject secondNode = nodeScript.connectedNodes[secondIndex];

                        // Create null in between if both are star (no fill in between)
                        if (firstNode.GetComponent<NodeScript>().type == "star" && secondNode.GetComponent<NodeScript>().type == "star")
                        {
                            nodeScript.connectedNodes.Insert(secondIndex, null); // could possibly make these actual objects, but inaccessible?
                            break;
                        }
                    }
                }
            }

            // Possibly could be interconnecting extra star node connections here to connect to fill nodes? (instead of using cells)
            // Makes sense considering that it does in effect *actually* connect the cells together spacially

            // This would probably apply to in-between border connections too

            // If this is implemented, it would mean that edge generation is much simpler. It just takes connections as a parameter and creates edges accordingly
            // *however*, in the case of null edges this is a little complicated, as the direction of them is not as obvious

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

                // Inherit (non-null) connections
                foreach (GameObject connectedNode in nodeScript.connectedNodes)
                {
                    if (connectedNode != null)
                    {
                        cellScript.connectedCells.Add(connectedNode.GetComponent<NodeScript>().cell);
                    }
                    else
                    {
                        cellScript.connectedCells.Add(null);
                    }
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
                    GenerateStarCellEdges(cell, roadWidth);
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

                // Manage edges to adjacents and borders
                if (nodeScript.type == "tween")
                {
                    // Adopt edges of adjacent star cells
                    for (int i = 0; i < cellScript.connectedCells.Count; i++)
                    {
                        GameObject connectedCell = cellScript.connectedCells[i];

                        // Generate edge differently depending on connected type
                        if (connectedCell != null)
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
                            else if (connectedCellScript.type == "fill") // need to make sure this is set
                            {
                                // Get adjacent cell connections
                                GameObject clockwiseCell;
                                GameObject antiClockwiseCell;
                                if (i < cellScript.connectedCells.Count - 1)
                                {
                                    clockwiseCell = cellScript.connectedCells[i + 1];
                                }
                                else
                                {
                                    clockwiseCell = cellScript.connectedCells[0];
                                }
                                if (i > 0)
                                {
                                    antiClockwiseCell = cellScript.connectedCells[i - 1];
                                }
                                else
                                {
                                    antiClockwiseCell = cellScript.connectedCells[cellScript.connectedCells.Count - 1];
                                }

                                // Get edges
                                GameObject clockwiseEdge = null;
                                foreach (GameObject edge in clockwiseCell.GetComponent<CellScript>().edges)
                                {
                                    if (edge.GetComponent<EdgeScript>().toCell == cell)
                                    {
                                        clockwiseEdge = edge;
                                    }
                                }
                                GameObject antiClockwiseEdge = null;
                                foreach (GameObject edge in antiClockwiseCell.GetComponent<CellScript>().edges)
                                {
                                    if (edge.GetComponent<EdgeScript>().toCell == cell)
                                    {
                                        antiClockwiseEdge = edge;
                                    }
                                }

                                // Create connecting edge (between this and fill cell)
                                GameObject newEdge = Instantiate(edgePrefab, cell.transform);
                                EdgeScript newEdgeScript = newEdge.GetComponent<EdgeScript>();
                                newEdgeScript.fromNode = clockwiseEdge.GetComponent<EdgeScript>().toNode;
                                newEdgeScript.toNode = antiClockwiseEdge.GetComponent<EdgeScript>().fromNode;
                                newEdgeScript.fromCell = cell;
                                newEdgeScript.toCell = connectedCell;
                                cellScript.edges.Add(newEdge);
                                edgeList.Add(newEdge);

                                // Set accessibility
                                newEdgeScript.accessible = false;
                            }
                        }
                        else // Generate null (filler) edge if null connection
                        {
                            // Get adjacent cell connections
                            GameObject clockwiseCell;
                            GameObject antiClockwiseCell;
                            if (i < cellScript.connectedCells.Count - 1)
                            {
                                clockwiseCell = cellScript.connectedCells[i + 1];
                            }
                            else
                            {
                                clockwiseCell = cellScript.connectedCells[0];
                            }
                            if (i > 0)
                            {
                                antiClockwiseCell = cellScript.connectedCells[i - 1];
                            }
                            else
                            {
                                antiClockwiseCell = cellScript.connectedCells[cellScript.connectedCells.Count - 1];
                            }

                            // Get edges
                            GameObject clockwiseEdge = null;
                            foreach (GameObject edge in clockwiseCell.GetComponent<CellScript>().edges)
                            {
                                if (edge.GetComponent<EdgeScript>().toCell == cell)
                                {
                                    clockwiseEdge = edge;
                                }
                            }
                            GameObject antiClockwiseEdge = null;
                            foreach (GameObject edge in antiClockwiseCell.GetComponent<CellScript>().edges)
                            {
                                if (edge.GetComponent<EdgeScript>().toCell == cell)
                                {
                                    antiClockwiseEdge = edge;
                                }
                            }

                            // Create border edge
                            GameObject newEdge = Instantiate(edgePrefab, cell.transform);
                            EdgeScript newEdgeScript = newEdge.GetComponent<EdgeScript>();
                            newEdgeScript.fromNode = clockwiseEdge.GetComponent<EdgeScript>().toNode;
                            newEdgeScript.toNode = antiClockwiseEdge.GetComponent<EdgeScript>().fromNode;
                            cellScript.edges.Add(newEdge);
                            edgeList.Add(newEdge);

                            // Set edge accessibility
                            newEdgeScript.accessible = false;
                        }
                    }
                }

                // Add corner nodes
                foreach (GameObject edge in cellScript.edges)
                {
                    EdgeScript edgeScript = edge.GetComponent<EdgeScript>();
                    cellScript.cornerNodes.Add(edgeScript.fromNode);
                }
                cellScript.edges.Sort(new GameObjectAxisAngleComparer(node.transform.position));
            }
        }

        // Adopt edges from tween to fill cells
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            foreach (GameObject cell in cellList)
            {
                // Get script
                CellScript cellScript = cell.GetComponent<CellScript>();

                // Adopt edges of adjacent tween cells
                if (cellScript.type == "fill")
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
                                cellScript.cornerNodes.Add(connectedCellEdge.GetComponent<EdgeScript>().fromNode);
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

        // Generate meshes
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            UpdateCityConstraints();
            foreach (GameObject cell in cellList)
            {
                // Get cell script
                CellScript cellScript = cell.GetComponent<CellScript>();

                // Create meshes based on cell type
                if (cellScript.type == "fill")
                {
                    // Work out material based on range
                    float cellDistance = Vector3.Distance(centre, cell.transform.position);
                    Material material;
                    if (cellDistance < downTownRange)
                    {
                        material = downTownMaterial;
                    }
                    else if (cellDistance < midTownRange)
                    {
                        material = midTownMaterial;
                    }
                    else
                    {
                        material = upTownMaterial;
                    }

                    // Create meshes based on subcells
                    List<GameObject> subCellList = SubdivideCell(cell, 2);
                    foreach (GameObject subCell in subCellList)
                    {
                        CreateBuildingMesh(subCell, material);
                    }
                }
                else if (cellScript.type == "star")
                {
                    CreateFloorMesh(cell, roadCellMaterial);
                }
                else if (cellScript.type == "tween")
                {
                    CreateFloorMesh(cell, roadCellMaterial);
                }
            }
        }

        // Create car
        if (Input.GetKeyDown(KeyCode.Alpha0))
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
        SubdivideNodeList(nodeList);
    }

    void GenerateStarCellEdges(GameObject originCell, float radius)
    {
        // Original node
        CellScript originCellScript = originCell.GetComponent<CellScript>();
        Vector3 originPoint = originCell.transform.position;

        // Get connected nodes
        List<GameObject> connectedCells = originCellScript.connectedCells;

        // Create edges at connection midpoints
        List<GameObject> connectionEdges = new List<GameObject>();
        foreach (GameObject connectedCell in connectedCells)
        {
            // Get position
            Vector3 connectionPoint = connectedCell.transform.position;
            Vector3 connectionVector = (originPoint - connectionPoint).normalized;
            Vector3 connectionMidPoint = originPoint - (connectionVector * radius); // was -
            
            // Create edge
            GameObject connectionEdge = Instantiate(edgePrefab, connectionMidPoint, Quaternion.identity, originCell.transform);
            connectionEdge.GetComponent<EdgeScript>().fromCell = originCell;
            connectionEdge.GetComponent<EdgeScript>().toCell = connectedCell;
            connectionEdges.Add(connectionEdge);
            edgeList.Add(connectionEdge);
        }

        // Sort edges by angle
        connectionEdges.Sort(new GameObjectAxisAngleComparer(originPoint));

        // Create in-between edges
        for (int i = 0; i < connectionEdges.Count; i++)
        {
            // Get edges
            GameObject edge1 = connectionEdges[i];
            GameObject edge2;
            if (i != connectionEdges.Count-1) { edge2 = connectionEdges[i + 1]; }
            else 
            { 
                edge2 = connectionEdges[0]; // also need to increment angle by 360
            }

            // Get angles
            Vector3 vector1 = (originPoint - edge1.transform.position).normalized;
            Vector3 vector2 = (originPoint - edge2.transform.position).normalized;
            float angle1 = Vector3.SignedAngle(Vector3.forward, vector1, Vector3.up); // maybe always add 180
            float angle2 = Vector3.SignedAngle(Vector3.forward, vector2, Vector3.up);
            if (angle1 < 0) { angle1 += 360; }
            if (angle2 < 0) { angle2 += 360; }
            if (i == connectionEdges.Count - 1) // For looping, add 360 degrees
            {
                angle2 += 360.0f;
            }

            // Difference
            float angleDifference = angle2 - angle1;

            // Count in betweens
            int inBetweenAngleCount = (int)Mathf.Round(angleDifference / 90)-1;
            inBetweenAngleCount = Mathf.Max(inBetweenAngleCount, 0);
            float inBetweenAngleDistance = angleDifference / (inBetweenAngleCount+1);

            // Create in betweens
            for (int j = 0; j < inBetweenAngleCount; j++)
            {
                // Create edge
                GameObject edgeToInsert = Instantiate(edgePrefab, originCell.transform);
                edgeList.Add(edgeToInsert);

                // Get position
                float inBetweenAngle = angle1 + (inBetweenAngleDistance * (j+1));
                Quaternion inBetweenQuaternion = Quaternion.AngleAxis(inBetweenAngle, Vector3.up); // needs to be changed back to -+?
                Vector3 inBetweenVector = inBetweenQuaternion * Vector3.forward;
                Vector3 inBetweenPosition = originPoint - (inBetweenVector * radius); // why - tho

                // Set angle
                edgeToInsert.transform.position = inBetweenPosition;
                connectionEdges.Insert(i + j + 1, edgeToInsert);

                // Set accessibility
                edgeToInsert.GetComponent<EdgeScript>().accessible = false;
            }
            if (i > 1000) { break; }
            i += inBetweenAngleCount;
        }

        // Re-sort edges (shouldn't need to do this actually)
        connectionEdges.Sort(new GameObjectAxisAngleComparer(originPoint));

        // Create corner nodes (can combine this with last function later
        for (int i = 0; i < connectionEdges.Count; i++)
        {
            // Get edges
            GameObject edge1 = connectionEdges[i];
            GameObject edge2;
            if (i != connectionEdges.Count - 1) 
            { 
                edge2 = connectionEdges[i + 1]; 
            }
            else
            {
                edge2 = connectionEdges[0]; // also need to increment angle by 360
            }

            // Get in between angle
            Vector3 vector1 = (originPoint - edge1.transform.position).normalized;
            Vector3 vector2 = (originPoint - edge2.transform.position).normalized;
            float angle1 = Vector3.SignedAngle(Vector3.forward, vector1, Vector3.up);
            float angle2 = Vector3.SignedAngle(Vector3.forward, vector2, Vector3.up);
            if (angle1 < 0) { angle1 += 360; }
            if (angle2 < 0) { angle2 += 360; }
            if (i == connectionEdges.Count - 1) // For looping, add 360 degrees
            {
                angle2 += 360.0f;
            }
            float tweenAngle = Mathf.Lerp(angle1, angle2, 0.5f);

            // Get in between position
            Quaternion inBetweenQuaternion = Quaternion.AngleAxis(tweenAngle, Vector3.up);
            Vector3 inBetweenVector = inBetweenQuaternion * Vector3.forward;
            Vector3 inBetweenPosition = originPoint - (inBetweenVector * radius);

            // Create corner in between
            GameObject node = Instantiate(nodePrefab, inBetweenPosition, new Quaternion(), originCell.transform);
            node.GetComponent<NodeScript>().positionColour = Color.green;

            // Add to edges
            edge1.GetComponent<EdgeScript>().toNode = node;
            edge2.GetComponent<EdgeScript>().fromNode = node;

            // Add to cell
            originCellScript.cornerNodes.Add(node);
        }

        // Add connected edges to origin cell (maybe to both cells?)
        originCellScript.edges = connectionEdges;
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

        // Make connections unique (might break other things. add bool if so)
        List<Connection> uniqueConnections = new List<Connection>();
        foreach (Connection connection in nodeConnections)
        {
            bool isUnique = true;
            foreach (Connection otherConnection in nodeConnections)
            {
                if (connection.from == otherConnection.to && connection.to == otherConnection.from)
                {
                    isUnique = false;
                }
            }
            if (isUnique)
            {
                uniqueConnections.Add(connection);
            }
        }

        return uniqueConnections;
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

    List<Connection> GetConnections(List<GameObject> nodes) // Should probably make this the only variant eventually
    {
        List<Connection> nodeConnections = new List<Connection>();
        foreach (GameObject node in nodes)
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

    List<GameObject> SubdivideNodeList(List<GameObject> nodes)
    {
        // Get connections
        List<Connection> connections = GetConnections(nodes);

        // Subdivide connections
        foreach (Connection connection in connections)
        {
            SubdivideConnection(connection, Vector3.Lerp(connection.from.transform.position, connection.to.transform.position, 0.5f));
        }

        // Create loop list
        List<List<GameObject>> loopList = new List<List<GameObject>>();

        // Populate looplist
        loopList = DetectLoops(nodes);

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
            fillNodeScript.type = "fill"; // Should I pass loop here?

            // Add to list
            nodes.Add(fillNode);
        }

        return nodes;
    }

    List<GameObject> SubdivideCell(GameObject cell, int subdivisions)
    {
        // Create object lists
        List<GameObject> cells = new List<GameObject>();
        List<GameObject> nodes = new List<GameObject>();

        // Get cell script
        CellScript cellScript = cell.GetComponent<CellScript>();

        // Copy cornernodes from cell
        foreach(GameObject node in cellScript.cornerNodes)
        {
            GameObject newNode = Instantiate(nodePrefab, node.transform);
            nodes.Add(newNode);
        }

        // Connect corner nodes
        for (int i = 0; i < nodes.Count; i++)
        {
            int firstIndex = i;
            int secondIndex = i + 1;
            if (secondIndex == nodes.Count) { secondIndex = 0; }

            GameObject firstNode = nodes[firstIndex];
            GameObject secondNode = nodes[secondIndex];

            firstNode.GetComponent<NodeScript>().connectedNodes.Add(secondNode);
            secondNode.GetComponent<NodeScript>().connectedNodes.Add(firstNode);
        }

        // Sort nodes
        nodes.Sort(new GameObjectAxisAngleComparer(cell.transform.position));

        // Subdivide
        for (int i = 0; i < subdivisions; i++)
        {
            SubdivideNodeList(nodes);
        }

        // One last DetectLoops() to make cells
        List<Connection> finalConnections = GetConnections(nodes);

        // Subdivide connections
        foreach (Connection connection in finalConnections)
        {
            SubdivideConnection(connection, Vector3.Lerp(connection.from.transform.position, connection.to.transform.position, 0.5f));
        }

        // Create loop list
        List<List<GameObject>> loopList = new List<List<GameObject>>();

        // Populate looplist
        loopList = DetectLoops(nodes);

        // For every loop, generate cell in the middle
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
            GameObject subCell = Instantiate(cellPrefab, averagePosition, Quaternion.identity, cell.transform);

            // Add corners from loop
            CellScript subCellScript = subCell.GetComponent<CellScript>();
            foreach (GameObject node in loop)
            {
                NodeScript nodeScript = node.GetComponent<NodeScript>();
                if (nodeScript.type != "tween")
                {
                    subCellScript.cornerNodes.Add(node);
                }
            }
            subCellScript.cornerNodes.Sort(new GameObjectAxisAngleComparer(subCell.transform.position));


            // Set type 
            subCellScript.type = "sub_cell"; // Should I pass loop here?

            // Add to list
            cells.Add(subCell);
        }

        // Return
        return cells;
    }

    GameObject SubdivideConnection(Connection connection, Vector3 point)
    {
        // Create node
        GameObject node = Instantiate(nodePrefab, point, Quaternion.identity);
        node.GetComponent<NodeScript>().type = "tween";
        nodeList.Add(node);

        // Break old connections
        connection.from.GetComponent<NodeScript>().connectedNodes.Remove(connection.to);
        connection.to.GetComponent<NodeScript>().connectedNodes.Remove(connection.from);

        // Form new connections
        connection.from.GetComponent<NodeScript>().connectedNodes.Add(node);
        connection.to.GetComponent<NodeScript>().connectedNodes.Add(node);
        node.GetComponent<NodeScript>().connectedNodes.Add(connection.from);
        node.GetComponent<NodeScript>().connectedNodes.Add(connection.to);

        // Change types
        connection.from.GetComponent<NodeScript>().type = "star";
        connection.to.GetComponent<NodeScript>().type = "star";

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

    void CreateFloorMesh(GameObject cell, Material material)
    {
        CellScript cellScript = cell.GetComponent<CellScript>();
        List<Vector3> cornerList = new List<Vector3>();
        foreach (GameObject cornerNode in cellScript.cornerNodes)
        {
            cornerList.Add(cornerNode.transform.position - cell.transform.position); // from cell's transform's perspective
        }
        List<int> triangleList = TriangulateConvexPolygonInts(cornerList);
        MeshFilter cellMeshFilter = cell.GetComponent<MeshFilter>();
        MeshRenderer cellMeshRenderer = cell.GetComponent<MeshRenderer>();
        cellMeshFilter.mesh = new Mesh();
        cellMeshFilter.mesh.vertices = cornerList.ToArray();
        cellMeshFilter.mesh.triangles = triangleList.ToArray();
        cellMeshFilter.mesh.RecalculateNormals();
        cellMeshRenderer.material = material;
    }

    void CreateBuildingMesh(GameObject cell, Material material)
    {
        // Set height
        float heightMinMaxDifference = buildingHeightMax - buildingHeightMin;
        float heightScalar = 1 - (Vector3.Distance(centre, cell.transform.position) / cityRadius);
        float height = buildingHeightMin + (heightMinMaxDifference * heightScalar);
        float heightRandomnessRange = buildingHeightRandomnessScalar * height;
        float heightRandomness = Random.Range(-heightRandomnessRange * height, heightRandomnessRange * height);
        height += heightRandomness;
        height = Mathf.Max(height, 1.0f);

        // Get cell script
        CellScript cellScript = cell.GetComponent<CellScript>();

        // Create face list for mesh generation
        List<List<Vector3>> faces = new List<List<Vector3>>();

        // Get floor face
        List<Vector3> floorFace = new List<Vector3>();
        foreach (GameObject cornerNode in cellScript.cornerNodes)
        {
            floorFace.Add(cornerNode.transform.position - cell.transform.position); // from cell's transform's perspective
        }
        faces.Add(floorFace);

        // Get ceiling face
        List<Vector3> ceilingFace = new List<Vector3>();
        foreach (Vector3 position in floorFace)
        {
            ceilingFace.Add(position + (Vector3.up * height));
        }
        faces.Add(ceilingFace);

        // Create faces for sides
        for (int i = 0; i < floorFace.Count; i++)
        {
            // Get indexes
            int firstIndex = i;
            int secondIndex = i + 1;
            if (secondIndex == floorFace.Count) { secondIndex = 0; }

            // Get corner positions
            Vector3 firstPos = floorFace[firstIndex];
            Vector3 secondPos = floorFace[secondIndex];
            Vector3 thirdPos = secondPos + (Vector3.up * height);
            Vector3 fourthPos = firstPos + (Vector3.up * height);

            // Create face
            List<Vector3> newFace = new List<Vector3>();
            newFace.Add(firstPos);
            newFace.Add(secondPos);
            newFace.Add(thirdPos);
            newFace.Add(fourthPos);

            // Add face to facelist
            faces.Add(newFace);
        }

        // Create meshes for all faces
        foreach (List<Vector3> face in faces)
        {
            // Create face object
            GameObject faceObject = Instantiate(facePrefab,cell.transform);
            MeshFilter faceMeshFilter = faceObject.GetComponent<MeshFilter>();
            MeshRenderer faceMeshRenderer = faceObject.GetComponent<MeshRenderer>();

            // Get tri list
            List<int> triangleList = TriangulateConvexPolygonInts(face);
            faceMeshFilter.mesh = new Mesh();
            faceMeshFilter.mesh.vertices = face.ToArray();
            faceMeshFilter.mesh.triangles = triangleList.ToArray();
            faceMeshFilter.mesh.RecalculateNormals();
            faceMeshRenderer.material = material;
        }
    }

    void UpdateCityConstraints()
    {
        // Get centre
        centre = Vector3.zero;
        foreach (GameObject node in nodeList)
        {
            centre += node.transform.position;
        }
        centre /= nodeList.Count;

        // Find radius from furthest point
        cityRadius = 0.0f;
        foreach (GameObject node in nodeList)
        {
            float distance = Vector3.Distance(node.transform.position,centre);
            if (distance > cityRadius)
            {
                cityRadius = distance;
            }
        }

        // Town ranges
        midTownRange = cityRadius * 0.5f;
        downTownRange = cityRadius * 0.25f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(cursor.transform.position, 1.0f);
        if (selection != null) { Gizmos.DrawLine(cursor.transform.position, selection.transform.position); }
    }

    // Prefabs
    public GameObject nodePrefab;
    public GameObject cellPrefab;
    public GameObject edgePrefab;
    public GameObject facePrefab; // This should probably be renamed to MeshObject or something
    public GameObject carPrefab;
    public List<GameObject> nodeList;
    public List<GameObject> cellList;
    public List<GameObject> edgeList;

    // Generation rules
    public float roadWidth;
    public float buildingHeightMax;
    public float buildingHeightMin;
    public float cityRadius;
    public float buildingHeightRandomnessScalar;
    public Vector3 centre;
    float midTownRange = 20.0f;
    float downTownRange = 10.0f;

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

    // Visual
    public Material roadCellMaterial;
    public Material buildingCellMaterial;
    public Material upTownMaterial;
    public Material midTownMaterial;
    public Material downTownMaterial;
}
