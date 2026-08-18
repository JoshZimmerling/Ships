using Newtonsoft.Json.Bson;
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

    [SerializeField] RectTransform minimapViewportRectangle;

    // Start is called before the first frame update
    void Start()
    {
        cam = this.gameObject.GetComponent<Camera>();
        currentZoomLevel = cam.orthographicSize;
        camLocked = true;

        //minimapViewportRectangle.gameObject.SetActive(false);
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
        if (((Input.mousePosition.y >= (Screen.height * (1 - moveCamBorderSize)) && !camLocked) || Input.GetKey(KeyCode.W)) && transform.position.y <= 125)
        {
            //Move cam up
            transform.Translate(Vector3.up * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }
        if (((Input.mousePosition.y <= (Screen.height * moveCamBorderSize) && !camLocked) || Input.GetKey(KeyCode.S)) && transform.position.y >= -125)
        {
            //Move cam down
            transform.Translate(Vector3.down * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }
        if (((Input.mousePosition.x >= (Screen.width * (1 - moveCamBorderSize)) && !camLocked) || Input.GetKey(KeyCode.D)) && transform.position.x <= 125)
        {
            //Move cam right
            transform.Translate(Vector3.right * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }
        if (((Input.mousePosition.x <= (Screen.width * moveCamBorderSize) && !camLocked) || Input.GetKey(KeyCode.A)) && transform.position.x >= -125)
        {
            //Move cam left
            transform.Translate(Vector3.left * camMoveSpeed * currentZoomLevel * Time.deltaTime);
        }

        MoveViewport();
    }

    public void ToggleLockState()
    {
        if (camLocked)
            camLocked = false;
        else
            camLocked = true;
    }

    private void MoveViewport()
    {
        minimapViewportRectangle.anchoredPosition = new Vector2(transform.position.x, transform.position.y) * 1f;

        minimapViewportRectangle.transform.localScale = new Vector3(0.5f + ((currentZoomLevel-maxZoomIn) / (maxZoomOut - maxZoomIn) * 1.9f), 0.5f + ((currentZoomLevel - maxZoomIn) / (maxZoomOut - maxZoomIn) * 1.9f), 1f);
    }
}
