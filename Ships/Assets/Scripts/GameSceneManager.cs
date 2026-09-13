using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameSceneManager : Singleton<GameSceneManager>
{
    public NetworkPrefabsList shipList;

    private List<Transform> prioPlayerSpawnZones = new List<Transform>();
    private List<Transform> playerSpawnZones = new List<Transform>();
    private int prioritySpawnGroup;

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

        prioritySpawnGroup = Random.Range(0, 2);
        
        //Populate the spawn zones based on prio grouping
        foreach (Transform prioSpawnZone in map.transform.Find("Mothership Spawn Positions").GetChild(prioritySpawnGroup))
        {
            prioPlayerSpawnZones.Add(prioSpawnZone);
            foreach (Transform spawnLocation in prioSpawnZone)
            {
                spawnLocation.GetComponent<SpriteRenderer>().enabled = false;
            }
        }
        foreach (Transform spawnZone in map.transform.Find("Mothership Spawn Positions").GetChild((prioritySpawnGroup + 1) % 2))
        {
            playerSpawnZones.Add(spawnZone);
            foreach (Transform spawnLocation in spawnZone)
            {
                spawnLocation.GetComponent<SpriteRenderer>().enabled = false;
            }
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
        //Pick a random spawn zone and then remove it from the list
        Transform spawnZone;
        if (prioPlayerSpawnZones.Count > 0)
        {
            spawnZone = prioPlayerSpawnZones[Random.Range(0, prioPlayerSpawnZones.Count)];
            prioPlayerSpawnZones.Remove(spawnZone);
        }
        else
        {
            spawnZone = playerSpawnZones[Random.Range(0, playerSpawnZones.Count)];
            playerSpawnZones.Remove(spawnZone);
        }

        //From your spawn zone select a random spawn location
        Transform spawnLoc = spawnZone.GetChild(Random.Range(0, spawnZone.childCount));
        return spawnLoc;
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

