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
        Lightning,
        Drone,
        Scout,
        Mothership
    }

    // Ship Variables
    [SerializeField] private ShipTypes shipType;
    [SerializeField] private float shipCost;
    [SerializeField] protected float maxShipHP;
    protected NetworkVariable<float> currentShipHP = new NetworkVariable<float>();

    // Ship Components
    private Transform hpBar;
    private SpriteRenderer outlineSprite;

    private PlayerData playerData;

    public override void OnNetworkSpawn()
    {
        playerData = PlayerDataList.Singleton.players[OwnerClientId];

        // Finding ship components
        hpBar = transform.Find("Health Bar/Health");
        outlineSprite = transform.Find("Outline").GetComponent<SpriteRenderer>();
        SpriteRenderer mapMarkerSprite = transform.Find("Scout Marker").GetComponent<SpriteRenderer>();


        // Setting up healthbar
        if (IsHost) currentShipHP.Value = maxShipHP;

        currentShipHP.OnValueChanged += (float previousValue, float newValue) => {
            hpBar.transform.localScale = new Vector3(currentShipHP.Value / maxShipHP, 1, 1);
            hpBar.transform.localPosition = new Vector3((currentShipHP.Value / maxShipHP * 0.5f) - 0.5f, 0, 0);
        };

        // Changes based on ship owner
        if (!IsOwner) {
            GetComponentInChildren<SpriteMask>().enabled = false;
            outlineSprite.gameObject.SetActive(false);
            mapMarkerSprite.gameObject.SetActive(true);
        }

        // Set the team color
        Color teamColor = playerData.playerColor;
        transform.Find("Ship Accent").GetComponent<SpriteRenderer>().color = teamColor;
        transform.Find("Minimap Marker").GetComponent<SpriteRenderer>().color = teamColor;
        transform.Find("Minimap Scout Marker").GetComponent<SpriteRenderer>().color = teamColor;
        mapMarkerSprite.color = teamColor;
        teamColor.a = 0f;
        outlineSprite.color = teamColor;

        //Ship specific setup
        SetupBasedOnShipType();
    }

    public void FixedUpdate()
    {
        //Ship specific updates
        UpdateBasedOnShipType();
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
                PlayerData[] playerDataList = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
                foreach (PlayerData data in playerDataList)
                    if (data.OwnerClientId == OwnerClientId)
                        data.SetMothership(this);
                break;
        }
    }

    public void UpdateBasedOnShipType()
    {
        switch (shipType)
        {
            case ShipTypes.Goliath:
                if (!IsHost) return;
                
                if (currentShipHP.Value < maxShipHP)
                    currentShipHP.Value += 1 * Time.deltaTime;
                break;
        }
    }

    public void DoDamage(float damage)
    {
        currentShipHP.Value -= damage;
        if (currentShipHP.Value <= 0)
            DestroyShipRPC();
    }

    [Rpc(SendTo.Server)]
    public void DestroyShipRPC()
    {
        if (shipType == ShipTypes.Mothership)
        {
            playerData.KillMothershipRPC();
        }
        this.GetComponent<NetworkObject>().Despawn();
        Destroy(this.gameObject);
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
