using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerDataList : Singleton<PlayerDataList>
{
    public Dictionary<ulong, PlayerData> players = new();

    public void Start()
    {
        DontDestroyOnLoad(this);
    }

    public PlayerData GetLocalPlayer()
    {
        return players[NetworkManager.Singleton.LocalClientId];
    }
}
