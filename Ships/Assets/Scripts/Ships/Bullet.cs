using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    float dmg;
    float bulletSpeed;

    Vector2 spawnPos;
    float sqrRange;
    Turret.TurretType turretType;
    Transform target;

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
    }

    float bulletLifetime = 0;
    void FixedUpdate()
    {
        if (!IsHost || spawnPos == null) return;

        if (turretType == Turret.TurretType.MissilePods && target != null)
        {
            Vector2 direction = target.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
        
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
            if (collision.GetComponent<Ship>().OwnerClientId == this.OwnerClientId)
                return;
            else
                collision.GetComponent<Ship>().DoDamage(dmg);

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
        switch (type)
        {
            case Turret.TurretType.MissilePods:
                transform.localScale = new Vector3(1f, 1f, 1);
                break;
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

    public void SetupBullet(float range, float damage, float speed, Turret.TurretType type, Transform ship)
    {
        spawnPos = transform.position;
        sqrRange = range * range;
        dmg = damage;
        bulletSpeed = speed;
        turretType = type;
        target = ship;
        transform.localScale = new Vector3(1, 1, 1);
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
