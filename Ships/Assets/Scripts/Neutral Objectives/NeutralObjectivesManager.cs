using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private Dictionary<Transform, bool> supplyDropPositions; //Dictionary for referencing if that spawn position currently has a supply drop in it
    [SerializeField] int maxSupplyDrops = 1;
    private int currentNumOfSupplyDrops = 0;

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
        if (SceneManager.GetActiveScene().name != "Multiplayer Scene") gameObject.SetActive(false); // To not run in menu

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

        supplyDropPositions = new Dictionary<Transform, bool>();
        foreach (Transform spawnLocation in GameObject.Find("Supply Drop Locations").transform)
        {
            spawnLocation.GetComponent<SpriteRenderer>().enabled = false;
            supplyDropPositions.Add(spawnLocation, false);
        }

        if (maxSupplyDrops > supplyDropPositions.Count)
            maxSupplyDrops = supplyDropPositions.Count;

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
        if (currentSupplyDropSpawningTimer < 0)
        {
            // Checks if more supply drops are needed
            if (currentNumOfSupplyDrops < maxSupplyDrops)
            {
                // Checks for unused spawn
                Transform spawnPos = null;
                int r = 0;
                while (spawnPos == null)
                {
                    r = Random.Range(0, supplyDropPositions.Count);
                    if (!supplyDropPositions.Values.ElementAt(r))
                        spawnPos = supplyDropPositions.Keys.ElementAt(r);
                }

                GameObject spawnedSupplyDrop = Instantiate(supplyDropPrefab, spawnPos.position, Quaternion.identity);
                spawnedSupplyDrop.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
                spawnedSupplyDrop.transform.parent = transform;

                supplyDropPositions[spawnPos] = true;
                spawnedSupplyDrop.GetComponent<SupplyDrop>().Setup(spawnPos, supplyDropLifetime, supplyDropTotalPayout);

                currentNumOfSupplyDrops++;
                currentSupplyDropSpawningTimer = secondsBetweenSupplyDropSpawns;
            }
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
        if (currentNumOfNeutralShips == maxNeutralShips && currentShipSpawningTimer < (secondsBetweenNeutralShipSpawns / 3f))
            currentShipSpawningTimer = secondsBetweenNeutralShipSpawns / 3f; //Give a cooldown on respawning if we were previously at the max number of ships
        currentNumOfNeutralShips--;
    }

    public void SupplyDropCompleted(Transform spawn)
    {
        supplyDropPositions[spawn] = false;
        if (currentNumOfSupplyDrops == maxSupplyDrops && currentSupplyDropSpawningTimer < (secondsBetweenSupplyDropSpawns / 2f))
            currentSupplyDropSpawningTimer = (secondsBetweenSupplyDropSpawns / 2f); //Give a cooldown on respawning if we were previously at the max number of ships
        currentNumOfSupplyDrops--;
    }

    public bool IsShipVisibleOnSupplyDrop(GameObject ship)
    {
        if (currentNumOfSupplyDrops <= 0)
            return false;

        //Supply drop does not give vision of invis ship
        if (ship.GetComponent<Ship>() != null && ship.GetComponent<Ship>().isInvis)
            return false;

        // Loop through each supply drop location, if it has an active supply drop and that supply drop has vision of the ship return true
        for (int i = 0; i < supplyDropPositions.Count; i++)
        {
            if (supplyDropPositions.Values.ElementAt(i))
                if ((ship.transform.position - supplyDropPositions.Keys.ElementAt(i).position).magnitude < supplyDropPrefab.GetComponent<VisionCone>().visionRange)
                    return true;
        }

        return false;
    }

    public void NeutralPatrolDestroyed(Transform spawn)
    {
        neutralPatrolSpawnPositions[spawn] = false;
        if (currentNumOfNeutralPatrols == maxNeutralPatrols && currentPatrolSpawningTimer < (secondsBetweenNeutralPatrolSpawns / 2f))
            currentPatrolSpawningTimer = secondsBetweenNeutralPatrolSpawns / 2f; //Give a cooldown on respawning if we were previously at the max number of patrols
        currentNumOfNeutralPatrols--;
    }
}
