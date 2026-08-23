using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : Singleton<MenuManager>
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
    private TMP_Text usernameText;
    private Button updateUsernameButton;
    [SerializeField] private GameObject lobbyRepeaterPrefab;
    private Button createLobbyButton;
    // Lobby screen
    private GameObject lobbyScreen;
    private TextMeshProUGUI lobbyName;
    private GameObject playerViewerObject;
    [SerializeField] private GameObject playerRepeaterPrefab;
    private Button startGameButton;

    // Runtime variables
    public enum ScreenNames { LobbyListScreen, LobbyScreen };
    private ScreenNames currentScreen;

    private Lobby currentLobby;

    private string playerName;

    //public List<PlayerData> playerDatas = new List<PlayerData>();

    void Start()
    {
        // Activate all ui elements (for if they are disabled for testing)
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);
        
        // Initialize Variables
        usernameScreen = transform.Find("Choose Username Screen").gameObject;
        usernameTitleText = transform.Find("Choose Username Screen").Find("Header").GetComponentInChildren<TMP_Text>();
        closeUsernameScreenButton = transform.Find("Choose Username Screen").Find("Close Button").GetComponent<Button>();
        closeUsernameScreenButton.onClick.AddListener(() => usernameScreen.SetActive(false));
        usernameTextInput = transform.Find("Choose Username Screen").Find("Username Text Input").GetComponentInChildren<TMP_InputField>();
        submitUsernameButton = transform.Find("Choose Username Screen").Find("Submit Username Button").GetComponent<Button>();
        submitUsernameButton.onClick.AddListener(SetUsername);

        lobbyListScreen = transform.Find("Lobby List Screen").gameObject;
        lobbyViewerObject = transform.Find("Lobby List Screen").Find("Lobby Viewer").gameObject;
        usernameText = transform.Find("Lobby List Screen").Find("Your Username").Find("Username Text").GetComponentInChildren<TMP_Text>();
        updateUsernameButton = transform.Find("Lobby List Screen").Find("Your Username").Find("Edit Username Button").GetComponent<Button>();
        updateUsernameButton.onClick.AddListener(() => OpenUsernamePopup());
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
        ChangeScreen(ScreenNames.LobbyListScreen);
        if (playerName == null)
        {
            OpenUsernamePopup();
            lobbyListScreen.SetActive(false);
        }
    }

    // Update timer parameters
    private readonly float lobbyRefreshTimeMax = 1.1f;
    private float lobbyRefreshTimer = 0f;

    private readonly float lobbyVisualTimeMax = 0.1f;
    private float lobbyVisualTimer = 0f;

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
                }
                lobbyVisualTimer -= Time.deltaTime;
                if (lobbyVisualTimer < 0f)
                {
                    RefreshLobbyVisuals();
                }
                return;
        }

        HandleLobbyHeartbeat();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            Debug.Log("Enter key pressed " + usernameScreen.activeSelf);
        if (usernameScreen.activeSelf && Input.GetKeyDown(KeyCode.Return))
            SetUsername();
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
    public void ChangeScreen(ScreenNames newScreen)
    {
        currentScreen = newScreen;
        lobbyRefreshTimer = 0f; //Starts immediately data refresh

        // Updates screen state
        usernameScreen.SetActive(false);
        lobbyListScreen.SetActive(currentScreen == ScreenNames.LobbyListScreen);
        lobbyScreen.SetActive(currentScreen == ScreenNames.LobbyScreen);

        // Does additional code if needed
        switch (currentScreen)
        {
            case ScreenNames.LobbyListScreen:
                usernameText.text = playerName;
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

        // Create the lobby enviroment
        try
        {
            Player player = GetLocalPlayer();
            string lobbyName = player.Data["PlayerName"].Value + "'s Lobby";
            int maxPlayers = 8;

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Player = player,
                Data = new Dictionary<string, DataObject> {
                   { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                }
            };
            
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            //Debug.Log("Created Lobby! " + currentLobby.Name + " " + currentLobby.MaxPlayers);
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        foreach (Player player in currentLobby.Players)
            Debug.Log("Player Id: " + player.Id);
        ChangeScreen(ScreenNames.LobbyScreen);
    }

    public async void JoinLobby(string lobbyId)
    {
        // Join lobby
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetLocalPlayer()
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByIdOptions);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        // Join real time relay
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(currentLobby.Data["RelayCode"].Value);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

        await RefreshLobbyInfo();
        //ChangeScreen(MenuManager.ScreenNames.LobbyScreen);
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

    private async Task RefreshLobbyInfo()
    {
        lobbyRefreshTimer = lobbyRefreshTimeMax;

        // Checks are in an active lobby
        if (currentLobby == null)
            return;
        // Gets updated lobby info
        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        InLobbyCheck();
    }

    private void InLobbyCheck()
    {
        // Verifies if player is still in lobby
        bool inLobby = false;
        if (currentLobby != null)
            foreach (Player player in currentLobby.Players)
                if (player.Id == AuthenticationService.Instance.PlayerId) inLobby = true;
        if (!inLobby)
        {
            PlayerDataList.Singleton.players = new();
            NetworkManager.Singleton.Shutdown();
            currentLobby = null;

            ChangeScreen(ScreenNames.LobbyListScreen);
        }
    }

    private void RefreshLobbyVisuals()
    {
        if (currentLobby == null) return;

        lobbyVisualTimer = lobbyVisualTimeMax;

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
        }

        // Update lobby info
        lobbyName.text = currentLobby.Name;
        startGameButton.gameObject.SetActive(AuthenticationService.Instance.PlayerId == currentLobby.HostId);
    }

    public async void RemovePlayerFromLobby(string playerId)
    {
        await RefreshLobbyInfo();
        try
        {
            // Migrate host if needed
            if (playerId == currentLobby.HostId && currentLobby.Players.Count > 1)
            {
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    HostId = currentLobby.Players[1].Id
                });
            }

            // Remove from lobby
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);

            ulong kickId = new ulong();
            foreach (var (id, playerData) in PlayerDataList.Singleton.players)
                if (playerData.authenticationServicePlayerId.Value == playerId)
                    kickId = id;
            PlayerDataList.Singleton.players.Remove(kickId);

            // Remove reference to lobby if you leave
            if (playerId == AuthenticationService.Instance.PlayerId)
            {
                PlayerDataList.Singleton.players = new();
                NetworkManager.Singleton.Shutdown();
                currentLobby = null;
                ChangeScreen(ScreenNames.LobbyListScreen);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        RefreshLobbyVisuals();
    }

    private Player GetLocalPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                    { "PlayerName",   new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
                }
        };
    }

    public Color[] playerColors = new Color[12];
    
    private int GetAvailableColor(int colorValue)
    {
        List<int> colors = new List<int>();
        foreach (var (id, player) in PlayerDataList.Singleton.players)
            colors.Add(player.playerColorIndex.Value);

        
        while (true)
        {
            if (colors.Contains(colorValue))
                if (colorValue == playerColors.Length - 1)
                    colorValue = 0;
                else
                    colorValue++;
            else
                break;
        }

        return colorValue;
    }
    
    public void ChangePlayerColor(string playerId)
    {
        foreach (var (id, player) in PlayerDataList.Singleton.players)
            if (player.authenticationServicePlayerId.Value == playerId)
                player.playerColorIndex.Value = GetAvailableColor(player.playerColorIndex.Value);
    }

    private void StartGame()
    {
        //foreach (PlayerData data in playerDatas)
        //    data.playerColor = playerColors[data.playerColorIndex.Value];

        NetworkManager.Singleton.SceneManager.LoadScene("Multiplayer Scene", LoadSceneMode.Single);
    }

    private void OpenUsernamePopup()
    {
        usernameScreen.SetActive(true);
        usernameTextInput.text = "";
        usernameTextInput.ActivateInputField();

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
    }

    private void SetUsername()
    {
        if (usernameTextInput.text != "")
        {
            //This is at the top to recognize if this was a first time username setup
            if (playerName == null)
                ChangeScreen(ScreenNames.LobbyListScreen);

            string newUsername = usernameTextInput.text;
            playerName = newUsername;
            Save.myGlobalSaveData.UpdateUsername(newUsername);

            usernameScreen.SetActive(false);

            //Switch statement for if we want different screens to have change username functionality
            switch (currentScreen)
            {
                case ScreenNames.LobbyListScreen:
                    usernameText.text = playerName;
                    return;
            }
        }
    }
}
