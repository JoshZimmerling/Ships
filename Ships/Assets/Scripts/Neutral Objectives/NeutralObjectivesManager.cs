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
                spawnedShip.GetComponent<NeutralShip>().SetupShipSpawn(spawnPos);
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
}
