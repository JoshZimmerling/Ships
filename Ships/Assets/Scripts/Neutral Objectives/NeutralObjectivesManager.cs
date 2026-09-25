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

    private Dictionary<Transform, bool> spawnPositions; //Dictionary for referencing if that spawn position currently has a ship in it

    [SerializeField] int maxNeutralShips = 2;
    private int currentNumOfNeutralShips = 0;

    void Start()
    {
        currentShipSpawningTimer = secondsUntilFirstNeutralShipSpawns;

        spawnPositions = new Dictionary<Transform, bool>();
        foreach (Transform spawnLocation in GameObject.Find("Neutral Ship Spawn Locations").transform)
        {
            spawnLocation.GetComponent<SpriteRenderer>().enabled = false;
            spawnPositions.Add(spawnLocation, false);
        }

        if (maxNeutralShips > spawnPositions.Count)
            maxNeutralShips = spawnPositions.Count;
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

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
                    r = Random.Range(0, spawnPositions.Count);
                    if (!spawnPositions.Values.ElementAt(r))
                        spawnPos = spawnPositions.Keys.ElementAt(r);
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

                spawnPositions[spawnPos] = true;
                spawnedShip.GetComponent<NeutralShip>().SetupShipSpawn(spawnPos);
                GameSceneManager.Singleton.shipsInScene.Add(spawnedShip);

                currentNumOfNeutralShips++;
                currentShipSpawningTimer = secondsBetweenNeutralShipSpawns;
            }
        }
    }

    public void NeutralShipDeath(Transform spawn)
    {
        spawnPositions[spawn] = false;
        if (currentNumOfNeutralShips == maxNeutralShips && currentShipSpawningTimer < 5f)
            currentShipSpawningTimer = 5f; //Give a cooldown on respawning if we were previously at the max number of ships
        currentNumOfNeutralShips--;
    }
}
