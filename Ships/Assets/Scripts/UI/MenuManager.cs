using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;
using Unity.Networking.Transport.Relay;

public class MenuManager : MonoBehaviour
{
    private GameObject lobbyListScreen;
    private GameObject lobbyViewerObject;
    [SerializeField] private GameObject lobbyRepeaterPrefab;
    private Button createLobbyButton;

    private GameObject lobbyScreen;
    private TextMeshProUGUI lobbyName;
    private GameObject playerViewerObject;
    [SerializeField] private GameObject playerRepeaterPrefab;
    private Button startGameButton;

    private enum ScreenNames { LobbyListScreen, LobbyScreen };
    private ScreenNames currentScreen;

    private Lobby currentLobby;

    private string playerName;

    void Start()
    {
        //TODO: Move to file storage system
        playerName = "Player " + Random.Range(100, 999);
        Debug.Log(playerName);

        // Activate all ui elements (for if they are disabled for testing)
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);
        
        // Initialize Variables
        lobbyListScreen = transform.Find("Lobby List Screen").gameObject;
        lobbyViewerObject = transform.Find("Lobby List Screen").Find("Lobby Viewer").gameObject;
        createLobbyButton = transform.Find("Lobby List Screen").Find("Create Lobby Button").GetComponent<Button>();
        createLobbyButton.onClick.AddListener(() => CreateLobby());

        lobbyScreen = transform.Find("Lobby Screen").gameObject;
        lobbyName = transform.Find("Lobby Screen").transform.Find("Header").GetComponentInChildren<TextMeshProUGUI>();
        playerViewerObject = transform.Find("Lobby Screen").Find("Player Viewer").gameObject;
        startGameButton = transform.Find("Lobby Screen").Find("Start Game Button").GetComponent<Button>();
        startGameButton.onClick.AddListener(() => StartGame());

