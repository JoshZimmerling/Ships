using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    private GameManager gameManager;
    private GameObject spawnPlatform;

    private Ship motherShip;
    private GameObject mapFogRemover;

    public NetworkVariable<int> playerColorIndex = new NetworkVariable<int>(0, writePerm : NetworkVariableWritePermission.Owner);
    public Color playerColor;

    public NetworkVariable<FixedString32Bytes> authenticationServicePlayerId = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        //Debug.Log("TEST"); //TODO Move into the create lobby / join lobby code so that player is spawned with a color that is not already in the lobby
        authenticationServicePlayerId.Value = AuthenticationService.Instance.PlayerId;
    }

    public void PlayerSetup()
    {
        gameManager = GameManager.Singleton;
        spawnPlatform = gameManager.playerSpawns[OwnerClientId];

        gameManager.AddPlayer(this);
        // Color _color = playerColor;
        // _color.a = 0.1f;
        // spawnPlatform.GetComponent<SpriteRenderer>().color = _color;
        // spawnPlatform.SetActive(true);
        // gameObject.name = "Player " + OwnerClientId;

        GameManager.Singleton.ChangeState(GameState.Gameplay);

        if (IsOwner){
            SpawnShipServerRPC(Ship.ShipTypes.Mothership);
            Camera.main.gameObject.GetComponent<Camera_Control>().MoveCameraToWorldSpace(new Vector2(spawnPlatform.transform.position.x, spawnPlatform.transform.position.y));

            mapFogRemover = GameObject.Find("MapFogRemover");
            mapFogRemover.SetActive(false);
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnShipServerRPC(Ship.ShipTypes shipType)
    {
        Vector3 spawnPos;
        Vector2 offset = Random.onUnitCircle * 15;
        if (motherShip == null)
            spawnPos = spawnPlatform.transform.position;
        else
            spawnPos = motherShip.transform.position + new Vector3(offset.x, offset.y);  //motherShip.transform.rotation * new Vector3(0, 15, 0);
        GameObject ship = Instantiate(gameManager.GetShipPrefab((int)shipType), spawnPos, Quaternion.LookRotation(new Vector3(0, 0, 1), -spawnPos));
        ship.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        ship.transform.parent = transform;
        ship.GetComponent<Ship>().SetPlayerData(this);
    }

    public void SetMothership(Ship ms)
    {
        motherShip = ms;
    }

    public bool IsMothershipAlive()
    {
        return motherShip != null;
    }

    public void KillMothership()
    {
        foreach (Transform child in transform)
            if (child.gameObject.GetComponent<Ship>() != null)
                Destroy(child.gameObject);

        mapFogRemover.SetActive(true);
    }
}
