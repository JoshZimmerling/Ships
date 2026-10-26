using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DarkStarAbility : NetworkBehaviour
{
    // Dark Star ability settings
    [SerializeField] private float abilityCooldown = 0.5f;
    private float abilityTimer;

    [SerializeField] private int abilityRadius;

    private List<Ship> affectedShips = new List<Ship>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            transform.Find("Ability Aura").localScale = new Vector3(abilityRadius / 6.5f, abilityRadius / 6.5f);
        else
            transform.Find("Ability Aura").gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        if (!IsHost) return;

        abilityTimer -= Time.deltaTime;
        if (abilityTimer < 0)
        {
            List<Ship> newShipsInRange = new List<Ship>();
        
            // Get ships in range
            foreach (GameObject go in GameSceneManager.Singleton.shipsInScene)
            {
                Ship ship = go.GetComponent<Ship>();
                if (ship != null &&
                    ship.OwnerClientId == OwnerClientId &&
                    !ship.isMothership &&
                    (transform.position - go.transform.position).magnitude + ship.correctionFactor <= abilityRadius &&
                    ship.GetType() != typeof(NeutralShip))
                {
                    newShipsInRange.Add(ship);
                    ship.hiddenFromRadar.Value = true;
                }
            }

            // Remove ships not in range anymore
            for (int i = affectedShips.Count - 1; i > 0; i--)
            {
                Ship ship = affectedShips[i];
                if (!newShipsInRange.Contains(ship))
                {
                    affectedShips.Remove(ship);
                    ship.hiddenFromRadar.Value = false;
                }
                else
                {
                    newShipsInRange.Remove(ship);
                }
            }

            // Add new ships to running list
            foreach (Ship ship in newShipsInRange)
            {
                affectedShips.Add(ship);
            }
            abilityTimer = abilityCooldown;
        }
    }
}
