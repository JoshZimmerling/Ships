using Unity.Collections.LowLevel.Unsafe;
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
    [SerializeField] public GameObject popupTextPrefab;
    [SerializeField] AudioClip deathSound;
    private SpriteRenderer outlineSprite;

    private PlayerData playerData;

    private GameObject scoutMarker; // Marker in fog of war (Enemy)
    private GameObject minimapMarker; // Market on minimap (Both)
    private GameObject minimapScoutMarker; // Marker in minimap fog of war (Enemy)

    private float hpPercentOnFire = 0.5f;
    private ParticleSystemRenderer fireEmitter;

    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject visionPrefab;

    public override void OnNetworkSpawn()
    {
        // Finding ship components
        hpBar = transform.Find("Health Bar/Health");
        outlineSprite = transform.Find("Outline").GetComponent<SpriteRenderer>();

        scoutMarker = transform.Find("Scout Marker").gameObject;
        minimapMarker = transform.Find("Minimap Marker").gameObject;
        minimapScoutMarker = transform.Find("Minimap Scout Marker").gameObject;

        fireEmitter = transform.GetComponent<ParticleSystemRenderer>();
        fireEmitter.enabled = false;

        // Setting up healthbar
        if (IsHost) currentShipHP.Value = maxShipHP;

        currentShipHP.OnValueChanged += (float previousValue, float newValue) => {
            hpBar.transform.localScale = new Vector3(currentShipHP.Value / maxShipHP, 1, 1);
            hpBar.transform.localPosition = new Vector3((currentShipHP.Value / maxShipHP * 0.5f) - 0.5f, 0, 0);

            fireEmitter.enabled = newValue / maxShipHP < hpPercentOnFire;

            if (IsOwner && newValue > previousValue)
            {
                PopupText popupText = Instantiate(popupTextPrefab, transform.position + new Vector3(-1, 0) * correctionFactor * 0.5f, Quaternion.identity).GetComponent<PopupText>();
                popupText.SetupText("+", Color.greenYellow, 1f);
                popupText = Instantiate(popupTextPrefab, transform.position + new Vector3(1, -1f) * correctionFactor * 0.5f, Quaternion.identity).GetComponent<PopupText>();
                popupText.SetupText("+", Color.greenYellow, 1f);
            }


        };

        // If player owned ship
        if (this.GetType() != typeof(NeutralShip))
        {
            playerData = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>();

            // Set the team color
            Color teamColor = playerData.playerColor;
            transform.Find("Ship Accent").GetComponent<SpriteRenderer>().color = teamColor;
            scoutMarker.GetComponent<SpriteRenderer>().color = teamColor;
            minimapMarker.GetComponent<SpriteRenderer>().color = teamColor;
            minimapScoutMarker.GetComponent<SpriteRenderer>().color = teamColor;
            teamColor.a = 0f;
            outlineSprite.color = teamColor;

            // Select the ship
            if (IsOwner && shipType != ShipTypes.GoliathFighter)
                GameplayInputManager.Singleton.AddNewSelectedShip(this);
        }

        // Changes based on ship owner
        if (!IsOwner || this.GetType() == typeof(NeutralShip))
        {
            outlineSprite.gameObject.SetActive(false);
            scoutMarker.SetActive(true);
        }

        //Ship specific setup
        SetupBasedOnShipType();
    }

    public void FixedUpdate()
    {
        //Ship specific updates
        UpdateBasedOnShipType();

        //Don't rotate minimap icons
        scoutMarker.transform.rotation = Quaternion.identity;
        minimapMarker.transform.rotation = Quaternion.identity;
        minimapScoutMarker.transform.rotation = Quaternion.identity;
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
                playerData.SetMothership(this);
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
            if (gameObject.GetComponent<NeutralShip>() != null)
                gameObject.GetComponent<NeutralShip>().DestroyShip(shipDamageCameFrom);
            else
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
        else
        {
            //Check if this was last ship and no money left
            CheckIfLastShipAndNoMoneyRPC(OwnerClientId);
        }

        //Cleaning up old references
        GameSceneManager.Singleton.shipsInScene.Remove(gameObject);
        UnselectShipRPC();

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
                    if (shipType == ShipTypes.GoliathFighter)
                        shipDamageCameFrom.GetComponent<Movement>().ChangeSpeed(1.25f, 7f);
                    else
                        shipDamageCameFrom.GetComponent<Movement>().ChangeSpeed(1.5f, 7f);
                    break;
                default:
                    break;
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void CheckIfLastShipAndNoMoneyRPC(ulong shipsClientID)
    {
        //If my client ID is the ship who just died, check if that is my last ship and if I have no money left, kill my mothership
        if (shipsClientID == NetworkManager.LocalClientId)
        {
            if (Shop.Singleton.GetGold() <= 0)
            { // If I have no money, loop through my remaining alive ships and if they are all not my mothership, this ship, or Goliath Fighters, we can consider ourselves still alive. Otherwise, kill my mothership
                foreach (Transform myShip in NetworkManager.Singleton.LocalClient.PlayerObject.transform)
                {
                    if (myShip.GetComponent<Ship>().shipType != ShipTypes.Mothership && myShip.GetComponent<Ship>().shipType != ShipTypes.GoliathFighter && myShip != transform)
                        return;
                }
                playerData.KillMothershipRPC();
                NetworkManager.Singleton.LocalClient.PlayerObject.transform.GetChild(0).GetComponent<Ship>().SelfDestroyShipRPC();
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

    [Rpc(SendTo.Owner)]
    public void UnselectShipRPC()
    {
        UnselectShip();
    }

    public ShipTypes GetShipType()
    {
        return shipType;
    }

    public float GetShipCost()
    {
        return shipCost;
    }

    public override void OnNetworkDespawn()
    {
        // Play sound
        if (Camera_Control.Singleton.IsOnScreen(transform) && Camera_Control.Singleton.IsSeenByMyShips(transform) && deathSound != null)
        {
            if (shipType != ShipTypes.GoliathFighter)
                AudioSource.PlayClipAtPoint(deathSound, Camera.main.transform.position, 0.4f);
            else
                AudioSource.PlayClipAtPoint(deathSound, Camera.main.transform.position, 0.2f);
        }
        // Play effect
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        if (shipType == ShipTypes.Mothership)
            Destroy(explosion, 10f);
        else
            Destroy(explosion, 3f);

        if (IsOwner && GetComponent<NeutralShip>() == null)
        {
            GameObject vision = Instantiate(visionPrefab, transform.position, Quaternion.identity);
            vision.GetComponent<VisionCone>().visionRange = visionRange;
            Destroy(vision, vision.GetComponent<VisionCone>().decayDuration);
        }
    }
}
