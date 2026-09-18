using Unity.Netcode;
using UnityEngine;
using static Ship;

public class MothershipAbility : NetworkBehaviour
{
    private float abilityTimer = 0;
    // Mothership ability settings
    [SerializeField] private int mothershipHealRadius = 20;
    [SerializeField] private int mothershipHealTimer = 2;
    [SerializeField] private int mothershipHealAmount = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            transform.Find("Heal Aura").localScale = new Vector3(mothershipHealRadius / 6.5f, mothershipHealRadius / 6.5f);
        else
            transform.Find("Heal Aura").gameObject.SetActive(false);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!IsHost) return;
        abilityTimer -= Time.deltaTime;
        if (abilityTimer < 0)
        {
            foreach (GameObject go in GameSceneManager.Singleton.shipsInScene)
            {
                Ship ship = go.GetComponent<Ship>();
                if (ship != null &&
                    ship.OwnerClientId == OwnerClientId &&
                    ship.shipType != ShipTypes.Mothership &&
                    (transform.position - go.transform.position).magnitude + ship.correctionFactor <= mothershipHealRadius &&
                    ship.currentShipHP.Value != ship.maxShipHP)
                {
                    ship.currentShipHP.Value = Mathf.Min(ship.currentShipHP.Value + mothershipHealAmount, ship.maxShipHP);
                }
            }
            abilityTimer = mothershipHealTimer;
        }
    }
}
