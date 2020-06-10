using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandPlotScript : MonoBehaviour
{
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Nodes
    public GameObject forwardConnection;
    public GameObject rightConnection;
    public GameObject backConnection;
    public GameObject leftConnection;

    // Plots
    public GameObject forwardPlot;
    public GameObject rightPlot;
    public GameObject backPlot;
    public GameObject leftPlot;

    // Plot details
    public bool accessible;
}
