using System.Collections.Generic;
using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // Reference variables
    // Username screen
    private GameObject usernameScreen;
    private TMP_Text usernameTitleText;
    private Button closeUsernameScreenButton;
    private TMP_InputField usernameTextInput;
    private Button submitUsernameButton;
    // Lobby list screen
    private GameObject lobbyListScreen;
    private GameObject lobbyViewerObject;
    [SerializeField] private GameObject lobbyRepeaterPrefab;
    private Button createLobbyButton;
    // Lobby screen
    private GameObject lobbyScreen;
    private TextMeshProUGUI lobbyName;
    private GameObject playerViewerObject;
    [SerializeField] private GameObject playerRepeaterPrefab;
    private Button startGameButton;

    // Runtime variables
    private enum ScreenNames { UsernameScreen, LobbyListScreen, LobbyScreen };
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
        usernameScreen = transform.Find("Choose Username Screen").gameObject;
        usernameTitleText = transform.Find("Choose Username Screen").Find("Header").GetComponentInChildren<TMP_Text>();
        closeUsernameScreenButton = transform.Find("Choose Username Screen").Find("Close Button").GetComponent<Button>();
        closeUsernameScreenButton.onClick.AddListener(() => ChangeScreen(ScreenNames.LobbyListScreen));
        usernameTextInput = transform.Find("Choose Username Screen").Find("Username Text Input").GetComponentInChildren<TMP_InputField>();
        submitUsernameButton = transform.Find("Choose Username Screen").Find("Submit Username Button").GetComponent<Button>();
        submitUsernameButton.onClick.AddListener(SetUsername);

        lobbyListScreen = transform.Find("Lobby List Screen").gameObject;
        lobbyViewerObject = transform.Find("Lobby List Screen").Find("Lobby Viewer").gameObject;
        createLobbyButton = transform.Find("Lobby List Screen").Find("Create Lobby Button").GetComponent<Button>();
        createLobbyButton.onClick.AddListener(() => CreateLobby());

        lobbyScreen = transform.Find("Lobby Screen").gameObject;
        lobbyName = transform.Find("Lobby Screen").transform.Find("Header").GetComponentInChildren<TextMeshProUGUI>();
        playerViewerObject = transform.Find("Lobby Screen").Find("Player Viewer").gameObject;
        startGameButton = transform.Find("Lobby Screen").Find("Start Game Button").GetComponent<Button>();
        startGameButton.onClick.AddListener(() => StartGame());

        //Get player username from save file
        playerName = Save.myGlobalSaveData.username;

        // Initialize the starting screen
        if (playerName == null)
        {
            ChangeScreen(ScreenNames.UsernameScreen);
        }
        else
        {
            ChangeScreen(ScreenNames.LobbyListScreen);
        }
    }

    // Update timer parameters
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

    // Pings lobby to keep it active
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

    // Handles changing the menu screens
    private void ChangeScreen(ScreenNames newScreen)
    {
        currentScreen = newScreen;
        lobbyRefreshTimer = 0f; //Starts immediately data refresh

        // Updates screen state
        usernameScreen.SetActive(currentScreen == ScreenNames.UsernameScreen);
        lobbyListScreen.SetActive(currentScreen == ScreenNames.LobbyListScreen);
        lobbyScreen.SetActive(currentScreen == ScreenNames.LobbyScreen);

        // Does additional code if needed
        switch (currentScreen)
        {
            case ScreenNames.UsernameScreen:
                if (playerName == null)
                {
                    usernameTitleText.text = "Enter Your Username";
                    closeUsernameScreenButton.gameObject.SetActive(false);
                }
                else
                {
                    usernameTitleText.text = "Change Your Username";
                    closeUsernameScreenButton.gameObject.SetActive(true);
                }
                return;
            case ScreenNames.LobbyListScreen:
                return;
            case ScreenNames.LobbyScreen:
                return;
        }
    }

    private async void CreateLobby()
    {
        // Create relay and start real time connection
        string relayCode = null;
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;
            relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

        Debug.Log("RELAY CODE: " + relayCode);

        // Create the lobby enviroment
        try
        {
            string lobbyName = GetPlayer().Data["PlayerName"].Value + "'s Lobby";
            int maxPlayers = 8;

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> {
                   { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
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

        //await SceneManager.LoadSceneAsync("Multiplayer Scene");
    }

    public async void JoinLobby(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByIdOptions);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        string relayCode = currentLobby.Data["RelayCode"].Value;

        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;
            
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

        //await SceneManager.LoadSceneAsync("Multiplayer Scene");

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
        /*
        // Checks if new players have entered lobby
        foreach (Player player in currentLobby.Players)
        {
            Transform t = playerViewerObject.transform.Find(player.Id);
            GameObject playerObject;
            if (t == null)
            {
                playerObject = Instantiate(playerRepeaterPrefab, playerViewerObject.transform);
                playerObject.GetComponent<PlayerRepeater>().menuManager = this;
                playerObject.name = player.Id;
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
                if (child.name == player.Id)
                    exists = true;
            if (!exists)
                Destroy(child.gameObject);
        }*/

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

    
    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
                }
        };
    }

    public Color[] playerColors = new Color[12];
    /*
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

    private void StartGame()
    {


        // Change to just switch scene
    }

    private void SetUsername()
    {
        if (usernameTextInput != null)
        {
            string newUsername = usernameTextInput.text;
            playerName = newUsername;
            Save.myGlobalSaveData.UpdateUsername(newUsername);

            ChangeScreen(ScreenNames.LobbyListScreen);
        }
    }
}
