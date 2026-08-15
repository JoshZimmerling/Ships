using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameplayInputManager : Singleton<GameplayInputManager>
{
    private readonly List<Ship> selectedShips = new();
    private Camera_Control cameraScript; // TODO: move to using singleton

    protected override void Awake()
    {
        base.Awake();

        cameraScript = Camera.main.GetComponent<Camera_Control>();
        selectionBox = transform.Find("Selection Box");
    }

    // Update is called once per frame
    void Update()
    {
        // Setting the target destination for the ships
        if (Input.GetButtonDown("Fire2"))
        {

            VerifySelection();

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 0;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);

            if (selectedShips.Count == 1)
            {
                selectedShips[0].GetComponent<Movement>().SetTargetDestinationServerRPC(worldPosition);
            }
            else
            {
                SetDestinationInFormation(); 
            }
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

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            UpdateBox(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseBox();
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

    // Ship movement / selection
    Transform selectionBox;

    private Vector2 startPos;
    private Vector2 curPos;

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
    public void SetDestinationInFormation()
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
            ship.GetComponent<Movement>().SetTargetDestinationServerRPC((Vector2) worldPosition + ((Vector2) ship.transform.position - shipCenter));
        }

    }

    public void SetShips(List<Ship> ships)
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