        // Initialize the starting screen
        ChangeScreen(ScreenNames.LobbyListScreen);
    }

    private readonly float lobbyRefreshTimeMax = 3f;
    private float lobbyRefreshTimer = 0f;


    private readonly float heartbeatTimeMax = 15f;
    private float heartbeatTimer = 0f;
    void FixedUpdate()
    {
        lobbyRefreshTimer -= Time.deltaTime;

        switch (currentScreen)
        {
            case ScreenNames.LobbyListScreen:
                if (lobbyRefreshTimer < 0f)
                {
                    RefreshLobbyList();
                }
                return;
            case ScreenNames.LobbyScreen:
                if (lobbyRefreshTimer < 0f)
                {
                    RefreshLobbyInfo();
                    RefreshLobbyVisuals();
                }
                return;
        }

        HandleLobbyHeartbeat();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                heartbeatTimer = heartbeatTimeMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    private void ChangeScreen(ScreenNames newScreen)
    {
        currentScreen = newScreen;
        lobbyRefreshTimer = 0f; //Starts immediately data refresh

        // Updates screen state
        lobbyListScreen.SetActive(currentScreen == ScreenNames.LobbyListScreen);
        lobbyScreen.SetActive(currentScreen == ScreenNames.LobbyScreen);

        // Does additional code if needed
        switch (currentScreen)
        {
            case ScreenNames.LobbyListScreen:
                return;
            case ScreenNames.LobbyScreen:
                return;
        }
    }

    private async void CreateLobby()
    {
        try
        {
            string lobbyName = GetPlayer().Data["PlayerName"].Value + "'s Lobby";
            int maxPlayers = 8;

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                //IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> {
                   { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "") }
                }
            };


            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            currentLobby = lobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        ChangeScreen(ScreenNames.LobbyScreen);
    }

    public async void JoinLobby(string lobbyId)
    {
        try
        {
            Player player = GetPlayer();
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = player
            };
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByIdOptions);
            ChangePlayerColor(player.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        ChangeScreen(ScreenNames.LobbyScreen);
    }

    private async void RefreshLobbyList()
    {
        lobbyRefreshTimer = lobbyRefreshTimeMax;

        // Attempts to pull lobby list
        QueryResponse lobbyList = null;
        try {
            lobbyList = await LobbyService.Instance.QueryLobbiesAsync();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
        if (lobbyList == null) return;

        foreach (Lobby lobby in lobbyList.Results)
        {
            Transform t = lobbyViewerObject.transform.Find(lobby.Name);
            GameObject lobbyObject = null;
            if (t == null)
            {
                lobbyObject = Instantiate(lobbyRepeaterPrefab, lobbyViewerObject.transform);
                lobbyObject.GetComponent<LobbyRepeater>().menuManager = this;
                lobbyObject.name = lobby.Name;
            }
            else
                lobbyObject = t.gameObject;
            lobbyObject.GetComponent<LobbyRepeater>().UpdateLobbyDetails(lobby);
        }

        for (int i = lobbyViewerObject.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = lobbyViewerObject.transform.GetChild(i);
            bool exists = false;
            foreach (Lobby lobby in lobbyList.Results)
                if (child.name == lobby.Name)
                    exists = true;
            if (!exists)
                Destroy(child.gameObject);
        }
    }

    private async void RefreshLobbyInfo()
    {
        lobbyRefreshTimer = lobbyRefreshTimeMax;

        // Checks are in an active lobby
        if (currentLobby == null) //TODO: maybe verify lobby still exists
            return;
        // Gets updated lobby info
        try {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }

        // Checks for if the game has started
        if (currentLobby.Data["RelayCode"].Value != "")
            JoinGame(currentLobby.Data["RelayCode"].Value);
    }

    private void RefreshLobbyVisuals()
    {
        // Verifies if player is still in lobby
        bool inLobby = false;
        if (currentLobby != null)
            foreach (Player player in currentLobby.Players)
                if (player.Id == AuthenticationService.Instance.PlayerId) inLobby = true;
        if (!inLobby)
        {
            currentLobby = null;
            ChangeScreen(ScreenNames.LobbyListScreen);
            return;
        }

        // Checks if new players have entered lobby
        foreach (Player player in currentLobby.Players)
        {
            if (player.Id == AuthenticationService.Instance.PlayerId) inLobby = true;
            Transform t = playerViewerObject.transform.Find(player.Data["PlayerName"].Value);
            GameObject playerObject = null;
            if (t == null)
            {
                playerObject = Instantiate(playerRepeaterPrefab, playerViewerObject.transform);
                playerObject.GetComponent<PlayerRepeater>().menuManager = this;
                playerObject.name = player.Data["PlayerName"].Value;
            }
            else
                playerObject = t.gameObject;
            // Updates the data in the repeaters
            playerObject.GetComponent<PlayerRepeater>().UpdatePlayerDetails(player, currentLobby.HostId);
        }

        // Remove players that have left the lobby
        for (int i = playerViewerObject.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = playerViewerObject.transform.GetChild(i);
            bool exists = false;
            foreach (Player player in currentLobby.Players)
                if (child.name == player.Data["PlayerName"].Value)
                    exists = true;
            if (!exists)
                Destroy(child.gameObject);
        }

        // Update lobby info
        lobbyName.text = currentLobby.Name;
        startGameButton.gameObject.SetActive(AuthenticationService.Instance.PlayerId == currentLobby.HostId);
    }

    public async void RemovePlayerFromLobby(string playerId)
    {
        try
        {
            RefreshLobbyInfo();
            // Migrate host if needed
            if (playerId == currentLobby.HostId && currentLobby.Players.Count > 1)
            {
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    HostId = currentLobby.Players[1].Id
                });
            }
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            // Remove reference to lobby
            if (playerId == AuthenticationService.Instance.PlayerId)
            {
                currentLobby = null;
                ChangeScreen(ScreenNames.LobbyListScreen);
            }
            RefreshLobbyVisuals();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    
    private Player GetPlayer(Lobby lobby = null)
    {
        int colorValue = 0;

        if (lobby != null)
            colorValue = GetAvailableColor(lobby, 0);

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)},
                    { "Color",      new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, colorValue.ToString())}
                }
        };
    }

    public Color[] playerColors = new Color[12];
    private int GetAvailableColor(Lobby lobby, int colorValue)
    {
        List<string> colors = new List<string>();
        foreach (var player in lobby.Players)
            colors.Add(player.Data["Color"].Value);

        
        while (colorValue < playerColors.Length)
        {
            if (colors.Contains(colorValue.ToString()))
                colorValue++;
            else
                break;
        }

        return colorValue;
    }

    public async void ChangePlayerColor(string playerId)
    {
        try
        {
            Player player = null;
            foreach (Player p2 in currentLobby.Players)
                if (p2.Id == playerId)
                    player = p2;

            UpdatePlayerOptions updatePlayerOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject> {
                    { "Color",      new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetAvailableColor(currentLobby, int.Parse(player.Data["Color"].Value)).ToString())}
                }
            };
            currentLobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, playerId, updatePlayerOptions);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        RefreshLobbyVisuals();
    }

    private async void StartGame()
    {
        string relayCode = null;
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;
            relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
     
        if (relayCode == null) return;
        try
        {
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) } }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        Debug.Log("Relay created: " + relayCode);

        await SceneManager.LoadSceneAsync("Multiplayer Scene");
        NetworkManager.Singleton.StartHost();
    }

    private async void JoinGame(string relayCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;
            // return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
        Debug.Log("Relay joined: " + relayCode);

        await SceneManager.LoadSceneAsync("Multiplayer Scene");
        NetworkManager.Singleton.StartClient();
    }
}
