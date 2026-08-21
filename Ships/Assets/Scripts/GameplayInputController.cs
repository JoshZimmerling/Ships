using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameplayInputManager : Singleton<GameplayInputManager>
{
    private readonly List<Ship> selectedShips = new();
    private Camera_Control cameraScript; // TODO: move to using singleton

    // Ship movement / selection
    Transform selectionBox;

    private Vector2 startPos;
    private Vector2 curPos;
    private bool mouseDownInGame = false;
    private bool mouseDownInMinmap = false;

    private float curWidth;
    private float curHeight;

    public List<Collider2D> hitColliders = new List<Collider2D>();
    public List<Ship> shipsFromHit = new List<Ship>();

    float xMax;
    float yMax;
    float xMin;
    float yMin;
    float xDiff;
    float yDiff;
    Vector2 shipCenter;

    private RectTransform minimapTransform;
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    protected override void Awake()
    {
        base.Awake();

        cameraScript = Camera.main.GetComponent<Camera_Control>();
        selectionBox = transform.Find("Selection Box");

        minimapTransform = GameObject.Find("Minimap Image").GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        // Setting the target destination for the ships
        if (Input.GetMouseButtonDown(1))
        {
            DirectShips(false);
        }

        // Rotate only ships
        if (Input.GetMouseButtonDown(2))
        {
            DirectShips(true);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            foreach (Ship ship in selectedShips)
            {
                ship.GetComponent<Movement>().StopShipServerRPC(); 
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            foreach (Ship ship in selectedShips)
            {
                ship.GetComponent<Movement>().BackupServerRPC();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Shop.Singleton.ToggleShop();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            cameraScript.ToggleLockState();
        }

        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = Input.mousePosition;
            List<RaycastResult> clickedUIElements = new List<RaycastResult>();
            raycaster.Raycast(pointerData, clickedUIElements);

            foreach (RaycastResult UI_Element in clickedUIElements)
            {
                if (UI_Element.gameObject.name == "Minimap Image")
                {
                    mouseDownInMinmap = true;
                }
            }

            //If we did not click on a UI element, start drawing our ship selection box
            if (clickedUIElements.Count <= 0)
            {
                startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseDownInGame = true;
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (mouseDownInGame)
            {
                UpdateBox(Input.mousePosition);
            }
            else if (mouseDownInMinmap)
            {
                Vector2 localClickPos;

                // Directly translate the shifting mouse position into the target's coordinates
                RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapTransform, Input.mousePosition, null, out localClickPos);
                Vector2 normalizedClick = new Vector2(((minimapTransform.rect.width / 2) + localClickPos.x) / minimapTransform.rect.width, ((minimapTransform.rect.height / 2) + localClickPos.y) / minimapTransform.rect.height);

                if (normalizedClick.x <= 0)
                {
                    normalizedClick.x = 0;
                }
                if (normalizedClick.x >= 1)
                {
                    normalizedClick.x = 1;
                }
                if (normalizedClick.y <= 0)
                {
                    normalizedClick.y = 0;
                }
                if (normalizedClick.y >= 1)
                {
                    normalizedClick.y = 1;
                }

                cameraScript.MoveCameraToNormalizedPosition(normalizedClick);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (mouseDownInGame)
            {
                ReleaseBox();
            }

            mouseDownInGame = false;
            mouseDownInMinmap = false;
        }
    }

    private void VerifySelection()
    {
        for (int i = 0; i < selectedShips.Count; i++) 
        {
            if (selectedShips[i] == null) 
            {
                selectedShips.RemoveAt(i);
            }
        }
    }

    private void DirectShips(bool rotateOnly)
    {
        VerifySelection();

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);

        if (selectedShips.Count == 1)
        {
            selectedShips[0].GetComponent<Movement>().SetTargetDestinationServerRPC(worldPosition, rotateOnly);
        }
        else
        {
            SetDestinationInFormation(rotateOnly);
        }
    }

    void SetDestinationInFormation(bool rotateOnly)
    {
        if (selectedShips.Count == 0) 
        {
            return;
        }
        else 
        {
            xMax = selectedShips[0].transform.position.x;
            yMax = selectedShips[0].transform.position.y;
            xMin = selectedShips[0].transform.position.x;
            yMin = selectedShips[0].transform.position.y;
        }

        foreach (Ship ship in selectedShips)
        {
            if (ship.transform.position.x > xMax) { xMax = ship.transform.position.x; }
            if (ship.transform.position.x < xMin) { xMin = ship.transform.position.x; }
            if (ship.transform.position.y > yMax) { yMax = ship.transform.position.y; }
            if (ship.transform.position.y < yMin) { yMin = ship.transform.position.y; }
        }

        xDiff = xMax - xMin;
        yDiff = yMax - yMin;

        shipCenter = new Vector2(xMin + (xDiff / 2), yMin + (yDiff / 2)); 

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);

        foreach (Ship ship in selectedShips)
        {
            ship.GetComponent<Movement>().SetTargetDestinationServerRPC((Vector2) worldPosition + ((Vector2) ship.transform.position - shipCenter), rotateOnly);
        }

    }

    void SetShips(List<Ship> ships)
    {
        VerifySelection();

        foreach (Ship ship in this.selectedShips)
        {
            ship.UnselectShip();
        }

        foreach (Ship ship in ships)
        {
            ship.SelectShip(); 
        }

        this.selectedShips.Clear(); 
        foreach (Ship newShip in ships)
        {
            this.selectedShips.Add(newShip);
        }
    }

    void UpdateBox(Vector2 mousePos)
    {
        selectionBox.gameObject.SetActive(true);

        curPos = Camera.main.ScreenToWorldPoint(mousePos);

        curWidth = startPos.x - curPos.x;
        curHeight = startPos.y - curPos.y;

        selectionBox.localScale = new Vector2(curWidth, curHeight);

        selectionBox.transform.position = new Vector3(startPos.x - (curWidth / 2), startPos.y - (curHeight / 2), -1);
    }

    void ReleaseBox()
    {
        Transform box = selectionBox.transform;
        ContactFilter2D contactFilter = new ContactFilter2D();

        Physics2D.OverlapBox(box.position, new Vector2(Mathf.Abs(box.localScale.x), Mathf.Abs(box.localScale.y)), 0, contactFilter, hitColliders);

        shipsFromHit.Clear();
        foreach (Collider2D col in hitColliders)
        {
            Ship ship = col.GetComponent<Ship>();
            if (ship != null)
                if (NetworkManager.Singleton.LocalClientId == ship.OwnerClientId)
                    shipsFromHit.Add(ship);
        }

        SetShips(shipsFromHit);

        selectionBox.gameObject.SetActive(false);
    }

    public bool MouseScreenCheck()
    {
        #if UNITY_EDITOR
        if (Input.mousePosition.x == 0 || Input.mousePosition.y == 0 || Input.mousePosition.x >= Handles.GetMainGameViewSize().x - 1 || Input.mousePosition.y >= Handles.GetMainGameViewSize().y - 1)
        {
            return false;
        }
        #else
        if (Input.mousePosition.x == 0 || Input.mousePosition.y == 0 || Input.mousePosition.x >= Screen.width - 1 || Input.mousePosition.y >= Screen.height - 1) {
            return false;
        }
        #endif
        else
        {
            return true;
        }
    }
}
