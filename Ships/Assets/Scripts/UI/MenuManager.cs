using System;
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
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

public class MenuManager : MonoBehaviour
{
    private GameObject usernameScreen;
    private TMP_Text usernameTitleText;
    private Button closeUsernameScreenButton;
    private TMP_InputField usernameTextInput;
    private Button submitUsernameButton;

    private GameObject lobbyListScreen;
    private GameObject lobbyViewerObject;
    [SerializeField] private GameObject lobbyRepeaterPrefab;
    private Button createLobbyButton;

    private GameObject lobbyScreen;
    private TextMeshProUGUI lobbyName;
    private GameObject playerViewerObject;
    [SerializeField] private GameObject playerRepeaterPrefab;
    private Button startGameButton;

    private enum ScreenNames { UsernameScreen, LobbyListScreen, LobbyScreen };
    private ScreenNames currentScreen;

    private Lobby currentLobby;

    private string playerName;

    void Start()
    {
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
        // Verifies player is still in lobby
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
            playerObject.GetComponent<PlayerRepeater>().UpdatePlayerDetails(player, currentLobby.HostId);
        }

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

        lobbyName.text = currentLobby.Name;
        startGameButton.gameObject.SetActive(AuthenticationService.Instance.PlayerId == currentLobby.HostId);

        if (currentLobby.Data["RelayCode"].Value != "")
            JoinGame(currentLobby.Data["RelayCode"].Value);
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
            RefreshLobbyInfo();
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
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)},
                    { "Color",      new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "-1")}
                }
        };
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
        //GameManager.Singleton.ChangeState(GameState.Gameplay);
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
        //TODO: start game

        await SceneManager.LoadSceneAsync("Multiplayer Scene");
        NetworkManager.Singleton.StartClient();
        //GameManager.Singleton.ChangeState(GameState.Gameplay);
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
