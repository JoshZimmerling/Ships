using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRepeater : MonoBehaviour
{
    public MenuManager menuManager;

    private TextMeshProUGUI lobbyName;
    private TextMeshProUGUI lobbyCapacity;
    private Image lobbyConnection;
    private Button lobbyJoinButton;
    private string lobbyJoinCode;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        lobbyName = transform.Find("Lobby Name").GetComponent<TextMeshProUGUI>();
        lobbyCapacity = transform.Find("Player Count").GetComponent<TextMeshProUGUI>();
        lobbyConnection = transform.Find("Connection Strength").GetComponent<Image>();
        lobbyJoinButton = transform.Find("Join Button").GetComponent<Button>();
        lobbyJoinButton.onClick.AddListener(() => JoinLobby());
    }

    public void UpdateLobbyDetails(Lobby lobby)
    {
        if (lobbyName == null) return;
        lobbyName.text = lobby.Name;
        lobbyCapacity.text = lobby.Players.Count + " / " + lobby.MaxPlayers + " players";
        //lobbyConnection = lobby.
        lobbyJoinCode = lobby.Id;
    }

    private void JoinLobby()
    {
        menuManager.JoinLobby(lobbyJoinCode);
    }
}
