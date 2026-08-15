using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    private GameManager gameManager = GameManager.Singleton;
    private Color playerColor;
    private GameObject spawnPlatform;

    public override void OnNetworkSpawn()
    {
        gameManager.AddPlayer(this);
        playerColor = gameManager.playerColors[OwnerClientId];
        spawnPlatform = gameManager.playerSpawns[OwnerClientId];
        Color _color = playerColor;
        _color.a = 0.1f;
        spawnPlatform.GetComponent<SpriteRenderer>().color = _color;
        spawnPlatform.SetActive(true);
        gameObject.name = "Player " + OwnerClientId;

        if (!IsOwner) return;

        Camera.main.transform.position = new Vector3(spawnPlatform.transform.position.x, spawnPlatform.transform.position.y, Camera.main.transform.position.z);
    }

    [ServerRpc]
    public void SpawnShipServerRPC(Ship.ShipTypes shipType)
    {
        Vector3 spawnPos = spawnPlatform.transform.position;
        GameObject ship = Instantiate(gameManager.GetShipPrefab((int)shipType), spawnPos, Quaternion.LookRotation(new Vector3(0, 0, 1), -spawnPos));
        ship.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        ship.transform.parent = transform;
    }
}
