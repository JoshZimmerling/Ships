using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SupplyDrop : NetworkBehaviour
{
    private Transform spawn;
    private float remainingTime;
    private float initialLifetime;
    private int totalPayout;
    private List<ulong> playersToPay;

    private Collider2D myCollider;
    Transform rotatingHand;

    [SerializeField] GameObject popupTextPrefab;

    public void Setup(Transform spawnObject, float lifetime, int payout)
    {
        spawn = spawnObject;
        initialLifetime = lifetime;
        remainingTime = lifetime;
        totalPayout = payout;

        myCollider = GetComponent<Collider2D>();
        rotatingHand = transform.Find("Rotating Hand");
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        remainingTime -= Time.deltaTime;
        rotatingHand.rotation = Quaternion.Euler(0, 0, -360f * (1 - (remainingTime/initialLifetime)));

        if (remainingTime < 0)
            DoPayout();
    }

    private void DoPayout()
    {
        playersToPay = new List<ulong>();

        List<Collider2D> collidersInSupplyDropArea = new List<Collider2D>();
        myCollider.Overlap(ContactFilter2D.noFilter, collidersInSupplyDropArea);
        foreach (Collider2D collider in collidersInSupplyDropArea)
        {
            Ship shipScript = collider.attachedRigidbody.GetComponent<Ship>();
            if (shipScript != null && shipScript.shipClass != Ship.ShipClasses.Minion && shipScript.shipClass != Ship.ShipClasses.NeutralShip)
            {
                if (!playersToPay.Contains(shipScript.OwnerClientId))
                {
                    playersToPay.Add(shipScript.OwnerClientId);
                }
            }
        }

        if (playersToPay.Count > 0)
        {
            foreach (ulong playerToPayId in playersToPay)
            {
                ReceiveNeutralObjectivePayoutRPC(playerToPayId, totalPayout / playersToPay.Count);
            }
        }

        GameObject.Find("Neutral Objectives Manager").GetComponent<NeutralObjectivesManager>().SupplyDropCompleted(spawn);

        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ReceiveNeutralObjectivePayoutRPC(ulong playersClientId, int payoutAmount)
    {
        //If my client ID matches, gain part of the payout
        if (playersClientId == NetworkManager.LocalClientId)
        {
            PopupText popupText = Instantiate(popupTextPrefab, transform.position, Quaternion.identity).GetComponent<PopupText>();
            popupText.SetupText("+ $" + payoutAmount, Color.gold, 2.5f);
            Shop.Singleton.AddGold(payoutAmount);
        }
    }
}
