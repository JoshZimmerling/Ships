using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    float dmg;
    float maxbulletLifetime;
    float bulletSpeed;
    Turret.TurretType turretType;
    Transform target;

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
    }

    float bulletLifetime = 0;
    void FixedUpdate()
    {
        if (!IsHost) return;

        if (turretType == Turret.TurretType.MissilePods && target != null)
        {
            //transform.LookAt(target, new Vector3(0, 0, 1)); 
            //transform.Translate((target.position - transform.position).normalized * bulletSpeed * Time.deltaTime);

            Vector2 direction = target.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
        //else
        
        transform.Translate(new Vector2(0, 1) * bulletSpeed * Time.deltaTime);
        bulletLifetime += Time.deltaTime;    

        if (bulletLifetime > maxbulletLifetime)
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

    public void SetupBullet(float lifetime, float damage, float speed, Turret.TurretType type)
    {
        maxbulletLifetime = lifetime;
        dmg = damage;
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

    public void SetupBullet(float lifetime, float damage, float speed, Turret.TurretType type, Transform ship)
    {
        Debug.Log("spawned missile");
        Debug.Log("Target: " + ship.position);
        maxbulletLifetime = lifetime;
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
