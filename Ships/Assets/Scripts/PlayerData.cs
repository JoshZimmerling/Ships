using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    private GameSceneManager gameManager;

    private Ship motherShip;
    private GameObject mapFogRemover;

    public NetworkVariable<int> playerColorIndex = new NetworkVariable<int>(-1, writePerm : NetworkVariableWritePermission.Owner);
    public Color playerColor;

    public NetworkVariable<FixedString32Bytes> authenticationServicePlayerId = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> playerUsername = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        PlayerDataList.Singleton.players.Add(OwnerClientId, this);

        playerColorIndex.OnValueChanged += (int previousValue, int newValue) =>
        {
            playerColor = MenuScreenManager.Singleton.playerColors[newValue];
        };

        if (IsOwner) 
        {
            authenticationServicePlayerId.Value = AuthenticationService.Instance.PlayerId;
            MenuScreenManager.Singleton.ChangePlayerColor(AuthenticationService.Instance.PlayerId);
            playerUsername.Value = Save.myGlobalSaveData.username;

            // Change screen after syncronize is complete
            NetworkManager.Singleton.SceneManager.OnSceneEvent += (SceneEvent sceneEvent) => {
                if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
                    MenuScreenManager.Singleton.ChangeScreen(MenuScreenManager.ScreenNames.LobbyScreen);
            };
        }

        if (playerColorIndex.Value != -1) playerColor = MenuScreenManager.Singleton.playerColors[playerColorIndex.Value];
    }

    public void PlayerSetup()
    {
        gameManager = GameSceneManager.Singleton;

        GameSceneManager.Singleton.ChangeState(GameState.Gameplay);

        if (IsOwner){
            SpawnShipServerRPC(Ship.ShipTypes.Mothership);

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
        {
            GameObject spawnerPlatform = gameManager.playerSpawns[Random.Range(0, gameManager.playerSpawns.Count)];
            spawnPos = spawnerPlatform.transform.position;
            gameManager.playerSpawns.Remove(spawnerPlatform);
        }
        else
            spawnPos = motherShip.transform.position + new Vector3(offset.x, offset.y);
        GameObject ship = Instantiate(gameManager.GetShipPrefab((int)shipType), spawnPos, Quaternion.LookRotation(new Vector3(0, 0, 1), -spawnPos));

        ship.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        ship.transform.parent = transform;

        GameSceneManager.Singleton.shipsInScene.Add(ship);
    }

    public void SetMothership(Ship ms)
    {
        motherShip = ms;

        if (IsOwner)
            Camera.main.gameObject.GetComponent<Camera_Control>().MoveCameraToWorldSpace(new Vector2(ms.transform.position.x, ms.transform.position.y));
    }

    public Ship GetMothership()
    {
        return motherShip;
    }

    public bool IsMothershipAlive()
    {
        return motherShip != null;
    }

    [Rpc(SendTo.Owner)]
    public void KillMothershipRPC()
    {
        foreach (Transform child in transform)
            if (child.gameObject.GetComponent<Ship>() != null && child.gameObject.GetComponent<Ship>().GetShipType() != Ship.ShipTypes.Mothership)
                child.gameObject.GetComponent<Ship>().DestroyShipRPC();

        mapFogRemover.SetActive(true);
        Shop.Singleton.gameObject.SetActive(false);

        if (!IsHost)
            GameplayInputManager.Singleton.ShowLeaveGameButton();

        //Check if you are last mothership standing, if so show the leave lobby button
        int numMothershipsLeft = 0;
        foreach (Ship ship in FindObjectsByType(typeof(Ship), FindObjectsSortMode.None))
        {
            if (ship.GetShipType() == Ship.ShipTypes.Mothership)
            {
                numMothershipsLeft++;
            }
        }

        //Set to 2 since this runs right before destroying our own 
        if (numMothershipsLeft <= 2)
            ShowAllPlayersLeaveButtonRPC();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowAllPlayersLeaveButtonRPC()
    {
        GameplayInputManager.Singleton.ShowLeaveGameButton();
    }
}
