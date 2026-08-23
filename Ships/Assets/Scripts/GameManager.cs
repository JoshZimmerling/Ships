using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public NetworkPrefabsList shipList;

    // TODO: Get rid of these
    public Transform bulletContainer;
    public List<GameObject> playerSpawns = new List<GameObject>();

    public void Start()
    {
        foreach (var (id, player) in PlayerDataList.Singleton.players)
            player.PlayerSetup();
    }

    public GameObject GetShipPrefab(int shipNum)
    {
        return shipList.PrefabList[shipNum].Prefab;
    }

    // TODO: make this better
    [SerializeField] private GameObject map;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject inputManager;

    // GameState code
    public static event Action<GameState> OnBeforeStateChange;
    public static event Action<GameState> OnAfterStateChange;
    
    public GameState State { get; private set; }
    //private void Start() => ChangeState(GameState.Gameplay);

    public void ChangeState(GameState newState)
    {
        if (State == newState) return;

        OnBeforeStateChange?.Invoke(newState);

        State = newState;
        switch (newState)
        {
            case GameState.Starting:
                break;
            case GameState.Menu:
                break;
            case GameState.Gameplay:
                map.SetActive(true);
                gameUI.SetActive(true);
                inputManager.SetActive(true);
                Shop.Singleton.SetupShop();
                Shop.Singleton.ToggleShop(); //Opens the shop by default
                break;
            case GameState.Gameover:
                break;
            default:
                Debug.LogError("Invalid GameState");
                break;
        }

        OnAfterStateChange?.Invoke(newState);
    }
}

[Serializable]
public enum GameState
{
   Starting = 0,
   Menu = 1,
   Gameplay = 2,
   Gameover = 3
}

