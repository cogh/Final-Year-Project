using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

public class CellCityGenerationScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        nodeGrid = new GameObject[sliceCount, layerCount];
        cellGrid = new GameObject[sliceCount, layerCount - 1];

        CreateCornerNodes();

        ConnectCornerNodes();

        CreateCells();

        ConnectCells();

        CreateEdges();

        ConnectEdges();

        CreateEdgeNodes();

        ConnectEdgeNodes();

        // Attach car to random node
        SpawnCar(4, 4);
    }

    void SpawnCar(int x, int y)
    {
        GameObject car = Instantiate(carPrefab);
        GameObject startingCell = cellGrid[x, y];
        GameObject startingEdge = startingCell.GetComponent<CellScript>().edges["forward"];
        GameObject startingNode = startingEdge.GetComponent<EdgeScript>().entranceNodes[0];
        car.GetComponent<CarScript>().targetNode = startingNode;
        car.transform.position = startingNode.transform.position;
    }

    void ConnectEdgeNodes()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            for (int layerIndex = 0; layerIndex < layerCount - 1; layerIndex++)
            {
                CellScript cellScript = cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>();
                foreach (KeyValuePair<string, GameObject> edge in cellScript.edges)
                {
                    // Get scripts
                    EdgeScript edgeScript = edge.Value.GetComponent<EdgeScript>();

                    // Connect entrance nodes to exit nodes within this cell
                    foreach (GameObject entranceNode in edgeScript.entranceNodes)
                    {
                        NodeScript entranceNodeScript = entranceNode.GetComponent<NodeScript>();
                        foreach (KeyValuePair<string, GameObject> targetEdge in cellScript.edges)
                        {
                            // Get scripts
                            EdgeScript targetEdgeScript = targetEdge.Value.GetComponent<EdgeScript>();
                            foreach (GameObject exitNode in targetEdgeScript.exitNodes)
                            {
                                entranceNodeScript.connectedNodes.Add(exitNode);
                            }
                        }
                    }

                    // Connect exit nodes to entrance nodes of adjacent cells
                    if (edgeScript.targetEdge != null)
                    {
                        EdgeScript adjacentEdgeScript = edgeScript.targetEdge.GetComponent<EdgeScript>();
                        foreach (GameObject exitNode in edgeScript.exitNodes)
                        {
                            foreach (GameObject entranceNode in adjacentEdgeScript.entranceNodes)
                            {
                                exitNode.GetComponent<NodeScript>().connectedNodes.Add(entranceNode);
                            }
                        }
                    }
                }
            }
        }
    }

    void CreateEdgeNodes()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            for (int layerIndex = 0; layerIndex < layerCount - 1; layerIndex++)
            {
                CellScript cellScript = cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>();
                foreach (KeyValuePair<string, GameObject> edge in cellScript.edges)
                {
                    edge.Value.GetComponent<EdgeScript>().CreateNodes();
                }
            }
        }
    }

    void ConnectEdges()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            for (int layerIndex = 0; layerIndex < layerCount - 1; layerIndex++)
            {
                CellScript cellScript = cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>();
                if (cellScript.connectedCells.ContainsKey("forward") &&
                    cellScript.edges.ContainsKey("forward"))
                {
                    EdgeScript edgeScript = cellScript.edges["forward"].GetComponent<EdgeScript>();
                    if (cellScript.connectedCells["forward"].GetComponent<CellScript>().edges.ContainsKey("back")) // Temporary check, what's wrong?
                    {   
                        GameObject targetEdge = cellScript.connectedCells["forward"].GetComponent<CellScript>().edges["back"];
                        edgeScript.ConnectTo(targetEdge);
                    }
                }
                if (cellScript.connectedCells.ContainsKey("right") &&
                    cellScript.edges.ContainsKey("right"))
                {
                    EdgeScript edgeScript = cellScript.edges["right"].GetComponent<EdgeScript>();
                    GameObject targetEdge = cellScript.connectedCells["right"].GetComponent<CellScript>().edges["left"];
                    edgeScript.ConnectTo(targetEdge);
                }
                if (cellScript.connectedCells.ContainsKey("back") &&
                    cellScript.edges.ContainsKey("back"))
                {
                    EdgeScript edgeScript = cellScript.edges["back"].GetComponent<EdgeScript>();
                    GameObject targetEdge = cellScript.connectedCells["back"].GetComponent<CellScript>().edges["forward"];
                    edgeScript.ConnectTo(targetEdge);
                }
                if (cellScript.connectedCells.ContainsKey("left") &&
                    cellScript.edges.ContainsKey("left"))
                {
                    EdgeScript edgeScript = cellScript.edges["left"].GetComponent<EdgeScript>();
                    GameObject targetEdge = cellScript.connectedCells["left"].GetComponent<CellScript>().edges["right"];
                    edgeScript.ConnectTo(targetEdge);
                }
            }
        }
    }

    void CreateEdges()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            for (int layerIndex = 0; layerIndex < layerCount - 1; layerIndex++)
            {
                // Get cell script
                CellScript cellScript = cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>();

                // Corners
                Vector3 outerCCW = cellScript.cornersNodes["outerCCW"].transform.position;
                Vector3 outerCW = cellScript.cornersNodes["outerCW"].transform.position;
                Vector3 innerCW = cellScript.cornersNodes["innerCW"].transform.position;
                Vector3 innerCCW = cellScript.cornersNodes["innerCCW"].transform.position;

                GameObject edgeForward = null, edgeRight = null, edgeBack = null, edgeLeft = null;

                // Forward edge
                if (cellScript.connectedCells.ContainsKey("forward"))
                {
                    edgeForward = Instantiate(edgePrefab, Vector3.Lerp(outerCCW, outerCW, 0.5f), Quaternion.identity);
                    edgeForward.GetComponent<EdgeScript>().point1 = outerCCW;
                    edgeForward.GetComponent<EdgeScript>().point2 = outerCW;
                }

                // Right edge
                if (cellScript.connectedCells.ContainsKey("right"))
                {

                    edgeRight = Instantiate(edgePrefab, Vector3.Lerp(outerCW, innerCW, 0.5f), Quaternion.identity);
                    edgeRight.GetComponent<EdgeScript>().point1 = outerCW;
                    edgeRight.GetComponent<EdgeScript>().point2 = innerCW;
                }

                // Back edge
                if (cellScript.connectedCells.ContainsKey("back"))
                {

                    edgeBack = Instantiate(edgePrefab, Vector3.Lerp(innerCW, innerCCW, 0.5f), Quaternion.identity);
                    edgeBack.GetComponent<EdgeScript>().point1 = innerCW;
                    edgeBack.GetComponent<EdgeScript>().point2 = innerCCW;
                }

                // Left edge
                if (cellScript.connectedCells.ContainsKey("left"))
                {

                    edgeLeft = Instantiate(edgePrefab, Vector3.Lerp(innerCCW, outerCCW, 0.5f), Quaternion.identity);
                    edgeLeft.GetComponent<EdgeScript>().point1 = innerCCW;
                    edgeLeft.GetComponent<EdgeScript>().point2 = outerCCW;
                }

                // Add edges to cell
                if (edgeForward != null) { cellScript.edges.Add("forward", edgeForward); }
                if (edgeRight != null) { cellScript.edges.Add("right", edgeRight); }
                if (edgeBack != null) { cellScript.edges.Add("back", edgeBack); }
                if (edgeLeft != null) { cellScript.edges.Add("left", edgeLeft); }
            }
        }
    }

    void ConnectCells()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            for (int layerIndex = 0; layerIndex < layerCount - 1; layerIndex++)
            {
                // Initialise cells as null
                GameObject cellForward = null;
                GameObject cellRight = null;
                GameObject cellBack = null;
                GameObject cellLeft = null;

                // Set cells to adjacent (including wrapping)
                // Forward
                if (layerIndex + 1 < layerCount - 1)
                {
                    cellForward = cellGrid[sliceIndex, layerIndex + 1];
                }
                // Back
                if (layerIndex - 1 >= 0)
                {
                    cellBack = cellGrid[sliceIndex, layerIndex - 1];
                }
                // Right
                if (sliceIndex + 1 < sliceCount)
                {
                    cellRight = cellGrid[sliceIndex + 1, layerIndex];
                }
                else
                {
                    cellRight = cellGrid[0, layerIndex];
                }
                // Left
                if (sliceIndex - 1 >= 0)
                {
                    cellLeft = cellGrid[sliceIndex - 1, layerIndex];
                }
                else
                {
                    cellLeft = cellGrid[sliceCount - 1, layerIndex];
                }

                // Add cell connections
                if (cellForward != null)
                {
                    cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>().connectedCells.Add("forward", cellForward);
                }
                if (cellRight != null)
                {
                    cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>().connectedCells.Add("right", cellRight);
                }
                if (cellBack != null)
                {
                    cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>().connectedCells.Add("back", cellBack);
                }
                if (cellLeft != null)
                {
                    cellGrid[sliceIndex, layerIndex].GetComponent<CellScript>().connectedCells.Add("left", cellLeft);
                }
            }
        }
    }

    void CreateCells()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            for (int layerIndex = 0; layerIndex + 1 < layerCount; layerIndex++)
            {
                // Get corner nodes
                GameObject innerCW, outerCW, innerCCW, outerCCW;
                if (sliceIndex + 1 < sliceCount)
                {
                    innerCW = nodeGrid[sliceIndex + 1, layerIndex];
                    outerCW = nodeGrid[sliceIndex + 1, layerIndex + 1];
                }
                else
                {
                    innerCW = nodeGrid[0, layerIndex];
                    outerCW = nodeGrid[0, layerIndex + 1];
                }
                innerCCW = nodeGrid[sliceIndex, layerIndex];
                outerCCW = nodeGrid[sliceIndex, layerIndex + 1];

                // Get central position
                Vector3 cellPosition = 
                    (innerCW.transform.position + innerCCW.transform.position + 
                     outerCW.transform.position + outerCCW.transform.position) /  4;

                // Create cell
                GameObject newCell = Instantiate(cellPrefab, cellPosition, new Quaternion());

                // Add corners
                newCell.GetComponent<CellScript>().cornersNodes.Add("innerCW", innerCW);
                newCell.GetComponent<CellScript>().cornersNodes.Add("outerCW", outerCW);
                newCell.GetComponent<CellScript>().cornersNodes.Add("innerCCW", innerCCW);
                newCell.GetComponent<CellScript>().cornersNodes.Add("outerCCW", outerCCW);

                // Reference cell in grid
                cellGrid[sliceIndex, layerIndex] = newCell;
            }
        }
    }

    void CreateCornerNodes()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            float angleNormalized = (float)sliceIndex / sliceCount;
            float angleDegrees = angleNormalized * 360.0f;
            float angleRadians = angleNormalized * Mathf.PI * 2;
            Quaternion angleQuaternion = Quaternion.AngleAxis(angleDegrees, Vector3.up);
            Vector3 angleVector = angleQuaternion * Vector3.forward;
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                float nodeDistance = centreRadius + (layerIndex * layerWidth);
                Vector3 nodePosition = centrePoint + (angleVector * nodeDistance);
                nodeGrid[sliceIndex, layerIndex] = Instantiate(nodePrefab, nodePosition, new Quaternion());
            }
        }
    }

    void ConnectCornerNodes()
    {
        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
        {
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                // Initialise nodes as null
                GameObject nodeForward = null;
                GameObject nodeRight = null;
                GameObject nodeBack = null;
                GameObject nodeLeft = null;

                // Set nodes to adjacent (including wrapping)
                // Forward
                if (layerIndex + 1 < layerCount)
                {
                    nodeForward = nodeGrid[sliceIndex, layerIndex + 1];
                }
                // Back
                if (layerIndex - 1 >= 0)
                {
                    nodeBack = nodeGrid[sliceIndex, layerIndex - 1];
                }
                // Right
                if (sliceIndex + 1 < sliceCount)
                {
                    nodeRight = nodeGrid[sliceIndex + 1, layerIndex];
                }
                else
                {
                    nodeRight = nodeGrid[0, layerIndex];
                }
                // Left
                if (sliceIndex - 1 >= 0)
                {
                    nodeLeft = nodeGrid[sliceIndex - 1, layerIndex];
                }
                else
                {
                    nodeLeft = nodeGrid[sliceCount - 1, layerIndex];
                }

                // Add connections
                if (nodeForward != null)
                {
                    nodeGrid[sliceIndex, layerIndex].GetComponent<NodeScript>().connectedNodes.Add(nodeForward);
                }
                if (nodeRight != null)
                {
                    nodeGrid[sliceIndex, layerIndex].GetComponent<NodeScript>().connectedNodes.Add(nodeRight);
                }
                if (nodeBack != null)
                {
                    nodeGrid[sliceIndex, layerIndex].GetComponent<NodeScript>().connectedNodes.Add(nodeBack);
                }
                if (nodeLeft != null)
                {
                    nodeGrid[sliceIndex, layerIndex].GetComponent<NodeScript>().connectedNodes.Add(nodeLeft);
                }
            }
        }
    }

    public Vector3 centrePoint;
    public float centreRadius;
    public int layerCount;
    public int sliceCount;
    public float layerWidth;
    public float sliceWidth;
    public GameObject[,] nodeGrid;
    public GameObject[,] cellGrid;
    public GameObject nodePrefab;
    public GameObject cellPrefab;
    public GameObject edgePrefab;
    public GameObject carPrefab;
}
