using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Control : MonoBehaviour
{
    [SerializeField] float maxZoomOut = 50f;
    [SerializeField] float maxZoomIn = 10f;
    [SerializeField] float zoomSpeed = 1f;
    [SerializeField] float moveCamBorderSize = 0.05f;
    [SerializeField] float camMoveSpeed = 2.5f;
    float currentZoomLevel;
    bool camLocked;

    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = this.gameObject.GetComponent<Camera>();
        currentZoomLevel = cam.orthographicSize;
        camLocked = true;
    }

    void Update()
    {
        //Zooming in and out with scroll wheel
        if(Input.mouseScrollDelta.y != 0)
        {
            currentZoomLevel -= (Input.mouseScrollDelta.y * zoomSpeed);

            if (currentZoomLevel > maxZoomOut)
                currentZoomLevel = maxZoomOut;
            else if (currentZoomLevel < maxZoomIn)
                currentZoomLevel = maxZoomIn;

            cam.orthographicSize = currentZoomLevel;
        }

        //Moving camera around
        if ((Input.mousePosition.y >= (Screen.height * (1 - moveCamBorderSize)) || Input.GetKey(KeyCode.W)) && !camLocked && transform.position.y <= 125)
        {
            //Move cam up
            transform.Translate(Vector3.up * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }
        if ((Input.mousePosition.y <= (Screen.height * moveCamBorderSize) || Input.GetKey(KeyCode.S)) && !camLocked && transform.position.y >= -125)
        {
            //Move cam down
            transform.Translate(Vector3.down * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }
        if ((Input.mousePosition.x >= (Screen.width * (1 - moveCamBorderSize)) || Input.GetKey(KeyCode.D)) && !camLocked && transform.position.x <= 125)
        {
            //Move cam right
            transform.Translate(Vector3.right * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }
        if ((Input.mousePosition.x <= (Screen.width * moveCamBorderSize) || Input.GetKey(KeyCode.A)) && !camLocked && transform.position.x >= -125)
        {
            //Move cam left
            transform.Translate(Vector3.left * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }
    }

    public void ToggleLockState()
    {
        if (camLocked)
            camLocked = false;
        else
            camLocked = true;
    }
}
