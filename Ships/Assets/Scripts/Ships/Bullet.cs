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

    public override void OnNetworkSpawn()
    {
        GetComponent<SpriteRenderer>().color = PlayerDataList.Singleton.players[OwnerClientId].playerColor;
    }

    float bulletLifetime = 0;
    void FixedUpdate()
    {
        if (!IsHost) return;

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
        Destroy(this.gameObject);
    }

    public void SetupBullet(float lifetime, float damage, float speed, Turret.TurretType type)
    {
        maxbulletLifetime = lifetime;
        dmg = damage;
        bulletSpeed = speed;
        turretType = type;
        switch (type)
        {
            case Turret.TurretType.HeavyTurret:
                transform.localScale = new Vector3(0.3f, 0.3f, 1);
                break;
            case Turret.TurretType.MediumTurret:
                transform.localScale = new Vector3(0.2f, 0.2f, 1);
                break;
            case Turret.TurretType.LightTurret:
                transform.localScale = new Vector3(0.1f, 0.1f, 1);
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
