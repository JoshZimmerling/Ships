using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NeutralShip : NetworkBehaviour
{
    private Transform hpBar;
    [SerializeField] protected float maxShipHP;
    protected NetworkVariable<float> currentShipHP = new NetworkVariable<float>();

    private Transform spawn;

    private List<Transform> patrolRouteLocations = new List<Transform>();
    private int currentPatrolTarget = 0;

    [SerializeField] int moveSpeed = 5;
    [SerializeField] int goldOnKill = 10;

    public override void OnNetworkSpawn()
    {
        // Finding ship components
        hpBar = transform.Find("Health Bar/Health");

        // Setting up healthbar
        if (IsHost) currentShipHP.Value = maxShipHP;

        currentShipHP.OnValueChanged += (float previousValue, float newValue) => {
            hpBar.transform.localScale = new Vector3(currentShipHP.Value / maxShipHP, 1, 1);
            hpBar.transform.localPosition = new Vector3((currentShipHP.Value / maxShipHP * 0.5f) - 0.5f, 0, 0);
        };
    }

    void FixedUpdate()
    {
        if (!IsHost || spawn == null) return;

        transform.position = Vector2.MoveTowards(transform.position, patrolRouteLocations[currentPatrolTarget].position, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, patrolRouteLocations[currentPatrolTarget].position) < .05f)
            currentPatrolTarget++;

        if (currentPatrolTarget >= patrolRouteLocations.Count)
            currentPatrolTarget = 0;
    }

    public Vector2 GetFuturePosition(float seconds)
    {
        return Vector2.MoveTowards(transform.position, patrolRouteLocations[currentPatrolTarget].position, moveSpeed * seconds);
    }

    public void SetupShipSpawn(Transform spawnObject)
    {
        spawn = spawnObject;
        transform.position = spawn.position;

        foreach (Transform patrolStop in spawn.Find("Patrol Route"))
            patrolRouteLocations.Add(patrolStop);
    }

    public void DoDamage(float damage, ulong damageDealersClientID)
    {
        currentShipHP.Value -= damage;
        if (currentShipHP.Value <= 0)
            DestroyShipRPC(damageDealersClientID);
    }

    [Rpc(SendTo.Server)]
    public void DestroyShipRPC(ulong damageDealersClientID)
    {
        GameSceneManager.Singleton.shipsInScene.Remove(gameObject);
        ReceiveNeutralObjectivePayoutRPC(damageDealersClientID);

        GetComponent<NetworkObject>().Despawn();
        Destroy(this.gameObject);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ReceiveNeutralObjectivePayoutRPC(ulong damageDealersClientID)
    {
        //If my client ID is the one who killed the neutral ship, gain money
        if (damageDealersClientID == NetworkManager.LocalClientId)
            Shop.Singleton.AddGold(goldOnKill);
    }
}
