using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NeutralShip : Ship
{
    private Transform spawn;
    private Movement movement;
    private bool moved = false;
    private List<Transform> patrolRouteLocations = new List<Transform>();
    private int currentPatrolTarget = 0;

    [SerializeField] int goldOnKill = 10;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Finding ship components
        movement = GetComponent<Movement>();
    }

    public new void FixedUpdate()
    {
        base.FixedUpdate();

        if (!IsHost || spawn == null) return;

        if (!movement.moving && !moved)
        {
            currentPatrolTarget++;
            if (currentPatrolTarget >= patrolRouteLocations.Count) currentPatrolTarget = 0;

            movement.SetTargetDestinationRPC((Vector2)patrolRouteLocations[currentPatrolTarget].position);
            moved = true;
        }
        if (movement.moving) moved = false;
    }

    public void SetupShipSpawn(Transform spawnObject)
    {
        spawn = spawnObject;
        transform.position = spawn.position;

        foreach (Transform patrolStop in spawn.Find("Patrol Route"))
            patrolRouteLocations.Add(patrolStop);
    }

    public new void DestroyShip(GameObject shipDamageCameFrom)
    {
        GameSceneManager.Singleton.neutralObjectivesManager.NeutralShipDeath(spawn);
        GameSceneManager.Singleton.shipsInScene.Remove(gameObject);
        ReceiveNeutralObjectivePayoutRPC(shipDamageCameFrom.GetComponent<Ship>().OwnerClientId);
        InformShipWhoKilled(shipDamageCameFrom);

        GetComponent<NetworkObject>().Despawn();
        Destroy(this.gameObject);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ReceiveNeutralObjectivePayoutRPC(ulong damageDealersClientID)
    {
        //If my client ID is the one who killed the neutral ship, gain money
        if (damageDealersClientID == NetworkManager.LocalClientId)
        {
            PopupText popupText = Instantiate(popupTextPrefab, transform.position, Quaternion.identity).GetComponent<PopupText>();
            popupText.SetupText("+ $" + goldOnKill, Color.gold, 2.5f);
            Shop.Singleton.AddGold(goldOnKill);
        }
    }
}
