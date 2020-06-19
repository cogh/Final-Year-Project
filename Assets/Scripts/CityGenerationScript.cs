using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

class LandPlot
{
    public GameObject plotPrefab;
    public GameObject plotObject;
    public string type;
    public string subType;
    public bool connectForward;
    public bool connectRight;
    public bool connectBack;
    public bool connectLeft;
    public int connectCount;
}

public class CityGenerationScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        cityGrid = new LandPlot[gridSize, gridSize];
        SetPlotTypes();
        SetPlotConnections();
        SetPlotPrefabs();
        InstantiatePlotObjects();
        ConnectPlots();
        ConnectNodes();
        camera.transform.position = new Vector3(gridSize * plotSize / 2, 10, gridSize * plotSize / 2);

        // Create some cars
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(0, 0);
        SpawnCar(2, 0);
        SpawnCar(4, 0);
    }

    void SetPlotTypes()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                cityGrid[x, z] = new LandPlot();
                if ((x + 1) % 2 == 0 &&
                    (z + 1) % 2 == 0)
                {
                    cityGrid[x, z].type = "building";
                }
                else
                {
                    cityGrid[x, z].type = "road";
                }
            }
        }
    }

    void SetPlotPrefabs()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (cityGrid[x, z].type == "building")
                {
                    cityGrid[x, z].plotPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
                }
                else if (cityGrid[x, z].type == "road")
                {
                    // Directional connection checks
                    bool f = cityGrid[x, z].connectForward;
                    bool r = cityGrid[x, z].connectRight;
                    bool b = cityGrid[x, z].connectBack;
                    bool l = cityGrid[x, z].connectLeft;

                    // Count
                    int count = 0;
                    if (f) { count++; }
                    if (r) { count++; }
                    if (b) { count++; }
                    if (l) { count++; }

                    // Set road prefab
                    switch (count)
                    {
                        case 1:
                            // End
                            cityGrid[x, z].plotPrefab = deadEndRoadPrefab;
                            break;
                        case 2:
                            // Straights
                            if (r && l)
                            {
                                cityGrid[x, z].plotPrefab = horizontalRoadPrefab;
                            }
                            else if (f && b)
                            {
                                cityGrid[x, z].plotPrefab = verticalRoadPrefab;
                            }
                            // Corners
                            else if (f && r)
                            {
                                cityGrid[x, z].plotPrefab = cornerRoadFRPrefab;
                            }
                            else if (r && b)
                            {
                                cityGrid[x, z].plotPrefab = cornerRoadRBPrefab;
                            }
                            else if (b && l)
                            {
                                cityGrid[x, z].plotPrefab = cornerRoadBLPrefab;
                            }
                            else if (l && f)
                            {
                                cityGrid[x, z].plotPrefab = cornerRoadLFPrefab;
                            }
                            break;
                        case 3:
                            // Ts
                            if (f && r && b)
                            {
                                cityGrid[x, z].plotPrefab = tRoadFRBPrefab;
                            }
                            else if (r && b && l)
                            {
                                cityGrid[x, z].plotPrefab = tRoadRBLPrefab;
                            }
                            else if (b && l && f)
                            {
                                cityGrid[x, z].plotPrefab = tRoadBLFPrefab;
                            }
                            else if (l && f && r)
                            {
                                cityGrid[x, z].plotPrefab = tRoadLFRPrefab;
                            }
                            break;
                        case 4:
                            // Cross
                            cityGrid[x, z].plotPrefab = crossRoadPrefab;
                            break;
                    }
                }
            }
        }
    }

    void InstantiatePlotObjects()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                cityGrid[x, z].plotObject = Instantiate(cityGrid[x, z].plotPrefab, new Vector3(x * plotSize, 0, z * plotSize), cityGrid[x, z].plotPrefab.transform.rotation);
            }
        }
    }

    void SetPlotConnections()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // This plot
                GameObject thisPlotObject = cityGrid[x, z].plotObject;

                // Check forward
                if (z + 1 < gridSize && cityGrid[x, z + 1].type != "building")
                {
                    cityGrid[x, z].connectForward = true;
                    //cityGrid[x, z].plotObject.GetComponent<LandPlotScript>().forwardPlot = cityGrid[x, z + 1].plotObject;
                }

                // Check right
                if (x + 1 < gridSize && cityGrid[x + 1, z].type != "building")
                {
                    cityGrid[x, z].connectRight = true;
                }

                // Check back
                if (z - 1 >= 0 && cityGrid[x, z - 1].type != "building")
                {
                    cityGrid[x, z].connectBack = true;
                }

                // Check left
                if (x - 1 >= 0 && cityGrid[x - 1, z].type != "building")
                {
                    cityGrid[x, z].connectLeft = true;
                }
            }
        }
    }

    void ConnectPlots()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Plot object
                GameObject plot = cityGrid[x, z].plotObject;

                // Set adjacent plots
                if (z + 1 < gridSize) { plot.GetComponent<LandPlotScript>().forwardPlot = cityGrid[x, z + 1].plotObject; }
                if (x + 1 < gridSize) { plot.GetComponent<LandPlotScript>().rightPlot = cityGrid[x + 1, z].plotObject; }
                if (z - 1 >= 0) { plot.GetComponent<LandPlotScript>().backPlot = cityGrid[x, z - 1].plotObject; }
                if (x - 1 >= 0) { plot.GetComponent<LandPlotScript>().leftPlot = cityGrid[x - 1, z].plotObject; }
            }
        }
    }

    void ConnectNodes()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (cityGrid[x, z].type == "road")
                {
                    // Plot object
                    GameObject plot = cityGrid[x, z].plotObject;

                    // Adjacent plots
                    GameObject forwardPlot = plot.GetComponent<LandPlotScript>().forwardPlot;
                    GameObject rightPlot = plot.GetComponent<LandPlotScript>().rightPlot;
                    GameObject backPlot = plot.GetComponent<LandPlotScript>().backPlot;
                    GameObject leftPlot = plot.GetComponent<LandPlotScript>().leftPlot;

                    // Connect adjacent plot nodes
                    if (forwardPlot != null && forwardPlot.GetComponent<LandPlotScript>().accessible == true)
                    {
                        GameObject exitNode = plot.GetComponent<LandPlotScript>().forwardConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject entranceNode = plot.GetComponent<LandPlotScript>().forwardConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        GameObject targetExitNode = forwardPlot.GetComponent<LandPlotScript>().backConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject targetEntranceNode = forwardPlot.GetComponent<LandPlotScript>().backConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        exitNode.GetComponent<NodeScript>().connectedNodes.Add(targetEntranceNode);
                    }
                    if (rightPlot != null && rightPlot.GetComponent<LandPlotScript>().accessible == true)
                    {
                        GameObject exitNode = plot.GetComponent<LandPlotScript>().rightConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject entranceNode = plot.GetComponent<LandPlotScript>().rightConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        GameObject targetExitNode = rightPlot.GetComponent<LandPlotScript>().leftConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject targetEntranceNode = rightPlot.GetComponent<LandPlotScript>().leftConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        exitNode.GetComponent<NodeScript>().connectedNodes.Add(targetEntranceNode);
                    }
                    if (backPlot != null && backPlot.GetComponent<LandPlotScript>().accessible == true)
                    {
                        GameObject exitNode = plot.GetComponent<LandPlotScript>().backConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject entranceNode = plot.GetComponent<LandPlotScript>().backConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        GameObject targetExitNode = backPlot.GetComponent<LandPlotScript>().forwardConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject targetEntranceNode = backPlot.GetComponent<LandPlotScript>().forwardConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        exitNode.GetComponent<NodeScript>().connectedNodes.Add(targetEntranceNode);
                    }
                    if (leftPlot != null && leftPlot.GetComponent<LandPlotScript>().accessible == true)
                    {
                        GameObject exitNode = plot.GetComponent<LandPlotScript>().leftConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject entranceNode = plot.GetComponent<LandPlotScript>().leftConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        GameObject targetExitNode = leftPlot.GetComponent<LandPlotScript>().rightConnection.GetComponent<PlotConnectionScript>().exitNode;
                        GameObject targetEntranceNode = leftPlot.GetComponent<LandPlotScript>().rightConnection.GetComponent<PlotConnectionScript>().entranceNode;
                        exitNode.GetComponent<NodeScript>().connectedNodes.Add(targetEntranceNode);
                    }
                }
            }
        }
    }

    void SpawnCar(int x, int y)
    {
        GameObject car = Instantiate(carPrefab);
        GameObject startingPlot = cityGrid[x, y].plotObject;
        GameObject startingConnection = startingPlot.GetComponent<LandPlotScript>().forwardConnection;
        GameObject startingNode = startingConnection.GetComponent<PlotConnectionScript>().exitNode;
        car.GetComponent<CarScript>().targetNode = startingNode;
    }

    public List<GameObject> buildingPrefabs;
    public GameObject tRoadPrefab;
    public GameObject horizontalRoadPrefab;
    public GameObject verticalRoadPrefab;
    public GameObject cornerRoadFRPrefab;
    public GameObject cornerRoadRBPrefab;
    public GameObject cornerRoadBLPrefab;
    public GameObject cornerRoadLFPrefab;
    public GameObject tRoadFRBPrefab;
    public GameObject tRoadRBLPrefab;
    public GameObject tRoadBLFPrefab;
    public GameObject tRoadLFRPrefab;
    public GameObject deadEndRoadPrefab;
    public GameObject crossRoadPrefab;
    public int gridSize;
    LandPlot[,] cityGrid;
    public int plotSize;
    public GameObject camera;
    public GameObject carPrefab;
}