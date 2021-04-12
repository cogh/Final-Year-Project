using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timeScalar += Time.deltaTime / 10.0f;
        if (timeScalar > 1.0f) { timeScalar -= 1.0f; }
        transform.rotation = Quaternion.Euler(timeScalar * 360.0f, 45.0f, 0.0f);
        GetComponent<Light>().color = Color.Lerp(upColor, downColor, timeScalar);
        if (timeScalar > 0.5f)
        {
            GetComponent<Light>().intensity = 0.0f;
        }
        else
        {
            GetComponent<Light>().intensity = 1.0f;
        }
    }

    public float timeScalar;
    public Color upColor;
    public Color downColor;
}
