using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    private GameManager gameManager;
    private GameObject spawnPlatform;

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

        gameManager.AddPlayer(this);
        spawnPlatform = gameManager.playerSpawns[OwnerClientId];
        Color _color = playerColor;
        _color.a = 0.1f;
        spawnPlatform.GetComponent<SpriteRenderer>().color = _color;
        spawnPlatform.SetActive(true);
        gameObject.name = "Player " + OwnerClientId;

        GameManager.Singleton.ChangeState(GameState.Gameplay);

        if (!IsOwner) return;

        Camera.main.transform.position = new Vector3(spawnPlatform.transform.position.x, spawnPlatform.transform.position.y, Camera.main.transform.position.z);
    }

    [Rpc(SendTo.Server)]
    public void SpawnShipServerRPC(Ship.ShipTypes shipType)
    {
        Vector3 spawnPos = spawnPlatform.transform.position;
        GameObject ship = Instantiate(gameManager.GetShipPrefab((int)shipType), spawnPos, Quaternion.LookRotation(new Vector3(0, 0, 1), -spawnPos));
        ship.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        ship.transform.parent = transform;
    }
}
