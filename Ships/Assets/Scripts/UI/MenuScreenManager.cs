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
    private Button showShipInfoScreenButton;
    // Lobby screen
    private GameObject lobbyScreen;
    private TextMeshProUGUI lobbyName;
    private GameObject playerViewerObject;
    [SerializeField] private GameObject playerRepeaterPrefab;
    private Button startGameButton;
    // Map settings
    private Button nextMapButton;
    private Button lastMapButton;
    private TextMeshProUGUI mapName;
    private Image mapImage;
    private GameSettingsManager gameSettingsManager;
    // Player settings
    private Button choosePrevFactionButton;
    private Button chooseNextFactionButton;
    private TextMeshProUGUI chooseFactionNameText;
    private int currentChosenFactionIndex = 0;
    private Button choosePrevColor;
    private Button chooseNextColor;
    private Image chosenColorBox;
    // Ship info screen
    private GameObject shipInfoScreen;
    private Button exitShipInfoScreenButton;
    private int currentViewedFactionIndex = 0;
    private TextMeshProUGUI factionNameText;
    private TextMeshProUGUI factionDescriptionText;
    private Button prevFactionButton;
    private Button nextFactionButton;
    [SerializeField] private GameObject shipRepeaterPrefab;
    private GameObject shipList;

    private GameObject shipDetailsPanel;
    private TextMeshProUGUI shipNameText;
    private RectTransform shipClassTransform;
    private TextMeshProUGUI shipClassText;
    private Image shipClassIconImage;
    private Image shipImage;
    private TextMeshProUGUI shipHealthText;
    private TextMeshProUGUI shipTopSpeedText;
    private TextMeshProUGUI shipAccelerationText;
    private TextMeshProUGUI shipTurningSpeedText;
    private TextMeshProUGUI shipDescriptionText;

    [SerializeField] private GameObject turretRepeaterPrefab;
    private GameObject turretDetailsPanel;
    private TextMeshProUGUI turretTypeText;
    private TextMeshProUGUI turretDmgText;
    private TextMeshProUGUI turretFireRateText;
    private TextMeshProUGUI turretProjectileSpeedText;
    private TextMeshProUGUI turretRangeText;
    private Transform turretLine_Left;
    private Transform turretLine_Right;
    private Transform turretCircle;

    // Runtime variables
    public enum ScreenNames { LobbyListScreen, LobbyScreen, ShipInfoScreen };
    private ScreenNames currentScreen;

    public Lobby currentLobby;

    private string playerName;

    bool everyoneReady = false;
    public Color[] playerColors = new Color[12];

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
        showShipInfoScreenButton = transform.Find("Lobby List Screen/Ship Info Button").GetComponent<Button>();
        showShipInfoScreenButton.onClick.AddListener(() => ChangeScreen(ScreenNames.ShipInfoScreen));

        nextMapButton = transform.Find("Lobby Screen/Map Settings Window/Minimap/Next Map Button").GetComponent<Button>();
        nextMapButton.onClick.AddListener(() => gameSettingsManager.NextMap());
        lastMapButton = transform.Find("Lobby Screen/Map Settings Window/Minimap/Last Map Button").GetComponent<Button>();
        lastMapButton.onClick.AddListener(() => gameSettingsManager.LastMap());
        mapName = transform.Find("Lobby Screen/Map Settings Window/Minimap/Map Name").GetComponent<TextMeshProUGUI>();
        mapImage = transform.Find("Lobby Screen/Map Settings Window/Minimap").GetComponent<Image>();

        choosePrevFactionButton = transform.Find("Lobby Screen/Player Settings Window/Faction/Choose Previous Faction Button").GetComponent<Button>();
        choosePrevFactionButton.onClick.AddListener(() => ChooseFaction(-1));
        chooseNextFactionButton = transform.Find("Lobby Screen/Player Settings Window/Faction/Choose Next Faction Button").GetComponent<Button>();
        chooseNextFactionButton.onClick.AddListener(() => ChooseFaction(1));
        chooseFactionNameText = transform.Find("Lobby Screen/Player Settings Window/Faction/Chosen Faction Name").GetComponent<TextMeshProUGUI>();

        choosePrevColor = transform.Find("Lobby Screen/Player Settings Window/Color/Choose Previous Color Button").GetComponent<Button>();
        choosePrevColor.onClick.AddListener(() => ChangePlayerColor(-1));
        chooseNextColor = transform.Find("Lobby Screen/Player Settings Window/Color/Choose Next Color Button").GetComponent<Button>();
        chooseNextColor.onClick.AddListener(() => ChangePlayerColor(1));
        chosenColorBox = transform.Find("Lobby Screen/Player Settings Window/Color/Chosen Color").GetComponent<Image>();

        lobbyScreen = transform.Find("Lobby Screen").gameObject;
        lobbyName = transform.Find("Lobby Screen/Header").GetComponentInChildren<TextMeshProUGUI>();
        playerViewerObject = transform.Find("Lobby Screen/Player Viewer").gameObject;
        foreach (Transform t in playerViewerObject.transform)
            Destroy(t.gameObject);
        startGameButton = transform.Find("Lobby Screen/Start Game Button").GetComponent<Button>();
        startGameButton.onClick.AddListener(() => ReadyOrStartGame());

        shipInfoScreen = transform.Find("Ship Info Screen").gameObject;
        exitShipInfoScreenButton = transform.Find("Ship Info Screen/Back Button").GetComponent<Button>();
        exitShipInfoScreenButton.onClick.AddListener(() => ChangeScreen(ScreenNames.LobbyListScreen));
        factionNameText = transform.Find("Ship Info Screen/Faction Panel/Faction Name Text").GetComponentInChildren<TextMeshProUGUI>();
        factionDescriptionText = transform.Find("Ship Info Screen/Faction Panel/Faction Description Text").GetComponentInChildren<TextMeshProUGUI>();
        prevFactionButton = transform.Find("Ship Info Screen/Faction Panel/Previous Faction Button").GetComponent<Button>();
        prevFactionButton.onClick.AddListener(() => SetupShipListPanel(-1));
        nextFactionButton = transform.Find("Ship Info Screen/Faction Panel/Next Faction Button").GetComponent<Button>();
        nextFactionButton.onClick.AddListener(() => SetupShipListPanel(1));
        shipList = transform.Find("Ship Info Screen/Ships Panel/Ship List").gameObject;

        shipDetailsPanel = transform.Find("Ship Info Screen/Ship Details Panel").gameObject;
        shipNameText = shipDetailsPanel.transform.Find("Ship Name").GetComponent<TextMeshProUGUI>();
        shipClassTransform = shipDetailsPanel.transform.Find("Ship Class").GetComponent<RectTransform>();
        shipClassText = shipDetailsPanel.transform.Find("Ship Class/Ship Class Text").GetComponent<TextMeshProUGUI>();
        shipClassIconImage = shipDetailsPanel.transform.Find("Ship Class/Ship Class Icon").GetComponent<Image>();
        shipImage = shipDetailsPanel.transform.Find("Ship Image").GetComponent<Image>();
        shipHealthText = shipDetailsPanel.transform.Find("Ship Stats Panel/Health Text").GetComponent<TextMeshProUGUI>();
        shipTopSpeedText = shipDetailsPanel.transform.Find("Ship Stats Panel/Top Speed Text").GetComponent<TextMeshProUGUI>();
        shipAccelerationText = shipDetailsPanel.transform.Find("Ship Stats Panel/Acceleration Text").GetComponent<TextMeshProUGUI>();
        shipTurningSpeedText = shipDetailsPanel.transform.Find("Ship Stats Panel/Turning Speed Text").GetComponent<TextMeshProUGUI>();
        shipDescriptionText = shipDetailsPanel.transform.Find("Ship Stats Panel/Description Text").GetComponent<TextMeshProUGUI>();
        shipDetailsPanel.SetActive(false);

        turretDetailsPanel = transform.Find("Ship Info Screen/Ship Details Panel/Turret Stats Panel").gameObject;
        turretTypeText = turretDetailsPanel.transform.Find("Turret Type Text").GetComponent<TextMeshProUGUI>();
        turretDmgText = turretDetailsPanel.transform.Find("Damage Text").GetComponent<TextMeshProUGUI>();
        turretFireRateText = turretDetailsPanel.transform.Find("Fire Rate Text").GetComponent<TextMeshProUGUI>();
        turretProjectileSpeedText = turretDetailsPanel.transform.Find("Projectile Speed Text").GetComponent<TextMeshProUGUI>();
        turretRangeText = turretDetailsPanel.transform.Find("Range Text").GetComponent<TextMeshProUGUI>();
        turretLine_Left = shipImage.transform.Find("Turret Line - Left");
        turretLine_Right = shipImage.transform.Find("Turret Line - Right");
        turretCircle = shipImage.transform.Find("Turret Circle");
        SetTurretLinesVisibility(false);

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
        shipInfoScreen.SetActive(currentScreen == ScreenNames.ShipInfoScreen);

        // Does additional code if needed
        switch (currentScreen)
        {
            case ScreenNames.LobbyListScreen:
                lobbyRefreshTimer = 0f;
                currentLobby = null;
                usernameText.text = playerName;
                return;
            case ScreenNames.LobbyScreen:
                //currentChosenFactionIndex = 0; //Resets chosen faction to default everytime you load this screen. This way when you hop in and out of lobbies will reset the same way in and out of game will reset.
                ChooseFaction(0);
                return;
            case ScreenNames.ShipInfoScreen:
                //currentViewedFactionIndex = 0; Right now will reopen with the same faction it was last closed with, if we want it reset back to default faction everytime uncomment this
                SetupShipListPanel(0);
                shipDetailsPanel.SetActive(false);
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
        mapName.text = gameSettingsManager.GetSelectedMapPrefab().name.ToString();
        mapImage.sprite = gameSettingsManager.GetSelectedMapImage();
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
    
    public void ChangePlayerColor(int colorChangeAmount)
    {
        PlayerData player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>();
        int colorValue = player.playerColorIndex.Value;

        List<int> colors = new List<int>();
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            colors.Add(client.PlayerObject.GetComponent<PlayerData>().playerColorIndex.Value);
        }

        while (true)
        {
            if (colors.Contains(colorValue))
            {
                colorValue += colorChangeAmount;
                if (colorValue >= playerColors.Length)
                    colorValue = 0;
                if (colorValue < 0)
                    colorValue = playerColors.Length - 1;
            }
            else
                 break;
        }

        player.playerColorIndex.Value = colorValue;
        player.playerReady.Value = false;

        chosenColorBox.color = playerColors[colorValue];
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

    private void SetupShipListPanel(int changeFaction)
    {
        //Change factions to view if applicable
        currentViewedFactionIndex += changeFaction;
        if (currentViewedFactionIndex < 0)
            currentViewedFactionIndex = Factions.Singleton.factionList.Count - 1;
        else if (currentViewedFactionIndex >= Factions.Singleton.factionList.Count)
            currentViewedFactionIndex = 0;

        Faction factionToSetup = Factions.Singleton.factionList[currentViewedFactionIndex];
        factionNameText.text = factionToSetup.factionName;
        factionDescriptionText.text = factionToSetup.factionDescription;

        //Clear out the ships panel of old ship repeaters
        foreach (Transform shipRepeater in shipList.transform)
            Destroy(shipRepeater.gameObject);

        //Hide selected ship details
        shipDetailsPanel.SetActive(false);

        //Populate the ships list
        foreach (NetworkPrefab prefab in factionToSetup.factionShipList.PrefabList)
        {
            GameObject shipRepeater = Instantiate(shipRepeaterPrefab, shipList.transform);
            shipRepeater.GetComponent<ShipRepeater>().Setup(prefab);
        }
    }

    public void SelectedShipChange(NetworkPrefab selectedShipPrefab)
    {
        //Set the background colors for all the other repeaters to unselected
        foreach (Transform shipRepeater in shipList.transform)
            shipRepeater.GetComponent<ShipRepeater>().Unselect();

        //Setup ship details panel
        shipDetailsPanel.SetActive(true);
        turretDetailsPanel.SetActive(false);
        SetTurretLinesVisibility(false);
        Ship selectedShipScript = selectedShipPrefab.Prefab.GetComponent<Ship>();
        Movement selectedShipMovementScript = selectedShipPrefab.Prefab.GetComponent<Movement>();
        shipNameText.text = selectedShipPrefab.Prefab.name;
        shipClassText.text = selectedShipScript.shipClass.ToString();
        shipClassIconImage.sprite = Resources.Load<Sprite>("Sprites/Ship Class Icons/Icon_" + selectedShipScript.shipClass.ToString());
        shipImage.sprite = selectedShipPrefab.Prefab.transform.Find("Ship Sprite").GetComponent<SpriteRenderer>().sprite;
        shipHealthText.text = "Health: " + selectedShipScript.maxShipHP;
        if (selectedShipMovementScript != null)
        {
            shipTopSpeedText.text = "Top Speed: " + selectedShipMovementScript.GetMaxSpeed();
            shipAccelerationText.text = "Acceleration: " + selectedShipMovementScript.GetAcceleration();
            shipTurningSpeedText.text = "Turning Speed: " + (selectedShipMovementScript.GetRotationSpeed() > 999 ? "\u221E" : selectedShipMovementScript.GetRotationSpeed()); //Infinity sign if high rotation speed
        }
        else
        {
            shipTopSpeedText.text = "Top Speed: N/A";
            shipAccelerationText.text = "Acceleration: N/A";
            shipTurningSpeedText.text = "Turning Speed: N/A";
        }
        shipDescriptionText.text = selectedShipScript.shipDescriptionForUI;

        //Clear out previous turret indicators
        foreach (Transform turretRepeater in shipImage.transform)
            if(turretRepeater.GetComponent<TurretRepeater>() != null)
                Destroy(turretRepeater.gameObject);

        //Setup new turret indicators
        foreach (Transform turret in selectedShipPrefab.Prefab.transform.Find("Ship Sprite/Turrets"))
        {
            GameObject turretRepeater = Instantiate(turretRepeaterPrefab, shipImage.transform);
            turretRepeater.GetComponent<TurretRepeater>().Setup(turret);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(shipClassTransform); //This realigns the icon and text to fit correctly for ship class
    }

    public void SelectedTurretChange(TurretRepeater selectedTurretRepeater)
    {
        //Show and update turret panel
        turretDetailsPanel.SetActive(true);
        Turret turretScript = selectedTurretRepeater.turretTransform.GetComponent<Turret>();

        turretTypeText.text = "Turret Type: ";
        turretDmgText.text = "Dmg / Shot: " + turretScript.GetDamage();
        turretFireRateText.text = "Shots / Sec: " + turretScript.GetShotsPerSec();
        turretProjectileSpeedText.text = "Projectile Speed: " + turretScript.GetProjectileSpeed();
        turretRangeText.text = "Range: " + turretScript.GetRange();

        switch (turretScript.GetTurretType())
        {
            case Turret.TurretType.LightTurret:
                turretTypeText.text += "Light";
                break;
            case Turret.TurretType.MediumTurret:
                turretTypeText.text += "Medium";
                break;
            case Turret.TurretType.HeavyTurret:
                turretTypeText.text += "Heavy";
                break;
            case Turret.TurretType.HawkGun:
                turretTypeText.text += "Heavy";
                turretRangeText.text += " | " + (turretScript.GetRange() * 2);
                break;
            case Turret.TurretType.MissilePods:
                turretTypeText.text += "Missile";
                break;

        }

        //Put the turret lines to the right spot
        SetTurretLinesVisibility(true);
        turretLine_Left.position = selectedTurretRepeater.transform.position;
        turretLine_Right.position = selectedTurretRepeater.transform.position;
        turretLine_Left.rotation = Quaternion.Euler(0f, 0f, turretScript.GetAimDirection() - 90f + turretScript.GetFiringArc() / 2f);
        turretLine_Right.rotation = Quaternion.Euler(0f, 0f, turretScript.GetAimDirection() - 90f - turretScript.GetFiringArc() / 2f);

        //If it is a 360 turret show the circle, and rotate the lines to show direction of fire for missiles
        if (turretScript.GetFiringArc() == 360)
        {
            if (turretScript.GetTurretType() == Turret.TurretType.MissilePods)
            {
                turretLine_Left.rotation = Quaternion.Euler(0f, 0f, turretScript.GetAimDirection() - 90f);
                turretLine_Right.rotation = Quaternion.Euler(0f, 0f, turretScript.GetAimDirection() - 90f);
            }
            else
            {
                SetTurretLinesVisibility(false);
            }
            turretCircle.gameObject.SetActive(true);
            turretCircle.position = selectedTurretRepeater.transform.position;
        }
    }

    private void SetTurretLinesVisibility(bool visible)
    {
        turretLine_Left.gameObject.SetActive(visible);
        turretLine_Right.gameObject.SetActive(visible);
        turretCircle.gameObject.SetActive(false);
    }

    private void ChooseFaction(int changeFaction)
    {
        //Change faction selection
        currentChosenFactionIndex += changeFaction;
        if (currentChosenFactionIndex < 0)
            currentChosenFactionIndex = Factions.Singleton.factionList.Count - 1;
        else if (currentChosenFactionIndex >= Factions.Singleton.factionList.Count)
            currentChosenFactionIndex = 0;

        chooseFactionNameText.text = Factions.Singleton.factionList[currentChosenFactionIndex].factionName;
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().playerFactionIndex.Value = currentChosenFactionIndex;
    }

    public int GetSelectedFactionIndex()
    {
        return currentChosenFactionIndex;
    }
}
