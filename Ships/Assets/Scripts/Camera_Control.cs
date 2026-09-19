using UnityEngine;

public class Camera_Control : Singleton<Camera_Control>
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
    private float minimapWidth;
    private float mapWidth;

    // Start is called before the first frame update
    void Start()
    {
        cam = this.gameObject.GetComponent<Camera>();
        currentZoomLevel = cam.orthographicSize;
        camLocked = true;

        minimapWidth = GameObject.Find("Minimap").GetComponent<RectTransform>().rect.width;
        mapWidth = GameSceneManager.Singleton.map.GetComponent<RectTransform>().rect.width;
    }

    void Update()
    {
        //Zooming in and out with scroll wheel
        if(Input.mouseScrollDelta.y != 0 && GameplayInputManager.Singleton.IsMouseOverUI() != GameplayInputManager.UIHoverState.OTHER_NON_MINIMAP_UI)
        {
            currentZoomLevel -= (Input.mouseScrollDelta.y * zoomSpeed);

            if (currentZoomLevel > maxZoomOut)
                currentZoomLevel = maxZoomOut;
            else if (currentZoomLevel < maxZoomIn)
                currentZoomLevel = maxZoomIn;

            cam.orthographicSize = currentZoomLevel;

            MoveViewport();
        }

        //Moving camera around
        Vector3 camMoveDirection = Vector3.zero;
        if (((Input.mousePosition.y >= (Screen.height * (1 - moveCamBorderSize)) && !camLocked) || Input.GetKey(KeyCode.W)) && transform.position.y <= (mapWidth/2))
        {
            //Move cam up
            camMoveDirection += Vector3.up;
        }
        if (((Input.mousePosition.y <= (Screen.height * moveCamBorderSize) && !camLocked) || Input.GetKey(KeyCode.S)) && transform.position.y >= (-mapWidth/2))
        {
            //Move cam down
            camMoveDirection += Vector3.down;
        }
        if (((Input.mousePosition.x >= (Screen.width * (1 - moveCamBorderSize)) && !camLocked) || Input.GetKey(KeyCode.D)) && transform.position.x <= (mapWidth / 2))
        {
            //Move cam right
            camMoveDirection += Vector3.right;
        }
        if (((Input.mousePosition.x <= (Screen.width * moveCamBorderSize) && !camLocked) || Input.GetKey(KeyCode.A)) && transform.position.x >= (-mapWidth / 2))
        {
            //Move cam left
            camMoveDirection += Vector3.left;
        }

        if (camMoveDirection != Vector3.zero)
        {
            transform.Translate(camMoveDirection.normalized * camMoveSpeed * currentZoomLevel * Time.deltaTime);
            MoveViewport();
        }

        if (Input.GetKey(KeyCode.Space))
        {
            //Center camera on mothership
            MoveCameraToWorldSpace(PlayerDataList.Singleton.GetLocalPlayer().GetMothership().gameObject.transform.position);
            MoveViewport();
        }
    }

    public void ToggleLockState()
    {
        if (camLocked)
            camLocked = false;
        else
            camLocked = true;
    }

    public void MoveCameraToWorldSpace(Vector2 position)
    {
        gameObject.transform.position = new Vector3(position.x, position.y, gameObject.transform.position.z);
        MoveViewport();
    }

    public void MoveCameraToNormalizedPosition(Vector2 normalizedPositionVector)
    {
        gameObject.transform.position = new Vector3((-mapWidth / 2) + (mapWidth * normalizedPositionVector.x), (-mapWidth / 2) + (mapWidth * normalizedPositionVector.y), gameObject.transform.position.z);
        MoveViewport();
    }

    private void MoveViewport()
    {
        minimapViewportRectangle.anchoredPosition = new Vector2(transform.position.x, transform.position.y) * (minimapWidth/mapWidth);

        minimapViewportRectangle.transform.localScale = new Vector3((125f/mapWidth) + ((currentZoomLevel-maxZoomIn) / (maxZoomOut - maxZoomIn) * (475f/mapWidth)), (125f / mapWidth) + ((currentZoomLevel - maxZoomIn) / (maxZoomOut - maxZoomIn) * (475f / mapWidth)), 1f);
    }

    public bool IsOnScreen(Transform obj)
    {
        Vector3 screenPoint = cam.WorldToScreenPoint(obj.position);

        return screenPoint.y > 0 &&
               screenPoint.y < Screen.height &&
               screenPoint.x > 0 &&
               screenPoint.x < Screen.width;
    }

    public bool IsSeenByMyShips(Transform obj)
    {
        foreach (Transform ship in PlayerDataList.Singleton.GetLocalPlayer().transform)
        {
            Ship myShip = ship.GetComponent<Ship>();
            if ((myShip.transform.position - obj.position).magnitude < myShip.visionRange)
            {
                return true;
            }
        }
        return false;
    }
}
