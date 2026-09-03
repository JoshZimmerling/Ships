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

    void Start()
    {
        currentShipSpawningTimer = secondsUntilFirstNeutralShipSpawns;

        spawnPositions = new List<Transform>();
        foreach (Transform spawnLocation in GameObject.Find("Neutral Ship Spawn Locations").transform)
            spawnPositions.Add(spawnLocation);
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        currentShipSpawningTimer -= Time.deltaTime;
        if (currentShipSpawningTimer < 0)
        {
            Transform spawnPos = spawnPositions[Random.Range(0, spawnPositions.Count)];
            GameObject spawnedShip = Instantiate(neutralShipPrefab, spawnPos.position, Quaternion.identity);
            spawnedShip.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            spawnedShip.transform.parent = transform;

            spawnedShip.GetComponent<NeutralShip>().SetupShipSpawn(spawnPos);
            GameSceneManager.Singleton.shipsInScene.Add(spawnedShip);

            currentShipSpawningTimer = secondsBetweenNeutralShipSpawns;
        }
    }
}
