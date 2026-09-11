using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameSceneManager : Singleton<GameSceneManager>
{
    public NetworkPrefabsList shipList;

    private List<Transform>[] playerSpawns;

    // TODO: Get rid of these
    public Transform bulletContainer;
    public List<GameObject> shipsInScene = new List<GameObject>();
    public List<GameObject> missilesInScene = new List<GameObject>();

    // TODO: make this better
    [SerializeField] public GameObject map;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject inputManager;

    protected override void Awake()
    {
        base.Awake();
        playerSpawns = new List<Transform>[map.transform.Find("Mothership Spawn Positions").childCount];
        for(int i = 0; i < map.transform.Find("Mothership Spawn Positions").childCount; i++)
        {
            playerSpawns[i] = new List<Transform>();
            foreach (Transform spawnLocation in map.transform.Find("Mothership Spawn Positions").GetChild(i))
                playerSpawns[i].Add(spawnLocation);
        }
            
    }

    public void Start()
    {
        foreach (var (id, player) in PlayerDataList.Singleton.players)
            player.PlayerSetup();
    }

    public GameObject GetShipPrefab(int shipNum)
    {
        return shipList.PrefabList[shipNum].Prefab;
    }

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

    public Transform GetOneMothershipSpawnPosition()
    {
        // Shuffles the array
        for (int i = 0; i < playerSpawns.Length; i++)
        {
            List<Transform> temp = playerSpawns[i];
            int r = Random.Range(i, playerSpawns.Length);
            playerSpawns[i] = playerSpawns[r];
            playerSpawns[r] = temp;
        }
        // Finds the first longest list
        int longest = 0;
        int longestIndex = 0;
        for (int i = 0; i < playerSpawns.Length; i++)
            if (playerSpawns[i].Count > longest)
            {
                longest = playerSpawns[i].Count;
                longestIndex = i;
            }
        // Gets the spawn and removes it from the list
        List<Transform> spawnGroup = playerSpawns[longestIndex];
        Transform randomSpawn = spawnGroup[Random.Range(0, spawnGroup.Count)];
        spawnGroup.Remove(randomSpawn);
        return randomSpawn;
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

