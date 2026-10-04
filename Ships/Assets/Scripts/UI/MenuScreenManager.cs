using System.Collections.Generic;
using TMPro;
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

public class MenuScreenManager : Singleton<MenuScreenManager>
{
    // Reference variables
    private GameObject spinner;
    private bool canClickButtons = true;
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

    public Lobby currentLobby;

    private string playerName;

    bool everyoneReady = false;
    public Color[] playerColors = new Color[12];

    private GameSettingsManager gameSettingsManager;
    private Button nextMapButton;
    private Button lastMapButton;
    private TextMeshProUGUI mapName;
    private Image mapImage;

    private 

    void Start()
    {
        // Activate all ui elements (for if they are disabled for testing)
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);

        // Initialize Variables
        spinner = transform.Find("Spinner").gameObject;

        usernameScreen = transform.Find("Choose Username Screen").gameObject;
        usernameTitleText = transform.Find("Choose Username Screen/Header").GetComponentInChildren<TMP_Text>();
        closeUsernameScreenButton = transform.Find("Choose Username Screen/Close Button").GetComponent<Button>();
        closeUsernameScreenButton.onClick.AddListener(() => usernameScreen.SetActive(false));
        usernameTextInput = transform.Find("Choose Username Screen/Username Text Input").GetComponentInChildren<TMP_InputField>();
        submitUsernameButton = transform.Find("Choose Username Screen/Submit Username Button").GetComponent<Button>();
        submitUsernameButton.onClick.AddListener(SetUsername);

        lobbyListScreen = transform.Find("Lobby List Screen").gameObject;
        lobbyViewerObject = transform.Find("Lobby List Screen/Lobby Viewer").gameObject;
        usernameText = transform.Find("Lobby List Screen/Your Username/Username Text").GetComponentInChildren<TMP_Text>();
        updateUsernameButton = transform.Find("Lobby List Screen/Your Username/Edit Username Button").GetComponent<Button>();
        updateUsernameButton.onClick.AddListener(() => OpenUsernamePopup());
        createLobbyButton = transform.Find("Lobby List Screen/Create Lobby Button").GetComponent<Button>();
        createLobbyButton.onClick.AddListener(() => CreateLobby());

        nextMapButton = transform.Find("Lobby Screen/Map Settings Window/Minimap/Next Map Button").GetComponent<Button>();
        nextMapButton.onClick.AddListener(() => gameSettingsManager.NextMap());
        lastMapButton = transform.Find("Lobby Screen/Map Settings Window/Minimap/Last Map Button").GetComponent<Button>();
        lastMapButton.onClick.AddListener(() => gameSettingsManager.LastMap());
        mapName = transform.Find("Lobby Screen/Map Settings Window/Minimap/Map Name").GetComponent<TextMeshProUGUI>();
        mapImage = transform.Find("Lobby Screen/Map Settings Window/Minimap").GetComponent<Image>();

        lobbyScreen = transform.Find("Lobby Screen").gameObject;
        lobbyName = transform.Find("Lobby Screen/Header").GetComponentInChildren<TextMeshProUGUI>();
        playerViewerObject = transform.Find("Lobby Screen/Player Viewer").gameObject;
        foreach (Transform t in playerViewerObject.transform)
            Destroy(t.gameObject);
        startGameButton = transform.Find("Lobby Screen/Start Game Button").GetComponent<Button>();
        startGameButton.onClick.AddListener(() => ReadyOrStartGame());

        //Get player username from save file
        playerName = Save.myGlobalSaveData.username;

        // Initialize the starting screen
        ChangeScreen(ScreenNames.LobbyListScreen);
        if (playerName == null)
        {
            OpenUsernamePopup();
            lobbyListScreen.SetActive(false);
        }

