using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NeutralObjectivesManager : NetworkBehaviour
{
    [SerializeField] List<GameObject> neutralShipPrefabs;
    [SerializeField] List<int> neutralShipSpawnRates;
    [SerializeField] float secondsUntilFirstNeutralShipSpawns = 10;
    [SerializeField] float secondsBetweenNeutralShipSpawns = 30;
    private float currentShipSpawningTimer;
    private Dictionary<Transform, bool> neutralShipSpawnPositions; //Dictionary for referencing if that spawn position currently has a ship in it
    [SerializeField] int maxNeutralShips = 2;
    private int currentNumOfNeutralShips = 0;

    [SerializeField] GameObject supplyDropPrefab;
    [SerializeField] float secondsUntilFirstSupplyDropSpawns = 30;
    [SerializeField] float secondsBetweenSupplyDropSpawns = 120;
    [SerializeField] float supplyDropLifetime = 60;
    [SerializeField] int supplyDropTotalPayout = 50;
    private float currentSupplyDropSpawningTimer;
    private bool supplyDropCurrentlyOnMap;
    private List<Transform> supplyDropPositions; //Dictionary for referencing if that spawn position currently has a supply drop in it

    [SerializeField] List<GameObject> neutralPatrolPrefabs;
    [SerializeField] List<int> neutralPatrolSpawnRates;
    [SerializeField] float secondsUntilFirstNeutralPatrolSpawns = 15;
    [SerializeField] float secondsBetweenNeutralPatrolSpawns = 60;
    private float currentPatrolSpawningTimer;
    private Dictionary<Transform, bool> neutralPatrolSpawnPositions; //Dictionary for referencing if that spawn position currently has a patrol in it
    [SerializeField] int maxNeutralPatrols = 1;
    private int currentNumOfNeutralPatrols = 0;

    void Start()
    {
        //Setup stuff for neutral ships
        currentShipSpawningTimer = secondsUntilFirstNeutralShipSpawns;

        neutralShipSpawnPositions = new Dictionary<Transform, bool>();
        foreach (Transform spawnLocation in GameObject.Find("Neutral Ship Spawn Locations").transform)
        {
            spawnLocation.GetComponent<SpriteRenderer>().enabled = false;
            neutralShipSpawnPositions.Add(spawnLocation, false);
        }

        if (maxNeutralShips > neutralShipSpawnPositions.Count)
            maxNeutralShips = neutralShipSpawnPositions.Count;

        //Setup stuff for supply drops
        currentSupplyDropSpawningTimer = secondsUntilFirstSupplyDropSpawns;
        supplyDropCurrentlyOnMap = false;

        supplyDropPositions = new List<Transform>();
        foreach (Transform spawnLocation in GameObject.Find("Supply Drop Locations").transform)
        {
            spawnLocation.GetComponent<SpriteRenderer>().enabled = false;
            supplyDropPositions.Add(spawnLocation);
        }

        //Setup stuff for neutral patrols
        currentPatrolSpawningTimer = secondsUntilFirstNeutralPatrolSpawns;

        neutralPatrolSpawnPositions = new Dictionary<Transform, bool>();
        foreach (Transform spawnLocation in GameObject.Find("Neutral Patrol Spawn Locations").transform)
        {
            spawnLocation.GetComponent<SpriteRenderer>().enabled = false;
            neutralPatrolSpawnPositions.Add(spawnLocation, false);
        }

        if (maxNeutralPatrols > neutralPatrolSpawnPositions.Count)
            maxNeutralPatrols = neutralPatrolSpawnPositions.Count;
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        //Neutral Ship Spawn Check
        currentShipSpawningTimer -= Time.deltaTime;
        if (currentShipSpawningTimer < 0)
        {
            // Checks if more ships are needed
            if (currentNumOfNeutralShips < maxNeutralShips)
            {
                // Checks for unused spawn
                Transform spawnPos = null;
                int r = 0;
                while (spawnPos == null)
                {
                    r = Random.Range(0, neutralShipSpawnPositions.Count);
                    if (!neutralShipSpawnPositions.Values.ElementAt(r))
                        spawnPos = neutralShipSpawnPositions.Keys.ElementAt(r);
                }

                //Selects a random neutral ship type to spawn
                int shipTypeToSpawn = 0;
                int random = Random.Range(0, 100);
                foreach (int spawnRate in neutralShipSpawnRates)
                {
                    if (random > spawnRate)
                    {
                        random -= spawnRate;
                        shipTypeToSpawn++;
                    }
                    else
                    {
                        break;
                    }
                }
                // Spawns ship
                GameObject spawnedShip = Instantiate(neutralShipPrefabs[shipTypeToSpawn], spawnPos.position, Quaternion.identity);
                spawnedShip.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
                spawnedShip.transform.parent = transform;

                neutralShipSpawnPositions[spawnPos] = true;
                spawnedShip.GetComponent<NeutralShip>().SetupShipSpawn(spawnPos, null);
                GameSceneManager.Singleton.shipsInScene.Add(spawnedShip);

                currentNumOfNeutralShips++;
                currentShipSpawningTimer = secondsBetweenNeutralShipSpawns;
            }
        }

        //Supply Drop Spawn Check
        currentSupplyDropSpawningTimer -= Time.deltaTime;
        if (currentSupplyDropSpawningTimer < 0 && !supplyDropCurrentlyOnMap)
        {
            supplyDropCurrentlyOnMap = true;

            Transform spawnLoc = supplyDropPositions[Random.Range(0, supplyDropPositions.Count)];
            GameObject spawnedSupplyDrop = Instantiate(supplyDropPrefab, spawnLoc.position, Quaternion.identity);
            spawnedSupplyDrop.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            spawnedSupplyDrop.GetComponent<SupplyDrop>().Setup(supplyDropLifetime, supplyDropTotalPayout);
        }

        //Neutral Patrol Spawn Check
        currentPatrolSpawningTimer -= Time.deltaTime;
        if (currentPatrolSpawningTimer < 0)
        {
            // Checks if more patrols are needed
            if (currentNumOfNeutralPatrols < maxNeutralPatrols)
            {
                // Checks for unused spawn
                Transform spawnPos = null;
                int r = 0;
                while (spawnPos == null)
                {
                    r = Random.Range(0, neutralPatrolSpawnPositions.Count);
                    if (!neutralPatrolSpawnPositions.Values.ElementAt(r))
                        spawnPos = neutralPatrolSpawnPositions.Keys.ElementAt(r);
                }

                //Selects a random neutral patrol group to spawn
                int patrolGroupToSpawn = 0;
                int random = Random.Range(0, 100);
                foreach (int spawnRate in neutralPatrolSpawnRates)
                {
                    if (random > spawnRate)
                    {
                        random -= spawnRate;
                        patrolGroupToSpawn++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Spawns patrol
                GameObject spawnedPatrol = Instantiate(neutralPatrolPrefabs[patrolGroupToSpawn], spawnPos.position, Quaternion.identity);
                spawnedPatrol.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
                spawnedPatrol.transform.parent = transform;

                neutralPatrolSpawnPositions[spawnPos] = true;
                spawnedPatrol.GetComponent<NeutralPatrol>().CreatePatrolGroup(spawnPos);

                currentNumOfNeutralPatrols++;
                currentPatrolSpawningTimer = secondsBetweenNeutralPatrolSpawns;
            }
        }
    }

    public void NeutralShipDeath(Transform spawn)
    {
        neutralShipSpawnPositions[spawn] = false;
        if (currentNumOfNeutralShips == maxNeutralShips && currentShipSpawningTimer < 5f)
            currentShipSpawningTimer = 5f; //Give a cooldown on respawning if we were previously at the max number of ships
        currentNumOfNeutralShips--;
    }

    public void SupplyDropCompleted()
    {
        currentSupplyDropSpawningTimer = secondsBetweenSupplyDropSpawns;
        supplyDropCurrentlyOnMap = false;
    }

    public void NeutralPatrolDestroyed(Transform spawn)
    {
        neutralPatrolSpawnPositions[spawn] = false;
        if (currentNumOfNeutralPatrols == maxNeutralPatrols && currentPatrolSpawningTimer < secondsBetweenNeutralPatrolSpawns)
            currentPatrolSpawningTimer = secondsBetweenNeutralPatrolSpawns; //Give a cooldown on respawning if we were previously at the max number of patrols
        currentNumOfNeutralPatrols--;
    }
}
