using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class NeutralPatrol : NetworkBehaviour
{
    [SerializeField] List<Transform> patrolSpawnPositions;
    [SerializeField] List<GameObject> patrolShips;
    [SerializeField] public int goldOnKill = 30;

    private List<GameObject> spawnedShipsList;
    private Transform spawn;

    
    void Awake()
    {
        foreach (Transform spawnPos in patrolSpawnPositions)
        {
            spawnPos.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    public void CreatePatrolGroup(Transform spawnPos)
    {
        if (patrolSpawnPositions.Count != patrolShips.Count)
        {
            Debug.LogError("Not spawning patrol, uneven number of ships and spawn positions in prefab");
            return;
        }

        spawn = spawnPos;
        spawnedShipsList = new List<GameObject>();
        for (int i = 0; i < patrolShips.Count; i++)
        {
            GameObject spawnedShip = Instantiate(patrolShips[i], patrolSpawnPositions[i].position, Quaternion.identity);
            spawnedShip.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            spawnedShip.transform.parent = transform;

            spawnedShip.GetComponent<NeutralShip>().SetupShipSpawn(spawn, this);
            GameSceneManager.Singleton.shipsInScene.Add(spawnedShip);

            spawnedShipsList.Add(spawnedShip);
        }
    }

    public Vector2 GetDestinationBasedOnGroup(GameObject ship, Vector2 baseDestionationPoint)
    {
        if (spawnedShipsList.Count == 1)
        {
            return baseDestionationPoint;
        }
        else
        {
            float xMax = spawnedShipsList[0].transform.position.x;
            float yMax = spawnedShipsList[0].transform.position.y;
            float xMin = spawnedShipsList[0].transform.position.x;
            float yMin = spawnedShipsList[0].transform.position.y;

            foreach (GameObject patrolShip in spawnedShipsList)
            {
                if (patrolShip.transform.position.x > xMax) { xMax = patrolShip.transform.position.x; }
                if (patrolShip.transform.position.x < xMin) { xMin = patrolShip.transform.position.x; }
                if (patrolShip.transform.position.y > yMax) { yMax = patrolShip.transform.position.y; }
                if (patrolShip.transform.position.y < yMin) { yMin = patrolShip.transform.position.y; }
            }

            float xDiff = xMax - xMin;
            float yDiff = yMax - yMin;

            Vector2 patrolGroupCenter = new Vector2(xMin + (xDiff / 2), yMin + (yDiff / 2));

            return baseDestionationPoint + ((Vector2)ship.transform.position - patrolGroupCenter);
        }
    }


    public float GetGroupSpeed()
    {
        float lowestMaxSpeed = float.MaxValue;
        foreach (GameObject patrolShip in spawnedShipsList)
            if (patrolShip.gameObject.GetComponent<Movement>().GetMaxSpeed() < lowestMaxSpeed)
                lowestMaxSpeed = patrolShip.gameObject.GetComponent<Movement>().GetMaxSpeed();
        return lowestMaxSpeed;
    }

    public bool IsLastPatrolShipToDie(GameObject shipThatDied)
    {
        spawnedShipsList.Remove(shipThatDied);
        if (spawnedShipsList.Count > 0)
        {
            return false;
        }
        else
        {
            StartCoroutine(CleanUpPatrolObject());
            return true;
        }
    }

    private IEnumerator CleanUpPatrolObject()
    {
        GameSceneManager.Singleton.neutralObjectivesManager.NeutralPatrolDestroyed(spawn);
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}
