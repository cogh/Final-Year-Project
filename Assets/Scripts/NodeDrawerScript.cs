using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
                    }
                    selection = null;
                    connectState = "idle";
                }
                break;
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
