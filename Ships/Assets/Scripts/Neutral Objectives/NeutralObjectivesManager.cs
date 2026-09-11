using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NeutralObjectivesManager : NetworkBehaviour
{
    [SerializeField] GameObject neutralShipPrefab;
    [SerializeField] float secondsBetweenNeutralShipSpawns;
    [SerializeField] float secondsUntilFirstNeutralShipSpawns;
    private float currentShipSpawningTimer;

    private List<Transform> spawnPositions;
    public List<bool> spawnHasShip;
    [SerializeField] int maxNeutralShips = 2;

    void Start()
    {
        currentShipSpawningTimer = secondsUntilFirstNeutralShipSpawns;

        spawnPositions = new List<Transform>();
        spawnHasShip = new List<bool>();

        foreach (Transform spawnLocation in GameObject.Find("Neutral Ship Spawn Locations").transform)
        {
            spawnPositions.Add(spawnLocation);
            spawnHasShip.Add(false);
        }
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        currentShipSpawningTimer -= Time.deltaTime;
        if (currentShipSpawningTimer < 0)
        {
            // Counts ships in scene
            int c = 0;
            foreach (bool b in spawnHasShip)
                if (b) c++;
            // Checks if more ships are needed
            if (c < maxNeutralShips)
            {

                // Checks for unused spawn
                Transform spawnPos = null;
                int r = 0;
                while (spawnPos == null)
                {
                    r = Random.Range(0, spawnPositions.Count);
                    if (!spawnHasShip[r])
                        spawnPos = spawnPositions[r];
                }
                // Spawns ship
                GameObject spawnedShip = Instantiate(neutralShipPrefab, spawnPos.position, Quaternion.identity);
                spawnedShip.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
                spawnedShip.transform.parent = transform;

                spawnedShip.GetComponent<NeutralShip>().SetupShipSpawn(spawnPos, this, r);
                GameSceneManager.Singleton.shipsInScene.Add(spawnedShip);

                currentShipSpawningTimer = secondsBetweenNeutralShipSpawns;
            }
        }
    }
}
