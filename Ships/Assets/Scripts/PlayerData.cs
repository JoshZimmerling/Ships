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

    public NetworkVariable<int> playerColorIndex = new NetworkVariable<int>(-1, writePerm : NetworkVariableWritePermission.Owner);
    public Color playerColor;

    public NetworkVariable<FixedString32Bytes> authenticationServicePlayerId = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        MenuManager.Singleton.playerDatas.Add(this);
        playerColorIndex.OnValueChanged += UpdateColor;

        if (!IsOwner) return;

        // Ensure the NetworkManager is initialized before subscribing
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEventReceived;
        }

        authenticationServicePlayerId.Value = AuthenticationService.Instance.PlayerId;
        MenuManager.Singleton.ChangePlayerColor(AuthenticationService.Instance.PlayerId);
    }

    private void UpdateColor(int previousValue, int newValue)
    {
        playerColor = MenuManager.Singleton.playerColors[newValue];
    }

    public void PlayerSetup()
    {
        gameManager = GameManager.Singleton;
        spawnPlatform = gameManager.playerSpawns[OwnerClientId];

        gameManager.AddPlayer(this);

        GameManager.Singleton.ChangeState(GameState.Gameplay);

        if (IsOwner){
            SpawnShipServerRPC(Ship.ShipTypes.Mothership);
            Camera.main.gameObject.GetComponent<Camera_Control>().MoveCameraToWorldSpace(new Vector2(spawnPlatform.transform.position.x, spawnPlatform.transform.position.y));

            mapFogRemover = GameObject.Find("MapFogRemover");
            mapFogRemover.SetActive(false);
        }
    }
    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEventReceived;
        }
    }

    private void OnSceneEventReceived(SceneEvent sceneEvent)
    {
        // Check if the event type is SynchronizeComplete
        if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
        {
            MenuManager.Singleton.ChangeScreen(MenuManager.ScreenNames.LobbyScreen);
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
            spawnPos = motherShip.transform.position + new Vector3(offset.x, offset.y);
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
