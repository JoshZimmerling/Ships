using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NeutralObjectivesManager : NetworkBehaviour
{
    [SerializeField] GameObject neutralShipPrefab;
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
            spawnPositions.Add(spawnLocation, false);
        }
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
                // Spawns ship
                GameObject spawnedShip = Instantiate(neutralShipPrefab, spawnPos.position, Quaternion.identity);
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
        if (currentNumOfNeutralShips == maxNeutralShips)
            currentShipSpawningTimer = 5f; //Give a cooldown on respawning if we were previously at the max number of ships
        currentNumOfNeutralShips--;
    }
}
