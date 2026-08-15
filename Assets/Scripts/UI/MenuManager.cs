using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private LobbyManager lobbyManager;

    private GameObject firstScreen;
    private Button mainPlayButton;
    //TODO: Change Name button

    private GameObject lobbyListScreen;
    private Button lobbyListBackButton;
    private GameObject lobbyViewerObject;
    [SerializeField] private GameObject lobbyRepeaterPrefab;
    private Button createLobbyButton;

    private GameObject lobbyScreen;
    private TextMeshProUGUI lobbyName;
    private GameObject playerViewerObject;
    [SerializeField] private GameObject playerRepeaterPrefab;
    private Button startGameButton;

    private enum screenNames { FirstScreen, LobbyListScreen, LobbyScreen };
    private screenNames currentScreen;

    private Lobby currentLobby;

    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);

        lobbyManager = GetComponent<LobbyManager>();

        firstScreen = transform.Find("First Screen").gameObject;
        mainPlayButton = firstScreen.GetComponentInChildren<Button>();
        mainPlayButton.onClick.AddListener(() => ChangeScreen(screenNames.LobbyListScreen));

        lobbyListScreen = transform.Find("Lobby List Screen").gameObject;
        lobbyListBackButton = lobbyListScreen.transform.Find("Header").Find("Lobby List Back Button").GetComponent<Button>();
        lobbyListBackButton.onClick.AddListener(() => ChangeScreen(screenNames.FirstScreen));
        lobbyViewerObject = transform.Find("Lobby List Screen").Find("Lobby Viewer").gameObject;
        createLobbyButton = transform.Find("Lobby List Screen").Find("Create Lobby Button").GetComponent<Button>();
        createLobbyButton.onClick.AddListener(() => CreateLobby());

        lobbyScreen = transform.Find("Lobby Screen").gameObject;
        lobbyName = transform.Find("Lobby Screen").transform.Find("Header").GetComponentInChildren<TextMeshProUGUI>();
        playerViewerObject = transform.Find("Lobby Screen").Find("Player Viewer").gameObject;
        startGameButton = transform.Find("Lobby Screen").Find("Start Game Button").GetComponent<Button>();
        startGameButton.onClick.AddListener(() => StartGame());

        ChangeScreen(screenNames.FirstScreen);
    }

    private readonly float lobbyRefreshTimeMax = 3f;
    private float lobbyRefreshTimer = 0f;
    void FixedUpdate()
    {
        lobbyRefreshTimer -= Time.deltaTime;

        switch (currentScreen)
        {
            case screenNames.FirstScreen:
                return;
            case screenNames.LobbyListScreen:
                if (lobbyRefreshTimer < 0f)
                {
                    RefreshLobbyList();
                }
                return;
            case screenNames.LobbyScreen:
                if (lobbyRefreshTimer < 0f)
                {
                    RefreshLobbyInfo();
                }
                return;
        }
    }

    private void ChangeScreen(screenNames newScreen)
    {
        currentScreen = newScreen;

        firstScreen.SetActive(currentScreen == screenNames.FirstScreen);
        lobbyListScreen.SetActive(currentScreen == screenNames.LobbyListScreen);
        lobbyScreen.SetActive(currentScreen == screenNames.LobbyScreen);

        switch (currentScreen)
        {
            case screenNames.FirstScreen:
                return;
            case screenNames.LobbyListScreen:
                RefreshLobbyList();
                return;
            case screenNames.LobbyScreen:
                RefreshLobbyInfo();
                return;
        }
    }

    private async void CreateLobby()
    {
        currentLobby = await lobbyManager.CreateLobby();
        ChangeScreen(screenNames.LobbyScreen);
    }

    public async void JoinLobby(string lobbyId)
    {
        currentLobby = await lobbyManager.JoinLobby(lobbyId);
        ChangeScreen(screenNames.LobbyScreen);
    }


    private async void RefreshLobbyList()
    {
        lobbyRefreshTimer = lobbyRefreshTimeMax;

        QueryResponse lobbyList = await lobbyManager.RefreshLobbyList();
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

        currentLobby = await lobbyManager.PollLobbyForUpdates();

        bool inLobby = false;
        if (currentLobby != null)
            foreach (Player player in currentLobby.Players)
                if (player.Id == AuthenticationService.Instance.PlayerId) inLobby = true;
        if (!inLobby)
        {
            currentLobby = null;
            ChangeScreen(screenNames.LobbyListScreen);
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

    private async void StartGame()
    {
        string joinCode = await lobbyManager.CreateRelay();
        await lobbyManager.SetLobbyRelayCode(joinCode);

        Debug.Log("Relay created: " + joinCode);
        //TODO: start game
    }
    private async void JoinGame(string joinCode)
    {
        await lobbyManager.JoinRelay(joinCode);
        Debug.Log("Relay joined: " + joinCode);
        //TODO: start game
    }

    public async void RemovePlayerFromLobby(string playerId)
    {
        RefreshLobbyInfo();
        if (playerId == currentLobby.HostId && currentLobby.Players.Count > 1) await lobbyManager.MigrateLobbyHost();
        if (currentLobby.Players.Count == 1)
            currentLobby = null;
        await lobbyManager.RemovePlayer(playerId);
        if (playerId == AuthenticationService.Instance.PlayerId)
            ChangeScreen(screenNames.LobbyListScreen);
    }
 }
