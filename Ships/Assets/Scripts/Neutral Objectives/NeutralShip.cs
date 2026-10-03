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

    private NeutralPatrol patrolGroup;
    private Vector2 patrolOffset;

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

            if (patrolGroup == null)
                movement.SetTargetDestinationRPC((Vector2)patrolRouteLocations[currentPatrolTarget].position);
            else
                movement.SetTargetDestinationWithSpeedCapRPC((Vector2)patrolRouteLocations[currentPatrolTarget].position + patrolOffset, patrolGroup.GetGroupSpeed(), patrolGroup.GetGroupRotationSpeed());

            moved = true;
        }
        if (movement.moving) moved = false;
    }

    public void SetupShipSpawn(Transform spawnObject, NeutralPatrol patrolGroup)
    {
        spawn = spawnObject;
        this.patrolGroup = patrolGroup;
        if (patrolGroup != null)
        {
            patrolOffset = transform.localPosition;
        }

        foreach (Transform patrolStop in spawn.Find("Patrol Route"))
            patrolRouteLocations.Add(patrolStop);
    }

    public new void DestroyShip(GameObject shipDamageCameFrom)
    {
        if (patrolGroup == null)
        {
            GameSceneManager.Singleton.neutralObjectivesManager.NeutralShipDeath(spawn);
            ReceiveNeutralObjectivePayoutRPC(shipDamageCameFrom.GetComponent<Ship>().OwnerClientId, goldOnKill);
        }
        else
        {
            if (patrolGroup.IsLastPatrolShipToDie(gameObject))
            {
                ReceiveNeutralObjectivePayoutRPC(shipDamageCameFrom.GetComponent<Ship>().OwnerClientId, patrolGroup.goldOnKill);
            }
        }
        GameSceneManager.Singleton.shipsInScene.Remove(gameObject);
        InformShipWhoKilled(shipDamageCameFrom);

        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ReceiveNeutralObjectivePayoutRPC(ulong damageDealersClientID, int payoutAmount)
    {
        //If my client ID is the one who killed the neutral ship, gain money
        if (damageDealersClientID == NetworkManager.LocalClientId)
        {
            PopupText popupText = Instantiate(popupTextPrefab, transform.position, Quaternion.identity).GetComponent<PopupText>();
            popupText.SetupText("+ $" + payoutAmount, Color.gold, 2.5f);
            Shop.Singleton.AddGold(payoutAmount);
        }
    }
}
