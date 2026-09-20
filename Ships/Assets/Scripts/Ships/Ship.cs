using Unity.Netcode;
using UnityEngine;

public class Ship : NetworkBehaviour
{
    public enum ShipTypes
    {
        Destroyer,
        Hawk,
        Challenger,
        Goliath,
        GoliathFighter,
        Lightning,
        Drone,
        Scout,
        Mothership

        //Marauder,
    }

    // Ship Variables
    public ShipTypes shipType;
    [SerializeField] private float shipCost;
    public float maxShipHP;
    public readonly NetworkVariable<float> currentShipHP = new NetworkVariable<float>();
    public int correctionFactor; // Opponent range adjustments
    public int visionRange;

    // Ship Components
    private Transform hpBar;
    [SerializeField] GameObject popupTextPrefab;
    private SpriteRenderer outlineSprite;

    private PlayerData playerData;

    private GameObject scoutMarker; // Marker in fog of war (Enemy)
    private GameObject scoutMarkerHider;
    private GameObject minimapMarker; // Market on minimap (Both)
    private GameObject minimapScoutMarker; // Marker in minimap fog of war (Enemy)

    public override void OnNetworkSpawn()
    {
        playerData = PlayerDataList.Singleton.players[OwnerClientId];

        // Finding ship components
        hpBar = transform.Find("Health Bar/Health");
        outlineSprite = transform.Find("Outline").GetComponent<SpriteRenderer>();

        scoutMarker = transform.Find("Scout Marker").gameObject;
        scoutMarkerHider = transform.Find("Scout Marker Hider").gameObject;
        minimapMarker = transform.Find("Minimap Marker").gameObject;
        minimapScoutMarker = transform.Find("Minimap Scout Marker").gameObject;


        // Setting up healthbar
        if (IsHost) currentShipHP.Value = maxShipHP;

        currentShipHP.OnValueChanged += (float previousValue, float newValue) => {
            hpBar.transform.localScale = new Vector3(currentShipHP.Value / maxShipHP, 1, 1);
            hpBar.transform.localPosition = new Vector3((currentShipHP.Value / maxShipHP * 0.5f) - 0.5f, 0, 0);

            if (IsOwner && newValue > previousValue)
            {
                PopupText popupText = Instantiate(popupTextPrefab, transform.position + new Vector3(-1, 0) * correctionFactor * 0.5f, Quaternion.identity).GetComponent<PopupText>();
                popupText.SetupText("+", Color.greenYellow, 1f);
                popupText = Instantiate(popupTextPrefab, transform.position + new Vector3(1, -1f) * correctionFactor * 0.5f, Quaternion.identity).GetComponent<PopupText>();
                popupText.SetupText("+", Color.greenYellow, 1f);
            }
        };

        // Set the team color
        Color teamColor = playerData.playerColor;
        transform.Find("Ship Accent").GetComponent<SpriteRenderer>().color = teamColor;
        scoutMarker.GetComponent<SpriteRenderer>().color = teamColor;
        minimapMarker.GetComponent<SpriteRenderer>().color = teamColor;
        minimapScoutMarker.GetComponent<SpriteRenderer>().color = teamColor;
        teamColor.a = 0f;
        outlineSprite.color = teamColor;

        //Ship specific setup
        SetupBasedOnShipType();

        Transform fogRemover = transform.Find("Fog Remover");
        // Changes based on ship owner
        if (!IsOwner)
        {
            fogRemover.gameObject.SetActive(false);
            outlineSprite.gameObject.SetActive(false);
            scoutMarker.gameObject.SetActive(true);
        }
        else
        {
            fogRemover.localScale = new Vector3(visionRange / 6f, visionRange / 6f);
            if (shipType != ShipTypes.GoliathFighter)
                GameplayInputManager.Singleton.AddNewSelectedShip(this);
        }
    }

    public void FixedUpdate()
    {
        //Ship specific updates
        UpdateBasedOnShipType();

        //Don't rotate minimap icons
        scoutMarker.transform.rotation = Quaternion.Euler(0, 0, -transform.rotation.z);
        scoutMarkerHider.transform.rotation = Quaternion.Euler(0, 0, -transform.rotation.z);
        minimapMarker.transform.rotation = Quaternion.Euler(0, 0, -transform.rotation.z);
        minimapScoutMarker.transform.rotation = Quaternion.Euler(0, 0, -transform.rotation.z);
    }

    public void SetupBasedOnShipType()
    {
        switch (shipType)
        {
            case ShipTypes.Scout:
                if (IsOwner)
                    transform.Find("Scout Radar").gameObject.SetActive(true);
                break;
            case ShipTypes.Mothership:
                foreach (var (id, player) in PlayerDataList.Singleton.players)
                    if (player.OwnerClientId == OwnerClientId)
                        player.SetMothership(this);
                break;
        }
    }

    public void UpdateBasedOnShipType()
    {
        /*
        switch (shipType)
        {
            case ShipTypes.Mothership:

            case ShipTypes.Goliath:
                if (!IsHost) return;

                if (currentShipHP.Value < maxShipHP)
                    currentShipHP.Value += 1 * Time.deltaTime;
                break;
        
        }
        */
    }

    public void DoDamage(float damage, GameObject shipDamageCameFrom)
    {
        currentShipHP.Value -= damage;
        if (currentShipHP.Value <= 0)
            DestroyShip(shipDamageCameFrom);
    }

    [Rpc(SendTo.Server)]
    public void SelfDestroyShipRPC()
    {
        //This method is used by the mothership when it dies to remove all of your own ships from the scene
        GameSceneManager.Singleton.shipsInScene.Remove(gameObject);

        GetComponent<NetworkObject>().Despawn();
        Destroy(this.gameObject);
    }

    public void DestroyShip(GameObject shipDamageCameFrom)
    {
        if (shipType == ShipTypes.Mothership)
        {
            playerData.KillMothershipRPC();
        }

        GameSceneManager.Singleton.shipsInScene.Remove(gameObject);
        if (shipDamageCameFrom != null)
            InformShipWhoKilled(shipDamageCameFrom);

        GetComponent<NetworkObject>().Despawn();
        Destroy(this.gameObject);
    }

    public void InformShipWhoKilled(GameObject shipDamageCameFrom)
    {
        Ship shipScript = shipDamageCameFrom.GetComponent<Ship>();
        if (shipScript != null)
        {
            switch (shipScript.shipType)
            {
                case ShipTypes.Lightning:
                    shipDamageCameFrom.GetComponent<Movement>().ChangeSpeed(1.5f, 7f);
                    break;
                default:
                    break;
            }
        }
    }

    public void SelectShip()
    {
        Color newColor = outlineSprite.color;
        newColor.a = 1f;
        outlineSprite.color = newColor;
    }

    public void UnselectShip()
    {
        Color newColor = outlineSprite.color;
        newColor.a = 0f;
        outlineSprite.color = newColor;
    }

    public ShipTypes GetShipType()
    {
        return shipType;
    }

    public float GetShipCost()
    {
        return shipCost;
    }
}