        gameSettingsManager = GameObject.Find("Game Settings Manager").GetComponent<GameSettingsManager>();
    }

    // Update timer parameters
    private readonly float lobbyRefreshTimeMax = 1.1f;
    private float lobbyRefreshTimer = 0f;

    private readonly float heartbeatTimeMax = 15f;
    private float heartbeatTimer = 0f;

    private void Update()
    {
        if (usernameScreen.activeSelf && Input.GetKeyDown(KeyCode.Return))
            SetUsername();

        switch (currentScreen)
        {
            case ScreenNames.LobbyListScreen:
                lobbyRefreshTimer -= Time.deltaTime;
                if (lobbyRefreshTimer < 0f)
                {
                    RefreshLobbyList();
                }
                break;
            case ScreenNames.LobbyScreen:
                RefreshLobbyVisuals();
                break;
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
    public void ChangeScreen(ScreenNames newScreen)
    {
        currentScreen = newScreen;

        // Updates screen state
        usernameScreen.SetActive(false);
        spinner.SetActive(false);
        canClickButtons = true;

        lobbyListScreen.SetActive(currentScreen == ScreenNames.LobbyListScreen);
        lobbyScreen.SetActive(currentScreen == ScreenNames.LobbyScreen);

        // Does additional code if needed
        switch (currentScreen)
        {
            case ScreenNames.LobbyListScreen:
                lobbyRefreshTimer = 0f;
                currentLobby = null;
                usernameText.text = playerName;
                return;
            case ScreenNames.LobbyScreen:
                return;
        }
    }

    private async void CreateLobby()
    {
        if (!canClickButtons) return;

        spinner.SetActive(true);
        canClickButtons = false;
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
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        lobbyName.text = currentLobby.Name;
        ChangeScreen(ScreenNames.LobbyScreen);
    }

    public async void JoinLobby(string lobbyId)
    {
        if (!canClickButtons) return;

        spinner.SetActive(true);
        canClickButtons = false;
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

        lobbyName.text = currentLobby.Name;
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
            Transform t = lobbyViewerObject.transform.Find(lobby.Id);
            GameObject lobbyObject = null;
            if (t == null)
            {
                lobbyObject = Instantiate(lobbyRepeaterPrefab, lobbyViewerObject.transform);
                lobbyObject.GetComponent<LobbyRepeater>().menuManager = this;
                lobbyObject.name = lobby.Id;
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
                if (child.name == lobby.Id)
                    exists = true;
            if (!exists)
                Destroy(child.gameObject);
        }
    }

    private void RefreshLobbyVisuals()
    {
        // Get clients in scene
        IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;

        everyoneReady = true;
        foreach (NetworkClient client in clients)
        {
            // Checks if new players have entered lobby
            if (playerViewerObject.transform.Find(client.ClientId.ToString()) == null)
            {
                GameObject playerObject = Instantiate(playerRepeaterPrefab, playerViewerObject.transform);
                playerObject.name = client.ClientId.ToString();
                playerObject.GetComponent<PlayerRepeater>().client = client;
            }

            // Check that everyones ready
            if (!client.PlayerObject.GetComponent<PlayerData>().playerReady.Value && !client.PlayerObject.IsLocalPlayer)
                everyoneReady = false;
        }

        if (NetworkManager.Singleton.IsHost)
        {   // Set button to clickable game
            startGameButton.interactable = everyoneReady;
            startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Game";
        }
        else
        {
            startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
            //ColorBlock colors = startGameButton.colors; //TODO: Fix later
            //colors.normalColor = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().playerReady.Value ? Color.gray : Color.white;
            //startGameButton.colors = colors;
        }

        nextMapButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
        lastMapButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
        mapName.text = gameSettingsManager.mapsList.PrefabList[gameSettingsManager.mapIndex.Value].Prefab.name.ToString();
        mapImage.sprite = gameSettingsManager.mapSprites[gameSettingsManager.mapIndex.Value];
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
    
    public void ChangePlayerColor(PlayerData player)
    {
        int colorValue = player.playerColorIndex.Value;

        List<int> colors = new List<int>();
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            colors.Add(client.PlayerObject.GetComponent<PlayerData>().playerColorIndex.Value);
        }

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

        player.playerColorIndex.Value = colorValue;
        player.playerReady.Value = false;
    }

    private void ReadyOrStartGame() //TODO: and ready
    {
        if (!canClickButtons) return;

        if (NetworkManager.Singleton.IsHost)
        {
            spinner.SetActive(true);
            canClickButtons = false;
            try
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions
                {
                    IsLocked = true,
                    IsPrivate = true
                };

                LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

            NetworkManager.Singleton.SceneManager.LoadScene("Multiplayer Scene", LoadSceneMode.Single);
        }
        else
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().playerReady.Value = !NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().playerReady.Value;
        }
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
