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

        InstantiateNodes();

        ConnectNodes();

        CreateCells();
    }

    void CreateCells()
    {

    }

    void InstantiateNodes()
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

    void ConnectNodes()
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
    public GameObject nodePrefab;
    public GameObject cellPrefab;
}
