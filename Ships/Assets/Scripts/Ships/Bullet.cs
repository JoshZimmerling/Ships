using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;
    float slowShipPercentage;
    float slowShipDuration;

    Vector2 spawnPos;
    float sqrRange;
    Turret.TurretType turretType;
    public GameObject parentShip;

    public bool isFromNeutralShip = false;
    private Color neutralShipColor = new Color(212/255f, 175/255f, 55/255f);

    [SerializeField] private GameObject explosionPrefab;

    private bool destroyInProgress = false;

    void FixedUpdate()
    {
        if (!IsHost || spawnPos == null) return;
        
        transform.Translate(new Vector2(0, 1) * bulletSpeed * Time.deltaTime);        
        if (Vector2.SqrMagnitude(spawnPos - (Vector2)transform.position) > sqrRange)
        {
            GetComponent<NetworkObject>().Despawn();
            Destroy(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost || destroyInProgress) return;

        if (collision.gameObject.name == "Challenger Shield")
        {
            if(collision.transform.parent.GetComponent<Ship>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;
        }
        else if (collision.transform.parent.GetComponent<NeutralShip>() != null)
        {
            if (isFromNeutralShip)
                return;

            collision.transform.parent.GetComponent<NeutralShip>().DoDamage(dmg, parentShip);
            if (slowShipDuration > 0 && slowShipPercentage > 0)
                collision.transform.parent.GetComponent<Movement>().ChangeSpeed(slowShipPercentage, slowShipDuration);
        }
        else if (collision.transform.parent.GetComponent<Ship>() != null)
        {
            if (collision.transform.parent.GetComponent<Ship>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;

            collision.transform.parent.GetComponent<Ship>().DoDamage(dmg, parentShip);
            if (slowShipDuration > 0 && slowShipPercentage > 0)
                collision.transform.parent.GetComponent<Movement>().ChangeSpeed(slowShipPercentage, slowShipDuration);
        }
        else if (collision.GetComponent<Missile>() != null)
        {
            if ((collision.GetComponent<Missile>().OwnerClientId == this.OwnerClientId && !collision.GetComponent<Missile>().isFromNeutralShip && !isFromNeutralShip) || (isFromNeutralShip && collision.GetComponent<Missile>().isFromNeutralShip))
                return;
        }
        else if (collision.GetComponentInChildren<RaptorAbility>() != null)
        {
            if (collision.GetComponentInChildren<RaptorAbility>().OwnerClientId == this.OwnerClientId && !isFromNeutralShip)
                return;
        }

        destroyInProgress = true;
        GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }

    public void SetupBullet(float range, float damage, float speed, float slowPercentage, float slowDuration, Turret.TurretType type, GameObject shipWhoShot)
    {
        spawnPos = transform.position;
        dmg = damage;
        range = range + 5;
        sqrRange = range * range;
        bulletSpeed = speed;
        slowShipPercentage = 1f - (slowPercentage / 100f);
        slowShipDuration = slowDuration;
        turretType = type;
        parentShip = shipWhoShot;
        isFromNeutralShip = shipWhoShot.GetComponent<NeutralShip>() != null;
        switch (turretType)
        {
            case Turret.TurretType.HeavyTurret:
            case Turret.TurretType.HawkGun:
                transform.localScale = new Vector3(0.6f, 0.6f, 1);
                break;
            case Turret.TurretType.MediumTurret:
                transform.localScale = new Vector3(0.4f, 0.4f, 1);
                break;
            case Turret.TurretType.LightTurret:
                transform.localScale = new Vector3(0.2f, 0.2f, 1);
                break;
        }

        if (isFromNeutralShip)
            SetBulletColorRPC(neutralShipColor);
        else
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                if (client.ClientId == OwnerClientId)
                    SetBulletColorRPC(client.PlayerObject.GetComponent<PlayerData>().playerColor);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetBulletColorRPC(Color bulletColor)
    {
        GetComponent<SpriteRenderer>().color = bulletColor;
    }

    public override void OnNetworkDespawn()
    {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.transform.localScale = transform.localScale;
        Destroy(explosion, 1f);
    }
}
