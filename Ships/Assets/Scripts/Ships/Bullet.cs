using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;

    Vector2 spawnPos;
    float sqrRange;
    Turret.TurretType turretType;

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
    }

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
        if (!IsHost) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ship"))
        {
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg);
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Missile"))
        {
            if (collision.GetComponent<Missile>().OwnerClientId == this.OwnerClientId)
                return;
        }

        GetComponent<NetworkObject>().Despawn();
        Destroy(this);
    }

    public void SetupBullet(float range, float damage, float speed, Turret.TurretType type)
    {
        spawnPos = transform.position;
        dmg = damage;
        range = range + 5;
        sqrRange = range * range;
        bulletSpeed = speed;
        turretType = type;
        switch (turretType)
        {
            case Turret.TurretType.HeavyTurret:
                transform.localScale = new Vector3(0.6f, 0.6f, 1);
                break;
            case Turret.TurretType.MediumTurret:
                transform.localScale = new Vector3(0.4f, 0.4f, 1);
                break;
            case Turret.TurretType.LightTurret:
                transform.localScale = new Vector3(0.2f, 0.2f, 1);
                break;
        }
    }
    /*
    [ClientRpc]
    public void SetupBulletClientRPC(float lifetime, float damage, float speed)
    {
        maxbulletLifetime = lifetime;
        dmg = damage;
        bulletSpeed = speed;
    }
    */
}
