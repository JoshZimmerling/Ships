using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MyPlayer : NetworkBehaviour
{
    
    private readonly NetworkVariable<Color> playerColor = new NetworkVariable<Color>(Color.red, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int money;
    private bool alive;


    public override void OnNetworkSpawn()
    {
        playerColor.OnValueChanged += UpdateColor;
    }

    public void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            playerColor.Value = Color.blue;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SpawnShipServerRPC();
        }
    }

    private void UpdateColor(Color previousColor, Color newColor)
    {
        Debug.Log(newColor);
    }

    [ServerRpc]
    private void SpawnShipServerRPC()
    {
        Debug.Log("Spawning Ship for: " + OwnerClientId);
    }
}
