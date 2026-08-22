using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRepeater : MonoBehaviour
{
    public MenuManager menuManager;

    private Button changeColorButton;
    private TextMeshProUGUI playerName;
    private GameObject hostIcon;
    private Button leaveLobbyButton;
    private Image leaveKickLobbyImage;
    private Image backgroundColor;

    [SerializeField] private Sprite kickIcon;
    [SerializeField] private Sprite leaveIcon;

    private string playerId;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        changeColorButton = transform.Find("Change Color Button").GetComponent<Button>();
        changeColorButton.onClick.AddListener(() => menuManager.ChangePlayerColor(playerId));
        playerName = transform.Find("Name Bar").Find("Player Name").GetComponent<TextMeshProUGUI>();
        hostIcon = transform.Find("Name Bar").Find("Host Icon").gameObject;
        leaveLobbyButton = transform.Find("Leave Button").GetComponent<Button>();
        leaveLobbyButton.onClick.AddListener(() => menuManager.RemovePlayerFromLobby(playerId));
        leaveKickLobbyImage = transform.Find("Leave Button").GetComponent<Image>();
        backgroundColor = gameObject.GetComponent<Image>();
    }

    public void UpdatePlayerDetails(Player player, string hostId)
    {
        playerId = player.Id;

        playerName.text = player.Data["PlayerName"].Value;
        hostIcon.SetActive(playerId == hostId);

        if (playerId == AuthenticationService.Instance.PlayerId) // Looking at self
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

            if (AuthenticationService.Instance.PlayerId == hostId) // If I am host
            {
                leaveKickLobbyImage.gameObject.SetActive(true);
                leaveKickLobbyImage.sprite = kickIcon;
            }
            else // if not host and not you
                leaveKickLobbyImage.gameObject.SetActive(false);
        }

        foreach (var (id, playerData) in PlayerDataList.Singleton.players)
            if (playerData.authenticationServicePlayerId.Value == playerId)
                backgroundColor.color = menuManager.playerColors[playerData.playerColorIndex.Value];
    }
}
