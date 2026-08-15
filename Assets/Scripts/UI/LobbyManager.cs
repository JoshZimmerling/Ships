using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using IngameDebugConsole;
using System.Collections.Generic;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.VisualScripting;
using System.Threading.Tasks;

public class LobbyManager: MonoBehaviour
{
    private Lobby currentLobby;

    private float heartbeatTimer;
    private string playerName;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        playerName = "Player " + Random.Range(100, 999);
        Debug.Log(playerName);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        await RefreshLobbyList();

        //DebugLogConsole.AddCommand("CreateLobby", "", CreateLobby);
        //DebugLogConsole.AddCommand("JoinLobby", "", QuickJoinLobby);
        //DebugLogConsole.AddCommand("ListLobby", "", ListLobby);
    }


    private void Update()
    {
        HandleLobbyHeartbeat();
        //HandleLobbyPollForUpdates();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) 
            {
                float heartbeatTimeMax = 15;
                heartbeatTimer = heartbeatTimeMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    public async Task<Lobby> PollLobbyForUpdates()
    {
        if (currentLobby == null)
            return null;
        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            return currentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        return null;
    }

    public async Task<QueryResponse> RefreshLobbyList()
    {
        try
        {
            /*
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created),
                }
            };
            */

            return await LobbyService.Instance.QueryLobbiesAsync();
            /*
            Debug.Log("Lobbies Found: " + queryResonse.Results.Count);
            foreach (Lobby lobby in queryResonse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
            */
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e); 
        }
        return null;
    }

    public async Task<Lobby> CreateLobby()
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

            //PrintPlayers(hostLobby);

        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
        return currentLobby;
    }

    public async Task<Lobby> JoinLobby(string lobbyId)
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
        return currentLobby;
    }

    /*
    private async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions quickJoinLobbyOptions = new QuickJoinLobbyOptions {
                Player = GetPlayer()
            };
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinLobbyOptions);

            //PrintPlayers(joinedLobby);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    */

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

    public async Task RemovePlayer(string playerId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            if (playerId == AuthenticationService.Instance.PlayerId) currentLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async Task MigrateLobbyHost()
    {
        try
        {
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
            {
                HostId = currentLobby.Players[1].Id
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    /*
    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in Lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value);
        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }
    private async void UpdatePlayerName(string newPlayerName)
    {
        playerName = newPlayerName;
        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
                Data = new Dictionary<string, PlayerDataObject> {
                { "playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) } }
            });

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    
    */

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            //return NetworkManager.Singleton.StartHost() ? joinCode : null
            Debug.Log(joinCode);
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
        return null;
    }
    public async Task SetLobbyRelayCode(string relayCode)
    {
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
    }

    public async Task JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            NetworkManager.Singleton.GetComponent<UnityTransport>().UseWebSockets = true;
            // return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
