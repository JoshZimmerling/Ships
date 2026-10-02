using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using static MenuScreenManager;

public class PlayerRepeater : MonoBehaviour
{
    private Button changeColorButton;
    private TextMeshProUGUI playerName;
    private GameObject hostIcon;
    private GameObject readyIcon;
    private Button leaveLobbyButton;
    private Image leaveKickLobbyImage;
    private Image backgroundColor;

    [SerializeField] private Sprite kickIcon;
    [SerializeField] private Sprite leaveIcon;

    public NetworkClient client;
    private string playerId;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        MenuScreenManager.Singleton.ChangeScreen(ScreenNames.LobbyScreen);

        changeColorButton = transform.Find("Change Color Button").GetComponent<Button>();
        changeColorButton.onClick.AddListener(() => MenuScreenManager.Singleton.ChangePlayerColor(client.PlayerObject.GetComponent<PlayerData>()));
        playerName = transform.Find("Name Bar").Find("Player Name").GetComponent<TextMeshProUGUI>();
        hostIcon = transform.Find("Name Bar").Find("Host Icon").gameObject;
        readyIcon = transform.Find("Name Bar").Find("Ready Icon").gameObject;
        leaveLobbyButton = transform.Find("Leave Button").GetComponent<Button>();
        leaveLobbyButton.onClick.AddListener(() => RemovePlayerFromLobby(client));
        leaveKickLobbyImage = transform.Find("Leave Button").GetComponent<Image>();
        backgroundColor = gameObject.GetComponent<Image>();
    }

    private void FixedUpdate()
    {   
        // Destroys lobby repeaters when client is no longer in lobby
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
        {
            try
            {
                LobbyService.Instance.DeleteLobbyAsync(MenuScreenManager.Singleton.currentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
            Destroy(gameObject);
            MenuScreenManager.Singleton.ChangeScreen(MenuScreenManager.ScreenNames.LobbyListScreen);
        }
        else if (client.PlayerObject == null)
        {
            try
            {
                LobbyService.Instance.RemovePlayerAsync(MenuScreenManager.Singleton.currentLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
            Destroy(gameObject);
        }
        else
        {
            PlayerData playerData = client.PlayerObject.GetComponent<PlayerData>();
            playerId = playerData.authenticationServicePlayerId.Value.ToString();
            playerName.text = playerData.playerUsername.Value.ToString();
            if (playerData.playerColorIndex.Value != -1)
            {
                backgroundColor.color = playerData.playerColor;
            }

            hostIcon.SetActive(playerData.OwnerClientId == NetworkManager.ServerClientId);
            readyIcon.SetActive(playerData.playerReady.Value && playerData.OwnerClientId != NetworkManager.ServerClientId);

            if (playerData.IsLocalPlayer)
            {
                changeColorButton.gameObject.SetActive(true);
                playerName.color = new Color(215 / 255f, 215 / 255f, 100 / 255f);

                leaveKickLobbyImage.gameObject.SetActive(true);
                leaveKickLobbyImage.sprite = leaveIcon;
            }
            else // looking at other people
            {
                changeColorButton.gameObject.SetActive(false);
                playerName.color = new Color(1f, 1f, 1f);

                if (NetworkManager.Singleton.IsHost) // If I am host
                {
                    leaveKickLobbyImage.gameObject.SetActive(true);
                    leaveKickLobbyImage.sprite = kickIcon;
                }
                else // if not host and not you
                    leaveKickLobbyImage.gameObject.SetActive(false);
            }
        }
    }

    public void RemovePlayerFromLobby(NetworkClient client)
    {
        // Closes network connection
        if (client.PlayerObject.IsLocalPlayer)
        {
            // Local player leaving
            NetworkManager.Singleton.Shutdown();
        }
        else
        {
            // Remote player leaving
            NetworkManager.Singleton.DisconnectClient(client.ClientId);
        }

    }
}
