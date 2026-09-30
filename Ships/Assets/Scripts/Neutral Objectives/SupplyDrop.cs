using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SupplyDrop : NetworkBehaviour
{
    private float remainingTime;
    private int totalPayout;
    private Collider2D myCollider;
    private List<ulong> playersToPay;

    [SerializeField] GameObject popupTextPrefab;

    public void Setup(float lifetime, int payout)
    {
        remainingTime = lifetime;
        totalPayout = payout;

        myCollider = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        remainingTime -= Time.deltaTime;

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
            Ship shipScript = collider.gameObject.GetComponent<Ship>();
            if (shipScript != null && shipScript.shipType != Ship.ShipTypes.GoliathFighter && collider.gameObject.GetComponent<NeutralShip>() == null)
            {
                if (!playersToPay.Contains(shipScript.OwnerClientId))
                {
                    Debug.Log("Adding player " + shipScript.OwnerClientId + " to the payout list");
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

        GameObject.Find("Neutral Objectives Manager").GetComponent<NeutralObjectivesManager>().SupplyDropCompleted();

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
