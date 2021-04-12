using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Get camera
        Camera camera = gameObject.GetComponent<Camera>();
        float currentSize = camera.orthographicSize;

        // Zoom
        if (Input.mouseScrollDelta.y < 0)
        {
            camera.orthographicSize += currentSize * 10.0f * Time.deltaTime;
        }
        if (Input.mouseScrollDelta.y > 0)
        {
            camera.orthographicSize -= currentSize * 10.0f * Time.deltaTime;
        }

        // Move
        if (Input.GetKey(KeyCode.D))
        {
            gameObject.transform.position += Vector3.right * currentSize * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            gameObject.transform.position += Vector3.left * currentSize * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            gameObject.transform.position += Vector3.back * currentSize * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.W))
        {
            gameObject.transform.position += Vector3.forward * currentSize * Time.deltaTime;
        }
    }
}
