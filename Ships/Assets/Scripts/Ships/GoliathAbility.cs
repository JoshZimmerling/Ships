using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GoliathAbility : NetworkBehaviour
{
    [SerializeField] private GameObject fighterPrefab;

    [SerializeField] private int fightersOnSpawn;
    [SerializeField] private int maxFighters;
    [SerializeField] private float spawnCooldown;
    public List<GameObject> fighters;

    [SerializeField] private int detectionRange;
    [SerializeField] private int maxChaseRange;
    private GameObject currentTarget;

    private float abilityTimer = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsHost) return;

        fighters = new List<GameObject>();
        for (int i = 0; i < fightersOnSpawn; i++)
            SpawnFighter();

        abilityTimer = spawnCooldown;
    }

    public void FixedUpdate()
    {
        if (!IsHost) return;
        abilityTimer -= Time.deltaTime;
        
        if (abilityTimer < 0 && fighters.Count < maxFighters)
        {
            SpawnFighter();
            abilityTimer = spawnCooldown;
        }

        if (currentTarget == null)
        {
            // Find a target
            float closestRange = Mathf.Infinity;
            foreach (GameObject shipInScene in GameSceneManager.Singleton.shipsInScene)
            {
                if (shipInScene.GetComponent<Fighter>())
                    continue;
                if (shipInScene.GetComponent<Ship>() != null && shipInScene.GetComponent<Ship>().OwnerClientId != OwnerClientId)
                {
                    float dist2Ship = (shipInScene.transform.position - transform.position).magnitude - shipInScene.GetComponent<Ship>().correctionFactor;
                    if (dist2Ship < detectionRange && dist2Ship < closestRange)
                    {
                        closestRange = dist2Ship;
                        currentTarget = shipInScene;
                    }
                }
                else if (shipInScene.GetComponent<NeutralShip>() != null)
                {
                    float dist2Ship = (shipInScene.transform.position - transform.position).magnitude - shipInScene.GetComponent<NeutralShip>().correctionFactor;
                    if (dist2Ship < detectionRange && dist2Ship < closestRange)
                    {
                        closestRange = dist2Ship;
                        currentTarget = shipInScene;
                    }
                }
            }
            // Send drones after target
            if (currentTarget != null)
                foreach (GameObject fighter in fighters)
                    fighter.GetComponent<Fighter>().SetTarget(currentTarget);
        }
        else if ((currentTarget.GetComponent<Ship>() != null && (currentTarget.transform.position - transform.position).magnitude - currentTarget.GetComponent<Ship>().correctionFactor > maxChaseRange)
            || (currentTarget.GetComponent<NeutralShip>() != null && (currentTarget.transform.position - transform.position).magnitude - currentTarget.GetComponent<NeutralShip>().correctionFactor > maxChaseRange))
        {
            // Tell ships to return
            currentTarget = null;
            foreach(GameObject fighter in fighters)
                fighter.GetComponent<Fighter>().SetTarget(gameObject);
        }
    }

    private void SpawnFighter()
    {
        Vector3 spawnPos;
        Vector2 offset = Random.onUnitCircle * 5;
        spawnPos = transform.position + new Vector3(offset.x, offset.y);
        GameObject ship = Instantiate(fighterPrefab, spawnPos, Quaternion.LookRotation(new Vector3(0, 0, 1), -spawnPos));

        ship.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        ship.transform.parent = PlayerDataList.Singleton.GetLocalPlayer().transform;

        GameSceneManager.Singleton.shipsInScene.Add(ship);
        fighters.Add(ship);

        ship.GetComponent<Fighter>().SetupFighter(gameObject);
        if(currentTarget != null)
            ship.GetComponent<Fighter>().SetTarget(currentTarget);
    }
}
